// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Sample;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher module host process
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Main(string[] args)
        {
            var checkTrust = true;
            var withServer = false;
            var verbose = false;
            string deviceId = null, moduleId = null;

            var logger = Log.Console<Program>();

            logger.LogInformation("Publisher module command line interface.");
            var configuration = new ConfigurationBuilder()
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                // Above configuration providers will provide connection
                // details for KeyVault configuration provider.
                .AddFromKeyVault(providerPriority: ConfigurationProviderPriority.Lowest)
                .Build();
            var cs = configuration.GetValue<string>(PcsVariable.PCS_IOTHUB_CONNSTRING, null);
            if (string.IsNullOrEmpty(cs))
            {
                cs = configuration.GetValue<string>("_HUB_CS", null);
            }
            IIoTHubConfig config = null;
            var instances = 1;
            var unknownArgs = new List<string>();
            try
            {
                for (var i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-C":
                        case "--connection-string":
                            i++;
                            if (i < args.Length)
                            {
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
                            if (i < args.Length && int.TryParse(args[i], out instances))
                            {
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
                if (string.IsNullOrEmpty(cs))
                {
                    throw new ArgumentException("Missing connection string.");
                }
                if (!ConnectionString.TryParse(cs, out var connectionString))
                {
                    throw new ArgumentException("Bad connection string.");
                }
                config = connectionString.ToIoTHubConfig();

                if (deviceId == null)
                {
                    deviceId = Utils.GetHostName();
                    logger.LogInformation("Using <deviceId> '{DeviceId}'", deviceId);
                }
                if (moduleId == null)
                {
                    moduleId = "publisher";
                    logger.LogInformation("Using <moduleId> '{ModuleId}'", moduleId);
                }

                args = unknownArgs.ToArray();
            }
            catch (Exception e)
            {
                logger.LogError(
                    @"{Error}

Usage:       Azure.IIoT.OpcUa.Publisher.Module.Cli [options]

Options:
     -C
    --connection-string
             IoT Hub owner connection string to use to connect to IoT hub for
             operations on the registry.  If not provided, read from environment.

    --help
     -?
     -h      Prints out this help.
",
                    e.Message);
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (s, e) => logger.LogError(e.ExceptionObject as Exception, "Exception");

            var tasks = new List<Task>(instances);
            try
            {
                var enableEventBroker = instances == 1;
                for (var i = 1; i < instances; i++)
                {
                    tasks.Add(HostAsync(config, logger, deviceId + "_" + i, moduleId, args, verbose, !checkTrust));
                }
                if (!withServer)
                {
                    tasks.Add(HostAsync(config, logger, deviceId, moduleId, args, verbose, !checkTrust));
                }
                else
                {
                    tasks.Add(WithServerAsync(config, logger, deviceId, moduleId, args, verbose));
                }
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception");
            }
        }

        /// <summary>
        /// Host the module giving it its connection string.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="args"></param>
        /// <param name="verbose"></param>
        /// <param name="acceptAll"></param>
        private static async Task HostAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, string[] args, bool verbose = false,
            bool acceptAll = false)
        {
            logger.LogInformation("Create or retrieve connection string for {DeviceId} {ModuleId}...",
                deviceId, moduleId);

            var cs = await Retry2.WithExponentialBackoffAsync(logger,
                () => AddOrGetAsync(config, deviceId, moduleId, logger)).ConfigureAwait(false);

            Run(logger, deviceId, moduleId, args, verbose, acceptAll, cs);

            static void Run(ILogger logger, string deviceId, string moduleId, string[] args,
                bool verbose, bool acceptAll, ConnectionString cs)
            {
                logger.LogInformation("Starting publisher module {DeviceId} {ModuleId}...",
                    deviceId, moduleId);
                var arguments = args.ToList();
                arguments.Add($"--ec={cs}");
                if (acceptAll)
                {
                    arguments.Add("--aa");
                }
                Module.Program.Main(arguments.ToArray());
                logger.LogInformation("Publisher module {DeviceId} {ModuleId} exited.",
                    deviceId, moduleId);
            }
        }

        /// <summary>
        /// setup publishing from sample server
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="args"></param>
        /// <param name="verbose"></param>
        private static async Task WithServerAsync(IIoTHubConfig config, ILogger logger,
            string deviceId, string moduleId, string[] args, bool verbose = false)
        {
            try
            {
                using (var cts = new CancellationTokenSource())
                using (var server = new ServerWrapper(logger))
                { // Start test server
                    // Start publisher module
                    var host = Task.Run(() => HostAsync(config, logger, deviceId,
                        moduleId, args, verbose, false), cts.Token);

                    Console.WriteLine("Press key to cancel...");
                    Console.ReadKey();

                    logger.LogInformation("Server exiting - tear down publisher...");
                    cts.Cancel();

                    await host.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Add or get module identity
        /// </summary>
        /// <param name="config"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="logger"></param>
        private static async Task<ConnectionString> AddOrGetAsync(IIoTHubConfig config,
            string deviceId, string moduleId, ILogger logger)
        {
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonsoftJsonSerializer(), logger);
            try
            {
                await registry.CreateOrUpdateAsync(new DeviceTwinModel
                {
                    Id = deviceId,
                    Tags = new Dictionary<string, VariantValue>
                    {
                        [TwinProperty.Type] = IdentityType.Gateway
                    },
                    Capabilities = new DeviceCapabilitiesModel
                    {
                        IotEdge = true
                    }
                }, false, default).ConfigureAwait(false);
            }
            catch (ConflictingResourceException)
            {
                logger.LogInformation("Gateway {DeviceId} exists.", deviceId);
            }
            try
            {
                await registry.CreateOrUpdateAsync(new DeviceTwinModel
                {
                    Id = deviceId,
                    ModuleId = moduleId
                }, false, default).ConfigureAwait(false);
            }
            catch (ConflictingResourceException)
            {
                logger.LogInformation("Module {ModuleId} exists...", moduleId);
            }
            return await registry.GetConnectionStringAsync(deviceId, moduleId).ConfigureAwait(false);
        }

        /// <summary>
        /// Wraps server and disposes after use
        /// </summary>
        private class ServerWrapper : IDisposable
        {
            public string EndpointUrl { get; }

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="logger"></param>
            public ServerWrapper(ILogger logger)
            {
                _cts = new CancellationTokenSource();
                _server = RunSampleServerAsync(logger, _cts.Token);
                EndpointUrl = "opc.tcp://" + Utils.GetHostName() +
                    ":51210/UA/SampleServer";
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _cts.Cancel();
                _server.Wait();
                _cts.Dispose();
            }

            /// <summary>
            /// Run server until cancelled
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="ct"></param>
            private static async Task RunSampleServerAsync(ILogger logger, CancellationToken ct)
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetResult(true));
                using (var server = new ServerConsoleHost(new ServerFactory(logger)
                {
                    LogStatus = false
                }, logger)
                {
                    AutoAccept = true
                })
                {
                    logger.LogInformation("Starting server.");
                    await server.StartAsync(new List<int> { 51210 }).ConfigureAwait(false);
                    logger.LogInformation("Server started.");
                    await tcs.Task.ConfigureAwait(false);
                    logger.LogInformation("Server exited.");
                }
            }

            private readonly CancellationTokenSource _cts;
            private readonly Task _server;
        }
    }
}
