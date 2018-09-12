// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Discovery request wrapper
    /// </summary>
    internal class DiscoveryRequest {

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
        /// Port ranges to use if not from discovery mode
        /// </summary>
        public IEnumerable<PortRange> PortRanges { get; }

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
            this (new DiscoveryRequestModel {
                Configuration = configuration,
                Discovery = mode
            }, NetworkClass.Wired) {
        }

        /// <summary>
        /// Create request wrapper
        /// </summary>
        /// <param name="request"></param>
        /// <param name="networkClass"></param>
        public DiscoveryRequest(DiscoveryRequestModel request,
            NetworkClass networkClass = NetworkClass.Wired) {
            Request = request?.Clone() ?? throw new ArgumentNullException(nameof(request));
            NetworkClass = networkClass;

            if (!string.IsNullOrEmpty(request.Configuration?.AddressRangesToScan)) {
                if (AddressRange.TryParse(request.Configuration?.AddressRangesToScan,
                    out var addresses)) {
                    AddressRanges = addresses;
                }
            }

            if (!string.IsNullOrEmpty(request.Configuration?.PortRangesToScan)) {
                if (PortRange.TryParse(request.Configuration?.PortRangesToScan,
                    out var ports)) {
                    PortRanges = ports;
                }
            }
        }

        /// <summary>
        /// Create request wrapper
        /// </summary>
        /// <param name="options"></param>
        public DiscoveryRequest(DiscoveryRequest options) :
            this (options.Request, options.NetworkClass) {
        }

        /// <summary>
        /// Clone options
        /// </summary>
        /// <returns></returns>
        internal DiscoveryRequest Clone() => new DiscoveryRequest(this);
    }
}
