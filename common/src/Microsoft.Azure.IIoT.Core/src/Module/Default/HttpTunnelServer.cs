// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Net.Http;

    /// <summary>
    /// Provides server side handling
    /// </summary>
    public sealed class HttpTunnelServer : IDeviceTelemetryHandler, IDisposable {

        /// <inheritdoc/>
        public string MessageSchema => HttpTunnelRequestModel.SchemaName;

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="http"></param>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public HttpTunnelServer(IHttpClient http, IMethodClient client,
            IJsonSerializer serializer, ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _requests = new ConcurrentDictionary<string, HttpRequestProcessor>();
            _timer = new Timer(_ => OnTimer(), null,
                kTimeoutCheckInterval, kTimeoutCheckInterval);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _timer.Dispose();
            _requests.Clear();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId, byte[] payload,
            IDictionary<string, string> properties, Func<Task> checkpoint) {
            var completed = await HandleEventAsync(deviceId, moduleId,
                payload, properties);
            if (completed) {
                await Try.Async(() => checkpoint?.Invoke());
            }
        }

        /// <inheritdoc/>
        public async Task<bool> HandleEventAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties) {

            if (!properties.TryGetValue("content-type", out var type) &&
                !properties.TryGetValue("iothub-content-type", out type)) {
                _logger.Error(
                    "Missing content type in tunnel event from {deviceId} {moduleId}.",
                    deviceId, moduleId);
                return true;
            }

            // Get message id and correlation id from content type
            var typeParsed = type.Split("_", StringSplitOptions.RemoveEmptyEntries);
            if (typeParsed.Length != 2 ||
                !int.TryParse(typeParsed[1], out var messageId)) {
                _logger.Error("Bad content type {contentType} in tunnel event" +
                    " from {deviceId} {moduleId}.", type, deviceId, moduleId);
                return true;
            }
            var requestId = typeParsed[0];

            HttpRequestProcessor processor;
            if (messageId == 0) {
                try {
                    var chunk0 = DeserializeRequest0(payload, out var request, out var chunks);
                    processor = new HttpRequestProcessor(this, deviceId, moduleId,
                        requestId, request, chunks, chunk0, null);
                    if (chunks != 0) { // More to follow?
                        if (!_requests.TryAdd(requestId, processor)) {
                            throw new InvalidOperationException(
                                $"Adding request {requestId} failed.");
                        }
                        // Need more
                        return false;
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to parse tunnel request from {deviceId} " +
                        "{moduleId} with id {requestId} - giving up.",
                        deviceId, moduleId, requestId);
                    return true;
                }
                // Complete request
            }
            else if (_requests.TryGetValue(requestId, out processor)) {
                if (!processor.AddChunk(messageId, payload)) {
                    // Need more
                    return false;
                }
                // Complete request
                _requests.TryRemove(requestId, out _);
            }
            else {
                // Timed out or expired
                _logger.Debug("Request from {deviceId} {moduleId} " +
                    "with id {requestId} timed out - give up.",
                    deviceId, moduleId, requestId);
                return true;
            }

            // Complete request
            try {
                await processor.CompleteAsync();
            }
            catch (Exception ex) {
                _logger.Error(ex,
                    "Failed to complete request from {deviceId} {moduleId} " +
                    "with id {requestId} - giving up.",
                    deviceId, moduleId, requestId);
            }
            return true;
        }

        /// <summary>
        /// Deserialize request number 0
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="payload"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] DeserializeRequest0(byte[] payload,
            out HttpTunnelRequestModel request, out int chunks) {
            // Deserialize data
            using (var header = new MemoryStream(payload))
            using (var reader = new BinaryReader(header)) {
                var headerLen = reader.ReadInt32();
                if (headerLen > payload.Length - 8) {
                    throw new ArgumentException("Bad encoding length");
                }
                var headerBuf = reader.ReadBytes(headerLen);
                var bufferLen = reader.ReadInt32();
                if (bufferLen > payload.Length - (headerLen + 8)) {
                    throw new ArgumentException("Bad encoding length");
                }
                var chunk0 = bufferLen > 0 ? reader.ReadBytes(bufferLen) : null;
                chunks = reader.ReadInt32();
                if (chunks > kMaxNumberOfChunks) {
                    throw new ArgumentException("Bad encoding length");
                }
                request = _serializer.Deserialize<HttpTunnelRequestModel>(
                    headerBuf.Unzip());
                return chunk0;
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Manage requests
        /// </summary>
        private void OnTimer() {
            foreach (var item in _requests.Values) {
                if (item.IsTimedOut) {
                    _requests.TryRemove(item.RequestId, out var tmp);
                }
            }
        }

        /// <summary>
        /// Processes request chunks
        /// </summary>
        private class HttpRequestProcessor {

            /// <summary>
            /// Request handle
            /// </summary>
            public string RequestId { get; }

            /// <summary>
            /// Whether the request timed out
            /// </summary>
            public bool IsTimedOut => DateTime.UtcNow > _lastActivity + _timeout;

            /// <summary>
            /// Create chunk
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="deviceId"></param>
            /// <param name="moduleId"></param>
            /// <param name="requestId"></param>
            /// <param name="request"></param>
            /// <param name="chunks"></param>
            /// <param name="chunk0"></param>
            /// <param name="timeout"></param>
            public HttpRequestProcessor(HttpTunnelServer outer, string deviceId,
                string moduleId, string requestId, HttpTunnelRequestModel request,
                int chunks, byte[] chunk0, TimeSpan? timeout) {
                RequestId = requestId ??
                    throw new ArgumentNullException(nameof(requestId));
                _outer = outer ??
                    throw new ArgumentNullException(nameof(outer));
                _deviceId = deviceId;
                _moduleId = moduleId;
                _timeout = timeout ?? TimeSpan.FromSeconds(20);
                _request = request;
                _chunks = chunks + 1;
                _payload = new byte[_chunks][];
                _payload[0] = chunk0 ?? new byte[0];
            }

            /// <summary>
            /// Perform the request
            /// </summary>
            /// <returns></returns>
            internal async Task CompleteAsync() {
                try {
                    // Create new request
                    var request = _outer._http.NewRequest(_request.Uri, _request.ResourceId);

                    // Add payload
                    byte[] payload = null;
                    if (_payload.Length > 1) {
                        // Combine chunks
                        using (var stream = new MemoryStream()) {
                            foreach (var chunk in _payload) {
                                stream.Write(chunk);
                            }
                            payload = stream.ToArray().Unzip();
                        }
                    }
                    else if (_payload.Length == 1 && _payload[0].Length > 0) {
                        payload = _payload[0].Unzip();
                    }

                    if (payload != null) {
#if LOG_PAYLOAD_STRING
                        var debug = Try.Op(() => Encoding.UTF8.GetString(payload));
                        if (!string.IsNullOrEmpty(debug)) {
                            _outer._logger.Information("{Message}", debug);
                        }
#endif
                        request.Content = new ByteArrayContent(payload);
                        // Add content headers
                        if (_request.ContentHeaders != null) {
                            foreach (var header in _request.ContentHeaders) {
                                request.Content.Headers.TryAddWithoutValidation(
                                    header.Key, header.Value);
                            }
                        }
                    }

                    // Add remaining headers
                    if (_request.RequestHeaders != null) {
                        foreach (var header in _request.RequestHeaders) {
                            request.Headers.TryAddWithoutValidation(
                                header.Key, header.Value);
                        }
                    }

                    // Perform request
                    var response = (_request.Method.ToLowerInvariant()) switch {
                        "put" => await _outer._http.PutAsync(request),
                        "get" => await _outer._http.GetAsync(request),
                        "post" => await _outer._http.PostAsync(request),
                        "delete" => await _outer._http.DeleteAsync(request),
                        "patch" => await _outer._http.PatchAsync(request),
                        "head" => await _outer._http.HeadAsync(request),
                        "options" => await _outer._http.OptionsAsync(request),
                        _ => throw new ArgumentException(_request.Method, "Bad method"),
                    };

                    // Forward response back to caller
                    await _outer._client.CallMethodAsync(
                        _deviceId, _moduleId, MethodNames.Response,
                        _outer._serializer.SerializeToString(new HttpTunnelResponseModel {
                            Headers = response.Headers?
                                .ToDictionary(h => h.Key, h => h.Value.ToList()),
                            RequestId = RequestId,
                            Status = (int)response.StatusCode,
                            Payload = response.Content
                        }));
                    return;
                }
                catch (Exception ex) {
                    // Forward failure back to caller
                    await _outer._client.CallMethodAsync(
                        _deviceId, _moduleId, MethodNames.Response,
                        _outer._serializer.SerializeToString(new HttpTunnelResponseModel {
                            RequestId = RequestId,
                            Status = (int)HttpStatusCode.InternalServerError,
                            Payload = _outer._serializer.SerializeToBytes(ex).ToArray()
                        }));
                }
            }

            /// <summary>
            /// Add payload
            /// </summary>
            /// <param name="id"></param>
            /// <param name="payload"></param>
            /// <returns></returns>
            internal bool AddChunk(int id, byte[] payload) {
                if (id < 0 || id >= _payload.Length || _payload[id] != null) {
                    return false;
                }
                _payload[id] = payload;
                _lastActivity = DateTime.UtcNow;
                if (_payload.Any(p => p == null)) {
                    return false;
                }
                return true;
            }

            private readonly HttpTunnelServer _outer;
            private readonly string _deviceId;
            private readonly string _moduleId;
            private readonly TimeSpan _timeout;
            private readonly HttpTunnelRequestModel _request;
            private readonly int _chunks;
            private readonly byte[][] _payload;
            private DateTime _lastActivity;
        }

        private const int kMaxNumberOfChunks = 1024;
        private const int kTimeoutCheckInterval = 10000;
        private readonly ConcurrentDictionary<string, HttpRequestProcessor> _requests;
        private readonly Timer _timer;
        private readonly IJsonSerializer _serializer;
        private readonly IMethodClient _client;
        private readonly IHttpClient _http;
        private readonly ILogger _logger;
    }
}
