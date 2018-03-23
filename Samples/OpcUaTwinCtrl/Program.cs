// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Cli {
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Services;
    using Microsoft.Azure.IoTSolutions.Shared.Runtime;
    using Microsoft.Azure.IoTSolutions.Common.Http;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Autofac;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Sample program to run imports
    /// </summary>
    public class Program {

        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IContainer ConfigureContainer(IConfigurationRoot configuration) {
            var builder = new ContainerBuilder();

            var config = new Config(configuration);
            // Register logger
            builder.RegisterInstance(config.Logger)
                .AsImplementedInterfaces().SingleInstance();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // Register http client implementation
            builder.RegisterType<HttpClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Register twin services and ...
            builder.RegisterType<OpcTwinServiceClient>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

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
                .AddInMemoryCollection(new Dictionary<string, string> {
                    ["OpcTwinServiceUrl"] = "http://localhost:9042/v1"
                })
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
            var service = context.Resolve<IOpcTwinService>();
            var interactive = false;
            do {
                if (interactive) {
                    Console.Write("> ");
                    args = Console.ReadLine().ParseAsCommandLine();
                }
                try {
                    if (args.Length < 1) {
                        throw new ArgumentException("Need a command!");
                    }

                    Dictionary<string, string> options;
                    var command = args[0].ToLowerInvariant();
                    switch (command) {
                        case "exit":
                            interactive = false;
                            break;
                        case "console":
                            interactive = true;
                            break;
                        case "status":
                            options = CollectOptions(1, args);
                            await GetStatusAsync(service, options);
                            break;
                        case "apps":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = CollectOptions(2, args);
                            switch (command) {
                                case "register":
                                    await RegisterApplicationAsync(service, options);
                                    break;
                                case "add":
                                    await AddServerAsync(service, options);
                                    break;
                                case "update":
                                    await UpdateApplicationAsync(service, options);
                                    break;
                                case "unregister":
                                    await UnregisterApplicationAsync(service, options);
                                    break;
                                case "list":
                                    await ListApplicationsAsync(service, options);
                                    break;
                                case "query":
                                    await QueryApplicationsAsync(service, options);
                                    break;
                                case "get":
                                    await GetApplicationAsync(service, options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintApplicationsHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "twins":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = CollectOptions(2, args);
                            switch (command) {
                                case "update":
                                    await UpdateTwinAsync(service, options);
                                    break;
                                case "get":
                                    await GetTwinAsync(service, options);
                                    break;
                                case "list":
                                    await ListTwinsAsync(service, options);
                                    break;
                                case "query":
                                    await QueryTwinsAsync(service, options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintTwinsHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "supervisors":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = CollectOptions(2, args);
                            switch (command) {
                                case "get":
                                    await GetSupervisorAsync(service, options);
                                    break;
                                case "update":
                                    await UpdateSupervisorAsync(service, options);
                                    break;
                                case "list":
                                    await ListSupervisorsAsync(service, options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintSupervisorsHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "nodes":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = CollectOptions(2, args);
                            switch (command) {
                                case "browse":
                                    await BrowseAsync(service, options);
                                    break;
                                case "publish":
                                    await PublishAsync(service, options);
                                    break;
                                case "nodes":
                                    await ListNodesAsync(service, options);
                                    break;
                                case "read":
                                    await ReadAsync(service, options);
                                    break;
                                case "write":
                                    await WriteAsync(service, options);
                                    break;
                                case "metadata":
                                    await MethodMetadataAsync(service, options);
                                    break;
                                case "call":
                                    await MethodCallAsync(service, options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintNodesHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
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
        /// Call method
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task MethodCallAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.NodeMethodCallAsync(
                GetOption<string>(options, "-i", "--id"),
                new MethodCallRequestApiModel {
                    MethodId = GetOption<string>(options, "-n", "--nodeid"),
                    ObjectId = GetOption<string>(options, "-o", "--objectid")

                    // ...
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task MethodMetadataAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.NodeMethodGetMetadataAsync(
                GetOption<string>(options, "-i", "--id"),
                new MethodMetadataRequestApiModel {
                    MethodId = GetOption<string>(options, "-n", "--nodeid")
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task WriteAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.NodeValueWriteAsync(
                GetOption<string>(options, "-i", "--id"),
                new ValueWriteRequestApiModel {
                    Node = new NodeApiModel {
                        Id = GetOption<string>(options, "-n", "--nodeid"),
                        DataType = GetOption<string>(options, "-t", "--datatype")
                    },
                    Value = GetOption<string>(options, "-v", "--value")
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ReadAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.NodeValueReadAsync(
                GetOption<string>(options, "-i", "--id"),
                new ValueReadRequestApiModel {
                    NodeId = GetOption<string>(options, "-n", "--nodeid")
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Publish node
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task PublishAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.NodePublishAsync(
                GetOption<string>(options, "-i", "--id"),
                new PublishRequestApiModel {
                    NodeId = GetOption<string>(options, "-n", "--nodeid"),
                    Enabled = GetOption(options, "-x", "--delete", false) ? (bool?)null :
                        !GetOption(options, "-d", "--disable", false)
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task BrowseAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.NodeBrowseAsync(
                GetOption<string>(options, "-i", "--id"),
                new BrowseRequestApiModel {
                    NodeId = GetOption<string>(options, "-n", "--nodeid", null),
                    ExcludeReferences = GetOption<bool>(options, "-x", "--norefs", null),
                    IncludePublishingStatus = GetOption<bool>(options, "-p", "--publish", null)
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// List published nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ListNodesAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            if (GetOption(options, "-a", "--all", false)) {
                var result = await service.ListPublishedNodesAsync(
                    GetOption<string>(options, "-i", "--id"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListPublishedNodesAsync(
                    GetOption<string>(options, "-c", "--continuation", null),
                    GetOption<string>(options, "-i", "--id"));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List supervisor registrations
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ListSupervisorsAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            if (GetOption(options, "-a", "--all", false)) {
                var result = await service.ListAllSupervisorsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListSupervisorsAsync(
                    GetOption<string>(options, "-c", "--continuation", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetSupervisorAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.GetSupervisorAsync(
                GetOption<string>(options, "-i", "--id"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Update twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task UpdateSupervisorAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            await service.UpdateSupervisorAsync(
                new SupervisorUpdateApiModel {
                    Id = GetOption<string>(options, "-i", "--id"),
                    Domain = GetOption<string>(options, "-n", "--domain", null),
                    Discovery = GetOption<DiscoveryMode>(options, "-d", "--discovery", null)
                });
        }

        /// <summary>
        /// Registers application
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task RegisterApplicationAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.RegisterAsync(
                new ApplicationRegistrationRequestApiModel {
                    ApplicationUri = GetOption<string>(options, "-u", "--url"),
                    ApplicationName = GetOption<string>(options, "-n", "--name", null),
                    ApplicationType = GetOption<ApplicationType>(options, "-t", "--type", null),
                    ProductUri = GetOption<string>(options, "-p", "--product", null),
                    DiscoveryUrls = new List<string> {
                        GetOption<string>(options, "-d", "--discoveryUrl")
                    }
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Registers server
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task AddServerAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.RegisterAsync(
                new ServerRegistrationRequestApiModel {
                    DiscoveryUrl = GetOption<string>(options, "-u", "--url")
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task UpdateApplicationAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            await service.UpdateApplicationAsync(
                new ApplicationRegistrationUpdateApiModel {
                    Id = GetOption<string>(options, "-i", "--id"),
                    // ...
                    ApplicationName = GetOption<string>(options, "-n", "--name", null)
                });
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static Task UnregisterApplicationAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            return service.UnregisterApplicationAsync(
                GetOption<string>(options, "-i", "--id"));
        }

        /// <summary>
        /// List applications
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ListApplicationsAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            if (GetOption(options, "-a", "--all", false)) {
                var result = await service.ListAllApplicationsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListApplicationsAsync(
                    GetOption<string>(options, "-c", "--continuation", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query applications
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task QueryApplicationsAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var query = new ApplicationRegistrationQueryApiModel {
                ApplicationUri = GetOption<string>(options, "-u", "--uri", null),
                ProductUri = GetOption<string>(options, "-p", "--product", null),
                ApplicationType = GetOption<ApplicationType>(options, "-t", "--type", null),
                ApplicationName = GetOption<string>(options, "-n", "--name", null)
            };
            if (GetOption(options, "-a", "--all", false)) {
                var result = await service.QueryAllApplicationsAsync(query);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.QueryApplicationsAsync(query);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get application
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetApplicationAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.GetApplicationAsync(
                GetOption<string>(options, "-i", "--id"));
            PrintResult(options, result);
        }

        /// <summary>
        /// List twin registrations
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ListTwinsAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            if (GetOption(options, "-a", "--all", false)) {
                var result = await service.ListAllTwinsAsync(
                    GetOption<bool>(options, "-s", "--server", null));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListTwinsAsync(
                    GetOption<string>(options, "-c", "--continuation", null),
                    GetOption<bool>(options, "-s", "--server", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task QueryTwinsAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var query = new TwinRegistrationQueryApiModel {
                Url = GetOption<string>(options, "-u", "--uri", null),
                SecurityMode = GetOption<SecurityMode>(options, "-m", "--mode", null),
                SecurityPolicy = GetOption<string>(options, "-p", "--policy", null),
                IsTrusted = GetOption<bool>(options, "-t", "--trusted", null)
            };
            if (GetOption(options, "-a", "--all", false)) {
                var result = await service.QueryAllTwinsAsync(query,
                    GetOption<bool>(options, "-s", "--server", null));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.QueryTwinsAsync(query,
                    GetOption<bool>(options, "-s", "--server", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetTwinAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.GetTwinAsync(
                GetOption<string>(options, "-i", "--id"),
                GetOption<bool>(options, "-s", "--server", null));
            PrintResult(options, result);
        }

        /// <summary>
        /// Update twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task UpdateTwinAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            await service.UpdateTwinAsync(
                new TwinRegistrationUpdateApiModel {
                    Id = GetOption<string>(options, "-i", "--id"),
                    // ...
                    IsTrusted = GetOption<bool>(options, "-t", "--trusted", null)
                });
        }

        /// <summary>
        /// Get status
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetStatusAsync(IOpcTwinService service,
            Dictionary<string, string> options) {
            var result = await service.GetServiceStatusAsync();
            PrintResult(options, result);
        }

        /// <summary>
        /// Print result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <param name="status"></param>
        private static void PrintResult<T>(Dictionary<string, string> options, T status) {
            Console.WriteLine("==================");
            Console.WriteLine(JsonConvert.SerializeObject(status,
                GetOption(options, "-f", "--format", Formatting.Indented)));
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
            return (T)Convert.ChangeType(value, typeof(T));
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
            return (T)Convert.ChangeType(value, typeof(T));
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
            return (T)Convert.ChangeType(value, typeof(T));
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
        /// Print help
        /// </summary>
        private static void PrintHelp() {
            Console.WriteLine(
                @"
OpcUaTwinCtrl cli - Allows to script opc twin web service api.
usage:      OpcUaTwinCtrl command [options]

Commands and Options

     console     Run in interactive mode. Enter commands after the >
     exit        Exit interactive mode and thus the cli.
     apps        Manage applications
     twins       Manage Twins
     supervisors Manage supervisors
     nodes       Call nodes services on twin
     status      Print service status
     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintApplicationsHelp() {
            Console.WriteLine(
                @"
Manage applications registry.

Commands and Options

     list        List applications
        with ...
        -c, --continuation
                        Continuation from previous result.
        -a, --all       Return all application infos (unpaged)
        -f, --format    Json format for result

     add         Register server and twins through discovery url
        with ...
        -u, --url       Url of the discovery endpoint (mandatory)
        -f, --format    Json format for result

     register    Register Application
        with ...
        -u, --url       Uri of the application (mandatory)
        -n  --name      Application name of the application
        -t, --type      Application type (default to Server)
        -p, --product   Product uri of the application
        -d, --discovery Url of the discovery endpoint
        -f, --format    Json format for result

     query       Find applications
        with ...
        -u, --uri       Application uri of the application 
        -n  --name      Application name of the application
        -t, --type      Application type (default to all)
        -p, --product   Product uri of the application
        -f, --format    Json format for result

     get         Get application
        with ...
        -i, --id        Id of application to get (mandatory)
        -f, --format    Json format for result

     update      Update application
        with ...
        -i, --id        Id of application to update (mandatory)
        -n, --name      Application name

     unregister  Unregister application
        with ...
        -i, --id        Id of application to unregister (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }


        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintTwinsHelp() {
            Console.WriteLine(
                @"
Manage Twins in registry.

Commands and Options

     list        List twins
        with ...
        -c, --continuation
                        Continuation from previous result.
        -a, --all       Return all endpoints (unpaged)
        -s, --server    Return only server state (default:false)
        -f, --format    Json format for result

     query       Find twins
        -a, --all       Return all endpoints (unpaged)
        -s, --server    Return only server state (default:false)
        -f, --format    Json format for result
        -u, --uri       Endpoint uri to seach for
        -m, --mode      Security mode to search for
        -p, --policy    Security policy to match
        -t, --trusted   Only return trusted or untrusted.

     get         Get twin
        with ...
        -i, --id        Id of twin to retrieve (mandatory)
        -s, --server    Return only server state (default:false)
        -f, --format    Json format for result

     update      Update twin
        with ...
        -i, --id        Id of twin to update (mandatory)
        -t, --trusted   Whether the server endpoint is trusted

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintNodesHelp() {
            Console.WriteLine(
                @"
Access Nodes on twin.

Commands and Options

     browse      Browse nodes on twin
        with ...
        -i, --id        Id of twin to browse (mandatory)
        -n, --nodeid    Node to browse
        -x, --norefs    Exclude references
        -p, --publish   Include publishing status.
        -f, --format    Json format for result

     publish     Publish node values on twin
        with ...
        -i, --id        Id of twin to publish value from (mandatory)
        -n, --nodeid    Node to browse (mandatory)
        -d, --disable   Disable (Pause) publishing (default: false)
        -x, --delete    Delete publish state (default: false)

     list        List published nodes on twin
        with ...
        -i, --id        Id of twin with published nodes (mandatory)
        -c, --continuation
                        Continuation from previous result.
        -a, --all       Return all twins (unpaged)
        -f, --format    Json format for result

     read        Read node value on twin
        with ...
        -i, --id        Id of twin to read value from (mandatory)
        -n, --nodeid    Node to read value from (mandatory)
        -f, --format    Json format for result

     write       Write node value on twin
        with ...
        -i, --id        Id of twin to write value on (mandatory)
        -n, --nodeid    Node to write value to (mandatory)
        -t, --datatype  Datatype of value (mandatory)
        -v, --value     Value to write (mandatory)

     metadata    Get Call meta data
        with ...
        -i, --id        Id of twin with meta data (mandatory)
        -n, --nodeid    Method Node to get meta data for (mandatory)
        -f, --format    Json format for result

     call        Call method node on twin
        with ...
        -i, --id        Id of twin to call method on (mandatory)
        -n, --nodeid    Method Node to call (mandatory)
        -o, --objectid  Object context for method

     help, -h, -? --help
                Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintSupervisorsHelp() {
            Console.WriteLine(
                @"
Manage and configure Twin supervisors

Commands and Options

     list        List supervisors
        with ...
        -c, --continuation
                        Continuation from previous result.
        -a, --all       Return all supervisors (unpaged)
        -f, --format    Json format for result

     get         Get supervisor
        with ...
        -i, --id        Id of supervisor to retrieve (mandatory)
        -f, --format    Json format for result

     update      Update supervisor
        with ...
        -i, --id        Id of twin to update (mandatory)
        -d, --discovery Set supervisor discovery mode
        -n, --domain    Domain of supervisor

     help, -h, -? --help
                Prints out this help.
"
                );
        }

    }
}
