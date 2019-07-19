// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Concurrent {
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Provides a thread-safe priority queue data structure.
    /// Based on ParallelExtensions sample implementation.
    /// </summary>
    [DebuggerDisplay("Count={Count}")]
    public sealed class PriorityQueue<T, V> : IProducerConsumerCollection<ValueTuple<T, V>>
        where T : IComparable<T> {

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => true;

        /// <inheritdoc/>
        object ICollection.SyncRoot => _syncLock;

        /// <inheritdoc/>
        public int Count {
            get {
                lock (_syncLock) {
                    return _minHeap.Count;
                }
            }
        }

        /// <summary>
        /// Gets whether the queue is empty.
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PriorityQueue() {
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrentPriorityQueue
        /// class that contains elements copied from the specified collection.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied
        /// to the new ConcurrentPriorityQueue.</param>
        public PriorityQueue(IEnumerable<ValueTuple<T, V>> collection) {
            if (collection == null) {
                throw new ArgumentNullException(nameof(collection));
            }
            foreach (var item in collection) {
                _minHeap.Insert(item);
            }
        }

        /// <inheritdoc/>
        public void CopyTo(ValueTuple<T, V>[] array, int index) {
            lock (_syncLock) {
                _minHeap.Items.CopyTo(array, index);
            }
        }

        /// <inheritdoc/>
        bool IProducerConsumerCollection<ValueTuple<T, V>>.TryAdd(
            ValueTuple<T, V> item) {
            Enqueue(item);
            return true;
        }

        /// <inheritdoc/>
        bool IProducerConsumerCollection<ValueTuple<T, V>>.TryTake(
            out ValueTuple<T, V> item) {
            return TryDequeue(out item);
        }

        /// <inheritdoc/>
        public IEnumerator<ValueTuple<T, V>> GetEnumerator() {
            var arr = ToArray();
            return ((IEnumerable<ValueTuple<T, V>>)arr).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index) {
            lock (_syncLock) {
                ((ICollection)_minHeap.Items).CopyTo(array, index);
            }
        }

        /// <summary>
        /// Copies the elements stored in the queue to a new array.
        /// </summary>
        /// <returns>
        /// A new array containing a snapshot of elements copied from
        /// the queue.
        /// </returns>
        public ValueTuple<T, V>[] ToArray() {
            lock (_syncLock) {
                var clonedHeap = new MinBinaryHeap(_minHeap);
                var result = new ValueTuple<T, V>[_minHeap.Count];
                for (var i = 0; i < result.Length; i++) {
                    result[i] = clonedHeap.Remove();
                }
                return result;
            }
        }

        /// <summary>
        /// Adds the key/value pair to the priority queue.
        /// </summary>
        /// <param name="priority">The priority of the item to be added.</param>
        /// <param name="value">The item to be added.</param>
        public void Enqueue(T priority, V value) {
            Enqueue(new ValueTuple<T, V>(priority, value));
        }

        /// <summary>
        /// Adds the key/value pair to the priority queue.
        /// </summary>
        /// <param name="item">The key/value pair to be added to the queue.
        /// </param>
        public void Enqueue(ValueTuple<T, V> item) {
            lock (_syncLock) {
                _minHeap.Insert(item);
            }
        }

        /// <summary>
        /// Attempts to remove and return the next prioritized item in the queue.
        /// </summary>
        /// <param name="result"> When this method returns, if the operation was
        /// successful, result contains the object removed. If no object was
        /// available to be removed, the value is unspecified.
        /// </param>
        /// <returns>
        /// true if an element was removed and returned from the queue succesfully;
        /// otherwise, false.
        /// </returns>
        public bool TryDequeue(out ValueTuple<T, V> result) {
            result = default;
            lock (_syncLock) {
                if (_minHeap.Count > 0) {
                    result = _minHeap.Remove();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to return the next prioritized item in the queue.
        /// </summary>
        /// <param name="result">
        /// When this method returns, if the operation was successful, result
        /// contains the object. The queue was not modified by the operation.
        /// </param>
        /// <returns> true if an element was returned from the queue
        /// succesfully; otherwise, false. </returns>
        public bool TryPeek(out ValueTuple<T, V> result) {
            result = default;
            lock (_syncLock) {
                if (_minHeap.Count > 0) {
                    result = _minHeap.Peek();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Empties the queue.
        /// </summary>
        public void Clear() {
            lock (_syncLock) {
                _minHeap.Clear();
            }
        }

        /// <summary>
        /// Implements a binary heap that prioritizes smaller values.
        /// </summary>
        private sealed class MinBinaryHeap {

            /// <summary>
            /// Gets the number of objects stored in the heap.
            /// </summary>
            public int Count => Items.Count;

            /// <summary>
            /// Gets the items stored in the heap.
            /// </summary>
            internal List<ValueTuple<T, V>> Items { get; }

            /// <summary>
            /// Initializes an empty heap.
            /// </summary>
            public MinBinaryHeap() {
                Items = new List<ValueTuple<T, V>>();
            }

            /// <summary>
            /// Initializes a heap as a copy of another heap instance.
            /// </summary>
            /// <param name="heapToCopy">The heap to copy.</param>
            /// <remarks>Key/Value values are not deep cloned.</remarks>
            public MinBinaryHeap(MinBinaryHeap heapToCopy) {
                Items = new List<ValueTuple<T, V>>(heapToCopy.Items);
            }

            /// <summary>
            /// Empties the heap.
            /// </summary>
            public void Clear() {
                Items.Clear();
            }

            /// <summary>
            /// Adds an item to the heap.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public void Insert(T key, V value) {
                Insert(new ValueTuple<T, V>(key, value));
            }

            /// <summary>
            /// Adds an item to the heap.
            /// </summary>
            /// <param name="entry"></param>
            public void Insert(ValueTuple<T, V> entry) {
                // Add the item to the list, making sure to keep track of where it was added.
                Items.Add(entry);
                var pos = Items.Count - 1;
                // If the new item is the only item, we're done.
                if (pos == 0) {
                    return;
                }
                // Otherwise, perform log(n) operations, walking up the tree, swapping
                // where necessary based on key values
                while (pos > 0) {
                    // Get the next position to check
                    var nextPos = (pos - 1) / 2;
                    // Extract the entry at the next position
                    var toCheck = Items[nextPos];
                    // Compare that entry to our new one.  If our entry has a smaller key, move it up.
                    // Otherwise, we're done.
                    if (entry.Item1.CompareTo(toCheck.Item1) < 0) {
                        Items[pos] = toCheck;
                        pos = nextPos;
                    }
                    else {
                        break;
                    }
                }
                // Make sure we put this entry back in, just in case
                Items[pos] = entry;
            }

            /// <summary>
            /// Returns the entry at the top of the heap.
            /// </summary>
            /// <returns></returns>
            public ValueTuple<T, V> Peek() {
                // Returns the first item
                if (Items.Count == 0) {
                    throw new InvalidOperationException("The heap is empty.");
                }
                return Items[0];
            }

            /// <summary>
            /// Removes the entry at the top of the heap.
            /// </summary>
            /// <returns></returns>
            public ValueTuple<T, V> Remove() {
                // Get the first item and save it for later (this is what will be returned).
                if (Items.Count == 0) {
                    throw new InvalidOperationException("The heap is empty.");
                }
                var toReturn = Items[0];

                // Remove the first item if there will only be 0 or 1 items left
                // after doing so.
                if (Items.Count <= 2) {
                    Items.RemoveAt(0);
                }
                // A reheapify will be required for the removal
                else {
                    // Remove the first item and move the last item to the front.
                    Items[0] = Items[Items.Count - 1];
                    Items.RemoveAt(Items.Count - 1);

                    // Start reheapify
                    int current = 0, possibleSwap = 0;

                    // Keep going until the tree is a heap
                    while (true) {
                        // Get the positions of the node's children
                        var leftChildPos = (2 * current) + 1;
                        var rightChildPos = leftChildPos + 1;

                        // Should we swap with the left child?
                        if (leftChildPos < Items.Count) {
                            // Get the two entries to compare (node and its left child)
                            var entry1 = Items[current];
                            var entry2 = Items[leftChildPos];

                            //
                            // If the child has a lower key than the parent, set that
                            // as a possible swap
                            //
                            if (entry2.Item1.CompareTo(entry1.Item1) < 0) {
                                possibleSwap = leftChildPos;
                            }
                        }
                        else {
                            break; // if can't swap this, we're done
                        }

                        //
                        // Should we swap with the right child?  Note that now we
                        // check with the possible swap position (which might be
                        // current and might be left child).
                        //
                        if (rightChildPos < Items.Count) {
                            // Get the two entries to compare (node and its left child)
                            var entry1 = Items[possibleSwap];
                            var entry2 = Items[rightChildPos];

                            // If the child has a lower key than the parent, set that
                            // as a possible swap
                            if (entry2.Item1.CompareTo(entry1.Item1) < 0) {
                                possibleSwap = rightChildPos;
                            }
                        }

                        // Now swap current and possible swap if necessary
                        if (current != possibleSwap) {
                            var temp = Items[current];
                            Items[current] = Items[possibleSwap];
                            Items[possibleSwap] = temp;
                        }
                        else {
                            break; // if nothing to swap, we're done
                        }

                        // Update current to the location of the swap
                        current = possibleSwap;
                    }
                }
                // Return the item from the heap
                return toReturn;
            }
        }

        private readonly object _syncLock = new object();
        private readonly MinBinaryHeap _minHeap = new MinBinaryHeap();
    }

}
