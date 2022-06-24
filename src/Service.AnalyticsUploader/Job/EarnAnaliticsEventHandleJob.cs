using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.ClientProfile.Grpc;
using Service.HighYieldEngine.Domain.Models.Constants;
using Service.HighYieldEngine.Domain.Models.Messages;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class EarnAnaliticsEventHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<EarnAnaliticsEventHandleJob> _logger;

		public EarnAnaliticsEventHandleJob(ILogger<EarnAnaliticsEventHandleJob> logger,
			ISubscriber<IReadOnlyList<EarnAnaliticsEvent>> registerSubscriber,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			registerSubscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<EarnAnaliticsEvent> messages)
		{
			foreach (EarnAnaliticsEvent message in messages)
			{
				string clientId = message.ClientId;

				_logger.LogInformation("Handle EarnAnaliticsEvent message, clientId: {clientId}, offerId: {offerId}.", clientId, messages);

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

				await SendMessage(clientId, analiticsEvent);
			}
		}
	}
}