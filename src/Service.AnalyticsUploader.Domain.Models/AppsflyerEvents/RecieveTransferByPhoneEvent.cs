using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class RecieveTransferByPhoneEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_recieve_transfer_by_phone";

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; }

		[JsonPropertyName("currency")]
		public string Currency { get; set; }

		[JsonPropertyName("sender")]
		public string Sender { get; set; }
	}
}