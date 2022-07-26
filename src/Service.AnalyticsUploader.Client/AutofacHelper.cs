using Autofac;
using MyJetWallet.Sdk.NoSql;

namespace Service.AnalyticsUploader.Client;

public static class AutofacHelper
{
    public static void RegisterKycStatusClients(this ContainerBuilder builder, string myNoSqlWriterUrl)
    {
        builder.RegisterMyNoSqlWriter<AnalyticIdToClientNoSql>(() => myNoSqlWriterUrl, AnalyticIdToClientNoSql.TableName);

        builder
            .RegisterType<AnalyticIdToClientManager>()
            .As<IAnalyticIdToClientManager>()
            .SingleInstance();
    }
}