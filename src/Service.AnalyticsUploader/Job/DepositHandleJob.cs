using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.Grpc.Models;
using Service.ClientProfile.Grpc;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class DepositHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<DepositHandleJob> _logger;
		private readonly IDepositService _depositService;

		public DepositHandleJob(ILogger<DepositHandleJob> logger,
			ISubscriber<IReadOnlyList<Deposit>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IDepositService depositService) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			_depositService = depositService;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<Deposit> messages)
		{
			foreach (Deposit message in messages)
			{
				if (message.Status != DepositStatus.Processed)
					continue;

				string clientId = message.ClientId;

				IAnaliticsEvent analiticsEvent = GetEvent(message);
				if (analiticsEvent == null)
					return;

				_logger.LogInformation("Handle Deposit message, clientId: {clientId}.", clientId);

				await SendMessage(clientId, analiticsEvent);
			}
		}

		private static IAnaliticsEvent GetEvent(Deposit message)
		{
			switch (message.Integration)
			{
				case "Simplex+Fireblocks":
					return new BuyFromCardSimplexEvent
					{
						PaidAmount = message.SimplexData.FromAmount + message.SimplexData.Fee,
						PaidCurrency = message.SimplexData.FromCurrency,
						ReceivedAmount = message.Amount,
						ReceivedCurrency = message.AssetSymbol,
					};
				case "CircleCard":
					return new BuyFromCardCircleEvent
					{
						PaidAmount = message.IncomingAmount + message.IncomingFeeAmount,
						PaidCurrency = message.AssetSymbol,
						ReceivedAmount = message.Amount,
						ReceivedCurrency = message.AssetSymbol,
					};
				case "Fireblocks":
					return new RecieveDepositFromExternalWalletEvent
					{
						Amount = message.Amount,
						Currency = message.AssetSymbol,
						Network = message.Network
					};
				default:
					return null;
			}
		}

		private async Task<bool> GetFirstTimeBuy(string clientId)
		{
			GetDepositsCountResponse response = await _depositService.GetDepositsCount(new GetDepositsCountRequest
			{
				ClientId = clientId
			});

			return response?.Count <= 1;
		}
	}
}