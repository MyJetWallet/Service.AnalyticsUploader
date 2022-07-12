using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.ServiceBus.SessionAudit.Models;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AmplitudeEvents;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;
using Service.IndexPrices.Client;
using Service.IndexPrices.Domain.Models;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Contracts;
using Service.PersonalData.Grpc.Models;

namespace Service.AnalyticsUploader.Job
{
	public abstract class MessageHandleJobBase
	{
		private readonly ILogger _logger;
		private readonly IPersonalDataServiceGrpc _personalDataServiceGrpc;
		private readonly IClientProfileService _clientProfileService;
		private readonly IAppsFlyerSender _appsFlyerSender;
		private readonly IAmplitudeSender _amplitudeSender;
		private readonly IIndexPricesClient _converter;

		protected MessageHandleJobBase(ILogger logger, 
			IPersonalDataServiceGrpc personalDataServiceGrpc, 
			IClientProfileService clientProfileService, 
			IAppsFlyerSender appsFlyerSender, 
			IAmplitudeSender amplitudeSender, 
			IIndexPricesClient converter)
		{
			_logger = logger;
			_personalDataServiceGrpc = personalDataServiceGrpc;
			_clientProfileService = clientProfileService;
			_appsFlyerSender = appsFlyerSender;
			_amplitudeSender = amplitudeSender;
			_converter = converter;
		}

		protected async Task<PersonalDataGrpcModel[]> GetPersonalData(List<string> clientIds)
		{
			var request = new GetByIdsRequest
			{
				Ids = clientIds
			};

			PersonalDataBatchResponseContract personalDataResponse = await _personalDataServiceGrpc.GetByIdsAsync(request);

			if (personalDataResponse == null)
			{
				_logger.LogError("Can't get personal data with request: {@request}", request);

				return Array.Empty<PersonalDataGrpcModel>();
			}

			PersonalDataGrpcModel[] personalDataItems = personalDataResponse.PersonalDatas.ToArray();

			return personalDataItems;
		}

		protected async Task<ClientProfile.Domain.Models.ClientProfile> GetClientProfile(string clientId)
		{
			var request = new GetClientProfileRequest
			{
				ClientId = clientId
			};

			ClientProfile.Domain.Models.ClientProfile clientProfile = await _clientProfileService.GetOrCreateProfile(request);

			if (clientProfile == null)
				_logger.LogError("Can't get client profile for clientId: {clientId}", clientId);

			return clientProfile;
		}

		protected async Task<string> GetExternalClientId(string clientId)
		{
			if (clientId == null)
				return null;

			ClientProfile.Domain.Models.ClientProfile profile = await GetClientProfile(clientId);

			return profile?.ExternalClientId;
		}

		protected string GetApplicationId(DeviceOperationSystem deviceOperationSystem)
		{
			switch (deviceOperationSystem)
			{
				case DeviceOperationSystem.Ios:
					return Program.Settings.AppsFlyerIosApplicationId;
				case DeviceOperationSystem.Android:
					return Program.Settings.AppsFlyerAndroidApplicationId;
				default:
					return Program.Settings.AppsFlyerDefaultApplicationId;
			}
		}

		protected async Task SendAppsflyerMessage(string clientId, IAnaliticsEvent analiticsEvent, string ipAddress = null)
		{
			ClientProfile.Domain.Models.ClientProfile clientProfile = await GetClientProfile(clientId);
			if (clientProfile == null)
				return;

			string applicationId = GetApplicationId(clientProfile.DeviceOperationSystem);
			if (string.IsNullOrWhiteSpace(applicationId))
			{
				_logger.LogWarning("Can't detect mobile os version for clientId: {clientId}, analitics upload skipped.", clientId);
				return;
			}

			string cuid = clientProfile.ExternalClientId;
			if (cuid == null)
			{
				_logger.LogWarning("Can't get externalId for clientId: {clientId}.", clientId);
				return;
			}

			await _appsFlyerSender.SendMessage(applicationId, analiticsEvent, cuid, ipAddress);
		}

		protected async Task SendAmplitudeRevenueMessage(string clientId, RevenueEvent revenueEvent)
		{
			string cuid = await GetExternalClientId(clientId);

			await _amplitudeSender.SendMessage(revenueEvent, cuid, revenueEvent.RevenueVolumeInUsd, revenueEvent.RevenueType);
		}

		protected decimal? GetAmountUsdValue(string amountStr, string asset)
		{
			if (decimal.TryParse(amountStr, NumberStyles.AllowDecimalPoint, new NumberFormatInfo(), out decimal amount))
				return GetAmountUsdValue(amount, asset);

			_logger.LogError("Can't get decimal amount value from \"Volume2\", string \"{value}\", asset: {@asset}.", amountStr, asset);

			return null;
		}

		protected decimal GetAmountUsdValue(decimal amount, string asset)
		{
			if (amount == 0m)
				return 0m;

			(IndexPrice _, decimal usdValue) = _converter.GetIndexPriceByAssetVolumeAsync(asset, amount);

			return usdValue;
		}
	}
}