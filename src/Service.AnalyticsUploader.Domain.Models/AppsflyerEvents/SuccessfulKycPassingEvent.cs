using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class SuccessfulKycPassingEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_successful_kyc_passing";

		[JsonProperty("res_country")]
		public string ResCountry { get; set; }

		[JsonProperty("age")]
		public int? Age { get; set; }

		[JsonProperty("gender")]
		public string Sex { get; set; }
	}
}