// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Router
{
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using FluentAssertions;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Additional unit tests for <see cref="MethodRouter"/> and related types
    /// targeting the uncovered branches.
    /// </summary>
    public sealed class MethodRouterUnitTests
    {
        // ── Controllers setter ───────────────────────────────────────────────

        [Fact]
        public void Controllers_Set_ThrowsInvalidOperationException()
        {
            var router = CreateRouter();

            Action act = () => router.Controllers =
                Array.Empty<IMethodController>();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*generated descriptors*");
        }

        // ── ExternalInvokers setter ──────────────────────────────────────────

        [Fact]
        public void ExternalInvokers_Set_DoesNotThrow()
        {
            var router = CreateRouter();

            Action act = () => router.ExternalInvokers =
                Array.Empty<IMethodInvoker>();

            act.Should().NotThrow();
        }

        // ── MissingMethodRouterJsonTypeInfoProvider ───────────────────────────

        [Fact]
        public void MissingMethodRouterJsonTypeInfoProvider_GetTypeInfo_Throws()
        {
            var provider = new MissingMethodRouterJsonTypeInfoProvider();

            Action act = () => provider.GetTypeInfo(typeof(string));

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No source-generated JSON metadata*");
        }

        // ── JsonContextMethodRouterJsonTypeInfoProvider ───────────────────────

        [Fact]
        public void JsonContextMethodRouterJsonTypeInfoProvider_Null_Throws()
        {
            Action act = () => new JsonContextMethodRouterJsonTypeInfoProvider(null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }

        [Fact]
        public void JsonContextMethodRouterJsonTypeInfoProvider_UnknownType_ReturnsNull()
        {
            var provider = new JsonContextMethodRouterJsonTypeInfoProvider(
                MethodRouterUnitTestsJsonContext.Default);

            var info = provider.GetTypeInfo(typeof(UnknownModel));

            info.Should().BeNull();
        }

        // ── MethodRouterJsonSerializer ───────────────────────────────────────

        [Fact]
        public void MethodRouterJsonSerializer_NullProviders_Throws()
        {
            Action act = () => new MethodRouterJsonSerializer(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void MethodRouterJsonSerializer_GetTypeInfo_WhenNotFound_Throws()
        {
            var serializer = new MethodRouterJsonSerializer(
                Array.Empty<IMethodRouterJsonTypeInfoProvider>());

            Action act = () => serializer.GetTypeInfo<UnknownModel>();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No source-generated JSON metadata*");
        }

        [Fact]
        public void MethodRouterJsonSerializer_GetTypeInfo_WhenFound_ReturnsInfo()
        {
            var serializer = new MethodRouterJsonSerializer(
            [
                new JsonContextMethodRouterJsonTypeInfoProvider(
                    MethodRouterUnitTestsJsonContext.Default)
            ]);

            var typeInfo = serializer.GetTypeInfo<MethodRouterUnitTestsModel>();

            typeInfo.Should().NotBeNull();
            typeInfo.Type.Should().Be(typeof(MethodRouterUnitTestsModel));
        }

        // ── MethodRouterJson static helpers ─────────────────────────────────

        [Fact]
        public void MethodRouterJson_Serialize_ProducesJsonBytes()
        {
            var value = new MethodRouterUnitTestsModel { Name = "test" };
            var typeInfo = MethodRouterUnitTestsJsonContext.Default.MethodRouterUnitTestsModel;

            var result = MethodRouterJson.Serialize(value, typeInfo);

            result.IsEmpty.Should().BeFalse();
            var json = System.Text.Encoding.UTF8.GetString(result.Span);
            json.Should().Contain("test");
        }

        [Fact]
        public void MethodRouterJson_Deserialize_FromMemory_ReturnsModel()
        {
            var json = System.Text.Encoding.UTF8.GetBytes("""{"name":"hello"}""");
            var typeInfo = MethodRouterUnitTestsJsonContext.Default.MethodRouterUnitTestsModel;

            var result = MethodRouterJson.Deserialize<MethodRouterUnitTestsModel>(
                new ReadOnlyMemory<byte>(json), typeInfo);

            result.Should().NotBeNull();
            result!.Name.Should().Be("hello");
        }

        [Fact]
        public void MethodRouterJson_Deserialize_FromJsonElement_ReturnsModel()
        {
            var doc = JsonDocument.Parse("""{"name":"element"}""");
            var element = doc.RootElement;
            var typeInfo = MethodRouterUnitTestsJsonContext.Default.MethodRouterUnitTestsModel;

            var result = MethodRouterJson.Deserialize(element, typeInfo);

            result.Should().NotBeNull();
            result!.Name.Should().Be("element");
        }

        [Fact]
        public async Task MethodRouterJson_DrainAsync_CollectsAllItems()
        {
            var typeInfo =
                MethodRouterUnitTestsJsonContext.Default.ListMethodRouterUnitTestsModel;

            var result = await MethodRouterJson.DrainAsync(
                GenerateItemsAsync(), typeInfo);

            result.IsEmpty.Should().BeFalse();
            var json = System.Text.Encoding.UTF8.GetString(result.Span);
            json.Should().Contain("item0");
            json.Should().Contain("item1");
            json.Should().Contain("item2");
        }

        // ── MethodRouter server connection failure path ───────────────────────

        [Fact]
        public async Task MethodRouter_ConnectAsync_WhenServerThrows_SkipsServerAsync()
        {
            // A server that throws on ConnectAsync should be logged and skipped,
            // leaving the router connected to zero servers.
            var failingServer = new ThrowingRpcServer();

            await using var router = new MethodRouter(
                new[] { failingServer },
                NullLogger<MethodRouter>.Instance,
                new MethodRouterJsonSerializer(
                    [new JsonContextMethodRouterJsonTypeInfoProvider(
                        MethodRouterUnitTestsJsonContext.Default)]));

            // Awaiting the router completes once all connection attempts finish.
            await router;
        }

        private static MethodRouter CreateRouter()
        {
            return new MethodRouter(
                Array.Empty<IRpcServer>(),
                NullLogger<MethodRouter>.Instance,
                new MethodRouterJsonSerializer(
                    [new JsonContextMethodRouterJsonTypeInfoProvider(
                        MethodRouterUnitTestsJsonContext.Default)]));
        }

        private static async IAsyncEnumerable<MethodRouterUnitTestsModel> GenerateItemsAsync()
        {
            await Task.Yield();
            yield return new MethodRouterUnitTestsModel { Name = "item0" };
            yield return new MethodRouterUnitTestsModel { Name = "item1" };
            yield return new MethodRouterUnitTestsModel { Name = "item2" };
        }

        private sealed class ThrowingRpcServer : IRpcServer
        {
            public string Name => "throwing";
            public System.Collections.Generic.IEnumerable<IRpcHandler> Connected =>
                System.Array.Empty<IRpcHandler>();

            public void Start() { }

            public ValueTask<IAsyncDisposable> ConnectAsync(IRpcHandler handler,
                CancellationToken ct = default)
            {
                throw new InvalidOperationException("server unavailable");
            }
        }

        private sealed class UnknownModel { }
    }

    public sealed class MethodRouterUnitTestsModel
    {
        public string? Name { get; set; }
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(MethodRouterUnitTestsModel))]
    [JsonSerializable(typeof(List<MethodRouterUnitTestsModel>))]
    internal sealed partial class MethodRouterUnitTestsJsonContext : JsonSerializerContext
    {
    }
}
