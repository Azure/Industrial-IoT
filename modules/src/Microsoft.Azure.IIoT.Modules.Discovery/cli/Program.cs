// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.Cli {
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Serilog.Events;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Discovery module host process
    /// </summary>
    public class Program {

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main(string[] args) {
            string deviceId = null, moduleId = null;
            Console.WriteLine("Discovery module command line interface.");
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build();
            var cs = configuration.GetValue<string>("PCS_IOTHUB_CONNSTRING", null);
            if (string.IsNullOrEmpty(cs)) {
                cs = configuration.GetValue<string>("_HUB_CS", null);
            }
            IIoTHubConfig config = null;
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "-C":
                        case "--connection-string":
                            i++;
                            if (i < args.Length) {
                                cs = args[i];
                                break;
                            }
                            throw new ArgumentException(
                                "Missing arguments for connection string");
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException("Help");
                    }

                }
                if (string.IsNullOrEmpty(cs)) {
                    throw new ArgumentException("Missing connection string.");
                }
                if (!ConnectionString.TryParse(cs, out var connectionString)) {
                    throw new ArgumentException("Bad connection string.");
                }
                config = connectionString.ToIoTHubConfig();
                if (deviceId == null) {
                    deviceId = Dns.GetHostName();
                    Console.WriteLine($"Using <deviceId> '{deviceId}'");
                }
                if (moduleId == null) {
                    moduleId = "discovery";
                    Console.WriteLine($"Using <moduleId> '{moduleId}'");
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Usage:       Microsoft.Azure.IIoT.Modules.Discovery.Cli [options]

Options:
     -C
    --connection-string
             IoT Hub owner connection string to use to connect to IoT hub for
             operations on the registry.  If not provided, read from environment.

    --help
     -?
     -h      Prints out this help.
"
                    );
                return;
            }
            try {
                HostAsync(config, deviceId, moduleId, args).Wait();
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Host the module giving it its connection string.
        /// </summary>
        private static async Task HostAsync(IIoTHubConfig config,
            string deviceId, string moduleId, string[] args) {
            Console.WriteLine("Create or retrieve connection string...");
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var cs = await Retry.WithExponentialBackoff(logger,
                () => AddOrGetAsync(config, deviceId, moduleId));
            Console.WriteLine("Starting discovery module...");
            var arguments = args.ToList();
            arguments.Add($"EdgeHubConnectionString={cs}");
            Discovery.Program.Main(arguments.ToArray());
            Console.WriteLine("Discovery module exited.");
        }

        /// <summary>
        /// Add or get module identity
        /// </summary>
        private static async Task<ConnectionString> AddOrGetAsync(IIoTHubConfig config,
            string deviceId, string moduleId) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);
            await registry.CreateAsync(new DeviceTwinModel {
                Id = deviceId,
                ModuleId = moduleId,
                Capabilities = new DeviceCapabilitiesModel {
                    IotEdge = true
                }
            }, true, CancellationToken.None);
            var cs = await registry.GetConnectionStringAsync(deviceId, moduleId);
            return cs;
        }
    }
}
