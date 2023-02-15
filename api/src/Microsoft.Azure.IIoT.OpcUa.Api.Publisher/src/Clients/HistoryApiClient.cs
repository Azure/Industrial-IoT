// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of supervisor module history api.
    /// </summary>
    public sealed class HistoryApiClient : IHistoryModuleApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="serializer"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public HistoryApiClient(IMethodClient methodClient, string deviceId,
            string moduleId = null, ISerializer serializer = null) {
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _methodClient = methodClient ?? throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId;
            _deviceId = deviceId;
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public HistoryApiClient(IMethodClient methodClient, IPublisherModuleConfig config = null,
            ISerializer serializer = null) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer) {
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseApiModel<VariantValue>> HistoryReadRawAsync(
            ConnectionApiModel connection, HistoryReadRequestApiModel<VariantValue> request,
            CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryRead_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct);
            return _serializer.Deserialize<HistoryReadResponseApiModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseApiModel<VariantValue>> HistoryReadRawNextAsync(
            ConnectionApiModel connection, HistoryReadNextRequestApiModel request,
            CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.ContinuationToken)) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadNext_V2", _serializer.SerializeToString(new {
                    connection,
                    request
                }), null, ct);
            return _serializer.Deserialize<HistoryReadNextResponseApiModel<VariantValue>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            ConnectionApiModel connection, HistoryUpdateRequestApiModel<VariantValue> request,
            CancellationToken ct) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null) {
                throw new ArgumentNullException(nameof(request.Details));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryUpdate_V2", _serializer.SerializeToString(new {
                    connection,
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
