using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public abstract class BuyFromCardEvent : IAnaliticsEvent
	{
		public abstract string GetEventName();

		[JsonProperty("paidAmount")]
		public decimal PaidAmount { get; set; }

		[JsonProperty("paidCurrency")]
		public string PaidCurrency { get; set; }

		[JsonProperty("receivedAmount")]
		public decimal ReceivedAmount { get; set; }

		[JsonProperty("receivedCurrency")]
		public string ReceivedCurrency { get; set; }

		[JsonProperty("firstTimeBuy")]
		public bool FirstTimeBuy { get; set; }
	}

	public class BuyFromCardSimplexEvent : BuyFromCardEvent
	{
		public override string GetEventName() => "af_buy_from_card_simplex";
	}

	public class BuyFromCardCircleEvent : BuyFromCardEvent
	{
		public override string GetEventName() => "af_buy_from_card_circle";
	}
}