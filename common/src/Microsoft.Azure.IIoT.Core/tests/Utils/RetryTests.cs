// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Core.Tests.Utils {
    using Microsoft.Azure.IIoT.Http.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Moq;
    using Serilog;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for Microsoft.Azure.IIoT.Utils.Retry utility.
    /// </summary>
    public class RetryTests {

        [Fact]
        public async Task TestWithExponentialBackoffAsync() {
            var loggerMock = new Mock<ILogger>();
            var logger = loggerMock.Object;

            // Lambda just returns, so no exception and single passthrough.
            {
                var maxRetryCount = 5;
                var retryCounter = 0;

                await Retry.WithExponentialBackoff(
                    logger,
                    () => { ++retryCounter; },
                    maxRetryCount
                );
                Assert.Equal(1, retryCounter);
            }
            // Lambda throws exception which is not covered by default continuation condition,
            // so the exception is propagated to the caller.
            {
                var maxRetryCount = 5;
                var retryCounter = 0;

                await Assert.ThrowsAsync<ArgumentException>(() => Retry.WithExponentialBackoff(
                    logger,
                    () => {
                        ++retryCounter;
                        throw new ArgumentException("Test");
                    },
                    maxRetryCount)
                );
                Assert.Equal(1, retryCounter);
            }
            // Lambda throws exception which is covered by default continuation condition,
            // so utility will keep retrying untill it exceeds maxRetryCount.
            {
                var maxRetryCount = 5;
                var retryCounter = 0;

                await Assert.ThrowsAsync<HttpTransientException>(() => Retry.WithExponentialBackoff(
                    logger,
                    () => {
                        ++retryCounter;
                        throw new HttpTransientException(HttpStatusCode.InternalServerError);
                    },
                    maxRetryCount)
                );
                Assert.Equal(6, retryCounter);
            }
        }

        [Fact]
        public async Task TestWithExponentialBackoffCTAsync() {
            var loggerMock = new Mock<ILogger>();
            var logger = loggerMock.Object;

            // Normal return will not cause TaskCanceledException.
            {
                var cts = new CancellationTokenSource();
                var maxRetryCount = 5;
                var retryCounter = 0;

                await Retry.WithExponentialBackoff(
                    logger,
                    cts.Token,
                    () => {
                        ++retryCounter;
                        cts.Cancel();
                    },
                    maxRetryCount
                );
                Assert.Equal(1, retryCounter);
            }
            // Lambda throws exception which is not covered by default continuation condition,
            // so the exception is propagated to the caller.
            {
                var cts = new CancellationTokenSource();
                var maxRetryCount = 5;
                var retryCounter = 0;

                await Assert.ThrowsAsync<ArgumentException>(() => Retry.WithExponentialBackoff(
                    logger,
                    cts.Token,
                    () => {
                        ++retryCounter;
                        cts.Cancel();
                        throw new ArgumentException("Test");
                    },
                    maxRetryCount)
                );
                Assert.Equal(1, retryCounter);
            }
            // Lambda throws exception which is covered by default continuation condition,
            // so utility will keep retrying untill cancelation.
            {
                var cts = new CancellationTokenSource();
                var maxRetryCount = 5;
                var retryCounter = 0;

                await Assert.ThrowsAsync<TaskCanceledException>(() => Retry.WithExponentialBackoff(
                    logger,
                    cts.Token,
                    () => {
                        ++retryCounter;
                        if (retryCounter == 3) {
                            cts.Cancel();
                        }
                        throw new HttpTransientException(HttpStatusCode.InternalServerError);
                    },
                    maxRetryCount)
                );
                Assert.Equal(3, retryCounter);
            }
        }
    }
}
