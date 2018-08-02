// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Simulation.Cli {
    using Microsoft.Azure.IIoT.Simulation.Services;
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Infrastructure.Compute.Services;
    using Microsoft.Azure.IIoT.Infrastructure.Hub.Services;
    using Microsoft.Azure.IIoT.Infrastructure.Runtime;
    using Microsoft.Azure.IIoT.Infrastructure.Services;
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Ssh;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Api command line interface
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true)
                .Build();

            // Set up dependency injection for the module host
            var container = ConfigureContainer(config);

            using (var scope = container.BeginLifetimeScope()) {
                RunAsync(args, scope).Wait();
            }
        }

        /// <summary>
        /// Run client
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static async Task RunAsync(string[] args, IComponentContext context) {
            var simulator = context.Resolve<ISimulationHost>();
            var run = true;
            do {
                if (run) {
                    Console.Write("> ");
                    args = Console.ReadLine().ParseAsCommandLine();
                }
                try {
                    if (args.Length < 1) {
                        throw new ArgumentException("Need a command!");
                    }
                    var command = args[0].ToLowerInvariant();
                    var options = CollectOptions(1, args);
                    switch (command) {
                        case "exit":
                            run = false;
                            break;
                        case "shell":
                            await RunSimulationShellAsync(simulator, options);
                            break;
                        case "restart":
                            await RestartSimulationAsync(simulator);
                            break;
                        //   case "logs":
                        //       await GetEdgeLogsAsync(simulator, options);
                        //       break;
                        //   case "status":
                        //       await GetEdgeStatusAsync(simulator, options);
                        //       break;
                        //   case "reset":
                        //       await ResetEdgeAsync(simulator, options);
                        //       break;
                        //   case "list":
                        //       await ListSimulationsAsync(simulator, options);
                        //       break;
                        //   case "create":
                        //       await CreateSimulationAsync(simulator, options);
                        //       break;
                        //   case "delete":
                        //       await DeleteSimulationAsync(simulator, options);
                        //       break;
                        case "-?":
                        case "-h":
                        case "--help":
                        case "help":
                            PrintHelp();
                            break;
                        default:
                            throw new ArgumentException($"Unknown command {command}.");
                    }
                }
                catch (ArgumentException e) {
                    Console.WriteLine(e.Message);
                    if (!run) {
                        PrintHelp();
                        return;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("==================");
                    Console.WriteLine(e);
                    Console.WriteLine("==================");
                }
            }
            while (run);
        }

        /// <summary>
        /// Shell into simulation
        /// </summary>
        /// <param name="simulation"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task RunSimulationShellAsync(ISimulationHost simulation,
            Dictionary<string, string> options) {
            using (var shell = await simulation.OpenSecureShellAsync()) {
                var timeout = GetOption(options, "-t", "--timeout", -1);
                var cts = new CancellationTokenSource(
                    GetOption(options, "-t", "--timeout", -1));
                await shell.BindAsync(cts.Token);
            }
        }

        /// <summary>
        /// Restart simulation
        /// </summary>
        /// <param name="simulation"></param>
        /// <returns></returns>
        private static async Task RestartSimulationAsync(ISimulationHost simulation) {
            await simulation.RestartAsync();
            Console.WriteLine("Simulation restarted");
        }

     // /// <summary>
     // /// Reset Edge
     // /// </summary>
     // /// <param name="simulator"></param>
     // /// <param name="options"></param>
     // /// <returns></returns>
     // private static async Task ResetEdgeAsync(ISimulationHost simulator,
     //     Dictionary<string, string> options) {
     //     if (simulator == null) {
     //         throw new ArgumentException("Must login first");
     //     }
     //     var simulation = await simulator.GetAsync(
     //         GetOption<string>(options, "-i", "--id"));
     //     if (simulation == null) {
     //         return;
     //     }
     //     await simulation.ResetEdgeAsync();
     //     Console.WriteLine("Gateway reset");
     // }
     //
     // /// <summary>
     // /// Get edge daemon status
     // /// </summary>
     // /// <param name="simulator"></param>
     // /// <param name="options"></param>
     // /// <returns></returns>
     // private static async Task GetEdgeStatusAsync(ISimulationHost simulator,
     //     Dictionary<string, string> options) {
     //     if (simulator == null) {
     //         throw new ArgumentException("Must login first");
     //     }
     //     var simulation = await simulator.GetAsync(
     //         GetOption<string>(options, "-i", "--id"));
     //     if (simulation == null) {
     //         return;
     //     }
     //     var active = await simulation.IsEdgeRunningAsync();
     //     Console.WriteLine("Edge is " + (active ? "active" : "not active"));
     //     var connected = await simulation.IsEdgeConnectedAsync();
     //     Console.WriteLine("Edge is " + (connected ? "connected" : "not connected"));
     // }
     //
     // /// <summary>
     // /// Get edge logs
     // /// </summary>
     // /// <param name="simulator"></param>
     // /// <param name="options"></param>
     // /// <returns></returns>
     // private static async Task GetEdgeLogsAsync(ISimulationHost simulator,
     //     Dictionary<string, string> options) {
     //     if (simulator == null) {
     //         throw new ArgumentException("Must login first");
     //     }
     //     var simulation = await simulator.GetAsync(
     //         GetOption<string>(options, "-i", "--id"));
     //     if (simulation == null) {
     //         return;
     //     }
     //     var logs = await simulation.GetEdgeLogAsync();
     //     Console.Write(logs);
     // }

        /// <summary>
        /// Print result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <param name="status"></param>
        private static void PrintResult<T>(Dictionary<string, string> options,
            T status) {
            Console.WriteLine("==================");
            Console.WriteLine(JsonConvert.SerializeObject(status,
                GetOption(options, "-F", "--format", Formatting.Indented)));
            Console.WriteLine("==================");
        }

        /// <summary>
        /// Get option value
        /// </summary>
        /// <param name="options"></param>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static T GetOption<T>(Dictionary<string, string> options,
            string key1, string key2, T defaultValue) {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                return defaultValue;
            }
            return value.As<T>();
        }

        /// <summary>
        /// Get mandatory option value
        /// </summary>
        /// <param name="options"></param>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        private static T GetOption<T>(Dictionary<string, string> options,
            string key1, string key2) {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                throw new ArgumentException($"Missing {key1}/{key2} option.");
            }
            return value.As<T>();
        }

        /// <summary>
        /// Get mandatory option value
        /// </summary>
        /// <param name="options"></param>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        private static T? GetOption<T>(Dictionary<string, string> options,
            string key1, string key2, T? defaultValue) where T : struct {
            if (!options.TryGetValue(key1, out var value) &&
                !options.TryGetValue(key2, out value)) {
                return defaultValue;
            }
            if (typeof(T).IsEnum) {
                return Enum.Parse<T>(value, true);
            }
            return value.As<T>();
        }

        /// <summary>
        /// Helper to collect options
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Dictionary<string, string> CollectOptions(int offset,
            string[] args) {
            var options = new Dictionary<string, string>();
            for (var i = offset; i < args.Length;) {
                var key = args[i];
                if (key[0] != '-') {
                    throw new ArgumentException($"{key} is not an option.");
                }
                i++;
                if (i == args.Length) {
                    options.Add(key, "true");
                    break;
                }
                var val = args[i];
                if (val[0] == '-') {
                    // An option, so previous one is a boolean option
                    options.Add(key, "true");
                    continue;
                }
                options.Add(key, val);
                i++;
            }
            return options;
        }

        /// <summary>
        /// Simulator configuration - wraps a configuration root
        /// </summary>
        private class SimulatorConfig : IIoTHubConfig, IClientConfig {
            public IConfigurationRoot Configuration { get; set; }

            /// <summary>
            /// Client id with permissions to Windows Azure Management
            /// Service API.  The registration manifest must contain
            /// the following entry
            ///
            /// "resourceAppId": "797f4846-ba00-4fd7-ba43-dac1f8f63013",
            /// "resourceAccess": [
            ///     {
            ///         "id": "41094075-9dad-400e-a0bd-54e686782033",
            ///         "type": "Scope"
            ///     }
            /// ]
            /// </summary>
            public string ClientId =>
                Configuration.GetValue("IIOT_MANAGEMENT_CLIENT_ID",
                Configuration.GetValue<string>("IIOT_AUTH_CLIENT_ID"));
            public string TenantId =>
                Configuration.GetValue<string>("IIOT_AAD_TENANT_ID");
            public string Authority =>
                null; // default
            public string ClientSecret =>
                null; // not required
            public string IoTHubConnString =>
                Configuration.GetValue<string>("_HUB_CS", null);
            public string IoTHubResourceId =>
                null; // not required
        }

        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        /// <returns></returns>
        private static IContainer ConfigureContainer(
            IConfigurationRoot configuration) {

            var config = new SimulatorConfig {
                Configuration = configuration
            };

            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.RegisterType<ConsoleLogger>()
                .AsImplementedInterfaces().SingleInstance();

            // Register infrastructure code
            builder.RegisterType<ResourceGroupFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AzureSubscription>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ConsoleSelector>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VirtualMachineFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SshShellFactory>()
                .AsImplementedInterfaces().SingleInstance();

            // Register infrastructure code
            builder.RegisterType<ResourceGroupFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AzureSubscription>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ConsoleSelector>()
                .AsImplementedInterfaces().SingleInstance();

            // Register http client implementation
            builder.RegisterType<HttpClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HttpClientFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HttpHandlerFactory>()
                .AsImplementedInterfaces().SingleInstance();

            // Register simulator
            builder.RegisterType<IoTHubServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EdgeSimulator>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<FilePersistance>()
                .AsImplementedInterfaces().SingleInstance();

            if (config.ClientId == null ||
                System.Diagnostics.Debugger.IsAttached) {
                builder.RegisterType<VisualStudioCredentials>()
                    .AsImplementedInterfaces().SingleInstance();
            }
            else {
                builder.RegisterType<DeviceCodeCredentials>()
                    .AsImplementedInterfaces().SingleInstance();
            }
            return builder.Build();
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintHelp() {
            Console.WriteLine(
                @"
Simulator cli - Allows to control the simulatator
usage:      Simulator command [options]

Commands and Options

     login       Login and start simulator
     logout      Stop and exit simulator.

     list        List simulations
        with ...
        -F, --format    Json format for result

     create      Create new simulation
        with ...
        -t, --tag       Tag to use for the simulation.
        -F, --format    Json format for result

     get         Get simulation
        with ...
        -i, --id        Id of simulation to get (mandatory)
        -F, --format    Json format for result

     shell       Open a shell into the simulation
        with ...
        -i, --id        Id of simulation to open shell 
                        for (mandatory)

     delete      Delete simulation
        with ...
        -i, --id        Id of simulation to delete (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }
    }
}
