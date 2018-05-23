// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Ctrl {
    using Microsoft.Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Autofac;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Api command line interface
    /// </summary>
    public class Program {

        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IContainer ConfigureContainer(IConfigurationRoot configuration) {
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(
                new ApiConfig {
                    Configuration = configuration
                })
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.RegisterType<TraceLogger>()
                .AsImplementedInterfaces().SingleInstance();

            // Register http client implementation
            builder.RegisterType<HttpClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Register twin services and ...
            builder.RegisterType<OpcUaTwinApiClient>()
                .AsImplementedInterfaces().SingleInstance();

            // registry services and ...
            builder.RegisterType<OpcUaRegistryApiClient>()
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
            var twin = context.Resolve<IOpcUaTwinApi>();
            var registry = context.Resolve<IOpcUaRegistryApi>();
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
                            await GetStatusAsync(twin, registry, options);
                            break;
                        case "apps":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = CollectOptions(2, args);
                            switch (command) {
                                case "register":
                                    await RegisterApplicationAsync(registry, options);
                                    break;
                                case "add":
                                    await AddServerAsync(registry, options);
                                    break;
                                case "update":
                                    await UpdateApplicationAsync(registry, options);
                                    break;
                                case "unregister":
                                    await UnregisterApplicationAsync(registry, options);
                                    break;
                                case "purge":
                                    await PurgeDisabledApplicationsAsync(registry, options);
                                    break;
                                case "list":
                                    await ListApplicationsAsync(registry, options);
                                    break;
                                case "query":
                                    await QueryApplicationsAsync(registry, options);
                                    break;
                                case "get":
                                    await GetApplicationAsync(registry, options);
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
                                    await UpdateTwinAsync(registry, options);
                                    break;
                                case "get":
                                    await GetTwinAsync(registry, options);
                                    break;
                                case "list":
                                    await ListTwinsAsync(registry, options);
                                    break;
                                case "query":
                                    await QueryTwinsAsync(registry, options);
                                    break;
                                case "activate":
                                    await ActivateTwinsAsync(registry, options, true);
                                    break;
                                case "deactivate":
                                    await ActivateTwinsAsync(registry, options, false);
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
                                    await GetSupervisorAsync(registry, options);
                                    break;
                                case "update":
                                    await UpdateSupervisorAsync(registry, options);
                                    break;
                                case "list":
                                    await ListSupervisorsAsync(registry, options);
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
                                    await BrowseAsync(twin, options);
                                    break;
                                case "publish":
                                    await PublishAsync(twin, options);
                                    break;
                                case "nodes":
                                    await ListNodesAsync(twin, options);
                                    break;
                                case "read":
                                    await ReadAsync(twin, options);
                                    break;
                                case "write":
                                    await WriteAsync(twin, options);
                                    break;
                                case "metadata":
                                    await MethodMetadataAsync(twin, options);
                                    break;
                                case "call":
                                    await MethodCallAsync(twin, options);
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
        private static async Task MethodCallAsync(IOpcUaTwinApi service,
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
        private static async Task MethodMetadataAsync(IOpcUaTwinApi service,
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
        private static async Task WriteAsync(IOpcUaTwinApi service,
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
        private static async Task ReadAsync(IOpcUaTwinApi service,
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
        private static async Task PublishAsync(IOpcUaTwinApi service,
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
        private static async Task BrowseAsync(IOpcUaTwinApi service,
            Dictionary<string, string> options) {
            var id = GetOption<string>(options, "-i", "--id");
            var recursive = GetOption(options, "-r", "--recursive", false);
            var request = new BrowseRequestApiModel {
                ExcludeReferences = GetOption<bool>(options, "-x", "--norefs", null),
                IncludePublishingStatus = GetOption<bool>(options, "-p", "--publish", null)
            };
            var nodes = new Queue<string>();
            nodes.Enqueue(GetOption<string>(options, "-n", "--nodeid", null));
            var visited = new HashSet<string>();
            while (nodes.Count > 0) {
                request.NodeId = nodes.Dequeue();
                try {
                    var result = await service.NodeBrowseAsync(id, request);
                    visited.Add(request.NodeId);
                    PrintResult(options, result);

                    // Do recursive browse
                    if (recursive) {
                        foreach (var r in result.References) {
                            if (!visited.Contains(r.Id)) {
                                nodes.Enqueue(r.Id);
                            }
                            if (!visited.Contains(r.Target.Id)) {
                                nodes.Enqueue(r.Target.Id);
                            }
                            if (r.Target.NodeClass != "Variable") {
                                continue;
                            }
                            var read = await service.NodeValueReadAsync(id,
                                new ValueReadRequestApiModel {
                                    NodeId = r.Target.Id
                                });
                            PrintResult(options, read);
                        }
                    }
                }
                catch(Exception e) {
                    Console.WriteLine("==================");
                    Console.WriteLine(e);
                    Console.WriteLine("==================");
                }
            }
        }

        /// <summary>
        /// List published nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ListNodesAsync(IOpcUaTwinApi service,
            Dictionary<string, string> options) {
            if (GetOption(options, "-A", "--all", false)) {
                var result = await service.ListPublishedNodesAsync(
                    GetOption<string>(options, "-i", "--id"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListPublishedNodesAsync(
                    GetOption<string>(options, "-C", "--continuation", null),
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
        private static async Task ListSupervisorsAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            if (GetOption(options, "-A", "--all", false)) {
                var result = await service.ListAllSupervisorsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListSupervisorsAsync(
                    GetOption<string>(options, "-C", "--continuation", null),
                    GetOption<int>(options, "-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetSupervisorAsync(IOpcUaRegistryApi service,
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
        private static async Task UpdateSupervisorAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            await service.UpdateSupervisorAsync(
                new SupervisorUpdateApiModel {
                    Id = GetOption<string>(options, "-i", "--id"),
                    SiteId = GetOption<string>(options, "-s", "--siteId", null),
                    Discovery = GetOption<DiscoveryMode>(options, "-d", "--discovery", null)
                });
        }

        /// <summary>
        /// Registers application
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task RegisterApplicationAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            var result = await service.RegisterAsync(
                new ApplicationRegistrationRequestApiModel {
                    ApplicationUri = GetOption<string>(options, "-u", "--url"),
                    ApplicationName = GetOption<string>(options, "-n", "--name", null),
                    ApplicationType = GetOption<ApplicationType>(options, "-t", "--type", null),
                    ProductUri = GetOption<string>(options, "-p", "--product", null),
                    DiscoveryUrls = new HashSet<string> {
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
        private static async Task AddServerAsync(IOpcUaRegistryApi service,
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
        private static async Task UpdateApplicationAsync(IOpcUaRegistryApi service,
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
        private static Task UnregisterApplicationAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            return service.UnregisterApplicationAsync(
                GetOption<string>(options, "-i", "--id"));
        }

        /// <summary>
        /// Purge disabled applications not seen since specified amount of time.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static Task PurgeDisabledApplicationsAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            return service.PurgeDisabledApplicationsAsync(
                GetOption<TimeSpan>(options, "-f", "--for"));
        }

        /// <summary>
        /// List applications
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ListApplicationsAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            if (GetOption(options, "-A", "--all", false)) {
                var result = await service.ListAllApplicationsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListApplicationsAsync(
                    GetOption<string>(options, "-C", "--continuation", null),
                    GetOption<int>(options, "-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query applications
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task QueryApplicationsAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            var query = new ApplicationRegistrationQueryApiModel {
                ApplicationUri = GetOption<string>(options, "-u", "--uri", null),
                ProductUri = GetOption<string>(options, "-p", "--product", null),
                ApplicationType = GetOption<ApplicationType>(options, "-t", "--type", null),
                ApplicationName = GetOption<string>(options, "-n", "--name", null)
            };
            if (GetOption(options, "-A", "--all", false)) {
                var result = await service.QueryAllApplicationsAsync(query);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.QueryApplicationsAsync(query,
                    GetOption<int>(options, "-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get application
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetApplicationAsync(IOpcUaRegistryApi service,
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
        private static async Task ListTwinsAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            if (GetOption(options, "-A", "--all", false)) {
                var result = await service.ListAllTwinsAsync(
                    GetOption<bool>(options, "-s", "--server", null));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListTwinsAsync(
                    GetOption<string>(options, "-C", "--continuation", null),
                    GetOption<bool>(options, "-s", "--server", null),
                    GetOption<int>(options, "-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task QueryTwinsAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            var query = new TwinRegistrationQueryApiModel {
                Url = GetOption<string>(options, "-u", "--uri", null),
                SecurityMode = GetOption<SecurityMode>(options, "-m", "--mode", null),
                SecurityPolicy = GetOption<string>(options, "-l", "--policy", null),
                Connected = GetOption<bool>(options, "-c", "--connected", null),
                Activated = GetOption<bool>(options, "-a", "--activated", null)
            };
            if (GetOption(options, "-A", "--all", false)) {
                var result = await service.QueryAllTwinsAsync(query,
                    GetOption<bool>(options, "-s", "--server", null));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.QueryTwinsAsync(query,
                    GetOption<bool>(options, "-s", "--server", null),
                    GetOption<int>(options, "-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Activate or deactivate twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task ActivateTwinsAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options, bool enable) {

            // Activate all sign and encrypt twins
            var result = await service.QueryAllTwinsAsync(new TwinRegistrationQueryApiModel {
                SecurityMode = GetOption<SecurityMode>(options, "-m", "mode", null),
                Activated = !enable
            });
            foreach(var item in result) {
                await service.UpdateTwinAsync(new TwinRegistrationUpdateApiModel {
                    Id = item.Registration.Id,
                    Activate = enable
                });
            }
        }

        /// <summary>
        /// Get twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetTwinAsync(IOpcUaRegistryApi service,
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
        private static async Task UpdateTwinAsync(IOpcUaRegistryApi service,
            Dictionary<string, string> options) {
            await service.UpdateTwinAsync(
                new TwinRegistrationUpdateApiModel {
                    Id = GetOption<string>(options, "-i", "--id"),
                    // ...
                    Activate = GetOption<bool>(options, "-a", "--activated", null)
                });
        }

        /// <summary>
        /// Get status
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="registry"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task GetStatusAsync(IOpcUaTwinApi twin,
            IOpcUaRegistryApi registry,
            Dictionary<string, string> options) {
            var tresult = await twin.GetServiceStatusAsync();
            PrintResult(options, tresult);
            var rresult = await registry.GetServiceStatusAsync();
            PrintResult(options, rresult);
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
        /// Configuration - wraps a configuration root
        /// </summary>
        private class ApiConfig : IOpcUaTwinConfig, IOpcUaRegistryConfig {
            /// <summary>
            /// Configuration
            /// </summary>
            public IConfigurationRoot Configuration { get; set; }

            /// <summary>
            /// Service configuration
            /// </summary>
            /// <summary>OPC twin endpoint url</summary>
            public string OpcUaTwinServiceUrl =>
                Configuration.GetValue(kOpcUaTwinServiceUrlKey,
                    "http://localhost:9042/v1");
            /// <summary>OPC twin endpoint url</summary>
            public string OpcUaRegistryServiceUrl =>
                Configuration.GetValue(kOpcUaRegistryServiceUrlKey,
                    "http://localhost:9041/v1");

            private const string kOpcUaTwinServiceUrlKey = "OpcTwinServiceUrl";
            private const string kOpcUaRegistryServiceUrlKey = "OpcRegistryServiceUrl";
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
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all application infos (unpaged)
        -F, --format    Json format for result

     add         Register server and twins through discovery url
        with ...
        -u, --url       Url of the discovery endpoint (mandatory)
        -F, --format    Json format for result

     register    Register Application
        with ...
        -u, --url       Uri of the application (mandatory)
        -n  --name      Application name of the application
        -t, --type      Application type (default to Server)
        -p, --product   Product uri of the application
        -d, --discovery Url of the discovery endpoint
        -F, --format    Json format for result

     query       Find applications
        with ...
        -P, --page-size Size of page
        -A, --all       Return all application infos (unpaged)
        -u, --uri       Application uri of the application 
        -n  --name      Application name of the application
        -t, --type      Application type (default to all)
        -p, --product   Product uri of the application
        -F, --format    Json format for result

     get         Get application
        with ...
        -i, --id        Id of application to get (mandatory)
        -F, --format    Json format for result

     update      Update application
        with ...
        -i, --id        Id of application to update (mandatory)
        -n, --name      Application name

     unregister  Unregister application
        with ...
        -i, --id        Id of application to unregister (mandatory)

     purge       Purge applications not seen ...
        with ...
        -f, --for       ... a specified amount of time (mandatory)

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
        -s, --server    Return only server state (default:false)
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
        -F, --format    Json format for result

     query       Find twins
        -s, --server    Return only server state (default:false)
        -u, --uri       Endpoint uri to seach for
        -m, --mode      Security mode to search for
        -p, --policy    Security policy to match
        -a, --activated Only return activated or deactivated.
        -c, --connected Only return connected or disconnected.
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
        -F, --format    Json format for result

     get         Get twin
        with ...
        -i, --id        Id of twin to retrieve (mandatory)
        -s, --server    Return only server state (default:false)
        -F, --format    Json format for result

     update      Update twin
        with ...
        -i, --id        Id of twin to update (mandatory)
        -a, --activated Whether the twin is activated

     activate    Activate twins with specified
        with ...
        -m, --mode      Security mode (default:SignAndEncrypt)

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
        -r, --recursive Browse recursively and read node values
        -F, --format    Json format for result

     publish     Publish node values on twin
        with ...
        -i, --id        Id of twin to publish value from (mandatory)
        -n, --nodeid    Node to browse (mandatory)
        -d, --disable   Disable (Pause) publishing (default: false)
        -x, --delete    Delete publish state (default: false)

     list        List published nodes on twin
        with ...
        -i, --id        Id of twin with published nodes (mandatory)
        -C, --continuation
                        Continuation from previous result.
        -A, --all       Return all twins (unpaged)
        -F, --format    Json format for result

     read        Read node value on twin
        with ...
        -i, --id        Id of twin to read value from (mandatory)
        -n, --nodeid    Node to read value from (mandatory)
        -F, --format    Json format for result

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
        -F, --format    Json format for result

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
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all supervisors (unpaged)
        -F, --format    Json format for result

     get         Get supervisor
        with ...
        -i, --id        Id of supervisor to retrieve (mandatory)
        -F, --format    Json format for result

     update      Update supervisor
        with ...
        -i, --id        Id of twin to update (mandatory)
        -s, --siteId    Updated site of the supervisor.
        -d, --discovery Set supervisor discovery mode

     help, -h, -? --help
                Prints out this help.
"
                );
        }

    }
}
