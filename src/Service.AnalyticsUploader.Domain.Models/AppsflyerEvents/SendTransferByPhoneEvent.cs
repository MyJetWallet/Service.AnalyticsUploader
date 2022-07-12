using System.Text.Json.Serialization;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class SendTransferByPhoneEvent : IAnaliticsEvent
	{
		[JsonIgnore]
		public string EventName => "af_send_transfer_by_phone";

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; }

		[JsonPropertyName("currency")]
		public string Currency { get; set; }

		[JsonPropertyName("receiver")]
		public string Receiver { get; set; }
	}
}