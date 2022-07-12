using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class ExchangingAssetEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_exchanging_asset";

		[JsonProperty("frequency")]
		public string Frequency { get; set; }

		[JsonProperty("autoTrade")]
		public bool AutoTrade { get; set; }

		[JsonProperty("quoteId")]
		public string QuoteId { get; set; }

		[JsonProperty("recurringOrderId")]
		public string RecurringOrderId { get; set; }

		[JsonProperty("sourceCurrency")]
		public string SourceCurrency { get; set; }

		[JsonProperty("destinationCurrency")]
		public string DestinationCurrency { get; set; }

		[JsonProperty("amountUsd")]
		public decimal AmountUsd { get; set; }

		[JsonProperty("tradeFee")]
		public decimal TradeFee { get; set; }
	}
}