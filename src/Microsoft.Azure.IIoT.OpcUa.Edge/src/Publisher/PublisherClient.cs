// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
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
    /// Publisher client is a composite of the legacy v1 and new v2 method call
    /// functionality. The publisher client on startup sets up which of the clients
    /// it should use based on whether method call functionality is available
    /// at the edge, whether the publisher was discovered and usable, etc.
    /// </summary>
    public class PublisherClient : IPublishServices<EndpointModel>, IPublisher,
        IStartable, IDisposable {

        /// <inheritdoc/>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Create composite client
        /// </summary>
        /// <param name="methods"></param>
        /// <param name="opc"></param>
        /// <param name="modules"></param>
        /// <param name="discovery"></param>
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        public PublisherClient(IModuleDiscovery modules, IIdentity identity,
            IMethodClient methods, IEndpointDiscovery discovery, IEndpointServices opc,
            ILogger logger) {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _methods = methods ?? throw new ArgumentNullException(nameof(methods));
            _opc = opc ?? throw new ArgumentNullException(nameof(opc));
            _modules = modules ?? throw new ArgumentNullException(nameof(modules));
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            const string kPublisherName = "publisher";
            const int kPublisherPort = 62222;
            await _lock.WaitAsync();
            try {
                if (_publisher != null) {
                    return;
                }
                _logger.Info($"Finding publisher to use as publish service...");
                if (!string.IsNullOrEmpty(_identity.ModuleId)) {
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
                    if (await TestPublisherConnectivityAsync(publisher)) {
                        _logger.Info($"Using publisher module '{moduleId}' via methods.");
                        _publisher = publisher;
                        IsRunning = true;
                        return;
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
                    var publisher = new PublisherServerClient(_opc, Utils.GetHostName(),
                        _logger);
                    if (await TestPublisherConnectivityAsync(publisher)) {
                        _logger.Info($"Using publisher server on localhost.");
                        IsRunning = true;
                        _publisher = publisher;
                        return;
                    }
                }

                // Discover publishers in network - use fast scanning
                _logger.Info($"Try finding publishers in module network...");
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
                    _logger.Info($"Testing publisher at address {module} in network.");
                    var endpoints = await _discovery.FindEndpointsAsync(
                        new Uri("opc.tcp://" + module), null, CancellationToken.None);
                    if (!endpoints.Any()) {
                        continue;
                    }
                    var publisher = new PublisherServerClient(_opc,
                        module.Address.ToString(), _logger);
                    if (await TestPublisherConnectivityAsync(publisher)) {
                        _logger.Info($"Using publisher server at address {module}.");
                        IsRunning = true;
                        _publisher = publisher;
                        return;
                    }
                }

                // TODO Load publisher as side car service?
                // ...

                _logger.Info($"No publisher found - Publish services not supported.");
                _publisher = new PublisherStub();
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                if (_publisher == null) {
                    return;
                }
                if (_publisher is IDisposable dispose) {
                    dispose.Dispose();
                }
                _publisher = null;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Start() => StartAsync().Wait();

        /// <inheritdoc/>
        public void Dispose() => StopAsync().Wait();

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            EndpointModel endpoint, PublishStartRequestModel request) {
            if (_publisher == null) {
                await StartAsync();
            }
            return await _publisher.NodePublishStartAsync(endpoint, request);
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            EndpointModel endpoint, PublishStopRequestModel request) {
            if (_publisher == null) {
                await StartAsync();
            }
            return await _publisher.NodePublishStopAsync(endpoint, request);
        }

        /// <inheritdoc/>
        public async Task<PublishedNodeListResultModel> NodePublishListAsync(
            EndpointModel endpoint, PublishedNodeListRequestModel request) {
            if (_publisher == null) {
                await StartAsync();
            }
            return await _publisher.NodePublishListAsync(endpoint, request);
        }

        /// <summary>
        /// Test connectivity by listing and ensuring no exception happens.
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> TestPublisherConnectivityAsync(
            IPublishServices<EndpointModel> publisher) {
            try {
                await publisher.NodePublishListAsync(new EndpointModel {
                    Url = "opc.tcp://test"
                }, new PublishedNodeListRequestModel());
                return true;
            }
            catch {
                return false;
            }
        }

        private IPublishServices<EndpointModel> _publisher;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly IIdentity _identity;
        private readonly IMethodClient _methods;
        private readonly IEndpointServices _opc;
        private readonly IModuleDiscovery _modules;
        private readonly IEndpointDiscovery _discovery;
        private readonly ILogger _logger;
    }
}
