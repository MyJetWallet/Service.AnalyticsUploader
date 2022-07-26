using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AppsflyerEvents;
using Service.AnalyticsUploader.Domain.NoSql;
using Service.ClientProfile.Grpc;
using Service.HighYieldEngine.Domain.Models.Constants;
using Service.HighYieldEngine.Domain.Models.Messages;
using Service.IndexPrices.Client;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class EarnAnaliticsEventHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<EarnAnaliticsEventHandleJob> _logger;

		public EarnAnaliticsEventHandleJob(ILogger<EarnAnaliticsEventHandleJob> logger,
			ISubscriber<IReadOnlyList<EarnAnaliticsEvent>> registerSubscriber,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IAppsFlyerSender appsFlyerSender,
			IClientProfileService clientProfileService,
			IIndexPricesClient converter,
			IAmplitudeSender amplitudeSender,
			IAnalyticIdToClientManager analyticIdToClientManager) :
				base(logger, personalDataServiceGrpc, clientProfileService, appsFlyerSender, amplitudeSender, converter, analyticIdToClientManager)
		{
			_logger = logger;
			registerSubscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<EarnAnaliticsEvent> messages)
		{
			foreach (EarnAnaliticsEvent message in messages)
			{
				string clientId = message.ClientId;
				string offerId = message.OfferId;

				_logger.LogInformation("Handle EarnAnaliticsEvent message, clientId: {clientId}, offerId: {offerId}.", clientId, offerId);

				decimal amount = message.Amount;
				string asset = message.Asset;
				decimal balance = message.Balance;
				bool isHot = message.IsHot;
				bool? isTopUp = message.IsTopUp;
				decimal? apy = message.Apy;
				decimal currentApy = message.CurrentApy;

				IAnaliticsEvent analiticsEvent = message.ActionType == EarnAnaliticsEventType.Subscribe
					? (IAnaliticsEvent) new EarnSubscribeEvent
					{
						OfferId = offerId,
						Amount = amount,
						Asset = asset,
						Balance = balance,
						IsHot = isHot,
						IsTopUp = isTopUp,
						Apy = apy,
						CurrentApy = currentApy
					}
					: new EarnUnsubscribeEvent
					{
						OfferId = offerId,
						Amount = amount,
						Asset = asset,
						Balance = balance,
						IsHot = isHot,
						CurrentApy = currentApy
					};

				await SendAppsflyerMessage(clientId, analiticsEvent);
			}
		}
	}
}