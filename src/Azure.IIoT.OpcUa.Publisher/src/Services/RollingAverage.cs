// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using System;

    /// <summary>
    /// Rolling average calculator
    /// </summary>
    internal sealed class RollingAverage
    {
        /// <summary>
        /// Changes last minute
        /// </summary>
        public long LastMinute
        {
            get => CalculateSumForRingBuffer(_buffer,
                ref _lastPointer, _bucketWidth, _lastWriteTime);
            set => IncreaseRingBuffer(_buffer,
                ref _lastPointer, _bucketWidth, value, ref _lastWriteTime);
        }

        /// <summary>
        /// Changes total
        /// </summary>
        public long Count
        {
            get => _count;
            set
            {
                var difference = value - _count;
                _count = value;
                LastMinute = difference;
            }
        }

        public RollingAverage(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Iterates the array and add up all values
        /// </summary>
        /// <param name="array"></param>
        /// <param name="lastPointer"></param>
        /// <param name="bucketWidth"></param>
        /// <param name="lastWriteTime"></param>
        private long CalculateSumForRingBuffer(long[] array, ref int lastPointer,
            int bucketWidth, DateTimeOffset lastWriteTime)
        {
            // if IncreaseRingBuffer wasn't called for some time, maybe some stale values are included
            UpdateRingBufferBuckets(array, ref lastPointer, bucketWidth, ref lastWriteTime);
            // with cleaned buffer, we can just accumulate all buckets
            long sum = 0;
            for (var index = 0; index < array.Length; index++)
            {
                sum += array[index];
            }
            return sum;
        }

        /// <summary>
        /// Helper function to distribute values over array based on time
        /// </summary>
        /// <param name="array"></param>
        /// <param name="lastPointer"></param>
        /// <param name="bucketWidth"></param>
        /// <param name="difference"></param>
        /// <param name="lastWriteTime"></param>
        private void IncreaseRingBuffer(long[] array, ref int lastPointer,
            int bucketWidth, long difference, ref DateTimeOffset lastWriteTime)
        {
            var indexPointer = UpdateRingBufferBuckets(array, ref lastPointer,
                bucketWidth, ref lastWriteTime);
            array[indexPointer] += difference;
        }

        /// <summary>
        /// Empty the ring buffer buckets if necessary
        /// </summary>
        /// <param name="array"></param>
        /// <param name="lastPointer"></param>
        /// <param name="bucketWidth"></param>
        /// <param name="lastWriteTime"></param>
        private int UpdateRingBufferBuckets(long[] array, ref int lastPointer,
            int bucketWidth, ref DateTimeOffset lastWriteTime)
        {
            var now = _timeProvider.GetUtcNow();
            var indexPointer = now.Second % bucketWidth;

            // if last update was > bucketsize seconds in the past delete whole array
            if (lastWriteTime != DateTimeOffset.MinValue)
            {
                var deleteWholeArray = (now - lastWriteTime).TotalSeconds >= bucketWidth;
                if (deleteWholeArray)
                {
                    Array.Clear(array, 0, array.Length);
                    lastPointer = indexPointer;
                }
            }

            // reset all buckets, between last write and now
            while (lastPointer != indexPointer)
            {
                lastPointer = (lastPointer + 1) % bucketWidth;
                array[lastPointer] = 0;
            }

            lastWriteTime = now;
            return indexPointer;
        }

        private int _lastPointer;
        private long _count;
        private DateTimeOffset _lastWriteTime = DateTimeOffset.MinValue;
        private readonly TimeProvider _timeProvider;
        private readonly long[] _buffer = new long[_bucketWidth];
        private const int _bucketWidth = 60;
    }
}
