// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Cli {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Azure.IIoT.Api.Jobs.Clients;
    using Microsoft.Azure.IIoT.Api.Jobs;
    using Microsoft.Azure.IIoT.Api.Jobs.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.History;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.SignalR;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Api command line interface
    /// </summary>
    public class Program : IDisposable {

        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        public static IContainer ConfigureContainer(
            IConfiguration configuration, bool useMsgPack) {
            var builder = new ContainerBuilder();

            var config = new ApiConfig(configuration);

            // Register configuration interfaces and logger
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(new ApiClientConfig(configuration))
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.AddDiagnostics(config, addConsole: false);
            builder.RegisterModule<NewtonSoftJsonModule>();
            if (useMsgPack) {
                builder.RegisterModule<MessagePackModule>();
            }

            // Register http client module ...
            builder.RegisterModule<HttpClientModule>();
            // ... as well as signalR client (needed for api)
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Use bearer authentication
            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            // Use device code token provider to get tokens
            builder.RegisterType<CliAuthenticationProvider>()
                .AsImplementedInterfaces().SingleInstance();

            // Register twin, vault, and registry services clients
            builder.RegisterType<TwinServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<RegistryServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HistoryServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VaultServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<JobsServiceClient>()
                .AsImplementedInterfaces().SingleInstance();

            // ... with client event callbacks
            builder.RegisterType<RegistryServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build();

            using (var scope = new Program(config,
                args.Any(arg => arg.EqualsIgnoreCase("--useMsgPack")))) {
                scope.RunAsync(args).Wait();
            }
        }


        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        public Program(IConfiguration configuration, bool useMsgPack) {
            var container = ConfigureContainer(configuration, useMsgPack);
            _scope = container.BeginLifetimeScope();
            _twin = _scope.Resolve<ITwinServiceApi>();
            _registry = _scope.Resolve<IRegistryServiceApi>();
            _history = _scope.Resolve<IHistoryServiceApi>();
            _publisher = _scope.Resolve<IPublisherServiceApi>();
            _vault = _scope.Resolve<IVaultServiceApi>();
            _jobs = _scope.Resolve<IJobsServiceApi>();
            _serializer = _scope.Resolve<IJsonSerializer>();
        }

        /// <inheritdoc/>
        public void Dispose() {
            _scope.Dispose();
        }

        /// <summary>
        /// Run client
        /// </summary>
        public async Task RunAsync(string[] args) {
            var interactive = false;
            do {
                if (interactive) {
                    Console.Write("> ");
                    args = CliOptions.ParseAsCommandLine(Console.ReadLine());
                }
                try {
                    if (args.Length < 1) {
                        throw new ArgumentException("Need a command!");
                    }

                    CliOptions options;
                    var command = args[0].ToLowerInvariant();
                    switch (command) {
                        case "exit":
                            interactive = false;
                            break;
                        case "console":
                            Console.WriteLine(@"
  ___               _                 _            _           _           ___           _____
 |_ _|  _ __     __| |  _   _   ___  | |_   _ __  (_)   __ _  | |         |_ _|   ___   |_   _|
  | |  | '_ \   / _` | | | | | / __| | __| | '__| | |  / _` | | |  _____   | |   / _ \    | |
  | |  | | | | | (_| | | |_| | \__ \ | |_  | |    | | | (_| | | | |_____|  | |  | (_) |   | |
 |___| |_| |_|  \__,_|  \__,_| |___/  \__| |_|    |_|  \__,_| |_|         |___|  \___/    |_|
");
                            interactive = true;
                            break;
                        case "status":
                            options = new CliOptions(args);
                            await GetStatusAsync(options);
                            break;
                        case "monitor":
                            options = new CliOptions(args);
                            await MonitorAllAsync();
                            break;
                        case "apps":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "sites":
                                    await ListSitesAsync(options);
                                    break;
                                case "register":
                                    await RegisterApplicationAsync(options);
                                    break;
                                case "add":
                                    await RegisterServerAsync(options);
                                    break;
                                case "discover":
                                    await DiscoverServersAsync(options);
                                    break;
                                case "cancel":
                                    await CancelDiscoveryAsync(options);
                                    break;
                                case "update":
                                    await UpdateApplicationAsync(options);
                                    break;
                                case "disable":
                                    await DisableApplicationAsync(options);
                                    break;
                                case "enable":
                                    await EnableApplicationAsync(options);
                                    break;
                                case "unregister":
                                    await UnregisterApplicationAsync(options);
                                    break;
                                case "purge":
                                    await PurgeDisabledApplicationsAsync(options);
                                    break;
                                case "list":
                                    await ListApplicationsAsync(options);
                                    break;
                                case "monitor":
                                    await MonitorApplicationsAsync();
                                    break;
                                case "select":
                                    await SelectApplicationAsync(options);
                                    break;
                                case "query":
                                    await QueryApplicationsAsync(options);
                                    break;
                                case "get":
                                    await GetApplicationAsync(options);
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
                        case "endpoints":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "get":
                                    await GetEndpointAsync(options);
                                    break;
                                case "list":
                                    await ListEndpointsAsync(options);
                                    break;
                                case "monitor":
                                    await MonitorEndpointsAsync();
                                    break;
                                case "select":
                                    await SelectEndpointsAsync(options);
                                    break;
                                case "query":
                                    await QueryEndpointsAsync(options);
                                    break;
                                case "validate":
                                    await GetEndpointCertificateAsync(options);
                                    break;
                                case "activate":
                                    await ActivateEndpointsAsync(options);
                                    break;
                                case "deactivate":
                                    await DeactivateEndpointsAsync(options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintEndpointsHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "jobs":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "get":
                                    await GetJobAsync(options);
                                    break;
                                case "list":
                                    await ListJobsAsync(options);
                                    break;
                                case "workers":
                                    await ListWorkersAsync(options);
                                    break;
                                case "select":
                                    await SelectJobAsync(options);
                                    break;
                                case "query":
                                    await QueryJobAsync(options);
                                    break;
                                case "restart":
                                    await RestartJobAsync(options);
                                    break;
                                case "cancel":
                                    await CancelJobAsync(options);
                                    break;
                                case "delete":
                                    await DeleteJobAsync(options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintJobsHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "groups":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "root":
                                    await CreateRootAsync(options);
                                    break;
                                case "child":
                                    await CreateGroupAsync(options);
                                    break;
                                case "update":
                                    await UpdateGroupAsync(options);
                                    break;
                                case "delete":
                                    await DeleteGroupAsync(options);
                                    break;
                                case "list":
                                    await ListGroupsAsync(options);
                                    break;
                                case "select":
                                    await SelectGroupAsync(options);
                                    break;
                                case "get":
                                    await GetGroupAsync(options);
                                    break;
                                case "renew":
                                    await RenewIssuerCertAsync(options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintGroupsHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "requests":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "sign":
                                    await SigningRequestAsync(options);
                                    break;
                                case "keypair":
                                    await KeyPairRequestAsync(options);
                                    break;
                                case "approve":
                                    await ApproveRequestAsync(options);
                                    break;
                                case "reject":
                                    await RejectRequestAsync(options);
                                    break;
                                case "accept":
                                    await AcceptRequestAsync(options);
                                    break;
                                case "delete":
                                    await DeleteRequestAsync(options);
                                    break;
                                case "list":
                                    await ListRequestsAsync(options);
                                    break;
                                case "select":
                                    await SelectRequestAsync(options);
                                    break;
                                case "get":
                                    await GetRequestAsync(options);
                                    break;
                                case "query":
                                    await QueryRequestsAsync(options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintRequestsHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "trust":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "create":
                                    await AddTrustRelationshipAsync(options);
                                    break;
                                case "get":
                                    await GetTrustedCertificatesAsync(options);
                                    break;
                                case "delete":
                                    await RemoveTrustRelationshipAsync(options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintTrustHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "discoverers":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "get":
                                    await GetDiscovererAsync(options);
                                    break;
                                case "update":
                                    await UpdateDiscovererAsync(options);
                                    break;
                                case "scan":
                                    await DiscovererScanAsync(options);
                                    break;
                                case "monitor":
                                    await MonitorDiscoverersAsync(options);
                                    break;
                                case "list":
                                    await ListDiscoverersAsync(options);
                                    break;
                                case "select":
                                    await SelectDiscovererAsync(options);
                                    break;
                                case "query":
                                    await QueryDiscoverersAsync(options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintDiscoverersHelp();
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
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "get":
                                    await GetSupervisorAsync(options);
                                    break;
                                case "status":
                                    await GetSupervisorStatusAsync(options);
                                    break;
                                case "update":
                                    await UpdateSupervisorAsync(options);
                                    break;
                                case "monitor":
                                    await MonitorSupervisorsAsync();
                                    break;
                                case "reset":
                                    await ResetSupervisorAsync(options);
                                    break;
                                case "list":
                                    await ListSupervisorsAsync(options);
                                    break;
                                case "select":
                                    await SelectSupervisorAsync(options);
                                    break;
                                case "query":
                                    await QuerySupervisorsAsync(options);
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
                        case "publishers":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "get":
                                    await GetPublisherAsync(options);
                                    break;
                                case "update":
                                    await UpdatePublisherAsync(options);
                                    break;
                                case "monitor":
                                    await MonitorPublishersAsync();
                                    break;
                                case "list":
                                    await ListPublishersAsync(options);
                                    break;
                                case "select":
                                    await SelectPublisherAsync(options);
                                    break;
                                case "query":
                                    await QueryPublishersAsync(options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintPublishersHelp();
                                    break;
                                default:
                                    throw new ArgumentException($"Unknown command {command}.");
                            }
                            break;
                        case "gateways":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "get":
                                    await GetGatewayAsync(options);
                                    break;
                                case "update":
                                    await UpdateGatewayAsync(options);
                                    break;
                                case "monitor":
                                    await MonitorGatewaysAsync();
                                    break;
                                case "list":
                                    await ListGatewaysAsync(options);
                                    break;
                                case "select":
                                    await SelectGatewayAsync(options);
                                    break;
                                case "query":
                                    await QueryGatewaysAsync(options);
                                    break;
                                case "-?":
                                case "-h":
                                case "--help":
                                case "help":
                                    PrintGatewaysHelp();
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
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "browse":
                                    await BrowseAsync(options);
                                    break;
                                case "select":
                                    await SelectNodeAsync(options);
                                    break;
                                case "publish":
                                    await PublishAsync(options);
                                    break;
                                case "monitor":
                                    await MonitorSamplesAsync(options);
                                    break;
                                case "unpublish":
                                    await UnpublishAsync(options);
                                    break;
                                case "list":
                                    await ListPublishedNodesAsync(options);
                                    break;
                                case "read":
                                    await ReadAsync(options);
                                    break;
                                case "write":
                                    await WriteAsync(options);
                                    break;
                                case "metadata":
                                    await MethodMetadataAsync(options);
                                    break;
                                case "call":
                                    await MethodCallAsync(options);
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

        private string _nodeId;

        /// <summary>
        /// Get endpoint id
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private string GetNodeId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-n", "--nodeid", null);
            if (_nodeId != null) {
                if (id == null) {
                    return _nodeId;
                }
                _nodeId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -n/--nodeId option.");
        }

        /// <summary>
        /// Select node id
        /// </summary>
        private async Task SelectNodeAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _nodeId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_nodeId);
            }
            else {
                var nodeId = options.GetValueOrDefault<string>("-n", "--nodeid", null);
                if (string.IsNullOrEmpty(nodeId)) {
                    var id = GetEndpointId(options, false);
                    if (!string.IsNullOrEmpty(id)) {
                        var results = await _twin.NodeBrowseAsync(id, new BrowseRequestApiModel {
                            TargetNodesOnly = true,
                            NodeId = _nodeId
                        });
                        var node = ConsoleEx.Select(results.References.Select(r => r.Target),
                            n => n.BrowseName);
                        if (node != null) {
                            nodeId = node.NodeId;
                        }
                    }
                    if (string.IsNullOrEmpty(nodeId)) {
                        Console.WriteLine("Nothing selected.");
                        return;
                    }
                    Console.WriteLine($"Selected {nodeId}.");
                }
                _nodeId = nodeId;
            }
        }

        /// <summary>
        /// Call method
        /// </summary>
        private async Task MethodCallAsync(CliOptions options) {
            var result = await _twin.NodeMethodCallAsync(
                GetEndpointId(options),
                new MethodCallRequestApiModel {
                    MethodId = GetNodeId(options),
                    ObjectId = options.GetValue<string>("-o", "--objectid")

                    // ...
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        private async Task MethodMetadataAsync(CliOptions options) {
            var result = await _twin.NodeMethodGetMetadataAsync(
                GetEndpointId(options),
                new MethodMetadataRequestApiModel {
                    MethodId = GetNodeId(options)
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Write value
        /// </summary>
        private async Task WriteAsync(CliOptions options) {
            var result = await _twin.NodeValueWriteAsync(
                GetEndpointId(options),
                new ValueWriteRequestApiModel {
                    NodeId = GetNodeId(options),
                    DataType = options.GetValueOrDefault<string>("-t", "--datatype", null),
                    Value = _serializer.FromObject(options.GetValue<string>("-v", "--value"))
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        private async Task ReadAsync(CliOptions options) {
            var result = await _twin.NodeValueReadAsync(
                GetEndpointId(options),
                new ValueReadRequestApiModel {
                    NodeId = GetNodeId(options)
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        private async Task BrowseAsync(CliOptions options) {
            var id = GetEndpointId(options);
            var silent = options.IsSet("-s", "--silent");
            var all = options.IsSet("-A", "--all");
            var recursive = options.IsSet("-r", "--recursive");
            var readDuringBrowse = options.IsProvidedOrNull("-v", "--readvalue");
            var request = new BrowseRequestApiModel {
                TargetNodesOnly = options.IsProvidedOrNull("-t", "--targets"),
                ReadVariableValues = readDuringBrowse,
                MaxReferencesToReturn = options.GetValueOrDefault<uint>("-x", "--maxrefs", null),
                Direction = options.GetValueOrDefault<BrowseDirection>("-d", "--direction", null)
            };
            var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                options.GetValueOrDefault<string>("-n", "--nodeid", null)
            };
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nodesRead = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = 0;
            var sw = Stopwatch.StartNew();
            while (nodes.Count > 0) {
                request.NodeId = nodes.First();
                nodes.Remove(request.NodeId);
                try {
                    var result = await (all ?
                        _twin.NodeBrowseAsync(id, request) :
                        _twin.NodeBrowseFirstAsync(id, request));
                    visited.Add(request.NodeId);
                    if (!silent) {
                        PrintResult(options, result);
                    }
                    if (readDuringBrowse ?? false) {
                        continue;
                    }
                    // Do recursive browse
                    if (recursive) {
                        foreach (var r in result.References) {
                            if (!visited.Contains(r.ReferenceTypeId)) {
                                nodes.Add(r.ReferenceTypeId);
                            }
                            if (!visited.Contains(r.Target.NodeId)) {
                                nodes.Add(r.Target.NodeId);
                            }
                            if (nodesRead.Contains(r.Target.NodeId)) {
                                continue; // We have read this one already
                            }
                            if (!r.Target.NodeClass.HasValue ||
                                r.Target.NodeClass.Value != NodeClass.Variable) {
                                continue;
                            }
                            if (!silent) {
                                Console.WriteLine($"Reading {r.Target.NodeId}");
                            }
                            try {
                                nodesRead.Add(r.Target.NodeId);
                                var read = await _twin.NodeValueReadAsync(id,
                                    new ValueReadRequestApiModel {
                                        NodeId = r.Target.NodeId
                                    });
                                if (!silent) {
                                    PrintResult(options, read);
                                }
                            }
                            catch (Exception ex) {
                                Console.WriteLine($"Reading {r.Target.NodeId} resulted in {ex}");
                                errors++;
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"Browse {request.NodeId} resulted in {e}");
                    errors++;
                }
            }
            Console.WriteLine($"Browse took {sw.Elapsed}. Visited " +
                $"{visited.Count} nodes and read {nodesRead.Count} of them with {errors} errors.");
        }


        /// <summary>
        /// Publish node
        /// </summary>
        private async Task PublishAsync(CliOptions options) {
            var result = await _publisher.NodePublishStartAsync(
                GetEndpointId(options),
                new PublishStartRequestApiModel {
                    Item = new PublishedItemApiModel {
                        NodeId = GetNodeId(options),
                        SamplingInterval = TimeSpan.FromMilliseconds(1000),
                        PublishingInterval = TimeSpan.FromMilliseconds(1000)
                    }
                });
            if (result.ErrorInfo != null) {
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Monitor samples from endpoint
        /// </summary>
        private async Task MonitorSamplesAsync(CliOptions options) {
            var endpointId = GetEndpointId(options);
            var events = _scope.Resolve<IPublisherServiceEvents>();
            Console.WriteLine("Press any key to stop.");

            var finish = await events.NodePublishSubscribeByEndpointAsync(
                endpointId, PrintSample);
            try {
                Console.ReadKey();
            }
            finally {
                await finish.DisposeAsync();
            }
        }

        /// <summary>
        /// Unpublish node
        /// </summary>
        private async Task UnpublishAsync(CliOptions options) {
            var result = await _publisher.NodePublishStopAsync(
                GetEndpointId(options),
                new PublishStopRequestApiModel {
                    NodeId = GetNodeId(options)
                });
            if (result.ErrorInfo != null) {
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List published nodes
        /// </summary>
        private async Task ListPublishedNodesAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _publisher.NodePublishListAllAsync(GetEndpointId(options));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _publisher.NodePublishListAsync(GetEndpointId(options),
                    options.GetValueOrDefault<string>("-C", "--continuation", null));
                PrintResult(options, result);
            }
        }

        private string _publisherId;

        /// <summary>
        /// Get publisher id
        /// </summary>
        private string GetPublisherId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_publisherId != null) {
                if (id == null) {
                    return _publisherId;
                }
                _publisherId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select publisher registration
        /// </summary>
        private async Task SelectPublisherAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _publisherId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_publisherId);
            }
            else {
                var publisherId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(publisherId)) {
                    var result = await _registry.ListAllPublishersAsync();
                    publisherId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(publisherId)) {
                        Console.WriteLine("Nothing selected - publisher selection cleared.");
                    }
                    else {
                        Console.WriteLine($"Selected {publisherId}.");
                    }
                }
                _publisherId = publisherId;
            }
        }

        /// <summary>
        /// List publisher registrations
        /// </summary>
        private async Task ListPublishersAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.ListAllPublishersAsync(
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.ListPublishersAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query publisher registrations
        /// </summary>
        private async Task QueryPublishersAsync(CliOptions options) {
            var query = new PublisherQueryApiModel {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.QueryAllPublishersAsync(query,
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.QueryPublishersAsync(query,
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get publisher
        /// </summary>
        private async Task GetPublisherAsync(CliOptions options) {
            var result = await _registry.GetPublisherAsync(GetPublisherId(options),
                options.IsProvidedOrNull("-S", "--server"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Update publisher
        /// </summary>
        private async Task UpdatePublisherAsync(CliOptions options) {
            var config = BuildPublisherConfig(options);
            await _registry.UpdatePublisherAsync(GetPublisherId(options),
                new PublisherUpdateApiModel {
                    SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null),
                    LogLevel = options.GetValueOrDefault<TraceLogLevel>(
                        "-l", "--log-level", null),
                    Configuration = config,
                });
        }

        /// <summary>
        /// Monitor publishers
        /// </summary>
        private async Task MonitorPublishersAsync() {
            var events = _scope.Resolve<IRegistryServiceEvents>();
            Console.WriteLine("Press any key to stop.");
            var complete = await events.SubscribePublisherEventsAsync(PrintEvent);
            try {
                Console.ReadKey();
            }
            finally {
                await complete.DisposeAsync();
            }
        }

        private string _gatewayId;

        /// <summary>
        /// Get gateway id
        /// </summary>
        private string GetGatewayId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_gatewayId != null) {
                if (id == null) {
                    return _gatewayId;
                }
                _gatewayId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select gateway registration
        /// </summary>
        private async Task SelectGatewayAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _gatewayId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_gatewayId);
            }
            else {
                var gatewayId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(gatewayId)) {
                    var result = await _registry.ListAllGatewaysAsync();
                    gatewayId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(gatewayId)) {
                        Console.WriteLine("Nothing selected - gateway selection cleared.");
                    }
                    else {
                        Console.WriteLine($"Selected {gatewayId}.");
                    }
                }
                _gatewayId = gatewayId;
            }
        }

        /// <summary>
        /// List gateway registrations
        /// </summary>
        private async Task ListGatewaysAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.ListAllGatewaysAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.ListGatewaysAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query gateway registrations
        /// </summary>
        private async Task QueryGatewaysAsync(CliOptions options) {
            var query = new GatewayQueryApiModel {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.QueryAllGatewaysAsync(query);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.QueryGatewaysAsync(query,
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get gateway
        /// </summary>
        private async Task GetGatewayAsync(CliOptions options) {
            var result = await _registry.GetGatewayAsync(GetGatewayId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Update gateway
        /// </summary>
        private async Task UpdateGatewayAsync(CliOptions options) {
            await _registry.UpdateGatewayAsync(GetGatewayId(options),
                new GatewayUpdateApiModel {
                    SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null),
                });
        }

        /// <summary>
        /// Monitor gateways
        /// </summary>
        private async Task MonitorGatewaysAsync() {
            var events = _scope.Resolve<IRegistryServiceEvents>();
            Console.WriteLine("Press any key to stop.");
            var complete = await events.SubscribeGatewayEventsAsync(PrintEvent);
            try {
                Console.ReadKey();
            }
            finally {
                await complete.DisposeAsync();
            }
        }

        private string _groupId;

        /// <summary>
        /// Get group id
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private string GetGroupId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_groupId != null) {
                if (id == null) {
                    return _groupId;
                }
                _groupId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select group registration
        /// </summary>
        private async Task SelectGroupAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _groupId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_groupId);
            }
            else {
                var groupId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(groupId)) {
                    var result = await _vault.ListAllGroupsAsync();
                    groupId = ConsoleEx.Select(result);
                    if (string.IsNullOrEmpty(groupId)) {
                        Console.WriteLine("Nothing selected - group selection cleared.");
                    }
                }
                _groupId = groupId;
            }
        }

        /// <summary>
        /// List groups
        /// </summary>
        private async Task ListGroupsAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _vault.ListAllGroupsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _vault.ListGroupsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get root group
        /// </summary>
        private async Task CreateRootAsync(CliOptions options) {
            var result = await _vault.CreateRootAsync(new TrustGroupRootCreateRequestApiModel {
                IssuedKeySize = options.GetValueOrDefault<ushort>("-s", "--keysize", null),
                IssuedLifetime = options.GetValueOrDefault<TimeSpan>("-l", "--lifetime", null),
                IssuedSignatureAlgorithm = options.GetValueOrDefault<SignatureAlgorithm>(
                        "-a", "--algorithm", null),
                Name = options.GetValue<string>("-n", "--name"),
                SubjectName = options.GetValue<string>("-s", "--subject")
            });
            PrintResult(options, result);
        }

        /// <summary>
        /// Create group
        /// </summary>
        private async Task CreateGroupAsync(CliOptions options) {
            var result = await _vault.CreateGroupAsync(new TrustGroupRegistrationRequestApiModel {
                IssuedKeySize = options.GetValueOrDefault<ushort>("-s", "--keysize", null),
                IssuedLifetime = options.GetValueOrDefault<TimeSpan>("-l", "--lifetime", null),
                IssuedSignatureAlgorithm = options.GetValueOrDefault<SignatureAlgorithm>(
                        "-a", "--algorithm", null),
                Name = options.GetValueOrDefault<string>("-n", "--name", null),
                ParentId = options.GetValue<string>("-p", "--parent"),
                SubjectName = options.GetValue<string>("-s", "--subject")
            });
            PrintResult(options, result);
        }

        /// <summary>
        /// Get group
        /// </summary>
        private async Task GetGroupAsync(CliOptions options) {
            var result = await _vault.GetGroupAsync(GetGroupId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Delete group
        /// </summary>
        private async Task DeleteGroupAsync(CliOptions options) {
            await _vault.DeleteGroupAsync(GetGroupId(options));
        }

        /// <summary>
        /// Renew issuer cert
        /// </summary>
        private async Task RenewIssuerCertAsync(CliOptions options) {
            var result = await _vault.RenewIssuerCertificateAsync(GetGroupId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Update group
        /// </summary>
        private async Task UpdateGroupAsync(CliOptions options) {
            await _vault.UpdateGroupAsync(GetGroupId(options),
                new TrustGroupUpdateRequestApiModel {
                    IssuedKeySize = options.GetValueOrDefault<ushort>("-s", "--keysize", null),
                    IssuedLifetime = options.GetValueOrDefault<TimeSpan>("-l", "--lifetime", null),
                    IssuedSignatureAlgorithm = options.GetValueOrDefault<SignatureAlgorithm>(
                        "-a", "--algorithm", null),
                    Name = options.GetValueOrDefault<string>("-n", "--name", null)
                });
        }


        private string _jobId;

        /// <summary>
        /// Get supervisor id
        /// </summary>
        private string GetJobId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_jobId != null) {
                if (id == null) {
                    return _jobId;
                }
                _jobId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select job
        /// </summary>
        private async Task SelectJobAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _jobId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_jobId);
            }
            else {
                var jobId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(jobId)) {
                    var result = await _jobs.ListAllJobsAsync();
                    jobId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(jobId)) {
                        Console.WriteLine("Nothing selected - job selection cleared.");
                    }
                    else {
                        Console.WriteLine($"Selected {jobId}.");
                    }
                }
                _jobId = jobId;
            }
        }

        /// <summary>
        /// Get job
        /// </summary>
        private async Task GetJobAsync(CliOptions options) {
            var result = await _jobs.GetJobAsync(GetJobId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Delete job
        /// </summary>
        private async Task DeleteJobAsync(CliOptions options) {
            await _jobs.DeleteJobAsync(GetJobId(options));
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        private async Task CancelJobAsync(CliOptions options) {
            await _jobs.CancelJobAsync(GetJobId(options));
        }

        /// <summary>
        /// Restart job
        /// </summary>
        private async Task RestartJobAsync(CliOptions options) {
            await _jobs.RestartJobAsync(GetJobId(options));
        }

        /// <summary>
        /// Query jobs
        /// </summary>
        private async Task QueryJobAsync(CliOptions options) {
            var query = new JobInfoQueryApiModel {
                JobConfigurationType = options.GetValueOrDefault<string>("-t", "--type", null),
                Status = options.GetValueOrDefault<JobStatus>("-s", "--status", null),
                Name = options.GetValueOrDefault<string>("-n", "--name", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await _jobs.QueryAllJobsAsync(query);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _jobs.QueryJobsAsync(query,
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List jobs
        /// </summary>
        private async Task ListJobsAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _jobs.ListAllJobsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _jobs.ListJobsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List workers
        /// </summary>
        private async Task ListWorkersAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _jobs.ListAllAgentsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _jobs.ListWorkersAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        private string _supervisorId;

        /// <summary>
        /// Get supervisor id
        /// </summary>
        private string GetSupervisorId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_supervisorId != null) {
                if (id == null) {
                    return _supervisorId;
                }
                _supervisorId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select supervisor registration
        /// </summary>
        private async Task SelectSupervisorAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _supervisorId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_supervisorId);
            }
            else {
                var supervisorId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(supervisorId)) {
                    var result = await _registry.ListAllSupervisorsAsync();
                    supervisorId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(supervisorId)) {
                        Console.WriteLine("Nothing selected - supervisor selection cleared.");
                    }
                    else {
                        Console.WriteLine($"Selected {supervisorId}.");
                    }
                }
                _supervisorId = supervisorId;
            }
        }

        /// <summary>
        /// List supervisor registrations
        /// </summary>
        private async Task ListSupervisorsAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.ListAllSupervisorsAsync(
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.ListSupervisorsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query supervisor registrations
        /// </summary>
        private async Task QuerySupervisorsAsync(CliOptions options) {
            var query = new SupervisorQueryApiModel {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                EndpointId = options.GetValueOrDefault<string>("-e", "--endpoint", null),
                SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.QueryAllSupervisorsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.QuerySupervisorsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get supervisor
        /// </summary>
        private async Task GetSupervisorAsync(CliOptions options) {
            var result = await _registry.GetSupervisorAsync(GetSupervisorId(options),
                options.IsProvidedOrNull("-S", "--server"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Get supervisor status
        /// </summary>
        private async Task GetSupervisorStatusAsync(CliOptions options) {
            var result = await _registry.GetSupervisorStatusAsync(GetSupervisorId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Reset supervisor
        /// </summary>
        private async Task ResetSupervisorAsync(CliOptions options) {
            await _registry.ResetSupervisorAsync(GetSupervisorId(options));
        }

        /// <summary>
        /// Monitor supervisors
        /// </summary>
        private async Task MonitorSupervisorsAsync() {
            var events = _scope.Resolve<IRegistryServiceEvents>();
            Console.WriteLine("Press any key to stop.");
            var complete = await events.SubscribeSupervisorEventsAsync(PrintEvent);
            try {
                Console.ReadKey();
            }
            finally {
                await complete.DisposeAsync();
            }
        }

        /// <summary>
        /// Update supervisor
        /// </summary>
        private async Task UpdateSupervisorAsync(CliOptions options) {
            var config = BuildDiscoveryConfig(options);
            await _registry.UpdateSupervisorAsync(GetSupervisorId(options),
                new SupervisorUpdateApiModel {
                    SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null),
                    LogLevel = options.GetValueOrDefault<TraceLogLevel>(
                        "-l", "--log-level", null)
                });
        }

        private string _discovererId;

        /// <summary>
        /// Get discoverer id
        /// </summary>
        private string GetDiscovererId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_discovererId != null) {
                if (id == null) {
                    return _discovererId;
                }
                _discovererId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select discoverer registration
        /// </summary>
        private async Task SelectDiscovererAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _discovererId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_discovererId);
            }
            else {
                var discovererId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(discovererId)) {
                    var result = await _registry.ListAllDiscoverersAsync();
                    discovererId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(discovererId)) {
                        Console.WriteLine("Nothing selected - discoverer selection cleared.");
                    }
                    else {
                        Console.WriteLine($"Selected {discovererId}.");
                    }
                }
                _discovererId = discovererId;
            }
        }

        /// <summary>
        /// List discoverer registrations
        /// </summary>
        private async Task ListDiscoverersAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.ListAllDiscoverersAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.ListDiscoverersAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query discoverer registrations
        /// </summary>
        private async Task QueryDiscoverersAsync(CliOptions options) {
            var query = new DiscovererQueryApiModel {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                Discovery = options.GetValueOrDefault<DiscoveryMode>("-d", "--discovery", null),
                SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.QueryAllDiscoverersAsync(query,
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.QueryDiscoverersAsync(query,
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get discoverer
        /// </summary>
        private async Task GetDiscovererAsync(CliOptions options) {
            var result = await _registry.GetDiscovererAsync(GetDiscovererId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Monitor discoverers
        /// </summary>
        private async Task MonitorDiscoverersAsync(CliOptions options) {
            var events = _scope.Resolve<IRegistryServiceEvents>();
            Console.WriteLine("Press any key to stop.");
            IAsyncDisposable complete;
            var discovererId = options.GetValueOrDefault<string>("-i", "--id", null);
            if (discovererId != null) {
                // If specified - monitor progress
                complete = await events.SubscribeDiscoveryProgressByDiscovererIdAsync(
                    discovererId, PrintProgress);
            }
            else {
                complete = await events.SubscribeDiscovererEventsAsync(PrintEvent);
            }
            try {
                Console.ReadKey();
            }
            finally {
                await complete.DisposeAsync();
            }
        }

        /// <summary>
        /// Update discoverer
        /// </summary>
        private async Task UpdateDiscovererAsync(CliOptions options) {
            var config = BuildDiscoveryConfig(options);
            await _registry.UpdateDiscovererAsync(GetDiscovererId(options),
                new DiscovererUpdateApiModel {
                    SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null),
                    LogLevel = options.GetValueOrDefault<TraceLogLevel>(
                        "-l", "--log-level", null),
                    Discovery = options.GetValueOrDefault("-d", "--discovery",
                        config == null ? (DiscoveryMode?)null : DiscoveryMode.Fast),
                    DiscoveryConfig = config,
                });
        }

        /// <summary>
        /// Start and monitor discovery
        /// </summary>
        private async Task DiscovererScanAsync(CliOptions options) {
            var discovererId = GetDiscovererId(options);
            var events = _scope.Resolve<IRegistryServiceEvents>();
            Console.WriteLine("Press any key to stop.");
            var discovery = await events.SubscribeDiscoveryProgressByDiscovererIdAsync(
                discovererId, PrintProgress);
            try {
                var config = BuildDiscoveryConfig(options);
                var mode = options.GetValueOrDefault("-d", "--discovery",
                    config == null ? DiscoveryMode.Fast : DiscoveryMode.Scan);
                if (config == null) {
                    config = new DiscoveryConfigApiModel();
                }
                if (mode == DiscoveryMode.Off) {
                    throw new ArgumentException("-d/--discovery Off is not supported");
                }
                await _registry.SetDiscoveryModeAsync(discovererId, mode, config);
                Console.ReadKey();
                await _registry.SetDiscoveryModeAsync(discovererId, DiscoveryMode.Off,
                    new DiscoveryConfigApiModel());
            }
            catch {
                await discovery.DisposeAsync();
            }
        }

        private string _applicationId;

        /// <summary>
        /// Get application id
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private string GetApplicationId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_applicationId != null) {
                if (id == null) {
                    return _applicationId;
                }
                _applicationId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select application registration
        /// </summary>
        private async Task SelectApplicationAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _applicationId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_applicationId);
            }
            else {
                var applicationId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(applicationId)) {
                    var result = await _registry.ListAllApplicationsAsync();
                    applicationId = ConsoleEx.Select(result.Select(r => r.ApplicationId));
                    if (string.IsNullOrEmpty(applicationId)) {
                        Console.WriteLine("Nothing selected - application selection cleared.");
                    }
                    else {
                        Console.WriteLine($"Selected {applicationId}.");
                    }
                }
                _applicationId = applicationId;
            }
        }

        /// <summary>
        /// Registers application
        /// </summary>
        private async Task RegisterApplicationAsync(CliOptions options) {
            var discoveryUrl = options.GetValueOrDefault<string>("-d", "--discoveryUrl", null);
            var result = await _registry.RegisterAsync(
                new ApplicationRegistrationRequestApiModel {
                    ApplicationUri = options.GetValue<string>("-u", "--url"),
                    ApplicationName = options.GetValueOrDefault<string>("-n", "--name", null),
                    GatewayServerUri = options.GetValueOrDefault<string>("-g", "--gwuri", null),
                    ApplicationType = options.GetValueOrDefault<ApplicationType>("-t", "--type", null),
                    ProductUri = options.GetValueOrDefault<string>("-p", "--product", null),
                    DiscoveryProfileUri = options.GetValueOrDefault<string>("-r", "--dpuri", null),
                    DiscoveryUrls = string.IsNullOrEmpty(discoveryUrl) ? null :
                        new HashSet<string> { discoveryUrl }
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Registers server
        /// </summary>
        private async Task RegisterServerAsync(CliOptions options) {
            IRegistryServiceEvents events = null;
            var id = options.GetValueOrDefault("-i", "--id", Guid.NewGuid().ToString());
            if (options.IsSet("-m", "--monitor")) {
                events = _scope.Resolve<IRegistryServiceEvents>();
                var tcs = new TaskCompletionSource<bool>();

                var discovery = await events.SubscribeDiscoveryProgressByRequestIdAsync(
                    id, async ev => {
                        await PrintProgress(ev);
                        switch (ev.EventType) {
                            case DiscoveryProgressType.Error:
                            case DiscoveryProgressType.Cancelled:
                            case DiscoveryProgressType.Finished:
                                tcs.TrySetResult(true);
                                break;
                        }
                    });
                try {
                    await RegisterServerAsync(options, id);
                    await tcs.Task; // For completion
                }
                finally {
                    await discovery.DisposeAsync();
                }
            }
            else {
                await RegisterServerAsync(options, id);
            }
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        private async Task RegisterServerAsync(CliOptions options, string id) {
            var activate = options.IsSet("-a", "--activate");
            await _registry.RegisterAsync(
                new ServerRegistrationRequestApiModel {
                    Id = id,
                    DiscoveryUrl = options.GetValue<string>("-u", "--url"),
                    ActivationFilter = !activate ? null : new EndpointActivationFilterApiModel {
                        SecurityMode = SecurityMode.None
                    }
                });
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        private async Task DiscoverServersAsync(CliOptions options) {
            IRegistryServiceEvents events = null;
            var id = options.GetValueOrDefault("-i", "--id", Guid.NewGuid().ToString());
            if (options.IsSet("-m", "--monitor")) {
                events = _scope.Resolve<IRegistryServiceEvents>();
                var tcs = new TaskCompletionSource<bool>();
                var discovery = await events.SubscribeDiscoveryProgressByRequestIdAsync(
                    id, async ev => {
                        await PrintProgress(ev);
                        switch (ev.EventType) {
                            case DiscoveryProgressType.Error:
                            case DiscoveryProgressType.Cancelled:
                            case DiscoveryProgressType.Finished:
                                tcs.TrySetResult(true);
                                break;
                        }
                    });
                try {
                    await DiscoverServersAsync(options, id);
                    await tcs.Task; // For completion
                }
                finally {
                    await discovery.DisposeAsync();
                }

            }
            else {
                await DiscoverServersAsync(options, id);
            }
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        private async Task DiscoverServersAsync(CliOptions options, string id) {
            await _registry.DiscoverAsync(
                new DiscoveryRequestApiModel {
                    Id = id,
                    Discovery = options.GetValueOrDefault("-d", "--discovery", DiscoveryMode.Fast),
                    Configuration = BuildDiscoveryConfig(options)
                });
        }

        /// <summary>
        /// Cancel discovery
        /// </summary>
        private async Task CancelDiscoveryAsync(CliOptions options) {
            await _registry.CancelAsync(
                new DiscoveryCancelApiModel {
                    Id = options.GetValue<string>("-i", "--id")
                });
        }

        /// <summary>
        /// Update application
        /// </summary>
        private async Task UpdateApplicationAsync(CliOptions options) {
            await _registry.UpdateApplicationAsync(GetApplicationId(options),
                new ApplicationRegistrationUpdateApiModel {
                    ApplicationName = options.GetValueOrDefault<string>("-n", "--name", null),
                    GatewayServerUri = options.GetValueOrDefault<string>("-g", "--gwuri", null),
                    ProductUri = options.GetValueOrDefault<string>("-p", "--product", null),
                    DiscoveryProfileUri = options.GetValueOrDefault<string>("-r", "--dpuri", null)
                    // ...
                });
        }

        /// <summary>
        /// Disable application
        /// </summary>
        private async Task DisableApplicationAsync(CliOptions options) {
            await _registry.DisableApplicationAsync(GetApplicationId(options));
        }

        /// <summary>
        /// Enable application
        /// </summary>
        private async Task EnableApplicationAsync(CliOptions options) {
            await _registry.EnableApplicationAsync(GetApplicationId(options));
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        private async Task UnregisterApplicationAsync(CliOptions options) {

            var id = GetApplicationId(options, false);
            if (id != null) {
                await _registry.UnregisterApplicationAsync(id);
                return;
            }

            var query = new ApplicationRegistrationQueryApiModel {
                ApplicationUri = options.GetValueOrDefault<string>("-u", "--uri", null),
                ApplicationType = options.GetValueOrDefault<ApplicationType>("-t", "--type", null),
                ApplicationName = options.GetValueOrDefault<string>("-n", "--name", null),
                ProductUri = options.GetValueOrDefault<string>("-p", "--product", null),
                GatewayServerUri = options.GetValueOrDefault<string>("-g", "--gwuri", null),
                DiscoveryProfileUri = options.GetValueOrDefault<string>("-r", "--dpuri", null),
                Locale = options.GetValueOrDefault<string>("-l", "--locale", null)
            };

            // Unregister all applications
            var result = await _registry.QueryAllApplicationsAsync(query);
            foreach (var item in result) {
                try {
                    await _registry.UnregisterApplicationAsync(item.ApplicationId);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to unregister {item.ApplicationId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Purge disabled applications not seen since specified amount of time.
        /// </summary>
        private Task PurgeDisabledApplicationsAsync(CliOptions options) {
            return _registry.PurgeDisabledApplicationsAsync(
                options.GetValueOrDefault("-f", "--for", TimeSpan.Zero));
        }

        /// <summary>
        /// List applications
        /// </summary>
        private async Task ListApplicationsAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.ListAllApplicationsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.ListApplicationsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List sites
        /// </summary>
        private async Task ListSitesAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.ListAllSitesAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.ListSitesAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query applications
        /// </summary>
        private async Task QueryApplicationsAsync(CliOptions options) {
            var query = new ApplicationRegistrationQueryApiModel {
                ApplicationUri = options.GetValueOrDefault<string>("-u", "--uri", null),
                ProductUri = options.GetValueOrDefault<string>("-p", "--product", null),
                GatewayServerUri = options.GetValueOrDefault<string>("-g", "--gwuri", null),
                DiscoveryProfileUri = options.GetValueOrDefault<string>("-r", "--dpuri", null),
                ApplicationType = options.GetValueOrDefault<ApplicationType>("-t", "--type", null),
                ApplicationName = options.GetValueOrDefault<string>("-n", "--name", null),
                Locale = options.GetValueOrDefault<string>("-l", "--locale", null),
                IncludeNotSeenSince = options.IsProvidedOrNull("-d", "--deleted"),
                DiscovererId = options.GetValueOrDefault<string>("-D", "--discovererId", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.QueryAllApplicationsAsync(query);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.QueryApplicationsAsync(query,
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get application
        /// </summary>
        private async Task GetApplicationAsync(CliOptions options) {
            var result = await _registry.GetApplicationAsync(GetApplicationId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Monitor applications
        /// </summary>
        private async Task MonitorApplicationsAsync() {
            var events = _scope.Resolve<IRegistryServiceEvents>();
            Console.WriteLine("Press any key to stop.");
            var complete = await events.SubscribeApplicationEventsAsync(PrintEvent);
            try {
                Console.ReadKey();
            }
            finally {
                await complete.DisposeAsync();
            }
        }

        /// <summary>
        /// Monitor all
        /// </summary>
        private async Task MonitorAllAsync() {
            var events = _scope.Resolve<IRegistryServiceEvents>();
            Console.WriteLine("Press any key to stop.");
            var apps = await events.SubscribeApplicationEventsAsync(PrintEvent);
            try {
                var endpoint = await events.SubscribeEndpointEventsAsync(PrintEvent);
                try {
                    var supervisor = await events.SubscribeSupervisorEventsAsync(PrintEvent);
                    try {
                        var publisher = await events.SubscribePublisherEventsAsync(PrintEvent);
                        try {
                            var discoverers = await events.SubscribeDiscovererEventsAsync(PrintEvent);
                            try {
                                var supervisors = await _registry.ListAllDiscoverersAsync();
                                var discovery = await supervisors
                                    .Select(s => events.SubscribeDiscoveryProgressByDiscovererIdAsync(
                                        s.Id, PrintProgress)).AsAsyncDisposable();
                                try {
                                    Console.ReadKey();
                                }
                                finally {
                                    await discovery.DisposeAsync();
                                }
                            }
                            finally {
                                await discoverers.DisposeAsync();
                            }
                        }
                        finally {
                            await publisher.DisposeAsync();
                        }
                    }
                    finally {
                        await supervisor.DisposeAsync();
                    }
                }
                finally {
                    await endpoint.DisposeAsync();
                }
            }
            finally {
                await apps.DisposeAsync();
            }
        }


        private string _endpointId;

        /// <summary>
        /// Get endpoint id
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private string GetEndpointId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_endpointId != null) {
                if (id == null) {
                    return _endpointId;
                }
                _endpointId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select endpoint registration
        /// </summary>
        private async Task SelectEndpointsAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _endpointId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_endpointId);
            }
            else {
                var endpointId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(endpointId)) {
                    var result = await _registry.ListAllEndpointsAsync();
                    endpointId = ConsoleEx.Select(result.Select(r => r.Registration.Id));
                    if (string.IsNullOrEmpty(endpointId)) {
                        Console.WriteLine("Nothing selected - endpoint selection cleared.");
                    }
                    else {
                        Console.WriteLine($"Selected {endpointId}.");
                    }
                }
                _endpointId = endpointId;
            }
        }

        /// <summary>
        /// List endpoint registrations
        /// </summary>
        private async Task ListEndpointsAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.ListAllEndpointsAsync(
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.ListEndpointsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query endpoints
        /// </summary>
        private async Task QueryEndpointsAsync(CliOptions options) {
            var query = new EndpointRegistrationQueryApiModel {
                Url = options.GetValueOrDefault<string>("-u", "--uri", null),
                SecurityMode = options
                    .GetValueOrDefault<OpcUa.Api.Core.Models.SecurityMode>("-m", "--mode", null),
                SecurityPolicy = options.GetValueOrDefault<string>("-l", "--policy", null),
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                Activated = options.IsProvidedOrNull("-a", "--activated"),
                EndpointState = options.GetValueOrDefault<EndpointConnectivityState>(
                    "-s", "--state", null),
                IncludeNotSeenSince = options.IsProvidedOrNull("-d", "--deleted"),
                SupervisorId = options.GetValueOrDefault<string>("-T", "--supervisorId", null),
                ApplicationId = options.GetValueOrDefault<string>("-R", "--applicationId", null),
                SiteOrGatewayId = options.GetValueOrDefault<string>("-G", "--siteId", null),
                DiscovererId = options.GetValueOrDefault<string>("-D", "--discovererId", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await _registry.QueryAllEndpointsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _registry.QueryEndpointsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Activate endpoints
        /// </summary>
        private async Task ActivateEndpointsAsync(CliOptions options) {

            var id = GetEndpointId(options, false);
            if (id != null) {
                await _registry.ActivateEndpointAsync(id);
                return;
            }

            // Activate all sign and encrypt endpoints
            var result = await _registry.QueryAllEndpointsAsync(new EndpointRegistrationQueryApiModel {
                SecurityMode = options.GetValueOrDefault<OpcUa.Api.Core.Models.SecurityMode>("-m", "mode", null),
                Activated = false
            });
            foreach (var item in result) {
                try {
                    await _registry.ActivateEndpointAsync(item.Registration.Id);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to activate {item.Registration.Id}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Deactivate endpoints
        /// </summary>
        private async Task DeactivateEndpointsAsync(CliOptions options) {

            var id = GetEndpointId(options, false);
            if (id != null) {
                await _registry.DeactivateEndpointAsync(id);
                return;
            }

            // Activate all sign and encrypt endpoints
            var result = await _registry.QueryAllEndpointsAsync(new EndpointRegistrationQueryApiModel {
                SecurityMode = options.GetValueOrDefault<OpcUa.Api.Core.Models.SecurityMode>("-m", "mode", null),
                Activated = true
            });
            foreach (var item in result) {
                try {
                    await _registry.DeactivateEndpointAsync(item.Registration.Id);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to deactivate {item.Registration.Id}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get endpoint
        /// </summary>
        private async Task GetEndpointAsync(CliOptions options) {
            var result = await _registry.GetEndpointAsync(GetEndpointId(options),
                options.IsProvidedOrNull("-S", "--server"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        private async Task GetEndpointCertificateAsync(CliOptions options) {
            var result = await _registry.GetEndpointCertificateAsync(GetEndpointId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Monitor endpoints
        /// </summary>
        private async Task MonitorEndpointsAsync() {
            var events = _scope.Resolve<IRegistryServiceEvents>();
            Console.WriteLine("Press any key to stop.");
            var complete = await events.SubscribeEndpointEventsAsync(PrintEvent);
            try {
                Console.ReadKey();
            }
            finally {
                await complete.DisposeAsync();
            }
        }

        private string _requestId;

        /// <summary>
        /// Get request id
        /// </summary>
        private string GetRequestId(CliOptions options, bool shouldThrow = true) {
            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (_requestId != null) {
                if (id == null) {
                    return _requestId;
                }
                _requestId = null;
            }
            if (id != null) {
                return id;
            }
            if (!shouldThrow) {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select request registration
        /// </summary>
        private async Task SelectRequestAsync(CliOptions options) {
            if (options.IsSet("-c", "--clear")) {
                _requestId = null;
            }
            else if (options.IsSet("-s", "--show")) {
                Console.WriteLine(_requestId);
            }
            else {
                var requestId = options.GetValueOrDefault<string>("-i", "--id", null);
                if (string.IsNullOrEmpty(requestId)) {
                    var result = await _vault.ListAllRequestsAsync();
                    requestId = ConsoleEx.Select(result.Select(r => r.RequestId));
                    if (string.IsNullOrEmpty(requestId)) {
                        Console.WriteLine("Nothing selected - request selection cleared.");
                    }
                    else {
                        Console.WriteLine($"Selected {requestId}.");
                    }
                }
                _requestId = requestId;
            }
        }

        /// <summary>
        /// Query requests
        /// </summary>
        private async Task QueryRequestsAsync(CliOptions options) {
            var query = new CertificateRequestQueryRequestApiModel {
                EntityId = options.GetValueOrDefault<string>("-e", "--entityid", null),
                State = options.GetValueOrDefault<CertificateRequestState>("-s", "--state", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await _vault.QueryAllRequestsAsync(query);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _vault.QueryRequestsAsync(query,
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List requests
        /// </summary>
        private async Task ListRequestsAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _vault.ListAllRequestsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _vault.ListRequestsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get request
        /// </summary>
        private async Task GetRequestAsync(CliOptions options) {
            var result = await _vault.GetRequestAsync(GetRequestId(options));
            PrintResult(options, result);
        }

        /// <summary>
        /// Delete request
        /// </summary>
        private async Task DeleteRequestAsync(CliOptions options) {
            await _vault.DeleteRequestAsync(GetRequestId(options));
        }

        /// <summary>
        /// Accept request
        /// </summary>
        private async Task AcceptRequestAsync(CliOptions options) {
            await _vault.AcceptRequestAsync(GetRequestId(options));
        }

        /// <summary>
        /// Reject request
        /// </summary>
        private async Task RejectRequestAsync(CliOptions options) {
            await _vault.RejectRequestAsync(GetRequestId(options));
        }

        /// <summary>
        /// Approve request
        /// </summary>
        private async Task ApproveRequestAsync(CliOptions options) {
            await _vault.ApproveRequestAsync(GetRequestId(options));
        }

        /// <summary>
        /// Start and finish keypair request
        /// </summary>
        private async Task KeyPairRequestAsync(CliOptions options) {
            if (options.IsProvidedOrNull("-f", "--finish") == true) {
                var result = await _vault.FinishKeyPairRequestAsync(GetRequestId(options));
                PrintResult(options, result);
            }
            else {
                var result = await _vault.StartNewKeyPairRequestAsync(new StartNewKeyPairRequestApiModel {
                    CertificateType = options.GetValue<TrustGroupType>("-t", "--type"),
                    EntityId = options.GetValue<string>("-e", "--entityId"),
                    GroupId = options.GetValue<string>("-g", "--groupId"),
                    SubjectName = options.GetValueOrDefault<string>("-s", "--subject", null),
                    DomainNames = options.GetValueOrDefault<string>("-d", "--domain", null)?
                        .YieldReturn().ToList()
                });
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Start and finish signing request
        /// </summary>
        private async Task SigningRequestAsync(CliOptions options) {
            if (options.IsProvidedOrNull("-f", "--finish") == true) {
                var result = await _vault.FinishSigningRequestAsync(GetRequestId(options));
                PrintResult(options, result);
            }
            else {
                var result = await _vault.StartSigningRequestAsync(new StartSigningRequestApiModel {
                    CertificateRequest = _serializer.FromObject(
                        options.GetValue<byte[]>("-c", "--csr")),
                    EntityId = options.GetValue<string>("-e", "--entityId"),
                    GroupId = options.GetValue<string>("-g", "--groupId")
                });
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Remove trust relationship
        /// </summary>
        private async Task RemoveTrustRelationshipAsync(CliOptions options) {
            await _vault.RemoveTrustRelationshipAsync(
                options.GetValue<string>("-e", "--entityId"),
                options.GetValue<string>("-t", "--trustedId"));
        }

        /// <summary>
        /// Get trusted certificates
        /// </summary>
        private async Task GetTrustedCertificatesAsync(CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await _vault.ListAllTrustedCertificatesAsync(
                    options.GetValue<string>("-e", "--entityId"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await _vault.ListTrustedCertificatesAsync(
                    options.GetValue<string>("-e", "--entityId"),
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Add trust relationship
        /// </summary>
        private async Task AddTrustRelationshipAsync(CliOptions options) {
            await _vault.AddTrustRelationshipAsync(
                options.GetValue<string>("-e", "--entityId"),
                options.GetValue<string>("-t", "--trustedId"));
        }

        /// <summary>
        /// Get status
        /// </summary>
        private async Task GetStatusAsync(CliOptions options) {
            Console.WriteLine("Twin:      " + await _twin.GetServiceStatusAsync());
            Console.WriteLine("Registry:  " + await _registry.GetServiceStatusAsync());
            Console.WriteLine("Publisher: " + await _publisher.GetServiceStatusAsync());
            Console.WriteLine("Jobs:      " + await _jobs.GetServiceStatusAsync());
            Console.WriteLine("Vault:     " + await _vault.GetServiceStatusAsync());
            Console.WriteLine("History:   " + await _history.GetServiceStatusAsync());
        }

        /// <summary>
        /// Print result
        /// </summary>
        private void PrintResult<T>(CliOptions options, T result) {
            Console.WriteLine("==================");
            Console.WriteLine(_serializer.SerializeToString(result,
                options.GetValueOrDefault("-F", "--format", SerializeOption.Indented)));
            Console.WriteLine("==================");
        }

        /// <summary>
        /// Print progress
        /// </summary>
        private static Task PrintProgress(DiscoveryProgressApiModel ev) {
            switch (ev.EventType) {
                case DiscoveryProgressType.Pending:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Total} waiting...");
                    break;
                case DiscoveryProgressType.Started:
                    Console.WriteLine($"{ev.DiscovererId}: Started.");
                    Console.WriteLine("==========================================");
                    break;
                case DiscoveryProgressType.NetworkScanStarted:
                    Console.WriteLine($"{ev.DiscovererId}: Scanning network...");
                    Console.WriteLine("==========================================");
                    break;
                case DiscoveryProgressType.NetworkScanResult:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" {ev.Discovered} addresses found - NEW: {ev.Result}...");
                    break;
                case DiscoveryProgressType.NetworkScanProgress:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" {ev.Discovered} addresses found");
                    break;
                case DiscoveryProgressType.NetworkScanFinished:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" {ev.Discovered} addresses found - complete!");
                    break;
                case DiscoveryProgressType.PortScanStarted:
                    Console.WriteLine($"{ev.DiscovererId}: Scanning ports...");
                    Console.WriteLine("==========================================");
                    break;
                case DiscoveryProgressType.PortScanResult:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" {ev.Discovered} ports found - NEW: {ev.Result}...");
                    break;
                case DiscoveryProgressType.PortScanProgress:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" {ev.Discovered} ports found");
                    break;
                case DiscoveryProgressType.PortScanFinished:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" {ev.Discovered} ports found - complete!");
                    break;
                case DiscoveryProgressType.ServerDiscoveryStarted:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" Finding servers...");
                    Console.WriteLine("==========================================");
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryStarted:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" ... {ev.Discovered} servers found - find " +
                        $"endpoints on {ev.RequestDetails["url"]}...");
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryFinished:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" ... {ev.Discovered} servers found - {ev.Result} " +
                        $"endpoints found on {ev.RequestDetails["url"]}...");
                    break;
                case DiscoveryProgressType.ServerDiscoveryFinished:
                    Console.WriteLine($"{ev.DiscovererId}: {ev.Progress}/{ev.Total} :" +
                        $" ... {ev.Discovered} servers found.");
                    break;
                case DiscoveryProgressType.Cancelled:
                    Console.WriteLine($"{ev.DiscovererId}: Cancelled.");
                    Console.WriteLine("==========================================");
                    break;
                case DiscoveryProgressType.Error:
                    Console.WriteLine($"{ev.DiscovererId}: Failure: {ev.Result}");
                    Console.WriteLine("==========================================");
                    break;
                case DiscoveryProgressType.Finished:
                    Console.WriteLine($"{ev.DiscovererId}: Completed.");
                    Console.WriteLine("==========================================");
                    break;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Print event
        /// </summary>
        private Task PrintEvent(EndpointEventApiModel ev) {
            Console.WriteLine(_serializer.SerializePretty(ev));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Print event
        /// </summary>
        private Task PrintEvent(ApplicationEventApiModel ev) {
            Console.WriteLine(_serializer.SerializePretty(ev));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Print event
        /// </summary>
        private Task PrintEvent(SupervisorEventApiModel ev) {
            Console.WriteLine(_serializer.SerializePretty(ev));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Print event
        /// </summary>
        private Task PrintEvent(GatewayEventApiModel ev) {
            Console.WriteLine(_serializer.SerializePretty(ev));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Print event
        /// </summary>
        private Task PrintEvent(DiscovererEventApiModel ev) {
            Console.WriteLine(_serializer.SerializePretty(ev));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Print event
        /// </summary>
        private Task PrintEvent(PublisherEventApiModel ev) {
            Console.WriteLine(_serializer.SerializePretty(ev));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Print sample
        /// </summary>
        private Task PrintSample(MonitoredItemMessageApiModel samples) {
            Console.WriteLine(_serializer.SerializeToString(samples));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Build discovery config model from options
        /// </summary>
        private DiscoveryConfigApiModel BuildDiscoveryConfig(CliOptions options) {
            var config = new DiscoveryConfigApiModel();
            var empty = true;

            if (options.IsSet("-a", "--activate")) {
                config.ActivationFilter = new EndpointActivationFilterApiModel {
                    SecurityMode = SecurityMode.None
                };
                empty = false;
            }

            var addressRange = options.GetValueOrDefault<string>("-r", "--address-ranges", null);
            if (addressRange != null) {
                if (addressRange == "true") {
                    config.AddressRangesToScan = "";
                }
                else {
                    config.AddressRangesToScan = addressRange;
                }
                empty = false;
            }

            var portRange = options.GetValueOrDefault<string>("-p", "--port-ranges", null);
            if (portRange != null) {
                if (portRange == "true") {
                    config.PortRangesToScan = "";
                }
                else {
                    config.PortRangesToScan = portRange;
                }
                empty = false;
            }

            var netProbes = options.GetValueOrDefault<int>("-R", "--address-probes", null);
            if (netProbes != null && netProbes != 0) {
                config.MaxNetworkProbes = netProbes;
                empty = false;
            }

            var portProbes = options.GetValueOrDefault<int>("-P", "--port-probes", null);
            if (portProbes != null && portProbes != 0) {
                config.MaxPortProbes = portProbes;
                empty = false;
            }

            var netProbeTimeout = options.GetValueOrDefault<int>("-T", "--address-probe-timeout", null);
            if (netProbeTimeout != null && netProbeTimeout != 0) {
                config.NetworkProbeTimeout = TimeSpan.FromMilliseconds(netProbeTimeout.Value);
                empty = false;
            }

            var portProbeTimeout = options.GetValueOrDefault<int>("-t", "--port-probe-timeout", null);
            if (portProbeTimeout != null && portProbeTimeout != 0) {
                config.PortProbeTimeout = TimeSpan.FromMilliseconds(portProbeTimeout.Value);
                empty = false;
            }

            var idleTime = options.GetValueOrDefault<int>("-I", "--idle-time", null);
            if (idleTime != null && idleTime != 0) {
                config.IdleTimeBetweenScans = TimeSpan.FromSeconds(idleTime.Value);
                empty = false;
            }
            return empty ? null : config;
        }

        /// <summary>
        /// Build publisher config model from options
        /// </summary>
        private PublisherConfigApiModel BuildPublisherConfig(CliOptions options) {
            var config = new PublisherConfigApiModel();
            var empty = true;

            var url = options.GetValueOrDefault<string>("-o", "--orchestrator", null);
            if (url != null) {
                if (url == "true") {
                    config.JobOrchestratorUrl = "";
                }
                else {
                    config.JobOrchestratorUrl = url;
                }
                empty = false;
            }

            var maxWorkers = options.GetValueOrDefault<int>("-w", "--max-workers", null);
            if (maxWorkers != null && maxWorkers >= 0) {
                config.MaxWorkers = maxWorkers;
                empty = false;
            }

            var checkIntervalSec = options.GetValueOrDefault<int>("-c", "--check-interval", null);
            if (checkIntervalSec != null && checkIntervalSec != 0) {
                config.JobCheckInterval = TimeSpan.FromSeconds(checkIntervalSec.Value);
                empty = false;
            }

            var heartbeatInterval = options.GetValueOrDefault<int>("-h", "--heartbeat", null);
            if (heartbeatInterval != null && heartbeatInterval != 0) {
                config.HeartbeatInterval = TimeSpan.FromSeconds(heartbeatInterval.Value);
                empty = false;
            }

            return empty ? null : config;
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintHelp() {
            Console.WriteLine(
                @"
aziiotcli - Allows to script Industrial IoT Services api.
usage:      aziiotcli command [options]

Commands and Options

     console     Run in interactive mode. Enter commands after the >
     exit        Exit interactive mode and thus the cli.
     status      Print status of services
     monitor     Monitor all events from all services

     gateways    Manage edge gateways
     publishers  Manage publisher modules
     supervisors Manage twin modules
     discoverers Manage discovery modules

     apps        Manage applications
     endpoints   Manage endpoints
     nodes       Call twin module services on endpoint

     groups      Manage trust groups (Experimental)
     trust       Manage trust between above entities (Experimental)
     requests    Manage certificate requests (Experimental)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintApplicationsHelp() {
            Console.WriteLine(
                @"
Manage applications registry.

Commands and Options

     select      Select application as -i/--id argument in all calls.
        with ...
        -i, --id        Application id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     monitor     Monitor changes to applications.

     sites       List application sites
        with ...
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all sites (unpaged)
        -F, --format    Json format for result

     list        List applications
        with ...
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all application infos (unpaged)
        -F, --format    Json format for result

     add         Register server and endpoints through discovery url
        with ...
        -i, --id        Request id for the discovery request.
        -u, --url       Url of the discovery endpoint (mandatory)
        -a, --activate  Activate all endpoints during onboarding.
        -m, --monitor   Monitor the discovery process to completion.

     discover    Discover applications and endpoints through config.
        with ...
        -i, --id        Request id for the discovery request.
        -d, --discovery Set discovery mode to use
        -a, --activate  Activate all endpoints during onboarding.
        -m, --monitor   Monitor the discovery process to completion.

     cancel      Cancel application discovery.
        with ...
        -i, --id        Request id of the discovery request (mandatory).

     register    Manually register Application
        with ...
        -u, --url       Uri of the application (mandatory)
        -n  --name      Application name of the application
        -t, --type      Application type (default to Server)
        -p, --product   Product uri of the application
        -d, --discovery Url of the discovery endpoint
        -r, --dpuri     Discovery profile uri
        -g, --gwuri     Gateway uri
        -F, --format    Json format for result

     query       Find applications
        with ...
        -P, --page-size Size of page
        -A, --all       Return all application infos (unpaged)
        -u, --uri       Application uri of the application.
        -r, --dpuri     Discovery profile uri
        -g, --gwuri     Gateway uri
        -n  --name      Application name of the application
        -t, --type      Application type (default to all)
        -s, --state     Application state (default to all)
        -p, --product   Product uri of the application
        -d, --deleted   Include soft deleted applications.
        -D  --discovererId
                        Onboarded from specified discoverer.
        -F, --format    Json format for result

     get         Get application
        with ...
        -i, --id        Id of application to get (mandatory)
        -F, --format    Json format for result

     disable     Disable application
        with ...
        -i, --id        Id of application to get (mandatory)

     enable      Enable application
        with ...
        -i, --id        Id of application to get (mandatory)

     update      Update application
        with ...
        -i, --id        Id of application to update (mandatory)
        -n, --name      Application name
        -r, --dpuri     Discovery profile uri
        -g, --gwuri     Gateway uri
        -p, --product   Product uri of the application

     unregister  Unregister application
        with ...
        -i, --id        Id of application to unregister
                        -or- all matching
        -u, --uri       Application uri and/or
        -n  --name      Application name and/or
        -t, --type      Application type and/or
        -p, --product   Product uri and/or
        -r, --dpuri     Discovery profile uri
        -g, --gwuri     Gateway uri
        -s, --state     Application state (default to all)

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
        private void PrintEndpointsHelp() {
            Console.WriteLine(
                @"
Manage endpoints in registry.

Commands and Options

     select      Select endpoint as -i/--id argument in all calls.
        with ...
        -i, --id        Endpoint id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     monitor     Monitor changes to endpoints.

     list        List endpoints
        with ...
        -S, --server    Return only server state (default:false)
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
        -F, --format    Json format for result

     query       Find endpoints
        -S, --server    Return only server state (default:false)
        -u, --uri       Endpoint uri to seach for
        -m, --mode      Security mode to search for
        -p, --policy    Security policy to match
        -a, --activated Only return activated or deactivated.
        -c, --connected Only return connected or disconnected.
        -s, --state     Only return endpoints with specified state.
        -d, --deleted   Include soft deleted endpoints.
        -T  --supervisorId
                        Return endpoints with provided supervisor.
        -R  --applicationId
                        Return endpoints for specified Application.
        -G  --siteId    Site or Gateway identifier to filter with.
        -D  --discovererId
                        Onboarded from specified discoverer.
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
        -F, --format    Json format for result

     get         Get endpoint
        with ...
        -i, --id        Id of endpoint to retrieve (mandatory)
        -S, --server    Return only server state (default:false)
        -F, --format    Json format for result

     validate    Get endpoint certificate chain and validate
        with ...
        -i, --id        Id of endpoint to retrieve (mandatory)
        -F, --format    Json format for result

     activate    Activate endpoints
        with ...
        -i, --id        Id of endpoint or ...
        -m, --mode      Security mode (default:SignAndEncrypt)

     deactivate  Deactivate endpoints
        with ...
        -i, --id        Id of endpoint or ...
        -m, --mode      Security mode (default:SignAndEncrypt)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintNodesHelp() {
            Console.WriteLine(
                @"
Access address space through configured server endpoint.

Commands and Options

     select      Select node id as -n/--nodeid argument in all calls.
        with ...
        -n, --nodeId    Node id to select.
        -i, --id        Endpoint id to use for selection if browsing.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     browse      Browse nodes on endpoint
        with ...
        -i, --id        Id of endpoint to browse (mandatory)
        -n, --nodeid    Node to browse
        -x, --maxrefs   Max number of references
        -d, --direction Browse direction (Forward, Backward, Both)
        -r, --recursive Browse recursively and read node values
        -v, --readvalue Read node values in browse
        -t, --targets   Only return target nodes
        -s, --silent    Only show errors
        -F, --format    Json format for result

     next        Browse next nodes
        with ...
        -C, --continuation
                        Continuation from previous result.
        -F, --format    Json format for result

     read        Read node value on endpoint
        with ...
        -i, --id        Id of endpoint to read value from (mandatory)
        -n, --nodeid    Node to read value from (mandatory)
        -F, --format    Json format for result

     write       Write node value on endpoint
        with ...
        -i, --id        Id of endpoint to write value on (mandatory)
        -n, --nodeid    Node to write value to (mandatory)
        -t, --datatype  Datatype of value (mandatory)
        -v, --value     Value to write (mandatory)

     metadata    Get Call meta data
        with ...
        -i, --id        Id of endpoint with meta data (mandatory)
        -n, --nodeid    Method Node to get meta data for (mandatory)
        -F, --format    Json format for result

     call        Call method node on endpoint
        with ...
        -i, --id        Id of endpoint to call method on (mandatory)
        -n, --nodeid    Method Node to call (mandatory)
        -o, --objectid  Object context for method

     publish     Publish items from endpoint
        with ...
        -i, --id        Id of endpoint to publish value from (mandatory)
        -n, --nodeid    Node to browse (mandatory)

     monitor     Monitor published items on endpoint
        with ...
        -i, --id        Id of endpoint to monitor nodes on (mandatory)

     list        List published items on endpoint
        with ...
        -i, --id        Id of endpoint with published nodes (mandatory)
        -C, --continuation
                        Continuation from previous result.
        -A, --all       Return all items (unpaged)
        -F, --format    Json format for result

     unpublish   Unpublish items on endpoint
        with ...
        -i, --id        Id of endpoint to publish value from (mandatory)
        -n, --nodeid    Node to browse (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintGatewaysHelp() {
            Console.WriteLine(
                @"
Manage and configure Edge Gateways

Commands and Options

     select      Select gateway as -i/--id argument in all calls.
        with ...
        -i, --id        Gateway id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     monitor     Monitor changes to gateways.

     update      Update gateway
        with ...
        -i, --id        Id of gateway to retrieve (mandatory)
        -s, --siteId    Updated site of the gateway.

     list        List gateways
        with ...
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all gateways (unpaged)
        -F, --format    Json format for result

     query       Find gateways
        -c, --connected Only return connected or disconnected.
        -s, --siteId    Site of the gateways.
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
        -F, --format    Json format for result

     get         Get gateway info
        with ...
        -i, --id        Id of gateway to retrieve (mandatory)
        -F, --format    Json format for result

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintPublishersHelp() {
            Console.WriteLine(
                @"
Manage and configure Publisher modules

Commands and Options

     select      Select publisher as -i/--id argument in all calls.
        with ...
        -i, --id        Publisher id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     monitor     Monitor changes to publishers.

     update      Update publisher
        with ...
        -i, --id        Id of publisher to retrieve (mandatory)
        -s, --siteId    Updated site of the publisher.
        -l, --log-level Set publisher module logging level
        -o, --orchestrator
                        Orchestrator url
        -w, --max-workers
                        Max number of workers to to use
        -c, --check-interval
                        Job check interval in seconds
        -h, --heartbeat Heartbeat interval in seconds

     list        List publishers
        with ...
        -S, --server    Return only server state (default:false)
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all publishers (unpaged)
        -F, --format    Json format for result

     query       Find publishers
        -S, --server    Return only server state (default:false)
        -c, --connected Only return connected or disconnected.
        -s, --siteId    Site of the publishers.
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
        -F, --format    Json format for result

     get         Get publisher
        with ...
        -S, --server    Return only server state (default:false)
        -i, --id        Id of publisher to retrieve (mandatory)
        -F, --format    Json format for result

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintSupervisorsHelp() {
            Console.WriteLine(
                @"
Manage and configure Twin modules (endpoint supervisors)

Commands and Options

     select      Select supervisor as -i/--id argument in all calls.
        with ...
        -i, --id        Supervisor id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     monitor     Monitor changes to supervisors.

     list        List supervisors
        with ...
        -S, --server    Return only server state (default:false)
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all supervisors (unpaged)
        -F, --format    Json format for result

     query       Find supervisors
        -S, --server    Return only server state (default:false)
        -c, --connected Only return connected or disconnected.
        -s, --siteId    Site of the supervisors.
        -e, --endpoint  Manages Endpoint twin with given id.
        -P, --page-size Size of page
        -A, --all       Return all supervisors (unpaged)
        -F, --format    Json format for result

     get         Get supervisor
        with ...
        -S, --server    Return only server state (default:false)
        -i, --id        Id of supervisor to retrieve (mandatory)
        -F, --format    Json format for result

     status      Get supervisor runtime status
        with ...
        -i, --id        Id of supervisor to get status of (mandatory)
        -F, --format    Json format for result

     update      Update supervisor
        with ...
        -i, --id        Id of supervisor to update (mandatory)
        -s, --siteId    Updated site of the supervisor.
        -l, --log-level Set supervisor module logging level

     reset       Reset supervisor
        with ...
        -i, --id        Id of supervisor to reset (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintDiscoverersHelp() {
            Console.WriteLine(
                @"
Manage and configure discovery modules

Commands and Options

     select      Select discoverer as -i/--id argument in all calls.
        with ...
        -i, --id        Discoverer id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     monitor     Monitor changes to discoverer twins.

     monitor     Monitor discovery progress of specified discoverer.
        with ...
        -i, --id        Discoverer to monitor

     list        List discoverers
        with ...
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all discoverers (unpaged)
        -F, --format    Json format for result

     query       Find discoverers
        -c, --connected Only return connected or disconnected.
        -d, --discovery Discovery state.
        -s, --siteId    Site of the discoverers.
        -P, --page-size Size of page
        -A, --all       Return all discoverers (unpaged)
        -F, --format    Json format for result

     get         Get discoverer
        with ...
        -i, --id        Id of discoverer to retrieve (mandatory)
        -F, --format    Json format for result

     update      Update discoverer
        with ...
        -i, --id        Id of discoverer to update (mandatory)
        -s, --siteId    Updated site of the discoverer.
        -d, --discovery Set discoverer discovery mode
        -l, --log-level Set discoverer module logging level
        -a, --activate  Activate all endpoints during onboarding.
        -p, --port-ranges
                        Port ranges to scan.
        -r, --address-ranges
                        Address range to scan.
        -P, --port-probes
                        Max port probes to use.
        -R, --address-probes
                        Max networking probes to use.

     scan        Run a scan
        with ...
        -i, --id        Id of discoverer to run scanning on (mandatory)
        -d, --discovery Set discoverer discovery mode
        -a, --activate  Activate all endpoints during onboarding.
        -I, --idle-time Idle time between scans in seconds
        -p, --port-ranges
                        Port ranges to scan.
        -r, --address-ranges
                        Address range to scan.
        -P, --max-port-probes
                        Max port probes to use.
        -R, --max-address-probes
                        Max networking probes to use.
        -T, --address-probe-timeout
                        Network probe timeout in milliseconds
        -t, --port-probe-timeout
                        Port probe timeout in milliseconds

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintJobsHelp() {
            Console.WriteLine(
                @"
Manage jobs

Commands and Options

     select      Select job as -i/--id argument in all calls.
        with ...
        -i, --id        Job id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     list        List jobs
        with ...
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all jobs (unpaged)
        -F, --format    Json format for result

     query       Find jobs
        -S, --status    Status of the job
        -t, --type      Job type.
        -n, --name      Name of the job.
        -P, --page-size Size of page
        -A, --all       Return all jobs (unpaged)
        -F, --format    Json format for result

     get         Get Job
        with ...
        -i, --id        Id of job to retrieve (mandatory)
        -F, --format    Json format for result

     workers     List workers
        with ...
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all jobs (unpaged)
        -F, --format    Json format for result

     cancel      Cancel job
        with ...
        -i, --id        Id of job to cancel (mandatory)

     restart     Restart job
        with ...
        -i, --id        Id of job to restart (mandatory)

     delete      Delete job
        with ...
        -i, --id        Id of job to delete (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintGroupsHelp() {
            Console.WriteLine(
                @"
Manage entity trust groups

Commands and Options

     select      Select group as -i/--id argument in all calls.
        with ...
        -i, --id        Group id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     list        List groups
        with ...
        -C, --continuation
                        Continuation from previous result.
        -A, --all       Return all items (unpaged)
        -F, --format    Json format for result

     root        Create new root group
        with ...
        -n, --name      Name of the group (mandatory)
        -s, --subject   Subject distinguished name (mandatory)
        -a, --algorithm Signature algorithm
        -l, --lifetime  Issued certificate life times
        -s, --keysize   Issued Key size
        -F, --format    Json format for result

     child       Create new sub group
        with ...
        -p, --parent    Parent id for the group (mandatory)
        -n, --name      Name of the group (mandatory)
        -s, --subject   Subject distinguished name (mandatory)
        -a, --algorithm Signature algorithm
        -l, --lifetime  Issued certificate life times
        -s, --keysize   Issued Key size
        -F, --format    Json format for result

     delete      Delete group
        with ...
        -i, --id        Id of group to delete (mandatory)

     delete      Renew group issuer certificate
        with ...
        -i, --id        Id of group for renewal (mandatory)

     update      Update group information
        with ...
        -i, --id        Id of the group to update (mandatory)
        -n, --name      Name of the group (mandatory)
        -s, --subject   Subject distinguished name (mandatory)
        -a, --algorithm Signature algorithm
        -l, --lifetime  Issued certificate life times

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintRequestsHelp() {
            Console.WriteLine(
                @"
Submit and manage Certificate requests

Commands and Options

     select      Select request as -i/--id argument in all calls.
        with ...
        -i, --id        Request id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     list        List requests
        with ...
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all requests (unpaged)
        -F, --format    Json format for result

     query       Find requests
        -s, --state     State of request
        -e, --entityId  Entity id for which request was submitted
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
        -F, --format    Json format for result

     sign        Submit certificate signing request
        with ...
        -g, --groupId   Group to submit request to (mandatory)
        -e, --entityId  Entity id to create key for (mandatory)
        or ...
        -f, --finish    Retrieve finished signing result, then
        -i, --id        Id of request to finish (mandatory)

     get         Get request
        with ...
        -i, --id        Id of request to retrieve (mandatory)
        -F, --format    Json format for result

     approve     Approve request
        with ...
        -i, --id        Id of request to approve (mandatory)

     reject      Reject request
        with ...
        -i, --id        Id of request to reject (mandatory)

     accept      Accept request
        with ...
        -i, --id        Id of request to retrieve (mandatory)

     delete      Delete request
        with ...
        -i, --id        Id of request to retrieve (mandatory)

     keypair     Submit key pair generation request
        with ...
        -g, --groupId   Group to submit request to (mandatory)
        -e, --entityId  Entity id to create key for (mandatory)
        -t, --type      Type of certificate to generate (mandatory)
        -a, --subjct    Subject name (mandatory)
        -d, --domain    Domain name (mandatory)
        or ...
        -f, --finish    Retrieve finished signing result, then
        -i, --id        Id of request to finish (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintTrustHelp() {
            Console.WriteLine(
                @"
Manage trust between entities

Commands and Options

     create      Add trust relationship
        with ...
        -e, --entityId  Id of entity (mandatory)
        -t, --trustedId Id of trusted entity (mandatory)

     get         Get certificates the entity trusts.
        with ...
        -e, --entityId  Id of entity (mandatory)
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all items (unpaged)
        -F, --format    Json format for result

     delete      Remove trust relationship
        with ...
        -e, --entityId  Id of entity (mandatory)
        -t, --trustedId Id of entity not to trust (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        private readonly ILifetimeScope _scope;
        private readonly ITwinServiceApi _twin;
        private readonly IJobsServiceApi _jobs;
        private readonly IPublisherServiceApi _publisher;
        private readonly IRegistryServiceApi _registry;
        private readonly IHistoryServiceApi _history;
        private readonly IVaultServiceApi _vault;
        private readonly IJsonSerializer _serializer;
    }
}
