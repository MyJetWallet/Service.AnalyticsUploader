using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class EarnUnsubscribeEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_earn_unsubscribe";

		[JsonProperty("isHot")]
		public bool IsHot { get; set; }

		[JsonProperty("amount")]
		public decimal Amount { get; set; }

		[JsonProperty("offerId")]
		public string OfferId { get; set; }

		[JsonProperty("currency")]
		public string Asset { get; set; }

		[JsonProperty("apyPerOffer")]
		public decimal CurrentApy { get; set; }

		[JsonProperty("offerBalance")]
		public decimal Balance { get; set; }
	}
}