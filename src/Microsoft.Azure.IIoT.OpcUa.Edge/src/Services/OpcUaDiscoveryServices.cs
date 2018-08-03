// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Services {
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Discovery services open a connection to the server
    /// to gather application information.
    /// </summary>
    public class OpcUaDiscoveryServices : IOpcUaDiscoveryServices {

        /// <summary>
        /// Site id under which validation is happening
        /// </summary>
        public string SiteId { get; set; } = string.Empty;

        /// <summary>
        /// Supervisor id under which validation is happening
        /// </summary>
        public string SupervisorId { get; set; } = string.Empty;

        /// <summary>
        /// Create edge endpoint validator
        /// </summary>
        /// <param name="client"></param>
        public OpcUaDiscoveryServices(IOpcUaClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Discover / read application model from discovery url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        public async Task<List<ApplicationRegistrationModel>> DiscoverApplicationsAsync(
            Uri discoveryUrl) {

            var discovered = new List<ApplicationRegistrationModel>();
            var address = await GetHostAddressAsync(discoveryUrl.GetRoot());
            if (address == null) {
                throw new ResourceNotFoundException("Unable to find host");
            }

            var results = await _client.DiscoverAsync(discoveryUrl, CancellationToken.None);
            if (results.Any()) {
                _logger.Info($"Found {results.Count()} endpoints on {discoveryUrl}.",
                    () => { });
            }

            // Merge results...
            foreach (var result in results) {
                discovered.AddOrUpdate(result.ToServiceModel(
                    address?.ToString(), SiteId, SupervisorId));
            }

            // Check results
            if (discovered.Count == 0) {
                throw new ResourceNotFoundException("Unable to find applications");
            }
            return discovered;
        }

        /// <summary>
        /// Get a reachable host address from url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        protected virtual async Task<IPAddress> GetHostAddressAsync(Uri discoveryUrl) {
            try {
                var entry = await Dns.GetHostEntryAsync(discoveryUrl.DnsSafeHost);
                foreach (var address in entry.AddressList) {
                    var reply = await new Ping().SendPingAsync(address);
                    if (reply.Status == IPStatus.Success) {
                        return address;
                    }
                }
                return null;
            }
            catch {
                return null;
            }
        }

        protected readonly IOpcUaClient _client;
        protected readonly ILogger _logger;
    }
}
