using System;
using System.Threading.Tasks;
using Dapr.Actors;
using Dapr.Actors.Client;
using Optima.Domain.DatasetDefinition;
using Optima.Interfaces;

namespace Optima.ActorsClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            while (true) 
            {
                for (int i = 0; i < 1000; i++)
                    await InvokeActorMethodWithRemotingAsync(i);
                Console.WriteLine("Done");
                Console.ReadKey();
            }
        }
        
        static async Task InvokeActorMethodWithRemotingAsync(int i)
        {
            var actorType = "DatasetEntry";      // Registered Actor Type in Actor Service
            var actorId = new ActorId($"{i}");

            // Create the local proxy by using the same interface that the service implements
            // By using this proxy, you can call strongly typed methods on the interface using Remoting.
            var proxy = ActorProxy.Create<IDatasetEntry>(actorId, actorType);
            var response = await proxy.SetDataAsync(new DatasetInfo 
            {
                Name = "Test actor",
                Metadata = new DatasetMetadata
                {
                    Message = "Hi" 
                }
            });
            // Console.WriteLine(response);

            var savedData = await proxy.GetDataAsync();
            // Console.WriteLine(savedData);
        }
    }
}
