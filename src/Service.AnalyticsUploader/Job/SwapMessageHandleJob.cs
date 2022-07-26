using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AmplitudeEvents;
using Service.AnalyticsUploader.Domain.Models.AppsflyerEvents;
using Service.AnalyticsUploader.Domain.Models.Constants;
using Service.AnalyticsUploader.Domain.NoSql;
using Service.ClientProfile.Grpc;
using Service.IndexPrices.Client;
using Service.Liquidity.Converter.Domain.Models;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class SwapMessageHandleJob : MessageHandleJobBase
	{

		private readonly ILogger<SwapMessageHandleJob> _logger;

		public SwapMessageHandleJob(ILogger<SwapMessageHandleJob> logger,
			ISubscriber<IReadOnlyList<SwapMessage>> subscriber,
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

		private async ValueTask HandleEvent(IReadOnlyList<SwapMessage> messages)
		{
			foreach (SwapMessage message in messages)
			{
				if (message.QuoteType != QuoteType.Market)
					continue;

				string clientId = message.AccountId1;

				_logger.LogInformation("Handle SwapMessage message, clientId: {clientId}.", clientId);

				await ToAppsflyer(message, clientId);
				await ToAmplitude(message, clientId);
			}
		}

		private async Task ToAppsflyer(SwapMessage message, string clientId)
		{
			decimal? amountUsd = GetAmountUsdValue(message.Volume2, message.AssetId2);
			if (amountUsd == null)
				return;

			await SendAppsflyerMessage(clientId, new ExchangingAssetEvent
			{
				TradeFee = message.FeeAmount,
				SourceCurrency = message.AssetId1,
				DestinationCurrency = message.AssetId2,
				QuoteId = message.Id,
				AmountUsd = amountUsd.Value,
				AutoTrade = false
			});
		}

		private async Task ToAmplitude(SwapMessage message, string clientId)
		{
			string feeAsset = message.FeeAsset;
			decimal feeAmount = message.FeeAmount;

			await SendAmplitudeRevenueMessage(clientId, new RevenueEvent
			{
				RevenueVolumeInAsset = feeAmount,
				RevenueAsset = feeAsset,
				RevenueVolumeInUsd = GetAmountUsdValue(feeAmount, feeAsset),
				RevenueType = RevenueType.ConvertFee
			});

			string markupAsset = message.AssetId1;
			decimal markupAmount = message.MarkUp;

			await SendAmplitudeRevenueMessage(clientId, new RevenueEvent
			{
				RevenueVolumeInAsset = markupAmount,
				RevenueAsset = markupAsset,
				RevenueVolumeInUsd = GetAmountUsdValue(markupAmount, markupAsset),
				RevenueType = RevenueType.Markup
			});
		}
	}
}