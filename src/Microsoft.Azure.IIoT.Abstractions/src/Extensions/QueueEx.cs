// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {
    using System.Collections.Concurrent;

    /// <summary>
    /// Queue extensions
    /// </summary>
    public static class QueueEx {

        /// <summary>
        /// Add range to producer consumer collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="enumerable"></param>
        public static void AddRange<T>(this IProducerConsumerCollection<T> collection,
            IEnumerable<T> enumerable) {
            foreach (var item in enumerable) {
                while (!collection.TryAdd(item)) {
                }
            }
        }

        /// <summary>
        /// Pops a number of items from the queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="count"></param>
        public static IEnumerable<T> Dequeue<T>(this Queue<T> queue, int count) {
            for (var i = 0; i < count && queue.Count != 0; i++) {
                yield return queue.Dequeue();
            }
        }

        /// <summary>
        /// Pops a number of items from the queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="count"></param>
        public static IEnumerable<T> Dequeue<T>(this ConcurrentQueue<T> queue, int count) {
            for (var i = 0; i < count; i++) {
                if (!queue.TryDequeue(out T result)) {
                    yield break;
                }
                yield return result;
            }
        }
    }
}
