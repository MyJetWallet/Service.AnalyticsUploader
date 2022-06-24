using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class WithdrawalHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<WithdrawalHandleJob> _logger;

		public WithdrawalHandleJob(ILogger<WithdrawalHandleJob> logger,
			ISubscriber<IReadOnlyList<Withdrawal>> subscriber,
			IAppsFlyerSender sender,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IClientProfileService clientProfileService) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<Withdrawal> messages)
		{
			foreach (Withdrawal message in messages)
			{
				if (message.Status != WithdrawalStatus.Success)
					continue;

				string clientId = message.ClientId;

				_logger.LogInformation("Handle Withdrawal message, clientId: {clientId}.", message.ClientId);

				string destinationClientId = await GetExternalClientId(message.DestinationClientId);
				if (destinationClientId == null)
					continue;

				IAnaliticsEvent analiticsEvent;

				if (message.DestinationClientId == clientId)
				{
					analiticsEvent = message.IsInternal
						? (IAnaliticsEvent) new RecieveTransferFromInternalWalletEvent
						{
							Amount = message.Amount,
							Currency = message.AssetSymbol,
							Sender = destinationClientId,
							Network = message.Blockchain //todo
						}
						: new RecieveDepositFromExternalWalletEvent
						{
							Amount = message.Amount,
							Currency = message.AssetSymbol,
							Network = message.Blockchain //todo
						};
				}
				else
				{
					analiticsEvent = message.IsInternal
						? (IAnaliticsEvent) new SendTransferByWalletInternalEvent
						{
							Amount = message.Amount,
							Currency = message.AssetSymbol,
							Receiver = destinationClientId,
							Network = message.Blockchain //todo
						}
						: new SendTransferByWalletExternalEvent
						{
							Amount = message.Amount,
							Currency = message.AssetSymbol,
							Receiver = destinationClientId,
							Network = message.Blockchain //todo
						};
				}

				await SendMessage(clientId, analiticsEvent);
			}
		}
	}
}