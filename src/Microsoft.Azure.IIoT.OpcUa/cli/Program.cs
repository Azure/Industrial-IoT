// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cli {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Export;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Sample program to run imports
    /// </summary>
    public class Program {

        /// <summary>
        /// Test configuration
        /// </summary>
        private class IoTHubTestConfig : IIoTHubConfig {
            public string IoTHubConnString => Environment.GetEnvironmentVariable("_HUB_CS");
            public string IoTHubResourceId => null;
        }

        enum Op {
            None,
            TestOpcUaServerClient,
            TestOpcUaDiscoveryService,
            TestOpcUaExportService,
            TestOpcUaServerScanner,
            TestNetworkScanner,
            TestPortScanner,
            MakeSupervisor,
            ClearSupervisors,
            ClearRegistry
        }

        /// <summary>
        /// Importer entry point
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static void Main(string[] args) {
            var op = Op.None;
            var endpoint = new EndpointModel();
            var host = Utils.GetHostName();
            string deviceId = null, moduleId = null, addressRanges = null;
            var stress = false;

            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "--stress":
                            stress = true;
                            break;
                        case "-s":
                        case "--server":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaServerClient;
                            i++;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            else {
                                endpoint.Url = "opc.tcp://" + host + ":51210/UA/SampleServer";
                            }
                            break;
                        case "-m":
                        case "--make":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.MakeSupervisor;
                            i++;
                            if (i < args.Length) {
                                deviceId = args[i];
                                i++;
                                if (i < args.Length) {
                                    moduleId = args[i];
                                    break;
                                }
                            }
                            throw new ArgumentException("Missing arguments to make iotedge device");
                        case "-e":
                        case "--export":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaExportService;
                            i++;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            else {
                                endpoint.Url = "opc.tcp://" + host + ":51210/UA/SampleServer";
                            }
                            break;
                        case "-c":
                        case "--clear":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.ClearRegistry;
                            break;
                        case "-C":
                        case "--clear-supervisors":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.ClearSupervisors;
                            break;
                        case "-p":
                        case "--ports":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestPortScanner;
                            i++;
                            if (i < args.Length) {
                                host = args[i];
                            }
                            break;
                        case "-o":
                        case "--opcservers":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaServerScanner;
                            i++;
                            if (i < args.Length) {
                                host = args[i];
                            }
                            break;
                        case "-n":
                        case "--net":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestNetworkScanner;
                            break;
                        case "-d":
                        case "--discover":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaDiscoveryService;
                            i++;
                            if (i < args.Length) {
                                addressRanges = args[i];
                            }
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
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Services Client
usage:       Services.Client [options] operation [args]

Options:

    --help
     -?
     -h      Prints out this help.

Operations (Mutually exclusive):
     -n
    --network
             Tests network scanning.
     -p
    --ports
             Tests port scanning.
     -o
    --opcservers
             Tests opc server scanning on single machine.
     -d
    --discover
             Tests discovery stuff.
     -c
    --clear
             Clear device registry content.
     -C
    --clear-supervisors
             Clear supervisors in device registry.
     -m
    --make
             Make supervisor module.
     -e
    --export
             Tests server model export with passed endpoint url.
     -s
    --server
             Tests server stuff with passed endpoint url.  The server
             is queried or browsed to gather all nodes to import.
"
                    );
                return;
            }

            try {
                Console.WriteLine($"Running {op}...");
                switch(op) {
                    case Op.TestOpcUaServerClient:
                        TestOpcUaServerClient(endpoint).Wait();
                        break;
                    case Op.TestNetworkScanner:
                        TestNetworkScanner().Wait();
                        break;
                    case Op.TestPortScanner:
                        TestPortScanner(host, false).Wait();
                        break;
                    case Op.TestOpcUaServerScanner:
                        TestPortScanner(host, true).Wait();
                        break;
                    case Op.TestOpcUaDiscoveryService:
                        TestOpcUaDiscoveryService(addressRanges, stress).Wait();
                        break;
                    case Op.TestOpcUaExportService:
                        TestOpcUaModelExportService(endpoint).Wait();
                        break;
                    case Op.MakeSupervisor:
                        MakeSupervisor(deviceId, moduleId).Wait();
                        break;
                    case Op.ClearSupervisors:
                        ClearSupervisors().Wait();
                        break;
                    case Op.ClearRegistry:
                        ClearRegistry().Wait();
                        break;
                    default:
                        throw new ArgumentException("Unknown.");
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            Console.WriteLine("Press key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Create supervisor module identity in device registry
        /// </summary>
        /// <returns></returns>
        private static async Task MakeSupervisor(string deviceId, string moduleId) {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var config = new IoTHubTestConfig();
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);


            await registry.CreateOrUpdateAsync(new Hub.Models.DeviceTwinModel {
                Id = deviceId,
                ModuleId = moduleId
            });

            var module = await registry.GetRegistrationAsync(deviceId, moduleId);
            Console.WriteLine(JsonConvert.SerializeObject(module));
            var twin = await registry.GetAsync(deviceId, moduleId);
            Console.WriteLine(JsonConvert.SerializeObject(twin));
            var cs = ConnectionString.Parse(config.IoTHubConnString);
            Console.WriteLine("Connection string:");
            Console.WriteLine($"HostName={cs.HostName};DeviceId={deviceId};" +
                $"ModuleId={moduleId};SharedAccessKey={module.Authentication.PrimaryKey}");
        }

        /// <summary>
        /// Clear registry
        /// </summary>
        /// <returns></returns>
        private static async Task ClearSupervisors() {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var config = new IoTHubTestConfig();
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

        public const string kTypeProp = "__type__"; // TODO: Consolidate as common constant

        /// <summary>
        /// Clear registry
        /// </summary>
        /// <returns></returns>
        private static async Task ClearRegistry() {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var config = new IoTHubTestConfig();
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);

            var result = await registry.QueryDeviceTwinsAsync(
                "SELECT * from devices where IS_DEFINED(tags.DeviceType)");
            foreach (var item in result) {
                await registry.DeleteAsync(item.Id, item.ModuleId);
            }
        }

        /// <summary>
        /// Test port scanning
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private static async Task TestPortScanner(string host, bool opc) {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var addresses = await Dns.GetHostAddressesAsync(host);
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var watch = Stopwatch.StartNew();
            var scanning = new ScanServices(logger);
            var results = await scanning.ScanAsync(
                PortRange.All.SelectMany(r => r.GetEndpoints(addresses.First())),
                opc ? new ServerProbe(logger) : null, cts.Token);
            foreach(var result in results) {
                Console.WriteLine($"Found {result} open.");
            }
            Console.WriteLine($"Scan took: {watch.Elapsed}");
        }

        /// <summary>
        /// Test network scanning
        /// </summary>
        /// <returns></returns>
        private static async Task TestNetworkScanner() {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var watch = Stopwatch.StartNew();
            var scanning = new ScanServices(logger);
            var results = await scanning.ScanAsync(NetworkClass.Wired, cts.Token);
            foreach (var result in results) {
                Console.WriteLine($"Found {result.Address}...");
            }
            Console.WriteLine($"Scan took: {watch.Elapsed}");
        }

        /// <summary>
        /// Test discovery
        /// </summary>
        /// <returns></returns>
        private static async Task TestOpcUaDiscoveryService(string addressRanges,
            bool stress) {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var client = new ClientServices(logger);

            var discovery = new DiscoveryServices(client, new ConsoleEmitter(),
                new TaskProcessor(logger), logger);

            var rand = new Random();
            while (true) {
                discovery.Configuration = new DiscoveryConfigModel {
                    IdleTimeBetweenScans = TimeSpan.FromMilliseconds(1),
                    AddressRangesToScan = addressRanges
                };
                discovery.Mode = DiscoveryMode.Scan;
                await discovery.ScanAsync();
                await Task.Delay(!stress ? TimeSpan.FromMinutes(10) :
                    TimeSpan.FromMilliseconds(rand.Next(0, 120000)));
                logger.Info("Stopping discovery!", () => { });
                discovery.Mode = DiscoveryMode.Scan;
                await discovery.ScanAsync();
                if (!stress) {
                    break;
                }
            }
        }

        /// <summary>
        /// Test model browse
        /// </summary>
        /// <returns></returns>
        private static async Task TestOpcUaModelExportService(EndpointModel endpoint) {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var client = new ClientServices(logger);

            var exporter = new ExportServices(client, new ConsoleUploader(),
                new ConsoleEmitter(), new LimitingScheduler(), logger) {
                ExportIdleTime = TimeSpan.FromMilliseconds(1)
            };
            var id = await exporter.StartModelExportAsync(endpoint, "application/ua-json");
            await Task.Delay(TimeSpan.FromMinutes(10));
            await exporter.StopModelExportAsync(id);
        }

        /// <summary>
        /// Test client stuff
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private static async Task TestOpcUaServerClient(EndpointModel endpoint) {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var client = new ClientServices(logger);
            await client.ExecuteServiceAsync(endpoint, async session => {
                var mask = (uint)Opc.Ua.NodeClass.Variable | (uint)Opc.Ua.NodeClass.Object |
                    (uint)Opc.Ua.NodeClass.Method;
                Console.WriteLine("Browse the OPC UA server namespace.");
                var w = Stopwatch.StartNew();
                var stack = new Stack<Tuple<string, ReferenceDescription>>();
                session.Browse(null, null, ObjectIds.ObjectsFolder,
                    0u, Opc.Ua.BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                    true, mask, out var continuationPoint, out var references);
                Console.WriteLine(" DisplayName, BrowseName, NodeClass");
                references.Reverse();
                foreach (var rd in references) {
                    stack.Push(Tuple.Create("", rd));
                }
                while (stack.Count > 0) {
                    var browsed = stack.Pop();
                    session.Browse(null, null,
                        ExpandedNodeId.ToNodeId(browsed.Item2.NodeId, session.NamespaceUris),
                        0u, Opc.Ua.BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                        true, mask, out continuationPoint, out references);
                    references.Reverse();
                    foreach (var rd in references) {
                        stack.Push(Tuple.Create(browsed.Item1 + "   ", rd));
                    }
                    Console.WriteLine($"{browsed.Item1}{(references.Count == 0 ? "-" : "+")} " +
                        $"{browsed.Item2.DisplayName}, {browsed.Item2.BrowseName}, {browsed.Item2.NodeClass}");
                }
                Console.WriteLine($"   ....        took {w.ElapsedMilliseconds} ms...");
                Console.WriteLine("Create a subscription with publishing interval of 1 second.");
                var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 1000 };
                Console.WriteLine("Add a list of items (server current time and status) to the subscription.");
                var list = new List<MonitoredItem> {
                    new MonitoredItem(subscription.DefaultItem) {
                        DisplayName = "ServerStatusCurrentTime", StartNodeId = "i=2258",
                    }
                };
                list.ForEach(m => m.Notification += (item, e) => {
                    foreach (var value in item.DequeueValues()) {
                        Console.WriteLine(
                            $"{item.DisplayName}: {value.Value}, {value.SourceTimestamp}, {value.StatusCode}");
                    }
                });
                subscription.AddItems(list);
                Console.WriteLine("Add the subscription to the session.");
                session.AddSubscription(subscription);
                subscription.Create();

                await Task.Delay(60000);
                Console.WriteLine("Delete the subscription from the session.");
                subscription.Delete(false);
                subscription.Dispose();
                return true;
            });
        }

        private class ConsoleEmitter : IEventEmitter {

            public string DeviceId { get; } = "";

            public string ModuleId { get; } = "";

            public string SiteId { get; } = "";

            public Task SendAsync(byte[] data, string contentType) {
                var json = Encoding.UTF8.GetString(data);
                var o = JsonConvert.DeserializeObject(json);
                Console.WriteLine(contentType);
                Console.WriteLine(JsonConvertEx.SerializeObjectPretty(o));
                return Task.CompletedTask;
            }

            public async Task SendAsync(IEnumerable<byte[]> batch, string contentType) {
                foreach (var data in batch) {
                    await SendAsync(data, contentType);
                }
            }

            public Task SendAsync(string propertyId, dynamic value) {
                Console.WriteLine($"{propertyId}={value}");
                return Task.CompletedTask;
            }

            public Task SendAsync(IEnumerable<KeyValuePair<string, dynamic>> properties) {
                foreach (var prop in properties) {
                    Console.WriteLine($"{prop.Key}={prop.Value}");
                }
                return Task.CompletedTask;
            }
        }

        private class ConsoleUploader : IBlobUpload {
            public Task SendFileAsync(string fileName, string contentType) {
                using (var stream = new FileStream(fileName, FileMode.Open)) {
                    Console.WriteLine(new StringReader(fileName).ReadToEnd());
                }
                return Task.CompletedTask;
            }
        }
    }
}
