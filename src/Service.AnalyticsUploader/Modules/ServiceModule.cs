using Autofac;
using MyJetWallet.Circle.Settings.Ioc;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.DataReader;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.AnalyticsUploader.Domain.NoSql;
using Service.AnalyticsUploader.Job;
using Service.AnalyticsUploader.Services;
using Service.AutoInvestManager.Domain.Models;
using Service.Bitgo.DepositDetector.Client;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.ClientProfile.Client;
using Service.HighYieldEngine.Domain.Models.Messages;
using Service.IndexPrices.Client;
using Service.InternalTransfer.Domain.Models;
using Service.KYC.Domain.Models.Messages;
using Service.Liquidity.Converter.Domain.Models;
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
			builder.RegisterMyServiceBusSubscriberBatch<SwapMessage>(tcpServiceBus, SwapMessage.TopicName, QueueName, TopicQueueType.DeleteOnDisconnect);
			builder.RegisterMyServiceBusSubscriberBatch<InvestOrder>(tcpServiceBus, InvestOrder.TopicName, QueueName, TopicQueueType.DeleteOnDisconnect);
			builder.RegisterMyServiceBusSubscriberBatch<Deposit>(tcpServiceBus, Deposit.TopicName, QueueName, TopicQueueType.DeleteOnDisconnect);
			tcpServiceBus.Start();

			IMyNoSqlSubscriber myNosqlClient = builder.CreateNoSqlClient(Program.Settings.MyNoSqlReaderHostPort, Program.LogFactory);

			builder.RegisterClientProfileClients(myNosqlClient, Program.Settings.ClientProfileGrpcServiceUrl);
			builder.RegisterDepositServiceClient(Program.Settings.BitgoDepositDetectorGrpcServiceUrl);
			builder.RegisterPersonalDataClient(Program.Settings.PersonalDataGrpcServiceUrl);
			builder.RegisterIndexPricesClient(myNosqlClient);
			builder.RegisterCircleSettingsReader(myNosqlClient);

			builder.RegisterType<AppsFlyerSender>().AsImplementedInterfaces();
			builder.RegisterType<AmplitudeSender>().AsImplementedInterfaces();

			builder.RegisterType<ClientRegisterMessageHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<EarnAnaliticsEventHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<KycProfileUpdatedMessageHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<TransferHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<WithdrawalHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<SwapMessageHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<InvestOrderHandleJob>().AutoActivate().SingleInstance();
			builder.RegisterType<DepositHandleJob>().AutoActivate().SingleInstance();
			
			builder.RegisterMyNoSqlWriter<AnalyticIdToClientNoSql>(() => Program.Settings.MyNoSqlWriterUrl, AnalyticIdToClientNoSql.TableName);
			builder
				.RegisterType<AnalyticIdToClientManager>()
				.As<IAnalyticIdToClientManager>()
				.SingleInstance();
		}
	}
}