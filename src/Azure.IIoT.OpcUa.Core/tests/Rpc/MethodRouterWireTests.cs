// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Router
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Models;
    using Azure.IIoT.OpcUa.Core.Rpc.Protocol;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using FluentAssertions;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Wire-format regression tests for the tunnel <see cref="MethodRouter"/> and
    /// the direct-method chunking protocol ported from Legacy. These assert the
    /// exact JSON shape of the <see cref="MethodChunkModel"/> envelope, a full
    /// request/response round-trip through <see cref="ChunkMethodClient"/> and the
    /// server side reassembly, and the exception -> status-code propagation.
    /// </summary>
    [Trait("Compatibility", "Authoritative")]
    public sealed partial class MethodRouterWireTests
    {
        [Fact]
        public void MethodChunkModelSerializesWithLegacyCompatibleShape()
        {
            var model = new MethodChunkModel
            {
                MethodName = "Echo_V1",
                ContentType = "application/json",
                ContentLength = 5,
                MaxChunkLength = 1000,
                Payload = new byte[] { 1, 2, 3 }
            };

            var json = Encoding.UTF8.GetString(Json.SerializeToMemory(model).Span);

            // Declaration order: handle, method, contentType, length, payload,
            // status, timeout, acceptedSize, properties. Null members omitted and
            // the byte[] payload is base64 encoded to stay wire compatible.
            json.Should().Be(
                "{\"method\":\"Echo_V1\",\"contentType\":\"application/json\"," +
                "\"length\":5,\"payload\":\"AQID\",\"acceptedSize\":1000}");
        }

        [Fact]
        public void ContinuationChunkOnlyEmitsHandleAndPayload()
        {
            var model = new MethodChunkModel
            {
                Handle = "7",
                Payload = new byte[] { 42 }
            };

            var json = Encoding.UTF8.GetString(Json.SerializeToMemory(model).Span);

            json.Should().Be("{\"handle\":\"7\",\"payload\":\"Kg==\"}");
        }

        [Fact]
        public async Task RoundTripsSingleChunkRequestResponseAsync()
        {
            await using var router = CreateRouter();
            var client = new ChunkMethodClient(new RouterRpcClient(router, 256 * 1024),
                NullLogger<ChunkMethodClient>.Instance);

            var request = Json.SerializeToMemory(new EchoRequest { Value = "hello" });
            var response = await client.CallMethodAsync("target", "Echo_V1",
                request, ContentMimeType.Json, null, CancellationToken.None);

            var result = Json.Deserialize<EchoResponse>(response);
            result!.Value.Should().Be("hello");
        }

        [Fact]
        public async Task RoundTripsChunkRequestWithTraceParentOnlyAsync()
        {
            var controller = new TestController();
            await using var router = CreateRouter(controller);

            var payload = Json.SerializeToMemory(new EchoRequest { Value = "hello" }).ToArray().Zip();
            var request = new MethodChunkModel
            {
                MethodName = "Echo_V1",
                ContentType = ContentMimeType.Json,
                ContentLength = payload.Length,
                MaxChunkLength = payload.Length,
                Payload = payload,
                Properties = new Dictionary<string, string>
                {
                    ["traceparent"] =
                        "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
                }
            };

            var response = await router.InvokeAsync(MethodNames.Call,
                new ReadOnlySequence<byte>(Json.SerializeToMemory(request).ToArray()),
                ContentMimeType.Json, CancellationToken.None);

            var chunk = Json.Deserialize<MethodChunkModel>(response);
            chunk.Should().NotBeNull();
            controller.EchoCalls.Should().Be(1);
            chunk!.Handle.Should().BeNull();
            chunk.Status.Should().BeNull();
            chunk.ContentLength.Should().NotBeNull();
            chunk.Payload.Should().NotBeNull();

            var result = Json.Deserialize<EchoResponse>(chunk.Payload!.Unzip());
            result!.Value.Should().Be("hello");
        }

        [Fact]
        public async Task RoundTripsMultiChunkRequestResponseAsync()
        {
            // Small transport payload budget forces the client and server to
            // exercise the chunk upload / download reassembly path.
            await using var router = CreateRouter();
            var client = new ChunkMethodClient(new RouterRpcClient(router, 100),
                NullLogger<ChunkMethodClient>.Instance);

            var big = new string('x', 8000);
            var request = Json.SerializeToMemory(new EchoRequest { Value = big });
            var response = await client.CallMethodAsync("target", "Echo_V1",
                request, ContentMimeType.Json, null, CancellationToken.None);

            var result = Json.Deserialize<EchoResponse>(response);
            result!.Value.Should().Be(big);
        }

        [Fact]
        public async Task PropagatesMethodCallStatusFromControllerAsync()
        {
            await using var router = CreateRouter();
            var client = new ChunkMethodClient(new RouterRpcClient(router, 256 * 1024),
                NullLogger<ChunkMethodClient>.Instance);

            var request = Json.SerializeToMemory(new EchoRequest { Value = "x" });

            var act = async () => await client.CallMethodAsync("target",
                "FailNotFound_V1", request, ContentMimeType.Json, null,
                CancellationToken.None);

            var ex = (await act.Should().ThrowAsync<MethodCallStatusException>())
                .Which;
            ex.Details.Status.Should().Be(404);
            ex.Details.Detail.Should().Be("missing");
        }

        [Fact]
        public async Task MapsUnhandledExceptionToDefaultBadRequestStatusAsync()
        {
            // With no custom exception filter the router's DefaultFilter maps
            // any unhandled exception to HTTP 400.
            await using var router = CreateRouter();
            var client = new ChunkMethodClient(new RouterRpcClient(router, 256 * 1024),
                NullLogger<ChunkMethodClient>.Instance);

            var request = Json.SerializeToMemory(new EchoRequest { Value = "x" });

            var act = async () => await client.CallMethodAsync("target",
                "FailGeneric_V1", request, ContentMimeType.Json, null,
                CancellationToken.None);

            var ex = (await act.Should().ThrowAsync<MethodCallStatusException>())
                .Which;
            ex.Details.Status.Should().Be(400);
            ex.Details.Detail.Should().Be("boom");
        }

        private static MethodRouter CreateRouter()
        {
            return CreateRouter(new TestController());
        }

        private static MethodRouter CreateRouter(TestController controller)
        {
            var router = new MethodRouter(Array.Empty<IRpcServer>(),
                NullLogger<MethodRouter>.Instance,
                new MethodRouterJsonSerializer(
                    MethodRouterWireTestsJsonContext.Default,
                    MethodRouterWireTestsJsonContext.Default.Options));
            Azure_IIoT_OpcUa_Core_TestsMethodRouterDescriptors.Register(router,
                new[] { controller }, router.JsonSerializer);
            router.GetAwaiter().GetResult();
            return router;
        }

        [Version("_V1")]
        public sealed class TestController : IMethodController
        {
            public int EchoCalls => _echoCalls;

            public async Task<EchoResponse> EchoAsync(EchoRequest request)
            {
                Interlocked.Increment(ref _echoCalls);
                await Task.Yield();
                return new EchoResponse { Value = request?.Value };
            }

            public async Task<EchoResponse> FailNotFoundAsync(EchoRequest request)
            {
                await Task.Yield();
                throw new MethodCallStatusException(404, "missing", "Not Found");
            }

            public async Task<EchoResponse> FailGenericAsync(EchoRequest request)
            {
                await Task.Yield();
                throw new InvalidOperationException("boom");
            }

            private int _echoCalls;
        }

        public sealed class EchoRequest
        {
            public string? Value { get; set; }
        }

        public sealed class EchoResponse
        {
            public string? Value { get; set; }
        }

        /// <summary>
        /// Bridges the <see cref="IRpcClient"/> surface used by
        /// <see cref="ChunkMethodClient"/> directly onto the in-process
        /// <see cref="MethodRouter"/> so the chunk protocol can be exercised
        /// without a real transport.
        /// </summary>
        private sealed class RouterRpcClient : IRpcClient
        {
            public string Name => "test";

            public int MaxMethodPayloadSizeInBytes { get; }

            public RouterRpcClient(MethodRouter router, int maxPayload)
            {
                _router = router;
                MaxMethodPayloadSizeInBytes = maxPayload;
            }

            public async ValueTask<ReadOnlySequence<byte>> CallAsync(string target,
                string method, ReadOnlySequence<byte> payload, string contentType,
                TimeSpan? timeout = null, CancellationToken ct = default)
            {
                return await _router.InvokeAsync(method, payload, contentType, ct)
                    .ConfigureAwait(false);
            }

            private readonly MethodRouter _router;
        }

        [JsonSourceGenerationOptions(
            PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true)]
        [JsonSerializable(typeof(MethodRouterWireTests.EchoRequest))]
        [JsonSerializable(typeof(MethodRouterWireTests.EchoResponse))]
        internal sealed partial class MethodRouterWireTestsJsonContext : JsonSerializerContext
        {
        }
    }
}
