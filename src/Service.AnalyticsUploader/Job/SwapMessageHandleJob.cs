using System.Collections.Generic;
using System.Globalization;
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
		private readonly IIndexPricesClient _converter;

		public SwapMessageHandleJob(ILogger<SwapMessageHandleJob> logger,
			ISubscriber<IReadOnlyList<SwapMessage>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService,
			IPersonalDataServiceGrpc personalDataServiceGrpc, IIndexPricesClient converter) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			_converter = converter;
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

				decimal? amountUsd = GetAmountUsdValue(message);
				if (amountUsd == null)
					continue;

				IAnaliticsEvent analiticsEvent = new ExchangingAssetEvent
				{
					TradeFee = message.FeeAmount,
					SourceCurrency = message.AssetId1,
					DestinationCurrency = message.AssetId2,
					QuoteId = message.Id,
					AmountUsd = amountUsd.Value,
					AutoTrade = false
				};

				await SendMessage(clientId, analiticsEvent);
			}
		}

		private decimal? GetAmountUsdValue(SwapMessage message)
		{
			string amountStr = message.Volume2;

			if (!decimal.TryParse(amountStr, NumberStyles.AllowDecimalPoint, new NumberFormatInfo(), out decimal amount))
			{
				_logger.LogError("Can't get decimal amount value from \"Volume2\", string \"{value}\", SwapMessage: {@message}.", amountStr, message);
				return null;
			}

			if (amount == 0m)
				return 0m;

			(IndexPrice _, decimal usdValue) = _converter.GetIndexPriceByAssetVolumeAsync(message.AssetId2, amount);

			return usdValue;
		}
	}
}