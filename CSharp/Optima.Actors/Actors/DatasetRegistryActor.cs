using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using Optima.Domain.Core;
using Optima.Domain.DatasetDefinition;
// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.DatasetRegistry)]
    public class DatasetRegistryActor: StatefulActorBase<DatasetRegistryActor.RegistryState>, IDatasetRegistry
    {
        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public DatasetRegistryActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId, "dataset_registry", 
                () => new RegistryState
                {
                    Ids = new HashSet<DatasetId>(),
                    ByName = new Dictionary<string, HashSet<DatasetId>>()
                })
        {
        }
        
        public async Task<Result> RegisterDataset(DatasetInfo datasetInfo)
        {
            var response = await DatasetEntryActorProxy(datasetInfo.Id).SetDataAsync(datasetInfo);
            Logger.LogInformation($"Added {datasetInfo.Id} with response {response}");
            State.Ids.Add(datasetInfo.Id);
            if (!State.ByName.TryGetValue(datasetInfo.Name, out var ids))
            {
                ids = new HashSet<DatasetId>();
                State.ByName[datasetInfo.Name] = ids;
            }

            ids.Add(datasetInfo.Id);

            await SetStateAsync();
            
            return response;
        }
        
        public async Task DeleteDataset(DatasetId id)
        {
            State.Ids.Remove(id);

            foreach (var kv in State.ByName.Where(kv => kv.Value.Contains(id)).ToArray())
            {
                kv.Value.Remove(id);
                if (!kv.Value.Any())
                {
                    State.ByName.Remove(kv.Key);
                }
            }

            var response = await DatasetEntryActorProxy(id).DeleteDataAsync();
            Logger.LogInformation($"Deleted {Id} with response {response}");
            
            await SetStateAsync();
        }
        
        public Task<DatasetInfo> GetDataset(DatasetId id) => 
            DatasetEntryActorProxy(id).GetDataAsync();

        public Task<DatasetId[]> GetDatasetIds() =>
            Task.FromResult(State.Ids.ToArray());

        public Task<DatasetId[]> GetDatasetIdsForName(string name) =>
            Task.FromResult(
                State.ByName.TryGetValue(name, out var ids)
                    ? ids.ToArray()
                    : new DatasetId[0]);

        public async Task<DatasetInfo[]> GetDatasetsForName(string name)
        {
            var ids = await GetDatasetIdsForName(name);
            return (await Task.WhenAll(ids.Select(id => Task.Run(() => GetDataset(id))))).ToArray();
        }

        public Task<string[]> GetDatasetNames()=>
            Task.FromResult(State.ByName.Keys.ToArray());

        public async Task<DatasetInfo[]> GetDatasets()
        {
            Logger.LogInformation("GetDatasets");
            var ids = await GetDatasetIds();
            return (await Task.WhenAll(ids.Select(id => Task.Run(() => GetDataset(id))))).ToArray();
        }
        
        private static IDatasetEntry DatasetEntryActorProxy(DatasetId id) => 
            ActorProxy.Create<IDatasetEntry>(new ActorId(id.Uid), ActorTypes.DatasetEntry);

        public class RegistryState
        {
            public HashSet<DatasetId> Ids { get; set; } 
            public Dictionary<string, HashSet<DatasetId>> ByName { get; set; } 
        }
    }
}