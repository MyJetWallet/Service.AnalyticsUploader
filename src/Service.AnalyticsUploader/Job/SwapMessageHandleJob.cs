using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.AnalyticsUploader.Services;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;
using Service.IndexPrices.Client;
using Service.IndexPrices.Domain.Models;
using Service.Liquidity.Converter.Domain.Models;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Contracts;
using Service.PersonalData.Grpc.Models;

namespace Service.AnalyticsUploader.Job
{
	public class SwapMessageHandleJob
	{
		private const string UsdAsset = "USD";

		private readonly ILogger<SwapMessageHandleJob> _logger;
		private readonly IAppsFlyerSender _sender;
		private readonly IClientProfileService _clientProfileService;
		private readonly IPersonalDataServiceGrpc _personalDataServiceGrpc;
		private readonly IConvertIndexPricesClient _pricesConverter;

		public SwapMessageHandleJob(ILogger<SwapMessageHandleJob> logger,
			ISubscriber<IReadOnlyList<SwapMessage>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService, IPersonalDataServiceGrpc personalDataServiceGrpc, IConvertIndexPricesClient pricesConverter)
		{
			_logger = logger;
			_sender = sender;
			_clientProfileService = clientProfileService;
			_personalDataServiceGrpc = personalDataServiceGrpc;
			_pricesConverter = pricesConverter;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<SwapMessage> messages)
		{
			List<string> clientIds = messages.Select(message => message.AccountId1).ToList();

			var request = new GetByIdsRequest
			{
				Ids = clientIds
			};

			PersonalDataBatchResponseContract personalDataResponse = await _personalDataServiceGrpc.GetByIdsAsync(request);
			if (personalDataResponse == null)
			{
				_logger.LogError("Can't get personal data with request: {@request}", request);
				return;
			}

			PersonalDataGrpcModel[] personalDataItems = personalDataResponse.PersonalDatas.ToArray();

			foreach (SwapMessage message in messages)
			{
				if (message.QuoteType != QuoteType.Market)
					continue;

				string clientId = message.AccountId1;
				_logger.LogInformation("Handle SwapMessage message, clientId: {clientId}.", clientId);

				PersonalDataGrpcModel personalData = personalDataItems.FirstOrDefault(model => model.Id == clientId);
				if (personalData == null)
				{
					_logger.LogError("Can't get personal data for clientId: {clientId}", clientId);
					continue;
				}

				var userAgent = "Web"; //todo
				string applicationId = ApplicationHelper.GetApplicationId(userAgent);
				if (applicationId == null)
				{
					_logger.LogWarning("Can't detect mobile os version for UserAgent: {agent}, analitics upload skipped.", userAgent);
					continue;
				}

				string cuid = await GetExternalClientId(clientId);
				if (cuid == null)
					continue;

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
				
				await _sender.SendMessage(applicationId, analiticsEvent, cuid);
			}
		}

		private async Task<string> GetExternalClientId(string clientId)
		{
			ClientProfile.Domain.Models.ClientProfile clientProfile = await _clientProfileService.GetOrCreateProfile(new GetClientProfileRequest
			{
				ClientId = clientId
			});

			string id = clientProfile?.ExternalClientId;

			if (id == null)
				_logger.LogError("Can't get client profile for clientId: {clientId}", clientId);

			return id;
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