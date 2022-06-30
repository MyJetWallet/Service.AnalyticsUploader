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
		private const string SimplexFireblocksIntergationName = "Simplex+Fireblocks";
		private const string CirclecardIntergationName = "CircleCard";
		private const string FireblocksIntergationName = "Fireblocks";

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
				decimal amount = message.Amount;
				string currency = message.AssetSymbol;
				string integration = message.Integration;

				IAnaliticsEvent analiticsEvent = null;
				string firstTimeMethod = null;

				switch (integration)
				{
					case SimplexFireblocksIntergationName:
						analiticsEvent = new BuyFromCardSimplexEvent
						{
							PaidAmount = message.SimplexData.FromAmount + message.SimplexData.Fee,
							PaidCurrency = message.SimplexData.FromCurrency,
							ReceivedAmount = amount,
							ReceivedCurrency = currency,
						};
						firstTimeMethod = "Simplex";
						break;
					case CirclecardIntergationName:
						analiticsEvent = new BuyFromCardCircleEvent
						{
							PaidAmount = message.IncomingAmount + message.IncomingFeeAmount,
							PaidCurrency = currency,
							ReceivedAmount = amount,
							ReceivedCurrency = currency,
						};
						firstTimeMethod = "Circle";
						break;
					case FireblocksIntergationName:
						analiticsEvent = new RecieveDepositFromExternalWalletEvent
						{
							Amount = amount,
							Currency = currency,
							Network = message.Network
						};
						firstTimeMethod = "Transfer from external wallet";
						break;
				}

				if (analiticsEvent == null)
					return;

				_logger.LogInformation("Handle Deposit message, clientId: {clientId}.", clientId);

				await SendMessage(clientId, analiticsEvent);

				if (await CheckFirstTime(clientId, integration))
					await SendMessage(clientId, new FirstTimeBuyEvent
					{
						Amount = amount,
						Currency = currency,
						Method = firstTimeMethod
					});
			}
		}

		private async Task<bool> CheckFirstTime(string clientId, string integration)
		{
			GetDepositsCountResponse response = await _depositService.GetDepositsCount(new GetDepositsCountRequest
			{
				ClientId = clientId,
				Integration = integration
			});

			return response?.Count <= 1;
		}
	}
}