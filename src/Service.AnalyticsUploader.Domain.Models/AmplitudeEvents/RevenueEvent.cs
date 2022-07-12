using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AmplitudeEvents
{
	public class RevenueEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "User brought revenue";

		[JsonPropertyName("Revenue Asset")]
		public string RevenueAsset { get; set; }

		[JsonPropertyName("Revenue Volume In Asset")]
		public decimal RevenueVolumeInAsset { get; set; }

		[JsonPropertyName("Revenue Volume In Usd")]
		public decimal RevenueVolumeInUsd { get; set; }

		[JsonPropertyName("Revenue Type")]
		public string RevenueType { get; set; }
	}
}