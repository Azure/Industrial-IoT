// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Utils
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using FluentAssertions;
    using System;
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
    }
}
