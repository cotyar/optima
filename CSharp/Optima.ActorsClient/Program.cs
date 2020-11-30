using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.Actors;
using Dapr.Actors.Client;
using Google.Protobuf;
using Optima.DatasetLoader;
using Optima.Domain.DatasetDefinition;
using Optima.Interfaces;

namespace Optima.ActorsClient
{
    class Program
    {
        private static IDatasetRegistry Proxy = ActorProxy.Create<IDatasetRegistry>(new ActorId("default"), ActorTypes.DatasetRegistry);
        
        private static IDatasetEntry EntryProxy(DatasetId datasetId) => ActorProxy.Create<IDatasetEntry>(new ActorId(datasetId.Uid), ActorTypes.DatasetEntry);
        private static IDataProvider ProviderProxy(DatasetId datasetId) => ActorProxy.Create<IDataProvider>(new ActorId(datasetId.Uid), ActorTypes.DataProvider);
        static async Task Main(string[] args)
        {
            var csvSchema = FileSchemaLoader.InferCsvSchema(@"C:\Work\UMG\optima\FSharp\csv.csv");
            var jsonSchema = FileSchemaLoader.InferJsonSchema(@"C:\Work\UMG\optima\FSharp\json.json");
            var pgSchemas = PostgresDatasetSchemaLoader.LoadSchema("pg");
            var datasetInfos = await RegisterSchemas(new []{ csvSchema} /*, jsonSchema }.Concat(pgSchemas)*/);
            // await PrintAllDatasets();

            var csv = datasetInfos.First(di => di.Name == "csv");
            var csvEntryProxy = EntryProxy(csv.Id);
            var csvDataInfo = await csvEntryProxy.GetDataAsync();
            Console.WriteLine($"Dataset Info: '{JsonFormatter.Default.Format(csvDataInfo)}'");
            var csvProxy = ProviderProxy(csv.Id);
            var endpoint = await csvProxy.GetGrpcEndpoint();
            Console.WriteLine($"Endpoint: '{JsonSerializer.Serialize(endpoint)}'");
        }

        private static async Task<DatasetInfo[]> RegisterSchemas(IEnumerable<PersistenceType> schemas)
        {
            var datasets = await Task.WhenAll(schemas.Select(ProtoGenerator.GeneratorHelper.ToDatasetInfo));
            foreach (var dataset in datasets)
                await RegisterDatasetInfo(dataset);
            return datasets;
        }

        private static async Task RegisterDatasetInfo(DatasetInfo datasetInfo)
        {
            var response = await Proxy.RegisterDataset(datasetInfo);
            Console.WriteLine(response);
            // var savedDataset = await Proxy.GetDataset(datasetInfo.Id);
            // Console.WriteLine(JsonFormatter.Default.Format(savedDataset));
        }

        private static async Task PrintAllDatasets()
        {
            var savedDatasets = await Proxy.GetDatasets();
            foreach (var savedDataset  in savedDatasets)
            {
                Console.WriteLine(JsonFormatter.Default.Format(savedDataset));
            }
        }
    }
}
