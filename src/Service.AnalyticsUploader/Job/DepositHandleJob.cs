using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class DepositHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<DepositHandleJob> _logger;

		public DepositHandleJob(ILogger<DepositHandleJob> logger,
			ISubscriber<IReadOnlyList<Deposit>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService, IPersonalDataServiceGrpc personalDataServiceGrpc) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<Deposit> messages)
		{
			foreach (Deposit message in messages)
			{
				if (message.Status != DepositStatus.Processed)
					continue;

				string clientId = message.ClientId;

				_logger.LogInformation("Handle Deposit message, clientId: {clientId}.", clientId);

				IAnaliticsEvent analiticsEvent;

				if (message.SimplexData != null)
				{
					analiticsEvent = new BuyFromCardSimplexEvent
					{
						PaidAmount = message.SimplexData.FromAmount.ToString(CultureInfo.InvariantCulture),
						PaidCurrency = message.SimplexData.FromCurrency,
						ReceivedAmount = message.Amount.ToString(CultureInfo.InvariantCulture),
						ReceivedCurrency = message.AssetSymbol,
						FirstTimeBuy = false //todo
					};
				}
				else 
				{
					analiticsEvent = new RecieveDepositFromExternalWalletEvent
					{
						Amount = message.Amount,
						Currency = message.AssetSymbol,
						Network = message.Network
					};
				}

				await SendMessage(clientId, analiticsEvent);
			}
		}
	}
}