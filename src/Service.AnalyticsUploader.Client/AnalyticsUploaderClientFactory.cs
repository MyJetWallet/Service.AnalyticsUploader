using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;

namespace Service.AnalyticsUploader.Client
{
    [UsedImplicitly]
    public class AnalyticsUploaderClientFactory: MyGrpcClientFactory
    {
        public AnalyticsUploaderClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }
    }
}
