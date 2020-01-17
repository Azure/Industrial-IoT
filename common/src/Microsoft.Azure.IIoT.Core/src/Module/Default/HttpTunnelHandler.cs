// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Hub;
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

    /// <summary>
    /// Provides a http message handler using events and methods as tunnel
    /// Wrap the receive end into a chunk message server.
    /// </summary>
    public sealed class HttpTunnelHandler : HttpClientHandler, IMethodHandler {

        /// <summary>
        /// Create client wrapping a json method client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public HttpTunnelHandler(IEventClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxSize = 256 * 1024; // Max event message size
            _outstanding = new ConcurrentDictionary<string,
                TaskCompletionSource<HttpResponseMessage>>();
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) {

            var payload = await request.Content.ReadAsByteArrayAsync();
            var headers = request.Headers
                .ToDictionary(h => h.Key, h => h.Value.ToList());
            var method = request.Method.ToString();
            var buffer = payload.Zip(); // Gzip payload

            var correlationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            // Remove on cancellation
            ct.Register(() => {
                _outstanding.TryRemove(correlationId, out var completion);
                System.Diagnostics.Debug.Assert(tcs == completion);
                completion.TrySetCanceled();
            });
            if (!_outstanding.TryAdd(correlationId, tcs)) {
                throw new InvalidOperationException("Could not add completion.");
            }

            // Send headers
            await _client.SendEventAsync(Encoding.UTF8.GetBytes(
                JsonConvertEx.SerializeObject(new HttpTunnelMessageModel {
                    Headers = headers,
                    Method = method,
                    CorrelationId = correlationId
                })), "content", "schema", "application/json");

            // Send payload chunks
            var eventId = 0;
            for (var offset = 0; offset < buffer.Length; offset += _maxSize) {
                var length = Math.Min(buffer.Length - offset, _maxSize);
                var chunk = buffer.AsSpan(offset, length).ToArray();
                ++eventId;
                await _client.SendEventAsync(chunk, "content",
                    correlationId + "_" + eventId.ToString(), "gzip");
            }
            return await tcs.Task;
        }

        /// <inheritdoc/>
        public Task<byte[]> InvokeAsync(string method, byte[] payload,
            string contentType) {

            // Handle response from device method
            var result = Encoding.UTF8.GetString(payload);
            var response = JsonConvertEx.DeserializeObject<HttpTunnelMessageModel>(result);
            if (_outstanding.TryRemove(response.CorrelationId, out var tcs)) {
                var httpResponse = new HttpResponseMessage((HttpStatusCode)response.Status) {
                    Content = new ByteArrayContent(response.Payload)
                };
                foreach (var header in response.Headers) {
                    httpResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                tcs.TrySetResult(httpResponse);
            }
            return Task.FromResult(new byte[0]);
        }

        private readonly IEventClient _client;
        private readonly ILogger _logger;
        private readonly int _maxSize;
        private readonly ConcurrentDictionary<string,
            TaskCompletionSource<HttpResponseMessage>> _outstanding;
    }
}
