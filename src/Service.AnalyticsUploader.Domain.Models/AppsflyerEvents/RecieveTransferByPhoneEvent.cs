using Newtonsoft.Json;

namespace Service.AnalyticsUploader.Domain.Models.AppsflyerEvents
{
	public class RecieveTransferByPhoneEvent : IAnaliticsEvent
	{
		public string GetEventName() => "af_recieve_transfer_by_phone";

		[JsonProperty("amount")]
		public decimal Amount { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("sender")]
		public string Sender { get; set; }
	}
}