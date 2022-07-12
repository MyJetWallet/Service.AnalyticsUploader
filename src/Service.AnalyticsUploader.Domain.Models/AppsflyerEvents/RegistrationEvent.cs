using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class RegistrationEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_complete_registration";

		[JsonProperty("deviceId")]
		public string DeviceId { get; set; }

		[JsonProperty("userId")]
		public string UserId { get; set; }

		[JsonProperty("regCountry")]
		public string RegCountry { get; set; }

		[JsonProperty("referralCode")]
		public string ReferralCode { get; set; }
	}
}