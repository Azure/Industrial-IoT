// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Implementation of supervisor module history api.
    /// </summary>
    public sealed class HistoryModuleClient : IHistoryModuleApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public HistoryModuleClient(IMethodClient methodClient, string deviceId, string moduleId) {
            _methodClient = methodClient ?? throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        public HistoryModuleClient(IMethodClient methodClient, IHistoryModuleConfig config) :
            this(methodClient, config?.DeviceId, config?.ModuleId) {
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<JToken>> HistoryReadRawAsync(
            EndpointApiModel endpoint, HistoryReadRequestApiModel<JToken> request,
            CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryRead_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<HistoryReadResponseApiModel<JToken>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<JToken>> HistoryReadRawNextAsync(
            EndpointApiModel endpoint, HistoryReadNextRequestApiModel request,
            CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadNext_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<HistoryReadNextResponseApiModel<JToken>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            EndpointApiModel endpoint, HistoryUpdateRequestApiModel<JToken> request,
            CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryUpdate_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<HistoryUpdateResponseApiModel>(response);
        }

        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
