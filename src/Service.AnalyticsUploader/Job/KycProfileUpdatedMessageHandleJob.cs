using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.ClientProfile.Grpc;
using Service.KYC.Domain.Models;
using Service.KYC.Domain.Models.Enum;
using Service.KYC.Domain.Models.Messages;
using Service.PersonalData.Domain.Models;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Models;

namespace Service.AnalyticsUploader.Job
{
	public class KycProfileUpdatedMessageHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<KycProfileUpdatedMessageHandleJob> _logger;

		public KycProfileUpdatedMessageHandleJob(ILogger<KycProfileUpdatedMessageHandleJob> logger,
			ISubscriber<IReadOnlyList<KycProfileUpdatedMessage>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService, 
			IPersonalDataServiceGrpc personalDataServiceGrpc) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<KycProfileUpdatedMessage> messages)
		{
			KycProfileUpdatedMessage[] allowedMessages = messages.Where(KycPassed).ToArray();
			if (!allowedMessages.Any())
				return;

			List<string> clientIds = allowedMessages.Select(message => message.ClientId).ToList();
			
			PersonalDataGrpcModel[] personalDataItems = await GetPersonalData(clientIds);

			foreach (KycProfileUpdatedMessage message in allowedMessages)
			{
				string clientId = message.ClientId;

				_logger.LogInformation("Handle KycProfileUpdatedMessage message, clientId: {clientId}.", clientId);

				PersonalDataGrpcModel personalData = personalDataItems.FirstOrDefault(model => model.Id == clientId);
				if (personalData == null)
				{
					_logger.LogError("Can't get personal data for clientId: {clientId}", clientId);
					continue;
				}

				IAnaliticsEvent analiticsEvent = new SuccessfulKycPassingEvent
				{
					ResCountry = message.NewProfile.Country,
					Age = CalculateAge(personalData.DateOfBirth),
					Sex = personalData.Sex switch {PersonalDataSexEnum.Male => "male",PersonalDataSexEnum.Female => "female",_ => null}
				};

				await SendMessage(clientId, analiticsEvent);
			}
		}

		private static bool KycPassed(KycProfileUpdatedMessage message)
		{
			return message.OldProfile.DepositStatus != KycOperationStatus.Allowed &&
					message.NewProfile.DepositStatus == KycOperationStatus.Allowed ||
					message.OldProfile.WithdrawalStatus != KycOperationStatus.Allowed &&
					message.NewProfile.WithdrawalStatus == KycOperationStatus.Allowed ||
					message.OldProfile.TradeStatus != KycOperationStatus.Allowed &&
					message.NewProfile.TradeStatus == KycOperationStatus.Allowed;
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
	}
}