using System;
using System.Net;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using grpc = Grpc.Core;

namespace DatasetProbeServer
{
    public class GrpcTransport<TRequest, TResponse>
        where TRequest : class, IMessage, new()
        where TResponse : class, IMessage, new()
    {
        static T ParseFrom<T>(byte[] bytes) where T: IMessage, new()
        {
            var msg = new T();
            msg.MergeFrom(bytes);
            return msg;
        }
        
        public readonly string ServiceName;

        public static readonly Marshaller<TRequest> RequestMarshaller = 
            grpc::Marshallers.Create((arg) => arg.ToByteArray(), ParseFrom<TRequest>);
        public static readonly Marshaller<TResponse> ResponseMarshaller = 
            grpc::Marshallers.Create((arg) => arg.ToByteArray(), ParseFrom<TResponse>);

        /// <summary>Service descriptor</summary>
        // public static ServiceDescriptor Descriptor { get; }

        public GrpcTransport(string serviceName, /*ServiceDescriptor descriptor, */ Func<TRequest, IServerStreamWriter<TResponse>, ServerCallContext, Task> sourceMethodHandler)
        {
            ServiceName = serviceName;
            var methodData = new Method<TRequest, TResponse>(
                grpc::MethodType.ServerStreaming,
                ServiceName,
                "Data",
                RequestMarshaller,
                ResponseMarshaller);

            ServerServiceDefinition = ServerServiceDefinition.CreateBuilder()
                .AddMethod(methodData, new ServerStreamingServerMethod<TRequest, TResponse>(sourceMethodHandler))
                .Build();
        }

        public readonly ServerServiceDefinition ServerServiceDefinition;
    }
}