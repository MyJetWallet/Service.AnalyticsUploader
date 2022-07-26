namespace Service.AnalyticsUploader.Domain.NoSql;

public interface IAnalyticIdToClientManager
{
    Task SetAppsflyerId(string clientId, string appsflyerId);
    Task<string> GetAppsflyerId(string clientId);
}