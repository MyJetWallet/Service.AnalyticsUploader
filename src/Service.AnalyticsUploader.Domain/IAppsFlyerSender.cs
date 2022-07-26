using System.Threading.Tasks;

namespace Service.AnalyticsUploader.Domain
{
	public interface IAppsFlyerSender
	{
		Task SendMessage(string appsflyerId, string applicationId, IAnaliticsEvent analiticsEvent, string externalClientId = null, string ipAddress = null);
	}
}