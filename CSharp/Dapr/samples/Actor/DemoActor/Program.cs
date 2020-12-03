// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace DaprDemoActor
{
    using System.Text.Json;
    using Dapr.Actors.AspNetCore;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// Class for host.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates a IWebHostBuilder.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>IWebHostBuilder instance.</returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseActors(actorRuntime =>
                {
                    actorRuntime.RegisterActor<DemoActor>();

                    // Optionally register custom serialization options for inbound Actor requests
                    actorRuntime.ConfigureJsonSerializationOptions(options =>
                    {
                        options.PropertyNameCaseInsensitive = true;
                        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        //options.Converters.Add(new MyCustomConverter();
                    });
                });
    }
}
