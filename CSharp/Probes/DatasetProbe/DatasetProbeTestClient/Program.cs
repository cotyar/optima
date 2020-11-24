using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;
using Google.Protobuf.WellKnownTypes;
using LinNet;

// {{#UsingsDs}}
using Generated;
// {{/UsingsDs}}

namespace CalcProbeTestClient
{
    class Program
    {
        const int PortBase = 5000;
        
        static async Task Main(string[] args)
        {
            Console.WriteLine($"Calling {PortBase}");

            var sharpClient = new DatasetSource.DatasetSourceClient(new Channel("localhost", PortBase, ChannelCredentials.Insecure));
            
            var all = sharpClient.Data(new DatasetDataRequest { All = new Empty() });
            Console.WriteLine($"All from C# server: {JsonSerializer.Serialize(await all.ResponseStream.ToListAsync(), new JsonSerializerOptions {WriteIndented = true})}");
            

        }
    }
}