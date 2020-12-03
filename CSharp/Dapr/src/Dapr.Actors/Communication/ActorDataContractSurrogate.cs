// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Google.Protobuf;

namespace Dapr.Actors.Communication
{
    using System;
    using System.Runtime.Serialization;

    internal class ActorDataContractSurrogate : ISerializationSurrogateProvider
    {
        public static readonly ISerializationSurrogateProvider Instance = new ActorDataContractSurrogate();

        public Type GetSurrogateType(Type type) =>
            typeof(IActor).IsAssignableFrom(type) 
                ? typeof(ActorReference) 
                : typeof(IMessage).IsAssignableFrom(type) 
                    ? typeof(ProtoMessageEnvelop) 
                    : type;

        public object GetObjectToSerialize(object obj, Type targetType) =>
            obj switch
            {
                null => null,
                IActor _ => ActorReference.Get(obj),
                IMessage _ => ProtoMessageEnvelop.Get(obj),
                _ => obj
            };

        public object GetDeserializedObject(object obj, Type targetType) =>
            obj switch
            {
                null => null,
                IActorReference reference when typeof(IActor).IsAssignableFrom(targetType) &&
                                               !typeof(IActorReference).IsAssignableFrom(targetType) => reference.Bind(targetType),
                ProtoMessageEnvelop envelop when typeof(IMessage).IsAssignableFrom(targetType) &&
                                                 !typeof(ProtoMessageEnvelop).IsAssignableFrom(targetType) => envelop.Bind(targetType),
                _ => obj
            };
    }
}
