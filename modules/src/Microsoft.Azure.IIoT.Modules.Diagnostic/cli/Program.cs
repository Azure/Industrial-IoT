// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.Cli {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge Diagnostic CLI
    /// </summary>
    public class Program {

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main(string[] args) {
            string deviceId = null, moduleId = null;
            var standalone = false;
            var echo = false;
            var publish = false;
            var logger = ConsoleLogger.Create(LogEventLevel.Information);
            Console.WriteLine("Edge Diagnostics command line interface.");
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build();
            var cs = configuration.GetValue<string>(PcsVariable.PCS_IOTHUB_CONNSTRING, null);
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
                        case "-s":
                        case "--standalone":
                            standalone = true;
                            break;
                        case "--verbose":
                            logger = ConsoleLogger.Create(LogEventLevel.Debug);
                            break;
                        case "--silent":
                            logger = Logger.None;
                            break;
                        case "--echo":
                        case "-e":
                            echo = true;
                            break;
                        case "--publish":
                        case "-p":
                            publish = true;
                            break;
                        case "-d":
                        case "--deviceId":
                            i++;
                            if (i < args.Length) {
                                deviceId = args[i];
                                break;
                            }
                            throw new ArgumentException(
                                "Missing arguments for edge device id");
                        case "-m":
                        case "--moduleId":
                            i++;
                            if (i < args.Length) {
                                moduleId = args[i];
                                break;
                            }
                            throw new ArgumentException(
                                "Missing arguments for diagnostic module id");
                        default:
                            throw new ArgumentException($"Unknown {args[i]}");
                    }

                }
                if (string.IsNullOrEmpty(cs)) {
                    throw new ArgumentException("Missing connection string.");
                }
                if (!ConnectionString.TryParse(cs, out var connectionString)) {
                    throw new ArgumentException("Bad connection string.");
                }
                config = connectionString.ToIoTHubConfig();

                if (string.IsNullOrEmpty(deviceId)) {
                    standalone = true;
                    deviceId = Dns.GetHostName();
                }
                if (string.IsNullOrEmpty(moduleId)) {
                    moduleId = "diagnostic";
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Usage:       Microsoft.Azure.IIoT.Modules.Diagnostic.Cli [Arguments]

Arguments:
    --publish
     -P
             Publish test messages.
    --echo
     -e
             Send echo pings to diagnostic module.
    --standalone
     -s
             Run the diagnostic module standalone.
    --deviceId
     -d
             The edge device id that is hosting the diagnostic module. If the value
             is not set, the host name is used and the module is run standalone.
    --moduleId
     -m
             The id of the diagnostic module in the edge.  Default: 'diagnostic'.
    --connection-string
     -C
             IoT Hub owner connection string to use to connect to IoT hub for
             operations on the registry.  If not provided, read from environment.
    --verbose
    --silent
             Do debug or suppress trace logging - defaults to informational only.
    --help
     -?
     -h      Prints out this help.
"
                    );
                return;
            }

            Console.WriteLine("Press key to cancel...");
            using (var cts = new CancellationTokenSource()) {

                var runner = Task.CompletedTask;
                var pinger = Task.CompletedTask;
                try {
                    if (standalone) {
                        // Start diagnostic module process standalone
                        runner = Task.Run(() => HostAsync(config, logger, deviceId,
                            moduleId, args), cts.Token);
                    }

                    if (echo) {
                        // Call echo method until cancelled
                        pinger = Task.Run(() => PingAsync(config, logger, deviceId,
                            moduleId, cts.Token), cts.Token);
                    }

                    if (publish) {
                        StartPublishAsync(config, logger, deviceId, moduleId,
                            TimeSpan.Zero, cts.Token).Wait();
                    }

                    // Wait until cancelled
                    Console.ReadKey();

                    if (publish) {
                        StopPublishAsync(config, logger, deviceId, moduleId, cts.Token).Wait();
                    }

                    cts.Cancel();
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    logger.Information(e, "Error during execution");
                }
                finally {
                    Task.WaitAll(runner, pinger);
                }
            }
        }

        /// <summary>
        /// Ping continously
        /// </summary>
        private static async Task PingAsync(IIoTHubConfig config, ILogger logger, string deviceId,
            string moduleId, CancellationToken ct) {
            var serializer = new NewtonSoftJsonSerializer();
            var client = new IoTHubTwinMethodClient(CreateClient(config, logger), logger);
            logger.Information("Starting echo thread");
            var found = false;

            for (var index = 0; !ct.IsCancellationRequested; index++) {
                try {
                    var message = serializer.SerializePretty(new {
                        Index = index,
                        Started = DateTime.UtcNow
                    });
                    logger.Debug("Sending ECHO {Index}... ", index);
                    var result = await client.CallMethodAsync(deviceId, moduleId,
                        "Echo_V1", message, null, ct);
                    found = true;
                    try {
                        var returned = serializer.Parse(result);
                        logger.Debug("... received back ECHO {Index} - took {Passed}.",
                            returned["Index"], DateTime.UtcNow - ((DateTime)returned["Started"]));
                    }
                    catch (Exception e) {
                        logger.Error(e, "Bad result for ECHO {Index}: {result} ",
                            index, result);
                    }
                }
                catch (Exception ex) {
                    if (!found && (ex is ResourceNotFoundException)) {
                        logger.Debug("Waiting for module to connect...");
                        continue; // Initial startup ...
                    }
                    logger.Information(ex, "Failed to send ECHO {Index}.", index);
                }
            }
            logger.Information("Echo thread completed");
        }

        /// <summary>
        /// Start publishing
        /// </summary>
        private static async Task StartPublishAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, TimeSpan interval, CancellationToken ct) {
            var client = new IoTHubTwinMethodClient(CreateClient(config, logger), logger);
            while (!ct.IsCancellationRequested) {
                try {
                    logger.Information("Start publishing...");
                    var result = await client.CallMethodAsync(deviceId, moduleId,
                        "StartPublish_V1", ((int)interval.TotalSeconds).ToString(), null, ct);
                    logger.Information("... started");
                    break;
                }
                catch (Exception ex) {
                    logger.Verbose(ex, "Failed to start publishing");
                }
            }
        }

        /// <summary>
        /// Stop publishing
        /// </summary>
        private static async Task StopPublishAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, CancellationToken ct) {
            var client = new IoTHubTwinMethodClient(CreateClient(config, logger), logger);
            while (!ct.IsCancellationRequested) {
                try {
                    logger.Information("Stop publishing...");
                    var result = await client.CallMethodAsync(deviceId, moduleId,
                        "StopPublish_V1", "", null, ct);
                    logger.Information("... stopped");
                    break;
                }
                catch (Exception ex) {
                    logger.Verbose(ex, "Failed to stop publishing");
                }
            }
        }

        /// <summary>
        /// Host the diagnostic module giving it its connection string.
        /// </summary>
        private static async Task HostAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, string[] args) {
            logger.Information("Create or retrieve connection string...");

            var cs = await Retry.WithExponentialBackoff(logger,
                () => AddOrGetAsync(config, deviceId, moduleId, logger));

            logger.Information("Starting diagnostic module...");
            var arguments = args.ToList();
            arguments.Add($"EdgeHubConnectionString={cs}");
            arguments.Add($"LogLevel={LogControl.Level.MinimumLevel}");
            Diagnostic.Program.Main(arguments.ToArray());
            logger.Information("Diagnostic module exited.");
        }

        /// <summary>
        /// Add or get module identity
        /// </summary>
        private static async Task<ConnectionString> AddOrGetAsync(IIoTHubConfig config,
            string deviceId, string moduleId, ILogger logger) {
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);
            try {
                await registry.CreateAsync(new DeviceTwinModel {
                    Id = deviceId,
                    Tags = new Dictionary<string, VariantValue> {
                        [TwinProperty.Type] = IdentityType.Gateway
                    },
                    Capabilities = new DeviceCapabilitiesModel {
                        IotEdge = true
                    }
                }, false, CancellationToken.None);
            }
            catch (ConflictingResourceException) {
                logger.Information("Gateway {deviceId} exists.", deviceId);
            }
            try {
                await registry.CreateAsync(new DeviceTwinModel {
                    Id = deviceId,
                    ModuleId = moduleId
                }, false, CancellationToken.None);
            }
            catch (ConflictingResourceException) {
                logger.Information("Module {moduleId} exists...", moduleId);
            }
            var cs = await registry.GetConnectionStringAsync(deviceId, moduleId);
            return cs;
        }

        /// <summary>
        /// Create client
        /// </summary>
        private static IoTHubServiceHttpClient CreateClient(IIoTHubConfig config,
            ILogger logger) {
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);
            return registry;
        }

    }
}
