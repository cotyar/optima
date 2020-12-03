// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using System.Text.Json;

namespace Dapr.Actors.Communication
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// This is the implmentation  for <see cref="IActorMessageBodySerializationProvider"/>used by remoting service and client during
    /// request/response serialization . It uses request Wrapping and data contract for serialization.
    /// </summary>
    internal class ActorMessageBodyJsonSerializationProvider : IActorMessageBodySerializationProvider
    {
        public IActorMessageBodyFactory CreateMessageBodyFactory() => new WrappedRequestMessageFactory();

        public IActorRequestMessageBodySerializer CreateRequestMessageBodySerializer(Type serviceInterfaceType,
            IEnumerable<Type> methodRequestParameterTypes, IEnumerable<Type> wrappedRequestMessageTypes = null) =>
            new MessageBodySerializer<WrappedMessageBody, WrappedMessageBody>();

        public IActorResponseMessageBodySerializer CreateResponseMessageBodySerializer(Type serviceInterfaceType,
            IEnumerable<Type> methodReturnTypes, IEnumerable<Type> wrappedResponseMessageTypes = null) =>
            new MessageBodySerializer<WrappedMessageBody, WrappedMessageBody>();
        
        private class MessageBodySerializer<TRequest, TResponse> :
            IActorRequestMessageBodySerializer,
            IActorResponseMessageBodySerializer
            where TRequest : IActorRequestMessageBody
            where TResponse : IActorResponseMessageBody
        {
            private readonly JsonSerializerOptions _jsonSerializerOptions;

            public MessageBodySerializer()
            {
                _jsonSerializerOptions = new JsonSerializerOptions
                {
                    IncludeFields = true, 
                    WriteIndented = true
                };
            }

            public byte[] Serialize(IActorRequestMessageBody actorRequestMessageBody) => 
                JsonSerializer.SerializeToUtf8Bytes(((WrappedMessage)actorRequestMessageBody).Value, 
                    _jsonSerializerOptions);
                // JsonSerializer.SerializeToUtf8Bytes(((WrappedMessage)actorRequestMessageBody), 
                //     new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });

            public byte[] Serialize(IActorResponseMessageBody actorResponseMessageBody) => 
                JsonSerializer.SerializeToUtf8Bytes(((WrappedMessage)actorResponseMessageBody).Value, 
                    _jsonSerializerOptions);
                // JsonSerializer.SerializeToUtf8Bytes(actorResponseMessageBody, 
                //     new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            
            IActorRequestMessageBody IActorRequestMessageBodySerializer.Deserialize(Stream stream)
            {
                if (stream == null || stream.Length == 0)
                {
                    return null;
                }

                stream.Position = 0; // This interface always gets a MemoryStream. This assignment should be done 1 level up really, and MemoryStream should be passed to avoid this copy or cast 
                using var memStream = new MemoryStream();
                stream.CopyTo(memStream);
                return new WrappedMessageBody {Value = JsonSerializer.Deserialize<TRequest>(memStream.ToArray(), 
                    _jsonSerializerOptions)};
                // return JsonSerializer.Deserialize<TRequest>(memStream.ToArray(), 
                //     new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            }

            IActorResponseMessageBody IActorResponseMessageBodySerializer.Deserialize(Stream stream)
            {
                if (stream == null || stream.Length == 0)
                {
                    return null;
                }

                stream.Position = 0; // This interface always gets a MemoryStream. This assignment should be done 1 level up really, and MemoryStream should be passed to avoid this copy or cast 
                using var memStream = new MemoryStream();
                stream.CopyTo(memStream);
                return JsonSerializer.Deserialize<TResponse>(memStream.ToArray(), 
                    _jsonSerializerOptions);
                // return JsonSerializer.Deserialize<TResponse>(memStream.ToArray(), 
                //     new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            }
        }
    }
}
