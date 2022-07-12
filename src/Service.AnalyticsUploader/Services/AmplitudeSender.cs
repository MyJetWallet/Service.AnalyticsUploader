using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Service.AnalyticsUploader.Domain;

namespace Service.AnalyticsUploader.Services
{
	public class AmplitudeSender : IAmplitudeSender
	{
		private readonly ILogger<AmplitudeSender> _logger;

		public AmplitudeSender(ILogger<AmplitudeSender> logger)
		{
			_logger = logger;
		}

		public async Task SendMessage(IAnaliticsEvent analiticsEvent, string externalClientId, decimal? revenue = null, string revenueType = null)
		{
			Uri uri = new UriBuilder(Program.Settings.AmplitudeUriHost).Uri;
			var client = new RestClient(uri);
			var request = new RestRequest(Program.Settings.AmplitudeUriPath, Method.Post);

			request.AddHeader("Content-Type", "application/json");

			object body = new
			{
				api_key = Program.Settings.AmplitudeApiKey,
				events = new object[]
				{
					new
					{
						user_id = externalClientId,
						event_type = analiticsEvent.EventName,
						time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
						event_properties = JsonConvert.SerializeObject(analiticsEvent)
					}
				},
				insert_id = Guid.NewGuid().ToString("N"), 
				revenue,
				revenueType
			};

			_logger.LogDebug("Send Amplitude message: {@message}, to: {to}", body, client.Options.BaseUrl);

			request.AddBody(JsonConvert.SerializeObject(body));

			_logger.LogInformation("Send Amplitude \"{event}\" event with CUID: {cuid}.", analiticsEvent.EventName, externalClientId);

			RestResponse response = await client.ExecuteAsync(request);

			if (!response.IsSuccessful || response.ErrorException != null)
				_logger.LogError(response.ErrorException, response.ErrorMessage, body);

			else if (!(response.Content ?? string.Empty).Equals("ok", StringComparison.InvariantCultureIgnoreCase))
				_logger.LogError("Can't send event to Amplitude, respose: {response}", response.Content);
		}
	}
}