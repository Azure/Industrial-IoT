// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Servers.Cli {
    using Microsoft.Azure.IIoT.OpcUa.Servers.Sample;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.Loader;

    /// <summary>
    /// Simple server host runner
    /// </summary>
    public class Program {

        enum Op {
            None,
            RunSampleServer,

            // ...
        }

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static void Main(string[] args) {
            var op = Op.None;
            var ports = new List<int> { 51210 };
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
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException(
                                "Help");
                        default:
                            throw new ArgumentException(
                                $"Unknown {args[i]}");
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
Usage:       Microsoft.Azure.IIoT.OpcUa.Servers.Cli [options] operation [args]

Operations (Mutually exclusive):

    --sample <port>
             Run opc ua sample server instance

Options:
     -p
    --port
             Port to run the server on.  Can be specified multiple times.

    --help
     -?
     -h      Prints out this help.
"
                    );
                return;
            }
            try {
                switch(op) {
                    case Op.RunSampleServer:
                        RunSampleServer(ports).Wait();
                        break;
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Run server until exit
        /// </summary>
        /// <returns></returns>
        private static async Task RunSampleServer(IEnumerable<int> ports) {
            var logger = new ConsoleLogger(null, LogLevel.Debug);

            var tcs = new TaskCompletionSource<bool>();
            AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
            using (var server = new SampleServerHost(logger) {
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
}
