using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.AnalyticsUploader.Services;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;
using Service.HighYieldEngine.Domain.Models.Constants;
using Service.HighYieldEngine.Domain.Models.Messages;

namespace Service.AnalyticsUploader.Job
{
	public class EarnAnaliticsEventHandleJob
	{
		private readonly ILogger<EarnAnaliticsEventHandleJob> _logger;
		private readonly IAppsFlyerSender _sender;
		private readonly IClientProfileService _clientProfileService;

		public EarnAnaliticsEventHandleJob(ILogger<EarnAnaliticsEventHandleJob> logger,
			ISubscriber<IReadOnlyList<EarnAnaliticsEvent>> registerSubscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService)
		{
			_logger = logger;
			_sender = sender;
			_clientProfileService = clientProfileService;
			registerSubscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<EarnAnaliticsEvent> messages)
		{
			foreach (EarnAnaliticsEvent message in messages)
			{
				string clientId = message.ClientId;
				_logger.LogInformation("Handle EarnAnaliticsEvent message, clientId: {clientId}, offerId: {offerId}.", clientId, messages);

				var userAgent = "Web"; //todo
				string applicationId = ApplicationHelper.GetApplicationId(userAgent);
				if (applicationId == null)
				{
					_logger.LogWarning("Can't detect mobile os version for UserAgent: {agent}, analitics upload skipped.", userAgent);
					continue;
				}

				string cuid = await GetExternalClientId(clientId);
				if (cuid == null)
					continue;

				IAnaliticsEvent analiticsEvent = message.ActionType == EarnAnaliticsEventType.Subscribe
					? (IAnaliticsEvent) new EarnSubscribeEvent
					{
						OfferId = message.OfferId,
						Amount = message.Amount,
						Asset = message.Asset,
						Balance = message.Balance,
						IsHot = message.IsHot,
						IsTopUp = message.IsTopUp,
						Apy = message.Apy,
						CurrentApy = message.CurrentApy
					}
					: new EarnUnsubscribeEvent
					{
						OfferId = message.OfferId,
						Amount = message.Amount,
						Asset = message.Asset,
						Balance = message.Balance,
						IsHot = message.IsHot,
						CurrentApy = message.CurrentApy
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