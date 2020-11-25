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
        static async Task Main(string[] args)
        {
            var csvSchema = FileSchemaLoader.InferCsvSchema(@"C:\Work\UMG\optima\FSharp\csv.csv");
            var jsonSchema = FileSchemaLoader.InferJsonSchema(@"C:\Work\UMG\optima\FSharp\json.json");
            var pgSchemas = PostgresDatasetSchemaLoader.LoadSchema("pg");
            await RegisterSchema(new []{ csvSchema, jsonSchema }.Concat(pgSchemas));
        }

        private static async Task RegisterSchema(IEnumerable<PersistenceType> schemas)
        {
            foreach (var schema in schemas)
                await RegisterDatasetInfo(await ProtoGenerator.GeneratorHelper.ToDatasetInfo(schema));
        }

        private static async Task RegisterDatasetInfo(DatasetInfo datasetInfo)
        {
            var actorId = new ActorId(datasetInfo.Id.Uid);

            // Create the local proxy by using the same interface that the service implements
            // By using this proxy, you can call strongly typed methods on the interface using Remoting.
            var proxy = ActorProxy.Create<IDatasetEntry>(actorId, ActorTypes.DatasetEntry);
            var response = await proxy.SetDataAsync(datasetInfo);
            Console.WriteLine(response);

            var savedData = await proxy.GetDataAsync();
            Console.WriteLine(JsonFormatter.Default.Format(savedData));
        }
    }
}
