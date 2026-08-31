// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Protocol
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Models;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ChunkMethodClientTests
    {
        [Fact]
        public void ConstructorRejectsNullDependencies()
        {
            var rpc = new ScriptedRpcClient();

            Assert.Throws<ArgumentNullException>(() =>
                new ChunkMethodClient(null!, NullLogger<ChunkMethodClient>.Instance));
            Assert.Throws<ArgumentNullException>(() =>
                new ChunkMethodClient(rpc, null!));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(100, 66)]
        public void ConstructorCalculatesMaxChunkLength(int maxPayload, int expected)
        {
            var client = new ChunkMethodClient(new ScriptedRpcClient
            {
                MaxMethodPayloadSizeInBytes = maxPayload
            }, NullLogger<ChunkMethodClient>.Instance);

            Assert.Equal(expected, client.MaxChunkLength);
        }

        [Fact]
        public async Task CallMethodAsyncRejectsEmptyMethodAsync()
        {
            var client = new ChunkMethodClient(new ScriptedRpcClient(),
                NullLogger<ChunkMethodClient>.Instance);

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await client.CallMethodAsync("target", "", "{}"u8.ToArray(),
                    ContentMimeType.Json, null, CancellationToken.None));
        }

        [Theory]
        [InlineData(null, "{}")]
        [InlineData("application/json", "{}")]
        [InlineData("application/octet-stream", " ")]
        public async Task CallMethodAsyncSubstitutesEmptyPayloadAsync(
            string? contentType, string expectedPayload)
        {
            var rpc = new ScriptedRpcClient();
            rpc.Responses.Enqueue(CreateResponse("""{"ok":true}"""));
            var client = new ChunkMethodClient(rpc,
                NullLogger<ChunkMethodClient>.Instance);

            await client.CallMethodAsync("target", "method",
                ReadOnlyMemory<byte>.Empty, contentType!, null, CancellationToken.None);

            var request = Assert.Single(rpc.Requests);
            Assert.Equal("method", request.MethodName);
            Assert.Equal(contentType ?? ContentMimeType.Json, request.ContentType);
            Assert.Equal(expectedPayload, Encoding.UTF8.GetString(
                request.Payload!.Unzip()));
        }

        [Fact]
        public async Task CallMethodAsyncSplitsCompressedPayloadIntoChunksAsync()
        {
            var rpc = new ScriptedRpcClient
            {
                MaxMethodPayloadSizeInBytes = 8
            };
            rpc.Responses.Enqueue(CreateResponse("""{"ok":true}"""));
            var client = new ChunkMethodClient(rpc,
                NullLogger<ChunkMethodClient>.Instance);
            var payload = Encoding.UTF8.GetBytes(new string('x', 256));

            var response = await client.CallMethodAsync("target", "method",
                payload, ContentMimeType.Json, TimeSpan.FromSeconds(5),
                CancellationToken.None);

            Assert.Equal("""{"ok":true}""", Encoding.UTF8.GetString(response.Span));
            Assert.InRange(rpc.Requests.Count, 2, int.MaxValue);
            Assert.Equal("method", rpc.Requests[0].MethodName);
            Assert.Equal(TimeSpan.FromSeconds(5), rpc.Requests[0].Timeout);
            Assert.All(rpc.Requests.Skip(1), request =>
            {
                Assert.Null(request.MethodName);
                Assert.Null(request.ContentType);
            });
            var compressed = rpc.Requests.SelectMany(r => r.Payload ?? []).ToArray();
            Assert.Equal(payload, compressed.Unzip());
        }

        [Fact]
        public async Task CallMethodAsyncReceivesContinuationResponsesAsync()
        {
            var rpc = new ScriptedRpcClient();
            var compressed = Encoding.UTF8.GetBytes("""{"ok":true}""").Zip();
            rpc.Responses.Enqueue(new MethodChunkModel
            {
                Payload = compressed[..3],
                Handle = "next"
            });
            rpc.Responses.Enqueue(new MethodChunkModel
            {
                Payload = compressed[3..],
                Status = 200
            });
            var client = new ChunkMethodClient(rpc,
                NullLogger<ChunkMethodClient>.Instance);

            var response = await client.CallMethodAsync("target", "method",
                "{}"u8.ToArray(), ContentMimeType.Json, null, CancellationToken.None);

            Assert.Equal("""{"ok":true}""", Encoding.UTF8.GetString(response.Span));
            Assert.Equal(2, rpc.Requests.Count);
            Assert.Equal("next", rpc.Requests[1].Handle);
            Assert.Null(rpc.Requests[1].Payload);
        }

        [Fact]
        public async Task CallMethodAsyncThrowsStatusExceptionForNonSuccessAsync()
        {
            var rpc = new ScriptedRpcClient();
            var error = new MethodCallStatusException(429, "too many", "Too Many")
                .Serialize().ToArray();
            rpc.Responses.Enqueue(new MethodChunkModel
            {
                Payload = error.Zip(),
                Status = 429
            });
            var client = new ChunkMethodClient(rpc,
                NullLogger<ChunkMethodClient>.Instance);

            var ex = await Assert.ThrowsAsync<MethodCallStatusException>(async () =>
                await client.CallMethodAsync("target", "method", "{}"u8.ToArray(),
                    ContentMimeType.Json, null, CancellationToken.None));

            Assert.Equal(429, ex.Status);
            Assert.Equal("too many", ex.Details.Detail);
        }

        private static MethodChunkModel CreateResponse(string payload)
        {
            return new MethodChunkModel
            {
                Payload = Encoding.UTF8.GetBytes(payload).Zip(),
                Status = 200
            };
        }

        private sealed class ScriptedRpcClient : IRpcClient
        {
            public string Name => "scripted";
            public int MaxMethodPayloadSizeInBytes { get; init; } = 256 * 1024;
            public List<MethodChunkModel> Requests { get; } = [];
            public Queue<MethodChunkModel> Responses { get; } = [];

            public ValueTask<ReadOnlySequence<byte>> CallAsync(string target,
                string method, ReadOnlySequence<byte> payload, string contentType,
                TimeSpan? timeout = null, CancellationToken ct = default)
            {
                Assert.Equal("target", target);
                Assert.Equal(MethodNames.Call, method);
                Assert.Equal(ContentMimeType.Json, contentType);
                var payloadMemory = payload.IsSingleSegment ?
                    payload.First : payload.ToArray();
                Requests.Add(Json.Deserialize(payloadMemory,
                    CoreJsonContext.Default.MethodChunkModel)!);
                var response = Responses.Count == 0 ? new MethodChunkModel
                {
                    Status = 200
                } : Responses.Dequeue();
                return ValueTask.FromResult(new ReadOnlySequence<byte>(
                    Json.SerializeToMemory(response,
                        CoreJsonContext.Default.MethodChunkModel)));
            }
        }
    }
}
