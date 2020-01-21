// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    public class TestChunkServer : IJsonMethodClient, IMethodHandler {

        public TestChunkServer(int size,
            Func<string, byte[], string, byte[]> handler) {
            MaxMethodPayloadCharacterCount = size;
            _handler = handler;
            _server = new ChunkMethodServer(this, TraceLogger.Create());
        }

        public IMethodClient CreateClient() {
            return new ChunkMethodClient(this, TraceLogger.Create());
        }

        public int MaxMethodPayloadCharacterCount { get; }

        public async Task<string> CallMethodAsync(string deviceId,
            string moduleId, string method, string json, TimeSpan? timeout,
            CancellationToken ct) {
            var processed = await _server.ProcessAsync(
                JsonConvertEx.DeserializeObject<MethodChunkModel>(json));
            return JsonConvertEx.SerializeObject(processed);
        }

        public Task<byte[]> InvokeAsync(string method, byte[] payload, string contentType) {
            return Task.FromResult(_handler.Invoke(method, payload, contentType));
        }

        private readonly ChunkMethodServer _server;
        private readonly Func<string, byte[], string, byte[]> _handler;
    }
}
