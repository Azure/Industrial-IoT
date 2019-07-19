/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Sample {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides a queue for data changes.
    /// </summary>
    public class MonitoredItemQueue {
        /// <summary>
        /// Creates an empty queue.
        /// </summary>
        public MonitoredItemQueue() {
            _values = null;
            _errors = null;
            _start = -1;
            _end = -1;
            _overflow = -1;
            _discardOldest = false;
            _nextSampleTime = 0;
            _samplingInterval = 0;
        }

        /// <summary>
        /// Gets the current queue size.
        /// </summary>
        public uint QueueSize {
            get {
                if (_values == null) {
                    return 0;
                }

                return (uint)_values.Length;
            }
        }

        /// <summary>
        /// Sets the sampling interval used when queuing values.
        /// </summary>
        /// <param name="samplingInterval">The new sampling interval.</param>
        public void SetSamplingInterval(double samplingInterval) {
            // substract the previous sampling interval.
            if (_samplingInterval < _nextSampleTime) {
                _nextSampleTime -= _samplingInterval;
            }

            // calculate the next sampling interval.
            _samplingInterval = (long)(samplingInterval * TimeSpan.TicksPerMillisecond);

            if (_samplingInterval > 0) {
                _nextSampleTime += _samplingInterval;
            }
            else {
                _nextSampleTime = 0;
            }
        }

        /// <summary>
        /// Sets the queue size.
        /// </summary>
        /// <param name="queueSize">The new queue size.</param>
        /// <param name="discardOldest">Whether to discard the oldest values if the queue overflows.</param>
        /// <param name="diagnosticsMasks">Specifies which diagnostics which should be kept in the queue.</param>
        public void SetQueueSize(uint queueSize, bool discardOldest, DiagnosticsMasks diagnosticsMasks) {
            var length = (int)queueSize;

            if (length < 1) {
                length = 1;
            }

            var start = _start;
            var end = _end;

            // create new queue.
            var values = new DataValue[length];
            ServiceResult[] errors = null;

            if ((diagnosticsMasks & DiagnosticsMasks.OperationAll) != 0) {
                errors = new ServiceResult[length];
            }

            // copy existing values.
            List<DataValue> existingValues = null;
            List<ServiceResult> existingErrors = null;

            if (_start >= 0) {
                existingValues = new List<DataValue>();
                existingErrors = new List<ServiceResult>();

                DataValue value = null;
                ServiceResult error = null;

                while (Dequeue(out value, out error)) {
                    existingValues.Add(value);
                    existingErrors.Add(error);
                }
            }

            // update internals.
            _values = values;
            _errors = errors;
            _start = -1;
            _end = 0;
            _overflow = -1;
            _discardOldest = discardOldest;

            // requeue the data.
            if (existingValues != null) {
                for (var ii = 0; ii < existingValues.Count; ii++) {
                    Enqueue(existingValues[ii], existingErrors[ii]);
                }
            }
        }

        /// <summary>
        /// Adds the value to the queue.
        /// </summary>
        /// <param name="value">The value to queue.</param>
        /// <param name="error">The error to queue.</param>
        public void QueueValue(DataValue value, ServiceResult error) {
            var now = DateTime.UtcNow.Ticks;

            if (_start >= 0) {
                // check if too soon for another sample.
                if (now < _nextSampleTime) {
                    var last = _end - 1;

                    if (last < 0) {
                        last = _values.Length - 1;
                    }

                    // replace last value and error.
                    _values[last] = value;

                    if (_errors != null) {
                        _errors[last] = error;
                    }

                    return;
                }
            }

            // update next sample time.
            if (_nextSampleTime > 0) {
                var delta = now - _nextSampleTime;

                if (_samplingInterval > 0 && delta >= 0) {
                    _nextSampleTime += ((delta / _samplingInterval) + 1) * _samplingInterval;
                }
            }
            else {
                _nextSampleTime = now + _samplingInterval;
            }

            // queue next value.
            Enqueue(value, error);
        }

        /// <summary>
        /// Publishes the oldest value in the queue.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="error">The error associated with the value.</param>
        /// <returns>True if a value was found. False if the queue is empty.</returns>
        public bool Publish(out DataValue value, out ServiceResult error) {
            return Dequeue(out value, out error);
        }



        /// <summary>
        /// Adds the value to the queue. Discards values if the queue is full.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <param name="error">The error to add.</param>
        private void Enqueue(DataValue value, ServiceResult error) {
            // check for empty queue.
            if (_start < 0) {
                _start = 0;
                _end = 1;
                _overflow = -1;

                _values[_start] = value;

                if (_errors != null) {
                    _errors[_start] = error;
                }

                return;
            }

            var next = _end;

            // check for wrap around.
            if (next >= _values.Length) {
                next = 0;
            }

            // check if queue is full.
            if (_start == next) {
                if (!_discardOldest) {
                    _overflow = _end - 1;
                    return;
                }

                // remove oldest value.
                _start++;

                if (_start >= _values.Length) {
                    _start = 0;
                }

                // set overflow bit.
                _overflow = _start;
            }

            // add value.
            _values[next] = value;

            if (_errors != null) {
                _errors[next] = error;
            }

            _end = next + 1;
        }

        /// <summary>
        /// Removes a value and an error from the queue.
        /// </summary>
        /// <param name="value">The value removed from the queue.</param>
        /// <param name="error">The error removed from the queue.</param>
        /// <returns>True if a value was found. False if the queue is empty.</returns>
        private bool Dequeue(out DataValue value, out ServiceResult error) {
            value = null;
            error = null;

            // check for empty queue.
            if (_start < 0) {
                return false;
            }

            value = _values[_start];
            _values[_start] = null;

            if (_errors != null) {
                error = _errors[_start];
                _errors[_start] = null;
            }

            // set the overflow bit.
            if (_overflow == _start) {
                SetOverflowBit(ref value, ref error);
                _overflow = -1;
            }

            _start++;

            // check if queue has been emptied.
            if (_start == _end) {
                _start = -1;
                _end = 0;
            }

            // check for wrap around.
            else if (_start >= _values.Length) {
                _start = 0;
            }

            return true;
        }

        /// <summary>
        /// Sets the overflow bit in the value and error.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="error">The error to update.</param>
        private void SetOverflowBit(ref DataValue value, ref ServiceResult error) {
            if (value != null) {
                var status = value.StatusCode;
                status.Overflow = true;
                value.StatusCode = status;
            }

            if (error != null) {
                var status = error.StatusCode;
                status.Overflow = true;

                // have to copy before updating because the ServiceResult is invariant.
                var copy = new ServiceResult(
                    status,
                    error.SymbolicId,
                    error.NamespaceUri,
                    error.LocalizedText,
                    error.AdditionalInfo,
                    error.InnerResult);

                error = copy;
            }
        }

        private DataValue[] _values;
        private ServiceResult[] _errors;
        private int _start;
        private int _end;
        private int _overflow;
        private bool _discardOldest;
        private long _nextSampleTime;
        private long _samplingInterval;
    }
}
