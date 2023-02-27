// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils
{
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Retry helper class with different retry policies
    /// </summary>
    public static class Retry2
    {
        /// <summary>Retry count max</summary>
        public static int DefaultMaxRetryCount { get; set; } = 10;

        /// <summary>
        /// Default exponential policy with 20% jitter
        /// </summary>
        public static Func<int, Exception, int> Exponential => (k, ex) =>
            GetExponentialDelay(k, ExponentialBackoffIncrement, ExponentialMaxRetryCount);

        private static readonly Random kRand = new();
        /// <summary>Max retry count for exponential policy</summary>
        public static int ExponentialMaxRetryCount { get; set; } = 13;
        /// <summary>Exponential backoff increment</summary>
        public static int ExponentialBackoffIncrement { get; set; } = 10;

        /// <summary>
        /// Default linear policy
        /// </summary>
        public static Func<int, Exception, int> Linear => (k, ex) =>
            Math.Min(k, LinearMaxRetryDelayCount) * LinearBackoffDelta;
        /// <summary>Max retry multiplier</summary>
        public static int LinearMaxRetryDelayCount { get; set; } = 20;
        /// <summary>Incremental delay</summary>
        public static int LinearBackoffDelta { get; set; } = 1000;

        /// <summary>
        /// Helper to calcaulate exponential delay with jitter and max.
        /// </summary>
        /// <param name="k"></param>
        /// <param name="increment"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
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
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="policy"></param>
        /// <param name="maxRetry"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task DoAsync(ILogger logger, Func<Task> work, Func<Exception, bool> cont,
            Func<int, Exception, int> policy, int maxRetry, CancellationToken ct)
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
                    await DelayOrThrowAsync(logger, cont, policy, maxRetry, k, ex, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Retries a piece of work with return type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="policy"></param>
        /// <param name="maxRetry"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<T> DoAsync<T>(ILogger logger, Func<Task<T>> work, Func<Exception, bool> cont,
            Func<int, Exception, int> policy, int maxRetry, CancellationToken ct)
        {
            for (var k = 1; ; k++)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                try
                {
                    return await work().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await DelayOrThrowAsync(logger, cont, policy, maxRetry, k, ex, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Retries a piece of work
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="policy"></param>
        /// <param name="maxRetry"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task DoAsync(ILogger logger, Action work, Func<Exception, bool> cont,
            Func<int, Exception, int> policy, int maxRetry, CancellationToken ct)
        {
            for (var k = 1; ; k++)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                try
                {
                    work();
                    return;
                }
                catch (Exception ex)
                {
                    await DelayOrThrowAsync(logger, cont, policy, maxRetry, k, ex, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="ct"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task WithLinearBackoffAsync(ILogger logger, Func<Task> work,
            Func<Exception, bool> cont, CancellationToken ct, int? maxRetry = null)
        {
            return DoAsync(logger, work, cont, Linear, maxRetry ?? DefaultMaxRetryCount, ct);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="ct"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoffAsync(ILogger logger, Func<Task> work,
            Func<Exception, bool> cont, CancellationToken ct, int? maxRetry = null)
        {
            return DoAsync(logger, work, cont, Exponential, maxRetry ?? ExponentialMaxRetryCount, ct);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="ct"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoffAsync(ILogger logger, Func<Task> work,
            CancellationToken ct, int? maxRetry = null)
        {
            return WithExponentialBackoffAsync(logger, work, ex => ex is ITransientException, ct, maxRetry);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoffAsync(ILogger logger, Func<Task> work,
            int? maxRetry = null)
        {
            return WithExponentialBackoffAsync(logger, work, default, maxRetry);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="ct"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoffAsync<T>(ILogger logger, Func<Task<T>> work,
            Func<Exception, bool> cont, CancellationToken ct, int? maxRetry = null)
        {
            return DoAsync(logger, work, cont, Exponential, maxRetry ?? ExponentialMaxRetryCount, ct);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="ct"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoffAsync<T>(ILogger logger, Func<Task<T>> work,
            CancellationToken ct, int? maxRetry = null)
        {
            return WithExponentialBackoffAsync(logger, work, (ex) => ex is ITransientException, ct, maxRetry);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoffAsync<T>(ILogger logger, Func<Task<T>> work,
            int? maxRetry = null)
        {
            return WithExponentialBackoffAsync(logger, work, default, maxRetry);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="ct"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoffAsync(ILogger logger, Action work,
            Func<Exception, bool> cont, CancellationToken ct, int? maxRetry = null)
        {
            return DoAsync(logger, work, cont, Exponential, maxRetry ?? ExponentialMaxRetryCount, ct);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="ct"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoffAsync(ILogger logger, Action work,
            CancellationToken ct, int? maxRetry = null)
        {
            return WithExponentialBackoffAsync(logger, work, ex => ex is ITransientException, ct, maxRetry);
        }

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoffAsync(ILogger logger, Action work, int? maxRetry = null)
        {
            return WithExponentialBackoffAsync(logger, work, default, maxRetry);
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
        /// <returns></returns>
        private static async Task DelayOrThrowAsync(ILogger logger, Func<Exception, bool> cont,
            Func<int, Exception, int> policy, int maxRetry, int k, Exception ex,
            CancellationToken ct)
        {
            if (k > maxRetry || !cont(ex))
            {
                logger?.LogTrace(ex, "Give up after {Attempt}", k);
                throw ex;
            }
            if (ex is TemporarilyBusyException tbx && tbx.RetryAfter != null)
            {
                var delay = tbx.RetryAfter.Value;
                Log(logger, k, (int)delay.TotalMilliseconds, ex);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
            else
            {
                var delay = policy(k, ex);
                Log(logger, k, delay, ex);
                if (delay != 0)
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="retry"></param>
        /// <param name="delay"></param>
        /// <param name="ex"></param>
        private static void Log(ILogger logger, int retry, int delay, Exception ex)
        {
            if (logger != null)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace(ex, "Retry {Attempt} in {Delay} ms...", retry, delay);
                }
                else
                {
                    logger.LogDebug("  ... Retry {Attempt} in {Delay} ms...", retry, delay);
                }
            }
        }
    }
}
