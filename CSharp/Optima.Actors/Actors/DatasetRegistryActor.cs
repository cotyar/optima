using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Optima.Domain.DatasetDefinition;
// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.DatasetRegistry)]
    public class DatasetRegistryActor: Actor, IDatasetRegistry
    {
        private const string StateName = "dataset_registry";
        private RegistryState _state;

        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public DatasetRegistryActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }
        
        private async Task<RegistryState> GetStateAsync()
        {
            var state = await StateManager.TryGetStateAsync<RegistryState>(StateName);
            return state.HasValue
                ? state.Value
                : new RegistryState
                    {
                        Ids = new HashSet<DatasetId>(),
                        ByName = new Dictionary<string, HashSet<DatasetId>>()
                    };
        }

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
            _state.Ids.Add(datasetInfo.Id);
            if (!_state.ByName.TryGetValue(datasetInfo.Name, out var ids))
            {
                ids = new HashSet<DatasetId>();
                _state.ByName[datasetInfo.Name] = ids;
            }

            ids.Add(datasetInfo.Id);
            
            return response;
        }
        
        public async Task DeleteDataset(DatasetId id)
        {
            _state.Ids.Remove(id);

            foreach (var kv in _state.ByName.Where(kv => kv.Value.Contains(id)).ToArray())
            {
                kv.Value.Remove(id);
                if (!kv.Value.Any())
                {
                    _state.ByName.Remove(kv.Key);
                }
            }

            var response = await DatasetEntryActorProxy(id).DeleteDataAsync();
            Console.WriteLine($"Deleted {Id} with response {response}");
        }
        
        public Task<DatasetInfo> GetDataset(DatasetId id) => 
            DatasetEntryActorProxy(id).GetDataAsync();

        public Task<DatasetId[]> GetDatasetIds() =>
            Task.FromResult(_state.Ids.ToArray());

        public Task<DatasetId[]> GetDatasetIdsForName(string name) =>
            Task.FromResult(
                _state.ByName.TryGetValue(name, out var ids)
                    ? ids.ToArray()
                    : new DatasetId[0]);

        public async Task<DatasetInfo[]> GetDatasetsForName(string name)
        {
            var ids = await GetDatasetIdsForName(name);
            return (await Task.WhenAll(ids.Select(id => Task.Run(() => GetDataset(id))))).ToArray();
        }

        public Task<string[]> GetDatasetNames()=>
            Task.FromResult(_state.ByName.Keys.ToArray());

        public async Task<DatasetInfo[]> GetDatasets()
        {
            Console.WriteLine("GetDatasets");
            var ids = await GetDatasetIds();
            return (await Task.WhenAll(ids.Select(id => Task.Run(() => GetDataset(id))))).ToArray();
        }
        
        private static IDatasetEntry DatasetEntryActorProxy(DatasetId id) => 
            ActorProxy.Create<IDatasetEntry>(new ActorId(id.Uid), ActorTypes.DatasetEntry);

        private class RegistryState
        {
            public HashSet<DatasetId> Ids { get; set; } 
            public Dictionary<string, HashSet<DatasetId>> ByName { get; set; } 
        }
    }
}