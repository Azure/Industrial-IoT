// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Simple registry tool
    /// </summary>
    public class Program {

        public const string kTypeProp = "__type__"; // TODO: Consolidate as common constant

        enum Op {
            None,
            AddSupervisor,
            ClearSupervisors,
            ClearEntireRegistry,
            GetModuleConnectionString
        }

        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main(string[] args) {
            var op = Op.None;
            string deviceId = null, moduleId = null;
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables().Build();
            var cs = Environment.GetEnvironmentVariable("PCS_IOTHUB_CONNSTRING");
            if (string.IsNullOrEmpty(cs)) {
                cs = Environment.GetEnvironmentVariable("_HUB_CS");
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
                        case "--add":
                        case "--get":
                            if (op != Op.None) {
                                throw new ArgumentException(
                                    "Operations are mutually exclusive");
                            }
                            op = args[i] == "--get" ?
                                Op.GetModuleConnectionString : Op.AddSupervisor;
                            i++;
                            if (i < args.Length) {
                                deviceId = args[i];
                                i++;
                                if (i < args.Length) {
                                    moduleId = args[i];
                                    break;
                                }
                            }
                            throw new ArgumentException(
                                "Missing arguments for add/get command.");
                        case "--clear-all":
                            if (op != Op.None) {
                                throw new ArgumentException(
                                    "Operations are mutually exclusive");
                            }
                            op = Op.ClearEntireRegistry;
                            break;
                        case "--clear":
                            if (op != Op.None) {
                                throw new ArgumentException(
                                    "Operations are mutually exclusive");
                            }
                            op = Op.ClearSupervisors;
                            break;
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException("Help");
                        default:
                            throw new ArgumentException($"Unknown {args[i]}");
                    }
                }
                if (op == Op.None) {
                    throw new ArgumentException("Missing operation.");
                }
                if (string.IsNullOrEmpty(cs)) {
                    throw new ArgumentException("Missing connection string.");
                }
                if (!ConnectionString.TryParse(cs, out var connectionString)) {
                    throw new ArgumentException("Bad connection string.");
                }
                config = connectionString.ToIoTHubConfig();
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Usage:       Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli [options] operation [args]

Operations (Mutually exclusive):

    --add <deviceId> <moduleId>
             Add twin module with given device id and module id to device registry.

    --get <deviceId> <moduleId>
             Get twin module connection string from device registry.

    --clear
             Clear all registered supervisor module identities.

    --clear-all
             Clear entire OPC Twin registry content.

Options:
     -C
    --connection-string
             IoT Hub owner connection string to use to connect to IoT hub for
             operations on the registry.

    --help
     -?
     -h      Prints out this help.
"
                    );
                return;
            }

            try {
                switch(op) {
                    case Op.AddSupervisor:
                        AddSupervisorAsync(config, deviceId, moduleId)
                            .Wait();
                        break;
                    case Op.GetModuleConnectionString:
                        GetModuleConnectionStringAsync(config, deviceId, moduleId)
                            .Wait();
                        break;
                    case Op.ClearSupervisors:
                        ClearSupervisorsAsync(config)
                            .Wait();
                        break;
                    case Op.ClearEntireRegistry:
                        ClearEntireRegistryAsync(config)
                            .Wait();
                        break;
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Get module connection string
        /// </summary>
        private static async Task GetModuleConnectionStringAsync(
            IIoTHubConfig config, string deviceId, string moduleId) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (string.IsNullOrEmpty(moduleId)) {
                moduleId = "opctwin";
            }
            var logger = new ConsoleLogger(null, LogLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);
            var cs = await registry.GetConnectionStringAsync(deviceId, moduleId);
            Console.WriteLine(cs);
        }

        /// <summary>
        /// Add supervisor
        /// </summary>
        private static async Task AddSupervisorAsync(IIoTHubConfig config,
            string deviceId, string moduleId) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (string.IsNullOrEmpty(moduleId)) {
                moduleId = "opctwin";
            }
            var logger = new ConsoleLogger(null, LogLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);
            await registry.CreateOrUpdateAsync(new DeviceTwinModel {
                Id = deviceId,
                ModuleId = moduleId,
                Capabilities = new DeviceCapabilitiesModel {
                    IotEdge = true
                }
            });
            var cs = await registry.GetConnectionStringAsync(deviceId, moduleId);
            Console.WriteLine(cs);
        }

        /// <summary>
        /// Clear all twin module identities
        /// </summary>
        private static async Task ClearSupervisorsAsync(IIoTHubConfig config) {
            var logger = new ConsoleLogger(null, LogLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{kTypeProp} = 'supervisor'";
            var supers = await registry.QueryDeviceTwinsAsync(query);
            foreach (var item in supers) {
                foreach (var tag in item.Tags.Keys.ToList()) {
                    item.Tags[tag] = null;
                }
                foreach (var property in item.Properties.Desired.Keys.ToList()) {
                    item.Properties.Desired[property] = null;
                }
                foreach (var property in item.Properties.Reported.Keys.ToList()) {
                    if (!item.Properties.Desired.ContainsKey(property)) {
                        item.Properties.Desired.Add(property, null);
                    }
                }
                await registry.CreateOrUpdateAsync(item);
            }
        }

        /// <summary>
        /// Clear entire registry
        /// </summary>
        private static async Task ClearEntireRegistryAsync(IIoTHubConfig config) {
            var logger = new ConsoleLogger(null, LogLevel.Error);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);
            var result = await registry.QueryDeviceTwinsAsync(
                "SELECT * from devices where IS_DEFINED(tags.DeviceType)");
            foreach (var item in result) {
                await registry.DeleteAsync(item.Id, item.ModuleId);
            }
        }
    }
}
