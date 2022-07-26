using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AppsflyerEvents;
using Service.AnalyticsUploader.Domain.NoSql;
using Service.AutoInvestManager.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.IndexPrices.Client;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class InvestOrderHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<InvestOrderHandleJob> _logger;

		public InvestOrderHandleJob(ILogger<InvestOrderHandleJob> logger,
			ISubscriber<IReadOnlyList<InvestOrder>> subscriber,
			IAppsFlyerSender appsFlyerSender,
			IClientProfileService clientProfileService,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IIndexPricesClient converter,
			IAmplitudeSender amplitudeSender,
			IAnalyticIdToClientManager analyticIdToClientManager) :
				base(logger, personalDataServiceGrpc, clientProfileService, appsFlyerSender, amplitudeSender, converter, analyticIdToClientManager)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<InvestOrder> messages)
		{
			foreach (InvestOrder message in messages)
			{
				if (message.Status != OrderStatus.Executed)
					continue;

				string clientId = message.ClientId;

				_logger.LogInformation("Handle InvestOrder message, clientId: {clientId}.", clientId);

				decimal amountUsd = GetAmountUsdValue(message.ToAmount, message.ToAsset);

				IAnaliticsEvent analiticsEvent = new ExchangingAssetEvent
				{
					TradeFee = message.FeeAmount,
					SourceCurrency = message.FromAsset,
					DestinationCurrency = message.ToAsset,
					QuoteId = message.QuoteId,
					AmountUsd = amountUsd,
					AutoTrade = true,
					RecurringOrderId = message.Id,
					Frequency = GetFrequency(message.ScheduleType)
				};

				await SendAppsflyerMessage(clientId, analiticsEvent);
			}
		}

		private static string GetFrequency(ScheduleType scheduleType)
		{
			return scheduleType switch {
				ScheduleType.Daily => "daily",
				ScheduleType.Weekly => "weekly",
				ScheduleType.Biweekly => "bi-weekly",
				ScheduleType.Monthly => "monthly",
				_ => "one-time"
				};
		}
	}
}