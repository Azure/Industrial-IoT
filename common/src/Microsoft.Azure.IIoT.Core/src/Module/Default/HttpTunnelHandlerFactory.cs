// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Provides a http handler factory using events and methods as tunnel.
    /// This is for the module side to call cloud endpoints tunneled through
    /// multiple hops.
    /// Register on top of the HttpClientModule to use with injected
    /// <see cref="IHttpClient"/>.
    /// </summary>
    public sealed class HttpTunnelHandlerFactory : IMethodInvoker, IHttpHandlerFactory {

        /// <inheritdoc/>
        public string MethodName => MethodNames.Response;

        /// <summary>
        /// Create handler factory
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public HttpTunnelHandlerFactory(IEventClient client, IJsonSerializer serializer,
            IEnumerable<IHttpHandler> handlers, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _handlers = handlers?.ToList() ?? new List<IHttpHandler>();
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _outstanding = new ConcurrentDictionary<string, RequestTask>();
        }

        /// <inheritdoc/>
        public void Dispose() {
            // noop
        }

        /// <inheritdoc/>
        public TimeSpan Create(string name, out HttpMessageHandler handler) {
            var resource = name == HttpHandlerFactory.DefaultResourceId ? null : name;
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var del = new HttpHandlerDelegate(new HttpTunnelClientHandler(this),
#pragma warning restore IDE0067 // Dispose objects before losing scope
                resource, _handlers.Where(h => h.IsFor?.Invoke(resource) ?? true),
                null, _logger);
            handler = del;
            return del.MaxLifetime;
        }

        /// <inheritdoc/>
        public Task<byte[]> InvokeAsync(byte[] payload, string contentType,
            IMethodHandler context) {
            // Handle response from device method
            var response = _serializer.Deserialize<HttpTunnelResponseModel>(payload);
            if (_outstanding.TryRemove(response.RequestId, out var request)) {
                var httpResponse = new HttpResponseMessage((HttpStatusCode)response.Status) {
                    Content = response.Payload == null ? null :
                        new ByteArrayContent(response.Payload)
                };
                if (response.Headers != null) {
                    foreach (var header in response.Headers) {
                        httpResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                request.Completion.TrySetResult(httpResponse);
                request.Dispose();
            }
            return Task.FromResult(new byte[0]);
        }

        /// <summary>
        /// Http client handler for the tunnels
        /// </summary>
        private sealed class HttpTunnelClientHandler : Http.Default.HttpClientHandler {

            /// <inheritdoc/>
            public override bool SupportsAutomaticDecompression => true;

            /// <inheritdoc/>
            public override bool SupportsProxy => false;

            /// <inheritdoc/>
            public override bool SupportsRedirectConfiguration => false;

            /// <summary>
            /// Create handler
            /// </summary>
            /// <param name="outer"></param>
            public HttpTunnelClientHandler(HttpTunnelHandlerFactory outer) {
                // 256 KB - Max event message size - leave 4 kb for properties
                _maxSize = 252 * 1024;
                _outer = outer;
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing) {
                // TODO: Investigate to remove all outstanding requests on the handler
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken ct) {
                if (request.Headers.TryGetValues(HttpHeader.UdsPath, out var paths)) {
                    // On edge we must still support unix sockets to talk to edgelet
                    return await base.SendAsync(request, ct);
                }

                // Create tunnel request
                var tunnelRequest = new HttpTunnelRequestModel {
                    ResourceId = null, // TODO
                    Uri = request.RequestUri.ToString(),
                    RequestHeaders = request.Headers?
                        .ToDictionary(h => h.Key, h => h.Value.ToList()),
                    Method = request.Method.ToString()
                };

                // Get content
                byte[] payload = null;
                if (request.Content != null) {
                    payload = await request.Content.ReadAsByteArrayAsync();
                    payload = payload.Zip();

                    tunnelRequest.ContentHeaders = request.Content.Headers?
                        .ToDictionary(h => h.Key, h => h.Value.ToList());
                }

                // Serialize
                var buffers = SerializeRequest(tunnelRequest, payload);

                var requestId = Guid.NewGuid().ToString();
                var requestTask = new RequestTask(kDefaultTimeout, ct);
                if (!_outer._outstanding.TryAdd(requestId, requestTask)) {
                    throw new InvalidOperationException("Could not add completion.");
                }

                // Send events
                for (var messageId = 0; messageId < buffers.Count; messageId++) {
                    await _outer._client.SendEventAsync(buffers[messageId],
                        requestId + "_" + messageId.ToString(),
                        HttpTunnelRequestModel.SchemaName, ContentMimeType.Binary);
                }

                // Wait for completion
                try {
                    return await requestTask.Completion.Task;
                }
                catch {
                    // If thrown remove and dispose first
                    if (_outer._outstanding.TryRemove(requestId, out requestTask)) {
                        requestTask.Dispose();
                    }
                    throw;
                }
            }

            /// <summary>
            /// Serialize request into buffer chunks
            /// </summary>
            /// <param name="tunnelRequest"></param>
            /// <param name="payload"></param>
            /// <returns></returns>
            private List<byte[]> SerializeRequest(HttpTunnelRequestModel tunnelRequest, byte[] payload) {
                // Serialize data
                var buffers = new List<byte[]>();
                var remainingRoom = 0;
                using (var header = new MemoryStream())
                using (var writer = new BinaryWriter(header)) {
                    // Serialize header (0)
                    var headerBuffer =
                        _outer._serializer.SerializeToBytes(tunnelRequest).ToArray().Zip();

                    writer.Write(headerBuffer.Length);
                    writer.Write(headerBuffer);

                    // Assume chunk size and payload size also written
                    remainingRoom = _maxSize - (int)(header.Position + 8);
                    if (remainingRoom < 0) {
                        throw new ArgumentException("Header too large to sent");
                    }

                    // Create chunks from payload
                    if (payload != null && payload.Length > 0) {
                        // Fill remaining room with payload
                        remainingRoom = Math.Min(remainingRoom, payload.Length);
                        writer.Write(remainingRoom);
                        writer.Write(payload, 0, remainingRoom);

                        // Create remaining chunks
                        for (; remainingRoom < payload.Length; remainingRoom += _maxSize) {
                            var length = Math.Min(payload.Length - remainingRoom, _maxSize);
                            var chunk = payload.AsSpan(remainingRoom, length).ToArray();
                            buffers.Add(chunk);
                        }
                        writer.Write(buffers.Count);
                    }
                    else {
                        writer.Write(0);
                        writer.Write(0);
                    }
                    // Insert header as first buffer
                    buffers.Insert(0, header.ToArray());
                }
                return buffers;
            }

            private readonly int _maxSize;
            private readonly HttpTunnelHandlerFactory _outer;
        }

        /// <summary>
        /// Request tasks
        /// </summary>
        private class RequestTask : IDisposable {

            /// <summary>
            /// Outstanding task
            /// </summary>
            public TaskCompletionSource<HttpResponseMessage> Completion { get; }
                = new TaskCompletionSource<HttpResponseMessage>();

            /// <summary>
            /// Create task
            /// </summary>
            /// <param name="timeout"></param>
            /// <param name="ct"></param>
            public RequestTask(TimeSpan timeout, CancellationToken ct) {
                _timeout = new CancellationTokenSource(timeout);
                ct.Register(() => _timeout.Cancel());
                // Register timeout handler
                _timeout.Token.Register(() => {
                    if (ct.IsCancellationRequested) {
                        Completion.TrySetCanceled();
                    }
                    else {
                        Completion.TrySetException(
                            new TimeoutException("Request timed out"));
                    }
                });
            }

            /// <inheritdoc/>
            public void Dispose() {
                _timeout.Dispose();
            }

            private readonly CancellationTokenSource _timeout;
        }

        private static readonly TimeSpan kDefaultTimeout = TimeSpan.FromMinutes(5);
        private readonly List<IHttpHandler> _handlers;
        private readonly IEventClient _client;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly ConcurrentDictionary<string, RequestTask> _outstanding;
    }
}
