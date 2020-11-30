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
    [Actor(TypeName = ActorTypes.DataProviderManager)]
    public class DataProviderManagerActor: StatefulActorBase<DataProviderManagerActor.ManagerState>, IDataProviderManager
    {
        private string _generatedCodeFolder = @"C:\Work\UMG\Probs_Generated\";

        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public DataProviderManagerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId, "dataset_registry", 
                () => new ManagerState
                {
                    ActiveProviders = new Dictionary<string, uint>(),
                    NextFreePort = 10000
                })
        {
        }

        public async Task<uint> RegisterProvider(DatasetId requesterId)
        {
            var port = State.NextFreePort++;
            State.ActiveProviders[requesterId.Uid] = port; // TODO: Take care of the case when requestorId is already there
            await SetStateAsync();
            return port;
        }

        public async Task UnRegisterProvider(DatasetId requesterId)
        {
            State.ActiveProviders.Remove(requesterId.Uid);
            await SetStateAsync();
        }

        public Task<Dictionary<DatasetId, uint>> ActiveDatasetProviders() => 
            Task.FromResult(State.ActiveProviders.ToDictionary(x => new DatasetId { Uid = x.Key }, x => x.Value));

        public Task<string> GeneratedCodeFolder() => Task.FromResult(_generatedCodeFolder);

        public class ManagerState
        {
            // public HashSet<DatasetId> Ids { get; set; } 
            public Dictionary<string, uint> ActiveProviders { get; set; } 
            public uint NextFreePort { get; set; }
        }
    }
}