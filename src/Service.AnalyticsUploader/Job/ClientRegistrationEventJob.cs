﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Contracts;
using Service.PersonalData.Grpc.Models;
using Service.Registration.Domain.Models;

namespace Service.AnalyticsUploader.Job
{
	public class ClientRegistrationEventJob
	{
		private readonly ILogger<ClientRegistrationEventJob> _logger;
		private readonly IPersonalDataServiceGrpc _personalDataServiceGrpc;
		private readonly IAppsFlyerSender _sender;
		private readonly IClientProfileService _clientProfileService;

		public ClientRegistrationEventJob(ILogger<ClientRegistrationEventJob> logger,
			ISubscriber<IReadOnlyList<ClientRegisterMessage>> registerSubscriber,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService)
		{
			_logger = logger;
			_personalDataServiceGrpc = personalDataServiceGrpc;
			_sender = sender;
			_clientProfileService = clientProfileService;
			registerSubscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<ClientRegisterMessage> messages)
		{
			List<string> clientIds = messages.Select(message => message.TraderId).ToList();

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

			foreach (ClientRegisterMessage registerMessage in messages)
			{
				string clientId = registerMessage.TraderId;

				_logger.LogInformation("Handle ClientRegisterMessage, clientId: {clientId}, try send event...", clientId);

				string userAgent = registerMessage.UserAgent;
				string deviceId = GetDeviceId(userAgent);
				if (deviceId == null)
				{
					_logger.LogWarning("Can't detect mobile os version for UserAgent: {agent}, analitics upload skipped.", userAgent);
					continue;
				}

				PersonalDataGrpcModel personalData = personalDataItems.FirstOrDefault(model => model.Id == clientId);
				if (personalData == null)
				{
					_logger.LogError("Can't get personal data for clientId: {clientId}", clientId);
					continue;
				}

				ClientProfile.Domain.Models.ClientProfile clientProfile = await _clientProfileService.GetOrCreateProfile(new GetClientProfileRequest
				{
					ClientId = clientId
				});

				if (clientProfile == null)
				{
					_logger.LogError("Can't get client profile for clientId: {clientId}", clientId);
					continue;
				}

				string cuid = clientProfile.ExternalClientId;

				await _sender.SendMessage(Program.Settings.AppsFlyerAndroidApplicationId, new RegistrationEvent
				{
					RegCountry = personalData.CountryOfRegistration,
					UserId = cuid,
					ReferralCode = clientProfile.ReferralCode,
					DeviceId = deviceId
				}, cuid, registerMessage.IpAddress);
			}
		}

		private static string GetDeviceId(string userAgent)
		{
			if (string.IsNullOrWhiteSpace(userAgent))
				return null;

			return userAgent.Contains("iphone", StringComparison.InvariantCultureIgnoreCase)
				? Program.Settings.AppsFlyerIosApplicationId
				: userAgent.Contains("android", StringComparison.InvariantCultureIgnoreCase)
					? Program.Settings.AppsFlyerAndroidApplicationId
					: null;
		}
	}
}