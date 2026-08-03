// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Servers
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class FileSystemRpcServerTests : IDisposable
    {
        public FileSystemRpcServerTests()
        {
            _root = Path.Combine(Directory.GetCurrentDirectory(),
                "FileSystemRpcServerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Fact]
        public async Task ConnectAsyncAddsHandlerAndDisposeRemovesItAsync()
        {
            await using var server = CreateServer(out _, out _, out _);
            var handler = new TestRpcHandler();

            await using var registration = await server.ConnectAsync(handler)
                .ConfigureAwait(false);

            Assert.Same(handler, Assert.Single(server.Connected));

            await registration.DisposeAsync().ConfigureAwait(false);

            Assert.Empty(server.Connected);
        }

        [Fact]
        public async Task ChangedRequestFileIsParsedAndWrittenToResponseFileAsync()
        {
            await using var server = CreateServer(out var factory, out var requestPath,
                out var responsePath);
            var handler = new TestRpcHandler(response: """{"ok":true}""");
            await server.ConnectAsync(handler).ConfigureAwait(false);
            server.Start();
            await File.WriteAllTextAsync(requestPath, """
                Echo
                Content-Type: application/json

                {"value":1}
                """).ConfigureAwait(false);

            var responseProvider = factory.GetProvider(Path.GetDirectoryName(responsePath)!);
            var responseWritten = responseProvider.GetWriteTask(
                Path.GetFileName(responsePath));
            factory.GetProvider(Path.GetDirectoryName(requestPath)!).TriggerChange();
            // Wait for the response stream to close before disposal cancels writes.
            await responseWritten.ConfigureAwait(false);
            await server.DisposeAsync().ConfigureAwait(false);

            Assert.Equal("Echo", handler.Calls[0].Method);
            Assert.Equal("application/json", handler.Calls[0].ContentType);
            Assert.Equal("""{"value":1}""",
                Encoding.UTF8.GetString(handler.Calls[0].Payload));
            Assert.Contains("200",
                await File.ReadAllTextAsync(responsePath).ConfigureAwait(false));
            Assert.Contains("""{"ok":true}""",
                await File.ReadAllTextAsync(responsePath).ConfigureAwait(false));
        }

        [Fact]
        public async Task HttpUriRequestsReturnNotImplementedWithoutInvokingHandlerAsync()
        {
            await using var server = CreateServer(out var factory, out var requestPath,
                out var responsePath);
            var handler = new TestRpcHandler(response: """{"ok":true}""");
            await server.ConnectAsync(handler).ConfigureAwait(false);
            await File.WriteAllTextAsync(requestPath, """
                POST http://localhost/rpc HTTP/1.1

                """).ConfigureAwait(false);

            server.Start();
            var responseProvider = factory.GetProvider(Path.GetDirectoryName(responsePath)!);
            var responseWritten = responseProvider.GetWriteTask(
                Path.GetFileName(responsePath));
            factory.GetProvider(Path.GetDirectoryName(requestPath)!).TriggerChange();
            await responseWritten.ConfigureAwait(false);
            await server.DisposeAsync().ConfigureAwait(false);

            Assert.Empty(handler.Calls);
            Assert.Contains("501",
                await File.ReadAllTextAsync(responsePath).ConfigureAwait(false));
        }

        [Fact]
        public async Task MissingContentTypeDefaultsToApplicationJsonAsync()
        {
            await using var server = CreateServer(out var factory, out var requestPath,
                out _);
            var handler = new TestRpcHandler();
            await server.ConnectAsync(handler).ConfigureAwait(false);
            await File.WriteAllTextAsync(requestPath, """
                Echo

                {"value":1}
                """).ConfigureAwait(false);

            server.Start();
            factory.GetProvider(Path.GetDirectoryName(requestPath)!).TriggerChange();
            await handler.Invoked.ConfigureAwait(false);
            await server.DisposeAsync().ConfigureAwait(false);

            Assert.Equal("application/json", handler.Calls[0].ContentType);
        }

        [Fact]
        public async Task NotSupportedHandlerFallsThroughToNextHandlerAsync()
        {
            await using var server = CreateServer(out var factory, out var requestPath,
                out _);
            var first = new TestRpcHandler(exception: new NotSupportedException());
            var second = new TestRpcHandler(response: """{"handled":true}""");
            await server.ConnectAsync(first).ConfigureAwait(false);
            await server.ConnectAsync(second).ConfigureAwait(false);
            await File.WriteAllTextAsync(requestPath, """
                Echo

                """).ConfigureAwait(false);

            server.Start();
            factory.GetProvider(Path.GetDirectoryName(requestPath)!).TriggerChange();
            await second.Invoked.ConfigureAwait(false);
            await server.DisposeAsync().ConfigureAwait(false);

            Assert.Single(first.Calls);
            Assert.Single(second.Calls);
        }

        [Fact]
        public async Task MethodCallStatusExceptionIsWrittenAsReturnedStatusAsync()
        {
            await using var server = CreateServer(out var factory, out var requestPath,
                out var responsePath);
            var handler = new TestRpcHandler(exception:
                new MethodCallStatusException(404, "missing", "Not Found"));
            await server.ConnectAsync(handler).ConfigureAwait(false);
            await File.WriteAllTextAsync(requestPath, """
                Missing

                """).ConfigureAwait(false);

            server.Start();
            var responseProvider = factory.GetProvider(Path.GetDirectoryName(responsePath)!);
            var responseWritten = responseProvider.GetWriteTask(
                Path.GetFileName(responsePath));
            factory.GetProvider(Path.GetDirectoryName(requestPath)!).TriggerChange();
            // Wait for the response stream to close before disposal cancels writes.
            await responseWritten.ConfigureAwait(false);
            await server.DisposeAsync().ConfigureAwait(false);

            var response = await File.ReadAllTextAsync(responsePath)
                .ConfigureAwait(false);
            Assert.Contains("404", response);
            Assert.Contains("missing", response);
        }

        [Fact]
        public async Task GenericHandlerExceptionIsMappedToMethodNotAllowedAsync()
        {
            await using var server = CreateServer(out var factory, out var requestPath,
                out var responsePath);
            var handler = new TestRpcHandler(exception:
                new InvalidOperationException("boom"));
            await server.ConnectAsync(handler).ConfigureAwait(false);
            await File.WriteAllTextAsync(requestPath, """
                Fails

                """).ConfigureAwait(false);

            server.Start();
            var responseProvider = factory.GetProvider(Path.GetDirectoryName(responsePath)!);
            var responseWritten = responseProvider.GetWriteTask(
                Path.GetFileName(responsePath));
            factory.GetProvider(Path.GetDirectoryName(requestPath)!).TriggerChange();
            // Wait for the response stream to close before disposal cancels writes.
            await responseWritten.ConfigureAwait(false);
            await server.DisposeAsync().ConfigureAwait(false);

            Assert.Contains("405",
                await File.ReadAllTextAsync(responsePath).ConfigureAwait(false));
        }

        [Fact]
        public async Task UnsupportedMethodReturnsNotImplementedAsync()
        {
            await using var server = CreateServer(out var factory, out var requestPath,
                out var responsePath);
            var handler = new TestRpcHandler(exception: new NotSupportedException());
            await server.ConnectAsync(handler).ConfigureAwait(false);
            await File.WriteAllTextAsync(requestPath, """
                Unknown

                """).ConfigureAwait(false);

            server.Start();
            var responseProvider = factory.GetProvider(Path.GetDirectoryName(responsePath)!);
            var responseWritten = responseProvider.GetWriteTask(
                Path.GetFileName(responsePath));
            factory.GetProvider(Path.GetDirectoryName(requestPath)!).TriggerChange();
            // Wait for the response stream to close before disposal cancels writes.
            await responseWritten.ConfigureAwait(false);
            await server.DisposeAsync().ConfigureAwait(false);

            Assert.Contains("501",
                await File.ReadAllTextAsync(responsePath).ConfigureAwait(false));
        }

        private FileSystemRpcServer CreateServer(out TestFileProviderFactory factory,
            out string requestPath, out string responsePath)
        {
            requestPath = Path.Combine(_root, "rpc.req");
            responsePath = Path.Combine(_root, "rpc.resp");
            factory = new TestFileProviderFactory();
            return new FileSystemRpcServer(factory, Options.Create(
                new FileSystemRpcServerOptions
                {
                    RequestFilePath = requestPath,
                    ResponseFilePath = responsePath
                }), NullLogger<FileSystemRpcServer>.Instance);
        }

        private sealed class TestRpcHandler : IRpcHandler
        {
            public string MountPoint => string.Empty;

            public Task Invoked => _invoked.Task;

            public List<Invocation> Calls { get; } = [];

            public TestRpcHandler(string response = "", Exception? exception = null)
            {
                _response = response;
                _exception = exception;
            }

            public ValueTask<ReadOnlySequence<byte>> InvokeAsync(string method,
                ReadOnlySequence<byte> payload, string contentType,
                CancellationToken ct = default)
            {
                Calls.Add(new Invocation(method, payload.ToArray(), contentType));
                _invoked.TrySetResult();
                if (_exception != null)
                {
                    return ValueTask.FromException<ReadOnlySequence<byte>>(_exception);
                }
                return ValueTask.FromResult(new ReadOnlySequence<byte>(
                    Encoding.UTF8.GetBytes(_response)));
            }

            private readonly TaskCompletionSource _invoked = new();
            private readonly string _response;
            private readonly Exception? _exception;
        }

        private sealed record class Invocation(string Method, byte[] Payload,
            string ContentType);

        private sealed class TestFileProviderFactory : IFileProviderFactory
        {
            public IFileProvider Create(string root)
            {
                root = Path.GetFullPath(root);
                if (!_providers.TryGetValue(root, out var provider))
                {
                    provider = new TestFileProvider(root);
                    _providers.Add(root, provider);
                }
                return provider;
            }

            public TestFileProvider GetProvider(string root)
            {
                return _providers[Path.GetFullPath(root)];
            }

            private readonly Dictionary<string, TestFileProvider> _providers =
                new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class TestFileProvider : IFileProvider
        {
            public TestFileProvider(string root)
            {
                _root = root;
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                return new TestFileInfo(this, Path.Combine(_root, subpath));
            }

            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                return NotFoundDirectoryContents.Singleton;
            }

            public IChangeToken Watch(string filter)
            {
                _changeToken = new ManualChangeToken();
                return _changeToken;
            }

            public void TriggerChange()
            {
                _changeToken?.Trigger();
            }

            public Task GetWriteTask(string file)
            {
                return GetWriteCompletion(file).Task;
            }

            public void NotifyWritten(string file)
            {
                GetWriteCompletion(file).TrySetResult();
            }

            private TaskCompletionSource GetWriteCompletion(string file)
            {
                file = Path.GetFileName(file);
                if (!_writes.TryGetValue(file, out var write))
                {
                    write = new TaskCompletionSource();
                    _writes.Add(file, write);
                }
                return write;
            }

            private readonly string _root;
            private readonly Dictionary<string, TaskCompletionSource> _writes =
                new(StringComparer.OrdinalIgnoreCase);
            private ManualChangeToken? _changeToken;
        }

        private sealed class TestFileInfo : IFileInfoEx
        {
            public bool Exists => File.Exists(_path);

            public long Length => Exists ? new FileInfo(_path).Length : -1;

            public string? PhysicalPath => _path;

            public string Name => Path.GetFileName(_path);

            public DateTimeOffset LastModified => Exists
                ? File.GetLastWriteTimeUtc(_path)
                : DateTimeOffset.MinValue;

            public bool IsDirectory => false;

            public bool IsWritable => true;

            public TestFileInfo(TestFileProvider provider, string path)
            {
                _provider = provider;
                _path = path;
            }

            public Stream CreateReadStream()
            {
                return File.OpenRead(_path);
            }

            public Stream CreateWriteStream()
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                return new NotifyingStream(File.Open(_path, FileMode.Create,
                    FileAccess.Write, FileShare.Read), () => _provider.NotifyWritten(Name));
            }

            public void SetLastModified(DateTimeOffset timestamp)
            {
                File.SetLastWriteTimeUtc(_path, timestamp.UtcDateTime);
            }

            public Task DeleteAsync(CancellationToken ct)
            {
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }
                return Task.CompletedTask;
            }

            private readonly TestFileProvider _provider;
            private readonly string _path;
        }

        private sealed class NotifyingStream : Stream
        {
            public override bool CanRead => _inner.CanRead;

            public override bool CanSeek => _inner.CanSeek;

            public override bool CanWrite => _inner.CanWrite;

            public override long Length => _inner.Length;

            public override long Position
            {
                get => _inner.Position;
                set => _inner.Position = value;
            }

            public NotifyingStream(Stream inner, Action notify)
            {
                _inner = inner;
                _notify = notify;
            }

            public override void Flush()
            {
                _inner.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _inner.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _inner.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
            }

            public override async ValueTask DisposeAsync()
            {
                await _inner.DisposeAsync().ConfigureAwait(false);
                _notify();
                await base.DisposeAsync().ConfigureAwait(false);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                    _notify();
                }
                base.Dispose(disposing);
            }

            private readonly Stream _inner;
            private readonly Action _notify;
        }

        private sealed class ManualChangeToken : IChangeToken
        {
            public bool HasChanged { get; private set; }

            public bool ActiveChangeCallbacks => true;

            public IDisposable RegisterChangeCallback(Action<object?> callback,
                object? state)
            {
                var registration = new Registration(_registrations, callback, state);
                _registrations.Add(registration);
                return registration;
            }

            public void Trigger()
            {
                HasChanged = true;
                foreach (var registration in _registrations.ToList())
                {
                    registration.Invoke();
                }
            }

            private sealed class Registration : IDisposable
            {
                public Registration(List<Registration> registrations,
                    Action<object?> callback, object? state)
                {
                    _registrations = registrations;
                    _callback = callback;
                    _state = state;
                }

                public void Invoke()
                {
                    _callback(_state);
                }

                public void Dispose()
                {
                    _registrations.Remove(this);
                }

                private readonly List<Registration> _registrations;
                private readonly Action<object?> _callback;
                private readonly object? _state;
            }

            private readonly List<Registration> _registrations = [];
        }

        private readonly string _root;
    }
}
