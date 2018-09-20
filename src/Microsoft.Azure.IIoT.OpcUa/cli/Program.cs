// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cli {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Export;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control;
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
    /// Test client for opc ua services
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
            TestBrowseServer,
            MakeSupervisor,
            ClearSupervisors,
            ClearRegistry
        }

        /// <summary>
        /// Test client entry point
        /// </summary>
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
                        case "-b":
                        case "--browse":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestBrowseServer;
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
                        case "--testlogperf":
                            TestLoggerPerf();
                            return;
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
     -b
    --browse
             Tests server browsing.
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
                    case Op.TestBrowseServer:
                        TestBrowseServer(endpoint).Wait();
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
        /// Test client
        /// </summary>
        private static async Task TestOpcUaServerClient(EndpointModel endpoint) {
            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var client = new ClientServices(logger);
            await client.ExecuteServiceAsync(endpoint, session => {
                Console.WriteLine("Browse the OPC UA server namespace.");
                var w = Stopwatch.StartNew();
                var stack = new Stack<Tuple<string, ReferenceDescription>>();
                session.Browse(null, null, ObjectIds.RootFolder,
                    0u, Opc.Ua.BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                    true, 0, out var continuationPoint, out var references);
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
                        true, 0, out continuationPoint, out references);
                    references.Reverse();
                    foreach (var rd in references) {
                        stack.Push(Tuple.Create(browsed.Item1 + "   ", rd));
                    }
                    Console.WriteLine($"{browsed.Item1}{(references.Count == 0 ? "-" : "+")} " +
                        $"{browsed.Item2.DisplayName}, {browsed.Item2.BrowseName}, {browsed.Item2.NodeClass}");
                }
                Console.WriteLine($"   ....        took {w.ElapsedMilliseconds} ms...");
                return Task.FromResult(true);
            });
        }

        /// <summary>
        /// Test logger performance
        /// </summary>
        private static void TestLoggerPerf() {
            var list = new List<Tuple<TimeSpan, TimeSpan>>();
            var l = new ConsoleLogger("test", LogLevel.Debug);
            for (var j = 0; j < 100; j++) {
                var sw2 = Stopwatch.StartNew();
                for (var i = 0; i < 100000; i++) {
                    Console.WriteLine($"[ERROR][test][{DateTimeOffset.UtcNow.ToString("u")}][Program:TestBrowseServer] Test {i} {i}");
                }
                var passed2 = sw2.Elapsed;
                var sw1 = Stopwatch.StartNew();
                for (var i = 0; i < 100000; i++) {
                    l.Error($"Test {i}", () => i);
                }
                var passed1 = sw1.Elapsed;
                list.Add(Tuple.Create(passed1, passed2));
            }
            Console.Clear();
            var a1 = list.Skip(1).Average(t => t.Item1.TotalMilliseconds);
            var a2 = list.Skip(1).Average(t => t.Item2.TotalMilliseconds);
            Console.WriteLine($"PCSLogger {TimeSpan.FromMilliseconds(a1)} :: Raw console {TimeSpan.FromMilliseconds(a2)}");
        }

        /// <summary>
        /// Test address space control
        /// </summary>
        private static async Task TestBrowseServer(EndpointModel endpoint, bool silent = false) {

            var logger = new ConsoleLogger("test", LogLevel.Debug);
            var client = new ClientServices(logger);
            var service = new AddressSpaceServices(client, new JsonVariantEncoder(), logger);

            var request = new BrowseRequestModel {
                TargetNodesOnly = false
            };
            var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                ObjectIds.RootFolder.ToString()
            };

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nodesRead = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = 0;
            var sw = Stopwatch.StartNew();
            while (nodes.Count > 0) {
                request.NodeId = nodes.First();
                nodes.Remove(request.NodeId);
                try {
                    if (!silent) {
                        Console.WriteLine($"Browsing {request.NodeId}");
                        Console.WriteLine($"====================");
                    }
                    var result = await service.NodeBrowseAsync(endpoint, request);
                    visited.Add(request.NodeId);
                    if (!silent) {
                        Console.WriteLine(JsonConvert.SerializeObject(result,Formatting.Indented));
                    }

                    // Do recursive browse
                    foreach (var r in result.References) {
                        if (!visited.Contains(r.Id)) {
                            nodes.Add(r.Id);
                        }
                        if (!visited.Contains(r.Target.Id)) {
                            nodes.Add(r.Target.Id);
                        }
                        if (nodesRead.Contains(r.Target.Id)) {
                            continue; // We have read this one already
                        }
                        if (!r.Target.NodeClass.HasValue ||
                            r.Target.NodeClass.Value != Models.NodeClass.Variable) {
                            continue;
                        }
                        if (!silent) {
                            Console.WriteLine($"Reading {r.Target.Id}");
                            Console.WriteLine($"====================");
                        }
                        try {
                            nodesRead.Add(r.Target.Id);
                            var read = await service.NodeValueReadAsync(endpoint,
                                new ValueReadRequestModel {
                                    NodeId = r.Target.Id
                                });
                            if (!silent) {
                                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Reading {r.Target.Id} resulted in {ex}");
                            errors++;
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"Browse {request.NodeId} resulted in {e}");
                    errors++;
                }
            }
            Console.WriteLine($"Browse took {sw.Elapsed}. Visited " +
                $"{visited.Count} nodes and read {nodesRead.Count} of them with {errors} errors.");
        }

        /// <inheritdoc/>
        private class ConsoleEmitter : IEventEmitter {

            /// <inheritdoc/>
            public string DeviceId { get; } = "";

            /// <inheritdoc/>
            public string ModuleId { get; } = "";

            /// <inheritdoc/>
            public string SiteId { get; } = "";

            /// <inheritdoc/>
            public Task SendAsync(byte[] data, string contentType) {
                var json = Encoding.UTF8.GetString(data);
                var o = JsonConvert.DeserializeObject(json);
                Console.WriteLine(contentType);
                Console.WriteLine(JsonConvertEx.SerializeObjectPretty(o));
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public async Task SendAsync(IEnumerable<byte[]> batch, string contentType) {
                foreach (var data in batch) {
                    await SendAsync(data, contentType);
                }
            }

            /// <inheritdoc/>
            public Task SendAsync(string propertyId, dynamic value) {
                Console.WriteLine($"{propertyId}={value}");
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task SendAsync(IEnumerable<KeyValuePair<string, dynamic>> properties) {
                foreach (var prop in properties) {
                    Console.WriteLine($"{prop.Key}={prop.Value}");
                }
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        private class ConsoleUploader : IBlobUpload {
            /// <inheritdoc/>
            public Task SendFileAsync(string fileName, string contentType) {
                using (var stream = new FileStream(fileName, FileMode.Open)) {
                    Console.WriteLine(new StringReader(fileName).ReadToEnd());
                }
                return Task.CompletedTask;
            }
        }
    }
}
