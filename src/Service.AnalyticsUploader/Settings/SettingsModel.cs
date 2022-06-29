using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.AnalyticsUploader.Settings
{
	public class SettingsModel
	{
		[YamlProperty("AnalyticsUploader.SeqServiceUrl")]
		public string SeqServiceUrl { get; set; }

		[YamlProperty("AnalyticsUploader.ZipkinUrl")]
		public string ZipkinUrl { get; set; }

		[YamlProperty("AnalyticsUploader.ElkLogs")]
		public LogElkSettings ElkLogs { get; set; }

		[YamlProperty("AnalyticsUploader.SpotServiceBusHostPort")]
		public string SpotServiceBusHostPort { get; set; }

		[YamlProperty("AnalyticsUploader.MyNoSqlReaderHostPort")]
		public string MyNoSqlReaderHostPort { get; set; }

		[YamlProperty("AnalyticsUploader.PersonalDataGrpcServiceUrl")]
		public string PersonalDataGrpcServiceUrl { get; set; }

		[YamlProperty("AnalyticsUploader.ClientProfileGrpcServiceUrl")]
		public string ClientProfileGrpcServiceUrl { get; set; }

		[YamlProperty("AnalyticsUploader.BitgoDepositDetectorGrpcServiceUrl")]
		public string BitgoDepositDetectorGrpcServiceUrl  { get; set; }

		[YamlProperty("AnalyticsUploader.AppsFlyerUriHost")]
		public string AppsFlyerUriHost { get; set; }

		[YamlProperty("AnalyticsUploader.AppsFlyerUriPath")]
		public string AppsFlyerUriPath { get; set; }

		[YamlProperty("AnalyticsUploader.AppsFlyerAndroidApplicationId")]
		public string AppsFlyerAndroidApplicationId { get; set; }

		[YamlProperty("AnalyticsUploader.AppsFlyerDefaultApplicationId")]
		public string AppsFlyerDefaultApplicationId { get; set; }

		[YamlProperty("AnalyticsUploader.AppsFlyerIosApplicationId")]
		public string AppsFlyerIosApplicationId { get; set; }

		[YamlProperty("AnalyticsUploader.AppsFlyerDevKey")]
		public string AppsFlyerDevKey { get; set; }
	}
}