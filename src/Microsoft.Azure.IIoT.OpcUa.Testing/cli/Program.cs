// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cli {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Export;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Servers;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Graph.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Sample;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Hub.Runtime;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Serilog;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using Opc.Ua.Design;
    using Opc.Ua.Design.Resolver;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.Loader;

    /// <summary>
    /// Test client for opc ua services
    /// </summary>
    public class Program {

        enum Op {
            None,
            RunSampleServer,
            TestOpcUaServerClient,
            TestOpcUaIop,
            TestOpcUaDiscoveryService,
            TestOpcUaModelBrowseEncoder,
            TestOpcUaModelBrowseFile,
            TestOpcUaModelArchiver,
            TestOpcUaModelWriter,
            TestOpcUaModelDesign,
            TestOpcUaServerScanner,
            TestOpcUaPublisherClient,
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
            string deviceId = null, moduleId = null, addressRanges = null, fileName = null;
            var stress = false;
            var host = Utils.GetHostName();
            var ports = new List<int>();
            var configuration = new ConfigurationBuilder()
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .Build();
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "--sample":
                        case "-s":
                            op = Op.RunSampleServer;
                            break;
                        case "-p":
                        case "--port":
                            i++;
                            if (i < args.Length) {
                                ports.Add(ushort.Parse(args[i]));
                                break;
                            }
                            throw new ArgumentException(
                                "Missing arguments for port option");
                        case "--iop":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaIop;
                            break;
                        case "--stress":
                            stress = true;
                            break;
                        case "--test-client":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaServerClient;
                            i++;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            break;
                        case "--test-browse":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestBrowseServer;
                            i++;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            break;
                        case "--make-supervisor":
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
                        case "--test-archive":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaModelArchiver;
                            i++;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            break;
                        case "--test-export":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaModelBrowseEncoder;
                            i++;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            break;
                        case "--test-file":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaModelBrowseFile;
                            i++;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            break;
                        case "--test-writer":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaModelWriter;
                            i++;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            break;
                        case "--test-design":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaModelDesign;
                            i++;
                            if (i < args.Length) {
                                fileName = args[i];
                            }
                            break;
                        case "--clear-registry":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.ClearRegistry;
                            break;
                        case "--clear-supervisors":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.ClearSupervisors;
                            break;
                        case "--scan-ports":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestPortScanner;
                            i++;
                            if (i < args.Length) {
                                host = args[i];
                            }
                            break;
                        case "--scan-servers":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaServerScanner;
                            i++;
                            if (i < args.Length) {
                                host = args[i];
                            }
                            break;
                        case "--scan-net":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestNetworkScanner;
                            break;
                        case "--test-discovery":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaDiscoveryService;
                            i++;
                            if (i < args.Length) {
                                addressRanges = args[i];
                            }
                            break;
                        case "--test-publisher":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaPublisherClient;
                            i++;
                            if (i < args.Length) {
                                deviceId = args[i];
                            }
                            break;
                        case "--test-logperf":
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
Test Client
usage:       [options] operation [args]

Options:

    --stress                Run test as stress test (if supported)
    --port / -p             Port to listen on
    --help / -? / -h        Prints out this help.

Operations (Mutually exclusive):

     -s
    --sample / -s           Run sample server and wait for cancellation.

    --iop                   Iop discovery and browsing.
    --scan-net              Tests network scanning.
    --scan-ports            Tests port scanning.
    --scan-servers          Tests opc server scanning on single machine.

    --make-supervisor       Make supervisor module.
    --clear-registry        Clear device registry content.
    --clear-supervisors     Clear supervisors in device registry.

    --test-discovery        Tests discovery stuff.
    --test-publisher        Tests publisher clients.
    --test-logperf          Tests logger perf.
    --test-browse           Tests server browsing.

    --test_export           Tests server model export with passed endpoint url.
    --test-design           Test model design import
    --test-file             Tests server model export several files for perf.
    --test-writer           Tests server model import.
    --test-archive          Tests server model archiving to file.
    --test-client           Tests server stuff with passed endpoint url.

"
                    );
                return;
            }

            if (ports.Count == 0) {
                ports.Add(51210);
            }
            try {
                Console.WriteLine($"Running {op}...");
                switch (op) {
                    case Op.RunSampleServer:
                        RunServer(ports).Wait();
                        break;
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
                    case Op.TestOpcUaIop:
                        TestOpcUaIop().Wait();
                        break;
                    case Op.TestOpcUaModelBrowseEncoder:
                        TestOpcUaModelExportService(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelBrowseFile:
                        TestOpcUaModelExportToFile(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelArchiver:
                        TestOpcUaModelArchive(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelWriter:
                        TestOpcUaModelWriter(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelDesign:
                        TestOpcUaModelDesign(fileName).Wait();
                        break;
                    case Op.TestOpcUaPublisherClient:
                        TestOpcUaPublisherClient(deviceId).Wait();
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
        /// Run server until exit
        /// </summary>
        private static async Task RunServer(IEnumerable<int> ports) {
            using (var logger = StackLogger.Create(LogEx.Console())) {
                var tcs = new TaskCompletionSource<bool>();
                AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                using (var server = new ServerConsoleHost(new ServerFactory(logger.Logger),
                    logger.Logger) {
                    AutoAccept = true
                }) {
                    await server.StartAsync(ports);
#if DEBUG
                    if (!Console.IsInputRedirected) {
                        Console.WriteLine("Press any key to exit...");
                        Console.TreatControlCAsInput = true;
                        await Task.WhenAny(tcs.Task, Task.Run(() => Console.ReadKey()));
                        return;
                    }
#endif
                    await tcs.Task;
                }
            }
        }

        /// <summary>
        /// Create supervisor module identity in device registry
        /// </summary>
        private static async Task MakeSupervisor(string deviceId, string moduleId) {
            var logger = LogEx.ConsoleOut();
            var config = new IoTHubConfig(null);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);


            await registry.CreateOrUpdateAsync(new DeviceTwinModel {
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
            var logger = LogEx.ConsoleOut();
            var config = new IoTHubConfig(null);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);

            var query = "SELECT * FROM devices.modules WHERE " +
                $"properties.reported.{TwinProperty.kType} = 'supervisor'";
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
        /// Clear registry
        /// </summary>
        private static async Task ClearRegistry() {
            var logger = LogEx.ConsoleOut();
            var config = new IoTHubConfig(null);
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
            var logger = LogEx.ConsoleOut();
            var addresses = await Dns.GetHostAddressesAsync(host);
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var watch = Stopwatch.StartNew();
            var scanning = new ScanServices(logger);
            var results = await scanning.ScanAsync(
                PortRange.All.SelectMany(r => r.GetEndpoints(addresses.First())),
                opc ? new ServerProbe(logger) : null, cts.Token);
            foreach (var result in results) {
                Console.WriteLine($"Found {result} open.");
            }
            Console.WriteLine($"Scan took: {watch.Elapsed}");
        }

        /// <summary>
        /// Test network scanning
        /// </summary>
        private static async Task TestNetworkScanner() {
            var logger = LogEx.ConsoleOut();
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
            var logger = StackLogger.Create(LogEx.Console());
            var client = new ClientServices(logger.Logger);

            var discovery = new DiscoveryServices(client, new ConsoleEmitter(),
                new TaskProcessor(logger.Logger), logger.Logger);

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
                logger.Logger.Information("Stopping discovery!");
                discovery.Mode = DiscoveryMode.Off;
                await discovery.ScanAsync();
                if (!stress) {
                    break;
                }
            }
        }

        /// <summary>
        /// scan test for iop
        /// </summary>
        private static async Task TestOpcUaIop() {
            var logger = StackLogger.Create(LogEx.RollingFile("iop_log.txt"));
            var client = new ClientServices(logger.Logger);

            var discovery = new DiscoveryServices(client, new ModelWriter(client, logger.Logger),
                new TaskProcessor(logger.Logger), logger.Logger);

            var rand = new Random();
            while (true) {
                discovery.Configuration = new DiscoveryConfigModel {
                    IdleTimeBetweenScans = TimeSpan.FromMilliseconds(1),
                    MaxNetworkProbes = 1000,
                    MaxPortProbes = 5000
                };
                discovery.Mode = DiscoveryMode.Scan;
                await discovery.ScanAsync();
                Console.WriteLine("Press key to stop...");
                Console.ReadKey();
                discovery.Mode = DiscoveryMode.Off;
                await discovery.ScanAsync();
            }
        }

        /// <inheritdoc/>
        private class ModelWriter : IEventEmitter {

            public ModelWriter(ClientServices client, ILogger logger) {
                _logger = logger;
                _client = client;
            }

            /// <inheritdoc/>
            public string DeviceId { get; } = "";

            /// <inheritdoc/>
            public string ModuleId { get; } = "";

            /// <inheritdoc/>
            public string SiteId => null;

            /// <inheritdoc/>
            public async Task SendAsync(byte[] data, string contentType) {
                var events = JsonConvert.DeserializeObject<IEnumerable<DiscoveryEventModel>>(
                    Encoding.UTF8.GetString(data));
                foreach (var ev in events) {
                    var endpoint = ev.Registration?.Endpoint;
                    if (endpoint == null) {
                        continue;
                    }
                    try {
                        _logger.Information("Writing {id}.json for {@ev}", ev.Registration.Id, ev);
                        using (var writer = File.CreateText($"iop_{ev.Registration.Id}.json"))
                        using (var json = new JsonTextWriter(writer) {
                            AutoCompleteOnClose = true,
                            Formatting = Formatting.Indented,
                            DateFormatHandling = DateFormatHandling.IsoDateFormat
                        })
                        using (var encoder = new JsonEncoderEx(json, null,
                            JsonEncoderEx.JsonEncoding.Array) {
                            IgnoreDefaultValues = true,
                            UseAdvancedEncoding = true
                        })
                        using (var browser = new BrowseStreamEncoder(_client, endpoint, encoder,
                            null, _logger, null)) {
                            await browser.EncodeAsync(CancellationToken.None);
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed to browse");
                    }
                }
            }

            /// <inheritdoc/>
            public Task SendAsync(IEnumerable<byte[]> batch, string contentType) {
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task SendAsync(string propertyId, dynamic value) {
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task SendAsync(IEnumerable<KeyValuePair<string, dynamic>> properties) {
                return Task.CompletedTask;
            }

            private readonly ILogger _logger;
            private readonly ClientServices _client;
        }

        /// <summary>
        /// Test model browse encoder
        /// </summary>
        private static async Task TestOpcUaModelExportService(EndpointModel endpoint) {
            var logger = StackLogger.Create(LogEx.Console());
            using (var client = new ClientServices(logger.Logger))
            using (var server = new ServerWrapper(endpoint, logger))
            using (var stream = Console.OpenStandardOutput())
            using (var writer = new StreamWriter(stream))
            using (var json = new JsonTextWriter(writer) {
                AutoCompleteOnClose = true,
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            })
            using (var encoder = new JsonEncoderEx(json, null,
                JsonEncoderEx.JsonEncoding.Array) {
                IgnoreDefaultValues = true,
                UseAdvancedEncoding = true
            })
            using (var browser = new BrowseStreamEncoder(client, endpoint, encoder,
                null, logger.Logger, null)) {
                await browser.EncodeAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Test model archiver
        /// </summary>
        private static async Task TestOpcUaModelArchive(EndpointModel endpoint) {
            var logger = StackLogger.Create(LogEx.Console());
            var storage = new ZipArchiveStorage();

            var fileName = "tmp.zip";
            using (var client = new ClientServices(logger.Logger))
            using (var server = new ServerWrapper(endpoint, logger)) {
                var sw = Stopwatch.StartNew();
                using (var archive = await storage.OpenAsync(fileName, FileMode.Create, FileAccess.Write))
                using (var archiver = new AddressSpaceArchiver(client, endpoint, archive, logger.Logger)) {
                    await archiver.ArchiveAsync(CancellationToken.None);
                }
                var elapsed = sw.Elapsed;
                using (var file = File.Open(fileName, FileMode.OpenOrCreate)) {
                    Console.WriteLine($"Encode as to {fileName} took " +
                        $"{elapsed}, and produced {file.Length} bytes.");
                }
            }
        }

        /// <summary>
        /// Test model browse encoder to file
        /// </summary>
        private static async Task TestOpcUaModelExportToFile(EndpointModel endpoint) {
            var logger = StackLogger.Create(LogEx.Console());

            // Run both encodings twice to prime server and get realistic timings the
            // second time around
            var runs = new Dictionary<string, string> {
                ["json1.zip"] = ContentEncodings.MimeTypeUaJson,
                //  ["bin1.zip"] = ContentEncodings.MimeTypeUaBinary,
                ["json2.zip"] = ContentEncodings.MimeTypeUaJson,
                //  ["bin2.zip"] = ContentEncodings.MimeTypeUaBinary,
                ["json1.gzip"] = ContentEncodings.MimeTypeUaJson,
                //  ["bin1.gzip"] = ContentEncodings.MimeTypeUaBinary,
                ["json2.gzip"] = ContentEncodings.MimeTypeUaJson,
                // ["bin2.gzip"] = ContentEncodings.MimeTypeUaBinary
            };

            using (var client = new ClientServices(logger.Logger))
            using (var server = new ServerWrapper(endpoint, logger)) {
                foreach (var run in runs) {
                    var zip = Path.GetExtension(run.Key) == ".zip";
                    Console.WriteLine($"Writing {run.Key}...");
                    var sw = Stopwatch.StartNew();
                    using (var stream = new FileStream(run.Key, FileMode.Create)) {
                        using (var zipped = zip ?
                            new DeflateStream(stream, CompressionLevel.Optimal) :
                            (Stream)new GZipStream(stream, CompressionLevel.Optimal))
                        using (var browser = new BrowseStreamEncoder(client, endpoint, zipped,
                            run.Value, null, logger.Logger, null)) {
                            await browser.EncodeAsync(CancellationToken.None);
                        }
                    }
                    var elapsed = sw.Elapsed;
                    using (var file = File.Open(run.Key, FileMode.OpenOrCreate)) {
                        Console.WriteLine($"Encode as {run.Value} to {run.Key} took " +
                            $"{elapsed}, and produced {file.Length} bytes.");
                    }
                }
            }
        }

        /// <summary>
        /// Test model export and import
        /// </summary>
        private static async Task TestOpcUaModelWriter(EndpointModel endpoint) {
            var logger = StackLogger.Create(LogEx.Console());
            var filename = "model.zip";
            using (var server = new ServerWrapper(endpoint, logger)) {
                using (var client = new ClientServices(logger.Logger)) {
                    Console.WriteLine($"Reading into {filename}...");
                    using (var stream = new FileStream(filename, FileMode.Create)) {
                        using (var zipped = new DeflateStream(stream, CompressionLevel.Optimal))
                        using (var browser = new BrowseStreamEncoder(client, endpoint, zipped,
                            ContentEncodings.MimeTypeUaJson, null, logger.Logger, null)) {
                            await browser.EncodeAsync(CancellationToken.None);
                        }
                    }
                }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            IDatabaseServer database = new MemoryDatabase(logger.Logger);
            for (var i = 0; ; i++) {
                Console.WriteLine($"{i}: Writing from {filename}...");
                var sw = Stopwatch.StartNew();
                using (var file = File.Open(filename, FileMode.OpenOrCreate)) {
                    using (var unzipped = new DeflateStream(file, CompressionMode.Decompress)) {
                        var writer = new SourceStreamImporter(new ItemContainerFactory(database),
                            new JsonVariantEncoder(), logger.Logger);
                        await writer.ImportAsync(unzipped, Path.GetFullPath(filename + i),
                            ContentEncodings.MimeTypeUaJson, null, CancellationToken.None);
                    }
                }
                var elapsed = sw.Elapsed;
                Console.WriteLine($"{i}: Writing took {elapsed}.");
            }
        }

        /// <summary>
        /// Test client
        /// </summary>
        private static async Task TestOpcUaServerClient(EndpointModel endpoint) {
            var logger = StackLogger.Create(LogEx.Console());
            using (var client = new ClientServices(logger.Logger))
            using (var server = new ServerWrapper(endpoint, logger)) {
                await client.ExecuteServiceAsync(endpoint, null, session => {
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
        }

        /// <summary>
        /// Test model design import
        /// </summary>
        /// <param name="designFile"></param>
        /// <returns></returns>
        private static Task TestOpcUaModelDesign(string designFile) {
            if (string.IsNullOrEmpty(designFile)) {
                throw new ArgumentException(nameof(designFile));
            }
            var design = Model.Load(designFile, new CompositeModelResolver());
            design.Save(Path.GetDirectoryName(designFile));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Test publisher
        /// </summary>
        private static async Task TestOpcUaPublisherClient(string deviceId) {
            var logger = LogEx.ConsoleOut();
            var stackLogger = StackLogger.Create(logger);
            var config = new IoTHubConfig(null);
            var registry = new IoTHubServiceHttpClient(new HttpClient(logger),
                config, logger);
            var device = new TestDeviceMethodClient(registry);
            var publisherIdentity = new TestIdentity { DeviceId = deviceId };
            var endpoint = new EndpointModel();

            using (var opc = new ClientServices(logger)) {
                var discovery = new PublisherDiscovery(device, publisherIdentity, device, opc, opc, logger);
                using (var client = new PublisherServices(discovery, opc, logger))
                using (var server = new ServerWrapper(endpoint, StackLogger.Create(logger))) {

                    // Must start client first and find publisher
                    Console.WriteLine("Finding publisher...");
                    await client.StartAsync();

                    Console.WriteLine("Getting list of published nodes from publisher...");
                    var publishing = await client.NodePublishListAsync(endpoint, new PublishedItemListRequestModel());
                    Console.WriteLine(JsonConvertEx.SerializeObjectPretty(publishing));
                    Console.WriteLine("Start publishing...");
                    await client.NodePublishStartAsync(endpoint, new PublishStartRequestModel {
                        Item = new PublishedItemModel { NodeId = "i=2258" }
                    });
                    await client.NodePublishStartAsync(endpoint, new PublishStartRequestModel {
                        Item = new PublishedItemModel { NodeId = "http://test.org/UA/Data/#i=10217" }
                    });

                    Console.WriteLine("Getting list of published nodes from publisher...");
                    publishing = await client.NodePublishListAsync(endpoint, new PublishedItemListRequestModel());
                    Console.WriteLine(JsonConvertEx.SerializeObjectPretty(publishing));
                    Console.WriteLine("--------- Publishing ---------");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    Console.WriteLine("Getting list of published nodes from publisher...");
                    publishing = await client.NodePublishListAsync(endpoint, new PublishedItemListRequestModel());
                    Console.WriteLine(JsonConvertEx.SerializeObjectPretty(publishing));

                    Console.WriteLine("Stop publishing...");
                    await client.NodePublishStopAsync(endpoint, new PublishStopRequestModel {
                        NodeId = "i=2258"
                    });
                    await client.NodePublishStopAsync(endpoint, new PublishStopRequestModel {
                        NodeId = "http://test.org/UA/Data/#i=10217"
                    });

                    Console.WriteLine("Getting list of published nodes from publisher...");
                    publishing = await client.NodePublishListAsync(endpoint, new PublishedItemListRequestModel());
                    Console.WriteLine(JsonConvertEx.SerializeObjectPretty(publishing));
                }
            }
        }

        /// <summary>
        /// Test logger performance
        /// </summary>
        private static void TestLoggerPerf() {
            var list = new List<Tuple<TimeSpan, TimeSpan>>();
            var l = LogEx.ConsoleOut();
            for (var j = 0; j < 100; j++) {
                var sw2 = Stopwatch.StartNew();
                for (var i = 0; i < 100000; i++) {
                    Console.WriteLine($"[ERROR][test][{DateTimeOffset.UtcNow.ToString("u")}][Program:TestBrowseServer] Test {i} {i}");
                }
                var passed2 = sw2.Elapsed;
                var sw1 = Stopwatch.StartNew();
                for (var i = 0; i < 100000; i++) {
                    l.Error("Test {i}", i);
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

            var logger = StackLogger.Create(LogEx.Console());

            var request = new BrowseRequestModel {
                TargetNodesOnly = false
            };
            var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                ObjectIds.RootFolder.ToString()
            };

            using (var client = new ClientServices(logger.Logger)) {
                var service = new AddressSpaceServices(client, new JsonVariantEncoder(), logger.Logger);
                using (var server = new ServerWrapper(endpoint, logger)) {
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
                                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
                            }

                            // Do recursive browse
                            foreach (var r in result.References) {
                                if (!visited.Contains(r.ReferenceTypeId)) {
                                    nodes.Add(r.ReferenceTypeId);
                                }
                                if (!visited.Contains(r.Target.NodeId)) {
                                    nodes.Add(r.Target.NodeId);
                                }
                                if (nodesRead.Contains(r.Target.NodeId)) {
                                    continue; // We have read this one already
                                }
                                if (!r.Target.NodeClass.HasValue ||
                                    r.Target.NodeClass.Value != Twin.Models.NodeClass.Variable) {
                                    continue;
                                }
                                if (!silent) {
                                    Console.WriteLine($"Reading {r.Target.NodeId}");
                                    Console.WriteLine($"====================");
                                }
                                try {
                                    nodesRead.Add(r.Target.NodeId);
                                    var read = await service.NodeValueReadAsync(endpoint,
                                        new ValueReadRequestModel {
                                            NodeId = r.Target.NodeId
                                        });
                                    if (!silent) {
                                        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
                                    }
                                }
                                catch (Exception ex) {
                                    Console.WriteLine($"Reading {r.Target.NodeId} resulted in {ex}");
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
            }
        }

        /// <summary>
        /// Wraps server and disposes after use
        /// </summary>
        private class ServerWrapper : IDisposable {

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="endpoint"></param>
            public ServerWrapper(EndpointModel endpoint, StackLogger logger) {
                _cts = new CancellationTokenSource();
                if (endpoint.Url == null) {
                    _server = RunSampleServer(_cts.Token, logger.Logger);
                    endpoint.Url = "opc.tcp://" + Utils.GetHostName() +
                        ":51210/UA/SampleServer";
                }
                else {
                    _server = Task.CompletedTask;
                }
            }

            /// <inheritdoc/>
            public void Dispose() {
                _cts.Cancel();
                _server.Wait();
            }

            /// <summary>
            /// Run server until cancelled
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private static async Task RunSampleServer(CancellationToken ct, ILogger logger) {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetResult(true));
                using (var server = new ServerConsoleHost(new ServerFactory(logger) {
                    LogStatus = false
                }, logger) {
                    AutoAccept = true
                }) {
                    await server.StartAsync(new List<int> { 51210 });
                    await tcs.Task;
                }
            }

            private readonly CancellationTokenSource _cts;
            private readonly Task _server;
        }

        /// <inheritdoc/>
        private class ConsoleEmitter : IEventEmitter {

            /// <inheritdoc/>
            public string DeviceId { get; } = "";

            /// <inheritdoc/>
            public string ModuleId { get; } = "";

            /// <inheritdoc/>
            public string SiteId => null;

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
            public string DeviceId { get; } = "";

            /// <inheritdoc/>
            public string ModuleId { get; } = "";

            /// <inheritdoc/>
            public string SiteId { get; } = "";

            /// <inheritdoc/>
            public Task SendFileAsync(string fileName, Stream stream, string contentType) {
                Console.WriteLine(new StreamReader(stream).ReadToEnd());
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Interact with a device as if it was a module
        /// </summary>
        private sealed class TestDeviceMethodClient : IModuleDiscovery, IJsonMethodClient {

            /// <inheritdoc/>
            public int MaxMethodPayloadCharacterCount => 120 * 1024;

            /// <summary>
            /// Create client
            /// </summary>
            /// <param name="twin"></param>
            public TestDeviceMethodClient(IIoTHubTwinServices twin) {
                _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            }

            /// <inheritdoc/>
            public async Task<List<DiscoveredModuleModel>> GetModulesAsync(
                string deviceId) {
                var device = await _twin.GetAsync(deviceId);
                return new List<DiscoveredModuleModel> {
                        new DiscoveredModuleModel {
                            Id = deviceId,
                            ImageName = "publisher",
                            Status = device.Status
                        }
                    };
            }

            /// <inheritdoc/>
            public async Task<string> CallMethodAsync(string deviceId, string moduleId,
                string method, string payload, TimeSpan? timeout = null) {
                var result = await _twin.CallMethodAsync(deviceId, null,
                    new MethodParameterModel {
                        Name = method,
                        ResponseTimeout = timeout,
                        JsonPayload = payload
                    });
                if (result.Status != 200) {
                    throw new MethodCallStatusException(
                        Encoding.UTF8.GetBytes(result.JsonPayload), result.Status);
                }
                return result.JsonPayload;
            }

            private readonly IIoTHubTwinServices _twin;
        }

        /// <inheritdoc/>
        private class TestIdentity : IIdentity {

            /// <inheritdoc/>
            public string DeviceId { get; set; }

            /// <inheritdoc/>
            public string ModuleId { get; set; }

            /// <inheritdoc/>
            public string SiteId { get; set; }
        }
    }
}
