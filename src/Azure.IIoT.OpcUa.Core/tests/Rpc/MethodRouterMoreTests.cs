// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Router
{
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Buffers;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Additional tests for <see cref="MethodRouter"/> covering the
    /// <see cref="MethodRouter.Register"/> method and related paths.
    /// </summary>
    public sealed class MethodRouterMoreTests
    {
        // ── Register — basic registration ────────────────────────────────────

        [Fact]
        public async Task Register_AddsDescriptorAndInvocationSucceedsAsync()
        {
            await using var router = CreateRouter();
            await router;

            router.Register("Echo", CreateEchoDescriptor());

            var payload = new ReadOnlySequence<byte>(
                System.Text.Encoding.UTF8.GetBytes("""{"message":"hello"}"""));
            var result = await router.InvokeAsync("Echo", payload,
                "application/json", CancellationToken.None).ConfigureAwait(false);

            var json = System.Text.Encoding.UTF8.GetString(result.ToArray());
            Assert.Contains("hello", json);
        }

        // ── Register — registering the same name twice builds a collection ───

        [Fact]
        public async Task Register_SameName_SecondDescriptorIsInvokedAfterFirstFailsAsync()
        {
            await using var router = CreateRouter();
            await router;

            // First descriptor always throws; second should run.
            router.Register("MultiEcho", CreateThrowingDescriptor("MultiEcho"));
            router.Register("MultiEcho", CreateEchoDescriptor("MultiEcho"));

            var payload = new ReadOnlySequence<byte>(
                System.Text.Encoding.UTF8.GetBytes("""{"message":"multi"}"""));
            var result = await router.InvokeAsync("MultiEcho", payload,
                "application/json", CancellationToken.None).ConfigureAwait(false);

            var json = System.Text.Encoding.UTF8.GetString(result.ToArray());
            Assert.Contains("multi", json);
        }

        // ── Register — private invoker name collision throws ─────────────────

        [Fact]
        public async Task Register_WhenExternalInvokerAlreadyRegisteredForName_ThrowsAsync()
        {
            await using var router = CreateRouter();
            await router;

            // Add an external invoker first (occupies the slot as a non-collection).
            router.ExternalInvokers = [new ExternalInvoker("Clash")];

            // Now try to register a descriptor under the same name → should throw.
            Assert.Throws<InvalidOperationException>(() =>
                router.Register("Clash", CreateEchoDescriptor("Clash")));
        }

        // ── Dispose — normal path ─────────────────────────────────────────────

        [Fact]
        public async Task DisposeAsync_CompletesWithoutThrowingAsync()
        {
            var router = CreateRouter();
            await router;

            var ex = await Record.ExceptionAsync(
                async () => await router.DisposeAsync().ConfigureAwait(false))
                .ConfigureAwait(false);

            Assert.Null(ex);
        }

        [Fact]
        public async Task Dispose_SynchronousPath_CompletesWithoutThrowingAsync()
        {
            var router = CreateRouter();
            await router;

            var ex = Record.Exception(() => router.Dispose());

            Assert.Null(ex);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static MethodRouter CreateRouter()
        {
            return new MethodRouter(
                [],
                NullLogger<MethodRouter>.Instance,
                new MethodRouterJsonSerializer(
                    [new JsonContextMethodRouterJsonTypeInfoProvider(
                        MethodRouterMoreTestsJsonContext.Default)]));
        }

        private static MethodRouteDescriptor CreateEchoDescriptor(string name = "Echo")
        {
            return new MethodRouteDescriptor(name, null, async (payload, ct) =>
            {
                var req = JsonSerializer.Deserialize<MoreEchoRequest>(payload.Span,
                    MethodRouterMoreTestsJsonContext.Default.MoreEchoRequest)!;
                var resp = new MoreEchoResponse { Echo = req.Message };
                return (ReadOnlyMemory<byte>)JsonSerializer.SerializeToUtf8Bytes(resp,
                    MethodRouterMoreTestsJsonContext.Default.MoreEchoResponse);
            });
        }

        private static MethodRouteDescriptor CreateThrowingDescriptor(string name)
        {
            return new MethodRouteDescriptor(name, null, (payload, ct) =>
                throw new InvalidOperationException("Always fails."));
        }

        private sealed class ExternalInvoker : IMethodInvoker
        {
            public string MethodName { get; }

            public ExternalInvoker(string methodName) => MethodName = methodName;

            public ValueTask<ReadOnlyMemory<byte>> InvokeAsync(
                ReadOnlyMemory<byte> payload, string contentType,
                IRpcHandler context, CancellationToken ct)
            {
                return ValueTask.FromResult(ReadOnlyMemory<byte>.Empty);
            }
        }
    }

    internal sealed class MoreEchoRequest
    {
        public string? Message { get; set; }
    }

    internal sealed class MoreEchoResponse
    {
        public string? Echo { get; set; }
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(MoreEchoRequest))]
    [JsonSerializable(typeof(MoreEchoResponse))]
    internal sealed partial class MethodRouterMoreTestsJsonContext : JsonSerializerContext
    {
    }
}
