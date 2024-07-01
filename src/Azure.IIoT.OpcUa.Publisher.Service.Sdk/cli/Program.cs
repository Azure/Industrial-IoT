// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Cli
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Autofac;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Api command line interface
    /// </summary>
    public sealed class Program : IDisposable
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddFromKeyVault(ConfigurationProviderPriority.Lowest, true)
                .AddCommandLine(args)
                .Build();

            using (var scope = new Program(config))
            {
                scope.RunAsync(args).Wait();
            }
        }

        /// <summary>
        /// Configure Dependency injection
        /// </summary>
        /// <param name="configuration"></param>
        public Program(IConfiguration configuration)
        {
            _client = new ServiceClient(configuration,
                configureBuilder: builder => builder
                    .RegisterType<Configuration>().AsImplementedInterfaces());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client.Dispose();
        }

#pragma warning disable CA1308 // Normalize strings to uppercase
        /// <summary>
        /// Run client
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="ArgumentException"></exception>
        public async Task RunAsync(string[] args)
        {
            var interactive = false;
            do
            {
                if (interactive)
                {
                    Console.Write("> ");
                    args = CliOptions.ParseAsCommandLine(Console.ReadLine());
                }
                try
                {
                    if (args.Length < 1)
                    {
                        throw new ArgumentException("Need a command!");
                    }

                    CliOptions options;
                    var command = args[0].ToLowerInvariant();
                    switch (command)
                    {
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
                            await GetStatusAsync().ConfigureAwait(false);
                            break;
                        case "monitor":
                            options = new CliOptions(args);
                            await MonitorAllAsync().ConfigureAwait(false);
                            break;
                        case "apps":
                            if (args.Length < 2)
                            {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command)
                            {
                                case "sites":
                                    await ListSitesAsync(options).ConfigureAwait(false);
                                    break;
                                case "register":
                                    await RegisterApplicationAsync(options).ConfigureAwait(false);
                                    break;
                                case "add":
                                    await RegisterServerAsync(options).ConfigureAwait(false);
                                    break;
                                case "discover":
                                    await DiscoverServersAsync(options).ConfigureAwait(false);
                                    break;
                                case "cancel":
                                    await CancelDiscoveryAsync(options).ConfigureAwait(false);
                                    break;
                                case "update":
                                    await UpdateApplicationAsync(options).ConfigureAwait(false);
                                    break;
                                case "disable":
                                    await DisableApplicationAsync(options).ConfigureAwait(false);
                                    break;
                                case "enable":
                                    await EnableApplicationAsync(options).ConfigureAwait(false);
                                    break;
                                case "remove":
                                case "unregister":
                                    await UnregisterApplicationAsync(options).ConfigureAwait(false);
                                    break;
                                case "purge":
                                    await PurgeDisabledApplicationsAsync(options).ConfigureAwait(false);
                                    break;
                                case "list":
                                    await ListApplicationsAsync(options).ConfigureAwait(false);
                                    break;
                                case "monitor":
                                    await MonitorApplicationsAsync().ConfigureAwait(false);
                                    break;
                                case "select":
                                    await SelectApplicationAsync(options).ConfigureAwait(false);
                                    break;
                                case "query":
                                    await QueryApplicationsAsync(options).ConfigureAwait(false);
                                    break;
                                case "get":
                                    await GetApplicationAsync(options).ConfigureAwait(false);
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
                            if (args.Length < 2)
                            {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command)
                            {
                                case "get":
                                    await GetEndpointAsync(options).ConfigureAwait(false);
                                    break;
                                case "add":
                                    await RegisterEndpointAsync(options).ConfigureAwait(false);
                                    break;
                                case "list":
                                    await ListEndpointsAsync(options).ConfigureAwait(false);
                                    break;
                                case "monitor":
                                    await MonitorEndpointsAsync().ConfigureAwait(false);
                                    break;
                                case "select":
                                    await SelectEndpointsAsync(options).ConfigureAwait(false);
                                    break;
                                case "query":
                                    await QueryEndpointsAsync(options).ConfigureAwait(false);
                                    break;
                                case "info":
                                    await GetServerCapablitiesAsync(options).ConfigureAwait(false);
                                    break;
                                case "validate":
                                    await GetEndpointCertificateAsync(options).ConfigureAwait(false);
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
                        case "discoverers":
                            if (args.Length < 2)
                            {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command)
                            {
                                case "get":
                                    await GetDiscovererAsync(options).ConfigureAwait(false);
                                    break;
                                case "update":
                                    await UpdateDiscovererAsync(options).ConfigureAwait(false);
                                    break;
                                case "monitor":
                                    await MonitorDiscoverersAsync(options).ConfigureAwait(false);
                                    break;
                                case "list":
                                    await ListDiscoverersAsync(options).ConfigureAwait(false);
                                    break;
                                case "select":
                                    await SelectDiscovererAsync(options).ConfigureAwait(false);
                                    break;
                                case "query":
                                    await QueryDiscoverersAsync(options).ConfigureAwait(false);
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
                            if (args.Length < 2)
                            {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command)
                            {
                                case "get":
                                    await GetSupervisorAsync(options).ConfigureAwait(false);
                                    break;
                                case "update":
                                    await UpdateSupervisorAsync(options).ConfigureAwait(false);
                                    break;
                                case "monitor":
                                    await MonitorSupervisorsAsync().ConfigureAwait(false);
                                    break;
                                case "list":
                                    await ListSupervisorsAsync(options).ConfigureAwait(false);
                                    break;
                                case "select":
                                    await SelectSupervisorAsync(options).ConfigureAwait(false);
                                    break;
                                case "query":
                                    await QuerySupervisorsAsync(options).ConfigureAwait(false);
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
                            if (args.Length < 2)
                            {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command)
                            {
                                case "get":
                                    await GetPublisherAsync(options).ConfigureAwait(false);
                                    break;
                                case "update":
                                    await UpdatePublisherAsync(options).ConfigureAwait(false);
                                    break;
                                case "monitor":
                                    await MonitorPublishersAsync().ConfigureAwait(false);
                                    break;
                                case "get-config":
                                    await GetConfiguredEndpointsAsync(options).ConfigureAwait(false);
                                    break;
                                case "set-config":
                                    await SetConfiguredEndpointsAsync(options).ConfigureAwait(false);
                                    break;
                                case "list":
                                    await ListPublishersAsync(options).ConfigureAwait(false);
                                    break;
                                case "select":
                                    await SelectPublisherAsync(options).ConfigureAwait(false);
                                    break;
                                case "query":
                                    await QueryPublishersAsync(options).ConfigureAwait(false);
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
                            if (args.Length < 2)
                            {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command)
                            {
                                case "get":
                                    await GetGatewayAsync(options).ConfigureAwait(false);
                                    break;
                                case "update":
                                    await UpdateGatewayAsync(options).ConfigureAwait(false);
                                    break;
                                case "monitor":
                                    await MonitorGatewaysAsync().ConfigureAwait(false);
                                    break;
                                case "list":
                                    await ListGatewaysAsync(options).ConfigureAwait(false);
                                    break;
                                case "select":
                                    await SelectGatewayAsync(options).ConfigureAwait(false);
                                    break;
                                case "query":
                                    await QueryGatewaysAsync(options).ConfigureAwait(false);
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
                            if (args.Length < 2)
                            {
                                throw new ArgumentException("Need a command!");
                            }
                            command = args[1].ToLowerInvariant();
                            options = new CliOptions(args, 2);
                            switch (command)
                            {
                                case "browse":
                                    await BrowseAsync(options).ConfigureAwait(false);
                                    break;
                                case "select":
                                    await SelectNodeAsync(options).ConfigureAwait(false);
                                    break;
                                case "publish":
                                    await PublishAsync(options).ConfigureAwait(false);
                                    break;
                                case "monitor":
                                    await MonitorSamplesAsync(options).ConfigureAwait(false);
                                    break;
                                case "unpublish":
                                    await UnpublishAsync(options).ConfigureAwait(false);
                                    break;
                                case "list":
                                    await ListPublishedNodesAsync(options).ConfigureAwait(false);
                                    break;
                                case "read":
                                    await ReadAsync(options).ConfigureAwait(false);
                                    break;
                                case "write":
                                    await WriteAsync(options).ConfigureAwait(false);
                                    break;
                                case "metadata":
                                    await MethodMetadataAsync(options).ConfigureAwait(false);
                                    break;
                                case "call":
                                    await MethodCallAsync(options).ConfigureAwait(false);
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
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                    if (!interactive)
                    {
                        PrintHelp();
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("==================");
                    Console.WriteLine(e);
                    Console.WriteLine("==================");
                }
            }
            while (interactive);
        }
#pragma warning restore CA1308 // Normalize strings to uppercase

        private string _nodeId;

        /// <summary>
        /// Get endpoint id
        /// </summary>
        /// <param name="options"></param>
        /// <param name="shouldThrow"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string GetNodeId(CliOptions options, bool shouldThrow = true)
        {
            var id = options.GetValueOrNull<string>("-n", "--nodeid");
            if (_nodeId != null)
            {
                if (id == null)
                {
                    return _nodeId;
                }
                _nodeId = null;
            }
            if (id != null)
            {
                return id;
            }
            if (!shouldThrow)
            {
                return null;
            }
            throw new ArgumentException("Missing -n/--nodeId option.");
        }

        /// <summary>
        /// Select node id
        /// </summary>
        /// <param name="options"></param>
        private async Task SelectNodeAsync(CliOptions options)
        {
            if (options.IsSet("-c", "--clear"))
            {
                _nodeId = null;
            }
            else if (options.IsSet("-s", "--show"))
            {
                Console.WriteLine(_nodeId);
            }
            else
            {
                var nodeId = options.GetValueOrNull<string>("-n", "--nodeid");
                if (string.IsNullOrEmpty(nodeId))
                {
                    var id = GetEndpointId(options, false);
                    if (!string.IsNullOrEmpty(id))
                    {
                        var results = await _client.Twin.NodeBrowseAsync(id, new BrowseFirstRequestModel
                        {
                            TargetNodesOnly = true,
                            NodeId = _nodeId
                        }).ConfigureAwait(false);
                        var node = ConsoleEx.Select(results.References.Select(r => r.Target),
                            n => n.BrowseName);
                        if (node != null)
                        {
                            nodeId = node.NodeId;
                        }
                    }
                    if (string.IsNullOrEmpty(nodeId))
                    {
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
        /// <param name="options"></param>
        private async Task MethodCallAsync(CliOptions options)
        {
            var result = await _client.Twin.NodeMethodCallAsync(
                GetEndpointId(options),
                new MethodCallRequestModel
                {
                    Header = GetRequestHeader(options),
                    MethodId = GetNodeId(options),
                    ObjectId = options.GetValueOrThrow<string>("-o", "--objectid")

                    // ...
                }).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="options"></param>
        private async Task MethodMetadataAsync(CliOptions options)
        {
            var result = await _client.Twin.NodeMethodGetMetadataAsync(
                GetEndpointId(options),
                new MethodMetadataRequestModel
                {
                    Header = GetRequestHeader(options),
                    MethodId = GetNodeId(options)
                }).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Write value
        /// </summary>
        /// <param name="options"></param>
        private async Task WriteAsync(CliOptions options)
        {
            var result = await _client.Twin.NodeValueWriteAsync(
                GetEndpointId(options),
                new ValueWriteRequestModel
                {
                    Header = GetRequestHeader(options),
                    NodeId = GetNodeId(options),
                    DataType = options.GetValueOrNull<string>("-t", "--datatype"),
                    Value = _client.Serializer.FromObject(
                        options.GetValueOrThrow<string>("-v", "--value"))
                }).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="options"></param>
        private async Task ReadAsync(CliOptions options)
        {
            var result = await _client.Twin.NodeValueReadAsync(
                GetEndpointId(options),
                new ValueReadRequestModel
                {
                    Header = GetRequestHeader(options),
                    NodeId = GetNodeId(options)
                }).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Browse nodes
        /// </summary>
        /// <param name="options"></param>
        private async Task BrowseAsync(CliOptions options)
        {
            var id = GetEndpointId(options);
            var silent = options.IsSet("-s", "--silent");
            var all = options.IsSet("-A", "--all");
            var recursive = options.IsSet("-r", "--recursive");
            var readDuringBrowse = options.IsProvidedOrNull("-v", "--readvalue");
            var request = new BrowseFirstRequestModel
            {
                Header = GetRequestHeader(options),
                TargetNodesOnly = options.IsProvidedOrNull("-t", "--targets"),
                ReadVariableValues = readDuringBrowse,
                MaxReferencesToReturn = options.GetValueOrNull<uint?>("-x", "--maxrefs"),
                Direction = options.GetValueOrNull<BrowseDirection?>("-d", "--direction")
            };
            var nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                options.GetValueOrNull<string>("-n", "--nodeid")
            };
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nodesRead = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = 0;
            var sw = Stopwatch.StartNew();
            while (nodes.Count > 0)
            {
                request.NodeId = nodes.First();
                nodes.Remove(request.NodeId);
                try
                {
                    var result = await (all ?
                        _client.Twin.NodeBrowseAsync(id, request) :
                        _client.Twin.NodeBrowseFirstAsync(id, request)).ConfigureAwait(false);
                    visited.Add(request.NodeId);
                    if (!silent)
                    {
                        PrintResult(options, result);
                    }
                    if (readDuringBrowse ?? false)
                    {
                        continue;
                    }
                    // Do recursive browse
                    if (recursive)
                    {
                        foreach (var r in result.References)
                        {
                            if (!visited.Contains(r.ReferenceTypeId))
                            {
                                nodes.Add(r.ReferenceTypeId);
                            }
                            if (!visited.Contains(r.Target.NodeId))
                            {
                                nodes.Add(r.Target.NodeId);
                            }
                            if (nodesRead.Contains(r.Target.NodeId))
                            {
                                continue; // We have read this one already
                            }
                            if (!r.Target.NodeClass.HasValue ||
                                r.Target.NodeClass.Value != NodeClass.Variable)
                            {
                                continue;
                            }
                            if (!silent)
                            {
                                Console.WriteLine($"Reading {r.Target.NodeId}");
                            }
                            try
                            {
                                nodesRead.Add(r.Target.NodeId);
                                var read = await _client.Twin.NodeValueReadAsync(id,
                                    new ValueReadRequestModel
                                    {
                                        NodeId = r.Target.NodeId
                                    }).ConfigureAwait(false);
                                if (!silent)
                                {
                                    PrintResult(options, read);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Reading {r.Target.NodeId} resulted in {ex}");
                                errors++;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
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
        /// <param name="options"></param>
        private async Task PublishAsync(CliOptions options)
        {
            var result = await _client.Publisher.NodePublishStartAsync(
                GetEndpointId(options),
                new PublishStartRequestModel
                {
                    Header = GetRequestHeader(options),
                    Item = new PublishedItemModel
                    {
                        NodeId = GetNodeId(options),
                        SamplingInterval = TimeSpan.FromMilliseconds(1000),
                        PublishingInterval = TimeSpan.FromMilliseconds(1000)
                    }
                }).ConfigureAwait(false);
            if (result.ErrorInfo != null)
            {
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Monitor samples from endpoint
        /// </summary>
        /// <param name="options"></param>
        private async Task MonitorSamplesAsync(CliOptions options)
        {
            var endpointId = GetEndpointId(options);
            Console.WriteLine("Press any key to stop.");

            var finish = await _client.Telemetry.NodePublishSubscribeByEndpointAsync(
                endpointId, PrintSampleAsync).ConfigureAwait(false);
            try
            {
                Console.ReadKey();
            }
            finally
            {
                await finish.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Unpublish node
        /// </summary>
        /// <param name="options"></param>
        private async Task UnpublishAsync(CliOptions options)
        {
            var result = await _client.Publisher.NodePublishStopAsync(
                GetEndpointId(options),
                new PublishStopRequestModel
                {
                    Header = GetRequestHeader(options),
                    NodeId = GetNodeId(options)
                }).ConfigureAwait(false);
            if (result.ErrorInfo != null)
            {
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List published nodes
        /// </summary>
        /// <param name="options"></param>
        private async Task ListPublishedNodesAsync(CliOptions options)
        {
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Publisher.NodePublishListAllAsync(
                    GetEndpointId(options)).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Publisher.NodePublishListAsync(GetEndpointId(options),
                    options.GetValueOrNull<string>("-C", "--continuation")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        private string _publisherId;

        /// <summary>
        /// Get publisher id
        /// </summary>
        /// <param name="options"></param>
        /// <param name="shouldThrow"></param>
        /// <exception cref="ArgumentException"></exception>
        private string GetPublisherId(CliOptions options, bool shouldThrow = true)
        {
            var id = options.GetValueOrNull<string>("-i", "--id");
            if (_publisherId != null)
            {
                if (id == null)
                {
                    return _publisherId;
                }
                _publisherId = null;
            }
            if (id != null)
            {
                return id;
            }
            if (!shouldThrow)
            {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select publisher registration
        /// </summary>
        /// <param name="options"></param>
        private async Task SelectPublisherAsync(CliOptions options)
        {
            if (options.IsSet("-c", "--clear"))
            {
                _publisherId = null;
            }
            else if (options.IsSet("-s", "--show"))
            {
                Console.WriteLine(_publisherId);
            }
            else
            {
                var publisherId = options.GetValueOrNull<string>("-i", "--id");
                if (string.IsNullOrEmpty(publisherId))
                {
                    var result = await _client.Registry.ListAllPublishersAsync().ConfigureAwait(false);
                    publisherId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(publisherId))
                    {
                        Console.WriteLine("Nothing selected - publisher selection cleared.");
                    }
                    else
                    {
                        Console.WriteLine($"Selected {publisherId}.");
                    }
                }
                _publisherId = publisherId;
            }
        }

        /// <summary>
        /// List publisher registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task ListPublishersAsync(CliOptions options)
        {
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.ListAllPublishersAsync(
                    options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.ListPublishersAsync(
                    options.GetValueOrNull<string>("-C", "--continuation"),
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query publisher registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task QueryPublishersAsync(CliOptions options)
        {
            var query = new PublisherQueryModel
            {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                SiteId = options.GetValueOrNull<string>("-s", "--siteId")
            };
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.QueryAllPublishersAsync(query,
                    options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.QueryPublishersAsync(query,
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get configured endpoints on publisher
        /// </summary>
        /// <param name="options"></param>
        private async Task GetConfiguredEndpointsAsync(CliOptions options)
        {
            var file = options.GetValueOrNull<string>("f", "--file");
            var stream = file == null ? Console.Out : File.CreateText(file);
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteLineAsync("[").ConfigureAwait(false);
                var empty = true;

                await foreach (var endpoint in _client.Registry.GetConfiguredEndpointsAsync(
                    GetPublisherId(options), new GetConfiguredEndpointsRequestModel
                    {
                        IncludeNodes = options.IsProvidedOrNull("-n", "--nodes")
                    }))
                {
                    if (!empty)
                    {
                        await stream.WriteLineAsync(",").ConfigureAwait(false);
                    }
                    empty = false;
                    await stream.WriteAsync(_client.Serializer.SerializeToString(endpoint,
                        options.GetValueOrDefault(SerializeOption.Indented, "-F", "--format"))).ConfigureAwait(false);
                }
                if (!empty)
                {
                    await stream.WriteLineAsync().ConfigureAwait(false);
                }
                await stream.WriteLineAsync("]").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Clear configured endpoints on publisher
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task SetConfiguredEndpointsAsync(CliOptions options)
        {
            var publishedNodes = new SetConfiguredEndpointsRequestModel();
            var file = options.GetValueOrNull<string>("f", "--file");
            if (file != null)
            {
                publishedNodes.Endpoints = _client.Serializer.Deserialize<IEnumerable<PublishedNodesEntryModel>>(
                    await File.ReadAllBytesAsync(file).ConfigureAwait(false));
            }
            await _client.Registry.SetConfiguredEndpointsAsync(GetPublisherId(options),
                publishedNodes).ConfigureAwait(false);
        }

        /// <summary>
        /// Get publisher
        /// </summary>
        /// <param name="options"></param>
        private async Task GetPublisherAsync(CliOptions options)
        {
            var result = await _client.Registry.GetPublisherAsync(GetPublisherId(options),
                options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Update publisher
        /// </summary>
        /// <param name="options"></param>
        private async Task UpdatePublisherAsync(CliOptions options)
        {
            await _client.Registry.UpdatePublisherAsync(GetPublisherId(options),
                new PublisherUpdateModel
                {
                    SiteId = options.GetValueOrNull<string>("-s", "--siteId"),
                    ApiKey = options.GetValueOrNull<string>("-a", "--api-key")
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Monitor publishers
        /// </summary>
        private async Task MonitorPublishersAsync()
        {
            Console.WriteLine("Press any key to stop.");
            var complete = await _client.Events.SubscribePublisherEventsAsync(PrintEventAsync).ConfigureAwait(false);
            try
            {
                Console.ReadKey();
            }
            finally
            {
                await complete.DisposeAsync().ConfigureAwait(false);
            }
        }

        private string _gatewayId;

        /// <summary>
        /// Get gateway id
        /// </summary>
        /// <param name="options"></param>
        /// <param name="shouldThrow"></param>
        /// <exception cref="ArgumentException"></exception>
        private string GetGatewayId(CliOptions options, bool shouldThrow = true)
        {
            var id = options.GetValueOrNull<string>("-i", "--id");
            if (_gatewayId != null)
            {
                if (id == null)
                {
                    return _gatewayId;
                }
                _gatewayId = null;
            }
            if (id != null)
            {
                return id;
            }
            if (!shouldThrow)
            {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select gateway registration
        /// </summary>
        /// <param name="options"></param>
        private async Task SelectGatewayAsync(CliOptions options)
        {
            if (options.IsSet("-c", "--clear"))
            {
                _gatewayId = null;
            }
            else if (options.IsSet("-s", "--show"))
            {
                Console.WriteLine(_gatewayId);
            }
            else
            {
                var gatewayId = options.GetValueOrNull<string>("-i", "--id");
                if (string.IsNullOrEmpty(gatewayId))
                {
                    var result = await _client.Registry.ListAllGatewaysAsync().ConfigureAwait(false);
                    gatewayId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(gatewayId))
                    {
                        Console.WriteLine("Nothing selected - gateway selection cleared.");
                    }
                    else
                    {
                        Console.WriteLine($"Selected {gatewayId}.");
                    }
                }
                _gatewayId = gatewayId;
            }
        }

        /// <summary>
        /// List gateway registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task ListGatewaysAsync(CliOptions options)
        {
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.ListAllGatewaysAsync().ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.ListGatewaysAsync(
                    options.GetValueOrNull<string>("-C", "--continuation"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query gateway registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task QueryGatewaysAsync(CliOptions options)
        {
            var query = new GatewayQueryModel
            {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                SiteId = options.GetValueOrNull<string>("-s", "--siteId")
            };
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.QueryAllGatewaysAsync(query).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.QueryGatewaysAsync(query,
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get gateway
        /// </summary>
        /// <param name="options"></param>
        private async Task GetGatewayAsync(CliOptions options)
        {
            var result = await _client.Registry.GetGatewayAsync(GetGatewayId(options)).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Update gateway
        /// </summary>
        /// <param name="options"></param>
        private async Task UpdateGatewayAsync(CliOptions options)
        {
            await _client.Registry.UpdateGatewayAsync(GetGatewayId(options),
                new GatewayUpdateModel
                {
                    SiteId = options.GetValueOrNull<string>("-s", "--siteId")
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Monitor gateways
        /// </summary>
        private async Task MonitorGatewaysAsync()
        {
            Console.WriteLine("Press any key to stop.");
            var complete = await _client.Events.SubscribeGatewayEventsAsync(PrintEventAsync).ConfigureAwait(false);
            try
            {
                Console.ReadKey();
            }
            finally
            {
                await complete.DisposeAsync().ConfigureAwait(false);
            }
        }

        private string _supervisorId;

        /// <summary>
        /// Get supervisor id
        /// </summary>
        /// <param name="options"></param>
        /// <param name="shouldThrow"></param>
        /// <exception cref="ArgumentException"></exception>
        private string GetSupervisorId(CliOptions options, bool shouldThrow = true)
        {
            var id = options.GetValueOrNull<string>("-i", "--id");
            if (_supervisorId != null)
            {
                if (id == null)
                {
                    return _supervisorId;
                }
                _supervisorId = null;
            }
            if (id != null)
            {
                return id;
            }
            if (!shouldThrow)
            {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select supervisor registration
        /// </summary>
        /// <param name="options"></param>
        private async Task SelectSupervisorAsync(CliOptions options)
        {
            if (options.IsSet("-c", "--clear"))
            {
                _supervisorId = null;
            }
            else if (options.IsSet("-s", "--show"))
            {
                Console.WriteLine(_supervisorId);
            }
            else
            {
                var supervisorId = options.GetValueOrNull<string>("-i", "--id");
                if (string.IsNullOrEmpty(supervisorId))
                {
                    var result = await _client.Registry.ListAllSupervisorsAsync().ConfigureAwait(false);
                    supervisorId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(supervisorId))
                    {
                        Console.WriteLine("Nothing selected - supervisor selection cleared.");
                    }
                    else
                    {
                        Console.WriteLine($"Selected {supervisorId}.");
                    }
                }
                _supervisorId = supervisorId;
            }
        }

        /// <summary>
        /// List supervisor registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task ListSupervisorsAsync(CliOptions options)
        {
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.ListAllSupervisorsAsync(
                    options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.ListSupervisorsAsync(
                    options.GetValueOrNull<string>("-C", "--continuation"),
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query supervisor registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task QuerySupervisorsAsync(CliOptions options)
        {
            var query = new SupervisorQueryModel
            {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                EndpointId = options.GetValueOrNull<string>("-e", "--endpoint"),
                SiteId = options.GetValueOrNull<string>("-s", "--siteId")
            };
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.QueryAllSupervisorsAsync(query,
                    options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.QuerySupervisorsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="options"></param>
        private async Task GetSupervisorAsync(CliOptions options)
        {
            var result = await _client.Registry.GetSupervisorAsync(GetSupervisorId(options),
                options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Monitor supervisors
        /// </summary>
        private async Task MonitorSupervisorsAsync()
        {
            Console.WriteLine("Press any key to stop.");
            var complete = await _client.Events.SubscribeSupervisorEventsAsync(PrintEventAsync).ConfigureAwait(false);
            try
            {
                Console.ReadKey();
            }
            finally
            {
                await complete.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Update supervisor
        /// </summary>
        /// <param name="options"></param>
        private async Task UpdateSupervisorAsync(CliOptions options)
        {
            var config = BuildDiscoveryConfig(options);
            await _client.Registry.UpdateSupervisorAsync(GetSupervisorId(options),
                new SupervisorUpdateModel
                {
                    SiteId = options.GetValueOrNull<string>("-s", "--siteId")
                }).ConfigureAwait(false);
        }

        private string _discovererId;

        /// <summary>
        /// Get discoverer id
        /// </summary>
        /// <param name="options"></param>
        /// <param name="shouldThrow"></param>
        /// <exception cref="ArgumentException"></exception>
        private string GetDiscovererId(CliOptions options, bool shouldThrow = true)
        {
            var id = options.GetValueOrNull<string>("-i", "--id");
            if (_discovererId != null)
            {
                if (id == null)
                {
                    return _discovererId;
                }
                _discovererId = null;
            }
            if (id != null)
            {
                return id;
            }
            if (!shouldThrow)
            {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select discoverer registration
        /// </summary>
        /// <param name="options"></param>
        private async Task SelectDiscovererAsync(CliOptions options)
        {
            if (options.IsSet("-c", "--clear"))
            {
                _discovererId = null;
            }
            else if (options.IsSet("-s", "--show"))
            {
                Console.WriteLine(_discovererId);
            }
            else
            {
                var discovererId = options.GetValueOrNull<string>("-i", "--id");
                if (string.IsNullOrEmpty(discovererId))
                {
                    var result = await _client.Registry.ListAllDiscoverersAsync().ConfigureAwait(false);
                    discovererId = ConsoleEx.Select(result.Select(r => r.Id));
                    if (string.IsNullOrEmpty(discovererId))
                    {
                        Console.WriteLine("Nothing selected - discoverer selection cleared.");
                    }
                    else
                    {
                        Console.WriteLine($"Selected {discovererId}.");
                    }
                }
                _discovererId = discovererId;
            }
        }

        /// <summary>
        /// List discoverer registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task ListDiscoverersAsync(CliOptions options)
        {
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.ListAllDiscoverersAsync().ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.ListDiscoverersAsync(
                    options.GetValueOrNull<string>("-C", "--continuation"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query discoverer registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task QueryDiscoverersAsync(CliOptions options)
        {
            var query = new DiscovererQueryModel
            {
                Connected = options.IsProvidedOrNull("-c", "--connected"),
                Discovery = options.GetValueOrNull<DiscoveryMode?>("-d", "--discovery"),
                SiteId = options.GetValueOrNull<string>("-s", "--siteId")
            };
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.QueryAllDiscoverersAsync(query).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.QueryDiscoverersAsync(query,
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get discoverer
        /// </summary>
        /// <param name="options"></param>
        private async Task GetDiscovererAsync(CliOptions options)
        {
            var result = await _client.Registry.GetDiscovererAsync(GetDiscovererId(options)).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Monitor discoverers
        /// </summary>
        /// <param name="options"></param>
        private async Task MonitorDiscoverersAsync(CliOptions options)
        {
            Console.WriteLine("Press any key to stop.");
            IAsyncDisposable complete;
            var discovererId = options.GetValueOrNull<string>("-i", "--id");
            if (discovererId != null)
            {
                // If specified - monitor progress
                complete = await _client.Events.SubscribeDiscoveryProgressByDiscovererIdAsync(
                    discovererId, PrintProgressAsync).ConfigureAwait(false);
            }
            else
            {
                complete = await _client.Events.SubscribeDiscovererEventsAsync(PrintEventAsync).ConfigureAwait(false);
            }
            try
            {
                Console.ReadKey();
            }
            finally
            {
                await complete.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Update discoverer
        /// </summary>
        /// <param name="options"></param>
        private async Task UpdateDiscovererAsync(CliOptions options)
        {
            var config = BuildDiscoveryConfig(options);
            await _client.Registry.UpdateDiscovererAsync(GetDiscovererId(options),
                new DiscovererUpdateModel
                {
                    SiteId = options.GetValueOrNull<string>("-s", "--siteId")
                }).ConfigureAwait(false);
        }

        private string _applicationId;

        /// <summary>
        /// Get application id
        /// </summary>
        /// <param name="options"></param>
        /// <param name="shouldThrow"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string GetApplicationId(CliOptions options, bool shouldThrow = true)
        {
            var id = options.GetValueOrNull<string>("-i", "--id");
            if (_applicationId != null)
            {
                if (id == null)
                {
                    return _applicationId;
                }
                _applicationId = null;
            }
            if (id != null)
            {
                return id;
            }
            if (!shouldThrow)
            {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select application registration
        /// </summary>
        /// <param name="options"></param>
        private async Task SelectApplicationAsync(CliOptions options)
        {
            if (options.IsSet("-c", "--clear"))
            {
                _applicationId = null;
            }
            else if (options.IsSet("-s", "--show"))
            {
                Console.WriteLine(_applicationId);
            }
            else
            {
                var applicationId = options.GetValueOrNull<string>("-i", "--id");
                if (string.IsNullOrEmpty(applicationId))
                {
                    var result = await _client.Registry.ListAllApplicationsAsync().ConfigureAwait(false);
                    applicationId = ConsoleEx.Select(result.Select(r => r.ApplicationId));
                    if (string.IsNullOrEmpty(applicationId))
                    {
                        Console.WriteLine("Nothing selected - application selection cleared.");
                    }
                    else
                    {
                        Console.WriteLine($"Selected {applicationId}.");
                    }
                }
                _applicationId = applicationId;
            }
        }

        /// <summary>
        /// Registers application
        /// </summary>
        /// <param name="options"></param>
        private async Task RegisterApplicationAsync(CliOptions options)
        {
            var discoveryUrl = options.GetValueOrNull<string>("-d", "--discoveryUrl");
            var result = await _client.Registry.RegisterAsync(
                new ApplicationRegistrationRequestModel
                {
                    ApplicationUri = options.GetValueOrThrow<string>("-u", "--url"),
                    ApplicationName = options.GetValueOrNull<string>("-n", "--name"),
                    GatewayServerUri = options.GetValueOrNull<string>("-g", "--gwuri"),
                    ApplicationType = options.GetValueOrNull<ApplicationType?>("-t", "--type"),
                    ProductUri = options.GetValueOrNull<string>("-p", "--product"),
                    DiscoveryProfileUri = options.GetValueOrNull<string>("-r", "--dpuri"),
                    DiscoveryUrls = string.IsNullOrEmpty(discoveryUrl) ? null :
                        new HashSet<string> { discoveryUrl }
                }).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Registers server
        /// </summary>
        /// <param name="options"></param>
        private async Task RegisterServerAsync(CliOptions options)
        {
            IRegistryServiceEvents events = null;
            var id = options.GetValueOrDefault(Guid.NewGuid().ToString(), "-i", "--id");
            if (options.IsSet("-m", "--monitor"))
            {
                events = _client.Events;
                var tcs = new TaskCompletionSource<bool>();

                var discovery = await events.SubscribeDiscoveryProgressByRequestIdAsync(
                    id, async ev =>
                    {
                        await PrintProgressAsync(ev).ConfigureAwait(false);
                        switch (ev.EventType)
                        {
                            case DiscoveryProgressType.Error:
                            case DiscoveryProgressType.Cancelled:
                            case DiscoveryProgressType.Finished:
                                tcs.TrySetResult(true);
                                break;
                        }
                    }).ConfigureAwait(false);
                try
                {
                    await RegisterServerAsync(options, id).ConfigureAwait(false);
                    await tcs.Task.ConfigureAwait(false); // For completion
                }
                finally
                {
                    await discovery.DisposeAsync().ConfigureAwait(false);
                }
            }
            else
            {
                await RegisterServerAsync(options, id).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        /// <param name="options"></param>
        /// <param name="id"></param>
        private async Task RegisterServerAsync(CliOptions options, string id)
        {
            await _client.Registry.RegisterAsync(
                new ServerRegistrationRequestModel
                {
                    Id = id,
                    DiscoveryUrl = options.GetValueOrThrow<string>("-u", "--url")
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        /// <param name="options"></param>
        private async Task DiscoverServersAsync(CliOptions options)
        {
            IRegistryServiceEvents events = null;
            var id = options.GetValueOrDefault(Guid.NewGuid().ToString(), "-i", "--id");
            if (options.IsSet("-m", "--monitor"))
            {
                events = _client.Events;
                var tcs = new TaskCompletionSource<bool>();
                var discovery = await events.SubscribeDiscoveryProgressByRequestIdAsync(
                    id, async ev =>
                    {
                        await PrintProgressAsync(ev).ConfigureAwait(false);
                        switch (ev.EventType)
                        {
                            case DiscoveryProgressType.Error:
                            case DiscoveryProgressType.Cancelled:
                            case DiscoveryProgressType.Finished:
                                tcs.TrySetResult(true);
                                break;
                        }
                    }).ConfigureAwait(false);
                try
                {
                    await DiscoverServersAsync(options, id).ConfigureAwait(false);
                    await tcs.Task.ConfigureAwait(false); // For completion
                }
                finally
                {
                    await discovery.DisposeAsync().ConfigureAwait(false);
                }
            }
            else
            {
                await DiscoverServersAsync(options, id).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        /// <param name="options"></param>
        /// <param name="id"></param>
        private async Task DiscoverServersAsync(CliOptions options, string id)
        {
            await _client.Registry.DiscoverAsync(
                new DiscoveryRequestModel
                {
                    Id = id,
                    Discovery = options.GetValueOrDefault(DiscoveryMode.Fast,
                        "-d", "--discovery"),
                    Configuration = BuildDiscoveryConfig(options)
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancel discovery
        /// </summary>
        /// <param name="options"></param>
        private async Task CancelDiscoveryAsync(CliOptions options)
        {
            await _client.Registry.CancelAsync(
                new DiscoveryCancelRequestModel
                {
                    Id = options.GetValueOrThrow<string>("-i", "--id")
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Update application
        /// </summary>
        /// <param name="options"></param>
        private async Task UpdateApplicationAsync(CliOptions options)
        {
            await _client.Registry.UpdateApplicationAsync(GetApplicationId(options),
                new ApplicationRegistrationUpdateModel
                {
                    ApplicationName = options.GetValueOrNull<string>("-n", "--name"),
                    GatewayServerUri = options.GetValueOrNull<string>("-g", "--gwuri"),
                    ProductUri = options.GetValueOrNull<string>("-p", "--product"),
                    DiscoveryProfileUri = options.GetValueOrNull<string>("-r", "--dpuri")
                    // ...
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Disable application
        /// </summary>
        /// <param name="options"></param>
        private async Task DisableApplicationAsync(CliOptions options)
        {
            await _client.Registry.DisableApplicationAsync(GetApplicationId(options)).ConfigureAwait(false);
        }

        /// <summary>
        /// Enable application
        /// </summary>
        /// <param name="options"></param>
        private async Task EnableApplicationAsync(CliOptions options)
        {
            await _client.Registry.EnableApplicationAsync(GetApplicationId(options)).ConfigureAwait(false);
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        /// <param name="options"></param>
        private async Task UnregisterApplicationAsync(CliOptions options)
        {
            var id = GetApplicationId(options, false);
            if (id != null)
            {
                await _client.Registry.UnregisterApplicationAsync(id).ConfigureAwait(false);
                return;
            }

            var query = new ApplicationRegistrationQueryModel
            {
                ApplicationUri = options.GetValueOrNull<string>("-u", "--uri"),
                ApplicationType = options.GetValueOrNull<ApplicationType?>("-t", "--type"),
                ApplicationName = options.GetValueOrNull<string>("-n", "--name"),
                ProductUri = options.GetValueOrNull<string>("-p", "--product"),
                GatewayServerUri = options.GetValueOrNull<string>("-g", "--gwuri"),
                DiscoveryProfileUri = options.GetValueOrNull<string>("-r", "--dpuri"),
                Locale = options.GetValueOrNull<string>("-l", "--locale")
            };

            // Unregister all applications
            var result = await _client.Registry.QueryAllApplicationsAsync(query).ConfigureAwait(false);
            foreach (var item in result)
            {
                try
                {
                    await _client.Registry.UnregisterApplicationAsync(item.ApplicationId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to unregister {item.ApplicationId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Purge disabled applications not seen since specified amount of time.
        /// </summary>
        /// <param name="options"></param>
        private Task PurgeDisabledApplicationsAsync(CliOptions options)
        {
            return _client.Registry.PurgeDisabledApplicationsAsync(
                options.GetValueOrDefault(TimeSpan.Zero, "-f", "--for"));
        }

        /// <summary>
        /// List applications
        /// </summary>
        /// <param name="options"></param>
        private async Task ListApplicationsAsync(CliOptions options)
        {
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.ListAllApplicationsAsync().ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.ListApplicationsAsync(
                    options.GetValueOrNull<string>("-C", "--continuation"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// List sites
        /// </summary>
        /// <param name="options"></param>
        private async Task ListSitesAsync(CliOptions options)
        {
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.ListAllSitesAsync().ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.ListSitesAsync(
                    options.GetValueOrNull<string>("-C", "--continuation"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query applications
        /// </summary>
        /// <param name="options"></param>
        private async Task QueryApplicationsAsync(CliOptions options)
        {
            var query = new ApplicationRegistrationQueryModel
            {
                ApplicationUri = options.GetValueOrNull<string>("-u", "--uri"),
                ProductUri = options.GetValueOrNull<string>("-p", "--product"),
                GatewayServerUri = options.GetValueOrNull<string>("-g", "--gwuri"),
                DiscoveryProfileUri = options.GetValueOrNull<string>("-r", "--dpuri"),
                ApplicationType = options.GetValueOrNull<ApplicationType?>("-t", "--type"),
                ApplicationName = options.GetValueOrNull<string>("-n", "--name"),
                Locale = options.GetValueOrNull<string>("-l", "--locale"),
                IncludeNotSeenSince = options.IsProvidedOrNull("-d", "--deleted"),
                DiscovererId = options.GetValueOrNull<string>("-D", "--discovererId")
            };
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.QueryAllApplicationsAsync(query).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.QueryApplicationsAsync(query,
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Get application
        /// </summary>
        /// <param name="options"></param>
        private async Task GetApplicationAsync(CliOptions options)
        {
            var result = await _client.Registry.GetApplicationAsync(GetApplicationId(options)).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Monitor applications
        /// </summary>
        private async Task MonitorApplicationsAsync()
        {
            Console.WriteLine("Press any key to stop.");
            var complete = await _client.Events.SubscribeApplicationEventsAsync(PrintEventAsync).ConfigureAwait(false);
            try
            {
                Console.ReadKey();
            }
            finally
            {
                await complete.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Monitor all
        /// </summary>
        private async Task MonitorAllAsync()
        {
            var events = _client.Events;
            Console.WriteLine("Press any key to stop.");
            var apps = await events.SubscribeApplicationEventsAsync(PrintEventAsync).ConfigureAwait(false);
            try
            {
                var endpoint = await events.SubscribeEndpointEventsAsync(PrintEventAsync).ConfigureAwait(false);
                try
                {
                    var supervisor = await events.SubscribeSupervisorEventsAsync(PrintEventAsync).ConfigureAwait(false);
                    try
                    {
                        var publisher = await events.SubscribePublisherEventsAsync(PrintEventAsync).ConfigureAwait(false);
                        try
                        {
                            var discoverers = await events.SubscribeDiscovererEventsAsync(PrintEventAsync).ConfigureAwait(false);
                            try
                            {
                                var supervisors = await _client.Registry.ListAllDiscoverersAsync().ConfigureAwait(false);
                                var discovery = await Task.WhenAll(supervisors
                                    .Select(s => events.SubscribeDiscoveryProgressByDiscovererIdAsync(
                                        s.Id, PrintProgressAsync)).ToArray()).ConfigureAwait(false);
                                try
                                {
                                    Console.ReadKey();
                                }
                                finally
                                {
                                    foreach (var disposable in discovery)
                                    {
                                        await disposable.DisposeAsync().ConfigureAwait(false);
                                    }
                                }
                            }
                            finally
                            {
                                await discoverers.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            await publisher.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await supervisor.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await endpoint.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await apps.DisposeAsync().ConfigureAwait(false);
            }
        }

        private string _endpointId;

        /// <summary>
        /// Get endpoint id
        /// </summary>
        /// <param name="options"></param>
        /// <param name="shouldThrow"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string GetEndpointId(CliOptions options, bool shouldThrow = true)
        {
            var id = options.GetValueOrNull<string>("-i", "--id");
            if (_endpointId != null)
            {
                if (id == null)
                {
                    return _endpointId;
                }
                _endpointId = null;
            }
            if (id != null)
            {
                return id;
            }
            if (!shouldThrow)
            {
                return null;
            }
            throw new ArgumentException("Missing -i/--id option.");
        }

        /// <summary>
        /// Select endpoint registration
        /// </summary>
        /// <param name="options"></param>
        private async Task SelectEndpointsAsync(CliOptions options)
        {
            if (options.IsSet("-c", "--clear"))
            {
                _endpointId = null;
            }
            else if (options.IsSet("-s", "--show"))
            {
                Console.WriteLine(_endpointId);
            }
            else
            {
                var endpointId = options.GetValueOrNull<string>("-i", "--id");
                if (string.IsNullOrEmpty(endpointId))
                {
                    var result = await _client.Registry.ListAllEndpointsAsync().ConfigureAwait(false);
                    endpointId = ConsoleEx.Select(result.Select(r => r.Registration.Id));
                    if (string.IsNullOrEmpty(endpointId))
                    {
                        Console.WriteLine("Nothing selected - endpoint selection cleared.");
                    }
                    else
                    {
                        Console.WriteLine($"Selected {endpointId}.");
                    }
                }
                _endpointId = endpointId;
            }
        }

        /// <summary>
        /// List endpoint registrations
        /// </summary>
        /// <param name="options"></param>
        private async Task ListEndpointsAsync(CliOptions options)
        {
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.ListAllEndpointsAsync(
                    options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.ListEndpointsAsync(
                    options.GetValueOrNull<string>("-C", "--continuation"),
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Query endpoints
        /// </summary>
        /// <param name="options"></param>
        private async Task QueryEndpointsAsync(CliOptions options)
        {
            var query = new EndpointRegistrationQueryModel
            {
                Url = options.GetValueOrNull<string>("-u", "--uri"),
                SecurityMode = options.GetValueOrNull<SecurityMode?>("-m", "--mode"),
                SecurityPolicy = options.GetValueOrNull<string>("-l", "--policy"),
                EndpointState = options.GetValueOrNull<EndpointConnectivityState?>(
                    "-s", "--state", null),
                IncludeNotSeenSince = options.IsProvidedOrNull("-d", "--deleted"),
                ApplicationId = options.GetValueOrNull<string>("-R", "--applicationId"),
                SiteOrGatewayId = options.GetValueOrNull<string>("-G", "--siteId"),
                DiscovererId = options.GetValueOrNull<string>("-D", "--discovererId")
            };
            if (options.IsSet("-A", "--all"))
            {
                var result = await _client.Registry.QueryAllEndpointsAsync(query,
                    options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
                PrintResult(options, result);
                Console.WriteLine($"{result.Count()} item(s) found...");
            }
            else
            {
                var result = await _client.Registry.QueryEndpointsAsync(query,
                    options.IsProvidedOrNull("-S", "--server"),
                    options.GetValueOrNull<int?>("-P", "--page-size")).ConfigureAwait(false);
                PrintResult(options, result);
            }
        }

        /// <summary>
        /// Register endpoint directly
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task RegisterEndpointAsync(CliOptions options)
        {
            var result = await _client.Registry.RegisterEndpointAsync(
                new ServerEndpointQueryModel
                {
                    DiscoveryUrl = options.GetValueOrThrow<string>("-u", "--url"),
                    Url = options.GetValueOrNull<string>("-e", "--endpoint-url"),
                    SecurityMode = options.GetValueOrNull<SecurityMode?>("-m", "--mode"),
                    SecurityPolicy = options.GetValueOrNull<string>("-l", "--policy"),
                    Certificate = options.GetValueOrNull<string>("-c", "--certificate")
                }).ConfigureAwait(false);

            PrintResult(options, result);
        }

        /// <summary>
        /// Get endpoint
        /// </summary>
        /// <param name="options"></param>
        private async Task GetEndpointAsync(CliOptions options)
        {
            var result = await _client.Registry.GetEndpointAsync(GetEndpointId(options),
                options.IsProvidedOrNull("-S", "--server")).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Get server capabilities
        /// </summary>
        /// <param name="options"></param>
        private async Task GetServerCapablitiesAsync(CliOptions options)
        {
            var result = await _client.Twin.GetServerCapabilitiesAsync(
                GetEndpointId(options)).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Get endpoint certificate
        /// </summary>
        /// <param name="options"></param>
        private async Task GetEndpointCertificateAsync(CliOptions options)
        {
            var result = await _client.Registry.GetEndpointCertificateAsync(
                GetEndpointId(options)).ConfigureAwait(false);
            PrintResult(options, result);
        }

        /// <summary>
        /// Monitor endpoints
        /// </summary>
        private async Task MonitorEndpointsAsync()
        {
            Console.WriteLine("Press any key to stop.");
            var complete = await _client.Events.SubscribeEndpointEventsAsync(
                PrintEventAsync).ConfigureAwait(false);
            try
            {
                Console.ReadKey();
            }
            finally
            {
                await complete.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get status
        /// </summary>
        private async Task GetStatusAsync()
        {
            Console.WriteLine("Twin:      "
                + await _client.Twin.GetServiceStatusAsync().ConfigureAwait(false));
            Console.WriteLine("Registry:  "
                + await _client.Registry.GetServiceStatusAsync().ConfigureAwait(false));
            Console.WriteLine("Publisher: "
                + await _client.Publisher.GetServiceStatusAsync().ConfigureAwait(false));
            Console.WriteLine("History:   "
                + await _client.History.GetServiceStatusAsync().ConfigureAwait(false));
        }

        /// <summary>
        /// Get request header
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static RequestHeaderModel GetRequestHeader(CliOptions options)
        {
            var namespaceFormat = options.GetValueOrNull<NamespaceFormat?>("-N", "--ns-format");
            if (namespaceFormat == null)
            {
                return null;
            }
            return new RequestHeaderModel
            {
                NamespaceFormat = namespaceFormat
            };
        }

        /// <summary>
        /// Print result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <param name="result"></param>
        private void PrintResult<T>(CliOptions options, T result)
        {
            Console.WriteLine("==================");
            Console.WriteLine(_client.Serializer.SerializeToString(result,
                options.GetValueOrDefault(SerializeOption.Indented, "-F", "--format")));
            Console.WriteLine("==================");
        }

        /// <summary>
        /// Print progress
        /// </summary>
        /// <param name="ev"></param>
        private static Task PrintProgressAsync(DiscoveryProgressModel ev)
        {
            switch (ev.EventType)
            {
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
                        " Finding servers...");
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
        /// <typeparam name="T"></typeparam>
        /// <param name="ev"></param>
        private Task PrintEventAsync<T>(T ev)
        {
            Console.WriteLine(_client.Serializer.SerializeObjectToString(
                ev, format: SerializeOption.Indented));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Print sample
        /// </summary>
        /// <param name="samples"></param>
        private Task PrintSampleAsync(MonitoredItemMessageModel samples)
        {
            Console.WriteLine(_client.Serializer.SerializeToString(samples));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Build discovery config model from options
        /// </summary>
        /// <param name="options"></param>
        private static DiscoveryConfigModel BuildDiscoveryConfig(CliOptions options)
        {
            var config = new DiscoveryConfigModel();
            var empty = true;

            var addressRange = options.GetValueOrNull<string>("-r", "--address-ranges");
            if (addressRange != null)
            {
                if (addressRange == "true")
                {
                    config.AddressRangesToScan = "";
                }
                else
                {
                    config.AddressRangesToScan = addressRange;
                }
                empty = false;
            }

            var portRange = options.GetValueOrNull<string>("-p", "--port-ranges");
            if (portRange != null)
            {
                if (portRange == "true")
                {
                    config.PortRangesToScan = "";
                }
                else
                {
                    config.PortRangesToScan = portRange;
                }
                empty = false;
            }

            var netProbes = options.GetValueOrNull<int?>("-R", "--address-probes");
            if (netProbes != null && netProbes != 0)
            {
                config.MaxNetworkProbes = netProbes;
                empty = false;
            }

            var portProbes = options.GetValueOrNull<int?>("-P", "--port-probes");
            if (portProbes != null && portProbes != 0)
            {
                config.MaxPortProbes = portProbes;
                empty = false;
            }

            var netProbeTimeout = options.GetValueOrNull<int?>("-T", "--address-probe-timeout");
            if (netProbeTimeout != null && netProbeTimeout != 0)
            {
                config.NetworkProbeTimeout = TimeSpan.FromMilliseconds(netProbeTimeout.Value);
                empty = false;
            }

            var portProbeTimeout = options.GetValueOrNull<int?>("-t", "--port-probe-timeout");
            if (portProbeTimeout != null && portProbeTimeout != 0)
            {
                config.PortProbeTimeout = TimeSpan.FromMilliseconds(portProbeTimeout.Value);
                empty = false;
            }

            var idleTime = options.GetValueOrNull<int?>("-I", "--idle-time");
            if (idleTime != null && idleTime != 0)
            {
                config.IdleTimeBetweenScans = TimeSpan.FromSeconds(idleTime.Value);
                empty = false;
            }
            return empty ? null : config;
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintHelp()
        {
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

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintApplicationsHelp()
        {
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
        -m, --monitor   Monitor the discovery process to completion.

     discover    Discover applications and endpoints.
        with ...
        -i, --id        Request id for the discovery request.
        -d, --discovery Set discovery mode to use
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
        private static void PrintEndpointsHelp()
        {
            Console.WriteLine(
                @"
Manage endpoints in registry.

Commands and Options

     select      Select endpoint as -i/--id argument in all calls.
        with ...
        -i, --id        Endpoint id to select.
        -c, --clear     Clear current selection
        -s, --show      Show current selection

     add         Register a new endpoint that matches a query
        with ...
        -u, --url       The discovery url to use (mandatory)
        -e, --endpoint  The endpoint url to match against
        -c, --certificate
                        The certificate thumbprint to match
        -m, --mode      The security mode to match
        -l, --policy    The security policy to match

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
        -s, --state     Only return endpoints with specified state.
        -d, --deleted   Include soft deleted endpoints.
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

     info        Get server capabilities through endpoint
        with ...
        -i, --id        Id of endpoint of the server (mandatory)
        -N, --ns-format Namespace format for nodes and qualified names.
        -F, --format    Json format for result

     validate    Get endpoint certificate chain and validate
        with ...
        -i, --id        Id of endpoint to retrieve (mandatory)
        -F, --format    Json format for result

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintNodesHelp()
        {
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
        -N, --ns-format Namespace format for nodes and qualified names.
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
        -N, --ns-format Namespace format for nodes and qualified names.
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
        -N, --ns-format Namespace format for nodes and qualified names.
        -F, --format    Json format for result

     call        Call method node on endpoint
        with ...
        -i, --id        Id of endpoint to call method on (mandatory)
        -n, --nodeid    Method Node to call (mandatory)
        -N, --ns-format Namespace format for nodes and qualified names.
        -o, --objectid  Object context for method

     publish     Publish items from endpoint
        with ...
        -i, --id        Id of endpoint to publish value from (mandatory)
        -n, --nodeid    Node to publish (mandatory)

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
        private static void PrintGatewaysHelp()
        {
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
        private static void PrintPublishersHelp()
        {
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
        -a, --api-key   Set publisher module api key for rest auth.
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

     get-config  Get configured endpoints on publisher
        with ...
        -i, --id        Id of publisher to retrieve from (mandatory)
        -n, --nodes     Include nodes in the result
        -f, --file      Published nodes file to write to (optional
                        otherwise will write to console)
        -F, --format    Json format for result

     set-config  Set configured endpoints on publisher
        with ...
        -i, --id        Id of publisher to retrieve from (mandatory)
        -f, --file      Published nodes file with content to read.
                        (optional, without will erase configuration)
        -F, --format    Json format for result

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintSupervisorsHelp()
        {
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

     update      Update supervisor
        with ...
        -i, --id        Id of supervisor to update (mandatory)
        -s, --siteId    Updated site of the supervisor.

     help, -h, -? --help
                 Prints out this help.
"
                );
        }

        /// <summary>
        /// Print help
        /// </summary>
        private static void PrintDiscoverersHelp()
        {
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

     help, -h, -? --help
                 Prints out this help.
"
                );
        }
        private readonly ServiceClient _client;
    }
}
