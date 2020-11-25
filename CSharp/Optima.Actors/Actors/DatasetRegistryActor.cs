using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Google.Protobuf.WellKnownTypes;
using Optima.Domain.DatasetDefinition;
// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.DatasetRegistry)]
    public class DatasetRegistryActor: Actor, IDatasetRegistry
    {
        private const string StateName = "dataset_registry";
        private ConditionalValue<RegistryState> _state;

        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public DatasetRegistryActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }
        
        private Task<ConditionalValue<RegistryState>> GetStateAsync() => StateManager.TryGetStateAsync<RegistryState>(StateName);

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            // Provides opportunity to perform some optional setup.
            Console.WriteLine($"Activating actor id: {Id}");
            _state = await GetStateAsync();
            Console.WriteLine($"Loaded state: {_state}");
        }

        /// <summary>
        /// This method is called whenever an actor is deactivated after a period of inactivity.
        /// </summary>
        protected override Task OnDeactivateAsync()
        {
            // Provides Opportunity to perform optional cleanup.
            Console.WriteLine($"Deactivating actor id: {Id}");
            return Task.CompletedTask;
        }


        public async Task<string> RegisterDataset(DatasetInfo datasetInfo)
        {
            var response = await DatasetEntryActorProxy(datasetInfo.Id).SetDataAsync(datasetInfo);
            Console.WriteLine($"Added {datasetInfo.Id} with response {response}");
            return response;
        }
        
        public Task<DatasetInfo> GetDataset(DatasetId id) => 
            DatasetEntryActorProxy(id).GetDataAsync();
        
        public async Task DeleteDataset(DatasetId id)
        {
            var response = await DatasetEntryActorProxy(id).DeleteDataAsync();
            Console.WriteLine($"Deleted {Id} with response {response}");
        }

        public Task<ImmutableArray<DatasetId>> GetDatasetIds() =>
            Task.FromResult(
                _state.HasValue
                ? _state.Value.Ids.ToImmutableArray()
                : ImmutableArray<DatasetId>.Empty);

        public Task<ImmutableArray<DatasetId>> GetDatasetIdsForName(string name) =>
            Task.FromResult(
                _state.HasValue && _state.Value.ByName.TryGetValue(name, out var ids)
                    ? ids.ToImmutableArray()
                    : ImmutableArray<DatasetId>.Empty);

        public async Task<ImmutableArray<DatasetInfo>> GetDatasetsForName(string name)
        {
            var ids = await GetDatasetIdsForName(name);
            return (await Task.WhenAll(ids.Select(id => Task.Run(() => GetDataset(id))))).ToImmutableArray();
        }

        public Task<ImmutableArray<string>> GetDatasetNames()=>
            Task.FromResult(
                _state.HasValue
                    ? _state.Value.ByName.Keys.ToImmutableArray()
                    : ImmutableArray<string>.Empty);

        public async Task<ImmutableArray<DatasetInfo>> GetDatasets()
        {
            var ids = await GetDatasetIds();
            return (await Task.WhenAll(ids.Select(id => Task.Run(() => GetDataset(id))))).ToImmutableArray();
        }
        
        private static IDatasetEntry DatasetEntryActorProxy(DatasetId id) => 
            ActorProxy.Create<IDatasetEntry>(new ActorId(id.Uid), ActorTypes.DatasetEntry);

        private class RegistryState
        {
            public HashSet<DatasetId> Ids { get; set; } 
            public Dictionary<string, List<DatasetId>> ByName { get; set; } 
        }
    }
}