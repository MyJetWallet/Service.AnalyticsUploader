using System.Threading.Tasks;

namespace Service.AnalyticsUploader.Client;

public interface IAnalyticIdToClientManager
{
    Task SetAppsflyerId(string clientId, string appsflyerId);
    Task<string> GetAppsflyerId(string clientId);
}