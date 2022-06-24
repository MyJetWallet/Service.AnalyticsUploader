using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Contracts;
using Service.PersonalData.Grpc.Models;
using SimpleTrading.UserAgent;

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

		protected string GetApplicationId(string userAgent = null)
		{
			string appId = null;

			if (!string.IsNullOrWhiteSpace(userAgent))
			{
				string device = userAgent.GetDevice();
				bool isMobileClient = userAgent.IsMobileClient();

				if (isMobileClient && device == "Web-iOS")
					appId = Program.Settings.AppsFlyerIosApplicationId;

				else if (isMobileClient && device == "Web-Android")
					appId = Program.Settings.AppsFlyerAndroidApplicationId;
			}

			appId ??= Program.Settings.AppsFlyerDefaultApplicationId;

			if (appId == null)
				_logger.LogWarning("Can't detect mobile os version for UserAgent: {agent}, analitics upload skipped.", userAgent);

			return appId;
		}

		protected async Task SendMessage(string clientId, IAnaliticsEvent analiticsEvent, string userAgent = null, string ipAddress = null, string cuid = null)
		{
			string applicationId = GetApplicationId(userAgent);

			cuid ??= await GetExternalClientId(clientId);

			if (applicationId == null || cuid == null)
				return;

			await _sender.SendMessage(applicationId, analiticsEvent, cuid);
		}
	}
}