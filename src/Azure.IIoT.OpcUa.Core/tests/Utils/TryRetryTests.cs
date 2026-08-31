// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Utils
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class TryTests
    {
        [Fact]
        public void OpReturnsTrueWhenActionSucceeds()
        {
            Try.Op(() => { }).Should().BeTrue();
        }

        [Fact]
        public void OpReturnsFalseWhenActionThrows()
        {
            Try.Op(() => throw new InvalidOperationException()).Should().BeFalse();
        }

        [Fact]
        public void OpTReturnsValueOnSuccessAndDefaultOnFailure()
        {
            Try.Op(() => 42).Should().Be(42);
            Try.Op<string>(() => throw new InvalidOperationException()).Should().BeNull();
        }

        [Fact]
        public async Task AsyncReturnsFalseWhenTaskFaults()
        {
            var ok = await Try.Async(() => Task.FromException(new InvalidOperationException()));
            ok.Should().BeFalse();
        }

        [Fact]
        public async Task AsyncReturnsTrueWhenTaskSucceeds()
        {
            var ok = await Try.Async(() => Task.CompletedTask);

            Assert.Equal(true, ok);
        }

        [Fact]
        public async Task AsyncTReturnsValueOnSuccessAndDefaultOnFailure()
        {
            var success = await Try.Async(() => Task.FromResult("value"));
            var failure = await Try.Async<string>(() =>
                Task.FromException<string>(new InvalidOperationException()));

            Assert.Equal("value", success);
            Assert.Null(failure);
        }
    }

    public sealed class RetryTests
    {
        [Fact]
        public async Task DoRetriesUntilWorkSucceeds()
        {
            var attempts = 0;
            await Retry.Do(null, CancellationToken.None,
                () =>
                {
                    attempts++;
                    if (attempts < 3)
                    {
                        throw new InvalidOperationException("transient");
                    }
                    return Task.CompletedTask;
                },
                _ => true, (_, _) => 0, maxRetry: 5);

            attempts.Should().Be(3);
        }

        [Fact]
        public async Task DoGivesUpAndRethrowsAfterMaxRetryForNonTransient()
        {
            var attempts = 0;
            var act = async () => await Retry.Do(null, CancellationToken.None,
                () =>
                {
                    attempts++;
                    throw new InvalidOperationException("nope");
                },
                _ => true, (_, _) => 0, maxRetry: 2);

            await act.Should().ThrowAsync<InvalidOperationException>();
            attempts.Should().Be(3); // initial + 2 retries
        }

        [Fact]
        public async Task DoKeepsRetryingTransientExceptionsBeyondMaxRetry()
        {
            var attempts = 0;
            await Retry.Do(null, CancellationToken.None,
                () =>
                {
                    attempts++;
                    if (attempts < 4)
                    {
                        throw new TemporarilyBusyException("busy");
                    }
                    return Task.CompletedTask;
                },
                _ => false, (_, _) => 0, maxRetry: 1);

            // cont() returns false and maxRetry is 1, but ITransientException
            // keeps the loop going until the work finally succeeds.
            attempts.Should().Be(4);
        }

        [Fact]
        public async Task DoThrowsTaskCanceledExceptionBeforeWorkWhenCanceledAsync()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var called = false;

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await Retry.Do(null, cts.Token,
                    () =>
                    {
                        called = true;
                        return Task.CompletedTask;
                    },
                    _ => true, (_, _) => 0, maxRetry: 1));
            Assert.False(called);
        }

        [Fact]
        public async Task DoLogsRetryAndGiveUpWhenLoggerIsEnabledAsync()
        {
            var logger = new RecordingLogger(LogLevel.Trace);
            var attempts = 0;

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Retry.Do(logger, CancellationToken.None,
                    () =>
                    {
                        attempts++;
                        throw new InvalidOperationException("fail");
                    },
                    _ => true, (_, _) => 0, maxRetry: 1));

            Assert.Equal(2, attempts);
            Assert.Contains(logger.Events, e => e.LogLevel == LogLevel.Trace);
        }

        [Fact]
        public void BackoffPoliciesReturnConfiguredDelays()
        {
            var linearBackoffDelta = Retry.LinearBackoffDelta;
            var linearMaxRetryDelayCount = Retry.LinearMaxRetryDelayCount;
            var noBackoffDelta = Retry.NoBackoffDelta;
            try
            {
                Retry.LinearBackoffDelta = 10;
                Retry.LinearMaxRetryDelayCount = 2;
                Retry.NoBackoffDelta = 7;

                Assert.Equal(10, Retry.Linear(1, new Exception()));
                Assert.Equal(20, Retry.Linear(3, new Exception()));
                Assert.Equal(7, Retry.NoBackoff(100, new Exception()));
                Assert.InRange(Retry.GetExponentialDelay(2, 100, 2), 100, 1000);
            }
            finally
            {
                Retry.LinearBackoffDelta = linearBackoffDelta;
                Retry.LinearMaxRetryDelayCount = linearMaxRetryDelayCount;
                Retry.NoBackoffDelta = noBackoffDelta;
            }
        }

        private sealed class RecordingLogger : ILogger
        {
            public List<(LogLevel LogLevel, EventId EventId)> Events { get; } = [];

            public RecordingLogger(LogLevel enabledLevel)
            {
                _enabledLevel = enabledLevel;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= _enabledLevel;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId,
                TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Events.Add((logLevel, eventId));
            }

            private readonly LogLevel _enabledLevel;
        }
    }
}
