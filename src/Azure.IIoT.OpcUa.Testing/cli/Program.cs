// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Testing.Cli
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Sample;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Logging;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Test client for opc ua services
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Test client entry point
        /// </summary>
        public static void Main(string[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }
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
        }

        /// <summary>
        /// Run server until exit
        /// </summary>
        private static async Task RunServerAsync(IEnumerable<int> ports)
        {
            var logger = StackLogger.Create(Log.Console<StackLogger>());
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
            using (var server = new ServerConsoleHost(new ServerFactory(logger.Logger),
                logger.Logger)
            {
                AutoAccept = true
            })
            {
                await server.StartAsync(ports).ConfigureAwait(false);
#if DEBUG
                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.TreatControlCAsInput = true;
                    await Task.WhenAny(tcs.Task, Task.Run(() => Console.ReadKey())).ConfigureAwait(false);
                    return;
                }
#endif
                await tcs.Task.ConfigureAwait(false);
                logger.Logger.LogInformation("Exiting.");
            }
        }

        /// <summary>
        /// Wraps server and disposes after use
        /// </summary>
        private class ServerWrapper : IDisposable
        {
            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="endpoint"></param>
            public ServerWrapper(EndpointModel endpoint, StackLogger logger)
            {
                _cts = new CancellationTokenSource();
                if (endpoint.Url == null)
                {
                    _server = RunSampleServerAsync(logger.Logger, _cts.Token);
                    endpoint.Url = "opc.tcp://" + Utils.GetHostName() +
                        ":51210/UA/SampleServer";
                }
                else
                {
                    _server = Task.CompletedTask;
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _cts.Cancel();
                _server.Wait();
                _cts.Dispose();
            }

            /// <summary>
            /// Run server until cancelled
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private static async Task RunSampleServerAsync(ILogger logger, CancellationToken ct)
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                ct.Register(() => tcs.TrySetResult(true));
                using (var server = new ServerConsoleHost(new ServerFactory(logger)
                {
                    LogStatus = false
                }, logger)
                {
                    AutoAccept = true
                })
                {
                    await server.StartAsync(new List<int> { 51210 }).ConfigureAwait(false);
                    await tcs.Task.ConfigureAwait(false);
                }
            }

            private readonly CancellationTokenSource _cts;
            private readonly Task _server;
        }
    }
}
