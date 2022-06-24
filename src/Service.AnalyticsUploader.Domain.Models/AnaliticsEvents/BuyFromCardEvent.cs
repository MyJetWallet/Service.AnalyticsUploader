using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AnaliticsEvents
{
	public abstract class BuyFromCardEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public abstract string EventName { get; }

		[JsonPropertyName("paidAmount")]
		public string PaidAmount { get; set; }

		[JsonPropertyName("paidCurrency")]
		public string PaidCurrency { get; set; }

		[JsonPropertyName("receivedAmount")]
		public string ReceivedAmount { get; set; }

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