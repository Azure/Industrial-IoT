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
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Provides a http message handler using events and methods as tunnel
    /// Wrap the receive end into a chunk message server.
    /// </summary>
    public sealed class HttpTunnelClientHandler : HttpClientHandler, IMethodHandler {

        /// <summary>
        /// Create client wrapping a json method client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public HttpTunnelClientHandler(IEventClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxSize = 230 * 1024; // Max event message size
            _outstanding = new ConcurrentDictionary<string,
                TaskCompletionSource<HttpResponseMessage>>();
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) {

            var headers = request.Headers
                .ToDictionary(h => h.Key, h => h.Value.ToList());
            var method = request.Method.ToString();

            // Create chunks
            var payload = await request.Content.ReadAsByteArrayAsync();
            var chunks = new List<byte[]>();
            if (payload.Length > 0) {
                var buffer = payload.Zip(); // Gzip payload
                for (var offset = 0; offset < buffer.Length; offset += _maxSize) {
                    var length = Math.Min(buffer.Length - offset, _maxSize);
                    var chunk = buffer.AsSpan(offset, length).ToArray();
                    chunks.Add(chunk);
                }
            }

            var requestId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            // Remove on cancellation
            ct.Register(() => {
                _outstanding.TryRemove(requestId, out var completion);
                System.Diagnostics.Debug.Assert(tcs == completion);
                completion.TrySetCanceled();
            });
            if (!_outstanding.TryAdd(requestId, tcs)) {
                throw new InvalidOperationException("Could not add completion.");
            }

            // Send headers
            var messageId = 0;
            await _client.SendEventAsync(Encoding.UTF8.GetBytes(
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
                await _client.SendEventAsync(chunk,
                    requestId + "_" + messageId.ToString(),
                    HttpTunnelRequestModel.SchemaName, ContentMimeType.Binary);
            }
            return await tcs.Task;
        }

        /// <inheritdoc/>
        public Task<byte[]> InvokeAsync(string method, byte[] payload,
            string contentType) {

            // Handle response from device method
            var result = Encoding.UTF8.GetString(payload);
            var response = JsonConvertEx.DeserializeObject<HttpTunnelResponseModel>(result);
            if (_outstanding.TryRemove(response.RequestId, out var tcs)) {
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
