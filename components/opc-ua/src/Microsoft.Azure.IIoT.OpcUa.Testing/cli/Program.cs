// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cli {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Export.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Sample;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Design;
    using Opc.Ua.Design.Resolver;
    using Opc.Ua.Encoders;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Test client for opc ua services
    /// </summary>
    public class Program {
        private enum Op {
            None,
            RunSampleServer,
            TestOpcUaServerClient,
            TestOpcUaModelBrowseEncoder,
            TestOpcUaModelBrowseFile,
            TestOpcUaModelArchiver,
            TestOpcUaModelWriter,
            TestOpcUaModelDesign,
            TestBrowseServer,
        }

        /// <summary>
        /// Test client entry point
        /// </summary>
        public static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException +=
                (s, e) => Console.WriteLine("unhandled: " + e.ExceptionObject);
            var op = Op.None;
            var endpoint = new EndpointModel();
            string fileName = null;
            var host = Utils.GetHostName();
            var ports = new List<int>();
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
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException("Help");
                        default:
                            throw new ArgumentException($"Unknown {args[i]}");
                    }
                }
                if (op == Op.None) {
                    if (ports.Count == 0) {
                        var envPort = Environment.GetEnvironmentVariable("SERVER_PORT");
                        if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out var port)) {
                            ports.Add(port);
                        }
                        else {
                            throw new ArgumentException(
                                "Missing port to run sample server or specify --sample option.");
                        }
                    }
                    op = Op.RunSampleServer;
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Test server host
usage:       [options] operation [args]

Options:

    --port / -p             Port to listen on
    --help / -? / -h        Prints out this help.

