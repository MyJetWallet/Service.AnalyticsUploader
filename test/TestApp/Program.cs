using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace TestApp
{
	internal class Program
	{
		private const string AppId = "app.simple.com";

		private static async Task Main(string[] args)
		{
			Console.Write("Press enter to start");
			Console.ReadLine();

			var client = new RestClient("https://api2.appsflyer.com");
			var request = new RestRequest($"/inappevent/{AppId}", Method.Post);
			request.AddHeader("authentication", "hnJtbnSpC85TaCQKZMCKR8");
			request.AddHeader("Content-Type", "application/json");

			var payload = new
			{
				deviceId = Guid.NewGuid().ToString("N"),
				userId = Guid.NewGuid().ToString("N"),
				regCountry = "EU",
				referralCode = "1"
			};

			var body = new
			{
				appsflyer_id = Guid.NewGuid().ToString("N"),
				customer_user_id = Guid.NewGuid().ToString("N"),
				eventName = "af_complete_registration",
				ip = "192.0.2.1",
				eventTime = DateTime.UtcNow,
				bundleIdentifier = AppId,
				eventValue = JsonConvert.SerializeObject(payload)
			};

			string value = JsonConvert.SerializeObject(body);
			Console.WriteLine(value);

			request.AddBody(value);
			RestResponse response = await client.ExecuteAsync(request);

			Console.WriteLine(response.Content);
			Console.WriteLine(response.StatusCode);
			Console.ReadLine();
		}
	}
}