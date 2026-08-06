// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="AsyncEnumerableStack{T}"/> and
    /// <see cref="AsyncEnumerableEnumerableStack{T}"/>.
    /// </summary>
    public sealed class AsyncEnumerableStackTests
    {
        // ── AsyncEnumerableStack<T> ────────────────────────────────────────────

        [Fact]
        public void Stack_HasMore_ReturnsFalse_WhenEmpty()
        {
            var stack = new ConcreteStack<int>();
            Assert.False(stack.HasMore);
        }

        [Fact]
        public void Stack_HasMore_ReturnsTrue_AfterPush()
        {
            var stack = new ConcreteStack<int>();
            stack.PushOp(_ => new ValueTask<int>(42));
            Assert.True(stack.HasMore);
        }

        [Fact]
        public void Stack_Reset_ClearsOperations()
        {
            var stack = new ConcreteStack<int>();
            stack.PushOp(_ => new ValueTask<int>(1));
            stack.PushOp(_ => new ValueTask<int>(2));
            stack.Reset();
            Assert.False(stack.HasMore);
        }

        [Fact]
        public async Task Stack_ExecuteAsync_ReturnsResultFromOp()
        {
            var stack = new ConcreteStack<int>();
            var ctx = new ServiceCallContext(Mock.Of<IOpcUaSession>(), TimeSpan.FromSeconds(5));
            stack.PushOp(_ => new ValueTask<int>(99));
            var result = await stack.ExecuteAsync(ctx);
            Assert.Equal(99, result.Single());
        }

        [Fact]
        public async Task Stack_ExecuteAsync_RepushesOpOnException_WhenStackSizeUnchanged()
        {
            var stack = new ConcreteStack<int>();
            var ctx = new ServiceCallContext(Mock.Of<IOpcUaSession>(), TimeSpan.FromSeconds(5));
            var callCount = 0;
            stack.PushOp(_ =>
            {
                callCount++;
                throw new InvalidOperationException("test");
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                stack.ExecuteAsync(ctx).AsTask());

            // After exception the op should have been re-pushed
            Assert.True(stack.HasMore);
        }

        [Fact]
        public async Task Stack_ExecuteAsync_DoesNotRepushOp_WhenAnotherPushHappened()
        {
            var stack = new ConcreteStack<int>();
            var ctx = new ServiceCallContext(Mock.Of<IOpcUaSession>(), TimeSpan.FromSeconds(5));
            stack.PushOp(inner =>
            {
                // During the same call, push a second op — changing the count
                stack.PushOp(_ => new ValueTask<int>(0));
                throw new InvalidOperationException("test");
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                stack.ExecuteAsync(ctx).AsTask());

            // Only the second op pushed from inside remains; the original was NOT re-pushed
            Assert.Single(stack.ToArray());
        }

        // ── AsyncEnumerableEnumerableStack<T> ──────────────────────────────────

        [Fact]
        public void EnumerableStack_HasMore_ReturnsFalse_WhenEmpty()
        {
            var stack = new ConcreteEnumerableStack<string>();
            Assert.False(stack.HasMore);
        }

        [Fact]
        public void EnumerableStack_HasMore_ReturnsTrue_AfterPush()
        {
            var stack = new ConcreteEnumerableStack<string>();
            stack.PushOp(_ => new ValueTask<IEnumerable<string>>(["a"]));
            Assert.True(stack.HasMore);
        }

        [Fact]
        public void EnumerableStack_Reset_ClearsOperations()
        {
            var stack = new ConcreteEnumerableStack<string>();
            stack.PushOp(_ => new ValueTask<IEnumerable<string>>(["a"]));
            stack.Reset();
            Assert.False(stack.HasMore);
        }

        [Fact]
        public async Task EnumerableStack_ExecuteAsync_ReturnsResultFromOp()
        {
            var stack = new ConcreteEnumerableStack<string>();
            var ctx = new ServiceCallContext(Mock.Of<IOpcUaSession>(), TimeSpan.FromSeconds(5));
            stack.PushOp(_ => new ValueTask<IEnumerable<string>>(["x", "y"]));
            var result = await stack.ExecuteAsync(ctx);
            Assert.Equal(["x", "y"], result.ToArray());
        }

        [Fact]
        public async Task EnumerableStack_ExecuteAsync_RepushesOpOnException_WhenStackSizeUnchanged()
        {
            var stack = new ConcreteEnumerableStack<string>();
            var ctx = new ServiceCallContext(Mock.Of<IOpcUaSession>(), TimeSpan.FromSeconds(5));
            stack.PushOp(_ => throw new InvalidOperationException("test"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                stack.ExecuteAsync(ctx).AsTask());

            Assert.True(stack.HasMore);
        }

        // ── Concrete helpers ───────────────────────────────────────────────────

        private sealed class ConcreteStack<T> : AsyncEnumerableStack<T>
        {
            public void PushOp(Func<ServiceCallContext, ValueTask<T>> value) => Push(value);

            // Exposes internal list for assertion — uses HasMore + Reset as proxies
            public T[] ToArray()
            {
                var results = new List<T>();
                while (HasMore)
                {
                    // Pop by executing — use a null session context
                    var ctx = new ServiceCallContext(Mock.Of<IOpcUaSession>(), TimeSpan.FromSeconds(5));
                    try
                    {
                        results.Add(ExecuteAsync(ctx).AsTask().GetAwaiter().GetResult().Single());
                    }
                    catch
                    {
                        break;
                    }
                }
                return [.. results];
            }
        }

        private sealed class ConcreteEnumerableStack<T> : AsyncEnumerableEnumerableStack<T>
        {
            public void PushOp(Func<ServiceCallContext, ValueTask<IEnumerable<T>>> value)
                => Push(value);
        }
    }
}
