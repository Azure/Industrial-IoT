// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Cli {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Client;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Runtime;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Sample program to run imports
    /// </summary>
    public class Program {

        /// <summary>
        /// Test configuration
        /// </summary>
        private class OpcUaTestConfig : IOpcUaServicesConfig {
            public string IoTHubConnString => Environment.GetEnvironmentVariable("_HUB_CS");
            public string IoTHubManagerV1ApiUrl { get; set; }
            public bool BypassProxy { get; set; }
        }

        enum Op {
            None,
            TestOpcUaServerClient
        }

        /// <summary>
        /// Importer entry point
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static void Main(string[] args) {
            var op = Op.None;
            var proxy = false;
            var endpoint = new ServerEndpointModel { IsTrusted = true };

            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "-s":
                        case "--server":
                            i++;
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestOpcUaServerClient;
                            if (i < args.Length) {
                                endpoint.Url = args[i];
                            }
                            else {
                                endpoint.Url = "opc.tcp://" + Utils.GetHostName() + ":51210/UA/SampleServer";
                            }
                            break;
                        case "-p":
                        case "--proxy":
                            proxy = true;
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

Target (Mutually exclusive):

     -p
    --proxy 
             Use proxy

Operations (Mutually exclusive):

     -s
    --server
             Tests server stuff with passed endpoint url.  The server is queried 
             or browsed to gather all nodes to import.
"
                    );
                return;
            }

            try {
                Console.WriteLine($"Running {op}...");
                switch(op) {
                    case Op.TestOpcUaServerClient:
                        TestOpcUaServerClient(endpoint, proxy).Wait();
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
        /// Test client stuff
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="useProxy"></param>
        private static async Task TestOpcUaServerClient(ServerEndpointModel endpoint, 
            bool useProxy) {
            var logger = new Logger("test", LogLevel.Debug);
            var client = new OpcUaServerClient(logger, new OpcUaTestConfig {
                BypassProxy = !useProxy,
                IoTHubManagerV1ApiUrl = null
            });
            await client.ExecuteServiceAsync(endpoint, async session => {

                Console.WriteLine("Browse the OPC UA server namespace.");
                var w = Stopwatch.StartNew();
                var stack = new Stack<Tuple<string, ReferenceDescription>>();
                session.Browse(null, null, ObjectIds.ObjectsFolder,
                    0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                    true, (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                    out var continuationPoint, out var references);
                Console.WriteLine(" DisplayName, BrowseName, NodeClass");
                references.Reverse();
                foreach (var rd in references) {
                    stack.Push(Tuple.Create("", rd));
                }
                while (stack.Count > 0) {
                    var browsed = stack.Pop();
                    session.Browse(null, null, 
                        ExpandedNodeId.ToNodeId(browsed.Item2.NodeId, session.NamespaceUris),
                        0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                        true, (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method,
                        out continuationPoint, out references);
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
    }
}
