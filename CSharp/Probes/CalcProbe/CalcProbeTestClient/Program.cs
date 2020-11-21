using System;
using System.Linq;
using System.Threading.Tasks;
using Generated;
using Grpc.Core;
using Grpc.Core.Utils;
using AutoFixture;

namespace CalcProbeTestClient
{
    class Program
    {
        const int PortBase = 5050;
        
        static async Task Main(string[] args)
        {
            Console.WriteLine($"Calling {PortBase + 1} no lineage");

            var sharpClient = new Calc.CalcClient(new Channel("localhost", PortBase + 1, ChannelCredentials.Insecure));
            var sharpWithLinClient = new Calc.CalcClient(new Channel("localhost", PortBase + 2, ChannelCredentials.Insecure));
            
            var sharpEcho = sharpClient.Echo(ToTestRequest(-1));
            Console.WriteLine($"Echo C# server: {sharpEcho}");
            
            var sharpRunCall = sharpClient.Run();
            await sharpRunCall.RequestStream.WriteAllAsync(Enumerable.Range(0, 10).Select(ToTestRequest));
            await sharpRunCall.ResponseStream.ForEachAsync(async r => Console.WriteLine($"C# server response: {r}"));

            Console.ReadKey();
            
            Console.WriteLine($"Calling {PortBase + 2} with lineage");
            
            var sharpLinEcho = sharpWithLinClient.Echo(ToTestRequest(-1));
            Console.WriteLine($"Echo C# Lin server: {sharpEcho}");
            
            var sharpLinRunCall = sharpWithLinClient.RunWithLineage();
            await sharpLinRunCall.RequestStream.WriteAllAsync(Enumerable.Range(0, 10).Select(ToTestRequest).Select(ToTestRequestWithLineage));
            await sharpLinRunCall.ResponseStream.ForEachAsync(async r => Console.WriteLine($"C# Lin server response: {r}"));
        }

        static Req ToTestRequest(int i) => new Fixture().Create<Req>();
        static ReqWithLineage ToTestRequestWithLineage(Req req) => new ReqWithLineage { Request = req };
    }
}