Operations (Mutually exclusive):

    --sample / -s           Run sample server and wait for cancellation.
                            Default if port is specified.

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
                        RunServerAsync(ports).Wait();
                        return;
                    case Op.TestOpcUaServerClient:
                        TestOpcUaServerClientAsync(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelBrowseEncoder:
                        TestOpcUaModelExportServiceAsync(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelBrowseFile:
                        TestOpcUaModelExportToFileAsync(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelArchiver:
                        TestOpcUaModelArchiveAsync(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelWriter:
                        TestOpcUaModelWriterAsync(endpoint).Wait();
                        break;
                    case Op.TestOpcUaModelDesign:
                        TestOpcUaModelDesignAsync(fileName).Wait();
                        break;
                    case Op.TestBrowseServer:
                        TestBrowseServerAsync(endpoint).Wait();
                        break;
                    default:
                        throw new ArgumentException("Unknown.");
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return;
            }

            Console.WriteLine("Press key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Run server until exit
        /// </summary>
        private static async Task RunServerAsync(IEnumerable<int> ports) {
            using (var logger = StackLogger.Create(ConsoleLogger.Create())) {
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
                    logger.Logger.Information("Exiting.");
                }
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
            public async Task SendEventAsync(byte[] data, string contentType,
                string eventSchema, string contentEncoding) {
                var ev = JsonConvert.DeserializeObject<DiscoveryEventModel>(
                    Encoding.UTF8.GetString(data));
                var endpoint = ev.Registration?.Endpoint;
                if (endpoint == null) {
                    return;
                }
                try {
                    var id = endpoint.Url.ToSha1Hash();
                    _logger.Information("Writing {id}.json for {@ev}", id, ev);
                    using (var writer = File.CreateText($"iop_{id}.json"))
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

            /// <inheritdoc/>
            public async Task SendEventAsync(IEnumerable<byte[]> batch, string contentType,
                string eventSchema, string contentEncoding) {
                foreach (var item in batch) {
                    await SendEventAsync(item, contentType, eventSchema, contentEncoding);
                }
            }

            /// <inheritdoc/>
            public Task ReportAsync(string propertyId, VariantValue value) {
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task ReportAsync(IEnumerable<KeyValuePair<string, VariantValue>> properties) {
                return Task.CompletedTask;
            }

            private readonly ILogger _logger;
            private readonly ClientServices _client;
        }

        /// <summary>
        /// Test model browse encoder
        /// </summary>
        private static async Task TestOpcUaModelExportServiceAsync(EndpointModel endpoint) {
            using (var logger = StackLogger.Create(ConsoleLogger.Create()))
            using (var client = new ClientServices(logger.Logger, new TestClientServicesConfig()))
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
        private static async Task TestOpcUaModelArchiveAsync(EndpointModel endpoint) {
            using (var logger = StackLogger.Create(ConsoleLogger.Create())) {
                var storage = new ZipArchiveStorage();
                var fileName = "tmp.zip";
                using (var client = new ClientServices(logger.Logger, new TestClientServicesConfig()))
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
        }

        /// <summary>
        /// Test model browse encoder to file
        /// </summary>
        private static async Task TestOpcUaModelExportToFileAsync(EndpointModel endpoint) {
            using (var logger = StackLogger.Create(ConsoleLogger.Create())) {
                // Run both encodings twice to prime server and get realistic timings the
                // second time around
                var runs = new Dictionary<string, string> {
                    ["json1.zip"] = ContentMimeType.UaJson,
                    //  ["bin1.zip"] = ContentEncodings.MimeTypeUaBinary,
                    ["json2.zip"] = ContentMimeType.UaJson,
                    //  ["bin2.zip"] = ContentEncodings.MimeTypeUaBinary,
                    ["json1.gzip"] = ContentMimeType.UaJson,
                    //  ["bin1.gzip"] = ContentEncodings.MimeTypeUaBinary,
                    ["json2.gzip"] = ContentMimeType.UaJson,
                    // ["bin2.gzip"] = ContentEncodings.MimeTypeUaBinary
                };

                using (var client = new ClientServices(logger.Logger, new TestClientServicesConfig()))
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
        }

        /// <summary>
        /// Test model export and import
        /// </summary>
        private static async Task TestOpcUaModelWriterAsync(EndpointModel endpoint) {
            using (var logger = StackLogger.Create(ConsoleLogger.Create())) {
                var filename = "model.zip";
                using (var server = new ServerWrapper(endpoint, logger)) {
                    using (var client = new ClientServices(logger.Logger, new TestClientServicesConfig())) {
                        Console.WriteLine($"Reading into {filename}...");
                        using (var stream = new FileStream(filename, FileMode.Create)) {
                            using (var zipped = new DeflateStream(stream, CompressionLevel.Optimal))
                            using (var browser = new BrowseStreamEncoder(client, endpoint, zipped,
                                ContentMimeType.UaJson, null, logger.Logger, null)) {
                                await browser.EncodeAsync(CancellationToken.None);
                            }
                        }
                    }
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                var serializer = new NewtonSoftJsonSerializer();
                IDatabaseServer database = new MemoryDatabase(logger.Logger, serializer);
                for (var i = 0; ; i++) {
                    Console.WriteLine($"{i}: Writing from {filename}...");
                    var sw = Stopwatch.StartNew();
                    using (var file = File.Open(filename, FileMode.OpenOrCreate)) {
                        using (var unzipped = new DeflateStream(file, CompressionMode.Decompress)) {
                           // TODO
                           // var writer = new SourceStreamImporter(new ItemContainerFactory(database),
                           //     new VariantEncoderFactory(), logger.Logger);
                           // await writer.ImportAsync(unzipped, Path.GetFullPath(filename + i),
                           //     ContentMimeType.UaJson, null, CancellationToken.None);
                        }
                    }
                    var elapsed = sw.Elapsed;
                    Console.WriteLine($"{i}: Writing took {elapsed}.");
                }
            }
        }

        /// <summary>
        /// Test client
        /// </summary>
        private static async Task TestOpcUaServerClientAsync(EndpointModel endpoint) {
            using (var logger = StackLogger.Create(ConsoleLogger.Create()))
            using (var client = new ClientServices(logger.Logger, new TestClientServicesConfig()))
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
        private static Task TestOpcUaModelDesignAsync(string designFile) {
            if (string.IsNullOrEmpty(designFile)) {
                throw new ArgumentException(nameof(designFile));
            }
            var design = Model.Load(designFile, new CompositeModelResolver());
            design.Save(Path.GetDirectoryName(designFile));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Test address space control
        /// </summary>
        private static async Task TestBrowseServerAsync(EndpointModel endpoint, bool silent = false) {

            using (var logger = StackLogger.Create(ConsoleLogger.Create())) {

                var request = new BrowseRequestModel {
                    TargetNodesOnly = false
                };
                var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    ObjectIds.RootFolder.ToString()
                };

                using (var client = new ClientServices(logger.Logger, new TestClientServicesConfig())) {
                    var service = new AddressSpaceServices(client, new VariantEncoderFactory(), logger.Logger);
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
                                    Console.WriteLine(JsonConvert.SerializeObject(result,
                                        Formatting.Indented));
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
                                        r.Target.NodeClass.Value != Core.Models.NodeClass.Variable) {
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
                                            Console.WriteLine(JsonConvert.SerializeObject(result,
                                                Formatting.Indented));
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
                    _server = RunSampleServerAsync(_cts.Token, logger.Logger);
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
                _cts.Dispose();
            }

            /// <summary>
            /// Run server until cancelled
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private static async Task RunSampleServerAsync(CancellationToken ct, ILogger logger) {
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
    }
}
