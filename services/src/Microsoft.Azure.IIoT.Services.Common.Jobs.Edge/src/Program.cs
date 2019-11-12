// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge {
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Web service host
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            Console.WriteLine(@"                          _          _       _        _____                 _          ");
            Console.WriteLine(@"    /\                   | |        | |     | |      / ____|               (_)         ");
            Console.WriteLine(@"   /  \   __ _  ___ _ __ | |_       | | ___ | |__   | (___   ___ _ ____   ___  ___ ___ ");
            Console.WriteLine(@"  / /\ \ / _` |/ _ \ '_ \| __|  _   | |/ _ \| '_ \   \___ \ / _ \ '__\ \ / / |/ __/ _ \");
            Console.WriteLine(@" / ____ \ (_| |  __/ | | | |_  | |__| | (_) | |_) |  ____) |  __/ |   \ V /| | (_|  __/");
            Console.WriteLine(@"/_/    \_\__, |\___|_| |_|\__|  \____/ \___/|_.__/  |_____/ \___|_|    \_/ |_|\___\___|");
            Console.WriteLine(@"          __/ |                                                                        ");
            Console.WriteLine(@"         |___/                                                                         ");
            Console.WriteLine();

            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create builder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            return WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("hosting.json", true)
                    .AddEnvironmentVariables("ASPNETCORE_")
                    .AddInMemoryCollection(new Dictionary<string, string> { { "urls", "http://*:9051" } })
                    .AddCommandLine(args)
                    .Build())
                .UseKestrel(o => o.AddServerHeader = false)
                .UseIISIntegration()
                .UseStartup<Startup>()
                ;
        }
    }
}