using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Optima.DatasetLoader;
using Optima.Domain.Core;
using Optima.Domain.DatasetDefinition;
using static Optima.ProtoGenerator.GeneratorHelper;
// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.DataProvider)]
    public class DataProviderActor: Actor, IDataProvider
    {
        private string _generatedCodeFolder;
        private readonly string _generatedCodeFolderBase;
        private uint _port;
        private uint _appPort;
        private DatasetInfo _datasetInfo;
        private Task<int> _providerRunningTask;
        private Func<int, Task> _providerKillSwitch;

        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public DataProviderActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            _generatedCodeFolderBase = @"C:\Work\UMG\Probs_Generated\";
        }

        private DatasetId DatasetId => new DatasetId { Uid = Id.GetId() }; 

        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            _generatedCodeFolder = await DataProviderManagerActorProxy.GeneratedCodeFolder();
            _port = await DataProviderManagerActorProxy.RegisterProvider(DatasetId);
            _appPort = _port + 10000; // TODO: Change this
            _datasetInfo = await DatasetEntryActorProxy(DatasetId).GetDataAsync();

            _generatedCodeFolder = Path.Combine(_generatedCodeFolderBase, $"DatasetGen_{_datasetInfo.Name}");
            if (!Directory.Exists(_generatedCodeFolder))
            {
                await GenerateProbes(_datasetInfo, _generatedCodeFolderBase, modelProbePath: @"../Probes/DatasetProbe", prefix: "DatasetGen_");
            }
            
            await StartProvider();
        }

        protected override async Task OnDeactivateAsync()
        {
            await StopProvider();
            await DataProviderManagerActorProxy.UnRegisterProvider(DatasetId);
        }

        private Task StartProvider()
        {
            (_providerRunningTask, _providerKillSwitch) = 
                ProcessUtils.RunAsync(Path.Combine(_generatedCodeFolder, "DatasetProbeServer"), "dapr", BuildDapperCommand());
            
            Logger.LogInformation($"Data Provider for DatasetId '{_datasetInfo.Id}' starting on port {_port}");
            return Task.Delay(3000); // TODO: Replace timeout with event-based
        }
        
        private Task StopProvider() => _providerKillSwitch?.Invoke(5000);

        private string BuildDapperCommand() => 
            $"run --app-id ds{Id} --app-port {_appPort} --dapr-http-port {_port} dotnet run -- {Id} \"{_datasetInfo.PersistedTo.File.Path}\" {_datasetInfo.PersistedTo.File.FileFormat.ToString().ToLowerInvariant()} {_appPort}";

        private static IDataProviderManager DataProviderManagerActorProxy => 
            ActorProxy.Create<IDataProviderManager>(new ActorId("default"), ActorTypes.DataProviderManager);
        
        private static IDatasetEntry DatasetEntryActorProxy(DatasetId id) => 
            ActorProxy.Create<IDatasetEntry>(new ActorId(id.Uid), ActorTypes.DatasetEntry);

        public Task<DatasetEndpoint> GetGrpcEndpoint() => 
            Task.FromResult(new DatasetEndpoint { Url = "127.0.0.1", Port = _port });
    }
}