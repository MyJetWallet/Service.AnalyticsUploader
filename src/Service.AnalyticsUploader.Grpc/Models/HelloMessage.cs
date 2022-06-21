using System.Runtime.Serialization;
using Service.AnalyticsUploader.Domain.Models;

namespace Service.AnalyticsUploader.Grpc.Models
{
    [DataContract]
    public class HelloMessage : IHelloMessage
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}