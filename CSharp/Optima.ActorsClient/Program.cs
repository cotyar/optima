using System;
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
            Console.WriteLine("Hello World!");
            // while (true) 
            // {
            //     for (int i = 0; i < 1000; i++)
            //         await InvokeActorMethodWithRemotingAsync(i);
            //     Console.WriteLine("Done");
            //     Console.ReadKey();
            // }

            var schema = FileSchemaLoader.InferCsvSchema(@"C:\Work\UMG\optima\FSharp\csv.csv");
            var datasetInfo = await ProtoGenerator.GeneratorHelper.ToDatasetInfo(schema);
            await InvokeActorMethodWithRemotingAsync(datasetInfo);
        }
        
        static async Task InvokeActorMethodWithRemotingAsync(DatasetInfo datasetInfo)
        {
            var actorType = "DatasetEntry";      // Registered Actor Type in Actor Service
            var actorId = new ActorId(datasetInfo.Id.Uid);

            // Create the local proxy by using the same interface that the service implements
            // By using this proxy, you can call strongly typed methods on the interface using Remoting.
            var proxy = ActorProxy.Create<IDatasetEntry>(actorId, actorType);
            var response = await proxy.SetDataAsync(datasetInfo);
            // Console.WriteLine(response);

            var savedData = await proxy.GetDataAsync();
            Console.WriteLine(JsonFormatter.Default.Format(savedData));
        }
    }
}
