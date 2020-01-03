// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Cli {
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Net;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// Networking command line interface
    /// </summary>
    public class Program {
        private enum Op {
            None,
            TestNetworkScanner,
            TestPortScanner
        }

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static void Main(string[] args) {
            var op = Op.None;
            var host = Dns.GetHostName();

            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
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
                        case "-n":
                        case "--net":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.TestNetworkScanner;
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
Net Client
usage:       Net.Client [options] operation [args]

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
"
                    );
                return;
            }

            try {
                Console.WriteLine($"Running {op}...");
                switch (op) {
                    case Op.TestNetworkScanner:
                        TestNetworkScannerAsync().Wait();
                        break;
                    case Op.TestPortScanner:
                        while (true) {
                            TestPortScannerAsync(host).Wait();
                        }
                    //break;
                    default:
                        throw new ArgumentException("Unknown.");
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            DumpMemory();
            Console.WriteLine("Press key to exit...");
            Console.ReadKey();
        }


        /// <summary>
        /// Test port scanning
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private static async Task TestPortScannerAsync(string host) {
            var logger = ConsoleOutLogger.Create();
            var addresses = await Dns.GetHostAddressesAsync(host);
            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10))) {
                var watch = Stopwatch.StartNew();
                var scanning = new ScanServices(logger);
                DumpMemory();
                var results = await scanning.ScanAsync(
                    PortRange.All.SelectMany(r => r.GetEndpoints(addresses.First())),
                    cts.Token);
                foreach (var result in results) {
                    Console.WriteLine($"Found {result} open.");
                }
                Console.WriteLine($"Scan took: {watch.Elapsed}");
                DumpMemory();
            }
        }

        /// <summary>
        /// Test network scanning
        /// </summary>
        /// <returns></returns>
        private static async Task TestNetworkScannerAsync() {
            var logger = ConsoleOutLogger.Create();
            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10))) {
                var watch = Stopwatch.StartNew();
                var scanning = new ScanServices(logger);
                DumpMemory();
                var results = await scanning.ScanAsync(NetworkClass.Wired, cts.Token);
                foreach (var result in results) {
                    Console.WriteLine($"Found {result.Address}...");
                }
                Console.WriteLine($"Scan took: {watch.Elapsed}");
                DumpMemory();
            }
        }

        /// <summary>
        /// Write memory dump
        /// </summary>
        private static void DumpMemory() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine($"GC Mem: {GC.GetTotalMemory(false) / 1024} kb, Working set /" +
              $" Private Mem: {Process.GetCurrentProcess().WorkingSet64 / 1024} kb / " +
              $"{Process.GetCurrentProcess().PrivateMemorySize64 / 1024} kb, Handles:" +
              Process.GetCurrentProcess().HandleCount);
        }
    }
}
