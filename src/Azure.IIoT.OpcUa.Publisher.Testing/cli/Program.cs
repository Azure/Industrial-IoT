// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Cli
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Logging;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Loader;
    using System.Threading.Tasks;

    /// <summary>
    /// Test client for opc ua services
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Test client entry point
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (s, e) => Console.WriteLine("unhandled: " + e.ExceptionObject);
            var host = Utils.GetHostName();
            var ports = new List<int>();
            try
            {
                for (var i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--sample":
                        case "-s":
                            break;
                        case "-p":
                        case "--port":
                            i++;
                            if (i < args.Length)
                            {
                                ports.Add(ushort.Parse(args[i], CultureInfo.InvariantCulture));
                                break;
                            }
                            throw new ArgumentException(
                                "Missing arguments for port option");
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException("Help");
                        default:
                            throw new ArgumentException($"Unknown {args[i]}");
                    }
                }
                if (ports.Count == 0)
                {
                    var envPort = Environment.GetEnvironmentVariable("SERVER_PORT");
                    if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out var port))
                    {
                        ports.Add(port);
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Missing port to run sample server or specify --sample option.");
                    }
                }
            }
            catch (Exception e)
            {
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
"
                    );
                return;
            }

            if (ports.Count == 0)
            {
                ports.Add(51210);
            }
            try
            {
                Console.WriteLine("Running ...");
                RunServerAsync(ports).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            Console.WriteLine("Press key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Run server until exit
        /// </summary>
        /// <param name="ports"></param>
        private static async Task RunServerAsync(IEnumerable<int> ports)
        {
            var logger = Log.Console<ServerConsoleHost>();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
            using var server = new ServerConsoleHost(new TestServerFactory(Log.Console<TestServerFactory>()), logger)
            {
                AutoAccept = true
            };
            await server.StartAsync(ports).ConfigureAwait(false);
#if DEBUG
            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("Press any key to exit...");
                Console.TreatControlCAsInput = true;
                await Task.WhenAny(tcs.Task, Task.Run(Console.ReadKey)).ConfigureAwait(false);
                return;
            }
#endif
            await tcs.Task.ConfigureAwait(false);
            logger.LogInformation("Exiting.");
        }
    }
}
