// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Text;

    public class TestChunkServer : IJsonMethodClient, IMethodHandler {

        public TestChunkServer(IJsonSerializer serializer,
            int size, Func<string, byte[], string, byte[]> handler) {
            MaxMethodPayloadCharacterCount = size;
            _handler = handler;
            _serializer = serializer;
            _server = new ChunkMethodServer(_serializer, TraceLogger.Create());
        }

        public IMethodClient CreateClient() {
            return new ChunkMethodClient(this, _serializer, TraceLogger.Create());
        }

        public int MaxMethodPayloadCharacterCount { get; }

        public async Task<string> CallMethodAsync(string deviceId,
            string moduleId, string method, string json, TimeSpan? timeout,
            CancellationToken ct) {
            var payload = Encoding.UTF8.GetBytes(json);
            var processed = await _server.InvokeAsync(payload,
                ContentMimeType.Json, this);
            return Encoding.UTF8.GetString(processed);
        }

        public Task<byte[]> InvokeAsync(string method, byte[] payload, string contentType) {
            return Task.FromResult(_handler.Invoke(method, payload, contentType));
        }

        private readonly ChunkMethodServer _server;
        private readonly IJsonSerializer _serializer;
        private readonly Func<string, byte[], string, byte[]> _handler;
    }
}
