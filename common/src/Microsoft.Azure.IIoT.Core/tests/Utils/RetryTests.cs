// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils.Tests
{
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Http.Exceptions;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for Microsoft.Azure.IIoT.Utils.Retry utility.
    /// </summary>
    public class RetryTests
    {
        [Fact]
        public async Task TestWithExponentialBackoffAsync()
        {
            var loggerMock = new Mock<ILogger>();
            var logger = loggerMock.Object;

            // Lambda just returns, so no exception and single passthrough.
            {
                const int maxRetryCount = 5;
                var retryCounter = 0;

                await Retry2.WithExponentialBackoffAsync(
                    logger,
                    () => ++retryCounter,
                    maxRetryCount
                ).ConfigureAwait(false);
                Assert.Equal(1, retryCounter);
            }
            // Lambda throws exception which is not covered by default continuation condition,
            // so the exception is propagated to the caller.
            {
                const int maxRetryCount = 5;
                var retryCounter = 0;

                await Assert.ThrowsAsync<ArgumentException>(() => Retry2.WithExponentialBackoffAsync(
                    logger,
                    () =>
                    {
                        ++retryCounter;
                        throw new ArgumentException("Test");
                    },
                    maxRetryCount)
                ).ConfigureAwait(false);
                Assert.Equal(1, retryCounter);
            }
            // Lambda throws exception which is covered by default continuation condition,
            // so utility will keep retrying untill it exceeds maxRetryCount.
            {
                const int maxRetryCount = 5;
                var retryCounter = 0;

                await Assert.ThrowsAsync<HttpTransientException>(() => Retry2.WithExponentialBackoffAsync(
                    logger,
                    () =>
                    {
                        ++retryCounter;
                        throw new HttpTransientException(HttpStatusCode.InternalServerError);
                    },
                    maxRetryCount)
                ).ConfigureAwait(false);
                Assert.Equal(6, retryCounter);
            }
        }

        [Fact]
        public async Task TestWithExponentialBackoffCTAsync()
        {
            var loggerMock = new Mock<ILogger>();
            var logger = loggerMock.Object;

            // Normal return will not cause TaskCanceledException.
            {
                var cts = new CancellationTokenSource();
                const int maxRetryCount = 5;
                var retryCounter = 0;

                await Retry2.WithExponentialBackoffAsync(
                    logger,
                    () =>
                    {
                        ++retryCounter;
                        cts.Cancel();
                    },
                    cts.Token,
                    maxRetryCount
                ).ConfigureAwait(false);
                Assert.Equal(1, retryCounter);
            }
            // Lambda throws exception which is not covered by default continuation condition,
            // so the exception is propagated to the caller.
            {
                var cts = new CancellationTokenSource();
                const int maxRetryCount = 5;
                var retryCounter = 0;

                await Assert.ThrowsAsync<ArgumentException>(() => Retry2.WithExponentialBackoffAsync(
                    logger,
                    () =>
                    {
                        ++retryCounter;
                        cts.Cancel();
                        throw new ArgumentException("Test");
                    },
                    cts.Token,
                    maxRetryCount)
                ).ConfigureAwait(false);
                Assert.Equal(1, retryCounter);
            }
            // Lambda throws exception which is covered by default continuation condition,
            // so utility will keep retrying untill cancelation.
            {
                var cts = new CancellationTokenSource();
                const int maxRetryCount = 5;
                var retryCounter = 0;

                await Assert.ThrowsAsync<TaskCanceledException>(() => Retry2.WithExponentialBackoffAsync(
                    logger,
                    () =>
                    {
                        ++retryCounter;
                        if (retryCounter == 3)
                        {
                            cts.Cancel();
                        }
                        throw new HttpTransientException(HttpStatusCode.InternalServerError);
                    },
                    cts.Token,
                    maxRetryCount)
                ).ConfigureAwait(false);
                Assert.Equal(3, retryCounter);
            }
        }
    }
}
