using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class FirstTimeBuyEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_first_time_buy_event";

		[JsonProperty("amount")]
		public decimal Amount { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("method")]
		public string Method { get; set; }
	}
}