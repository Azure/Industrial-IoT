// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Servers {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Serilog;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Net.Scanner;
    using Autofac;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Finds the best publisher server and creates a client
    /// </summary>
    public sealed class PublisherDiscovery : IPublisherServer {

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="methods"></param>
        /// <param name="client"></param>
        /// <param name="modules"></param>
        /// <param name="discovery"></param>
        /// <param name="identity"></param>
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
            const string kPublisherName = "publisher";
            const int kPublisherPort = 62222;
            _logger.Information("Finding publisher to use as publish service...");
            if (!string.IsNullOrEmpty(_identity.DeviceId)) {
                // Get modules
                var modules = await _modules.GetModulesAsync(
                    _identity.DeviceId);
                var publisherModule = modules
                    .Where(m => m.Status.EqualsIgnoreCase("running"))
                    .FirstOrDefault(m =>
                        m.ImageName?.Contains(kPublisherName) ?? false ||
                        m.Id.EqualsIgnoreCase(kPublisherName));
                // Have publisher module
                var moduleId = publisherModule?.Id ?? kPublisherName;
                var publisher = new PublisherMethodClient(_methods,
                    _identity.DeviceId, moduleId, _logger);
                if (await TestConnectivityAsync(publisher)) {
                    _logger.Information("Using publisher module '{moduleId}' via methods.",
                        moduleId);
                    return publisher;
                }
                // use opc ua server as fallback
            }

            // Try shortcut of finding it on localhost
            var cts = new CancellationTokenSource();
            var localEndpoints = await _discovery.FindEndpointsAsync(
                new Uri($"opc.tcp://{Utils.GetHostName()}:62222"), null, cts.Token);
#if TEST_PNP_SCAN
            localEndpoints = Enumerable.Empty<Protocol.Models.DiscoveredEndpointModel>();
#endif
            if (localEndpoints.Any()) {
                var publisher = new PublisherServerClient(_client, Utils.GetHostName(), _logger);
                if (await TestConnectivityAsync(publisher)) {
                    _logger.Information("Using publisher server on localhost.");
                    return publisher;
                }
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
            foreach (var module in publishers) {
                _logger.Information("Testing publisher at address {module} in network.",
                    module);
                var endpoints = await _discovery.FindEndpointsAsync(
                    new Uri("opc.tcp://" + module), null, CancellationToken.None);
                if (!endpoints.Any()) {
                    continue;
                }
                var publisher = new PublisherServerClient(_client, module.Address.ToString(),
                    _logger);
                if (await TestConnectivityAsync(publisher)) {
                    _logger.Information("Using publisher server at address {module}.",
                        module);
                    return publisher;
                }
            }

            // TODO Load publisher as side car service?
            // ...

            return null;
        }

        /// <summary>
        /// Test connectivity by listing and ensuring no exception happens.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> TestConnectivityAsync(IPublisherClient server) {
            try {
                var test = Newtonsoft.Json.JsonConvertEx.SerializeObject(new GetNodesRequestModel {
                    EndpointUrl = "opc.tcp://test"
                });
                var (errorInfo, result) = await server.CallMethodAsync(
                    "GetConfiguredNodesOnEndpoint", test, null);
                return errorInfo == null;
            }
            catch {
                return false;
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
