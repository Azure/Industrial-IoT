// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class OpcUaStackKeySetLoggerTests : IDisposable
    {
        private readonly string _testFolder = Path.Combine("D:\\buildtemp", "OpcUaKeySetLoggerTests",
            Guid.NewGuid().ToString("N"));
        private static readonly ILogger<OpcUaStackKeySetLogger> kLogger =
            NullLogger<OpcUaStackKeySetLogger>.Instance;

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testFolder))
                {
                    Directory.Delete(_testFolder, true);
                }
            }
            catch { }
        }

        [Fact]
        public Task StartAsyncIsNoOp()
        {
            var diagnostics = Mock.Of<IClientDiagnostics>();
            using var sut = CreateNullFolderSut(diagnostics);
            return sut.StartAsync(CancellationToken.None);
        }

        [Fact]
        public Task StopAsyncIsNoOp()
        {
            var diagnostics = Mock.Of<IClientDiagnostics>();
            using var sut = CreateNullFolderSut(diagnostics);
            return sut.StopAsync(CancellationToken.None);
        }

        [Fact]
        public void ConstructorWithoutFolderNeverCallsDiagnosticsWatcher()
        {
            var diagnostics = new Mock<IClientDiagnostics>();
            using var sut = CreateNullFolderSut(diagnostics.Object);

            diagnostics.Verify(
                x => x.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void DisposeWithNullFolderCompletesCleanly()
        {
            var diagnostics = Mock.Of<IClientDiagnostics>();
            var sut = CreateNullFolderSut(diagnostics);

            var ex = Record.Exception(() => sut.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public async Task WriteDebugFileAsyncWithEmptyEnumerableWritesNothing()
        {
            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerableEmpty<ChannelDiagnosticModel>);

            using var sut = CreateNullFolderSut(diagnostics.Object);

            Directory.CreateDirectory(_testFolder);
            await sut.WriteDebugFileAsync(_testFolder, CancellationToken.None).ConfigureAwait(false);

            // No opcua_debug folder should be created when there are no changes
            Assert.False(Directory.Exists(Path.Combine(_testFolder, "opcua_debug")));
        }

        [Fact]
        public async Task WriteDebugFileAsyncWithNullClientSkipsFileWrite()
        {
            var change = new ChannelDiagnosticModel
            {
                TimeStamp = DateTimeOffset.UtcNow,
                Connection = new ConnectionModel
                {
                    Endpoint = new EndpointModel { Url = "opc.tcp://test:4840" }
                },
                // Client == null → WriteDebugLogFileAsync returns early
                Client = null,
                Server = null
            };

            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => AsyncEnumerableFrom(change, ct));

            using var sut = CreateNullFolderSut(diagnostics.Object);

            Directory.CreateDirectory(_testFolder);
            await sut.WriteDebugFileAsync(_testFolder, CancellationToken.None).ConfigureAwait(false);

            Assert.False(Directory.Exists(Path.Combine(_testFolder, "opcua_debug")));
        }

        [Fact]
        public async Task WriteDebugFileAsyncWithCompleteChangeWritesKeysetAndLog()
        {
            var change = new ChannelDiagnosticModel
            {
                TimeStamp = DateTimeOffset.UtcNow,
                Connection = new ConnectionModel
                {
                    Endpoint = new EndpointModel
                    {
                        Url = "opc.tcp://test:4840",
                        SecurityMode = SecurityMode.SignAndEncrypt
                    }
                },
                SessionCreated = DateTimeOffset.UtcNow,
                RemotePort = 4840,
                RemoteIpAddress = "127.0.0.1",
                LocalIpAddress = "127.0.0.1",
                LocalPort = 12345,
                ChannelId = 1u,
                TokenId = 1u,
                SessionId = "session1",
                Client = new ChannelKeyModel
                {
                    Iv = [1, 2, 3, 4],
                    Key = [5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
                    SigLen = 32
                },
                Server = new ChannelKeyModel
                {
                    Iv = [17, 18, 19, 20],
                    Key = [21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32],
                    SigLen = 16
                }
            };

            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => AsyncEnumerableFrom(change, ct));

            using var sut = CreateNullFolderSut(diagnostics.Object);
            Directory.CreateDirectory(_testFolder);
            await sut.WriteDebugFileAsync(_testFolder, CancellationToken.None).ConfigureAwait(false);

            var logFile = Path.Combine(_testFolder, "opcua_debug", "log.md");
            Assert.True(File.Exists(logFile), $"log.md was not created at {logFile}");

            var logContent = await File.ReadAllTextAsync(logFile).ConfigureAwait(false);
            Assert.Contains("opc.tcp://test:4840", logContent);
            Assert.Contains("4840", logContent);

            var keysetFiles = Directory.GetFiles(_testFolder, "opcua_debug.txt",
                SearchOption.AllDirectories);
            Assert.Single(keysetFiles);

            var keyContent = await File.ReadAllTextAsync(keysetFiles[0]).ConfigureAwait(false);
            Assert.Contains("client_iv_1_1:", keyContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("server_siglen_1_1: 16", keyContent);
        }

        [Fact]
        public async Task WriteDebugFileAsyncDeletesExistingDebugFolder()
        {
            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerableEmpty<ChannelDiagnosticModel>);

            using var sut = CreateNullFolderSut(diagnostics.Object);
            Directory.CreateDirectory(_testFolder);

            // Pre-create the opcua_debug folder with a marker file
            var debugFolder = Path.Combine(_testFolder, "opcua_debug");
            Directory.CreateDirectory(debugFolder);
            var markerFile = Path.Combine(debugFolder, "old-marker.txt");
            await File.WriteAllTextAsync(markerFile, "stale").ConfigureAwait(false);

            await sut.WriteDebugFileAsync(_testFolder, CancellationToken.None).ConfigureAwait(false);

            // The old debug folder should have been deleted
            Assert.False(File.Exists(markerFile));
        }

        [Fact]
        public async Task WriteDebugFileAsyncWithNullSessionCreatedSkipsFileWrite()
        {
            var change = new ChannelDiagnosticModel
            {
                TimeStamp = DateTimeOffset.UtcNow,
                Connection = new ConnectionModel
                {
                    Endpoint = new EndpointModel { Url = "opc.tcp://test:4840" }
                },
                RemotePort = 4840,
                SessionCreated = null, // <-- this triggers the early-return guard
                Client = new ChannelKeyModel { Iv = [1], Key = [2], SigLen = 16 },
                Server = new ChannelKeyModel { Iv = [3], Key = [4], SigLen = 16 }
            };

            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => AsyncEnumerableFrom(change, ct));

            using var sut = CreateNullFolderSut(diagnostics.Object);
            Directory.CreateDirectory(_testFolder);
            await sut.WriteDebugFileAsync(_testFolder, CancellationToken.None).ConfigureAwait(false);

            Assert.False(Directory.Exists(Path.Combine(_testFolder, "opcua_debug")));
        }

        [Fact]
        public async Task WriteDebugFileAsyncWithNullRemotePortSkipsFileWrite()
        {
            var change = new ChannelDiagnosticModel
            {
                TimeStamp = DateTimeOffset.UtcNow,
                Connection = new ConnectionModel
                {
                    Endpoint = new EndpointModel { Url = "opc.tcp://test:4840" }
                },
                RemotePort = null, // <-- triggers the early-return guard
                SessionCreated = DateTimeOffset.UtcNow,
                Client = new ChannelKeyModel { Iv = [1], Key = [2], SigLen = 16 },
                Server = new ChannelKeyModel { Iv = [3], Key = [4], SigLen = 16 }
            };

            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => AsyncEnumerableFrom(change, ct));

            using var sut = CreateNullFolderSut(diagnostics.Object);
            Directory.CreateDirectory(_testFolder);
            await sut.WriteDebugFileAsync(_testFolder, CancellationToken.None).ConfigureAwait(false);

            Assert.False(Directory.Exists(Path.Combine(_testFolder, "opcua_debug")));
        }

        [Fact]
        public async Task WriteDebugFileAsyncCatchesExceptionFromInvalidPath()
        {
            // Drive a valid change but with a folder that will cause
            // Directory.CreateDirectory to throw (invalid path chars on Windows)
            var change = new ChannelDiagnosticModel
            {
                TimeStamp = DateTimeOffset.UtcNow,
                Connection = new ConnectionModel
                {
                    Endpoint = new EndpointModel { Url = "opc.tcp://test:4840" }
                },
                RemotePort = 4840,
                SessionCreated = DateTimeOffset.UtcNow,
                Client = new ChannelKeyModel { Iv = [1], Key = [2], SigLen = 16 },
                Server = new ChannelKeyModel { Iv = [3], Key = [4], SigLen = 16 }
            };

            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => AsyncEnumerableFrom(change, ct));

            using var sut = CreateNullFolderSut(diagnostics.Object);
            // Pass an invalid folder path to cause the WriteDebugLogFileAsync to throw
            // The catch block in WriteDebugFileAsync should swallow this exception
            var invalidPath = "?:\0invalid";
            var ex = await Record.ExceptionAsync(async () =>
                await sut.WriteDebugFileAsync(invalidPath, CancellationToken.None)
                    .ConfigureAwait(false)).ConfigureAwait(false);
            Assert.Null(ex); // Exception is swallowed by the catch block
        }

        [Fact]
        public void ConstructorWithFolderStartsBackgroundTaskAndDisposeCompletesCleanly()
        {
            // Mock blocks until cancellation — simulates a running background task
            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => AsyncEnumerableUntilCancelled<ChannelDiagnosticModel>(ct));

            var sut = CreateFolderSut(diagnostics.Object);

            // Dispose should cancel the background task and not throw
            var ex = Record.Exception(() => sut.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public async Task ConstructorWithFolderAndEmptyEnumerableCompletesWithoutDisposeAsync()
        {
            // Mock returns empty enumerable — task completes before dispose is called
            var diagnostics = new Mock<IClientDiagnostics>();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerableEmpty<ChannelDiagnosticModel>);

            using var sut = CreateFolderSut(diagnostics.Object);

            // Allow the background task to complete
            await Task.Delay(50).ConfigureAwait(false);

            // Dispose should succeed even though the background task has already completed
            var ex = Record.Exception(() => sut.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void ConstructorWithFolderCallsWatchChannelDiagnosticsAsyncOnce()
        {
            var diagnostics = new Mock<IClientDiagnostics>();
            var tcs = new TaskCompletionSource();
            diagnostics
                .Setup(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) =>
                {
                    tcs.TrySetResult();
                    return AsyncEnumerableUntilCancelled<ChannelDiagnosticModel>(ct);
                });

            using var sut = CreateFolderSut(diagnostics.Object);

            // Wait for the background task to call WatchChannelDiagnosticsAsync
            tcs.Task.Wait(TimeSpan.FromSeconds(5));

            diagnostics.Verify(d => d.WatchChannelDiagnosticsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private OpcUaStackKeySetLogger CreateNullFolderSut(IClientDiagnostics diagnostics)
        {
            var options = Options.Create(new OpcUaClientOptions
            {
                OpcUaKeySetLogFolderName = null
            });
            return new OpcUaStackKeySetLogger(options, diagnostics, kLogger);
        }

        private OpcUaStackKeySetLogger CreateFolderSut(IClientDiagnostics diagnostics)
        {
            Directory.CreateDirectory(_testFolder);
            var options = Options.Create(new OpcUaClientOptions
            {
                OpcUaKeySetLogFolderName = _testFolder
            });
            return new OpcUaStackKeySetLogger(options, diagnostics, kLogger);
        }

        private static async IAsyncEnumerable<T> AsyncEnumerableEmpty<T>(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }

        private static async IAsyncEnumerable<T> AsyncEnumerableFrom<T>(T item,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield return item;
        }

        private static async IAsyncEnumerable<T> AsyncEnumerableUntilCancelled<T>(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
            yield break;
        }
    }
}
