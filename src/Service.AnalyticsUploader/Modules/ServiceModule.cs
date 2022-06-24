using Autofac;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.DataReader;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.AnalyticsUploader.Job;
using Service.AnalyticsUploader.Services;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.ClientProfile.Client;
using Service.HighYieldEngine.Domain.Models.Messages;
using Service.InternalTransfer.Domain.Models;
using Service.KYC.Domain.Models.Messages;
using Service.PersonalData.Client;
using Service.Registration.Client;

namespace Service.AnalyticsUploader.Modules
{
	public class ServiceModule : Module
	{
		private const string QueueName = "AnalyticsUploader";

		protected override void Load(ContainerBuilder builder)
		{
			MyServiceBusTcpClient tcpServiceBus = builder.RegisterMyServiceBusTcpClient(() => Program.Settings.SpotServiceBusHostPort, Program.LogFactory);
			builder.RegisterClientRegisteredSubscriber(tcpServiceBus, QueueName);
			builder.RegisterMyServiceBusSubscriberBatch<Withdrawal>(tcpServiceBus, Withdrawal.TopicName, QueueName, TopicQueueType.DeleteOnDisconnect);
			builder.RegisterMyServiceBusSubscriberBatch<EarnAnaliticsEvent>(tcpServiceBus, EarnAnaliticsEvent.TopicName, QueueName, TopicQueueType.DeleteOnDisconnect);
			builder.RegisterMyServiceBusSubscriberBatch<Transfer>(tcpServiceBus, Transfer.TopicName, QueueName, TopicQueueType.DeleteOnDisconnect);
			builder.RegisterMyServiceBusSubscriberBatch<KycProfileUpdatedMessage>(tcpServiceBus, KycProfileUpdatedMessage.TopicName, QueueName, TopicQueueType.DeleteOnDisconnect);
			tcpServiceBus.Start();

			builder.RegisterPersonalDataClient(Program.Settings.PersonalDataGrpcServiceUrl);

			IMyNoSqlSubscriber myNosqlClient = builder.CreateNoSqlClient(Program.Settings.MyNoSqlReaderHostPort, Program.LogFactory);
			builder.RegisterClientProfileClients(myNosqlClient, Program.Settings.ClientProfileGrpcServiceUrl);

			builder.RegisterType<AppsFlyerSender>().AsImplementedInterfaces();

			builder.RegisterType<ClientRegisterMessageHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<EarnAnaliticsEventHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<KycProfileUpdatedMessageHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<TransferHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<WithdrawalHandleJob>().AutoActivate().SingleInstance();
		}
	}
}