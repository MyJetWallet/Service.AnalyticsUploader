using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AnaliticsEvents
{
	public class ExchangingAssetEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_exchanging_asset";

		[JsonPropertyName("frequency")]
		public string Frequency { get; set; }

		[JsonPropertyName("autoTrade")]
		public bool AutoTrade { get; set; }

		[JsonPropertyName("quoteId")]
		public string QuoteId { get; set; }

		[JsonPropertyName("recurringOrderId")]
		public string RecurringOrderId { get; set; }

		[JsonPropertyName("sourceCurrency")]
		public string SourceCurrency { get; set; }

		[JsonPropertyName("destinationCurrency")]
		public string DestinationCurrency { get; set; }

		[JsonPropertyName("amountUsd")]
		public decimal AmountUsd { get; set; }

		[JsonPropertyName("tradeFee")]
		public decimal TradeFee { get; set; }
	}
}