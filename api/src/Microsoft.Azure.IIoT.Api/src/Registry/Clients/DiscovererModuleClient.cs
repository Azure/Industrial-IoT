// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Implements discovery through discovery module
    /// </summary>
    public sealed class DiscovererModuleClient : IDiscovererClient {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public DiscovererModuleClient(IMethodClient client, IJsonSerializer serializer,
            ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inhertitdoc/>
        public async Task DiscoverAsync(string discovererId,
            DiscoveryRequestModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await CallServiceOnDiscovererAsync(discovererId, "Discover_V2", request, ct);
        }

        /// <inhertitdoc/>
        public async Task CancelAsync(string discovererId,
            DiscoveryCancelModel request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await CallServiceOnDiscovererAsync(discovererId, "Cancel_V2", request, ct);
        }

        /// <summary>
        /// helper to invoke service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="discovererId"></param>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task CallServiceOnDiscovererAsync<T>(string discovererId,
            string service, T request, CancellationToken ct) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            var sw = Stopwatch.StartNew();
            var deviceId = DiscovererModelEx.ParseDeviceId(discovererId,
                out var moduleId);
            var result = await _client.CallMethodAsync(deviceId, moduleId, service,
                _serializer.SerializeToString(request), null, ct);
            _logger.Debug("Calling discoverer service '{service}' on " +
                "{deviceId}/{moduleId} took {elapsed} ms.", service,
                deviceId, moduleId, sw.ElapsedMilliseconds);
        }

        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _client;
        private readonly ILogger _logger;
    }
}
