// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using System.Text.Json;

namespace Dapr.Actors.Runtime
{
    using System;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Represents a host for an actor type within the actor runtime.
    /// </summary>
    public class ActorService : IActorService
    {
        private readonly Func<ActorService, ActorId, Actor> actorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorService"/> class.
        /// </summary>
        /// <param name="actorTypeInfo">The type information of the Actor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="actorFactory">The factory method to create Actor objects.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when actor state persistence and message deserialization</param>
        public ActorService(
            ActorTypeInformation actorTypeInfo,
            ILoggerFactory loggerFactory, Func<ActorService, ActorId, Actor> actorFactory = null, JsonSerializerOptions jsonSerializerOptions = null)
        {
            this.ActorTypeInfo = actorTypeInfo;
            this.actorFactory = actorFactory ?? this.DefaultActorFactory;
            this.StateProvider = new DaprStateProvider();
            this.LoggerFactory = loggerFactory;
            JsonSerializerOptions = jsonSerializerOptions;
        }

        /// <summary>
        /// Gets the ActorTypeInformation for actor service.
        /// </summary>
        public ActorTypeInformation ActorTypeInfo { get; }

        internal DaprStateProvider StateProvider { get; }
        internal JsonSerializerOptions JsonSerializerOptions { get; }

        /// <summary>
        /// Gets the LoggerFactory for actor service
        /// </summary>
        public ILoggerFactory LoggerFactory { get; private set; }

        internal Actor CreateActor(ActorId actorId)
        {
            return this.actorFactory.Invoke(this, actorId);
        }

        private Actor DefaultActorFactory(ActorService actorService, ActorId actorId)
        {
            return (Actor)Activator.CreateInstance(
                this.ActorTypeInfo.ImplementationType,
                actorService,
                actorId);
        }
    }
}
