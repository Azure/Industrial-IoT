// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Http;
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
        /// <param name="client"></param>
        /// <param name="http"></param>
        /// <param name="logger"></param>
        public HttpTunnelServer(IMethodClient client, IHttpClient http, ILogger logger) {
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

            if (!properties.TryGetValue("content-type", out var type) &&
                !properties.TryGetValue("iothub-content-type", out type)) {
                throw new ArgumentException("Missing content type in event.");
            }

            // Get message id and correlation id from content type
            var typeParsed = type.Split("_", StringSplitOptions.RemoveEmptyEntries);
            if (typeParsed.Length != 2 ||
                !int.TryParse(typeParsed[1], out var messageId)) {
                return;
            }
            var requestId = typeParsed[0];

            HttpRequestProcessor processor;
            if (messageId == 0) {
                var request = JsonConvertEx.DeserializeObject<HttpTunnelRequestModel>(
                    Encoding.UTF8.GetString(payload));
                processor = new HttpRequestProcessor(this, deviceId, moduleId,
                    requestId, request, null);
                if (request.Chunks != 0) {
                    if (!_requests.TryAdd(requestId, processor)) {
                        throw new InvalidOperationException(
                            $"Adding request {requestId} failed.");
                    }

                    // Need more
                    return;
                }
                // Complete request
            }
            else if (_requests.TryGetValue(requestId, out processor)) {
                if (!processor.AddChunk(messageId, payload)) {

                    // Need more
                    return;
                }
                // Complete request
                _requests.TryRemove(requestId, out _);
            }
            else {
                // Timed out or expired
                _logger.Debug("Request from {deviceId} {moduleId} " +
                    "with id {requestId} timed out - give up.",
                    deviceId, moduleId, requestId);
                return;
            }

            // Complete request
            try {
                await processor.CompleteAsync();
                await Try.Async(() => checkpoint?.Invoke());
            }
            catch (Exception ex) {
                _logger.Error(ex,
                    "Failed to complete request from {deviceId} {moduleId} " +
                    "with id {requestId} - giving up.",
                    deviceId, moduleId, requestId);
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
            /// <param name="timeout"></param>
            public HttpRequestProcessor(HttpTunnelServer outer, string deviceId,
                string moduleId, string requestId, HttpTunnelRequestModel request,
                TimeSpan? timeout) {
                RequestId = requestId ??
                    throw new ArgumentNullException(nameof(requestId));
                _outer = outer ??
                    throw new ArgumentNullException(nameof(outer));
                _deviceId = deviceId;
                _moduleId = moduleId;
                _timeout = timeout ?? TimeSpan.FromSeconds(20);
                _request = request;
                _payload = new List<byte[]>(request.Chunks);
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
                    if (_payload.Count > 0) {
                        using (var stream = new MemoryStream()) {
                            foreach (var chunk in _payload) {
                                stream.Write(chunk);
                            }
                            var payload = stream.ToArray().Unzip();
                            request.Content = new ByteArrayContent(payload);
                        }
                    }

                    // Add headers
                    if (_request.Headers != null) {
                        foreach (var header in _request.Headers) {
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
                        JsonConvertEx.SerializeObject(new HttpTunnelResponseModel {
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
                        JsonConvertEx.SerializeObject(new HttpTunnelResponseModel {
                            RequestId = RequestId,
                            Status = (int)HttpStatusCode.InternalServerError,
                            Payload = Encoding.UTF8.GetBytes(
                                JsonConvertEx.SerializeObject(ex))
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
                if (id < 1 || id > _request.Chunks) {
                    return false;
                }
                _payload.Insert(id - 1, payload);
                _lastActivity = DateTime.UtcNow;
                if (_payload.Count != _request.Chunks || _payload.Any(p => p == null)) {
                    return false;
                }
                return true;
            }

            private readonly HttpTunnelServer _outer;
            private readonly string _deviceId;
            private readonly string _moduleId;
            private readonly TimeSpan _timeout;
            private readonly HttpTunnelRequestModel _request;
            private readonly List<byte[]> _payload;
            private DateTime _lastActivity;
        }

        private const int kTimeoutCheckInterval = 10000;
        private readonly ConcurrentDictionary<string, HttpRequestProcessor> _requests;
        private readonly Timer _timer;
        private readonly IMethodClient _client;
        private readonly IHttpClient _http;
        private readonly ILogger _logger;
    }
}
