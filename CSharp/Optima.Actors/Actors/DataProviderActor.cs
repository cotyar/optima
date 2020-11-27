using System;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Optima.Domain.Core;
using Optima.Domain.DatasetDefinition;
// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.DatasetEntry)]
    public class DataProviderActor: Actor, IDataProvider
    {
        private string _generatedCodeFolder;
        private uint _port;
        private DatasetInfo _datasetInfo;

        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public DataProviderActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        private DatasetId DatasetId => new DatasetId { Uid = Id.GetId() }; 

        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            _generatedCodeFolder = await DataProviderManagerActorProxy.GeneratedCodeFolder();
            _port = await DataProviderManagerActorProxy.RegisterProvider(DatasetId);
            _datasetInfo = await DatasetEntryActorProxy(DatasetId).GetDataAsync();
            await StartProvider();
        }

        protected override async Task OnDeactivateAsync()
        {
            await StopProvider();
            await DataProviderManagerActorProxy.UnRegisterProvider(DatasetId);
        }

        private Task<(Task<int>, Action)> StartProvider()
        {
        }
        
        private Task<(Task<int>, Action)> StopProvider()
        {
        }

        private static IDataProviderManager DataProviderManagerActorProxy => 
            ActorProxy.Create<IDataProviderManager>(new ActorId("default"), ActorTypes.DataProviderManager);
        
        private static IDatasetEntry DatasetEntryActorProxy(DatasetId id) => 
            ActorProxy.Create<IDatasetEntry>(new ActorId(id.Uid), ActorTypes.DatasetEntry);
    }
}