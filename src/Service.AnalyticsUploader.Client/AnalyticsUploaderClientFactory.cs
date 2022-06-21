using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.AnalyticsUploader.Grpc;

namespace Service.AnalyticsUploader.Client
{
    [UsedImplicitly]
    public class AnalyticsUploaderClientFactory: MyGrpcClientFactory
    {
        public AnalyticsUploaderClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IHelloService GetHelloService() => CreateGrpcService<IHelloService>();
    }
}
