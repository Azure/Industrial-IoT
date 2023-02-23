// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module
{
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module.Default;
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class TestChunkServer : IJsonMethodClient, IMethodHandler
    {
        public TestChunkServer(IJsonSerializer serializer,
            int size, Func<string, byte[], string, byte[]> handler)
        {
            MaxMethodPayloadCharacterCount = size;
            _handler = handler;
            _serializer = serializer;
            _server = new ChunkMethodServer(_serializer, Log.Console<ChunkMethodServer>());
        }

        public IMethodClient CreateClient()
        {
            return new ChunkMethodClient(this, _serializer, Log.Console<ChunkMethodClient>());
        }

        public int MaxMethodPayloadCharacterCount { get; }

        public async Task<string> CallMethodAsync(string deviceId,
            string moduleId, string method, string json, TimeSpan? timeout,
            CancellationToken ct)
        {
            var payload = Encoding.UTF8.GetBytes(json);
            var processed = await _server.InvokeAsync(payload,
                ContentMimeType.Json, this).ConfigureAwait(false);
            return Encoding.UTF8.GetString(processed);
        }

        public Task<byte[]> InvokeAsync(string method, byte[] payload, string contentType)
        {
            return Task.FromResult(_handler.Invoke(method, payload, contentType));
        }

        private readonly ChunkMethodServer _server;
        private readonly IJsonSerializer _serializer;
        private readonly Func<string, byte[], string, byte[]> _handler;
    }
}
