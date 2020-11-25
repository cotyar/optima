using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Actors.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Optima.Actors.Actors;

namespace Optima.Actors
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        
        private const int AppChannelHttpPort = 3000;

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseActors(actorRuntime =>
                        {
                            // Register actor types
                            actorRuntime.RegisterActor<DatasetEntryActor>();
                            actorRuntime.RegisterActor<DatasetRegistryActor>();
                        })
                        .UseUrls($"http://localhost:{AppChannelHttpPort}/");
                });
    }
}
