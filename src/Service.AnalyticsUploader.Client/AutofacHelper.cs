using Autofac;
using MyJetWallet.Sdk.NoSql;
using Service.AnalyticsUploader.Domain.NoSql;

namespace Service.AnalyticsUploader.Client;

public static class AutofacHelper
{
    public static void RegisterAnalyticsUploaderClients(this ContainerBuilder builder, string myNoSqlWriterUrl)
    {
        builder.RegisterMyNoSqlWriter<AnalyticIdToClientNoSql>(() => myNoSqlWriterUrl, AnalyticIdToClientNoSql.TableName);

        builder
            .RegisterType<AnalyticIdToClientManager>()
            .As<IAnalyticIdToClientManager>()
            .SingleInstance();
    }
}