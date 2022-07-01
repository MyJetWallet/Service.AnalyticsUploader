using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.AutoInvestManager.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.IndexPrices.Client;
using Service.IndexPrices.Domain.Models;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class InvestOrderHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<InvestOrderHandleJob> _logger;
		private readonly IIndexPricesClient _converter;

		public InvestOrderHandleJob(ILogger<InvestOrderHandleJob> logger,
			ISubscriber<IReadOnlyList<InvestOrder>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IIndexPricesClient converter) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			_converter = converter;
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

				decimal amountUsd = GetAmountUsdValue(message);

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

				await SendMessage(clientId, analiticsEvent);
			}
		}

		private decimal GetAmountUsdValue(InvestOrder message)
		{
			decimal amount = message.ToAmount;
			if (amount == 0m)
				return 0m;

			(IndexPrice _, decimal usdValue) = _converter.GetIndexPriceByAssetVolumeAsync(message.ToAsset, amount);

			return usdValue;
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