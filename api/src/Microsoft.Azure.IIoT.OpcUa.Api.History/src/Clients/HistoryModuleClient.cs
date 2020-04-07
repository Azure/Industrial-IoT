// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implementation of supervisor module history api.
    /// </summary>
    public sealed class HistoryModuleClient : IHistoryModuleApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="serializer"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public HistoryModuleClient(IMethodClient methodClient,
            string deviceId, string moduleId, ISerializer serializer = null) {
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _methodClient = methodClient ?? throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public HistoryModuleClient(IMethodClient methodClient, IHistoryModuleConfig config,
            ISerializer serializer) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer) {
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<VariantValue>> HistoryReadRawAsync(
            EndpointApiModel endpoint, HistoryReadRequestApiModel<VariantValue> request,
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
                "HistoryRead_V2", _serializer.SerializeToString(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<HistoryReadResponseApiModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<VariantValue>> HistoryReadRawNextAsync(
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
                "HistoryReadNext_V2", _serializer.SerializeToString(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<HistoryReadNextResponseApiModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            EndpointApiModel endpoint, HistoryUpdateRequestApiModel<VariantValue> request,
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
                "HistoryUpdate_V2", _serializer.SerializeToString(new {
                    endpoint,
                    request
                }), null, ct);
            return _serializer.Deserialize<HistoryUpdateResponseApiModel>(response);
        }

        private readonly ISerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
