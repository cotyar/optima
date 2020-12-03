// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Google.Protobuf;

namespace Dapr.Actors
{
    using System;
    using System.Runtime.Serialization;
    using Dapr.Actors.Client;

    /// <summary>
    /// Encapsulation of a reference to an actor for serialization.
    /// </summary>
    [DataContract(Name = "ActorReference", Namespace = Constants.Namespace)]
    [Serializable]
    public sealed class ProtoMessageEnvelop
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActorReference"/> class.
        /// </summary>
        public ProtoMessageEnvelop()
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="Dapr.Actors.ActorId"/> of the actor.
        /// </summary>
        /// <value><see cref="Dapr.Actors.ActorId"/> of the actor.</value>
        [DataMember(Name = "MessageType", Order = 0, IsRequired = true)]
        public string MessageType { get; set; }

        /// <summary>
        /// Gets or sets the implementation type of the actor.
        /// </summary>
        /// <value>Implementation type name of the actor.</value>
        [DataMember(Name = "Message", Order = 1, IsRequired = true)]
        public string Message { get; set; }

        /// <summary>
        /// Gets <see cref="ActorReference"/> for the actor.
        /// </summary>
        /// <param name="message">Actor object to get <see cref="ActorReference"/> for.</param>
        /// <returns><see cref="ActorReference"/> object for the actor.</returns>
        /// <remarks>A null value is returned if actor is passed as null.</remarks>
        public static ProtoMessageEnvelop Get(object message) =>
            message switch
            {
                null => null,
                IMessage msg => new ProtoMessageEnvelop()
                {
                    MessageType = msg.GetType().FullName, // TODO: Change to Descriptor type 
                    Message = JsonFormatter.Default.Format(msg),
                },
                _ => throw new ArgumentOutOfRangeException(nameof(message))
            };
        
        /// <summary>
        /// Creates an <see cref="ActorProxy"/> that implements an actor interface for the actor using the
        ///     <see cref="ActorProxyFactory.CreateActorProxy(Dapr.Actors.ActorId, System.Type, string)"/>
        /// method.
        /// </summary>
        /// <param name="messageType">Actor interface for the created <see cref="ActorProxy"/> to implement.</param>
        /// <returns>An actor proxy object that implements <see cref="IActorProxy"/> and TActorInterface.</returns>
        public object Bind(Type messageType)
        {
            var method = JsonParser.Default.GetType().GetMethod("Parse", new []{ typeof(string) });
            var generic = method!.MakeGenericMethod(messageType);
            return generic.Invoke(JsonParser.Default, new object [] { Message });
        }
    }
}
