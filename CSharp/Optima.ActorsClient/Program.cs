using System;
using System.Collections.Generic;
using System.Linq;
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
        private static IDatasetRegistry Proxy = ActorProxy.Create<IDatasetRegistry>(new ActorId("Default"), ActorTypes.DatasetRegistry);
        static async Task Main(string[] args)
        {
            var csvSchema = FileSchemaLoader.InferCsvSchema(@"C:\Work\UMG\optima\FSharp\csv.csv");
            var jsonSchema = FileSchemaLoader.InferJsonSchema(@"C:\Work\UMG\optima\FSharp\json.json");
            var pgSchemas = PostgresDatasetSchemaLoader.LoadSchema("pg");
            await RegisterSchema(new []{ csvSchema, jsonSchema }.Concat(pgSchemas));
            await PrintAllDatasets();
        }

        private static async Task RegisterSchema(IEnumerable<PersistenceType> schemas)
        {
            foreach (var schema in schemas)
                await RegisterDatasetInfo(await ProtoGenerator.GeneratorHelper.ToDatasetInfo(schema));
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
