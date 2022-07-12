using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;
using Service.AnalyticsUploader.Domain;
using System.Text.Json;

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
						EventType = analiticsEvent.EventName,
						Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
						EventProperties = analiticsEvent
					}
				},
				InsertId = Guid.NewGuid().ToString("N"),
				Revenue = revenue,
				RevenueType = revenueType
			};

			_logger.LogDebug("Send Amplitude message: {@message}, to: {to}", body, client.Options.BaseUrl);

			request.AddBody(JsonSerializer.Serialize(body));

			_logger.LogInformation("Send Amplitude \"{event}\" event with CUID: {cuid}.", analiticsEvent.EventName, externalClientId);

			RestResponse response = await client.ExecuteAsync(request);

			if (response.IsSuccessful)
				return;

			if (response.ErrorException != null)
				_logger.LogError(response.ErrorException, response.ErrorMessage, body);

			if ((response.Content ?? string.Empty).Length > 0)
				_logger.LogError("Can't send event to Amplitude, respose: {response}", response.Content);
		}

		private class PacketDto
		{
			[JsonPropertyName("api_key")]
			public string ApiKey { get; set; }

			[JsonPropertyName("insert_id")]
			public string InsertId { get; set; }

			[JsonPropertyName("revenue")]
			public decimal? Revenue { get; set; }

			[JsonPropertyName("revenueType")]
			public string RevenueType { get; set; }

			[JsonPropertyName("events")]
			public EventDto[] Events { get; set; }
		}

		private class EventDto
		{
			[JsonPropertyName("user_id")]
			public string UserId { get; set; }

			[JsonPropertyName("event_type")]
			public string EventType { get; set; }

			[JsonPropertyName("time")]
			public long Time { get; set; }

			[JsonPropertyName("event_properties")]
			public IAnaliticsEvent EventProperties { get; set; }
		}
	}
}