// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Utils {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Retry helper class with different retry policies
    /// </summary>
    public static class Retry {

        private static readonly object Semaphore = new object();

        public static int MaxRetryCount = 5;
        public static int MaxRetryDelay = 1 * 60 * 1000; // 1 minute
        public static int BackoffDelta = 20;


        /// <summary>
        /// Default exponential policy with 20% jitter
        /// </summary>
        public static Func<int, Exception, int> Exponential => (k, ex) => {
            var r = new Random();
            var increment = (int)((Math.Pow(2, k) - 1) *
                r.Next((int)(BackoffDelta * 0.8), (int)(BackoffDelta * 1.2)));
            return (int)Math.Min(increment, MaxRetryDelay);
        };

        /// <summary>
        /// Default linear policy
        /// </summary>
        public static Func<int, Exception, int> Linear => (k, ex) => k * BackoffDelta;

        /// <summary>
        /// No backoff - just wait backoff delta
        /// </summary>
        public static Func<int, Exception, int> NoBackoff => (k, ex) => BackoffDelta;

        /// <summary>
        /// Retries a piece of work
        /// </summary>
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
                    if (k > maxRetry || !cont(ex)) {
                        logger.Info($"Give up after {k}", () => ex);
                        throw ex;
                    }
                    logger.Debug($"Retry {k}..", () => ex);
                    var delay = policy(k, ex);
                    if (delay == 0) {
                        continue;
                    }
                    await Task.Delay(delay, ct);
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
                    if (k > maxRetry || !cont(ex)) {
                        logger.Info($"Give up after {k}", () => ex);
                        throw ex;
                    }
                    logger.Debug($"Retry {k}...", () => ex);
                    var delay = policy(k, ex);
                    if (delay == 0) {
                        continue;
                    }
                    await Task.Delay(delay, ct);
                }
            }
        }

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
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
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger, CancellationToken ct,
            Func<Task> work) =>
            WithLinearBackoff(logger, ct, work, ex => ex is ITransientException);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithLinearBackoff(ILogger logger, Func<Task> work) =>
            WithLinearBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with linear backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithLinearBackoff<T>(ILogger logger, Func<Task<T>> work) =>
            WithLinearBackoff(logger, CancellationToken.None, work);


        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, CancellationToken ct,
            Func<Task> work, Func<Exception, bool> cont) =>
             Do(logger, ct, work, cont, Exponential, MaxRetryCount);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, CancellationToken ct,
            Func<Task> work) =>
            WithExponentialBackoff(logger, ct, work, (ex) => ex is ITransientException);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task WithExponentialBackoff(ILogger logger, Func<Task> work) =>
            WithExponentialBackoff(logger, CancellationToken.None, work);

        /// <summary>
        /// Retry with exponential backoff
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        /// <param name="work"></param>
        /// <returns></returns>
        public static Task<T> WithExponentialBackoff<T>(ILogger logger, Func<Task<T>> work) =>
             WithExponentialBackoff(logger, CancellationToken.None, work);
    }
}
