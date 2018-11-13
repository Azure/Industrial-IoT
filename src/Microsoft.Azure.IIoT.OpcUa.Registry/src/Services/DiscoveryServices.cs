// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Uses the registry identities to invoke the discovery request
    /// on all members
    /// </summary>
    public sealed class DiscoveryServices : IDiscoveryServices, IDisposable {

        private static readonly TimeSpan kSupervisorRefreshTimer =
            TimeSpan.FromMinutes(1);
        private static readonly TimeSpan kSupervisorErrorTimer =
            TimeSpan.FromSeconds(10);

        /// <summary>
        /// Create cloud side discovery services
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public DiscoveryServices(ISupervisorRegistry registry,
            IDiscoveryClient client, ILogger logger) {

            _registry = registry ??
                throw new ArgumentNullException(nameof(registry));
            _client = client ??
                throw new ArgumentNullException(nameof(client));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            _supervisors = new List<string>();
            _timer = new Timer(_ => Task.Run(OnTimerFiredAsync), null, 0, -1);
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var supervisors = _supervisors;
            foreach (var id in supervisors) {
                try {
                    await _client.DiscoverAsync(id, request);
                }
                catch (Exception ex) {
                    _logger.Debug($"Failed to call discover on {id}. Continue...",
                        () => ex);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() => _timer.Dispose();

        /// <summary>
        /// Called when timer fired
        /// </summary>
        /// <returns></returns>
        private async Task OnTimerFiredAsync() {
            try {
                _supervisors = await _registry.ListAllSupervisorIdsAsync();
                try {
                    _timer.Change((int)kSupervisorRefreshTimer.TotalMilliseconds, 0);
                }
                catch (ObjectDisposedException) {
                    // object disposed
                }
            }
            catch (Exception ex) {
                _logger.Error("Failed to refresh supervisor list, try again...",
                    () => ex);
                try {
                    _timer.Change((int)kSupervisorErrorTimer.TotalMilliseconds, 0);
                }
                catch (ObjectDisposedException) {
                    // object disposed
                }
            }
        }

        private List<string> _supervisors;
        private readonly Timer _timer;

        private readonly ILogger _logger;
        private readonly ISupervisorRegistry _registry;
        private readonly IDiscoveryClient _client;
    }
}
