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
        /// <param name="userString"></param>
        private static void LogLogo(string userString)
        {
            Console.WriteLine($@"
 ██████╗ ██████╗  ██████╗    ██████╗ ██╗   ██╗██████╗ ██╗     ██╗███████╗██╗  ██╗███████╗██████╗
██╔═══██╗██╔══██╗██╔════╝    ██╔══██╗██║   ██║██╔══██╗██║     ██║██╔════╝██║  ██║██╔════╝██╔══██╗
██║   ██║██████╔╝██║         ██████╔╝██║   ██║██████╔╝██║     ██║███████╗███████║█████╗  ██████╔╝
██║   ██║██╔═══╝ ██║         ██╔═══╝ ██║   ██║██╔══██╗██║     ██║╚════██║██╔══██║██╔══╝  ██╔══██╗
╚██████╔╝██║     ╚██████╗    ██║     ╚██████╔╝██████╔╝███████╗██║███████║██║  ██║███████╗██║  ██║
 ╚═════╝ ╚═╝      ╚═════╝    ╚═╝      ╚═════╝ ╚═════╝ ╚══════╝╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝
{PublisherConfig.Version,97}
{userString,97}
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
            var userString = string.Empty;

            if (PublisherConfig.IsContainer)
            {
                var currentDir = Environment.CurrentDirectory;
                var currentUser = Environment.UserName;
                if (!IsWriteable(currentDir))
                {
                    currentDir = "/home" +
                        (string.IsNullOrEmpty(currentUser) ? currentDir : $"/{currentUser}");
                    // Rootless containers have read-only filesystem except /home
                    Environment.CurrentDirectory = currentDir;
                }
                userString =  $"Running as [{currentUser}] in {currentDir}";
            }

            LogLogo(userString);
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


        /// <summary>
        /// Tests whether the path is writeable
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsWriteable(string path)
        {
            try
            {
                string tempFilePath = Path.Combine(path, Path.GetRandomFileName());
                using (FileStream fs = File.Create(tempFilePath))
                {
                    // File created successfully, directory is writable
                }
                // Delete the temporary file
                File.Delete(tempFilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }


    }
}
