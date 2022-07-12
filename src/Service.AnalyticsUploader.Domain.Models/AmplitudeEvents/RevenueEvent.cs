using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AmplitudeEvents
{
	public class RevenueEvent : IAnaliticsEvent
	{
		public string GetEventName() => "User brought revenue";

		[JsonProperty("Revenue Asset")]
		public string RevenueAsset { get; set; }

		[JsonProperty("Revenue Volume In Asset")]
		public decimal RevenueVolumeInAsset { get; set; }

		[JsonProperty("Revenue Volume In Usd")]
		public decimal RevenueVolumeInUsd { get; set; }

		[JsonProperty("Revenue Type")]
		public string RevenueType { get; set; }
	}
}