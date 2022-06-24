using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AnaliticsEvents
{
	public class SuccessfulKycPassingEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_successful_kyc_passing";

		[JsonPropertyName("res_country")]
		public string ResCountry { get; set; }

		[JsonPropertyName("age")]
		public int? Age { get; set; }

		[JsonPropertyName("gender")]
		public string Sex { get; set; }
	}
}