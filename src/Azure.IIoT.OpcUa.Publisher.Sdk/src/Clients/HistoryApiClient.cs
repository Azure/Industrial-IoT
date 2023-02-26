// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of history api.
    /// </summary>
    public sealed class HistoryApiClient : IHistoryApi
    {
        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="serializer"></param>
        public HistoryApiClient(IMethodClient methodClient, string deviceId,
            string moduleId = null, ISerializer serializer = null)
        {
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
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
        public HistoryApiClient(IMethodClient methodClient, ISdkConfig config = null,
            ISerializer serializer = null) :
            this(methodClient, config?.DeviceId, config?.ModuleId, serializer)
        {
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadValues_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadModifiedValues_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadValuesAtTimes_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadProcessedValues_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadValuesNext_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadNextResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReplaceValues_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryInsertValues_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryUpsertValues_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryDeleteValues_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryDeleteModifiedValues_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryDeleteValuesAtTimes_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadEvents_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadResponseModel<HistoricEventModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReadEventsNext_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryReadNextResponseModel<HistoricEventModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryReplaceEvents_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryInsertEvents_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryUpsertEvents_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "HistoryDeleteEvents_V2", _serializer.SerializeToString(new
                {
                    connection,
                    request
                }), null, ct).ConfigureAwait(false);
            return _serializer.Deserialize<HistoryUpdateResponseModel>(response);
        }

        private readonly ISerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
