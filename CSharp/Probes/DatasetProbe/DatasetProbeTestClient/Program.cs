using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Generated;
using Grpc.Core;
using Grpc.Core.Utils;
using AutoFixture;
using Google.Protobuf.WellKnownTypes;
using LinNet;

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