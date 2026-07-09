// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Utils
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Retry helper class with different retry policies
    /// </summary>
#pragma warning disable CA1068 // CancellationToken parameters must come last
#pragma warning disable IDE1006 // Naming Styles
    public static partial class Retry
    {
        /// <summary>Retry count max</summary>
        public static int DefaultMaxRetryCount { get; set; } = 10;

        private static readonly Random kRand = new();

        /// <summary>
        /// Default linear policy
        /// </summary>
        public static Func<int, Exception, int> Linear => (k, _) =>
            Math.Min(k, LinearMaxRetryDelayCount) * LinearBackoffDelta;
        /// <summary>Max retry multiplier</summary>
        public static int LinearMaxRetryDelayCount { get; set; } = 20;
        /// <summary>Incremental delay</summary>
        public static int LinearBackoffDelta { get; set; } = 1000;

        /// <summary>
        /// No backoff - just wait backoff delta
        /// </summary>
        public static Func<int, Exception, int> NoBackoff => (_, _) => NoBackoffDelta;
        /// <summary>Time between retry</summary>
        public static int NoBackoffDelta { get; set; } = 1000;

        /// <summary>
        /// Helper to calculate exponential delay with jitter and max.
        /// </summary>
        /// <param name="k"></param>
        /// <param name="increment"></param>
        /// <param name="maxRetry"></param>
        public static int GetExponentialDelay(int k, int increment, int maxRetry)
        {
            if (k > maxRetry)
            {
                k = maxRetry;
            }
#pragma warning disable CA5394 // Do not use insecure randomness
            var backoff = kRand.Next((int)(increment * 0.8), (int)(increment * 1.2));
#pragma warning restore CA5394 // Do not use insecure randomness
            var exp = 0.5 * (Math.Pow(2, k) - 1);
            var result = (int)(exp * backoff);
            System.Diagnostics.Debug.Assert(result > 0);
            return result;
        }

        /// <summary>
        /// Retries a piece of work
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="policy"></param>
        /// <param name="maxRetry"></param>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task Do(ILogger? logger, CancellationToken ct, Func<Task> work,
            Func<Exception, bool> cont, Func<int, Exception, int> policy, int maxRetry)
        {
            for (var k = 1; ; k++)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                try
                {
                    await work().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    await DelayOrThrow(logger, cont, policy, maxRetry, k, ex, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="maxRetry"></param>
        public static Task WithLinearBackoff(ILogger? logger, CancellationToken ct,
            Func<Task> work, Func<Exception, bool> cont, int? maxRetry = null)
        {
            return Do(logger, ct, work, cont, Linear, maxRetry ?? DefaultMaxRetryCount);
        }

        /// <summary>
        /// Helper to run the delay policy and output additional information.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="policy"></param>
        /// <param name="maxRetry"></param>
        /// <param name="k"></param>
        /// <param name="ex"></param>
        /// <param name="ct"></param>
        private static async Task DelayOrThrow(ILogger? logger, Func<Exception, bool> cont,
            Func<int, Exception, int> policy, int maxRetry, int k, Exception ex,
            CancellationToken ct)
        {
            if ((k > maxRetry || !cont(ex)) && ex is not ITransientException)
            {
                logger?.GiveUp(ex, k);
                throw ex;
            }
            var delay = policy(k, ex);
            Log(logger, k, delay, ex);
            if (delay != 0)
            {
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="retry"></param>
        /// <param name="delay"></param>
        /// <param name="ex"></param>
        private static void Log(ILogger? logger, int retry, int delay, Exception ex)
        {
            if (logger != null)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.RetryWithException(ex, retry, delay);
                }
                else
                {
                    logger.RetryWithoutException(retry, delay);
                }
            }
        }

        [LoggerMessage(EventId = 1000, Level = LogLevel.Trace,
            Message = "Retry {Times} in {Delay} ms...", SkipEnabledCheck = true)]
        private static partial void RetryWithException(this ILogger logger,
            Exception exception, int times, int delay);

        [LoggerMessage(EventId = 1001, Level = LogLevel.Debug,
            Message = "... Retry {Times} in {Delay} ms...")]
        private static partial void RetryWithoutException(
            this ILogger logger, int times, int delay);

        [LoggerMessage(EventId = 1002, Level = LogLevel.Trace,
           Message = "Give up after {Tries}")]
        private static partial void GiveUp(this ILogger logger, Exception ex, int tries);
    }

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1068 // CancellationToken parameters must come last
}
