using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class RecieveTransferFromInternalWalletEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_recieve_transfer_from_internal_wallet";

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; }

		[JsonPropertyName("currency")]
		public string Currency { get; set; }

		[JsonPropertyName("sender")]
		public string Sender { get; set; }

		[JsonPropertyName("network")]
		public string Network { get; set; }
	}
}