// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Http.Default;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
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
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public HttpTunnelHandlerFactory(IEventClient client,
            IEnumerable<IHttpHandler> handlers, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? new List<IHttpHandler>();
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _outstanding = new ConcurrentDictionary<string,
                TaskCompletionSource<HttpResponseMessage>>();
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
        public Task<byte[]> InvokeAsync(byte[] payload, string contentType) {
            // Handle response from device method
            var result = Encoding.UTF8.GetString(payload);
            var response = JsonConvertEx.DeserializeObject<HttpTunnelResponseModel>(result);
            if (_outstanding.TryRemove(response.RequestId, out var tcs)) {
                var httpResponse = new HttpResponseMessage((HttpStatusCode)response.Status) {
                    Content = response.Payload == null ? null :
                        new ByteArrayContent(response.Payload)
                };
                if (response.Headers != null) {
                    foreach (var header in response.Headers) {
                        httpResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                tcs.TrySetResult(httpResponse);
            }
            return Task.FromResult(new byte[0]);
        }

        /// <summary>
        /// Http client handler for the tunnels
        /// </summary>
        private sealed class HttpTunnelClientHandler : System.Net.Http.HttpClientHandler {

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
                _maxSize = 230 * 1024; // Max event message size
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

                var headers = request.Headers?
                    .ToDictionary(h => h.Key, h => h.Value.ToList());
                var method = request.Method.ToString();

                // Create chunks
                var payload = request.Content != null ? await request.Content.ReadAsByteArrayAsync() : null;
                var chunks = new List<byte[]>();
                if (payload != null && payload.Length > 0) {
                    var buffer = payload.Zip(); // Gzip payload
                    for (var offset = 0; offset < buffer.Length; offset += _maxSize) {
                        var length = Math.Min(buffer.Length - offset, _maxSize);
                        var chunk = buffer.AsSpan(offset, length).ToArray();
                        chunks.Add(chunk);
                    }
                }

                var requestId = Guid.NewGuid().ToString();
                var tcs = new TaskCompletionSource<HttpResponseMessage>();

                if (!_outer._outstanding.TryAdd(requestId, tcs)) {
                    throw new InvalidOperationException("Could not add completion.");
                }

                // Remove on cancellation
                ct.Register(() => {
                    _outer._outstanding.TryRemove(requestId, out _);
                    tcs.TrySetCanceled();
                });

                // Send headers
                var messageId = 0;
                await _outer._client.SendEventAsync(Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(new HttpTunnelRequestModel {
                        ResourceId = null, // TODO
                    Uri = request.RequestUri.ToString(),
                        Headers = headers,
                        Chunks = chunks.Count,
                        Method = method
                    })), requestId + "_" + messageId.ToString(),
                        HttpTunnelRequestModel.SchemaName, ContentMimeType.Json);

                // Send payload chunks
                foreach (var chunk in chunks) {
                    ++messageId;
                    await _outer._client.SendEventAsync(chunk,
                        requestId + "_" + messageId.ToString(),
                        HttpTunnelRequestModel.SchemaName, ContentMimeType.Binary);
                }

                // Wait for completion
                return await tcs.Task;
            }

            private readonly int _maxSize;
            private readonly HttpTunnelHandlerFactory _outer;
        }

        private readonly List<IHttpHandler> _handlers;
        private readonly IEventClient _client;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string,
            TaskCompletionSource<HttpResponseMessage>> _outstanding;
    }
}
