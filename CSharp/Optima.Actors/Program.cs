using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Dapr.Actors.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Optima.Actors.Actors;
using Optima.Interfaces;

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
                            // actorRuntime.ConfigureActorSettings(a =>
                            // {
                            //     a.ActorIdleTimeout = TimeSpan.FromMinutes(70);
                            //     a.ActorScanInterval = TimeSpan.FromSeconds(35);
                            //     a.DrainOngoingCallTimeout = TimeSpan.FromSeconds(35);
                            //     a.DrainRebalancedActors = true;
                            // });
                            
                            // Register actor types
                            actorRuntime.RegisterActor<DatasetEntryActor>();
                            actorRuntime.RegisterActor<DatasetRegistryActor>();
                            actorRuntime.RegisterActor<SecurityRegistryActor>();
                            actorRuntime.RegisterActor<DataProviderActor>();
                            actorRuntime.RegisterActor<DataProviderManagerActor>();
                            
                            actorRuntime.ConfigureJsonSerializerOptions(options =>
                            {
                                // options.PropertyNameCaseInsensitive = true;
                                // options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                options.Converters.Add(new ProtoMessageConverter());
                            });
                        })
                        .UseUrls($"http://localhost:{AppChannelHttpPort}/");
                });
    }
}
