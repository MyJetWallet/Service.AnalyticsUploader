using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class SendTransferByWalletExternalEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_send_transfer_by_wallet_external";

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; }

		[JsonPropertyName("currency")]
		public string Currency { get; set; }

		[JsonPropertyName("receiver")]
		public string Receiver { get; set; }

		[JsonPropertyName("network")]
		public string Network { get; set; }
	}
}