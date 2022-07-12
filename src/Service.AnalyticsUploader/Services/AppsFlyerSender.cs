using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Service.AnalyticsUploader.Domain;

namespace Service.AnalyticsUploader.Services
{
	public class AppsFlyerSender : IAppsFlyerSender
	{
		private readonly ILogger<AppsFlyerSender> _logger;

		public AppsFlyerSender(ILogger<AppsFlyerSender> logger)
		{
			_logger = logger;
		}

		public async Task SendMessage(string applicationId, IAnaliticsEvent analiticsEvent, string externalClientId = null, string ipAddress = null)
		{
			Uri uri = new UriBuilder(Program.Settings.AppsFlyerUriHost).Uri;
			var client = new RestClient(uri);
			var request = new RestRequest($"{Program.Settings.AppsFlyerUriPath}{applicationId}", Method.Post);

			request.AddHeader("authentication", Program.Settings.AppsFlyerDevKey);
			request.AddHeader("Content-Type", "application/json");

			object body = new
			{
				appsflyer_id = Guid.NewGuid().ToString("N"),
				customer_user_id = externalClientId,
				eventName = analiticsEvent.GetEventName(),
				ip = ipAddress,
				eventTime = DateTime.UtcNow,
				bundleIdentifier = applicationId,
				eventValue = JsonConvert.SerializeObject(analiticsEvent)
			};

			string bodyStr = JsonConvert.SerializeObject(body);
			_logger.LogDebug("Send AppsFlyer message: {@message}, to: {to}", bodyStr, client.Options.BaseUrl);
			request.AddBody(bodyStr);

			_logger.LogInformation("Send AppsFlyer \"{event}\" event with CUID: {cuid} to app \"{app}\".", analiticsEvent.GetEventName(), externalClientId, applicationId);
			RestResponse response = await client.ExecuteAsync(request);

			if (response.IsSuccessful)
				return;

			if (!response.IsSuccessful || response.ErrorException != null)
				_logger.LogError(response.ErrorException, response.ErrorMessage, body);

			if ((response.Content ?? string.Empty).Length > 0)
				_logger.LogError("Can't send event to AppsFlyer, respose: {response}", response.Content);
		}
	}
}