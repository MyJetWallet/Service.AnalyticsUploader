using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AnaliticsEvents
{
	public class EarnUnsubscribeEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_earn_unsubscribe";

		[JsonPropertyName("isHot")]
		public bool IsHot { get; set; }

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; }

		[JsonPropertyName("offerId")]
		public string OfferId { get; set; }

		[JsonPropertyName("currency")]
		public string Asset { get; set; }

		[JsonPropertyName("apyPerOffer")]
		public decimal CurrentApy { get; set; }

		[JsonPropertyName("offerBalance")]
		public decimal Balance { get; set; }
	}
}