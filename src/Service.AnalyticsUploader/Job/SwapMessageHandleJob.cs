using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.ClientProfile.Grpc;
using Service.IndexPrices.Client;
using Service.IndexPrices.Domain.Models;
using Service.Liquidity.Converter.Domain.Models;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class SwapMessageHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<SwapMessageHandleJob> _logger;
		private readonly IConvertIndexPricesClient _pricesConverter;

		public SwapMessageHandleJob(ILogger<SwapMessageHandleJob> logger,
			ISubscriber<IReadOnlyList<SwapMessage>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService,
			IPersonalDataServiceGrpc personalDataServiceGrpc, IConvertIndexPricesClient pricesConverter) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			_pricesConverter = pricesConverter;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<SwapMessage> messages)
		{
			foreach (SwapMessage message in messages)
			{
				if (message.QuoteType != QuoteType.Market)
					continue;

				string clientId = message.AccountId1;

				_logger.LogInformation("Handle SwapMessage message, clientId: {clientId}.", clientId);

				decimal? amountUsd = ConvertToAsset(message.AssetId2, UsdAsset, decimal.Parse(message.Volume2), _pricesConverter, _logger);

				IAnaliticsEvent analiticsEvent = new ExchangingAssetEvent
				{
					TradeFee = message.FeeAmount,
					SourceCurrency = message.AssetId1,
					DestinationCurrency = message.AssetId2,
					QuoteId = message.Id,
					AmountUsd = amountUsd.GetValueOrDefault(),
					AutoTrade = false
				};

				await SendMessage(clientId, analiticsEvent);
			}
		}

		public static decimal? ConvertToAsset(string amountAsset, string targetAsset, decimal amount, IConvertIndexPricesClient converter, ILogger logger)
		{
			(ConvertIndexPrice price, decimal value) = converter.GetConvertIndexPriceByPairVolumeAsync(amountAsset, targetAsset, amount);

			if (!string.IsNullOrWhiteSpace(price.Error))
			{
				logger.LogError("Can't convert {amount} {asset} to {target}, error: {error}", amount, amountAsset, targetAsset, price.Error);
				return null;
			}

			return value;
		}
	}
}