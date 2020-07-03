// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// OPC Twin module cli
    /// </summary>
    public class Program {

        private enum Op {
            None,
            Host,
            Add,
            Get,
            Reset,
            Delete,
            List,

            ResetAll,
            Cleanup,
            CleanupAll
        }

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main(string[] args) {
            var op = Op.None;
            bool verbose = false;
            string deviceId = null, moduleId = null;
            Console.WriteLine("Twin module command line interface.");
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
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "-v":
                        case "--verbose":
                            verbose = true;
                            break;
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
                        default:
                            if (op != Op.None) {
                                throw new ArgumentException(
                                    "Operations are mutually exclusive");
                            }
                            switch (args[i]) {
                                case "--list":
                                    op = Op.List;
                                    break;
                                case "--add":
                                    op = Op.Add;
                                    break;
                                case "--get":
                                    op = Op.Get;
                                    break;
                                case "--reset":
                                    op = Op.Reset;
                                    break;
                                case "--delete":
                                    op = Op.Delete;
                                    break;
                                case "--host":
                                    op = Op.Host;
                                    break;
                                case "--delete-all":
                                    op = Op.CleanupAll;
                                    break;
                                case "--reset-all":
                                    op = Op.ResetAll;
                                    break;
                                case "--cleanup":
                                    op = Op.Cleanup;
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown {args[i]}");
                            }
                            // Try parse ids
                            switch (op) {
                                case Op.Add:
                                case Op.Host:
                                case Op.Get:
                                case Op.Reset:
                                case Op.Delete:
                                    i++;
                                    if (i < args.Length) {
                                        deviceId = args[i];
                                        i++;
                                        if (i < args.Length) {
                                            moduleId = args[i];
                                            break;
                                        }
                                    }
                                    break;
                            }
                            break;
                    }

                }
                if (op == Op.None) {
                    op = Op.Host;
                }
                if (string.IsNullOrEmpty(cs)) {
                    throw new ArgumentException("Missing connection string.");
                }
                if (!ConnectionString.TryParse(cs, out var connectionString)) {
                    throw new ArgumentException("Bad connection string.");
                }
                config = connectionString.ToIoTHubConfig();

                switch (op) {
                    case Op.Get:
                    case Op.Reset:
                    case Op.Delete:
                        if (deviceId == null || moduleId == null) {
                            throw new ArgumentException(
                                "Missing arguments for delete/reset/get command.");
                        }
                        break;
                    case Op.Add:
                    case Op.Host:
                        if (deviceId == null) {
                            deviceId = Utils.GetHostName();
                            Console.WriteLine($"Using <deviceId> '{deviceId}'");
                        }
                        if (moduleId == null) {
                            moduleId = "twin";
                            Console.WriteLine($"Using <moduleId> '{moduleId}'");
                        }
                        break;
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Usage:       Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli [options] operation [args]

Operations (Mutually exclusive):

    --list
             List all registered supervisor module identities.

    --add <deviceId> <moduleId>
             Add twin module with given device id and module id to device registry.

    --get <deviceId> <moduleId>
             Get twin module connection string from device registry.

    --host <deviceId> <moduleId>
             Host the twin module under the given device id and module id.

    --reset <deviceId> <moduleId>
             Reset registered module identity twin properties and tags.

    --delete <deviceId> <moduleId>
             Delete registered module identity.

    --reset-all
             Clear all registered supervisor module identities.

    --cleanup
             Clear entire Registry content.

    --delete-all
             Cleanup and delete all supervisor identities.

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
                switch (op) {
                    case Op.Host:
                        HostAsync(config, deviceId, moduleId, verbose).Wait();
                        break;
                    case Op.Add:
                        AddAsync(config, deviceId, moduleId).Wait();
                        break;
                    case Op.Get:
                        GetAsync(config, deviceId, moduleId).Wait();
                        break;
                    case Op.Reset:
                        ResetAsync(config, deviceId, moduleId).Wait();
                        break;
                    case Op.Delete:
                        DeleteAsync(config, deviceId, moduleId).Wait();
                        break;
                    case Op.ResetAll:
                        ResetAllAsync(config).Wait();
                        break;
                    case Op.List:
                        ListAsync(config).Wait();
                        break;
                    case Op.Cleanup:
                        CleanupAsync(config, false).Wait();
                        break;
                    case Op.CleanupAll:
                        CleanupAsync(config, true).Wait();
                        break;
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Host the module giving it its connection string.
        /// </summary>
        private static async Task HostAsync(IIoTHubConfig config,
            string deviceId, string moduleId, bool verbose = false) {
            Console.WriteLine("Create or retrieve connection string...");
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var cs = await Retry.WithExponentialBackoff(logger,
                () => AddOrGetAsync(config, deviceId, moduleId));

            // Hook event source
            using (var broker = new EventSourceBroker()) {
                LogControl.Level.MinimumLevel = verbose ?
                    LogEventLevel.Verbose : LogEventLevel.Information;

                Console.WriteLine("Starting twin module...");
                broker.Subscribe(IoTSdkLogger.EventSource, new IoTSdkLogger(logger));
                var arguments = new List<string> {
                    $"EdgeHubConnectionString={cs}"
                };
                Twin.Program.Main(arguments.ToArray());
                Console.WriteLine("Twin module exited.");
            }
        }

        /// <summary>
        /// Add supervisor
        /// </summary>
        private static async Task AddAsync(IIoTHubConfig config,
            string deviceId, string moduleId) {
            var cs = await AddOrGetAsync(config, deviceId, moduleId);
            Console.WriteLine(cs);
        }

        /// <summary>
        /// Get module connection string
        /// </summary>
        private static async Task GetAsync(
            IIoTHubConfig config, string deviceId, string moduleId) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);
            var cs = await registry.GetConnectionStringAsync(deviceId, moduleId);
            Console.WriteLine(cs);
        }

        /// <summary>
        /// Reset supervisor
        /// </summary>
        private static async Task ResetAsync(IIoTHubConfig config,
            string deviceId, string moduleId) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);
            await ResetAsync(registry, await registry.GetAsync(deviceId, moduleId,
                CancellationToken.None));
        }

        /// <summary>
        /// Delete supervisor
        /// </summary>
        private static async Task DeleteAsync(IIoTHubConfig config,
            string deviceId, string moduleId) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);
            await registry.DeleteAsync(deviceId, moduleId, null, CancellationToken.None);
        }

        /// <summary>
        /// List all twin module identities
        /// </summary>
        private static async Task ListAsync(IIoTHubConfig config) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Supervisor}'";
            var supers = await registry.QueryAllDeviceTwinsAsync(query);
            foreach (var item in supers) {
                Console.WriteLine($"{item.Id} {item.ModuleId}");
            }
        }

        /// <summary>
        /// Reset all supervisor tags and properties
        /// </summary>
        private static async Task ResetAllAsync(IIoTHubConfig config) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.Type} = '{IdentityType.Supervisor}'";
            var supers = await registry.QueryAllDeviceTwinsAsync(query);
            foreach (var item in supers) {
                Console.WriteLine($"Resetting {item.Id} {item.ModuleId ?? ""}");
                await ResetAsync(registry, item);
            }
        }

        /// <summary>
        /// Clear registry
        /// </summary>
        private static async Task CleanupAsync(IIoTHubConfig config,
            bool includeSupervisors) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, new NewtonSoftJsonSerializer(), logger);
            var result = await registry.QueryAllDeviceTwinsAsync(
                "SELECT * from devices where IS_DEFINED(tags.DeviceType)");
            foreach (var item in result) {
                Console.WriteLine($"Deleting {item.Id} {item.ModuleId ?? ""}");
                await registry.DeleteAsync(item.Id, item.ModuleId, null,
                    CancellationToken.None);
            }
            if (!includeSupervisors) {
                return;
            }
            var query = "SELECT * FROM devices.modules WHERE " +
             $"properties.reported.{TwinProperty.Type} = '{IdentityType.Supervisor}'";
            var supers = await registry.QueryAllDeviceTwinsAsync(query);
            foreach (var item in supers) {
                Console.WriteLine($"Deleting {item.Id} {item.ModuleId ?? ""}");
                await registry.DeleteAsync(item.Id, item.ModuleId, null,
                    CancellationToken.None);
            }
        }

        /// <summary>
        /// Reset supervisor
        /// </summary>
        private static async Task ResetAsync(IoTHubServiceHttpClient registry,
            DeviceTwinModel item) {
            if (item.Tags != null) {
                foreach (var tag in item.Tags.Keys.ToList()) {
                    item.Tags[tag] = null;
                }
            }
            if (item.Properties?.Desired != null) {
                foreach (var property in item.Properties.Desired.Keys.ToList()) {
                    if (property.StartsWith('$')) {
                        continue;
                    }
                    item.Properties.Desired[property] = null;
                }
            }
            if (item.Properties?.Reported != null) {
                foreach (var property in item.Properties.Reported.Keys.ToList()) {
                    if (property.StartsWith('$')) {
                        continue;
                    }
                    if (!item.Properties.Desired.ContainsKey(property)) {
                        item.Properties.Desired.Add(property, null);
                    }
                }
            }
            await registry.CreateOrUpdateAsync(item, true, CancellationToken.None);
        }

        /// <summary>
        /// Add or get module identity
        /// </summary>
        private static async Task<ConnectionString> AddOrGetAsync(IIoTHubConfig config,
            string deviceId, string moduleId) {
            var logger = ConsoleLogger.Create(LogEventLevel.Error);
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
