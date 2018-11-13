// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Concurrent {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides an asynchronous waitable queue.
    /// </summary>
    [DebuggerDisplay("Count={CurrentCount}")]
    public sealed class AsyncQueue<T> : IDisposable, IEnumerable<T> {

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => _collection.Count;

        /// <summary>
        /// Initializes the asynchronous producer/consumer collection.
        /// </summary>
        public AsyncQueue() : this(new ConcurrentQueue<T>()) { }

        /// <summary>
        /// Initializes the asynchronous producer/consumer collection.
        /// </summary>
        /// <param name="collection">The underlying collection to use to store
        /// data.</param>
        public AsyncQueue(IProducerConsumerCollection<T> collection) {
            _collection = collection ??
                throw new ArgumentNullException(nameof(collection));
        }

        /// <summary>
        /// Adds an element to the collection.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        public bool TryAdd(T item) {
            if (_collection.TryAdd(item)) {
                _semaphore.Release();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Takes an element from the collection asynchronously.  Returns the
        /// default value if timeout passes or wait was cancelled.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="defaultValue"></param>
        /// <param name="ct"></param>
        /// <returns>
        /// A Task that represents the element removed from the collection.
        /// </returns>
        public Task<T> TakeAsync(TimeSpan timeout, T defaultValue,
            CancellationToken ct) {
            return _semaphore.WaitAsync(timeout, ct).ContinueWith(waited => {
                if (!waited.Result || !_collection.TryTake(out var result)) {
                    return defaultValue;
                }
                return result;
            }, TaskContinuationOptions.ExecuteSynchronously |
               TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();
        /// <inheritdoc/>
        public void Dispose() => _semaphore.Dispose();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private readonly IProducerConsumerCollection<T> _collection;
    }
}
