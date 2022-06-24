using System;
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
using Service.KYC.Domain.Models;
using Service.KYC.Domain.Models.Enum;
using Service.KYC.Domain.Models.Messages;
using Service.PersonalData.Domain.Models;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Contracts;
using Service.PersonalData.Grpc.Models;

namespace Service.AnalyticsUploader.Job
{
	public class KycProfileUpdatedMessageHandleJob
	{
		private readonly ILogger<KycProfileUpdatedMessageHandleJob> _logger;
		private readonly IAppsFlyerSender _sender;
		private readonly IClientProfileService _clientProfileService;
		private readonly IPersonalDataServiceGrpc _personalDataServiceGrpc;

		public KycProfileUpdatedMessageHandleJob(ILogger<KycProfileUpdatedMessageHandleJob> logger,
			ISubscriber<IReadOnlyList<KycProfileUpdatedMessage>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService, IPersonalDataServiceGrpc personalDataServiceGrpc)
		{
			_logger = logger;
			_sender = sender;
			_clientProfileService = clientProfileService;
			_personalDataServiceGrpc = personalDataServiceGrpc;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<KycProfileUpdatedMessage> messages)
		{
			List<string> clientIds = messages.Select(message => message.ClientId).ToList();

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

			foreach (KycProfileUpdatedMessage message in messages)
			{
				KycProfile newProfile = message.NewProfile;
				if (newProfile.ActiveVerificationStatus != VerificationStatus.Finished
					|| newProfile.ManualReviewStatus == ManualReviewStatus.ReviewedAndBlocked
					|| newProfile.ManualReviewStatus == ManualReviewStatus.NotReviewed)
					continue;

				string clientId = message.ClientId;
				_logger.LogInformation("Handle KycProfileUpdatedMessage message, clientId: {clientId}.", message.ClientId);

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

				PersonalDataSexEnum? sex = personalData.Sex;

				IAnaliticsEvent analiticsEvent = new SuccessfulKycPassingEvent
				{
					ResCountry = newProfile.Country,
					Age = CalculateAge(personalData.DateOfBirth),
					Sex = sex switch {PersonalDataSexEnum.Male => "male",PersonalDataSexEnum.Female => "female",_ => null}
				};

				await _sender.SendMessage(applicationId, analiticsEvent, cuid);
			}
		}

		private static int? CalculateAge(DateTime? birthDate)
		{
			if (birthDate == null)
				return null;

			DateTime now = DateTime.Today;
			int age = now.Year - birthDate.Value.Year;
			if (now < birthDate.Value.AddYears(age))
				age--;

			return age;
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
	}
}