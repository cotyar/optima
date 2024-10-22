// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

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
    public sealed class ActorReference : IActorReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActorReference"/> class.
        /// </summary>
        public ActorReference()
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="Dapr.Actors.ActorId"/> of the actor.
        /// </summary>
        /// <value><see cref="Dapr.Actors.ActorId"/> of the actor.</value>
        [DataMember(Name = "ActorId", Order = 0, IsRequired = true)]
        public ActorId ActorId { get; set; }

        /// <summary>
        /// Gets or sets the implementation type of the actor.
        /// </summary>
        /// <value>Implementation type name of the actor.</value>
        [DataMember(Name = "ActorType", Order = 0, IsRequired = true)]
        public string ActorType { get; set; }

        /// <summary>
        /// Gets <see cref="ActorReference"/> for the actor.
        /// </summary>
        /// <param name="actor">Actor object to get <see cref="ActorReference"/> for.</param>
        /// <returns><see cref="ActorReference"/> object for the actor.</returns>
        /// <remarks>A null value is returned if actor is passed as null.</remarks>
        public static ActorReference Get(object actor)
        {
            if (actor != null)
            {
                return GetActorReference(actor);
            }

            return null;
        }

        /// <summary>
        /// Creates an <see cref="ActorProxy"/> that implements an actor interface for the actor using the
        ///     <see cref="ActorProxyFactory.CreateActorProxy(Dapr.Actors.ActorId, System.Type, string)"/>
        /// method.
        /// </summary>
        /// <param name="actorInterfaceType">Actor interface for the created <see cref="ActorProxy"/> to implement.</param>
        /// <returns>An actor proxy object that implements <see cref="IActorProxy"/> and TActorInterface.</returns>
        public object Bind(Type actorInterfaceType)
        {
            return ActorProxy.ActorProxyFactory.CreateActorProxy(this.ActorId, actorInterfaceType, this.ActorType);
        }

        private static ActorReference GetActorReference(object actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException("actor");
            }

            // try as IActorProxy for backward compatibility as customers's mock framework may rely on it before V2 remoting stack.
            if (actor is IActorProxy actorProxy)
            {
                return new ActorReference()
                {
                    ActorId = actorProxy.ActorId,
                    ActorType = actorProxy.ActorType,
                };
            }

            // TODO check for ActorBase
            throw new ArgumentOutOfRangeException("actor");
        }
    }
}
