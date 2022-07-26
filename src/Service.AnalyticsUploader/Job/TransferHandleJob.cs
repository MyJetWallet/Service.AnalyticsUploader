using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AppsflyerEvents;
using Service.AnalyticsUploader.Domain.NoSql;
using Service.ClientProfile.Grpc;
using Service.IndexPrices.Client;
using Service.InternalTransfer.Domain.Models;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class TransferHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<TransferHandleJob> _logger;

		public TransferHandleJob(ILogger<TransferHandleJob> logger,
			ISubscriber<IReadOnlyList<Transfer>> subscriber,
			IAppsFlyerSender appsFlyerSender,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IClientProfileService clientProfileService,
			IIndexPricesClient converter,
			IAmplitudeSender amplitudeSender,
			IAnalyticIdToClientManager analyticIdToClientManager) :
				base(logger, personalDataServiceGrpc, clientProfileService, appsFlyerSender, amplitudeSender, converter, analyticIdToClientManager)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<Transfer> messages)
		{
			foreach (Transfer message in messages)
			{
				if (message.Status != TransferStatus.Completed)
					continue;

				string clientId = message.ClientId;

				_logger.LogInformation("Handle Transfer message, clientId: {clientId}.", message.ClientId);

				string destinationCuid = await GetExternalClientId(message.DestinationClientId);
				if (destinationCuid == null)
					continue;

				decimal amount = message.Amount;
				string assetSymbol = message.AssetSymbol;

				await SendAppsflyerMessage(clientId, new SendTransferByPhoneEvent
				{
					Amount = amount,
					Currency = assetSymbol,
					Receiver = destinationCuid
				});

				await SendAppsflyerMessage(destinationCuid, new RecieveTransferByPhoneEvent
				{
					Amount = amount,
					Currency = assetSymbol,
					Sender = clientId
				});
			}
		}
	}
}