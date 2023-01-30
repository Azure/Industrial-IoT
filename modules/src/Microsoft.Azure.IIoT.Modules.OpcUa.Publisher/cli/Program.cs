// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Cli {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Sample;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Net;
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
            var checkTrust = true;
            var withServer = false;
            var verbose = false;
            string deviceId = null, moduleId = null;

            var logger = ConsoleLogger.Create(LogEventLevel.Error);

            logger.Information("Publisher module command line interface.");
            var configuration = new ConfigurationBuilder()
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                // Above configuration providers will provide connection
                // details for KeyVault configuration provider.
                .AddFromKeyVault(providerPriority: ConfigurationProviderPriority.Lowest)
                .Build();
            var cs = configuration.GetValue<string>(PcsVariable.PCS_IOTHUB_CONNSTRING, null);
            if (string.IsNullOrEmpty(cs)) {
                cs = configuration.GetValue<string>("_HUB_CS", null);
            }
            IIoTHubConfig config = null;
            int instances = 1;
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
                                "Missing argument for --connection-string");
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException("Help");
                        case "-n":
                        case "--instances":
                            i++;
                            if (i < args.Length && int.TryParse(args[i], out instances)) {
                                break;
                            }
                            throw new ArgumentException(
                                "Missing argument for --instances");
                        case "-t":
                        case "--only-trusted":
                            checkTrust = true;
                            break;
                        case "-s":
                        case "--with-server":
                            withServer = true;
                            break;
                        case "-v":
                        case "--verbose":
                            verbose = true;
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
                    deviceId = Utils.GetHostName();
                    logger.Information($"Using <deviceId> '{deviceId}'");
                }
                if (moduleId == null) {
                    moduleId = "publisher";
                    logger.Information($"Using <moduleId> '{moduleId}'");
                }

                args = unknownArgs.ToArray();
            }
            catch (Exception e) {
                logger.Error(e.Message);
                logger.Error(
                    @"
Usage:       Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Cli [options]

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

            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                logger.Fatal(e.ExceptionObject as Exception, "Exception");
            };

            var tasks = new List<Task>(instances);
            try {
                var enableEventBroker = instances == 1;
                for (var i = 1; i < instances; i++) {
                    tasks.Add(HostAsync(config, logger, deviceId + "_" + i, moduleId, args, verbose, !checkTrust));
                }
                if (!withServer) {
                    tasks.Add(HostAsync(config, logger, deviceId, moduleId, args, verbose, !checkTrust, enableEventBroker));
                }
                else {
                    tasks.Add(WithServerAsync(config, logger, deviceId, moduleId, args, verbose));
                }
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e) {
                logger.Error(e, "Exception");
            }
        }

        /// <summary>
        /// Host the module giving it its connection string.
        /// </summary>
        private static async Task HostAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, string[] args, bool verbose = false,
            bool acceptAll = false, bool eventBroker = false) {
            logger.Information("Create or retrieve connection string for {deviceId} {moduleId}...",
                deviceId, moduleId);

            var cs = await Retry.WithExponentialBackoff(logger,
                () => AddOrGetAsync(config, deviceId, moduleId, logger));

            // Hook event source
            if (eventBroker) {
                using (var broker = new EventSourceBroker()) {
                    broker.Subscribe(IoTSdkLogger.EventSource, new IoTSdkLogger(logger));
                    Run(logger, deviceId, moduleId, args, verbose, acceptAll, eventBroker, cs);
                }
            }
            else {
                Run(logger, deviceId, moduleId, args, verbose, acceptAll, eventBroker, cs);
            }

            static void Run(ILogger logger, string deviceId, string moduleId, string[] args,
                bool verbose, bool acceptAll, bool eventBroker, ConnectionString cs) {
                LogControl.Level.MinimumLevel = verbose ? LogEventLevel.Verbose : LogEventLevel.Information;

                logger.Information("Starting publisher module {deviceId} {moduleId}...",
                    deviceId, moduleId);
                var arguments = args.ToList();
                arguments.Add($"--ec={cs}");
                if (acceptAll) {
                    arguments.Add("--aa");
                }
                Publisher.Program.Main(arguments.ToArray());
                logger.Information("Publisher module {deviceId} {moduleId} exited.",
                    deviceId, moduleId);
            }
        }

        /// <summary>
        /// setup publishing from sample server
        /// </summary>
        private static async Task WithServerAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, string[] args, bool verbose = false) {
            try {
                using (var cts = new CancellationTokenSource())
                using (var server = new ServerWrapper(logger)) { // Start test server
                    // Start publisher module
                    var host = Task.Run(() => HostAsync(config, logger, deviceId,
                        moduleId, args, verbose, false), cts.Token);

                    Console.WriteLine("Press key to cancel...");
                    Console.ReadKey();

                    logger.Information("Server exiting - tear down publisher...");
                    cts.Cancel();

                    await host;
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Add or get module identity
        /// </summary>
        private static async Task<ConnectionString> AddOrGetAsync(IIoTHubConfig config,
            string deviceId, string moduleId, ILogger logger) {
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);
            try {
                await registry.CreateOrUpdateAsync(new DeviceTwinModel {
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
                await registry.CreateOrUpdateAsync(new DeviceTwinModel {
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
                EndpointUrl = "opc.tcp://" + Utils.GetHostName() +
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
