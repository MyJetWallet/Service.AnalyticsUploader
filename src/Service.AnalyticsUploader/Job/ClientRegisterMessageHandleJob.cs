using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AppsflyerEvents;
using Service.AnalyticsUploader.Domain.NoSql;
using Service.ClientProfile.Grpc;
using Service.IndexPrices.Client;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Models;
using Service.Registration.Domain.Models;

namespace Service.AnalyticsUploader.Job
{
	public class ClientRegisterMessageHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<ClientRegisterMessageHandleJob> _logger;

		public ClientRegisterMessageHandleJob(ILogger<ClientRegisterMessageHandleJob> logger,
			ISubscriber<IReadOnlyList<ClientRegisterMessage>> registerSubscriber,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IAppsFlyerSender appsFlyerSender,
			IClientProfileService clientProfileService,
			IIndexPricesClient converter,
			IAmplitudeSender amplitudeSender,
			IAnalyticIdToClientManager analyticIdToClientManager) :
				base(logger, personalDataServiceGrpc, clientProfileService, appsFlyerSender, amplitudeSender, converter, analyticIdToClientManager)
		{
			_logger = logger;
			registerSubscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<ClientRegisterMessage> messages)
		{
			List<string> clientIds = messages.Select(message => message.TraderId).ToList();

			PersonalDataGrpcModel[] personalDataItems = await GetPersonalData(clientIds);

			foreach (ClientRegisterMessage message in messages)
			{
				string clientId = message.TraderId;

				_logger.LogInformation("Handle ClientRegisterMessage message, clientId: {clientId}.", clientId);

				PersonalDataGrpcModel personalData = personalDataItems.FirstOrDefault(model => model.Id == clientId);
				if (personalData == null)
				{
					_logger.LogError("Can't get personal data for clientId: {clientId}", clientId);
					continue;
				}

				ClientProfile.Domain.Models.ClientProfile clientProfile = await GetClientProfile(clientId);
				if (clientProfile == null)
					continue;

				string cuid = clientProfile.ExternalClientId;

				await SendAppsflyerMessage(clientId, new RegistrationEvent
				{
					RegCountry = personalData.CountryOfRegistration,
					UserId = cuid,
					ReferralCode = clientProfile.ReferralCode,
					DeviceId = GetApplicationId(clientProfile.DeviceOperationSystem)
				}, message.IpAddress);
			}
		}
	}
}