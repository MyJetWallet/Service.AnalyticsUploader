using System.ServiceModel;
using System.Threading.Tasks;
using Service.AnalyticsUploader.Grpc.Models;

namespace Service.AnalyticsUploader.Grpc
{
    [ServiceContract]
    public interface IHelloService
    {
        [OperationContract]
        Task<HelloMessage> SayHelloAsync(HelloRequest request);
    }
}