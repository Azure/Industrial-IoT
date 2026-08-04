// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Protocol
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Models;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Buffers;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ChunkMethodServerTests
    {
        [Fact]
        public void ConstructorRegistersChunkCallInvoker()
        {
            using var server = CreateServer();

            Assert.Equal(string.Empty, server.MountPoint);
            Assert.Equal(1, server.Count);
            Assert.False(server.IsReadOnly);
            Assert.Equal(true, server.TryGetValue(MethodNames.Call, out var invoker));
            Assert.Equal(true, server.Contains(invoker!));
        }

        [Fact]
        public void CollectionOperationsUseCaseInsensitiveMethodNames()
        {
            using var server = CreateServer();
            var invoker = new EchoInvoker("custom");

            server.Add(invoker);
            Assert.Equal(true, server.TryGetValue("CUSTOM", out var registered));
            Assert.Same(invoker, registered);

            var copy = new IMethodInvoker[server.Count];
            server.CopyTo(copy, 0);
            Assert.Contains(invoker, copy);
            Assert.Contains(invoker, server.Cast<IMethodInvoker>());

            Assert.Equal(true, server.Remove(new EchoInvoker("CUSTOM")));
            Assert.False(server.TryGetValue("custom", out _));

            server.Add("alias", invoker);
            Assert.Equal(true, server.TryGetValue("ALIAS", out registered));
            Assert.Same(invoker, registered);

            server.Clear();
            Assert.Empty(server);
        }

        [Fact]
        public async Task InvokeAsyncUsesRegisteredInvokerAsync()
        {
            using var server = CreateServer();
            server.Clear();
            server.Add(new EchoInvoker("echo"));

            var response = await server.InvokeAsync("ECHO",
                new ReadOnlySequence<byte>("payload"u8.ToArray()),
                ContentMimeType.Json, CancellationToken.None);

            Assert.Equal("payload", Encoding.UTF8.GetString(response.ToArray()));
        }

        [Fact]
        public async Task InvokeAsyncDelegatesUnknownMethodAsync()
        {
            using var server = CreateServer();
            server.Delegate = new EchoHandler("mount");

            var response = await server.InvokeAsync("unknown",
                new ReadOnlySequence<byte>("payload"u8.ToArray()),
                ContentMimeType.Json, CancellationToken.None);

            Assert.Equal("unknown:payload",
                Encoding.UTF8.GetString(response.ToArray()));
        }

        [Fact]
        public async Task NullDelegateRejectsUnknownMethodAsync()
        {
            using var server = CreateServer();

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await server.InvokeAsync("unknown",
                    new ReadOnlySequence<byte>("{}"u8.ToArray()),
                    ContentMimeType.Json, CancellationToken.None));
        }

        [Fact]
        public async Task ChunkInvokerRejectsUnknownContinuationHandleAsync()
        {
            using var server = CreateServer();
            var request = new MethodChunkModel
            {
                Handle = "missing",
                Payload = []
            };

            var exception = await Assert.ThrowsAsync<MethodCallStatusException>(
                async () => await InvokeChunkAsync(server, request));

            Assert.Equal(408, exception.Status);
            Assert.Contains("No handle missing", exception.Message,
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task ChunkInvokerRejectsMissingRequiredInitialFieldsAsync()
        {
            using var server = CreateServer();
            var request = new MethodChunkModel
            {
                MethodName = "echo",
                Payload = []
            };

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await InvokeChunkAsync(server, request));
        }

        [Fact]
        public async Task ChunkInvokerSerializesDelegateStatusExceptionAsync()
        {
            using var server = CreateServer();
            server.Delegate = new StatusHandler();
            var request = CreateInitialRequest("fail", "payload",
                maxChunkLength: 1024);

            var response = await InvokeChunkAsync(server, request);

            Assert.Equal(429, response.Status);
            Assert.NotNull(response.Payload);
            var exception = MethodCallStatusException.Deserialize(
                response.Payload!.Unzip());
            Assert.Equal(429, exception.Details.Status);
            Assert.Equal("too many", exception.Details.Detail);
        }

        [Fact]
        public async Task ChunkInvokerSerializesUnexpectedDelegateExceptionAsync()
        {
            using var server = CreateServer();
            server.Delegate = new ThrowingHandler();
            var request = CreateInitialRequest("throw", "payload",
                maxChunkLength: 1024);

            var response = await InvokeChunkAsync(server, request);

            Assert.Equal(500, response.Status);
            Assert.NotNull(response.Payload);
            var exception = MethodCallStatusException.Deserialize(
                response.Payload!.Unzip());
            Assert.Equal(500, exception.Details.Status);
            Assert.Equal("boom", exception.Details.Detail);
        }

        [Fact]
        public async Task ChunkInvokerReassemblesUploadContinuationsAsync()
        {
            using var server = CreateServer();
            server.Delegate = new EchoHandler("mount");
            var compressed = Encoding.UTF8.GetBytes("payload").Zip();
            var first = compressed[..3];
            var second = compressed[3..];

            var start = await InvokeChunkAsync(server, new MethodChunkModel
            {
                MethodName = "echo",
                ContentType = ContentMimeType.Json,
                ContentLength = compressed.Length,
                MaxChunkLength = 1024,
                Payload = first
            });

            Assert.NotNull(start.Handle);
            Assert.Null(start.Payload);

            var response = await InvokeChunkAsync(server, new MethodChunkModel
            {
                Handle = start.Handle,
                Payload = second
            });

            Assert.Null(response.Handle);
            Assert.Equal("echo:payload",
                Encoding.UTF8.GetString(response.Payload!.Unzip()));
        }

        [Fact]
        public async Task ChunkInvokerSplitsLargeResponseIntoContinuationsAsync()
        {
            using var server = CreateServer();
            var expected = Enumerable.Range(0, 4096).Select(i => (byte)(i % 251))
                .ToArray();
            server.Delegate = new FixedResponseHandler(expected);
            var request = CreateInitialRequest("large", "request", maxChunkLength: 64);

            var first = await InvokeChunkAsync(server, request);

            Assert.NotNull(first.Handle);
            Assert.NotNull(first.ContentLength);
            Assert.NotNull(first.Payload);

            var compressed = first.Payload!.ToList();
            var next = first;
            while (next.Handle != null)
            {
                next = await InvokeChunkAsync(server, new MethodChunkModel
                {
                    Handle = next.Handle
                });
                if (next.Payload != null)
                {
                    compressed.AddRange(next.Payload);
                }
            }

            Assert.Equal(expected, compressed.ToArray().Unzip());
        }

        private static ChunkMethodServer CreateServer()
        {
            return new ChunkMethodServer(NullLogger.Instance,
                TimeSpan.FromMinutes(1), TimeProvider.System);
        }

        private static MethodChunkModel CreateInitialRequest(string method,
            string payload, int maxChunkLength)
        {
            var compressed = Encoding.UTF8.GetBytes(payload).Zip();
            return new MethodChunkModel
            {
                MethodName = method,
                ContentType = ContentMimeType.Json,
                ContentLength = compressed.Length,
                MaxChunkLength = maxChunkLength,
                Payload = compressed
            };
        }

        private static async Task<MethodChunkModel> InvokeChunkAsync(
            ChunkMethodServer server, MethodChunkModel request)
        {
            var response = await server.InvokeAsync(MethodNames.Call,
                new ReadOnlySequence<byte>(Json.SerializeToMemory(request,
                    CoreJsonContext.Default.MethodChunkModel).ToArray()),
                ContentMimeType.Json, CancellationToken.None);
            return Json.Deserialize(response.First,
                CoreJsonContext.Default.MethodChunkModel)!;
        }

        private sealed class EchoInvoker : IMethodInvoker
        {
            public string MethodName { get; }

            public EchoInvoker(string methodName)
            {
                MethodName = methodName;
            }

            public ValueTask<ReadOnlyMemory<byte>> InvokeAsync(
                ReadOnlyMemory<byte> payload, string contentType,
                IRpcHandler context, CancellationToken ct)
            {
                return ValueTask.FromResult(payload);
            }
        }

        private sealed class EchoHandler : IRpcHandler
        {
            public string MountPoint { get; }

            public EchoHandler(string mountPoint)
            {
                MountPoint = mountPoint;
            }

            public ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
                ReadOnlySequence<byte> payload, string contentType, CancellationToken ct)
            {
                var response = Encoding.UTF8.GetBytes(method + ":" +
                    Encoding.UTF8.GetString(payload.ToArray()));
                return ValueTask.FromResult(new ReadOnlySequence<byte>(response));
            }
        }

        private sealed class StatusHandler : IRpcHandler
        {
            public string MountPoint => string.Empty;

            public ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
                ReadOnlySequence<byte> payload, string contentType, CancellationToken ct)
            {
                throw new MethodCallStatusException(429, "too many", "Too Many");
            }
        }

        private sealed class FixedResponseHandler : IRpcHandler
        {
            public string MountPoint => string.Empty;

            public FixedResponseHandler(byte[] response)
            {
                _response = response;
            }

            public ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
                ReadOnlySequence<byte> payload, string contentType, CancellationToken ct)
            {
                return ValueTask.FromResult(new ReadOnlySequence<byte>(_response));
            }

            private readonly byte[] _response;
        }

        private sealed class ThrowingHandler : IRpcHandler
        {
            public string MountPoint => string.Empty;

            public ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
                ReadOnlySequence<byte> payload, string contentType, CancellationToken ct)
            {
                throw new InvalidOperationException("boom");
            }
        }
    }
}
