// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Net.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

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
        public bool IsScan { get;}

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
                Configuration = configuration,
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

            if (!string.IsNullOrEmpty(request.Configuration?.AddressRangesToScan)) {
                if (AddressRange.TryParse(request.Configuration?.AddressRangesToScan,
                    out var addresses)) {
                    AddressRanges = addresses;
                }
            }

            if (AddressRanges == null) {
                if (request.Discovery == DiscoveryMode.Fast) {
                    var interfaces = NetworkInformationEx.GetAllNetInterfaces(NetworkClass.Wired);
                    AddressRanges = interfaces.Select(t => new AddressRange(t, false, 24));
                    AddressRanges = AddressRanges.Concat(interfaces
                        .Where(t => t.Gateway != null &&
                                    !t.Gateway.Equals(System.Net.IPAddress.Any) &&
                                    !t.Gateway.Equals(System.Net.IPAddress.None))
                        .Select(i => new AddressRange(i.Gateway, 32)));
                }
            }

            if (!string.IsNullOrEmpty(request.Configuration?.PortRangesToScan)) {
                if (PortRange.TryParse(request.Configuration?.PortRangesToScan,
                    out var ports)) {
                    PortRanges = ports;
                }
            }

            if (PortRanges == null) {
                switch (request.Discovery) {
                    case DiscoveryMode.Local:
                        PortRanges = PortRange.All;
                        break;
                    case DiscoveryMode.Fast:
                        PortRanges = PortRange.WellKnown;
                        break;
                    case DiscoveryMode.Scan:
                        PortRanges = PortRange.Unassigned;
                        break;
                    default:
                        PortRanges = PortRange.OpcUa;
                        break;
                }
            }

            TotalAddresses = AddressRanges?.Sum(r => r.Count) ?? 0;
            TotalPorts = PortRanges?.Sum(r => r.Count) ?? 0;
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
            _cts.Cancel();
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

        private readonly CancellationTokenSource _cts;
    }
}
