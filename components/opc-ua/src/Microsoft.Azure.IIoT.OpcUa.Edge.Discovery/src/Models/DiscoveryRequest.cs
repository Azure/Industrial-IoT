// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Net.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Discovery request wrapper
    /// </summary>
    internal sealed class DiscoveryRequest : IDisposable {

        /// <summary>
        /// Cancellation token to cancel request
        /// </summary>
        public CancellationToken Token => _cts.Token;

        /// <summary>
        /// Request is a scan request
        /// </summary>
        public bool IsScan { get; }

        /// <summary>
        /// Original discovery request model
        /// </summary>
        public DiscoveryRequestModel Request { get; }

        /// <summary>
        /// Network class
        /// </summary>
        public NetworkClass NetworkClass { get; }

        /// <summary>
        /// Address ranges to use or null to use from network info
        /// </summary>
        public IEnumerable<AddressRange> AddressRanges { get; }

        /// <summary>
        /// Total addresses to be scanned
        /// </summary>
        public int TotalAddresses { get; }

        /// <summary>
        /// Port ranges to use if not from discovery mode
        /// </summary>
        public IEnumerable<PortRange> PortRanges { get; }

        /// <summary>
        /// Total ports to be scanned
        /// </summary>
        public int TotalPorts { get; }

        /// <summary>
        /// Discovery mode
        /// </summary>
        public DiscoveryMode Mode =>
            Request.Discovery ?? DiscoveryMode.Off;

        /// <summary>
        /// Discovery configuration
        /// </summary>
        public DiscoveryConfigModel Configuration =>
            Request.Configuration ?? new DiscoveryConfigModel();

        /// <summary>
        /// Discovery urls
        /// </summary>
        public IEnumerable<Uri> DiscoveryUrls =>
            Configuration.DiscoveryUrls?.Select(s => new Uri(s)) ??
                Enumerable.Empty<Uri>();

        /// <summary>
        /// Create request wrapper
        /// </summary>
        public DiscoveryRequest() :
            this(null, null) {
        }

        /// <summary>
        /// Create request wrapper
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="configuration"></param>
        public DiscoveryRequest(DiscoveryMode? mode,
            DiscoveryConfigModel configuration) :
            this(new DiscoveryRequestModel {
                Id = "",
                Configuration = configuration.Clone(),
                Context = null,
                Discovery = mode
            }, NetworkClass.Wired, true) {
        }

        /// <summary>
        /// Create request wrapper
        /// </summary>
        /// <param name="request"></param>
        /// <param name="networkClass"></param>
        /// <param name="isScan"></param>
        public DiscoveryRequest(DiscoveryRequestModel request,
            NetworkClass networkClass = NetworkClass.Wired, bool isScan = false) {
            Request = request?.Clone() ?? throw new ArgumentNullException(nameof(request));
            _cts = new CancellationTokenSource();
            NetworkClass = networkClass;
            IsScan = isScan;

            if (Request.Configuration == null) {
                Request.Configuration = new DiscoveryConfigModel();
            }

            if (Request.Discovery == null ||
                Request.Discovery == DiscoveryMode.Off) {
                // Report empty configuration if off, but keep the
                // discovery urls details from the original request
                Request.Configuration = new DiscoveryConfigModel() {
                    ActivationFilter = Request.Configuration.ActivationFilter?.Clone(),
                    DiscoveryUrls = Request.Configuration.DiscoveryUrls?.ToList(),
                    Locales = Request.Configuration.Locales?.ToList()
                };
                Request.Discovery = DiscoveryMode.Off;
                return;
            }

            // Parse whatever provided

            if (!string.IsNullOrEmpty(Request.Configuration.PortRangesToScan)) {
                if (PortRange.TryParse(Request.Configuration.PortRangesToScan,
                    out var ports)) {
                    PortRanges = ports;
                    if (Request.Discovery == null) {
                        Request.Discovery = DiscoveryMode.Fast;
                    }
                }
            }

            if (!string.IsNullOrEmpty(Request.Configuration.AddressRangesToScan)) {
                if (AddressRange.TryParse(Request.Configuration.AddressRangesToScan,
                    out var addresses)) {
                    AddressRanges = addresses;
                    if (Request.Discovery == null) {
                        Request.Discovery = DiscoveryMode.Fast;
                    }
                }
            }

            // Set default ranges

            if (AddressRanges == null) {
                IEnumerable<NetInterface> interfaces;
                switch (Request.Discovery) {
                    case DiscoveryMode.Local:
                        interfaces = NetworkInformationEx.GetAllNetInterfaces(NetworkClass);
                        AddressRanges = AddLocalHost(interfaces
                            .Select(t => new AddressRange(t, true)))
                            .Distinct();
                        break;
                    case DiscoveryMode.Fast:
                        interfaces = NetworkInformationEx.GetAllNetInterfaces(NetworkClass.Wired);
                        AddressRanges = AddLocalHost(interfaces
                            .Select(t => new AddressRange(t, false, 24))
                            .Concat(interfaces
                                .Where(t => t.Gateway != null &&
                                            !t.Gateway.Equals(IPAddress.Any) &&
                                            !t.Gateway.Equals(IPAddress.None))
                                .Select(i => new AddressRange(i.Gateway, 32)))
                            .Distinct());
                        break;
                    case DiscoveryMode.Network:
                    case DiscoveryMode.Scan:
                        interfaces = NetworkInformationEx.GetAllNetInterfaces(NetworkClass);
                        AddressRanges = AddLocalHost(interfaces
                            .Select(t => new AddressRange(t, false))
                            .Concat(interfaces
                                .Where(t => t.Gateway != null &&
                                            !t.Gateway.Equals(IPAddress.Any) &&
                                            !t.Gateway.Equals(IPAddress.None))
                                .Select(i => new AddressRange(i.Gateway, 32)))
                            .Distinct());
                        break;
                    case DiscoveryMode.Off:
                    default:
                        AddressRanges = Enumerable.Empty<AddressRange>();
                        break;
                }
            }

            if (PortRanges == null) {
                switch (Request.Discovery) {
                    case DiscoveryMode.Local:
                        PortRanges = PortRange.All;
                        break;
                    case DiscoveryMode.Fast:
                        PortRanges = PortRange.WellKnown;
                        break;
                    case DiscoveryMode.Scan:
                        PortRanges = PortRange.Unassigned;
                        break;
                    case DiscoveryMode.Network:
                        PortRanges = PortRange.OpcUa;
                        break;
                    case DiscoveryMode.Off:
                    default:
                        PortRanges = Enumerable.Empty<PortRange>();
                        break;
                }
            }

            // Update reported configuration with used settings

            if (AddressRanges != null && AddressRanges.Any()) {
                Request.Configuration.AddressRangesToScan = AddressRange.Format(AddressRanges);
                TotalAddresses = AddressRanges?.Sum(r => r.Count) ?? 0;
            }

            if (PortRanges != null && PortRanges.Any()) {
                Request.Configuration.PortRangesToScan = PortRange.Format(PortRanges);
                TotalPorts = PortRanges?.Sum(r => r.Count) ?? 0;
            }

            Request.Configuration.IdleTimeBetweenScans ??= kDefaultIdleTime;
            Request.Configuration.PortProbeTimeout ??= kDefaultPortProbeTimeout;
            Request.Configuration.NetworkProbeTimeout ??= kDefaultNetworkProbeTimeout;
        }

        /// <summary>
        /// Create request wrapper
        /// </summary>
        /// <param name="request"></param>
        public DiscoveryRequest(DiscoveryRequest request) :
            this(request.Request, request.NetworkClass, request.IsScan) {
        }

        /// <summary>
        /// Cancel request
        /// </summary>
        public void Cancel() {
            Try.Op(() => _cts.Cancel());
        }

        /// <inheritdoc/>
        public void Dispose() {
            _cts.Dispose();
        }

        /// <summary>
        /// Clone options
        /// </summary>
        /// <returns></returns>
        internal DiscoveryRequest Clone() {
            return new DiscoveryRequest(this);
        }

        /// <summary>
        /// Add hosta address as fake address range
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public IEnumerable<AddressRange> AddLocalHost(IEnumerable<AddressRange> ranges) {
            if (Host.IsContainer) {
                try {
                    var addresses = Dns.GetHostAddresses("host.docker.internal");
                    var listedRanges = ranges.ToList();
                    return listedRanges.Concat(addresses
                        // Select ip4 addresses only
                        .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                        .Select(a => new IPv4Address(a))
                        // Check we do not already have them in the existing ranges
                        .Where(a => !listedRanges
                            .Any(r => a >= r.Low && a <= r.High))
                        // Select either the local or a small subnet around it
                        .Select(a => new AddressRange(a, 32, "localhost")));
                }
                catch {
                }
            }
            return ranges;
        }

        /// <summary> Default idle time is 6 hours </summary>
        private static readonly TimeSpan kDefaultIdleTime = TimeSpan.FromHours(6);
        /// <summary> Default port probe timeout is 5 seconds </summary>
        private static readonly TimeSpan kDefaultPortProbeTimeout = TimeSpan.FromSeconds(5);
        /// <summary> Default icmp timeout is 3 seconds </summary>
        private static readonly TimeSpan kDefaultNetworkProbeTimeout = TimeSpan.FromSeconds(3);

        private readonly CancellationTokenSource _cts;
    }
}
