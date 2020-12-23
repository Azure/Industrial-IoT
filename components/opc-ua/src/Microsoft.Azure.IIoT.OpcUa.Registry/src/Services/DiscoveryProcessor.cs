// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Processes the discovery results received from edge application discovery
    /// </summary>
    public sealed class DiscoveryProcessor : IDiscoveryResultProcessor {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="gateways"></param>
        /// <param name="applications"></param>
        public DiscoveryProcessor(IGatewayRegistry gateways, IApplicationBulkProcessor applications) {
            _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
            _applications = applications ?? throw new ArgumentNullException(nameof(applications));
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryResultsAsync(string discovererId, DiscoveryResultModel result,
            IEnumerable<DiscoveryEventModel> events) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }

            var gatewayId = DiscovererModelEx.ParseDeviceId(discovererId, out _);

            if (result == null) {
                throw new ArgumentNullException(nameof(result));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            if ((result.RegisterOnly ?? false) && !events.Any()) {
                return;
            }

            var sites = events.Select(e => e.Application.SiteId).Distinct();
            if (sites.Count() > 1) {
                throw new ArgumentException("Unexpected number of sites in discovery");
            }
            var siteId = sites.SingleOrDefault() ?? gatewayId;
            var gateway = await _gateways.GetGatewayAsync(gatewayId);

            //
            // Merge in global discovery configuration into the one sent
            // by the discoverer.
            //
            if (result.DiscoveryConfig == null) {
                // Use global discovery configuration
                result.DiscoveryConfig = gateway.Modules?.Discoverer?.DiscoveryConfig;
            }
            else {
                if (result.DiscoveryConfig.ActivationFilter == null) {
                    // Use global activation filter
                    result.DiscoveryConfig.ActivationFilter =
                        gateway.Modules?.Discoverer?.DiscoveryConfig?.ActivationFilter;
                }
            }

            // Process discovery events
            await _applications.ProcessDiscoveryEventsAsync(siteId, discovererId,
                gateway.Modules?.Supervisor?.Id, result, events);
        }

        private readonly IGatewayRegistry _gateways;
        private readonly IApplicationBulkProcessor _applications;
    }
}
