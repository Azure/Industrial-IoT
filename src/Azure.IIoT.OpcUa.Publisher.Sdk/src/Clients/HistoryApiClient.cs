// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Tunnel;
    using Microsoft.Extensions.Options;
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
        /// <param name="target"></param>
        /// <param name="timeout"></param>
        /// <param name="serializer"></param>
        public HistoryApiClient(IMethodClient methodClient, string target,
             TimeSpan? timeout = null, IJsonSerializer? serializer = null)
        {
            _serializer = serializer ??
                new NewtonsoftJsonSerializer();
            _methodClient = methodClient ??
                throw new ArgumentNullException(nameof(methodClient));
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }
            _target = target;
            _timeout = timeout ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public HistoryApiClient(IMethodClient methodClient,
            IOptions<SdkOptions> options, IJsonSerializer? serializer = null) :
            this(methodClient, options.Value.Target!, options.Value.Timeout,
                serializer)
        {
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadValuesDetailsModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadValues_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadModifiedValues_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadValuesAtTimes_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadProcessedValues_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadValuesNext_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadNextResponseModel<HistoricValueModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReplaceValues_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryInsertValues_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryUpsertValues_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryDeleteValues_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<DeleteValuesDetailsModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryDeleteModifiedValues_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            ConnectionModel connection, HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryDeleteValuesAtTimes_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadEventsDetailsModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadEvents_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadResponseModel<HistoricEventModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReadEventsNext_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryReadNextResponseModel<HistoricEventModel[]>>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryReplaceEvents_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryInsertEvents_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryUpsertEvents_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        /// <inheritdoc/>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint Url missing.", nameof(connection));
            }
            ArgumentNullException.ThrowIfNull(request);
            if (request.Details == null)
            {
                throw new ArgumentException("Details missing.", nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_target,
                "HistoryDeleteEvents_V2", _serializer.SerializeToMemory(new
                {
                    connection,
                    request
                }), ContentMimeType.Json, _timeout, ct).ConfigureAwait(false);
            return _serializer.DeserializeResponse<HistoryUpdateResponseModel>(response);
        }

        private readonly ISerializer _serializer;
        private readonly IMethodClient _methodClient;
        private readonly string _target;
        private readonly TimeSpan _timeout;
    }
}
