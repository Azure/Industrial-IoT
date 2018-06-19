// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.Cli {
    using Microsoft.Azure.IIoT.Net.Dhcp.Shared;
    using Microsoft.Azure.IIoT.Net.Dhcp.v4;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Run dhcp command line interface
    /// </summary>
    public class Program {

        enum Op {
            None,
            Client,
            Server
        }

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static void Main(string[] args) {
            var op = Op.None;
            var listenOnly = true;

            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "--active":
                        case "-a":
                            listenOnly = false;
                            break;
                        case "-s":
                        case "--server":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.Server;
                            break;
                        case "-c":
                        case "--client":
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutually exclusive");
                            }
                            op = Op.Client;
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
                    op = Op.Server;
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Dhcp Command line interface
usage:       Dhcp [options] operation [args]

Options:

    --help
     -?
     -h      Prints out this help.

Operations (Mutually exclusive):
     -c
    --client
             Tests client operation.
     -s
    --server
             Tests server operation.
"
                    );
                return;
            }

            try {
                Console.WriteLine($"Running {op}...");
                switch(op) {
                    case Op.Client:
                        TestClient().Wait();
                        break;
                    case Op.Server:
                        TestServer(listenOnly).Wait();
                        break;
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Test client
        /// </summary>
        /// <returns></returns>
        private static Task TestClient() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test server
        /// </summary>
        /// <returns></returns>
        private static async Task TestServer(bool listenOnly) {

            var logger = new ConsoleLogger("DHCPSERVER", LogLevel.Debug);
            var config = new DhcpServerDefaults {
                ListenOnly = listenOnly
            };

            using (var host = new DhcpServerHost(
                new DhcpServer(logger, config), logger, config)) {

                await host.StartAsync();

                Console.WriteLine("Press any key to stop");
                Console.ReadKey();
            }
        }
    }
}
