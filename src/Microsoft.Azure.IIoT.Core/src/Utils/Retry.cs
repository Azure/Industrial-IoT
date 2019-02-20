// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using Serilog;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Retry helper class with different retry policies
    /// </summary>
    public static class Retry {

        private static readonly object Semaphore = new object();

        /// <summary>Retry count max</summary>
        public static int MaxRetryCount = 10;

        /// <summary>
        /// Default exponential policy with 20% jitter
        /// </summary>
        public static Func<int, Exception, int> Exponential => (k, ex) =>
            GetExponentialDelay(k, ExponentialBackoffIncrement, ExponentialMaxRetryCount);

        private static Random r = new Random();
        /// <summary>Max retry count for exponential policy</summary>
        public static int ExponentialMaxRetryCount = 13;
        /// <summary>Exponential backoff increment</summary>
        public static int ExponentialBackoffIncrement = 10;

        /// <summary>
        /// Default linear policy
        /// </summary>
        public static Func<int, Exception, int> Linear => (k, ex) =>
            Math.Min(k, LinearMaxRetryDelayCount) * LinearBackoffDelta;
        /// <summary>Max retry multiplier</summary>
        public static int LinearMaxRetryDelayCount = 20;
        /// <summary>Incremental delay</summary>
        public static int LinearBackoffDelta = 1000;

        /// <summary>
        /// No backoff - just wait backoff delta
        /// </summary>
        public static Func<int, Exception, int> NoBackoff => (k, ex) => NoBackoffDelta;
        /// <summary>Time between retry</summary>
        public static int NoBackoffDelta = 1000;

        /// <summary>
        /// Helper to calcaulate exponential delay with jitter and max.
        /// </summary>
        /// <param name="k"></param>
        /// <param name="increment"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static int GetExponentialDelay(int k, int increment, int maxRetry) {
            if (k > maxRetry) {
                k = maxRetry;
            }
            var backoff = r.Next((int)(increment * 0.8), (int)(increment * 1.2));
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
        /// <returns></returns>
        public static async Task Do(ILogger logger, CancellationToken ct, Func<Task> work,
            Func<Exception, bool> cont, Func<int, Exception, int> policy, int maxRetry) {
            for (var k = 1; ; k++) {
                if (ct.IsCancellationRequested) {
                    throw new TaskCanceledException();
                }
                try {
                    await work();
                    return;
                }
                catch (Exception ex) {
                    await DelayOrThrow(logger, cont, policy, maxRetry, k, ex, ct);
                }
            }
        }

        /// <summary>
        /// Retries a piece of work with return type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="policy"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static async Task<T> Do<T>(ILogger logger, CancellationToken ct, Func<Task<T>> work,
            Func<Exception, bool> cont, Func<int, Exception, int> policy, int maxRetry) {
            for (var k = 1; ; k++) {
                if (ct.IsCancellationRequested) {
                    throw new TaskCanceledException();
                }
                try {
                    return await work();
                }
                catch (Exception ex) {
                    await DelayOrThrow(logger, cont, policy, maxRetry, k, ex, ct);
                }
            }
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
        /// <returns></returns>
        public static async Task Do(ILogger logger, CancellationToken ct, Action work,
            Func<Exception, bool> cont, Func<int, Exception, int> policy, int maxRetry) {
            for (var k = 1; ; k++) {
                if (ct.IsCancellationRequested) {
                    throw new TaskCanceledException();
                }
                try {
                    work();
                    return;
                }
                catch (Exception ex) {
                    await DelayOrThrow(logger, cont, policy, maxRetry, k, ex, ct);
                }
            }
        }

        /// <summary>
        /// Retries a piece of work with return type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <param name="policy"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public static async Task<T> Do<T>(ILogger logger, CancellationToken ct, Func<T> work,
            Func<Exception, bool> cont, Func<int, Exception, int> policy, int maxRetry) {
            for (var k = 1; ; k++) {
                if (ct.IsCancellationRequested) {
                    throw new TaskCanceledException();
                }
                try {
                    return work();
                }
                catch (Exception ex) {
                    await DelayOrThrow(logger, cont, policy, maxRetry, k, ex, ct);
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
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger, CancellationToken ct,
            Func<Task> work, Func<Exception, bool> cont) =>
                Do(logger, ct, work, cont, Linear, MaxRetryCount);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger, CancellationToken ct,
            Func<Task> work) =>
            WithLinearBackoff(logger, ct, work, ex => ex is ITransientException);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger, Func<Task> work) =>
            WithLinearBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger,
            Func<Task> work, Func<Exception, bool> cont) =>
            WithLinearBackoff(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger, CancellationToken ct,
            Func<Task<T>> work, Func<Exception, bool> cont) =>
            Do(logger, ct, work, cont, Linear, MaxRetryCount);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger, CancellationToken ct,
            Func<Task<T>> work) =>
            WithLinearBackoff(logger, ct, work, (ex) => ex is ITransientException);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger, Func<Task<T>> work) =>
            WithLinearBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger,
            Func<Task<T>> work, Func<Exception, bool> cont) =>
            WithLinearBackoff(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, CancellationToken ct,
            Func<Task> work, Func<Exception, bool> cont) =>
            Do(logger, ct, work, cont, Exponential, MaxRetryCount);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, CancellationToken ct,
            Func<Task> work) =>
            WithExponentialBackoff(logger, ct, work, ex => ex is ITransientException);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, Func<Task> work) =>
            WithExponentialBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger,
            Func<Task> work, Func<Exception, bool> cont) =>
            WithExponentialBackoff(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger, CancellationToken ct,
            Func<Task<T>> work, Func<Exception, bool> cont) =>
            Do(logger, ct, work, cont, Exponential, MaxRetryCount);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger, CancellationToken ct,
            Func<Task<T>> work) =>
            WithExponentialBackoff(logger, ct, work, (ex) => ex is ITransientException);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger, Func<Task<T>> work) =>
             WithExponentialBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger,
            Func<Task<T>> work, Func<Exception, bool> cont) =>
            WithExponentialBackoff(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger, CancellationToken ct,
            Action work, Func<Exception, bool> cont) =>
                Do(logger, ct, work, cont, Linear, MaxRetryCount);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger, CancellationToken ct,
            Action work) =>
            WithLinearBackoff(logger, ct, work, ex => ex is ITransientException);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger, Action work) =>
            WithLinearBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger,
            Action work, Func<Exception, bool> cont) =>
            WithLinearBackoff(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger, CancellationToken ct,
            Func<T> work, Func<Exception, bool> cont) =>
            Do(logger, ct, work, cont, Linear, MaxRetryCount);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger, CancellationToken ct,
            Func<T> work) =>
            WithLinearBackoff(logger, ct, work, (ex) => ex is ITransientException);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger, Func<T> work) =>
            WithLinearBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger,
            Func<T> work, Func<Exception, bool> cont) =>
            WithLinearBackoff(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, CancellationToken ct,
            Action work, Func<Exception, bool> cont) =>
             Do(logger, ct, work, cont, Exponential, MaxRetryCount);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, CancellationToken ct,
            Action work) =>
            WithExponentialBackoff(logger, ct, work, ex => ex is ITransientException);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, Action work) =>
            WithExponentialBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger,
            Action work, Func<Exception, bool> cont) =>
            WithExponentialBackoff(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger, CancellationToken ct,
            Func<T> work, Func<Exception, bool> cont) =>
            Do(logger, ct, work, cont, Exponential, MaxRetryCount);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger, CancellationToken ct,
            Func<T> work) =>
            WithExponentialBackoff(logger, ct, work, (ex) => ex is ITransientException);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger, Func<T> work) =>
             WithExponentialBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger,
            Func<T> work, Func<Exception, bool> cont) =>
            WithExponentialBackoff(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry without delay
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task WithoutDelay(ILogger logger, CancellationToken ct,
            Action work, Func<Exception, bool> cont) =>
                Do(logger, ct, work, cont, NoBackoff, MaxRetryCount);

        /// <summary>
        /// Retry without delay
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithoutDelay(ILogger logger, CancellationToken ct,
            Action work) =>
            WithoutDelay(logger, ct, work, ex => ex is ITransientException);

        /// <summary>
        /// Retry without delay
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithoutDelay(ILogger logger, Action work) =>
            WithoutDelay(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry without delay
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithoutDelay(ILogger logger,
            Action work, Func<Exception, bool> cont) =>
            WithoutDelay(logger, CancellationToken.None, work, cont);

        /// <summary>
        /// Retry without delay
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task<T> WithoutDelay<T>(ILogger logger, CancellationToken ct,
            Func<T> work, Func<Exception, bool> cont) =>
            Do(logger, ct, work, cont, NoBackoff, MaxRetryCount);

        /// <summary>
        /// Retry without delay
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithoutDelay<T>(ILogger logger, CancellationToken ct,
            Func<T> work) =>
            WithoutDelay(logger, ct, work, (ex) => ex is ITransientException);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithoutDelay<T>(ILogger logger, Func<T> work) =>
            WithoutDelay(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry without delay
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="cont"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithoutDelay<T>(ILogger logger,
            Func<T> work, Func<Exception, bool> cont) =>
            WithoutDelay(logger, CancellationToken.None, work, cont);

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
        private static async Task DelayOrThrow(ILogger logger, Func<Exception, bool> cont,
            Func<int, Exception, int> policy, int maxRetry, int k, Exception ex,
            CancellationToken ct) {
            if ((k > maxRetry || !cont(ex)) && !(ex is ITransientException)) {
                logger?.Verbose(ex, "Give up after {k}", k);
                throw ex;
            }
            if (ex is TemporarilyBusyException tbx && tbx.RetryAfter != null) {
                var delay = tbx.RetryAfter.Value;
                Log(logger, k, (int)delay.TotalMilliseconds, ex);
                await Task.Delay(delay, ct);
            }
            else {
                var delay = policy(k, ex);
                Log(logger, k, delay, ex);
                if (delay != 0) {
                    await Task.Delay(delay, ct);
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
        private static void Log(ILogger logger, int retry, int delay, Exception ex) {
            if (logger != null) {
                if (logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose)) {
                    logger.Verbose(ex, "Retry {k} in {delay} ms...", retry, delay);
                }
                else {
                    logger.Debug("  ... Retry {k} in {delay} ms...", retry, delay);
                }
            }
        }
    }
}
