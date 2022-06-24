using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.AnalyticsUploader.Services;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;

namespace Service.AnalyticsUploader.Job
{
	public class WithdrawalHandleJob
	{
		private readonly ILogger<WithdrawalHandleJob> _logger;
		private readonly IAppsFlyerSender _sender;
		private readonly IClientProfileService _clientProfileService;

		public WithdrawalHandleJob(ILogger<WithdrawalHandleJob> logger,
			ISubscriber<IReadOnlyList<Withdrawal>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService)
		{
			_logger = logger;
			_sender = sender;
			_clientProfileService = clientProfileService;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<Withdrawal> messages)
		{
			foreach (Withdrawal message in messages)
			{
				string clientId = message.ClientId;
				_logger.LogInformation("Handle Withdrawal message, clientId: {clientId}.", message.ClientId);

				var userAgent = "Web"; //todo
				string applicationId = ApplicationHelper.GetApplicationId(userAgent);
				if (applicationId == null)
				{
					_logger.LogWarning("Can't detect mobile os version for UserAgent: {agent}, analitics upload skipped.", userAgent);
					continue;
				}

				string cuid = await GetExternalClientId(clientId);
				string destinationClientId = await GetExternalClientId(message.DestinationClientId);
				if (cuid == null || destinationClientId == null)
					continue;

				IAnaliticsEvent analiticsEvent = message.IsInternal
					? (IAnaliticsEvent) new TransferByWalletInternalEvent
					{
						Amount = message.Amount,
						Currency = message.AssetSymbol,
						Receiver = destinationClientId
					}
					: new TransferByWalletExternalEvent
					{
						Amount = message.Amount,
						Currency = message.AssetSymbol,
						Receiver = destinationClientId,
						Network = message.Blockchain //todo
					};

				await _sender.SendMessage(applicationId, analiticsEvent, cuid);
			}
		}

		private async Task<string> GetExternalClientId(string clientId)
		{
			ClientProfile.Domain.Models.ClientProfile clientProfile = await _clientProfileService.GetOrCreateProfile(new GetClientProfileRequest
			{
				ClientId = clientId
			});

			string id = clientProfile?.ExternalClientId;

			if (id == null)
				_logger.LogError("Can't get client profile for clientId: {clientId}", clientId);

			return id;
		}
	}
}