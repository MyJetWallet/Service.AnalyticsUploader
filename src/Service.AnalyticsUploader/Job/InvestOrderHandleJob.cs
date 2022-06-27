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
		private readonly IConvertIndexPricesClient _pricesConverter;

		public InvestOrderHandleJob(ILogger<InvestOrderHandleJob> logger,
			ISubscriber<IReadOnlyList<InvestOrder>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService,
			IPersonalDataServiceGrpc personalDataServiceGrpc, 
			IConvertIndexPricesClient pricesConverter) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			_pricesConverter = pricesConverter;
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

				decimal? amountUsd = ConvertToAsset(message.ToAsset, UsdAsset, message.ToAmount, _pricesConverter, _logger);

				IAnaliticsEvent analiticsEvent = new ExchangingAssetEvent
				{
					TradeFee = message.FeeAmount,
					SourceCurrency = message.FromAsset,
					DestinationCurrency = message.ToAsset,
					QuoteId = message.QuoteId,
					AmountUsd = amountUsd.GetValueOrDefault(),
					AutoTrade = true,
					RecurringOrderId = message.Id, //todo
					Frequency = GetFrequency(message.ScheduleType)
				};

				await SendMessage(clientId, analiticsEvent);
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

		public static decimal? ConvertToAsset(string amountAsset, string targetAsset, decimal amount, IConvertIndexPricesClient converter, ILogger logger)
		{
			(ConvertIndexPrice price, decimal value) = converter.GetConvertIndexPriceByPairVolumeAsync(amountAsset, targetAsset, amount);
			if (string.IsNullOrWhiteSpace(price.Error)) 
				return value;

			logger.LogError("Can't convert {amount} {asset} to {target}, error: {error}", amount, amountAsset, targetAsset, price.Error);

			return null;
		}
	}
}