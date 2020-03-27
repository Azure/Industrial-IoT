// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Cli {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Azure.IIoT.Api.Jobs.Clients;
    using Microsoft.Azure.IIoT.Api.Jobs;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Clients;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.SignalR;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;

    /// <summary>
    /// Api command line interface
    /// </summary>
    public class Program : IDisposable {

        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        public static IContainer ConfigureContainer(
            IConfiguration configuration) {
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

            using (var scope = new Program(config)) {
                scope.RunAsync(args).Wait();
            }
        }


        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        public Program(IConfiguration configuration) {
            var container = ConfigureContainer(configuration);
            _scope = container.BeginLifetimeScope();
            _twin = _scope.Resolve<ITwinServiceApi>();
            _registry = _scope.Resolve<IRegistryServiceApi>();
            _publisher = _scope.Resolve<IPublisherServiceApi>();
            _vault = _scope.Resolve<IVaultServiceApi>();
            _jobs = _scope.Resolve<IJobsServiceApi>();
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
  ____                            _           _____         _
 / ___|  ___ ___ _ __   __ _ _ __(_) ___     |_   _|__  ___| |_ ___
 \___ \ / __/ _ \ '_ \ / _` | '__| |/ _ \ _____| |/ _ \/ __| __/ __|
  ___) | (_|  __/ | | | (_| | |  | | (_) |_____| |  __/\__ \ |_\__ \
 |____/ \___\___|_| |_|\__,_|_|  |_|\___/      |_|\___||___/\__|___/
");
                            interactive = true;
                            break;
                        case "activation":
                            options = new CliOptions(args, 1);
                            await TestActivationAsync(options);
                            break;
                        case "browse":
                            options = new CliOptions(args, 1);
                            await TestBrowseAsync(options);
                            break;
                        case "publish":
                            options = new CliOptions(args, 1);
                            await TestPublishAsync(options);
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
        /// Test activation and deactivation
        /// </summary>
        private async Task TestActivationAsync(CliOptions options) {
            IEnumerable<EndpointRegistrationApiModel> endpoints;
            if (!options.IsSet("-a", "--all")) {
                if (options.IsSet("-e", "--endpoint")) {
                    var id = await SelectEndpointAsync();
                    var ep = await _registry.GetEndpointAsync(id);
                    endpoints = ep.Registration.YieldReturn();
                }
                else {
                    var id = options.GetValueOrDefault<string>("-i", "--id", null);
                    if (id == null) {
                        id = await SelectApplicationAsync();
                        if (id == null) {
                            throw new ArgumentException("Needs an id");
                        }
                    }

                    var app = await _registry.GetApplicationAsync(id);
                    if (app.Endpoints.Count == 0) {
                        return;
                    }
                    endpoints = app.Endpoints;
                }
            }
            else {
                var infos = await _registry.ListAllEndpointsAsync();
                endpoints = infos.Select(e => e.Registration);
            }
            await Task.WhenAll(endpoints.Select(e => TestActivationAsync(e, options)));
            Console.WriteLine("Success!");
        }

        /// <summary>
        /// Test browsing
        /// </summary>
        private async Task TestBrowseAsync(CliOptions options) {
            IEnumerable<EndpointRegistrationApiModel> endpoints;
            if (!options.IsSet("-a", "--all")) {
                if (options.IsSet("-e", "--endpoint")) {
                    var id = await SelectEndpointAsync();
                    var ep = await _registry.GetEndpointAsync(id);
                    endpoints = ep.Registration.YieldReturn();
                }
                else {
                    var id = options.GetValueOrDefault<string>("-i", "--id", null);
                    if (id == null) {
                        id = await SelectApplicationAsync();
                        if (id == null) {
                            throw new ArgumentException("Needs an id");
                        }
                    }

                    var app = await _registry.GetApplicationAsync(id);
                    if (app.Endpoints.Count == 0) {
                        return;
                    }
                    endpoints = app.Endpoints;
                }
            }
            else {
                var infos = await _registry.ListAllEndpointsAsync();
                endpoints = infos.Select(e => e.Registration);
            }
            await Task.WhenAll(endpoints.Select(e => TestBrowseAsync(e, options)));
            Console.WriteLine("Success!");
        }

        /// <summary>
        /// Test publishing
        /// </summary>
        private async Task TestPublishAsync(CliOptions options) {
            IEnumerable<EndpointRegistrationApiModel> endpoints;
            if (!options.IsSet("-a", "--all")) {
                if (options.IsSet("-e", "--endpoint")) {
                    var id = await SelectEndpointAsync();
                    var ep = await _registry.GetEndpointAsync(id);
                    endpoints = ep.Registration.YieldReturn();
                }
                else {
                    var id = options.GetValueOrDefault<string>("-i", "--id", null);
                    if (id == null) {
                        id = await SelectApplicationAsync();
                        if (id == null) {
                            throw new ArgumentException("Needs an id");
                        }
                    }

                    var app = await _registry.GetApplicationAsync(id);
                    if (app.Endpoints.Count == 0) {
                        return;
                    }
                    endpoints = app.Endpoints;
                }
            }
            else {
                var infos = await _registry.ListAllEndpointsAsync();
                endpoints = infos.Select(e => e.Registration);
            }
            await Task.WhenAll(endpoints.Select(e => TestPublishAsync(e, options)));
            Console.WriteLine("Success!");
        }

        /// <summary>
        /// Test activation of endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task TestActivationAsync(EndpointRegistrationApiModel endpoint,
            CliOptions options) {
            EndpointInfoApiModel ep;
            var repeats = options.GetValueOrDefault("-r", "--repeat", 10); // 10 times
            for (var i = 0; i < repeats; i++) {
                await _registry.ActivateEndpointAsync(endpoint.Id);
                var sw = Stopwatch.StartNew();
                while (true) {
                    ep = await _registry.GetEndpointAsync(endpoint.Id);
                    if (ep.ActivationState == EndpointActivationState.ActivatedAndConnected) {
                        break;
                    }
                    if (sw.ElapsedMilliseconds > 60000) {
                        throw new Exception($"{endpoint.Id} failed to activate!");
                    }
                }

                Console.WriteLine($"{endpoint.Id} activated.");

                while (options.IsSet("-b", "--browse") || options.IsSet("-w", "--waitstate")) {
                    if (ep.EndpointState != EndpointConnectivityState.Connecting) {
                        Console.WriteLine($"{endpoint.Id} now in {ep.EndpointState} state.");
                        break;
                    }
                    if (sw.ElapsedMilliseconds > 60000) {
                        throw new Exception($"{endpoint.Id} failed to get endpoint state!");
                    }
                    ep = await _registry.GetEndpointAsync(endpoint.Id);
                }
                if (ep.EndpointState == EndpointConnectivityState.Ready &&
                    options.IsSet("-b", "--browse")) {

                    var silent = !options.IsSet("-V", "--verbose");
                    var recursive = options.IsSet("-R", "--recursive");
                    var readDuringBrowse = options.IsProvidedOrNull("-v", "--readvalue");
                    var node = options.GetValueOrDefault<string>("-n", "--nodeid", null);
                    var targetNodesOnly = options.IsProvidedOrNull("-t", "--targets");
                    var maxReferencesToReturn = options.GetValueOrDefault<uint>("-x", "--maxrefs", null);
                    var direction = options.GetValueOrDefault<BrowseDirection>("-d", "--direction", null);

                    await BrowseAsync(0, endpoint.Id, silent, recursive, readDuringBrowse, node,
                        targetNodesOnly, maxReferencesToReturn, direction, options);
                }
                else {
                    await Task.Delay(_rand.Next(
                        options.GetValueOrDefault("-l", "--min-wait", 1000), // 1 seconds
                        options.GetValueOrDefault("-h", "--max-wait", 20000)));  // 20 seconds
                }

                await _registry.DeactivateEndpointAsync(endpoint.Id);
                sw.Restart();
                while (true) {
                    ep = await _registry.GetEndpointAsync(endpoint.Id);
                    if (ep.ActivationState == EndpointActivationState.Deactivated) {
                        break;
                    }
                    if (sw.ElapsedMilliseconds > 60000) {
                        throw new Exception($"{endpoint.Id} failed to deactivate!");
                    }
                }
                Console.WriteLine($"{endpoint.Id} deactivated.");
            }
        }

        /// <summary>
        /// Test publish and unpublish
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task TestPublishAsync(EndpointRegistrationApiModel endpoint,
            CliOptions options) {
            EndpointInfoApiModel ep;

            Console.WriteLine($"Activating {endpoint.Id} for publishing ...");
            await _registry.ActivateEndpointAsync(endpoint.Id);
            var sw = Stopwatch.StartNew();
            while (true) {
                ep = await _registry.GetEndpointAsync(endpoint.Id);
                if (ep.ActivationState == EndpointActivationState.ActivatedAndConnected &&
                    ep.EndpointState == EndpointConnectivityState.Ready) {
                    break;
                }
                if (sw.ElapsedMilliseconds > 60000) {
                    Console.WriteLine($"{endpoint.Id} could not be activated - skip!");
                    return;
                }
            }
            Console.WriteLine($"{endpoint.Id} activated - get all variables.");

            var nodes = new List<string>();
            await BrowseAsync(0, endpoint.Id, true, true, false, null,
                true, 1000, null, options, nodes);

            Console.WriteLine($"{endpoint.Id} has {nodes.Count} variables.");
            sw.Restart();
            await _publisher.NodePublishBulkAsync(endpoint.Id, new PublishBulkRequestApiModel {
                NodesToAdd = nodes.Select(n => new PublishedItemApiModel {
                    NodeId = n
                }).ToList()
            });
            Console.WriteLine($"{endpoint.Id} Publishing {nodes.Count} variables took {sw.Elapsed}.");

            sw.Restart();
            await _publisher.NodePublishBulkAsync(endpoint.Id, new PublishBulkRequestApiModel {
                NodesToRemove = nodes.ToList()
            });
            Console.WriteLine($"{endpoint.Id} Unpublishing {nodes.Count} variables took {sw.Elapsed}.");

            await _registry.DeactivateEndpointAsync(endpoint.Id);
            sw.Restart();
            while (true) {
                ep = await _registry.GetEndpointAsync(endpoint.Id);
                if (ep.ActivationState == EndpointActivationState.Deactivated) {
                    break;
                }
                if (sw.ElapsedMilliseconds > 60000) {
                    throw new Exception($"{endpoint.Id} failed to deactivate!");
                }
            }
            Console.WriteLine($"{endpoint.Id} deactivated.");
        }

        /// <summary>
        /// Test activation of endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task TestBrowseAsync(EndpointRegistrationApiModel endpoint,
            CliOptions options) {
            EndpointInfoApiModel ep;

            Console.WriteLine($"Activating {endpoint.Id} for recursive browse...");
            await _registry.ActivateEndpointAsync(endpoint.Id);
            var sw = Stopwatch.StartNew();
            while (true) {
                ep = await _registry.GetEndpointAsync(endpoint.Id);
                if (ep.ActivationState == EndpointActivationState.ActivatedAndConnected &&
                    ep.EndpointState == EndpointConnectivityState.Ready) {
                    break;
                }
                if (sw.ElapsedMilliseconds > 60000) {
                    Console.WriteLine($"{endpoint.Id} could not be activated - skip!");
                    return;
                }
            }
            Console.WriteLine($"{endpoint.Id} activated - recursive browse.");

            var silent = !options.IsSet("-V", "--verbose");
            var readDuringBrowse = options.IsProvidedOrNull("-v", "--readvalue");
            var targetNodesOnly = options.IsProvidedOrNull("-t", "--targets");
            var maxReferencesToReturn = options.GetValueOrDefault<uint>("-x", "--maxrefs", null);

            var workers = options.GetValueOrDefault("-w", "--workers", 1);  // 1 worker per endpoint
            await Task.WhenAll(Enumerable.Range(0, workers).Select(i =>
                BrowseAsync(i, endpoint.Id, silent, true, readDuringBrowse, null,
                    targetNodesOnly, maxReferencesToReturn, null, options)));

            await _registry.DeactivateEndpointAsync(endpoint.Id);
            sw.Restart();
            while (true) {
                ep = await _registry.GetEndpointAsync(endpoint.Id);
                if (ep.ActivationState == EndpointActivationState.Deactivated) {
                    break;
                }
                if (sw.ElapsedMilliseconds > 60000) {
                    throw new Exception($"{endpoint.Id} failed to deactivate!");
                }
            }
            Console.WriteLine($"{endpoint.Id} deactivated.");
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        private async Task BrowseAsync(int index, string id, bool silent, bool recursive,
            bool? readDuringBrowse, string node, bool? targetNodesOnly,
            uint? maxReferencesToReturn, BrowseDirection? direction, CliOptions options,
            List<string> variables = null) {

            var request = new BrowseRequestApiModel {
                TargetNodesOnly = targetNodesOnly,
                ReadVariableValues = readDuringBrowse,
                MaxReferencesToReturn = maxReferencesToReturn,
                Direction = direction
            };
            var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { node };
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nodesRead = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = 0;
            var sw = Stopwatch.StartNew();
            while (nodes.Count > 0) {
                request.NodeId = nodes.First();
                nodes.Remove(request.NodeId);
                try {
                    var result = await NodeBrowseAsync(_twin, id, request);
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
                                if (variables != null &&
                                    r.Target.NodeClass == NodeClass.Variable) {
                                    variables.Add(r.Target.NodeId);
                                }
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
                                Console.WriteLine($"Browse {index} - reading {r.Target.NodeId} resulted in {ex}");
                                errors++;
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"Browse {index} {request.NodeId} resulted in {e}");
                    errors++;
                }
            }
            Console.WriteLine($"Browse {index} took {sw.Elapsed}. Visited " +
                $"{visited.Count} nodes and read {nodesRead.Count} of them with {errors} errors.");
        }

        /// <summary>
        /// Browse all references
        /// </summary>
        private static async Task<BrowseResponseApiModel> NodeBrowseAsync(
            ITwinServiceApi service, string endpoint, BrowseRequestApiModel request) {
            while (true) {
                var result = await service.NodeBrowseFirstAsync(endpoint, request);
                while (result.ContinuationToken != null) {
                    try {
                        var next = await service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestApiModel {
                                ContinuationToken = result.ContinuationToken,
                                Header = request.Header,
                                ReadVariableValues = request.ReadVariableValues,
                                TargetNodesOnly = request.TargetNodesOnly
                            });
                        result.References.AddRange(next.References);
                        result.ContinuationToken = next.ContinuationToken;
                    }
                    catch (Exception) {
                        await Try.Async(() => service.NodeBrowseNextAsync(endpoint,
                            new BrowseNextRequestApiModel {
                                ContinuationToken = result.ContinuationToken,
                                Abort = true
                            }));
                        throw;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Select application registration
        /// </summary>
        private async Task<string> SelectApplicationAsync() {
            var result = await _registry.ListAllApplicationsAsync();
            var applicationId = ConsoleEx.Select(result.Select(r => r.ApplicationId));
            if (string.IsNullOrEmpty(applicationId)) {
                Console.WriteLine("Nothing selected - application selection cleared.");
            }
            else {
                Console.WriteLine($"Selected {applicationId}.");
            }
            return applicationId;
        }

        /// <summary>
        /// Select endpoint registration
        /// </summary>
        private async Task<string> SelectEndpointAsync() {
            var result = await _registry.ListAllEndpointsAsync();
            var endpointId = ConsoleEx.Select(result.Select(r => r.Registration.Id));
            if (string.IsNullOrEmpty(endpointId)) {
                Console.WriteLine("Nothing selected - application selection cleared.");
            }
            else {
                Console.WriteLine($"Selected {endpointId}.");
            }
            return endpointId;
        }

        /// <summary>
        /// Print result
        /// </summary>
        private void PrintResult<T>(CliOptions options, T status) {
            Console.WriteLine("==================");
            Console.WriteLine(JsonConvert.SerializeObject(status,
                options.GetValueOrDefault("-F", "--format", Formatting.Indented)));
            Console.WriteLine("==================");
        }

        /// <summary>
        /// Print help
        /// </summary>
        private void PrintHelp() {
            Console.WriteLine(
                @"
aziiottest  Allows to excercise integration scenarios.
usage:      aziiottest command [options]

Commands and Options

     activation  Tests activation and deactivation of endpoints.
        with ...
        -i, --id        Application id to scope endpoints.
        -e, --endpoint  Whether to select and test single endpoint
        -a, --all       Use all endpoints
        -r, --repeat    How many times to repeat.
        -w, --waitstate Wait for state changes before deactivating.
        -l, --min-wait  Minimum wait time in between act/deact.
        -h, --max-wait  Maximum wait time in between act/deact.
        -b, --browse    Browse after activation if endpoint is Ready.
            -n, --nodeid    Node to browse
            -x, --maxrefs   Max number of references
            -d, --direction Browse direction (Forward, Backward, Both)
            -R, --recursive Browse recursively and read node values
            -v, --readvalue Read node values in browse
            -t, --targets   Only return target nodes
            -V, --verbose   Print browse results to screen
            -F, --format    Json format for result

     browse      Tests recursive browsing of endpoints.
        with ...
        -i, --id        Application id to scope endpoints.
        -e, --endpoint  Whether to select and test single endpoint
        -a, --all       Use all endpoints
        -w, --workers   How many workers browsing.
        -x, --maxrefs   Max number of references
        -v, --readvalue Read node values in browse
        -t, --targets   Only return target nodes
        -V, --verbose   Print browse results to screen
        -F, --format    Json format for result

     publish     Tests publishing and unpublishing.
        with ...
        -i, --id        Application id to scope endpoints.
        -e, --endpoint  Whether to select and test single endpoint
        -a, --all       Use all endpoints
        -F, --format    Json format for result

     console
     exit        To run in console mode and exit console mode.
     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        private readonly Random _rand = new Random();
        private readonly ILifetimeScope _scope;
        private readonly ITwinServiceApi _twin;
        private readonly IJobsServiceApi _jobs;
        private readonly IPublisherServiceApi _publisher;
        private readonly IRegistryServiceApi _registry;
        private readonly IVaultServiceApi _vault;
    }
}
