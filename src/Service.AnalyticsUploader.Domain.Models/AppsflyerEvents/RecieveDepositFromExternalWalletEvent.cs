using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class RecieveDepositFromExternalWalletEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_recieve_deposit_from_external_wallet";

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; }

		[JsonPropertyName("currency")]
		public string Currency { get; set; }

		[JsonPropertyName("network")]
		public string Network { get; set; }
	}
}