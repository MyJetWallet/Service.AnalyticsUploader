using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class RecieveDepositFromExternalWalletEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_recieve_deposit_from_external_wallet";

		[JsonProperty("amount")]
		public decimal Amount { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("network")]
		public string Network { get; set; }
	}
}