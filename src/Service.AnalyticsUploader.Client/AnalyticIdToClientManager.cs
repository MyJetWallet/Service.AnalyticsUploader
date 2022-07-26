using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;

namespace Service.AnalyticsUploader.Client;

public class AnalyticIdToClientManager : IAnalyticIdToClientManager
{
    private readonly IMyNoSqlServerDataWriter<AnalyticIdToClientNoSql> _writer;

    public AnalyticIdToClientManager(IMyNoSqlServerDataWriter<AnalyticIdToClientNoSql> writer)
    {
        _writer = writer;
    }

    public async Task SetAppsflyerId(string clientId, string appsflyerId)
    {
        var entity = AnalyticIdToClientNoSql.CreateAppsflyerId(clientId, appsflyerId);
        await _writer.InsertOrReplaceAsync(entity);
    }
    
    public async Task<string> GetAppsflyerId(string clientId)
    {
        var entity = await _writer.GetAsync(
            AnalyticIdToClientNoSql.GeneratePartitionKey(clientId),
            AnalyticIdToClientNoSql.GenerateAppsflyerIdRowKey());

        return entity?.Id;
    }
}