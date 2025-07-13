// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Cli
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Logging;
    using k8s;
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
            var runsInKubenetes = KubernetesClientConfiguration.IsInCluster();
            var ports = new List<int>();
            var server = "sample";
            try
            {
                for (var i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-server":
                        case "-s":
                            i++;
                            if (i < args.Length)
                            {
                                server = args[i];
                                break;
                            }
                            throw new ArgumentException(
                                "Missing arguments for server option");
                        case "-hosts":
                        case "-H":
                            i++;
                            if (i < args.Length)
                            {
                                host = args[i];
                                break;
                            }
                            throw new ArgumentException(
                                "Missing arguments for host option");
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

    --hosts / -H            Full host name to use with comma seperated alternative hosts.
    --port / -p             Port to listen on.
    --help / -? / -h        Prints out this help.

Operations (Mutually exclusive):

    --server / -s           Run server with the given name (e.g. sample, testdata, etc.)
"
                    );
                return;
            }

            if (ports.Count == 0)
            {
                ports.Add(runsInKubenetes ? 50000 : 51210);
            }
            try
            {
                var hosts = host.Split(',');
                var alternativeHosts = new List<string>();
                if (hosts.Length > 1)
                {
                    alternativeHosts.AddRange(hosts[1..]);
                }
                Console.WriteLine("Running ...");
                RunServerAsync(server, hosts[0], ports, alternativeHosts, runsInKubenetes).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            if (!runsInKubenetes)
            {
                Console.WriteLine("Press key to exit...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Run server until exit
        /// </summary>
        /// <param name="serverType"></param>
        /// <param name="host"></param>
        /// <param name="ports"></param>
        /// <param name="alternativeHosts"></param>
        /// <param name="runsInKubernetes"></param>
        private static async Task RunServerAsync(string serverType, string host, IEnumerable<int> ports,
            List<string> alternativeHosts, bool runsInKubernetes)
        {
            var logger = Log.Console<ServerConsoleHost>();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
            if (runsInKubernetes)
            {
                // Register FlatDirectoryCertificateStoreType as known certificate store type.
                var certStoreTypeName = CertificateStoreType.GetCertificateStoreTypeByName(
                    FlatCertificateStore.StoreTypeName);
                if (certStoreTypeName is null)
                {
                    CertificateStoreType.RegisterCertificateStoreType(
                        FlatCertificateStore.StoreTypeName, new FlatCertificateStore());
                }
            }
            using var server = new ServerConsoleHost(
                TestServerFactory.Create(serverType, Log.Console<TestServerFactory>()), logger)
            {
                HostName = host,
                AlternativeHosts = alternativeHosts,
                UriPath = runsInKubernetes ? string.Empty : null,
                CertStoreType = runsInKubernetes ? FlatCertificateStore.StoreTypeName : null,
                PkiRootPath = runsInKubernetes ? FlatCertificateStore.StoreTypePrefix + "pki" : null,
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
