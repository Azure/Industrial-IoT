// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Autofac.Extensions.DependencyInjection;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Furly.Extensions.Hosting;
    using k8s;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Module
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Log logo
        /// </summary>
        private static void LogLogo()
        {
            Console.WriteLine($@"
 ██████╗ ██████╗  ██████╗    ██████╗ ██╗   ██╗██████╗ ██╗     ██╗███████╗██╗  ██╗███████╗██████╗
██╔═══██╗██╔══██╗██╔════╝    ██╔══██╗██║   ██║██╔══██╗██║     ██║██╔════╝██║  ██║██╔════╝██╔══██╗
██║   ██║██████╔╝██║         ██████╔╝██║   ██║██████╔╝██║     ██║███████╗███████║█████╗  ██████╔╝
██║   ██║██╔═══╝ ██║         ██╔═══╝ ██║   ██║██╔══██╗██║     ██║╚════██║██╔══██║██╔══╝  ██╔══██╗
╚██████╔╝██║     ╚██████╗    ██║     ╚██████╔╝██████╔╝███████╗██║███████║██║  ██║███████╗██║  ██║
 ╚═════╝ ╚═╝      ╚═════╝    ╚═╝      ╚═════╝ ╚═════╝ ╚══════╝╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝
{PublisherConfig.Version,97}
");
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
#if DEBUG
            if (args.Any(a => a.Contains("wfd", StringComparison.InvariantCultureIgnoreCase) ||
                a.Contains("waitfordebugger", StringComparison.InvariantCultureIgnoreCase)) ||
                KubernetesClientConfiguration.IsInCluster())
            {
                Console.WriteLine("Waiting for debugger being attached...");
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                }
                Console.WriteLine("Debugger attached.");
                Debugger.Break();
            }
#endif
            using var cts = new CancellationTokenSource();
            RunAsync(args, cts.Token).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Async main entry point
        /// </summary>
        /// <param name="args"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task RunAsync(string[] args, CancellationToken ct)
        {
            LogLogo();
            await CreateHostBuilder(args).RunAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Create host builder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureHostConfiguration(builder => builder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables()
                    .AddFromDotEnvFile()
                    .AddSecrets()
                    .AddConnectorAdditionalConfiguration()
                    .AddInMemoryCollection(new CommandLine(args)))
                .ConfigureWebHostDefaults(builder => builder
                    //.UseUrls("http://*:9702", "https://*:9703")
                    .UseStartup<Startup>()
                    .UseKestrel(o => o.AddServerHeader = false))
                ;
        }
    }
}
