

using MyNoSqlServer.Abstractions;

public class AnalyticIdToClientNoSql: MyNoSqlDbEntity
{
    public const string TableName = "jetwallet-analytics-id-to-client-id";
    
    public static string GeneratePartitionKey(string clientId) => clientId;
    public static string GenerateAppsflyerIdRowKey() => "appsflyer-id";
    
    public string Id { get; set; }
    public string ClientId { get; set; }

    public static AnalyticIdToClientNoSql CreateAppsflyerId(string clientId, string appsflyerId)
    {
        return new AnalyticIdToClientNoSql()
        {
            PartitionKey = GeneratePartitionKey(clientId),
            RowKey = GenerateAppsflyerIdRowKey(),
            Id = appsflyerId,
            ClientId = clientId
        };
    }
    
}