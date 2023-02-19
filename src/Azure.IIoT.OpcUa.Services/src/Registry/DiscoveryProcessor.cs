// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Registry {
    using Azure.IIoT.OpcUa.Shared.Models;
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
        /// <param name="applications"></param>
        public DiscoveryProcessor(IApplicationBulkProcessor applications) {
            _applications = applications ?? throw new ArgumentNullException(nameof(applications));
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryResultsAsync(string discovererId, DiscoveryResultModel result,
            IEnumerable<DiscoveryEventModel> events) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }

            var gatewayId = PublisherModelEx.ParseDeviceId(discovererId, out _);

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

            // Process discovery events
            await _applications.ProcessDiscoveryEventsAsync(siteId, discovererId,
                result, events);
        }

        private readonly IApplicationBulkProcessor _applications;
    }
}
