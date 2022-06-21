using Autofac;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.DataReader;
using MyServiceBus.TcpClient;
using Service.AnalyticsUploader.Job;
using Service.AnalyticsUploader.Services;
using Service.ClientProfile.Client;
using Service.PersonalData.Client;
using Service.Registration.Client;

namespace Service.AnalyticsUploader.Modules
{
	public class ServiceModule : Module
	{
		private const string QueueName = "AnalyticsUploader";

		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<ClientRegistrationEventJob>().AutoActivate().SingleInstance();
			builder.RegisterType<AppsFlyerSender>().AsImplementedInterfaces();

			MyServiceBusTcpClient tcpServiceBus = builder.RegisterMyServiceBusTcpClient(() => Program.Settings.SpotServiceBusHostPort, Program.LogFactory);
			builder.RegisterClientRegisteredSubscriber(tcpServiceBus, QueueName);
			tcpServiceBus.Start();

			IMyNoSqlSubscriber myNosqlClient = builder.CreateNoSqlClient(Program.Settings.MyNoSqlReaderHostPort, Program.LogFactory);

			builder.RegisterPersonalDataClient(Program.Settings.PersonalDataGrpcServiceUrl);
			builder.RegisterClientProfileClients(myNosqlClient, Program.Settings.ClientProfileGrpcServiceUrl);
		}
	}
}