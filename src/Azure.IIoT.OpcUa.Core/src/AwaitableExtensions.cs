// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    /// <summary>
    /// Task extensions for the <see cref="IAwaitable"/> awaiter pattern.
    /// </summary>
    public static class AwaitableExtensions
    {
        /// <summary>
        /// Create awaiter from task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static IAwaiter<T> AsAwaiter<T>(this Task<T> task)
        {
            return new TaskAwaiter<T>(task);
        }

        /// <summary>
        /// Create awaiter from task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="result"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static IAwaiter<T> AsAwaiter<T>(this Task task, T result,
            TaskScheduler? scheduler = null)
        {
            return task.ContinueWith(_ => result,
                scheduler: scheduler ?? TaskScheduler.Default).AsAwaiter();
        }

        /// <summary>
        /// Wait until all awaitables are completed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="awaitables"></param>
        /// <returns></returns>
        public static Task<T[]> WhenAll<T>(this IAwaitable<T>[] awaitables)
        {
            return Task.WhenAll(awaitables.Select(a => a.AsTask()).ToArray());
        }

        /// <summary>
        /// Convert to task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="awaitable"></param>
        /// <returns></returns>
        public static async Task<T> AsTask<T>(this IAwaitable<T> awaitable)
        {
            return await awaitable;
        }

        /// <summary>
        /// Wait until all awaitables are completed
        /// </summary>
        /// <param name="awaitables"></param>
        /// <returns></returns>
        public static Task WhenAll(this IEnumerable<IAwaitable> awaitables)
        {
            return Task.WhenAll(awaitables.Select(a => a.AsTask()).ToArray());
        }

        /// <summary>
        /// Convert to task
        /// </summary>
        /// <param name="awaitable"></param>
        /// <returns></returns>
        public static async Task AsTask(this IAwaitable awaitable)
        {
            await (IAwaitable<object?>)awaitable;
        }

        /// <summary>
        /// Task awaiter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class TaskAwaiter<T> : IAwaiter<T>
        {
            /// <inheritdoc/>
            public bool IsCompleted => _t.IsCompleted;

            /// <inheritdoc/>
            public TaskAwaiter(Task<T> t)
            {
                _t = t.ConfigureAwait(false).GetAwaiter();
            }

            /// <inheritdoc/>
            public T GetResult()
            {
                return _t.GetResult();
            }

            /// <inheritdoc/>
            public void OnCompleted(Action continuation)
            {
                _t.OnCompleted(continuation);
            }

            private readonly ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter _t;
        }
    }
}
