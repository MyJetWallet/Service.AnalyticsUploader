using SimpleTrading.UserAgent;

namespace Service.AnalyticsUploader.Services
{
	public static class ApplicationHelper
	{
		public static string GetApplicationId(string userAgent)
		{
			if (!string.IsNullOrWhiteSpace(userAgent))
			{
				string device = userAgent.GetDevice();
				bool isMobileClient = userAgent.IsMobileClient();

				if (isMobileClient && device == "Web-iOS")
					return Program.Settings.AppsFlyerIosApplicationId;

				if (isMobileClient && device == "Web-Android")
					return Program.Settings.AppsFlyerAndroidApplicationId;
			}

			return Program.Settings.AppsFlyerDefaultApplicationId;
		}
	}
}