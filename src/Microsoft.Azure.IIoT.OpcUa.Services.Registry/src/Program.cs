// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using System.IO;
    using System.Collections.Generic;

    /// <summary>
    /// Main entry point
    /// </summary>
    public static class Program {

        /// <summary>
        /// Main entry point to run the micro service process
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Build host
            var host = new WebHostBuilder()
                .UseConfiguration(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("hosting.json", true)
                    .AddEnvironmentVariables("ASPNETCORE_")
                    .AddInMemoryCollection(new Dictionary<string, string> {
                        { "urls", "http://*:9042" }
                    })
                    .AddCommandLine(args)
                    .Build())
                .ConfigureAppConfiguration((_, b) => b
                    .AddEnvironmentVariables()
                    .AddCommandLine(args))
                .UseKestrel(o => o.AddServerHeader = false)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            // Run endpoint
            host.Run();
        }
    }
}
