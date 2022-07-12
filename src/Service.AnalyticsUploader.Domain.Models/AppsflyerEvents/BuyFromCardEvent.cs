using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public abstract class BuyFromCardEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public abstract string EventName { get; }

		[JsonPropertyName("paidAmount")]
		public decimal PaidAmount { get; set; }

		[JsonPropertyName("paidCurrency")]
		public string PaidCurrency { get; set; }

		[JsonPropertyName("receivedAmount")]
		public decimal ReceivedAmount { get; set; }

		[JsonPropertyName("receivedCurrency")]
		public string ReceivedCurrency { get; set; }

		[JsonPropertyName("firstTimeBuy")]
		public bool FirstTimeBuy { get; set; }
	}

	public class BuyFromCardSimplexEvent : BuyFromCardEvent
	{
		[JsonIgnore]
		public override string EventName => "af_buy_from_card_simplex";
	}

	public class BuyFromCardCircleEvent : BuyFromCardEvent
	{
		[JsonIgnore]
		public override string EventName => "af_buy_from_card_circle";
	}
}