using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class SendTransferByWalletExternalEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_send_transfer_by_wallet_external";

		[JsonProperty("amount")]
		public decimal Amount { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("receiver")]
		public string Receiver { get; set; }

		[JsonProperty("network")]
		public string Network { get; set; }
	}
}