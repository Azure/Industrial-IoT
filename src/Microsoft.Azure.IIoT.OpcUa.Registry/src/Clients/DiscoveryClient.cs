// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Implements discovery through twin supervisor
    /// </summary>
    public sealed class DiscoveryClient : IDiscoveryClient {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public DiscoveryClient(IMethodClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Kick off discovery
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task DiscoverAsync(string supervisorId,
            DiscoveryRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await CallServiceOnSupervisor(supervisorId, "Discover_V2", request);
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="supervisorId"></param>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task CallServiceOnSupervisor<T>(string supervisorId,
            string service, T request) {
            if (string.IsNullOrEmpty(supervisorId)) {
                throw new ArgumentNullException(nameof(supervisorId));
            }
            var sw = Stopwatch.StartNew();
            var deviceId = SupervisorModelEx.ParseDeviceId(supervisorId,
                out var moduleId);
            var result = await _client.CallMethodAsync(deviceId, moduleId, service,
                JsonConvertEx.SerializeObject(request));
            _logger.Debug("Calling supervisor service '{service}' on " +
                "{deviceId}/{moduleId} took {elapsed} ms.", service,
                deviceId, moduleId, sw.ElapsedMilliseconds);
        }

        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
