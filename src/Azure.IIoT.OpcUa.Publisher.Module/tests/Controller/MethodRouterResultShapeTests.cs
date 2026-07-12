// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Router;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using FluentAssertions;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Exercises the <see cref="MethodRouter"/> direct-method dispatch across
    /// every controller result shape (void <c>Task</c>/<c>ValueTask</c>, typed
    /// <c>Task&lt;T&gt;</c>, <c>ValueTask&lt;T&gt;</c> and
    /// <c>IAsyncEnumerable&lt;T&gt;</c>). This guards the AOT-safe invoker that
    /// replaced the former <c>MakeGenericMethod</c> continuation dispatch, in
    /// particular the reflective async-enumerable drain path.
    /// </summary>
    [Trait("Compatibility", "Authoritative")]
    public sealed partial class MethodRouterResultShapeTests
    {
        internal sealed class ShapeController : IMethodController
        {
            public Task TouchAsync(string input)
            {
                LastInput = input;
                return Task.CompletedTask;
            }

            public ValueTask PokeAsync(string input)
            {
                LastInput = input;
                return ValueTask.CompletedTask;
            }

            public Task<string> EchoTaskAsync(string input)
            {
                return Task.FromResult("task:" + input);
            }

            public ValueTask<string> EchoValueTaskAsync(string input)
            {
                return ValueTask.FromResult("value:" + input);
            }

            public async IAsyncEnumerable<int> RangeAsync(int count,
                [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var i = 0; i < count; i++)
                {
                    await Task.Yield();
                    yield return i;
                }
            }

            public Task<string> FailAsync(string input)
            {
                throw new InvalidOperationException("boom:" + input);
            }

            public string? LastInput { get; private set; }
        }

        private static MethodRouter NewRouter(ShapeController controller)
        {
            var router = new MethodRouter(Array.Empty<IRpcServer>(),
                NullLogger<MethodRouter>.Instance,
                new MethodRouterJsonSerializer(
                    MethodRouterResultShapeTestsJsonContext.Default,
                    MethodRouterResultShapeTestsJsonContext.Default.Options));
            Azure_IIoT_OpcUa_Publisher_Module_TestsMethodRouterDescriptors.Register(
                router, new[] { controller }, router.JsonSerializer);
            router.GetAwaiter().GetResult();
            return router;
        }

        private static async Task<ReadOnlyMemory<byte>> InvokeRawAsync(
            MethodRouter router, string method, object payload)
        {
            var json = Json.SerializeToString(payload);
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));
            var result = await router.InvokeAsync(method, buffer,
                "application/json", CancellationToken.None);
            return result.IsSingleSegment ? result.First : result.ToArray();
        }

        private static async Task<T?> InvokeAsync<T>(MethodRouter router,
            string method, object payload)
        {
            var bytes = await InvokeRawAsync(router, method, payload);
            return Json.Deserialize<T>(bytes);
        }

        [Fact]
        public async Task VoidTaskIsDispatchedAsync()
        {
            var controller = new ShapeController();
            await using var router = NewRouter(controller);
            var response = await InvokeRawAsync(router, "Touch", "hi");
            controller.LastInput.Should().Be("hi");
            response.Length.Should().Be(0);
        }

        [Fact]
        public async Task VoidValueTaskIsDispatchedAsync()
        {
            var controller = new ShapeController();
            await using var router = NewRouter(controller);
            var response = await InvokeRawAsync(router, "Poke", "yo");
            controller.LastInput.Should().Be("yo");
            response.Length.Should().Be(0);
        }

        [Fact]
        public async Task TypedTaskResultIsSerializedAsync()
        {
            var controller = new ShapeController();
            await using var router = NewRouter(controller);
            var response = await InvokeAsync<string>(router, "EchoTask", "a");
            response.Should().Be("task:a");
        }

        [Fact]
        public async Task TypedValueTaskResultIsSerializedAsync()
        {
            var controller = new ShapeController();
            await using var router = NewRouter(controller);
            var response = await InvokeAsync<string>(router, "EchoValueTask", "b");
            response.Should().Be("value:b");
        }

        [Fact]
        public async Task AsyncEnumerableIsDrainedAndSerializedAsync()
        {
            var controller = new ShapeController();
            await using var router = NewRouter(controller);
            var response = await InvokeAsync<List<int>>(router, "Range", 4);
            response.Should().Equal(0, 1, 2, 3);
        }

        [Fact]
        public async Task AsyncEnumerableEmptyIsDrainedAsync()
        {
            var controller = new ShapeController();
            await using var router = NewRouter(controller);
            var response = await InvokeAsync<List<int>>(router, "Range", 0);
            response.Should().BeEmpty();
        }

        [Fact]
        public async Task ThrowingMethodSurfacesMethodCallStatusExceptionAsync()
        {
            var controller = new ShapeController();
            await using var router = NewRouter(controller);
            var buffer = new ReadOnlySequence<byte>(
                Encoding.UTF8.GetBytes(Json.SerializeToString("x")));
            var act = async () => await router.InvokeAsync("Fail", buffer,
                "application/json", CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();
        }

        [JsonSourceGenerationOptions(
            PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true)]
        [JsonSerializable(typeof(string))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(List<int>))]
        internal sealed partial class MethodRouterResultShapeTestsJsonContext :
            JsonSerializerContext
        {
        }
    }
}
