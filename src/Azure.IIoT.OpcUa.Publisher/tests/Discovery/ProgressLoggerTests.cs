// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Discovery
{
    using Azure.IIoT.OpcUa.Publisher.Discovery;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Xunit;

    public class ProgressLoggerTests
    {
        [Fact]
        public void DiscoveryLifecycleMethodsEmitExpectedProgressAndLogs()
        {
            var logger = new CapturingLogger();
            var progress = CreateProgressLogger(logger);
            var request = CreateRequest();
            var exception = new InvalidOperationException("boom");

            progress.OnDiscoveryPending(request, 7);
            progress.OnDiscoveryStarted(request);
            progress.OnDiscoveryCancelled(request);
            progress.OnDiscoveryError(request, exception);
            progress.OnDiscoveryFinished(request);

            Assert.Collection(progress.Progress,
                item =>
                {
                    Assert.Equal(DiscoveryProgressType.Pending, item.EventType);
                    Assert.Equal(7, item.Total);
                    Assert.Equal(kNow, item.TimeStamp);
                },
                item => Assert.Equal(DiscoveryProgressType.Started, item.EventType),
                item => Assert.Equal(DiscoveryProgressType.Cancelled, item.EventType),
                item =>
                {
                    Assert.Equal(DiscoveryProgressType.Error, item.EventType);
                    Assert.Equal("boom", item.Result);
                    var details = item.ResultDetails ??
                        throw new InvalidOperationException("Missing error details.");
                    Assert.Contains("InvalidOperationException", details["exception"]);
                },
                item => Assert.Equal(DiscoveryProgressType.Finished, item.EventType));
            AssertLogged(logger, LogLevel.Trace, 31);
            AssertLogged(logger, LogLevel.Information, 32);
            AssertLogged(logger, LogLevel.Information, 33);
            AssertLogged(logger, LogLevel.Error, 34);
            AssertLogged(logger, LogLevel.Information, 35);
        }

        [Fact]
        public void NetworkScanMethodsEmitExpectedCountsResultsAndLogs()
        {
            var logger = new CapturingLogger();
            var progress = CreateProgressLogger(logger);
            var request = CreateRequest();
            var address = IPAddress.Parse("127.0.0.1");

            progress.OnNetScanStarted(request, 2, 3, 10);
            progress.OnNetScanResult(request, 2, 4, 10, 1, address);
            progress.OnNetScanProgress(request, 2, 5, 10, 1);
            progress.OnNetScanFinished(request, 0, 10, 10, 1);

            Assert.Collection(progress.Progress,
                item => AssertProgress(item, DiscoveryProgressType.NetworkScanStarted, 2, 3, 10, null, null),
                item => AssertProgress(item, DiscoveryProgressType.NetworkScanResult, 2, 4, 10, 1, "127.0.0.1"),
                item => AssertProgress(item, DiscoveryProgressType.NetworkScanProgress, 2, 5, 10, 1, null),
                item => AssertProgress(item, DiscoveryProgressType.NetworkScanFinished, 0, 10, 10, 1, null));
            AssertLogged(logger, LogLevel.Information, 36);
            AssertLogged(logger, LogLevel.Information, 37);
            AssertLogged(logger, LogLevel.Information, 38);
            AssertLogged(logger, LogLevel.Information, 39);
        }

        [Fact]
        public void PortScanMethodsEmitExpectedCountsResultsAndLogs()
        {
            var logger = new CapturingLogger();
            var progress = CreateProgressLogger(logger);
            var request = CreateRequest();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 4840);

            progress.OnPortScanStart(request, 4, 5, 20);
            progress.OnPortScanResult(request, 4, 6, 20, 2, endpoint);
            progress.OnPortScanProgress(request, 4, 7, 20, 2);
            progress.OnPortScanFinished(request, 0, 20, 20, 2);

            Assert.Collection(progress.Progress,
                item => AssertProgress(item, DiscoveryProgressType.PortScanStarted, 4, 5, 20, null, null),
                item => AssertProgress(item, DiscoveryProgressType.PortScanResult, 4, 6, 20, 2, "127.0.0.1:4840"),
                item => AssertProgress(item, DiscoveryProgressType.PortScanProgress, 4, 7, 20, 2, null),
                item => AssertProgress(item, DiscoveryProgressType.PortScanFinished, 0, 20, 20, 2, null));
            AssertLogged(logger, LogLevel.Information, 40);
            AssertLogged(logger, LogLevel.Information, 41);
            AssertLogged(logger, LogLevel.Information, 42);
            AssertLogged(logger, LogLevel.Information, 43);
        }

        [Fact]
        public void ServerAndEndpointDiscoveryMethodsEmitDetailsAndLogs()
        {
            var logger = new CapturingLogger();
            var progress = CreateProgressLogger(logger);
            var request = CreateRequest();
            var url = new Uri("opc.tcp://localhost:4840");
            var address = IPAddress.Loopback;

            progress.OnServerDiscoveryStarted(request, 3, 1, 5);
            progress.OnFindEndpointsStarted(request, 3, 2, 5, 1, url, address);
            progress.OnFindEndpointsFinished(request, 3, 3, 5, 1, url, address, 2);
            progress.OnServerDiscoveryFinished(request, 0, 5, 5, 1);

            Assert.Collection(progress.Progress,
                item => AssertProgress(item, DiscoveryProgressType.ServerDiscoveryStarted, 3, 1, 5, null, null),
                item =>
                {
                    AssertProgress(item, DiscoveryProgressType.EndpointsDiscoveryStarted, 3, 2, 5, 1, null);
                    var details = item.RequestDetails ??
                        throw new InvalidOperationException("Missing request details.");
                    //
                    // The url is reported as Uri formats it, which appends the
                    // root path, rather than as it was written.
                    //
                    Assert.Equal("opc.tcp://localhost:4840/", details["url"]);
                    Assert.Equal("127.0.0.1", details["address"]);
                    Assert.NotSame(request, item.Request);
                },
                item =>
                {
                    AssertProgress(item, DiscoveryProgressType.EndpointsDiscoveryFinished, 3, 3, 5, 1, "2");
                    var details = item.RequestDetails ??
                        throw new InvalidOperationException("Missing request details.");
                    Assert.Equal("opc.tcp://localhost:4840/", details["url"]);
                    Assert.Equal("127.0.0.1", details["address"]);
                    Assert.Same(request, item.Request);
                },
                item => AssertProgress(item, DiscoveryProgressType.ServerDiscoveryFinished, 0, 5, 5, 1, null));
            AssertLogged(logger, LogLevel.Information, 44);
            AssertLogged(logger, LogLevel.Information, 45);
            AssertLogged(logger, LogLevel.Information, 47);
            AssertLogged(logger, LogLevel.Information, 48);
        }

        [Fact]
        public void FindEndpointsFinishedWithNoDiscoveredEndpointsLogsNoEndpointsBeforeFinished()
        {
            var logger = new CapturingLogger();
            var progress = CreateProgressLogger(logger);

            progress.OnFindEndpointsFinished(CreateRequest(), 1, 2, 3, 0,
                new Uri("opc.tcp://localhost:4840"), IPAddress.Loopback, 0);

            Assert.Collection(logger.Logs,
                entry => AssertLog(entry, LogLevel.Information, 46),
                entry => AssertLog(entry, LogLevel.Information, 47));
        }

        private static CapturingProgressLogger CreateProgressLogger(CapturingLogger logger)
        {
            return new CapturingProgressLogger(logger, new FixedTimeProvider(kNow));
        }

        private static DiscoveryRequestModel CreateRequest()
        {
            return new DiscoveryRequestModel
            {
                Id = "request1",
                Discovery = DiscoveryMode.Fast
            };
        }

        private static void AssertProgress(DiscoveryProgressModel progress,
            DiscoveryProgressType eventType, int? workers, int? scanned, int? total,
            int? discovered, string? result)
        {
            Assert.Equal(eventType, progress.EventType);
            Assert.Equal(workers, progress.Workers);
            Assert.Equal(scanned, progress.Progress);
            Assert.Equal(total, progress.Total);
            Assert.Equal(discovered, progress.Discovered);
            Assert.Equal(result, progress.Result);
            Assert.Equal(kNow, progress.TimeStamp);
        }

        private static void AssertLogged(CapturingLogger logger, LogLevel level, int eventId)
        {
            Assert.Contains(logger.Logs, entry =>
                entry.Level == level && entry.EventId == eventId);
        }

        private static void AssertLog(LogEntry entry, LogLevel level, int eventId)
        {
            Assert.Equal(level, entry.Level);
            Assert.Equal(eventId, entry.EventId);
        }

        private static readonly DateTimeOffset kNow =
            new(2026, 8, 3, 15, 59, 41, TimeSpan.Zero);

        private sealed class CapturingProgressLogger : ProgressLogger
        {
            public CapturingProgressLogger(ILogger logger, TimeProvider timeProvider) :
                base(logger, timeProvider)
            {
            }

            public List<DiscoveryProgressModel> Progress { get; } = [];

            protected override void Send(DiscoveryProgressModel progress)
            {
                base.Send(progress);
                Progress.Add(progress);
            }
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

        private sealed class FixedTimeProvider : TimeProvider
        {
            public FixedTimeProvider(DateTimeOffset utcNow)
            {
                _utcNow = utcNow;
            }

            public override DateTimeOffset GetUtcNow()
            {
                return _utcNow;
            }

            private readonly DateTimeOffset _utcNow;
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
