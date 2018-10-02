// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    /// <summary>Application entry point</summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
            Kestrel is a cross-platform HTTP server based on libuv, a
            cross-platform asynchronous I/O library.
            https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers
            */
            // Load hosting configuration
            var configRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddCommandLine(args)
                .AddEnvironmentVariables("ASPNETCORE_")
                .AddJsonFile("hosting.json", true)
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "urls", "http://*:58801" }
                })
                .Build();

            /*
            Print some information to help development and debugging, like
            runtime and configuration settings
            */
            Console.WriteLine($"[{Uptime.ProcessId}] Starting web service, process ID: " + Uptime.ProcessId);

            var host = new WebHostBuilder()
                .UseConfiguration(configRoot)
                .UseKestrel(options => { options.AddServerHeader = false; })
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
