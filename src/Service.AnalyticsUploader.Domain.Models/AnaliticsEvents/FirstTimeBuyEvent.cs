using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AnaliticsEvents
{
	public class FirstTimeBuyEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_first_time_buy_event";

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; }

		[JsonPropertyName("currency")]
		public string Currency { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; }
	}
}