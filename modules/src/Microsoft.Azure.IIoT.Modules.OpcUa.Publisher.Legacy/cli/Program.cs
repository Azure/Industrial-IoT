// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Cli {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Sample;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher module host process
    /// </summary>
    public class Program {

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main(string[] args) {
            var publish = false;
            var checkTrust = false;
            var listNodes = false;
            string deviceId = null, moduleId = null;
            Console.WriteLine("Publisher module command line interface.");
            var configuration = new ConfigurationBuilder()
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                //.AddFromKeyVault()
                .Build();
            var cs = configuration.GetValue<string>("PCS_IOTHUB_CONNSTRING", null);
            if (string.IsNullOrEmpty(cs)) {
                cs = configuration.GetValue<string>("_HUB_CS", null);
            }
            IIoTHubConfig config = null;
            var unknownArgs = new List<string>();
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
                        case "-p":
                        case "--publish":
                            publish = true;
                            break;
                        case "-l":
                        case "--list":
                            listNodes = true;
                            break;
                        case "-t":
                        case "--only-trusted":
                            checkTrust = true;
                            break;
                        default:
                            unknownArgs.Add(args[i]);
                            break;
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
                    moduleId = "opcpublisher";
                    Console.WriteLine($"Using <moduleId> '{moduleId}'");
                }

                args = unknownArgs.ToArray();
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Usage:       Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Cli [options]

Options:
     -C
    --connection-string
             IoT Hub owner connection string to use to connect to IoT hub for
             operations on the registry.  If not provided, read from environment.
     -p
    --publish
             Connects to and publishes a set of nodes in the built-in sample
             server.
     -l
    --listNodes
             Continously lists published nodes - if combined with publish only
             lists the published nodes on the endpoint.

    --help
     -?
     -h      Prints out this help.
"
                    );
                return;
            }

            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                logger.Fatal(e.ExceptionObject as Exception, "Exception");
                Console.WriteLine(e);
            };

            try {
                if (publish) {
                    PublishAsync(config, logger, deviceId, moduleId, listNodes, args).Wait();
                }
                else {
                    HostAsync(config, logger, deviceId, moduleId, args, !checkTrust).Wait();
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Host the module giving it its connection string.
        /// </summary>
        private static async Task HostAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, string[] args, bool acceptAll = false) {
            Console.WriteLine("Create or retrieve connection string...");

            var cs = await Retry.WithExponentialBackoff(logger,
                () => AddOrGetAsync(config, deviceId, moduleId));

            // Hook event source
            using (var broker = new EventSourceBroker()) {
                LogControl.Level.MinimumLevel = LogEventLevel.Verbose;

                Console.WriteLine("Starting publisher module...");
                broker.Subscribe(IoTSdkLogger.EventSource, new IoTSdkLogger(logger));
                var arguments = args.ToList();
                arguments.Add($"--ec={cs}");
                arguments.Add($"--si=0");
                arguments.Add($"--ms=0");
                if (acceptAll) {
                    arguments.Add("--aa");
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    arguments.Add("--at=X509Store");
                }
                Try.Op(() => File.Delete("publishednodes.json"));
                OpcPublisher.Program.Main(arguments.ToArray());
                Console.WriteLine("Publisher module exited.");
            }
        }

        /// <summary>
        /// setup publishing from sample server
        /// </summary>
        private static async Task PublishAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, bool listNodes, string[] args) {
            try {
                using (var cts = new CancellationTokenSource())
                using (var server = new ServerWrapper(logger)) { // Start test server
                    // Start publisher module
                    var host = Task.Run(() => HostAsync(config, logger, deviceId,
                        moduleId, args, true), cts.Token);

                    // Wait a bit
                    await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);

                    // Nodes to publish
                    var nodes = new string[] {
                        "i=2258",  // Server time
                        "ns=12;s=0:Boiler #1?Drum/Level/Measurement"
                        // ...
                    };

                    foreach (var node in nodes) {
                        await PublishNodesAsync(config, logger, deviceId, moduleId,
                            server.EndpointUrl, node, true, cts.Token);
                    }

                    var lister = Task.CompletedTask;
                    if (listNodes) {
                        lister = Task.Run(() => ListNodesAsync(config, logger,
                            deviceId, moduleId, server.EndpointUrl, cts.Token), cts.Token);
                    }

                    Console.WriteLine("Press key to cancel...");
                    Console.ReadKey();

                    foreach (var node in nodes) {
                        await PublishNodesAsync(config, logger, deviceId, moduleId,
                            server.EndpointUrl, node, false, CancellationToken.None);
                    }

                    logger.Information("Server exiting - tear down publisher...");
                    cts.Cancel();

                    await lister;
                    await host;
                }
            }
            catch (OperationCanceledException) { }
            finally {
                Try.Op(() => File.Delete("publishednodes.json"));
            }
        }

        /// <summary>
        /// List nodes on endpoint
        /// </summary>
        private static async Task ListNodesAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, string endpointUrl, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointUrl)) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            var client = new IoTHubTwinMethodClient(CreateClient(config, logger), logger);
            while (!ct.IsCancellationRequested) {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                try {
                    var content = new GetNodesRequestModel {
                        EndpointUrl = endpointUrl
                    };
                    var result = await client.CallMethodAsync(deviceId, moduleId,
                        "GetConfiguredNodesOnEndpoint", JsonConvertEx.SerializeObject(content),
                        null, ct);
                    var response = JsonConvertEx.DeserializeObject<GetNodesResponseModel>(result);
                    logger.Information("Published nodes: {@response}", response);
                }
                catch (Exception ex) {
                    logger.Verbose(ex, "Failed to list published nodes.");
                }
            }
        }

        /// <summary>
        /// Configure publishing of a particular node
        /// </summary>
        private static async Task PublishNodesAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, string endpointUrl, string nodeId,
            bool publish, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpointUrl)) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            if (string.IsNullOrEmpty(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }
            var client = new IoTHubTwinMethodClient(CreateClient(config, logger), logger);
            while (!ct.IsCancellationRequested) {
                try {
                    logger.Information("Start publishing {nodeId}...", nodeId);
                    var content = new PublishNodesRequestModel {
                        EndpointUrl = endpointUrl,
                        UseSecurity = true,
                        OpcNodes = new List<PublisherNodeModel> {
                            new PublisherNodeModel {
                                Id = nodeId,
                                OpcPublishingInterval = 1000,
                                OpcSamplingInterval = 1000
                            }
                        }
                    };
                    var result = await client.CallMethodAsync(deviceId, moduleId,
                        publish ? "PublishNodes" : "UnpublishNodes",
                        JsonConvertEx.SerializeObject(content), null, ct);
                    logger.Information("... started");
                    break;
                }
                catch (Exception ex) {
                    logger.Verbose(ex, "Failed to configure publishing.");
                    // Wait a bit
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }
            }
        }

        /// <summary>
        /// Add or get module identity
        /// </summary>
        private static async Task<ConnectionString> AddOrGetAsync(IIoTHubConfig config,
            string deviceId, string moduleId) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var registry = CreateClient(config, logger);
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

        /// <summary>
        /// Create client
        /// </summary>
        private static IoTHubServiceHttpClient CreateClient(IIoTHubConfig config,
            ILogger logger) {
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);
            return registry;
        }

        /// <summary>
        /// Wraps server and disposes after use
        /// </summary>
        private class ServerWrapper : IDisposable {

            public string EndpointUrl { get; }

            /// <summary>
            /// Create wrapper
            /// </summary>
            public ServerWrapper(ILogger logger) {
                _cts = new CancellationTokenSource();
                _server = RunSampleServerAsync(_cts.Token, logger);
                EndpointUrl = "opc.tcp://" + Opc.Ua.Utils.GetHostName() +
                    ":51210/UA/SampleServer";
            }

            /// <inheritdoc/>
            public void Dispose() {
                _cts.Cancel();
                _server.Wait();
                _cts.Dispose();
            }

            /// <summary>
            /// Run server until cancelled
            /// </summary>
            private static async Task RunSampleServerAsync(CancellationToken ct, ILogger logger) {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetResult(true));
                using (var server = new ServerConsoleHost(new ServerFactory(logger) {
                    LogStatus = false
                }, logger) {
                    AutoAccept = true
                }) {
                    logger.Information("Starting server.");
                    await server.StartAsync(new List<int> { 51210 });
                    logger.Information("Server started.");
                    await tcs.Task;
                    logger.Information("Server exited.");
                }
            }

            private readonly CancellationTokenSource _cts;
            private readonly Task _server;
        }

        /// <summary>
        /// Sdk logger event source hook
        /// </summary>
        sealed class IoTSdkLogger : EventSourceSerilogSink {
            public IoTSdkLogger(ILogger logger) :
                base(logger.ForContext("SourceContext", EventSource.Replace('-', '.'))) {
            }

            public override void OnEvent(EventWrittenEventArgs eventData) {
                switch (eventData.EventName) {
                    case "Enter":
                    case "Exit":
                    case "Associate":
                        WriteEvent(LogEventLevel.Verbose, eventData);
                        break;
                    default:
                        WriteEvent(LogEventLevel.Debug, eventData);
                        break;
                }
            }

            // ddbee999-a79e-5050-ea3c-6d1a8a7bafdd
            public const string EventSource = "Microsoft-Azure-Devices-Device-Client";
        }
    }
}
