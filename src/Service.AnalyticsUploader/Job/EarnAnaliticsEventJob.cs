using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;
using Service.HighYieldEngine.Domain.Models.Constants;
using Service.HighYieldEngine.Domain.Models.Messages;
using SimpleTrading.UserAgent;

namespace Service.AnalyticsUploader.Job
{
	public class EarnAnaliticsEventJob
	{
		private readonly ILogger<EarnAnaliticsEventJob> _logger;
		private readonly IAppsFlyerSender _sender;
		private readonly IClientProfileService _clientProfileService;

		public EarnAnaliticsEventJob(ILogger<EarnAnaliticsEventJob> logger,
			ISubscriber<IReadOnlyList<EarnAnaliticsEvent>> registerSubscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService)
		{
			_logger = logger;
			_sender = sender;
			_clientProfileService = clientProfileService;
			registerSubscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<EarnAnaliticsEvent> messages)
		{
			foreach (EarnAnaliticsEvent message in messages)
			{
				string clientId = message.ClientId;
				_logger.LogInformation("Handle EarnAnaliticsEvent, clientId: {clientId}, offerId: {offerId}.", clientId, messages);

				var userAgent = "Web"; //todo
				string applicationId = GetApplicationId(userAgent);
				if (applicationId == null)
				{
					_logger.LogWarning("Can't detect mobile os version for UserAgent: {agent}, analitics upload skipped.", userAgent);
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

				IAnaliticsEvent analiticsEvent = message.ActionType == EarnAnaliticsEventType.Subscribe
					? (IAnaliticsEvent) new EarnSubscribeEvent
					{
						DeviceId = applicationId,
						UserId = cuid,
						OfferId = message.OfferId,
						Amount = message.Amount,
						Asset = message.Asset,
						Balance = message.Balance,
						IsHot = message.IsHot,
						IsTopUp = message.IsTopUp,
						Apy = message.Apy,
						CurrentApy = message.CurrentApy
					}
					: new EarnUnsubscribeEvent
					{
						DeviceId = applicationId,
						UserId = cuid,
						OfferId = message.OfferId,
						Amount = message.Amount,
						Asset = message.Asset,
						Balance = message.Balance,
						IsHot = message.IsHot,
						CurrentApy = message.CurrentApy
					};

				await _sender.SendMessage(applicationId, analiticsEvent, cuid);
			}
		}

		private static string GetApplicationId(string userAgent)
		{
			if (!string.IsNullOrWhiteSpace(userAgent))
			{
				string device = userAgent.GetDevice();
				bool isMobileClient = userAgent.IsMobileClient();

				if (isMobileClient && device == "Web-iOS")
					return Program.Settings.AppsFlyerIosApplicationId;

				if (isMobileClient && device == "Web-Android")
					return Program.Settings.AppsFlyerAndroidApplicationId;
			}

			return Program.Settings.AppsFlyerDefaultApplicationId;
		}
	}
}