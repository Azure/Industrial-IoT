// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs {
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Job service
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create host builder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            return WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("hosting.json", true)
                    .AddEnvironmentVariables("ASPNETCORE_")
                    .AddInMemoryCollection(new Dictionary<string, string> { { "urls", "http://*:9046" } })
                    .AddCommandLine(args)
                    .Build())
                .UseKestrel(o => o.AddServerHeader = false)
                .UseIISIntegration()
                .UseStartup<Startup>()
                ;
        }
    }
}