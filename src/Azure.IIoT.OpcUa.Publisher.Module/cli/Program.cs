// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
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
    using Nito.AsyncEx;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net;

    /// <summary>
    /// Publisher module host process
    /// </summary>
    public static class Program
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
            string? deviceId = null, moduleId = null;
            int? reverseConnectPort = null;

            var loggerFactory = Log.ConsoleFactory();
            var logger = loggerFactory.CreateLogger<PublisherModule>();

            logger.LogInformation("Publisher module command line interface.");
            var instances = 1;
            string? connectionString = null;
            string? publishProfile = null;
            string? publishedNodesFilePath = null;
            var useNullTransport = false;
            string? dumpMessages = null;
            string? dumpMessagesOutput = null;
            var scaleunits = 0u;
            var unknownArgs = new List<string>();
            try
            {
                for (var i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--dumpprofiles":
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine("The following messaging profiles are supported (selected with --mm and --me):");
                            Console.WriteLine();
                            Console.Write(MessagingProfile.GetAllAsMarkdownTable());
                            return;
                        case "-C":
                        case "--connection-string":
                            i++;
                            if (i < args.Length)
                            {
                                connectionString = args[i];
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
                            throw new ArgumentException("Missing argument for --instances");
                        case "-U":
                        case "--scale":
                            i++;
                            if (i < args.Length && uint.TryParse(args[i], out scaleunits))
                            {
                                withServer = true;
                                break;
                            }
                            throw new ArgumentException("Missing argument for --scale");
                        case "--reverse-connect":
                            reverseConnectPort = 4840;
                            break;
                        case "--port":
                            i++;
                            if (i < args.Length && int.TryParse(args[i], out var port))
                            {
                                reverseConnectPort = port;
                                break;
                            }
                            throw new ArgumentException("Missing argument for --port");
                        case "-T":
                        case "--only-trusted":
                            checkTrust = true;
                            break;
                        case "-X":
                        case "--out-null":
                            useNullTransport = true;
                            break;
                        case "-S":
                        case "--with-server":
                            withServer = true;
                            break;
                        case "-D":
                        case "--dump-messages":
                            i++;
                            useNullTransport = true;
                            if (i < args.Length)
                            {
                                dumpMessages = args[i];
                                break;
                            }
                            throw new ArgumentException("Missing argument for --dump-messages");
                        case "-O":
                        case "--dump-output":
                            i++;
                            if (i < args.Length)
                            {
                                dumpMessagesOutput = args[i];
                                break;
                            }
                            throw new ArgumentException("Missing argument for --dump-output");
                        case "-P":
                        case "--publish-profile":
                            i++;
                            if (i < args.Length)
                            {
                                publishProfile = args[i];
                                withServer = true;
                                break;
                            }
                            throw new ArgumentException("Missing argument for --publish-profile");
                        case "--pnjson":
                            i++;
                            if (i < args.Length)
                            {
                                publishedNodesFilePath = args[i];
                                break;
                            }
                            throw new ArgumentException("Missing argument for --pnjson");
                        case "--":
                            break;
                        default:
                            unknownArgs.Add(args[i]);
                            break;
                    }
                }

                if (string.IsNullOrEmpty(connectionString) && !useNullTransport)
                {
                    try
                    {
                        var configuration = new ConfigurationBuilder()
                            .AddFromDotEnvFile()
                            .AddEnvironmentVariables()
                            .AddFromKeyVault(ConfigurationProviderPriority.Lowest)
                            .Build();
                        connectionString = configuration.GetValue("PCS_IOTHUB_CONNSTRING", string.Empty);
                        if (string.IsNullOrEmpty(connectionString))
                        {
                            connectionString = configuration.GetValue("_HUB_CS", string.Empty);
                        }
                        if (!string.IsNullOrEmpty(connectionString) &&
                            !ConnectionString.TryParse(connectionString, out _))
                        {
                            throw new ArgumentException("Bad connection string configured.");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogInformation("Error {Error}: Missing connection string - continue...",
                            e.Message);
                    }
                }

                deviceId = Dns.GetHostName().ToUpperInvariant();
                logger.LogInformation("Using <deviceId> '{DeviceId}'", deviceId);
                moduleId = "publisher";
                logger.LogInformation("Using <moduleId> '{ModuleId}'", moduleId);

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

            AppDomain.CurrentDomain.UnhandledException +=
                (s, e) => logger.LogError(e.ExceptionObject as Exception, "Exception");

            using var cts = new CancellationTokenSource();
            Task hostingTask;
            try
            {
                if (dumpMessages != null)
                {
                    hostingTask = DumpMessagesAsync(dumpMessages, publishProfile, loggerFactory,
                        TimeSpan.FromMinutes(2), scaleunits, dumpMessagesOutput, args, cts.Token);
                }
                else if (!withServer)
                {
                    hostingTask = HostAsync(connectionString, loggerFactory,
                        deviceId, moduleId, args, reverseConnectPort, !checkTrust,
                        publishedNodesFilePath, cts.Token);
                }
                else
                {
                    hostingTask = WithServerAsync(connectionString, loggerFactory, deviceId, moduleId, args,
                        publishProfile, scaleunits, !checkTrust, reverseConnectPort, cts.Token);
                }

                while (!cts.Token.IsCancellationRequested)
                {
                    var key = Console.ReadKey();
                    switch (key.KeyChar)
                    {
                        case 'X':
                        case 'x':
                            Console.WriteLine("Exiting...");
                            cts.Cancel();
                            break;
                        case 'P':
                        case 'p':
                            Console.WriteLine("Restarting publisher...");
                            _restartPublisher.Set();
                            break;
                        case 'S':
                        case 's':
                            Console.WriteLine("Restarting server...");
                            _restartServer.Set();
                            break;
                    }
                }

                // Wait for hosting task to exit
                hostingTask.GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception");
            }
        }

        private static readonly AsyncAutoResetEvent _restartServer = new(false);
        private static readonly AsyncAutoResetEvent _restartPublisher = new(false);

        /// <summary>
        /// Host the module with connection string loaded from iot hub
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="args"></param>
        /// <param name="reverseConnectPort"></param>
        /// <param name="acceptAll"></param>
        /// <param name="publishedNodesFilePath"></param>
        /// <param name="ct"></param>
        private static async Task HostAsync(string? connectionString, ILoggerFactory loggerFactory,
            string deviceId, string moduleId, string[] args, int? reverseConnectPort,
            bool acceptAll, string? publishedNodesFilePath = null, CancellationToken ct = default)
        {
            var logger = loggerFactory.CreateLogger<PublisherModule>();
            logger.LogInformation("Create or retrieve connection string for {DeviceId} {ModuleId}...",
                deviceId, moduleId);

            ConnectionString? cs = null;
            if (connectionString != null)
            {
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
            }

            while (!ct.IsCancellationRequested)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                var running = RunAsync(logger, deviceId, moduleId, args, acceptAll, cs,
                    reverseConnectPort, publishedNodesFilePath, cts.Token);

                Console.WriteLine("Publisher running (Press P to restart)...");
                await _restartPublisher.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    await cts.CancelAsync().ConfigureAwait(false);
                    await running.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }

            static async Task RunAsync(ILogger logger, string deviceId, string moduleId, string[] args,
                bool acceptAll, ConnectionString? cs, int? reverseConnectPort, string? publishedNodesFilePath,
                CancellationToken ct)
            {
                logger.LogInformation("Starting publisher module {DeviceId} {ModuleId}...",
                    deviceId, moduleId);
                var arguments = args.ToList();

                if (publishedNodesFilePath != null)
                {
                    arguments.Add($"--pf={publishedNodesFilePath}");
                }
                if (!args.Any(a => a.StartsWith("-t=", StringComparison.OrdinalIgnoreCase)))
                {
                    if (cs != null)
                    {
                        arguments.Add($"--ec={cs}");
                    }
                    else
                    {
                        arguments.Add("-t=Null");
                    }
                }
                arguments.Add("--cl=5"); // enable 5 second client linger
                if (acceptAll)
                {
                    // Accept all certificates
                    arguments.Add("--aa");
                }
                if (reverseConnectPort != null)
                {
                    // Since we started the server with reverse connect
                    // default the profile to use reverse connect
                    arguments.Add("--urc");
                }
                await Publisher.Module.Program.RunAsync(arguments.ToArray(), ct).ConfigureAwait(false);
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
        /// <param name="scaleunits"></param>
        /// <param name="acceptAll"></param>
        /// <param name="reverseConnectPort"></param>
        /// <param name="ct"></param>
        private static async Task WithServerAsync(string? connectionString, ILoggerFactory loggerFactory,
            string deviceId, string moduleId, string[] args, string? publishProfile, uint scaleunits, bool acceptAll,
            int? reverseConnectPort, CancellationToken ct)
        {
            try
            {
                // Start test server
                using (var server = new ServerWrapper(scaleunits, loggerFactory, reverseConnectPort))
                {
                    var publishedNodesFilePath = await LoadPnJson(server, publishProfile,
                        $"opc.tcp://localhost:{server.Port}/UA/SampleServer", ct).ConfigureAwait(false);

                    // Start publisher module
                    await HostAsync(connectionString, loggerFactory, deviceId, moduleId,
                        args, reverseConnectPort, acceptAll, publishedNodesFilePath, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Dump messages
        /// </summary>
        /// <param name="messageMode"></param>
        /// <param name="publishProfile"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="duration"></param>
        /// <param name="scaleunits"></param>
        /// <param name="dumpMessagesOutput"></param>
        /// <param name="args"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task DumpMessagesAsync(string messageMode, string? publishProfile,
            ILoggerFactory loggerFactory, TimeSpan duration, uint scaleunits, string? dumpMessagesOutput,
            string[] args, CancellationToken ct)
        {
            try
            {
                // Dump one message encoding at a time
                var rootFolder = Path.Combine(dumpMessagesOutput ?? ".", "dump");
                foreach (var messageProfile in MessagingProfile.Supported)
                {
                    if (messageProfile.MessageEncoding.HasFlag(MessageEncoding.IsGzipCompressed))
                    {
                        // No need to dump gzip
                        continue;
                    }

                    if (messageMode != "all" &&
                        !messageProfile.MessagingMode.ToString().Equals(
                            messageMode, StringComparison.OrdinalIgnoreCase) &&
                        !messageProfile.MessageEncoding.ToString().Equals(
                            messageMode, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Skipping {messageProfile}...");
                        continue;
                    }

                    var outputFolder = Path.Combine(rootFolder, messageProfile.MessagingMode.ToString(),
                        messageProfile.MessageEncoding.ToString());
                    if (Directory.Exists(outputFolder) && Directory.EnumerateFiles(outputFolder).Any())
                    {
                        continue;
                    }
                    Directory.CreateDirectory(outputFolder);
                    await DumpPublishingProfiles(outputFolder, messageProfile, publishProfile).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }

            // Dump message profile for all publishing profiles
            async Task DumpPublishingProfiles(string rootFolder, MessagingProfile messageProfile, string? profile)
            {
                foreach (var publishProfile in Directory.EnumerateFiles("./Profiles", "*.json"))
                {
                    var publishProfileName = Path.GetFileNameWithoutExtension(publishProfile);
                    if (profile == null &&
                       (publishProfileName.StartsWith("Unified", StringComparison.OrdinalIgnoreCase) ||
                        publishProfileName.StartsWith("Empty", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    if (profile != null && !publishProfileName.Equals(profile, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var logger = loggerFactory.CreateLogger(publishProfileName);
                    var outputFolder = Path.Combine(rootFolder, publishProfileName);
                    if (Directory.Exists(outputFolder) && Directory.EnumerateFiles(outputFolder).Any())
                    {
                        continue;
                    }
                    Directory.CreateDirectory(outputFolder);
                    await DumpMessagesForDuration(outputFolder, publishProfile,
                            messageProfile, args).ConfigureAwait(false);
                }
            }

            async Task DumpMessagesForDuration(string outputFolder, string publishProfile,
                MessagingProfile messageProfile, string[] args)
            {
                using var runtime = new CancellationTokenSource(duration);
                try
                {
                    using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                        ct, runtime.Token);
                    var name = Path.GetFileNameWithoutExtension(publishProfile);
                    Console.Title = $"Dumping {messageProfile} for {name}...";
                    await RunAsync(loggerFactory, publishProfile, messageProfile,
                        outputFolder, scaleunits, args, linkedToken.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (runtime.IsCancellationRequested) { }
            }

            static async Task RunAsync(ILoggerFactory loggerFactory, string publishProfile,
                MessagingProfile messageProfile, string outputFolder, uint scaleunits, string[] args,
                CancellationToken ct)
            {
                // Start test server
                using (var server = new ServerWrapper(scaleunits, loggerFactory, null))
                {
                    var name = Path.GetFileNameWithoutExtension(publishProfile);
                    var publishedNodesFilePath = await LoadPnJson(server, name,
                        $"opc.tcp://localhost:{server.Port}/UA/SampleServer", ct).ConfigureAwait(false);
                    if (publishedNodesFilePath == null)
                    {
                        return;
                    }

                    //
                    // Check whether the profile overrides the messaging mode, then set it to the desired
                    // one regardless of whether it will work or not
                    //
                    var check = await File.ReadAllTextAsync(publishedNodesFilePath, ct).ConfigureAwait(false);
                    if (check.Contains("\"MessagingMode\":", StringComparison.InvariantCulture) &&
                        !check.Contains($"\"MessagingMode\": \"{messageProfile.MessagingMode}\"",
                        StringComparison.InvariantCulture))
                    {
                        check = ReplacePropertyValue(check, "MessagingMode", messageProfile.MessagingMode.ToString());
                        await File.WriteAllTextAsync(publishedNodesFilePath, check, ct).ConfigureAwait(false);
                    }

                    var arguments = new HashSet<string>
                    {
                        "-c",
                        "--ps",
                        $"--pf={publishedNodesFilePath}",
                        $"--me={messageProfile.MessageEncoding}",
                        $"--mm={messageProfile.MessagingMode}",
                        $"--ttt={name}/{{WriterGroup}}",
                        $"--mdt={name}/{{WriterGroup}}",
                        "-t=FileSystem",
                        $"-o={outputFolder}",
                        "--aa"
                    };
                    args.ForEach(a => arguments.Add(a));
                    await Publisher.Module.Program.RunAsync(arguments.ToArray(), ct).ConfigureAwait(false);
                }
            }
        }

        private static async Task<string?> LoadPnJson(ServerWrapper server, string? publishProfile,
            string endpointUrl, CancellationToken ct)
        {
            const string publishedNodesFilePath = "profile.json";
            if (!string.IsNullOrEmpty(publishProfile))
            {
                var publishedNodesFile = $"./Profiles/{publishProfile}.json";
                if (!File.Exists(publishedNodesFile))
                {
                    throw new ArgumentException($"Profile {publishProfile} does not exist");
                }
                await File.WriteAllTextAsync(publishedNodesFilePath,
                    (await File.ReadAllTextAsync(publishedNodesFile, ct).ConfigureAwait(false))
                    .Replace("{{EndpointUrl}}", endpointUrl,
                        StringComparison.Ordinal), ct).ConfigureAwait(false);

                return publishedNodesFilePath;
            }

            var testServer = await server.Server.Task.ConfigureAwait(false);
            if (testServer?.PublishedNodesJson != null)
            {
                var json = testServer.PublishedNodesJson.Replace("{{EndpointUrl}}",
                    endpointUrl, StringComparison.Ordinal);

                // var entries = JsonSerializer.Deserialize<PublishedNodesEntryModel[]>(server.PublishedNodesJson);

                await File.WriteAllTextAsync(publishedNodesFilePath, json, ct).ConfigureAwait(false);
                return publishedNodesFilePath;
            }
            return null;
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
            var container = builder.Build();
            await using (container.ConfigureAwait(false))
            {
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
                    deviceId, moduleId, module.PrimaryKey!);
            }
        }

        /// <summary>
        /// Wraps server and disposes after use
        /// </summary>
        private sealed class ServerWrapper : IDisposable
        {
            public int Port { get; } = 61457;

            public TaskCompletionSource<ITestServer?> Server { get; private set; } = new();

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="scaleunits"></param>
            /// <param name="logger"></param>
            /// <param name="reverseConnectPort"></param>
            public ServerWrapper(uint scaleunits, ILoggerFactory logger, int? reverseConnectPort)
            {
                _cts = new CancellationTokenSource();
                _server = RunSampleServerAsync(scaleunits, logger, Port, reverseConnectPort, _cts.Token);
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
            /// <param name="scaleunits"></param>
            /// <param name="loggerFactory"></param>
            /// <param name="port"></param>
            /// <param name="reverseConnectPort"></param>
            /// <param name="ct"></param>
            private async Task RunSampleServerAsync(uint scaleunits,
                ILoggerFactory loggerFactory, int port, int? reverseConnectPort, CancellationToken ct)
            {
                var logger = loggerFactory.CreateLogger<ServerWrapper>();
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        using (var server = new ServerConsoleHost(
                            new ServerFactory(loggerFactory.CreateLogger<ServerFactory>(), scaleunits)
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
                            Server.SetResult(server.TestServer);
                            logger.LogInformation("Server (re-)started (Press S to kill).");
                            if (reverseConnectPort != null)
                            {
                                logger.LogInformation("Reverse connect to client...");
                                await server.AddReverseConnectionAsync(
                                    new Uri($"opc.tcp://localhost:{reverseConnectPort}"),
                                    1).ConfigureAwait(false);
                            }
                            await _restartServer.WaitAsync(ct).ConfigureAwait(false);
                            logger.LogInformation("Stopping server...");
                            Server = new TaskCompletionSource<ITestServer?>();
                        }

                        logger.LogInformation("Server stopped.");
                        logger.LogInformation("Waiting to restarting server (Press S to restart)...");
                        await _restartServer.WaitAsync(ct).ConfigureAwait(false);
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

        public static string ReplacePropertyValue(string json, string propertyName, string newValue)
        {
            var pattern = $"\"{propertyName}\"\\s*:\\s*\"[^\"]*\"";
            var replacement = $"\"{propertyName}\": \"{newValue}\"";
            return Regex.Replace(json, pattern, replacement);
        }
    }
}
