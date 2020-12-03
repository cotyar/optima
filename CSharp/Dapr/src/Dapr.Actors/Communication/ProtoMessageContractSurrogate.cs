// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Google.Protobuf;

namespace Dapr.Actors.Communication
{
    using System;
    using System.Runtime.Serialization;

    internal class ProtoMessageDataContractSurrogate : ISerializationSurrogateProvider
    {
        public static readonly ISerializationSurrogateProvider Instance = new ProtoMessageDataContractSurrogate();

        public Type GetSurrogateType(Type type) => typeof(IMessage).IsAssignableFrom(type) ? typeof(ProtoMessageEnvelop) : type;

        public object GetObjectToSerialize(object obj, Type targetType) =>
            obj switch
            {
                null => null,
                IMessage _ => ProtoMessageEnvelop.Get(obj),
                _ => obj
            };

        public object GetDeserializedObject(object obj, Type targetType) =>
            obj switch
            {
                null => null,
                ProtoMessageEnvelop envelop when typeof(IMessage).IsAssignableFrom(targetType) &&
                                                !typeof(ProtoMessageEnvelop).IsAssignableFrom(targetType) => envelop.Bind(targetType),
                _ => obj
            };
    }
}
