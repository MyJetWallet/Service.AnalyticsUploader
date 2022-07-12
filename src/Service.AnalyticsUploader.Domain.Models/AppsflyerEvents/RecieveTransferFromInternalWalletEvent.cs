using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class RecieveTransferFromInternalWalletEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_recieve_transfer_from_internal_wallet";

		[JsonProperty("amount")]
		public decimal Amount { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("sender")]
		public string Sender { get; set; }

		[JsonProperty("network")]
		public string Network { get; set; }
	}
}