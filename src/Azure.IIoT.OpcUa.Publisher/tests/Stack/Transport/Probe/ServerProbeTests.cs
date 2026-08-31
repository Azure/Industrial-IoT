// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Probe
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Probe;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using Xunit;

    public class ServerProbeTests
    {
        [Fact]
        public void CreatedProbeCompletesFailedSocketOperationWithoutOpeningSocket()
        {
            var logger = new CapturingLogger();
            using var probe = new ServerProbe(logger)
            {
                Timeout = TimeSpan.FromMilliseconds(1234)
            }.Create();
            using var args = new SocketAsyncEventArgs
            {
                SocketError = SocketError.ConnectionRefused
            };

            var completed = probe.OnComplete(5, args, out var ok, out var timeout);

            Assert.True(completed);
            Assert.False(ok);
            Assert.Equal(1234, timeout);
            var entry = Assert.Single(logger.Logs);
            Assert.Equal(LogLevel.Debug, entry.Level);
            Assert.Equal(1601, entry.EventId);
            Assert.False(probe.Reset());
        }

        [Fact]
        public void CreatedProbeCompletesWhenSuccessfulArgsHaveNoConnectedSocket()
        {
            var logger = new CapturingLogger();
            using var probe = new ServerProbe(logger)
            {
                Timeout = TimeSpan.FromMilliseconds(250)
            }.Create();
            using var args = new SocketAsyncEventArgs
            {
                SocketError = SocketError.Success
            };

            var completed = probe.OnComplete(7, args, out var ok, out var timeout);

            Assert.True(completed);
            Assert.False(ok);
            Assert.Equal(250, timeout);
            var entry = Assert.Single(logger.Logs);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Equal(1602, entry.EventId);
            Assert.False(probe.Reset());
        }

        private sealed class CapturingLogger : ILogger
        {
            public List<LogEntry> Logs { get; } = [];

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Logs.Add(new LogEntry(logLevel, eventId.Id, formatter(state, exception)));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }

        private sealed record LogEntry(LogLevel Level, int EventId, string Message);
    }
}
