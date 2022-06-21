using Autofac;
using Service.AnalyticsUploader.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.AnalyticsUploader.Client
{
    public static class AutofacHelper
    {
        public static void RegisterAnalyticsUploaderClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new AnalyticsUploaderClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IHelloService>().SingleInstance();
        }
    }
}
