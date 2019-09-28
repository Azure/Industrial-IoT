// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Net;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Serilog;

    /// <summary>
    /// Discovery logger
    /// </summary>
    public class DiscoveryLogger : IDiscoveryListener {

        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="logger"></param>
        public DiscoveryLogger(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public virtual void OnDiscoveryStarted(DiscoveryRequestModel request) {
            _logger.Information("{request}: Discovery operation started.",
                request.Id);
        }

        /// <inheritdoc/>
        public virtual void OnDiscoveryCancelled(DiscoveryRequestModel request) {
            _logger.Information("{request}: Discovery operation cancelled.",
                request.Id);
        }

        /// <inheritdoc/>
        public virtual void OnDiscoveryError(DiscoveryRequestModel request, Exception ex) {
            _logger.Error(ex, "{request}: Error during discovery run...",
                request.Id);
        }

        /// <inheritdoc/>
        public virtual void OnDiscoveryComplete(DiscoveryRequestModel request) {
            _logger.Information("{request}: Discovery operation completed.",
                request.Id);
        }

        /// <inheritdoc/>
        public virtual void OnNetScanStarted(DiscoveryRequestModel request,
            IScanner netscanner) {
            _logger.Information(
                "{request}: Starting network scan ({active} probes active)...",
                request.Id, netscanner.ActiveProbes);
        }

        /// <inheritdoc/>
        public virtual void OnNetScanResult(DiscoveryRequestModel request,
            IScanner netscanner, IPAddress address) {
            _logger.Information("{request}: Found address {address} ({scanned} scanned)...",
                request.Id, address, netscanner.ScanCount);
        }

        /// <inheritdoc/>
        public virtual void OnNetScanProgress(DiscoveryRequestModel request,
            IScanner netscanner, IEnumerable<IPAddress> addresses) {
            _logger.Information("{request}: {scanned} addresses scanned - {found} " +
                "found ({active} probes active)...", request.Id,
                netscanner.ScanCount, addresses.Count(), netscanner.ActiveProbes);
        }

        /// <inheritdoc/>
        public virtual void OnNetScanComplete(DiscoveryRequestModel request,
            IScanner netscanner, IEnumerable<IPAddress> addresses, TimeSpan elapsed) {
            _logger.Information("{request}: Found {count} addresses took {elapsed} " +
                "({scanned} scanned)...", request.Id,
                addresses.Count(), elapsed, netscanner.ScanCount);
        }

        /// <inheritdoc/>
        public virtual void OnPortScanStart(DiscoveryRequestModel request, IScanner portscan) {
            _logger.Information("{request}: Starting port scanning ({active} probes active)...",
                request.Id, portscan.ActiveProbes);
        }

        /// <inheritdoc/>
        public virtual void OnPortScanProgress(DiscoveryRequestModel request,
            IScanner portscan, IEnumerable<IPEndPoint> ports) {
            _logger.Information("{request}: {scanned} ports scanned - {found} found" +
                " ({active} probes active)...", request.Id,
                portscan.ScanCount, ports.Count(), portscan.ActiveProbes);
        }

        /// <inheritdoc/>
        public virtual void OnPortScanResult(DiscoveryRequestModel request,
            IScanner portscan, IPEndPoint ep) {
            _logger.Information("{request}: Found server {endpoint} ({scanned} scanned)...",
                request.Id, ep, portscan.ScanCount);
        }

        /// <inheritdoc/>
        public virtual void OnPortScanComplete(DiscoveryRequestModel request,
            IScanner portscan, IEnumerable<IPEndPoint> ports, TimeSpan elapsed) {
            _logger.Information("{request}: Found {count} ports on servers " +
                "took {elapsed} ({scanned} scanned)...",
                request.Id, ports.Count(), elapsed, portscan.ScanCount);
        }

        /// <inheritdoc/>
        public virtual void OnServerDiscoveryStarted(DiscoveryRequestModel request,
            IDictionary<IPEndPoint, Uri> discoveryUrls) {
            _logger.Information(
                "{request}: Searching {count} discovery urls for endpoints...",
                request.Id, discoveryUrls.Count);
        }

        /// <inheritdoc/>
        public virtual void OnFindEndpointsStarted(DiscoveryRequestModel request,
            Uri url, IPAddress address) {
            _logger.Information(
                "{request}: Trying to find endpoints on {host}:{port} ({address})...",
                request.Id, url.Host, url.Port, address);
        }

        /// <inheritdoc/>
        public virtual void OnFindEndpointsComplete(DiscoveryRequestModel request,
            Uri url, IPAddress address, IEnumerable<string> endpoints) {
            var found = endpoints.Count();
            if (found == 0) {
                // TODO: Send telemetry
                _logger.Information(
                    "{request}: No endpoints found on {host}:{port} ({address}).",
                    request.Id, url.Host, url.Port, address);
            }
            _logger.Information(
                "{request}: Found {count} endpoints on {host}:{port} ({address}).",
                request.Id, found, url.Host, url.Port, address);
        }

        /// <inheritdoc/>
        public virtual void OnServerDiscoveryComplete(DiscoveryRequestModel request,
            IEnumerable<ApplicationRegistrationModel> discovered) {
            _logger.Information("{request}: Found total of {count} servers ...",
                request.Id, discovered.Count());
        }

        private readonly ILogger _logger;
    }
}
