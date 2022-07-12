using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class RegistrationEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_complete_registration";

		[JsonPropertyName("deviceId")]
		public string DeviceId { get; set; }

		[JsonPropertyName("userId")]
		public string UserId { get; set; }

		[JsonPropertyName("regCountry")]
		public string RegCountry { get; set; }

		[JsonPropertyName("referralCode")]
		public string ReferralCode { get; set; }
	}
}