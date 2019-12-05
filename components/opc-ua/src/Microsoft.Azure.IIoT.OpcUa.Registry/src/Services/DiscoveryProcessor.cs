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
        /// <param name="supervisors"></param>
        /// <param name="applications"></param>
        public DiscoveryProcessor(ISupervisorRegistry supervisors,
            IApplicationBulkProcessor applications) {
            _supervisors = supervisors ?? throw new ArgumentNullException(nameof(supervisors));
            _applications = applications ?? throw new ArgumentNullException(nameof(applications));
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryResultsAsync(string supervisorId, DiscoveryResultModel result,
            IEnumerable<DiscoveryEventModel> events) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
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
            var siteId = sites.SingleOrDefault() ?? supervisorId;
            //
            // Merge in global discovery configuration into the one sent
            // by the supervisor.
            //
            var supervisor = await _supervisors.GetSupervisorAsync(supervisorId, false);
            if (result.DiscoveryConfig == null) {
                // Use global discovery configuration
                result.DiscoveryConfig = supervisor.DiscoveryConfig;
            }
            else {
                if (result.DiscoveryConfig.ActivationFilter == null) {
                    // Use global activation filter
                    result.DiscoveryConfig.ActivationFilter =
                        supervisor.DiscoveryConfig?.ActivationFilter;
                }
            }

            // Process discovery events
            await _applications.ProcessDiscoveryEventsAsync(siteId, supervisorId, result, events);
        }

        private readonly ISupervisorRegistry _supervisors;
        private readonly IApplicationBulkProcessor _applications;
    }
}
