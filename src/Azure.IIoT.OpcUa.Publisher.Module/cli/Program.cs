// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Sample;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Autofac;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Models;
    using Furly.Exceptions;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
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
            var checkTrust = false;
            var withServer = false;
            string deviceId = null, moduleId = null;

            var loggerFactory = Log.ConsoleFactory();
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("Publisher module command line interface.");
            var configuration = new ConfigurationBuilder()
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddFromKeyVault(ConfigurationProviderPriority.Lowest)
                .Build();
            var cs = configuration.GetValue<string>("PCS_IOTHUB_CONNSTRING", null);
            if (string.IsNullOrEmpty(cs))
            {
                cs = configuration.GetValue<string>("_HUB_CS", null);
            }
            var instances = 1;
            string publishProfile = null;
            TimeSpan? disconnectInterval = null;
            TimeSpan? reconnectDelay = null;
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
                        case "-N":
                        case "--instances":
                            i++;
                            if (i < args.Length && int.TryParse(args[i], out instances))
                            {
                                break;
                            }
                            throw new ArgumentException(
                                "Missing argument for --instances");
                        case "-T":
                        case "--only-trusted":
                            checkTrust = true;
                            break;
                        case "-S":
                        case "--with-server":
                            withServer = true;
                            break;
                        case "-D":
                        case "--disconnect-interval-sec":
                            i++;
                            if (i < args.Length)
                            {
                                disconnectInterval = TimeSpan.FromSeconds(
                                    int.Parse(args[i], CultureInfo.InvariantCulture));
                                break;
                            }
                            throw new ArgumentException(
                                "Missing argument for --disconnect-interval-sec");
                        case "-R":
                        case "--reconnect-delay-sec":
                            i++;
                            if (i < args.Length)
                            {
                                reconnectDelay = TimeSpan.FromSeconds(
                                    int.Parse(args[i], CultureInfo.InvariantCulture));
                                break;
                            }
                            throw new ArgumentException(
                                "Missing argument for --reconnect-delay-sec");
                        case "-P":
                        case "--publish-profile":
                            i++;
                            if (i < args.Length)
                            {
                                publishProfile = args[i];
                                withServer = true;
                                break;
                            }
                            throw new ArgumentException(
                                "Missing argument for --publish-profile");
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
                    tasks.Add(HostAsync(cs, loggerFactory, deviceId + "_" + i, moduleId, args, !checkTrust));
                }
                if (!withServer)
                {
                    tasks.Add(HostAsync(cs, loggerFactory, deviceId, moduleId, args, !checkTrust));
                }
                else
                {
                    tasks.Add(WithServerAsync(cs, loggerFactory, deviceId, moduleId, args, publishProfile,
                        !checkTrust, disconnectInterval, reconnectDelay));
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
        /// <param name="connectionString"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="args"></param>
        /// <param name="acceptAll"></param>
        private static async Task HostAsync(string connectionString, ILoggerFactory loggerFactory,
            string deviceId, string moduleId, string[] args, bool acceptAll = false)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Create or retrieve connection string for {DeviceId} {ModuleId}...",
                deviceId, moduleId);

            ConnectionString cs;
            while (true)
            {
                try
                {
                    cs = await AddOrGetAsync(connectionString, deviceId, moduleId,
                        logger).ConfigureAwait(false);

                    logger.LogInformation("Retrieved connection string for {DeviceId} {ModuleId}.",
                       deviceId, moduleId);
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to get connection string for {DeviceId} {ModuleId}...",
                        deviceId, moduleId);
                }
            }

            Run(logger, deviceId, moduleId, args, acceptAll, cs);

            static void Run(ILogger logger, string deviceId, string moduleId, string[] args,
                bool acceptAll, ConnectionString cs)
            {
                logger.LogInformation("Starting publisher module {DeviceId} {ModuleId}...",
                    deviceId, moduleId);
                var arguments = args.ToList();
                arguments.Add($"--ec={cs}");
                if (acceptAll)
                {
                    arguments.Add("--aa");
                }
                Publisher.Module.Program.Main(arguments.ToArray());
                logger.LogInformation("Publisher module {DeviceId} {ModuleId} exited.",
                    deviceId, moduleId);
            }
        }

        /// <summary>
        /// setup publishing from sample server
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="args"></param>
        /// <param name="publishProfile"></param>
        /// <param name="acceptAll"></param>
        /// <param name="disconnectInterval"></param>
        /// <param name="reconnectDelay"></param>
        private static async Task WithServerAsync(string connectionString, ILoggerFactory loggerFactory,
            string deviceId, string moduleId, string[] args, string publishProfile = null, bool acceptAll = false,
            TimeSpan? disconnectInterval = null, TimeSpan? reconnectDelay = null)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            try
            {
                // Start test server
                using (var server = new ServerWrapper(loggerFactory, disconnectInterval, reconnectDelay))
                {
                    if (publishProfile != null)
                    {
                        var publishedNodesFile = $"./Profiles/{publishProfile}.json";
                        if (File.Exists(publishedNodesFile))
                        {
                            var publishedNodesFilePath = Path.GetTempFileName();

                            File.WriteAllText(publishedNodesFilePath,
                                File.ReadAllText(publishedNodesFile).Replace("{{Port}}",
                                server.Port.ToString(CultureInfo.InvariantCulture),
                                StringComparison.Ordinal));

                            args = args.Concat(new[]
                            {
                                $"--pf={publishedNodesFilePath}",
                                "--ln"
                            }).ToArray();
                        }
                    }

                    // Start publisher module
                    await HostAsync(connectionString, loggerFactory, deviceId, moduleId,
                        args, acceptAll).ConfigureAwait(false);

                    logger.LogInformation("Server exiting...");
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Add or get module identity
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="logger"></param>
        private static async Task<ConnectionString> AddOrGetAsync(string connectionString,
            string deviceId, string moduleId, ILogger logger)
        {
            var builder = new ContainerBuilder();
            builder.AddIoTHubServiceClient();
            builder.Configure<IoTHubServiceOptions>(
                options => options.ConnectionString = connectionString);
            builder.AddDefaultJsonSerializer();
            builder.AddLogging();
            using var container = builder.Build();

            var registry = container.Resolve<IIoTHubTwinServices>();

            // Create iot edge gateway
            try
            {
                await registry.CreateOrUpdateAsync(new DeviceTwinModel
                {
                    Id = deviceId,
                    Tags = new Dictionary<string, VariantValue>
                    {
                        [Constants.TwinPropertyTypeKey] = Constants.EntityTypeGateway
                    },
                    IotEdge = true
                }, false).ConfigureAwait(false);
            }
            catch (ResourceConflictException)
            {
                logger.LogInformation("IoT Edge device {DeviceId} already exists.", deviceId);
            }

            // Create publisher module
            try
            {
                await registry.CreateOrUpdateAsync(new DeviceTwinModel
                {
                    Id = deviceId,
                    ModuleId = moduleId
                }, false, default).ConfigureAwait(false);
            }
            catch (ResourceConflictException)
            {
                logger.LogInformation("Publisher {ModuleId} already exists...", moduleId);
            }
            var module = await registry.GetRegistrationAsync(deviceId, moduleId).ConfigureAwait(false);
            return ConnectionString.CreateModuleConnectionString(registry.HostName,
                deviceId, moduleId, module.PrimaryKey);
        }

        /// <summary>
        /// Wraps server and disposes after use
        /// </summary>
        private sealed class ServerWrapper : IDisposable
        {
            public int Port { get; } = 51257;

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="disconnectInterval"></param>
            /// <param name="reconnectDelay"></param>
            public ServerWrapper(ILoggerFactory logger, TimeSpan? disconnectInterval,
                TimeSpan? reconnectDelay)
            {
                _cts = new CancellationTokenSource();
                _server = RunSampleServerAsync(logger, Port,
                    disconnectInterval, reconnectDelay, _cts.Token);
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
            /// <param name="loggerFactory"></param>
            /// <param name="port"></param>
            /// <param name="disconnectInterval"></param>
            /// <param name="reconnectDelay"></param>
            /// <param name="ct"></param>
            private static async Task RunSampleServerAsync(ILoggerFactory loggerFactory,
                int port, TimeSpan? disconnectInterval, TimeSpan? reconnectDelay, CancellationToken ct)
            {
                var disconnectTimeout = disconnectInterval ?? Timeout.InfiniteTimeSpan;
                var reconnectTimeout = reconnectDelay ?? TimeSpan.FromSeconds(5);
                var logger = loggerFactory.CreateLogger<ServerWrapper>();
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        using (var server = new ServerConsoleHost(
                            new ServerFactory(loggerFactory.CreateLogger<ServerFactory>())
                            {
                                LogStatus = false
                            }, loggerFactory.CreateLogger<ServerConsoleHost>())
                        {
                            PkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "server"),
                            AutoAccept = true
                        })
                        {
                            logger.LogInformation("(Re-)Starting server...");
                            await server.StartAsync(new List<int> { port }).ConfigureAwait(false);
                            logger.LogInformation("Server (re-)started.");
                            await Task.Delay(disconnectTimeout, ct).ConfigureAwait(false);
                            logger.LogInformation("Stopping server...");
                        }

                        logger.LogInformation("Server stopped.");
                        logger.LogInformation("Restarting server in {Delay}...", reconnectTimeout);
                        await Task.Delay(reconnectTimeout, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Server ran into exception.");
                    }
                }
                logger.LogInformation("Server exited.");
            }

            private readonly CancellationTokenSource _cts;
            private readonly Task _server;
        }
    }
}
