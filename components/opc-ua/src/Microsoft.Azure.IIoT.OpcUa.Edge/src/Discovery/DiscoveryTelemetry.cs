// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Net;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Serilog;

    /// <summary>
    /// Discovery telemetry sender
    /// </summary>
    public class DiscoveryTelemetry : IDiscoveryListener {

        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="events"></param>
        public DiscoveryTelemetry(ILogger logger, IEventEmitter events) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc/>
        public void OnDiscoveryStarted(DiscoveryRequestModel request) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Discovery operation started.",
                request.Id);
        }

        /// <inheritdoc/>
        public void OnDiscoveryCancelled(DiscoveryRequestModel request) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Discovery operation cancelled.",
                request.Id);
        }

        /// <inheritdoc/>
        public void OnDiscoveryError(DiscoveryRequestModel request, Exception ex) {
            // TODO: Send telemetry
            _logger.Error(ex, "{request}: Error during discovery run...",
                request.Id);
        }

        /// <inheritdoc/>
        public void OnDiscoveryComplete(DiscoveryRequestModel request) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Discovery operation completed.",
                request.Id);
        }

        /// <inheritdoc/>
        public void OnNetScanStarted(DiscoveryRequestModel request,
            IScanner netscanner) {
            // TODO: Send telemetry
            _logger.Debug(
                "{request}: Starting network scan ({active} probes active)...",
                request.Id, netscanner.ActiveProbes);
        }

        /// <inheritdoc/>
        public void OnNetScanResult(DiscoveryRequestModel request,
            IScanner netscanner, IPAddress address) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Found address {address} ({scanned} scanned)...",
                request.Id, address, netscanner.ScanCount);
        }

        /// <inheritdoc/>
        public void OnNetScanProgress(DiscoveryRequestModel request,
            IScanner netscanner, IEnumerable<IPAddress> addresses) {
            // TODO: Send telemetry
            _logger.Debug("{request}: {scanned} addresses scanned - {found} " +
                "found ({active} probes active)...", request.Id,
                netscanner.ScanCount, addresses.Count(), netscanner.ActiveProbes);
        }

        /// <inheritdoc/>
        public void OnNetScanComplete(DiscoveryRequestModel request,
            IScanner netscanner, IEnumerable<IPAddress> addresses,
            TimeSpan elapsed) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Found {count} addresses took {elapsed} " +
                "({scanned} scanned)...", request.Id,
                addresses.Count(), elapsed, netscanner.ScanCount);
        }

        /// <inheritdoc/>
        public void OnPortScanStart(DiscoveryRequestModel request, IScanner portscan) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Starting port scanning ({active} probes active)...",
                request.Id, portscan.ActiveProbes);
        }

        /// <inheritdoc/>
        public void OnPortScanProgress(DiscoveryRequestModel request,
            IScanner portscan, IEnumerable<IPEndPoint> ports) {
            // TODO: Send telemetry
            _logger.Debug("{request}: {scanned} ports scanned - {found} found" +
                " ({active} probes active)...", request.Id,
                portscan.ScanCount, ports.Count(), portscan.ActiveProbes);
        }

        /// <inheritdoc/>
        public void OnPortScanResult(DiscoveryRequestModel request,
            IScanner portscan, IPEndPoint ep) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Found server {endpoint} ({scanned} scanned)...",
                request.Id, ep, portscan.ScanCount);
        }

        /// <inheritdoc/>
        public void OnPortScanComplete(DiscoveryRequestModel request,
            IScanner portscan, IEnumerable<IPEndPoint> ports,
            TimeSpan elapsed) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Found {count} ports on servers " +
                "took {elapsed} ({scanned} scanned)...",
                request.Id, ports.Count(), elapsed, portscan.ScanCount);
        }

        /// <inheritdoc/>
        public void OnServerDiscoveryStarted(DiscoveryRequestModel request,
            IDictionary<IPEndPoint, Uri> discoveryUrls) {
            // TODO: Send telemetry
            _logger.Debug(
                "{request}: Searching {count} discovery urls for endpoints...",
                request.Id, discoveryUrls.Count);
        }

        /// <inheritdoc/>
        public void OnFindEndpointsStarted(DiscoveryRequestModel request,
            Uri url, IPAddress address) {
            // TODO: Send telemetry
            _logger.Debug(
                "{request}: Trying to find endpoints on {host}:{port} ({address})...",
                request.Id, url.Host, url.Port, address);
        }

        /// <inheritdoc/>
        public void OnFindEndpointsComplete(DiscoveryRequestModel request,
            Uri url, IPAddress address, IEnumerable<string> endpoints) {
            // TODO: Send telemetry
            var found = endpoints.Count();
            if (found == 0) {
                // TODO: Send telemetry
                _logger.Debug(
                    "{request}: No endpoints found on {host}:{port} ({address}).",
                    request.Id, url.Host, url.Port, address);
            }
            _logger.Debug(
                "{request}: Found {count} endpoints on {host}:{port} ({address}).",
                request.Id, found, url.Host, url.Port, address);
        }

        /// <inheritdoc/>
        public void OnServerDiscoveryComplete(DiscoveryRequestModel request,
            IEnumerable<ApplicationRegistrationModel> discovered) {
            // TODO: Send telemetry
            _logger.Debug("{request}: Found total of {count} servers ...",
                request.Id, discovered.Count());
        }

        private readonly ILogger _logger;
        private readonly IEventEmitter _events;
    }
}
