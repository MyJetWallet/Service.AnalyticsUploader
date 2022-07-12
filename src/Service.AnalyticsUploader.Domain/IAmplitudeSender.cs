using System.Threading.Tasks;

namespace Service.AnalyticsUploader.Domain
{
	public interface IAmplitudeSender
	{
		Task SendMessage(IAnaliticsEvent analiticsEvent, string externalClientId, decimal? revenue = null, string revenueType = null);
	}
}