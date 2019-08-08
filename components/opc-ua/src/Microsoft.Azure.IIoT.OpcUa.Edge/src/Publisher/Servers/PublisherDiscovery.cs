// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Servers {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;

    /// <summary>
    /// Finds the best publisher server and creates a client
    /// </summary>
    public sealed class PublisherDiscovery : IPublisherServer {

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="modules"></param>
        /// <param name="identity"></param>
        /// <param name="methods"></param>
        /// <param name="discovery"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public PublisherDiscovery(IModuleDiscovery modules, IIdentity identity,
            IJsonMethodClient methods, IEndpointDiscovery discovery, IEndpointServices client,
            ILogger logger) {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _methods = methods ?? throw new ArgumentNullException(nameof(methods));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _modules = modules ?? throw new ArgumentNullException(nameof(modules));
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IPublisherClient> ConnectAsync() {
            const string kPublisherName = "opcpublisher";
            const int kPublisherPort = 62222;

            _logger.Information("Finding publisher to use as publish service...");

            var deviceId = _identity.DeviceId;
            if (string.IsNullOrEmpty(deviceId)) {
                // No identity at this point - look up from IoT Edge environment
                deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            }
            if (!string.IsNullOrEmpty(deviceId)) {
                _logger.Debug("Get all modules in current edge context.");
                // Get modules
                var modules = await _modules.GetModulesAsync(deviceId);
                var publisherModule = modules
                    .Where(m => m.Status.EqualsIgnoreCase("running"))
                    .FirstOrDefault(m =>
                        m.ImageName?.Contains("opc-publisher") ?? (false ||
                        m.Id.EqualsIgnoreCase(kPublisherName)));

                if (publisherModule == null) {
                    _logger.Warning("No publisher module running in edge context.");
                }

                // Have publisher module
                var moduleId = publisherModule?.Id ?? kPublisherName;
                _logger.Information("Testing publisher module {moduleId} using methods...",
                    moduleId);
                var publisher = new PublisherMethodClient(_methods, deviceId, moduleId, _logger);
                var error = await TestConnectivityAsync(publisher);
                if (error == null) {
                    _logger.Information(
                        "Success - using publisher module '{moduleId}' ({image}) via methods.",
                        moduleId, publisherModule?.ImageName ?? "<unknown>");
                    return publisher;
                }
                _logger.Debug("Publisher module {moduleId} method call uncuccessful." +
                    " Fallback to UA server...", moduleId, error);
            }

            using (var cts = new CancellationTokenSource()) {
                // Try shortcut of finding it on default publisher edge module
                var edgeUri = new Uri($"opc.tcp://{kPublisherName}:{kPublisherPort}");
                var edgeEndpoints = await _discovery.FindEndpointsAsync(edgeUri, null, cts.Token);
                if (edgeEndpoints.Any()) {
#if !TEST_PNP_SCAN
                    var publisher = new PublisherServerClient(_client, edgeUri, _logger);
                    var error = await TestConnectivityAsync(publisher);
                    if (error == null) {
                        _logger.Information("Using publisher server on localhost.");
                        return publisher;
                    }
#endif
                }

                // Try shortcut of finding it on localhost
                var uri = new Uri($"opc.tcp://{Utils.GetHostName()}:{kPublisherPort}");
                var localEndpoints = await _discovery.FindEndpointsAsync(uri, null, cts.Token);
                if (localEndpoints.Any()) {
#if !TEST_PNP_SCAN
                    var publisher = new PublisherServerClient(_client, uri, _logger);
                    var error = await TestConnectivityAsync(publisher);
                    if (error == null) {
                        _logger.Information("Using publisher server on localhost.");
                        return publisher;
                    }
#endif
                }

                // Discover publishers in network - use fast scanning
                _logger.Information("Try finding publishers in module network...");
                var addresses = new List<IPAddress>();
                using (var netscanner = new NetworkScanner(_logger,
                    reply => addresses.Add(reply.Address), false,
                    NetworkInformationEx.GetAllNetInterfaces(NetworkClass.Wired)
                        .Select(t => new AddressRange(t, false, 24)),
                    NetworkClass.Wired, 1000, // TODO: make configurable - intent is fast.
                    null, cts.Token)) {
                    await netscanner.Completion;
                }
                var publishers = new List<IPEndPoint>();
                var probe = new ServerProbe(_logger);
                using (var portscan = new PortScanner(_logger,
                    addresses.Select(a => new IPEndPoint(a, kPublisherPort)),
                    found => {
                        cts.Cancel(); // Cancel on first found publisher.
                    publishers.Add(found);
                    }, probe, null, null, null, cts.Token)) {
                    try {
                        await portscan.Completion;
                    }
                    catch (TaskCanceledException) {
                        // We got a port, and scanning is cancelled.
                    }
                }

                // We might have found a couple publishers - lets test them...
                foreach (var ep in publishers) {
                    var remoteEp = await ep.TryResolveAsync();
                    _logger.Information("Test publisher at address {remoteEp} in network.",
                        remoteEp);
                    uri = new Uri("opc.tcp://" + remoteEp);
                    var endpoints = await _discovery.FindEndpointsAsync(uri, null,
                        CancellationToken.None);
                    if (!endpoints.Any()) {
                        continue;
                    }
                    var publisher = new PublisherServerClient(_client, uri, _logger);
                    var error = await TestConnectivityAsync(publisher);
                    if (error == null) {
                        _logger.Information("Using publisher server at address {remoteEp}.",
                            remoteEp);
                        return publisher;
                    }
                }

                // TODO: Consider loading publisher as side car service?
                // No publisher found - will try again later.
                return null;
            }
        }

        /// <summary>
        /// Test connectivity by listing and ensuring no exception happens.
        /// </summary>
        /// <returns></returns>
        private async Task<ServiceResultModel> TestConnectivityAsync(IPublisherClient server) {
            try {
                var test = JsonConvertEx.SerializeObject(new GetNodesRequestModel {
                    EndpointUrl = "opc.tcp://test"
                });
                var (errorInfo, result) = await server.CallMethodAsync(
                    "GetConfiguredNodesOnEndpoint", test, null);
                return errorInfo;
            }
            catch {
                return null;
            }
        }

        private readonly IIdentity _identity;
        private readonly IJsonMethodClient _methods;
        private readonly IEndpointServices _client;
        private readonly IModuleDiscovery _modules;
        private readonly IEndpointDiscovery _discovery;
        private readonly ILogger _logger;
    }
}
