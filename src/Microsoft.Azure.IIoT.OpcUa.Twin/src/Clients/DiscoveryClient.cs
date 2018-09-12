// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
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
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public DiscoveryClient(IIoTHubTwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
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
            await CallServiceOnSupervisor(supervisorId, "Discover_V1", request);
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
            var result = await _twin.CallMethodAsync(deviceId, moduleId,
                new MethodParameterModel {
                    Name = service,
                    JsonPayload = JsonConvertEx.SerializeObject(request)
                });
            _logger.Debug($"Calling supervisor service '{service}' on {deviceId}/{moduleId} " +
                $"took {sw.ElapsedMilliseconds} ms and returned {result.Status}!",
                    () => { });
            if (result.Status != 200) {
                throw new MethodCallStatusException(result.Status, result.JsonPayload);
            }
        }

        private readonly IIoTHubTwinServices _twin;
        private readonly ILogger _logger;
    }
}
