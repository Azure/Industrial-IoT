// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Processes the discovery results received from edge application discovery
    /// </summary>
    public sealed class DiscoveryProcessor : IDiscoveryProcessor {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="supervisors"></param>
        /// <param name="applications"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public DiscoveryProcessor(ISupervisorRegistry supervisors,
            IApplicationBulkProcessor applications, IHttpClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            try {
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
                    if (supervisor.DiscoveryConfig?.Callbacks != null) {
                        if (result.DiscoveryConfig.Callbacks == null) {
                            result.DiscoveryConfig.Callbacks =
                                supervisor.DiscoveryConfig.Callbacks;
                        }
                        else {
                            result.DiscoveryConfig.Callbacks.AddRange(
                                supervisor.DiscoveryConfig.Callbacks);
                        }
                    }
                    if (result.DiscoveryConfig.ActivationFilter == null) {
                        // Use global activation filter
                        result.DiscoveryConfig.ActivationFilter =
                            supervisor.DiscoveryConfig?.ActivationFilter;
                    }
                }

                // Process discovery events
                await _applications.ProcessDiscoveryEventsAsync(siteId, supervisorId, result, events);

                // Notify callbacks
                await CallDiscoveryCallbacksAsync(siteId, supervisorId, result, null);
            }
            catch (Exception ex) {
                // Notify callbacks
                await CallDiscoveryCallbacksAsync(siteId, supervisorId, result, ex);
                throw ex;
            }
        }

        /// <summary>
        /// Notify discovery / registration callbacks
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="supervisorId"></param>
        /// <param name="result"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private async Task CallDiscoveryCallbacksAsync(string siteId,
            string supervisorId, DiscoveryResultModel result, Exception exception) {
            try {
                var callbacks = result?.DiscoveryConfig?.Callbacks;
                if (callbacks == null || callbacks.Count == 0) {
                    return;
                }
                await _client.CallAsync(JToken.FromObject(
                    new {
                        id = result.Id,
                        supervisorId,
                        siteId = siteId ?? supervisorId,
                        result = new {
                            config = result.DiscoveryConfig,
                            diagnostics = exception != null ?
                                JToken.FromObject(exception) : result.Diagnostics
                        }
                    }),
                    callbacks.ToArray());
            }
            catch (Exception ex) {
                _logger.Debug(ex, "Failed to notify callbacks. Continue...");
                // Continue...
            }
        }

        private readonly IHttpClient _client;
        private readonly ILogger _logger;
        private readonly ISupervisorRegistry _supervisors;
        private readonly IApplicationBulkProcessor _applications;
    }
}
