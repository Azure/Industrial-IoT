// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    /// <summary>Application entry point</summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
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
                    .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                        .ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console())
                    .Build();

                host.Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
