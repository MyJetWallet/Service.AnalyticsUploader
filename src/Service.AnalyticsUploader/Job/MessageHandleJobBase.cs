using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.ServiceBus.SessionAudit.Models;
using Service.AnalyticsUploader.Domain;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Contracts;
using Service.PersonalData.Grpc.Models;

namespace Service.AnalyticsUploader.Job
{
	public abstract class MessageHandleJobBase
	{
		protected const string UsdAsset = "USD";

		private readonly ILogger _logger;
		private readonly IPersonalDataServiceGrpc _personalDataServiceGrpc;
		private readonly IClientProfileService _clientProfileService;
		private readonly IAppsFlyerSender _sender;

		protected MessageHandleJobBase(ILogger logger, 
			IPersonalDataServiceGrpc personalDataServiceGrpc, 
			IClientProfileService clientProfileService, 
			IAppsFlyerSender sender)
		{
			_logger = logger;
			_personalDataServiceGrpc = personalDataServiceGrpc;
			_clientProfileService = clientProfileService;
			_sender = sender;
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

		protected async Task SendMessage(string clientId, IAnaliticsEvent analiticsEvent, string ipAddress = null)
		{
			ClientProfile.Domain.Models.ClientProfile clientProfile = await GetClientProfile(clientId);
			if (clientProfile == null)
				return;

			string applicationId = GetApplicationId(clientProfile.DeviceOperationSystem);
			if (applicationId == null)
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

			await _sender.SendMessage(applicationId, analiticsEvent, cuid, ipAddress);
		}
	}
}