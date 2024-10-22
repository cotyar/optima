﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using System.Text.Json;

namespace Dapr.Actors.Runtime
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the Dapr runtime options
    /// </summary>
    public class ActorRuntimeOptions
    {
        // Map of ActorType --> ActorService factory.
        internal readonly Dictionary<ActorTypeInformation, Func<ActorTypeInformation, ActorService>> actorServicesFunc = new Dictionary<ActorTypeInformation, Func<ActorTypeInformation, ActorService>>();
        internal JsonSerializerOptions _serializerOptions = null;

        /// <summary>
        /// Registers an actor with the runtime.
        /// </summary>
        /// <typeparam name="TActor">Type of actor.</typeparam>
        /// <param name="actorServiceFactory">An optional delegate to create actor service. This can be used for dependency injection into actors.</param>
        public void RegisterActor<TActor>(Func<ActorTypeInformation, ActorService> actorServiceFactory = null)
            where TActor : Actor
        {
            var actorTypeInfo = ActorTypeInformation.Get(typeof(TActor));
            this.actorServicesFunc.Add(actorTypeInfo, actorServiceFactory);
        }

        /// <summary>
        /// Configure JsonSerializerOptions. NOTE: Note fully supported by Dapr .NET yet
        /// </summary>
        /// <param name="options"></param>
        public void ConfigureJsonSerializerOptions(Action<JsonSerializerOptions> options)
        {
            _serializerOptions = new JsonSerializerOptions();
            options(_serializerOptions);
        }
    }
}
