// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Cli {
    using Microsoft.Azure.IIoT.OpcUa.Api.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Autofac;
    using AutofacSerilogIntegration;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Diagnostics;
    using Serilog;

    /// <summary>
    /// Api command line interface
    /// </summary>
    public class Program {

        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        public static IContainer ConfigureContainer(
            IConfigurationRoot configuration) {
            var builder = new ContainerBuilder();

            // Register configuration interfaces and logger
            builder.RegisterInstance(new ApiConfig(configuration))
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.RegisterLogger(LogEx.Trace());

            // Register http client module
            builder.RegisterModule<HttpClientModule>();

            // Use bearer authentication
            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            // Use device code token provider to get tokens
            builder.RegisterType<DeviceCodeTokenProvider>()
                .AsImplementedInterfaces().SingleInstance();

            // Register twin, vault, and registry services clients
            builder.RegisterType<TwinServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<RegistryServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VaultServiceClient>()
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
                .AddFromDotEnvFile()
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
        public static async Task RunAsync(string[] args, IComponentContext context) {
            var twin = context.Resolve<ITwinServiceApi>();
            var registry = context.Resolve<IRegistryServiceApi>();
            var vault = context.Resolve<IVaultServiceApi>();
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
                            interactive = true;
                            break;
                        case "status":
                            options = new CliOptions(args);
                            await GetStatusAsync(context, twin, registry, vault, options);
                            break;
                        case "apps":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "sites":
                                    await ListSitesAsync(registry, options);
                                    break;
                                case "register":
                                    await RegisterApplicationAsync(registry, options);
                                    break;
                                case "add":
                                    await RegisterServerAsync(registry, options);
                                    break;
                                case "discover":
                                    await DiscoverServerAsync(registry, options);
                                    break;
                                case "update":
                                    await UpdateApplicationAsync(registry, options);
                                    break;
                                case "disable":
                                    await DisableApplicationAsync(registry, options);
                                    break;
                                case "enable":
                                    await EnableApplicationAsync(registry, options);
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
                        case "endpoints":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "update":
                                    await UpdateEndpointAsync(registry, options);
                                    break;
                                case "get":
                                    await GetEndpointAsync(registry, options);
                                    break;
                                case "list":
                                    await ListEndpointsAsync(registry, options);
                                    break;
                                case "query":
                                    await QueryEndpointsAsync(registry, options);
                                    break;
                                case "activate":
                                    await ActivateEndpointsAsync(registry, options);
                                    break;
                                case "deactivate":
                                    await DeactivateEndpointsAsync(registry, options);
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
                        case "groups":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "root":
                                    await CreateRootAsync(vault, options);
                                    break;
                                case "child":
                                    await CreateGroupAsync(vault, options);
                                    break;
                                case "update":
                                    await UpdateGroupAsync(vault, options);
                                    break;
                                case "delete":
                                    await DeleteGroupAsync(vault, options);
                                    break;
                                case "list":
                                    await ListGroupsAsync(vault, options);
                                    break;
                                case "get":
                                    await GetGroupAsync(vault, options);
                                    break;
                                case "renew":
                                    await RenewIssuerCertAsync(vault, options);
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
                                    await SigningRequestAsync(vault, options);
                                    break;
                                case "keypair":
                                    await KeyPairRequestAsync(vault, options);
                                    break;
                                case "approve":
                                    await ApproveRequestAsync(vault, options);
                                    break;
                                case "reject":
                                    await RejectRequestAsync(vault, options);
                                    break;
                                case "accept":
                                    await AcceptRequestAsync(vault, options);
                                    break;
                                case "delete":
                                    await DeleteRequestAsync(vault, options);
                                    break;
                                case "list":
                                    await ListRequestsAsync(vault, options);
                                    break;
                                case "get":
                                    await GetRequestAsync(vault, options);
                                    break;
                                case "query":
                                    await QueryRequestsAsync(vault, options);
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
                                    await AddTrustRelationshipAsync(vault, options);
                                    break;
                                case "get":
                                    await GetTrustedCertificatesAsync(vault, options);
                                    break;
                                case "delete":
                                    await RemoveTrustRelationshipAsync(vault, options);
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
                        case "supervisors":
                            if (args.Length < 2) {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "get":
                                    await GetSupervisorAsync(registry, options);
                                    break;
                                case "status":
                                    await GetSupervisorStatusAsync(registry, options);
                                    break;
                                case "update":
                                    await UpdateSupervisorAsync(registry, options);
                                    break;
                                case "reset":
                                    await ResetSupervisorAsync(registry, options);
                                    break;
                                case "list":
                                    await ListSupervisorsAsync(registry, options);
                                    break;
                                case "query":
                                    await QuerySupervisorsAsync(registry, options);
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
                            options = new CliOptions(args, 2);
                            switch (command) {
                                case "browse":
                                    await BrowseAsync(twin, options);
                                    break;
                                case "publish":
                                    await PublishAsync(twin, options);
                                    break;
                                case "unpublish":
                                    await UnpublishAsync(twin, options);
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
        private static async Task MethodCallAsync(ITwinServiceApi service,
            CliOptions options) {
            var result = await service.NodeMethodCallAsync(
                options.GetValue<string>("-i", "--id"),
                new MethodCallRequestApiModel {
                    MethodId = options.GetValue<string>("-n", "--nodeid"),
                    ObjectId = options.GetValue<string>("-o", "--objectid")

                    // ...
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        private static async Task MethodMetadataAsync(ITwinServiceApi service,
            CliOptions options) {
            var result = await service.NodeMethodGetMetadataAsync(
                options.GetValue<string>("-i", "--id"),
                new MethodMetadataRequestApiModel {
                    MethodId = options.GetValue<string>("-n", "--nodeid")
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Write value
        /// </summary>
        private static async Task WriteAsync(ITwinServiceApi service,
            CliOptions options) {
            var result = await service.NodeValueWriteAsync(
                options.GetValue<string>("-i", "--id"),
                new ValueWriteRequestApiModel {
                    NodeId = options.GetValue<string>("-n", "--nodeid"),
                    DataType = options.GetValueOrDefault<string>("-t", "--datatype", null),
                    Value = options.GetValue<string>("-v", "--value")
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        private static async Task ReadAsync(ITwinServiceApi service,
            CliOptions options) {
            var result = await service.NodeValueReadAsync(
                options.GetValue<string>("-i", "--id"),
                new ValueReadRequestApiModel {
                    NodeId = options.GetValue<string>("-n", "--nodeid")
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Publish node
        /// </summary>
        private static async Task PublishAsync(ITwinServiceApi service,
            CliOptions options) {
            var result = await service.NodePublishStartAsync(
                options.GetValue<string>("-i", "--id"),
                new PublishStartRequestApiModel {
                    Item = new PublishedItemApiModel {
                        NodeId = options.GetValue<string>("-n", "--nodeid")
                    }
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Unpublish node
        /// </summary>
        private static async Task UnpublishAsync(ITwinServiceApi service,
            CliOptions options) {
            var result = await service.NodePublishStopAsync(
                options.GetValue<string>("-i", "--id"),
                new PublishStopRequestApiModel {
                    NodeId = options.GetValue<string>("-n", "--nodeid")
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        private static async Task BrowseAsync(ITwinServiceApi service,
            CliOptions options) {
            var id = options.GetValue<string>("-i", "--id");
            var silent = options.IsSet("-s", "--silent");
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
                    var result = await service.NodeBrowseAsync(id, request);
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
                                var read = await service.NodeValueReadAsync(id,
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
        /// List published nodes
        /// </summary>
        private static async Task ListNodesAsync(ITwinServiceApi service,
            CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await service.NodePublishListAllAsync(
                    options.GetValue<string>("-i", "--id"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.NodePublishListAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValue<string>("-i", "--id"));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List groups
        /// </summary>
        private static async Task ListGroupsAsync(IVaultServiceApi service, CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await service.ListAllGroupsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListGroupsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get root group
        /// </summary>
        private static async Task CreateRootAsync(IVaultServiceApi service,
            CliOptions options) {
            var result = await service.CreateRootAsync(new TrustGroupRootCreateRequestApiModel {
                IssuedKeySize = options.GetValueOrDefault<ushort>("-s", "--keysize", null),
                IssuedLifetime = options.GetValueOrDefault<TimeSpan>("-l", "--lifetime", null),
                IssuedSignatureAlgorithm = options.GetValueOrDefault<SignatureAlgorithm>(
                        "-a", "--algorithm", null),
                Name = options.GetValue<string>("-n", "--name"),
                SubjectName = options.GetValue <string>("-s", "--subject")
            });
            PrintResult(options, result);
        }

        /// <summary>
        /// Create group
        /// </summary>
        private static async Task CreateGroupAsync(IVaultServiceApi service,
            CliOptions options) {
            var result = await service.CreateGroupAsync(new TrustGroupRegistrationRequestApiModel {
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
        private static async Task GetGroupAsync(IVaultServiceApi service,
            CliOptions options) {
            var result = await service.GetGroupAsync(
                options.GetValue<string>("-i", "--id"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Delete group
        /// </summary>
        private static async Task DeleteGroupAsync(IVaultServiceApi service,
            CliOptions options) {
            await service.DeleteGroupAsync(
                options.GetValue<string>("-i", "--id"));
        }

        /// <summary>
        /// Renew issuer cert
        /// </summary>
        private static async Task RenewIssuerCertAsync(IVaultServiceApi service,
            CliOptions options) {
            var result = await service.RenewIssuerCertificateAsync(
                options.GetValue<string>("-i", "--id"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Update group
        /// </summary>
        private static async Task UpdateGroupAsync(IVaultServiceApi service,
            CliOptions options) {
            await service.UpdateGroupAsync(options.GetValue<string>("-i", "--id"),
                new TrustGroupUpdateRequestApiModel {
                    IssuedKeySize = options.GetValueOrDefault<ushort>("-s", "--keysize", null),
                    IssuedLifetime = options.GetValueOrDefault<TimeSpan>("-l", "--lifetime", null),
                    IssuedSignatureAlgorithm = options.GetValueOrDefault<SignatureAlgorithm>(
                        "-a", "--algorithm", null),
                    Name = options.GetValueOrDefault<string>("-n", "--name", null)
                });
        }

        /// <summary>
        /// List supervisor registrations
        /// </summary>
        private static async Task ListSupervisorsAsync(IRegistryServiceApi service,
            CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await service.ListAllSupervisorsAsync(
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListSupervisorsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query supervisor registrations
        /// </summary>
        private static async Task QuerySupervisorsAsync(IRegistryServiceApi service,
            CliOptions options) {
            var query = new SupervisorQueryApiModel {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                Discovery = options.GetValueOrDefault<DiscoveryMode>("-d", "--discovery", null),
                SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await service.QueryAllSupervisorsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.QuerySupervisorsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get supervisor
        /// </summary>
        private static async Task GetSupervisorAsync(IRegistryServiceApi service,
            CliOptions options) {
            var result = await service.GetSupervisorAsync(
                options.GetValue<string>("-i", "--id"),
                options.IsProvidedOrNull("-S", "--server"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Get supervisor status
        /// </summary>
        private static async Task GetSupervisorStatusAsync(IRegistryServiceApi service,
            CliOptions options) {
            var result = await service.GetSupervisorStatusAsync(
                options.GetValue<string>("-i", "--id"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Reset supervisor
        /// </summary>
        private static async Task ResetSupervisorAsync(IRegistryServiceApi service,
            CliOptions options) {
            await service.ResetSupervisorAsync(
                options.GetValue<string>("-i", "--id"));
        }

        /// <summary>
        /// Update supervisor
        /// </summary>
        private static async Task UpdateSupervisorAsync(IRegistryServiceApi service,
            CliOptions options) {
            var config = new DiscoveryConfigApiModel();

            if (options.IsSet("-a", "--activate")) {
                config.ActivationFilter = new EndpointActivationFilterApiModel {
                    SecurityMode = SecurityMode.None
                };
            }

            var addressRange = options.GetValueOrDefault<string>("-r", "--address-ranges", null);
            if (addressRange != null) {
                if (addressRange == "true") {
                    config.AddressRangesToScan = "";
                }
                else {
                    config.AddressRangesToScan = addressRange;
                }
            }

            var portRange = options.GetValueOrDefault<string>("-p", "--port-ranges", null);
            if (portRange != null) {
                if (portRange == "true") {
                    config.PortRangesToScan = "";
                }
                else {
                    config.PortRangesToScan = portRange;
                }
            }

            var netProbes = options.GetValueOrDefault<int>("-R", "--address-probes", null);
            if (netProbes != null && netProbes != 0) {
                config.MaxNetworkProbes = netProbes;
            }

            var portProbes = options.GetValueOrDefault<int>("-P", "--port-probes", null);
            if (portProbes != null && portProbes != 0) {
                config.MaxPortProbes = portProbes;
            }

            await service.UpdateSupervisorAsync(options.GetValue<string>("-i", "--id"),
                new SupervisorUpdateApiModel {
                    SiteId = options.GetValueOrDefault<string>("-s", "--siteId", null),
                    LogLevel = options.GetValueOrDefault<SupervisorLogLevel>("-l", "--log-level", null),
                    Discovery = options.GetValueOrDefault<DiscoveryMode>("-d", "--discovery", null),
                    DiscoveryConfig = config,
                });
        }

        /// <summary>
        /// Registers application
        /// </summary>
        private static async Task RegisterApplicationAsync(IRegistryServiceApi service,
            CliOptions options) {
            var result = await service.RegisterAsync(
                new ApplicationRegistrationRequestApiModel {
                    ApplicationUri = options.GetValue<string>("-u", "--url"),
                    ApplicationName = options.GetValueOrDefault<string>("-n", "--name", null),
                    GatewayServerUri = options.GetValueOrDefault<string>("-g", "--gwuri", null),
                    ApplicationType = options.GetValueOrDefault<ApplicationType>("-t", "--type", null),
                    ProductUri = options.GetValueOrDefault<string>("-p", "--product", null),
                    DiscoveryUrls = new HashSet<string> {
                        options.GetValue<string>("-d", "--discoveryUrl")
                    }
                });
            PrintResult(options, result);
        }

        /// <summary>
        /// Registers server
        /// </summary>
        private static async Task RegisterServerAsync(IRegistryServiceApi service,
            CliOptions options) {
            var activate = options.IsSet("-a", "--activate");
            await service.RegisterAsync(
                new ServerRegistrationRequestApiModel {
                    RegistrationId = Guid.NewGuid().ToString(),
                    DiscoveryUrl = options.GetValue<string>("-u", "--url"),
                    ActivationFilter = !activate ? null : new EndpointActivationFilterApiModel {
                        SecurityMode = SecurityMode.None
                    }
                });
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        private static async Task DiscoverServerAsync(IRegistryServiceApi service,
            CliOptions options) {
            var activate = options.IsSet("-a", "--activate");
            await service.DiscoverAsync(
                new DiscoveryRequestApiModel {
                    Id = Guid.NewGuid().ToString(),
                    Discovery = options.GetValueOrDefault("-d", "--discovery", DiscoveryMode.Fast),
                    Configuration = new DiscoveryConfigApiModel {
                        ActivationFilter = !activate ? null : new EndpointActivationFilterApiModel {
                            SecurityMode = SecurityMode.None
                        }
                    }
                });
        }

        /// <summary>
        /// Update application
        /// </summary>
        private static async Task UpdateApplicationAsync(IRegistryServiceApi service,
            CliOptions options) {
            await service.UpdateApplicationAsync(options.GetValue<string>("-i", "--id"),
                new ApplicationRegistrationUpdateApiModel {
                    ApplicationName = options.GetValueOrDefault<string>("-n", "--name", null),
                    GatewayServerUri = options.GetValueOrDefault<string>("-g", "--gwuri", null),
                    ProductUri = options.GetValueOrDefault<string>("-p", "--product", null),
                    DiscoveryProfileUri = options.GetValueOrDefault<string>("-d", "--dpuri", null)
                    // ...
                });
        }

        /// <summary>
        /// Disable application
        /// </summary>
        private static async Task DisableApplicationAsync(IRegistryServiceApi service,
            CliOptions options) {
            await service.DisableApplicationAsync(options.GetValue<string>("-i", "--id"));
        }

        /// <summary>
        /// Enable application
        /// </summary>
        private static async Task EnableApplicationAsync(IRegistryServiceApi service,
            CliOptions options) {
            await service.EnableApplicationAsync(options.GetValue<string>("-i", "--id"));
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        private static async Task UnregisterApplicationAsync(IRegistryServiceApi service,
            CliOptions options) {

            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (id != null) {
                await service.UnregisterApplicationAsync(id);
                return;
            }

            var query = new ApplicationRegistrationQueryApiModel {
                ApplicationUri = options.GetValueOrDefault<string>("-u", "--uri", null),
                ApplicationType = options.GetValueOrDefault<ApplicationType>("-t", "--type", null),
                ApplicationName = options.GetValueOrDefault<string>("-n", "--name", null),
                ProductUri = options.GetValueOrDefault<string>("-p", "--product", null),
                GatewayServerUri = options.GetValueOrDefault<string>("-g", "--gwuri", null),
                DiscoveryProfileUri = options.GetValueOrDefault<string>("-d", "--dpuri", null),
                Locale = options.GetValueOrDefault<string>("-l", "--locale", null)
            };

            // Unregister all applications
            var result = await service.QueryAllApplicationsAsync(query);
            foreach (var item in result) {
                try {
                    await service.UnregisterApplicationAsync(item.ApplicationId);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to unregister {item.ApplicationId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Purge disabled applications not seen since specified amount of time.
        /// </summary>
        private static Task PurgeDisabledApplicationsAsync(IRegistryServiceApi service,
            CliOptions options) {
            return service.PurgeDisabledApplicationsAsync(
                options.GetValue<TimeSpan>("-f", "--for"));
        }

        /// <summary>
        /// List applications
        /// </summary>
        private static async Task ListApplicationsAsync(IRegistryServiceApi service,
            CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await service.ListAllApplicationsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListApplicationsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List sites
        /// </summary>
        private static async Task ListSitesAsync(IRegistryServiceApi service,
            CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await service.ListAllSitesAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListSitesAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query applications
        /// </summary>
        private static async Task QueryApplicationsAsync(IRegistryServiceApi service,
            CliOptions options) {
            var query = new ApplicationRegistrationQueryApiModel {
                ApplicationUri = options.GetValueOrDefault<string>("-u", "--uri", null),
                ProductUri = options.GetValueOrDefault<string>("-p", "--product", null),
                GatewayServerUri = options.GetValueOrDefault<string>("-g", "--gwuri", null),
                DiscoveryProfileUri = options.GetValueOrDefault<string>("-d", "--dpuri", null),
                ApplicationType = options.GetValueOrDefault<ApplicationType>("-t", "--type", null),
                ApplicationName = options.GetValueOrDefault<string>("-n", "--name", null),
                Locale = options.GetValueOrDefault<string>("-l", "--locale", null),
                IncludeNotSeenSince = options.IsProvidedOrNull("-d", "--deleted")
            };
            if (options.IsSet("-A", "--all")) {
                var result = await service.QueryAllApplicationsAsync(query);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.QueryApplicationsAsync(query,
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get application
        /// </summary>
        private static async Task GetApplicationAsync(IRegistryServiceApi service,
            CliOptions options) {
            var result = await service.GetApplicationAsync(
                options.GetValue<string>("-i", "--id"));
            PrintResult(options, result);
        }

        /// <summary>
        /// List endpoint registrations
        /// </summary>
        private static async Task ListEndpointsAsync(IRegistryServiceApi service,
            CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await service.ListAllEndpointsAsync(
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListEndpointsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query endpoints
        /// </summary>
        private static async Task QueryEndpointsAsync(IRegistryServiceApi service,
            CliOptions options) {
            var query = new EndpointRegistrationQueryApiModel {
                Url = options.GetValueOrDefault<string>("-u", "--uri", null),
                SecurityMode = options.GetValueOrDefault<SecurityMode>("-m", "--mode", null),
                SecurityPolicy = options.GetValueOrDefault<string>("-l", "--policy", null),
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                Activated = options.IsProvidedOrNull("-a", "--activated"),
                EndpointState = options.GetValueOrDefault<EndpointConnectivityState>(
                    "-s", "--state", null),
                IncludeNotSeenSince = options.IsProvidedOrNull("-d", "--deleted")
            };
            if (options.IsSet("-A", "--all")) {
                var result = await service.QueryAllEndpointsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.QueryEndpointsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Activate endpoints
        /// </summary>
        private static async Task ActivateEndpointsAsync(IRegistryServiceApi service,
            CliOptions options) {

            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (id != null) {
                await service.ActivateEndpointAsync(id);
                return;
            }

            // Activate all sign and encrypt endpoints
            var result = await service.QueryAllEndpointsAsync(new EndpointRegistrationQueryApiModel {
                SecurityMode = options.GetValueOrDefault<SecurityMode>("-m", "mode", null),
                Activated = false
            });
            foreach (var item in result) {
                try {
                    await service.ActivateEndpointAsync(item.Registration.Id);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to activate {item.Registration.Id}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Update endpoint
        /// </summary>
        private static async Task UpdateEndpointAsync(IRegistryServiceApi service,
            CliOptions options) {

            var credential = options.GetValue<Registry.Models.CredentialType>(
                "-c", "--credential");
            var user = new Registry.Models.CredentialApiModel {
                Type = credential
            };
            switch (credential) {
                case Registry.Models.CredentialType.None:
                    user.Value = null;
                    break;
                case Registry.Models.CredentialType.UserName:
                    Console.WriteLine("User: ");
                    var name = Console.ReadLine();
                    Console.WriteLine("Password: ");
                    user.Value = JObject.FromObject(new {
                        user = name,
                        password = ConsoleEx.ReadPassword().ToString()
                    });
                    break;
                case Registry.Models.CredentialType.JwtToken:
                    // TODO:
                    throw new NotSupportedException();
                case Registry.Models.CredentialType.X509Certificate:
                    // TODO:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentException(nameof(credential));
            }
            await service.UpdateEndpointAsync(options.GetValue<string>("-i", "--id"),
                new EndpointRegistrationUpdateApiModel {
                    User = user
                });
        }

        /// <summary>
        /// Deactivate endpoints
        /// </summary>
        private static async Task DeactivateEndpointsAsync(IRegistryServiceApi service,
            CliOptions options) {

            var id = options.GetValueOrDefault<string>("-i", "--id", null);
            if (id != null) {
                await service.DeactivateEndpointAsync(id);
                return;
            }

            // Activate all sign and encrypt endpoints
            var result = await service.QueryAllEndpointsAsync(new EndpointRegistrationQueryApiModel {
                SecurityMode = options.GetValueOrDefault<SecurityMode>("-m", "mode", null),
                Activated = true
            });
            foreach (var item in result) {
                try {
                    await service.DeactivateEndpointAsync(item.Registration.Id);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to deactivate {item.Registration.Id}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get endpoint
        /// </summary>
        private static async Task GetEndpointAsync(IRegistryServiceApi service,
            CliOptions options) {
            var result = await service.GetEndpointAsync(
                options.GetValue<string>("-i", "--id"),
                options.IsProvidedOrNull("-S", "--server"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Query requests
        /// </summary>
        private static async Task QueryRequestsAsync(IVaultServiceApi service,
            CliOptions options) {
            var query = new CertificateRequestQueryRequestApiModel {
                EntityId = options.GetValueOrDefault<string>("-e", "--entityid", null),
                State = options.GetValueOrDefault<CertificateRequestState>("-s", "--state", null)
            };
            if (options.IsSet("-A", "--all")) {
                var result = await service.QueryAllRequestsAsync(query);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.QueryRequestsAsync(query,
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List requests
        /// </summary>
        private static async Task ListRequestsAsync(IVaultServiceApi service,
            CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await service.ListAllRequestsAsync();
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListRequestsAsync(
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get request
        /// </summary>
        private static async Task GetRequestAsync(IVaultServiceApi service,
            CliOptions options) {
            var result = await service.GetRequestAsync(
                options.GetValue<string>("-i", "--id"));
            PrintResult(options, result);
        }

        /// <summary>
        /// Delete request
        /// </summary>
        private static async Task DeleteRequestAsync(IVaultServiceApi service,
            CliOptions options) {
            await service.DeleteRequestAsync(
                options.GetValue<string>("-i", "--id"));
        }

        /// <summary>
        /// Accept request
        /// </summary>
        private static async Task AcceptRequestAsync(IVaultServiceApi service,
            CliOptions options) {
            await service.AcceptRequestAsync(
                options.GetValue<string>("-i", "--id"));
        }

        /// <summary>
        /// Reject request
        /// </summary>
        private static async Task RejectRequestAsync(IVaultServiceApi service,
            CliOptions options) {
            await service.RejectRequestAsync(
                options.GetValue<string>("-i", "--id"));
        }

        /// <summary>
        /// Approve request
        /// </summary>
        private static async Task ApproveRequestAsync(IVaultServiceApi service,
            CliOptions options) {
            await service.ApproveRequestAsync(
                options.GetValue<string>("-i", "--id"));
        }

        /// <summary>
        /// Start and finish keypair request
        /// </summary>
        private static async Task KeyPairRequestAsync(IVaultServiceApi service,
            CliOptions options) {
            if (options.IsProvidedOrNull("-f", "--finish") == true) {
                var result = await service.FinishKeyPairRequestAsync(
                    options.GetValue<string>("-i", "--id"));
                PrintResult(options, result);
            }
            else {
                var result = await service.StartNewKeyPairRequestAsync(new StartNewKeyPairRequestApiModel {
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
        private static async Task SigningRequestAsync(IVaultServiceApi service,
            CliOptions options) {
            if (options.IsProvidedOrNull("-f", "--finish") == true) {
                var result = await service.FinishSigningRequestAsync(
                    options.GetValue<string>("-i", "--id"));
                PrintResult(options, result);
            }
            else {
                var result = await service.StartSigningRequestAsync(new StartSigningRequestApiModel {
                    CertificateRequest = options.GetValue<byte[]>("-c", "--csr"),
                    EntityId = options.GetValue<string>("-e", "--entityId"),
                    GroupId = options.GetValue<string>("-g", "--groupId")
                });
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Remove trust relationship
        /// </summary>
        private static async Task RemoveTrustRelationshipAsync(IVaultServiceApi service,
            CliOptions options) {
            await service.RemoveTrustRelationshipAsync(
                options.GetValue<string>("-i", "--id"),
                options.GetValue<string>("-t", "--trustedId"));
        }

        /// <summary>
        /// Get trusted certificates
        /// </summary>
        private static async Task GetTrustedCertificatesAsync(IVaultServiceApi service,
            CliOptions options) {
            if (options.IsSet("-A", "--all")) {
                var result = await service.ListAllTrustedCertificatesAsync(
                    options.GetValue<string>("-i", "--id"));
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else {
                var result = await service.ListTrustedCertificatesAsync(
                    options.GetValue<string>("-i", "--id"),
                    options.GetValueOrDefault<string>("-C", "--continuation", null),
                    options.GetValueOrDefault<int>("-P", "--page-size", null));
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Add trust relationship
        /// </summary>
        private static async Task AddTrustRelationshipAsync(IVaultServiceApi service,
            CliOptions options) {
            await service.AddTrustRelationshipAsync(
                options.GetValue<string>("-i", "--id"),
                options.GetValue<string>("-t", "--trustedId"));
        }

        /// <summary>
        /// Get status
        /// </summary>
        private static async Task GetStatusAsync(IComponentContext context,
            ITwinServiceApi twin, IRegistryServiceApi registry, IVaultServiceApi vault,
            CliOptions options) {
            try {
                var cfg = context.Resolve<ITwinConfig>();
                Console.WriteLine($"Connecting to {cfg.OpcUaTwinServiceUrl}...");
                var result = await twin.GetServiceStatusAsync();
                PrintResult(options, result);
            }
            catch (Exception ex) {
                PrintResult(options, ex);
            }
            try {
                var cfg = context.Resolve<IRegistryConfig>();
                Console.WriteLine($"Connecting to {cfg.OpcUaRegistryServiceUrl}...");
                var result = await registry.GetServiceStatusAsync();
                PrintResult(options, result);
            }
            catch (Exception ex) {
                PrintResult(options, ex);
            }
            try {
                var cfg = context.Resolve<IVaultConfig>();
                Console.WriteLine($"Connecting to {cfg.OpcUaVaultServiceUrl}...");
                var result = await vault.GetServiceStatusAsync();
                PrintResult(options, result);
            }
            catch (Exception ex) {
                PrintResult(options, ex);
            }
        }

        /// <summary>
        /// Print result
        /// </summary>
        private static void PrintResult<T>(CliOptions options, T status) {
            Console.WriteLine("==================");
            Console.WriteLine(JsonConvert.SerializeObject(status,
                options.GetValueOrDefault("-F", "--format", Formatting.Indented)));
            Console.WriteLine("==================");
        }


        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintHelp() {
            Console.WriteLine(
                @"
aziiotcli - Allows to script Industrial IoT Services api.
usage:      aziiotcli command [options]

Commands and Options

     console     Run in interactive mode. Enter commands after the >
     exit        Exit interactive mode and thus the cli.
     groups      Manage trust groups
     apps        Manage applications
     endpoints   Manage endpoints
     supervisors Manage supervisors
     trust       Manage trust between above entities
     nodes       Call nodes services on endpoint
     requests    Manage certificate requests
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
        -u, --url       Url of the discovery endpoint (mandatory)
        -a, --activate  Activate all endpoints during onboarding.

     discover    Discover applications and endpoints through config.
        with ...
        -d, --discovery Set discovery mode to use
        -a, --activate  Activate all endpoints during onboarding.

     register    Register Application
        with ...
        -u, --url       Uri of the application (mandatory)
        -n  --name      Application name of the application
        -t, --type      Application type (default to Server)
        -p, --product   Product uri of the application
        -d, --discovery Url of the discovery endpoint
        -d, --dpuri     Discovery profile uri
        -g, --gwuri     Gateway uri
        -F, --format    Json format for result

     query       Find applications
        with ...
        -P, --page-size Size of page
        -A, --all       Return all application infos (unpaged)
        -u, --uri       Application uri of the application.
        -i, --dpuri     Discovery profile uri
        -g, --gwuri     Gateway uri
        -u, --uri       Application uri of the application.
        -n  --name      Application name of the application
        -t, --type      Application type (default to all)
        -s, --state     Application state (default to all)
        -p, --product   Product uri of the application
        -d, --deleted   Include soft deleted applications.
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
        -d, --dpuri     Discovery profile uri
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
        -i, --dpuri     Discovery profile uri
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
        private static void PrintEndpointsHelp() {
            Console.WriteLine(
                @"
Manage endpoints in registry.

Commands and Options

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
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
        -F, --format    Json format for result

     activate    Activate endpoints
        with ...
        -i, --id        Id of endpoint or ...
        -m, --mode      Security mode (default:SignAndEncrypt)

     get         Get endpoint
        with ...
        -i, --id        Id of endpoint to retrieve (mandatory)
        -S, --server    Return only server state (default:false)
        -F, --format    Json format for result

     update      Update endpoint
        with ...
        -i, --id        Id of endpoint
        -c, --credential
                        Credential type

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
        private static void PrintNodesHelp() {
            Console.WriteLine(
                @"
Access address space through configured server endpoint.

Commands and Options

     browse      Browse nodes on endpoint
        with ...
        -i, --id        Id of endpoint to browse (mandatory)
        -n, --nodeid    Node to browse
        -x, --maxrefs   Max number of references
        -x, --direction Browse direction (Forward, Backward, Both)
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
        private static void PrintSupervisorsHelp() {
            Console.WriteLine(
                @"
Manage and configure Twin modules (endpoint supervisors)

Commands and Options

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
        -d, --discovery Discovery state.
        -s, --siteId    Site of the supervisors.
        -P, --page-size Size of page
        -A, --all       Return all endpoints (unpaged)
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
        -d, --discovery Set supervisor discovery mode
        -l, --log-level Set supervisor module logging level
        -a, --activate  Activate all endpoints during onboarding.
        -p, --port-ranges
                        Port ranges to scan.
        -r, --address-ranges
                        Address range to scan.
        -P, --port-probes
                        Max port probes to use.
        -R, --address-probes
                        Max networking probes to use.

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
        private static void PrintGroupsHelp() {
            Console.WriteLine(
                @"
Manage entity trust groups

Commands and Options

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
        private static void PrintRequestsHelp() {
            Console.WriteLine(
                @"
Submit and manage Certificate requests

Commands and Options

     sign        Submit certificate signing request
        with ...
        -g, --groupId   Group to submit request to (mandatory)
        -e, --entityId  Entity id to create key for (mandatory)
        or ...
        -f, --finish    Retrieve finished signing result, then
        -i, --id        Id of request to finish (mandatory)

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
        private static void PrintTrustHelp() {
            Console.WriteLine(
                @"
Manage trust between entities

Commands and Options

     create      Add trust relationship
        with ...
        -i, --id        Id of entity (mandatory)
        -t, --trusted   Id of trusted entity (mandatory)

     get         Get certificates the entity trusts.
        with ...
        -i, --id        Id of entity (mandatory)
        -C, --continuation
                        Continuation from previous result.
        -P, --page-size Size of page
        -A, --all       Return all items (unpaged)
        -F, --format    Json format for result

     delete      Remove trust relationship
        with ...
        -i, --id        Id of entity (mandatory)
        -t, --trusted   Id of entity not to trust (mandatory)

     help, -h, -? --help
                 Prints out this help.
"
                );
        }
    }
}
