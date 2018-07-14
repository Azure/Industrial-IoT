// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Simulation.Cli {
    using Autofac;
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Edge.Simulation.Azure;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Management.Auth;
    using Microsoft.Azure.IIoT.Management.Runtime;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

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
            var simulator = context.Resolve<ISimulator>();
            var interactive = true;
            do {
                if (interactive) {
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
                        case "logout":
                        case "exit":
                            interactive = false;
                            await simulator.StopAsync();
                            break;
                        case "login":
                            await simulator.StartAsync();
                            interactive = true;
                            break;
                        case "get":
                            await GetSimulationAsync(simulator, options);
                            break;
                        case "list":
                            await ListSimulationsAsync(simulator, options);
                            break;
                        case "new":
                            await CreateSimulationAsync(simulator, options);
                            break;
                        case "delete":
                            await DeleteSimulationAsync(simulator, options);
                            break;
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
                    if (!interactive) {
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
            while (interactive);
        }

        /// <summary>
        /// Delete simulation
        /// </summary>
        /// <param name="simulator"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task DeleteSimulationAsync(ISimulator simulator,
            Dictionary<string, string> options) {
            await simulator.DeleteAsync(GetOption<string>(options, "-i", "--id"));
        }

        /// <summary>
        /// Create simulation
        /// </summary>
        /// <param name="simulator"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task CreateSimulationAsync(ISimulator simulator,
            Dictionary<string, string> options) {
            var simulation = await simulator.CreateAsync(new Dictionary<string, JToken> {
                ["tag"] = GetOption(options, "-t", "--tag", "none")
            });
            PrintResult(options, simulation);
        }

        /// <summary>
        /// List all active simulations
        /// </summary>
        /// <param name="simulator"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ListSimulationsAsync(ISimulator simulator,
            Dictionary<string, string> options) {
            var simulations = await simulator.ListAsync();
            PrintResult(options, simulations);
        }

        /// <summary>
        /// Get simulation
        /// </summary>
        /// <param name="simulator"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetSimulationAsync(ISimulator simulator,
            Dictionary<string, string> options) {
            var simulation = await simulator.GetAsync(
                GetOption<string>(options, "-i", "--id"));
            PrintResult(options, simulation);
        }

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
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(
                new SimulatorConfig {
                    Configuration = configuration
                })
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.RegisterType<ConsoleLogger>()
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
            builder.RegisterType<AzureManagement>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AzureBasedSimulator>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ConsoleSelector>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DeviceCodeCredentials>()
                .AsImplementedInterfaces().SingleInstance();

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

     login       Login and start simulator.

     exit        Stop and exit simulator.

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
