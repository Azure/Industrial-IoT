/* ========================================================================
 * Copyright (c) 2005-2013 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Aggregates {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Coordinates aggregation over a time series of raw data points to yield a time series of processed data points.
    /// </summary>
    public abstract class BaseAggregateCalculator : IAggregateCalculator, IAggregationContext, IAggregationActor, IAggregator {

        /// <summary>
        /// The start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Whether time flows backwards
        /// </summary>
        public bool IsReverseAggregation => EndTime < StartTime;

        /// <summary>
        /// The percentage data that can be bad.
        /// </summary>
        public byte PercentDataBad => Configuration.PercentDataBad;

        /// <summary>
        /// The percentage data that must be good.
        /// </summary>
        public byte PercentDataGood => Configuration.PercentDataGood;

        /// <summary>
        /// Whether to use sloped extrapolation.
        /// </summary>
        public bool UseSlopedExtrapolation => Configuration.UseSlopedExtrapolation;

        /// <summary>
        /// Whether value sematics of the underlying data require stepped interpolation.
        /// </summary>
        public bool SteppedVariable { get; set; }

        /// <summary>
        /// How to treat uncertain data.
        /// </summary>
        public bool TreatUncertainAsBad => Configuration.TreatUncertainAsBad;

        /// <summary>
        /// THe width of the processing interval.
        /// </summary>
        public double ProcessingInterval { get; set; }

        /// <summary>
        /// Processes the next value returns the calculated values up until the last complete interval.
        /// </summary>
        public IList<DataValue> ProcessValue(DataValue value, ServiceResult result) {
            if (_state == null) {
                InitializeAggregation();
            }

            _state.AddRawData(value);
            return ProcessedValues();
        }

        /// <summary>
        /// Processes all remaining intervals.
        /// </summary>
        public IList<DataValue> ProcessTermination(ServiceResult result) {
            if (_state == null) {
                InitializeAggregation();
            }

            _state.EndOfData();
            return ProcessedValues();
        }

        /// <summary>
        /// Updates the data processed by the aggregator.
        /// </summary>
        public void UpdateProcessedData(DataValue rawValue, AggregateState state) {
            // step 1: compute new TimeSlice instances to enqueue, until we reach the one the
            // rawValue belongs in or we've reached the one that goes to the EndTime. Ensure
            // that the raw value is added to the last one created.
            TimeSlice tmpTS = null;
            if (_pending == null) {
                _pending = new Queue<TimeSlice>();
            }

            if (_latest == null) {
                tmpTS = TimeSlice.CreateInitial(StartTime, EndTime, ProcessingInterval);
                if (tmpTS != null) {
                    _pending.Enqueue(tmpTS);
                    _latest = tmpTS;
                }
            }
            else {
                tmpTS = _latest;
            }
            var latestTime = (StartTime > EndTime) ? StartTime : EndTime;
            while ((tmpTS != null) && (state.HasTerminated || !tmpTS.AcceptValue(rawValue))) {
                tmpTS = TimeSlice.CreateNext(latestTime, ProcessingInterval, tmpTS);
                if (tmpTS != null) {
                    _pending.Enqueue(tmpTS);
                    _latest = tmpTS;
                }
            }

            // step 2: apply the aggregator to the head of the queue to see if we can convert
            // it into a processed point. If so, dequeue it and add the processed value to the
            // _released list. Keep doing it until one of the TimeSlices returns null or we
            // run out of enqueued TimeSlices (should only happen on termination).
            if (_released == null) {
                _released = new List<DataValue>();
            }

            foreach (var b in _pending) {
                UpdateBoundingValues(b, state);
            }

            var active = true;
            while ((_pending.Count > 0) && active) {
                var top = _pending.Peek();
                DataValue computed = null;
                if (!WaitForMoreData(top, state)) {
                    computed = Compute(this, top, state);
                }

                if (computed != null) {
                    _released.Add(computed);
                    _pending.Dequeue();
                }
                else {
                    active = false;
                }
            }
        }

        /// <summary>
        /// Returns the values processed by the aggregator.
        /// </summary>
        public IList<DataValue> ProcessedValues() {
            IList<DataValue> retval = null;
            retval = _released ?? new List<DataValue>();
            _released = null;
            return retval;
        }

        /// <summary>
        /// Computes the aggregate value for the time slice.
        /// </summary>
        public abstract DataValue Compute(IAggregationContext context,
            TimeSlice bucket, AggregateState state);

        /// <summary>
        /// Returns true if more data is required for the next interval.
        /// </summary>
        public abstract bool WaitForMoreData(TimeSlice bucket, AggregateState state);

        /// <summary>
        /// Updates the bounding values for the time slice.
        /// </summary>
        public abstract void UpdateBoundingValues(TimeSlice bucket, AggregateState state);



        /// <summary>
        /// The configuration to use when calculating aggregates.
        /// </summary>
        public AggregateConfiguration Configuration { get; set; }

        /// <summary>
        /// Computes the status code for the processing interval using the percent good/bad information in the context.
        /// </summary>
        protected virtual StatusCode ComputeStatus(IAggregationContext context, int numGood, int numBad, TimeSlice bucket) {
            var total = numGood + numBad;
            if (total > 0) {
                double pbad = numBad * 100 / total;
                if (pbad > context.PercentDataBad) {
                    return StatusCodes.Bad;
                }

                double pgood = numGood * 100 / total;
                if (pgood >= context.PercentDataGood) {
                    return StatusCodes.Good;
                }

                return StatusCodes.Uncertain;
            }
            else {
                return StatusCodes.GoodNoData;
            }
        }

        /// <summary>
        /// Initializes the aggregation.
        /// </summary>
        private void InitializeAggregation() {
            _state = new AggregateState(this, this);
        }

        private AggregateState _state;
        private TimeSlice _latest;
        private Queue<TimeSlice> _pending;
        private List<DataValue> _released;
    }
}
