using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Circle.Models.Payments;
using MyJetWallet.Circle.Models.Payouts;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.Circle.Webhooks.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class SignalCircleTransferHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<SignalCircleTransferHandleJob> _logger;

		public SignalCircleTransferHandleJob(ILogger<SignalCircleTransferHandleJob> logger,
			ISubscriber<IReadOnlyList<SignalCircleTransfer>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService, IPersonalDataServiceGrpc personalDataServiceGrpc) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<SignalCircleTransfer> messages)
		{
			foreach (SignalCircleTransfer message in messages)
			{
				PaymentInfo paymentInfo = message.PaymentInfo;
				if (paymentInfo.Source.Type != "card")
					continue;

				string clientId = message.ClientId;

				_logger.LogInformation("Handle SignalCircleTransfer message, clientId: {clientId}.", clientId);

				CircleAmount captureAmount = paymentInfo.CaptureAmount;
				CircleAmount amount = paymentInfo.Amount;

				IAnaliticsEvent analiticsEvent = new BuyFromCardCircleEvent
				{
					PaidAmount = captureAmount.Amount,
					PaidCurrency = captureAmount.Currency,
					ReceivedAmount = amount.Amount,
					ReceivedCurrency = amount.Currency,
					FirstTimeBuy = false //todo
				};

				await SendMessage(clientId, analiticsEvent);
			}
		}
	}
}