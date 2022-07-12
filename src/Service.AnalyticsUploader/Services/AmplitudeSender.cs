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

			object body = new PacketDto
			{
				ApiKey = Program.Settings.AmplitudeApiKey,
				Events = new[]
				{
					new EventDto
					{
						UserId = externalClientId,
						EventType = analiticsEvent.GetEventName(),
						Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
						EventProperties = analiticsEvent
					}
				},
				InsertId = Guid.NewGuid().ToString("N"),
				Revenue = revenue,
				RevenueType = revenueType
			};

			string bodyStr = JsonConvert.SerializeObject(body);
			_logger.LogDebug("Send Amplitude message: {@message}, to: {to}", bodyStr, client.Options.BaseUrl);
			request.AddBody(bodyStr);

			_logger.LogInformation("Send Amplitude \"{event}\" event with CUID: {cuid}.", analiticsEvent.GetEventName(), externalClientId);
			RestResponse response = await client.ExecuteAsync(request);

			if (response.IsSuccessful)
				return;

			if (response.ErrorException != null)
				_logger.LogError(response.ErrorException, response.ErrorMessage, bodyStr);

			if ((response.Content ?? string.Empty).Length > 0)
				_logger.LogError("Can't send event to Amplitude, respose: {response}", response.Content);
		}

		private class PacketDto
		{
			[JsonProperty("api_key")]
			public string ApiKey { get; set; }

			[JsonProperty("insert_id")]
			public string InsertId { get; set; }

			[JsonProperty("revenue")]
			public decimal? Revenue { get; set; }

			[JsonProperty("revenueType")]
			public string RevenueType { get; set; }

			[JsonProperty("events")]
			public EventDto[] Events { get; set; }
		}

		private class EventDto
		{
			[JsonProperty("user_id")]
			public string UserId { get; set; }

			[JsonProperty("event_type")]
			public string EventType { get; set; }

			[JsonProperty("time")]
			public long Time { get; set; }

			[JsonProperty("event_properties")]
			public IAnaliticsEvent EventProperties { get; set; }
		}
	}
}