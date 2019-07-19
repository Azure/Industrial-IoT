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
    /// A snapshot of a structured window over a stream of data to be aggregated
    /// </summary>
    public class AggregateCursor {

        /// <summary>
        /// A good data point later in time than the processed point(s) we want to compute.
        /// It could be used as one bounding value in a sloped interpolation. or as the only
        /// value needed for stepped interpolation. EarlyPoint and LatePoint may also be used
        /// to provide stepped extrapolation. If both exist, they may be used for sloped
        /// extrapolation
        /// </summary>
        public DataValue LatePoint { get; set; }

        /// <summary>
        /// A good data point earlier in time than the processed point(s) we want to compute.
        /// It could be used as one bounding value in a sloped interpolation. or as the only
        /// value needed for stepped interpolation. EarlyPoint and LatePoint may also be used
        /// to provide stepped extrapolation. If both exist, they may be used for sloped
        /// extrapolation
        /// </summary>
        public DataValue EarlyPoint { get; set; }

        /// <summary>
        /// The most recently superceded value of EarlyPoint. This is therefore a good data point.
        /// It can be used for sloped extrapolation with EarlyPoint in the case where no good
        /// value exists for LatePoint.
        /// </summary>
        public DataValue PriorPoint { get; set; }

        /// <summary>
        /// A collection of all bad points received since EarlyPoint. This is required to
        /// compute the status of interpolated and extrapolated points that use EarlyPoint as
        /// one bounding value.
        /// </summary>
        public List<DataValue> CurrentBadPoints { get; set; }

        /// <summary>
        /// A collection of all bad points received between PriorPoint and EarlyPoint. This
        /// is required to compute the status of values extrapolated using both PriorPoint and
        /// EarlyPoint.
        /// </summary>
        public List<DataValue> PriorBadPoints { get; set; }
    }

    /// <summary>
    /// Represents a snapshot or window onto a stream of raw data, presenting an interface helpful to aggregation methods
    /// </summary>
    public class AggregateState : AggregateCursor {
        /// <summary>
        /// Timestamp of the latest raw data point to be input. Note: this is not the most recent
        /// timestamp value that has been input, it is the timestamp of the raw data point most
        /// recently handled.
        /// </summary>
        public DateTime LatestTimestamp { get; set; }

        /// <summary>
        /// StatusCode of the latest raw data point to be input. Note: this is not the most recent
        /// StatusCode value that has been input, it is the StatusCode of the raw data point most
        /// recently handled.
        /// </summary>
        public StatusCode LatestStatus { get; set; }

        /// <summary>
        /// Indicates that no more data will be provided, regardless of whether we have enough
        /// to calculate good values for all of the remaining aggregation intervals
        /// </summary>
        public bool HasTerminated { get; set; }

        /// <summary>
        /// Provides contextual details of the aggregation
        /// </summary>
        private readonly IAggregationContext _aggregationContext;

        /// <summary>
        /// Something to call back on when we are ready to produce processed data points
        /// </summary>
        private readonly IAggregationActor _aggregationActor;

        /// <summary>
        /// Creates a new instance.
        /// </summary>>
        public AggregateState(IAggregationContext context, IAggregationActor actor) {
            _aggregationContext = context;
            _aggregationActor = actor;
            CurrentBadPoints = new List<DataValue>();
            PriorBadPoints = new List<DataValue>();
        }

        /// <summary>
        /// Use the TreatUncertainAsBad directive to determine whether a raw data point is a
        /// good value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool RawValueIsGood(DataValue value) {
            if (_aggregationContext.TreatUncertainAsBad) {
                return StatusCode.IsGood(value.StatusCode);
            }
            else {
                return !StatusCode.IsBad(value.StatusCode);
            }
        }

        /// <summary>
        /// Returns a -1 if we are not yet far enough into the stream of raw data points to
        /// be in the time range of the aggregation. Once we are in the time range, the return
        /// value will be 0. After we have left the time range, the return value will be 1.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int RawValueInWindow(DataValue value) {
            var retval = -1;
            if (_aggregationContext.IsReverseAggregation) {
                if (value.SourceTimestamp <= _aggregationContext.EndTime) {
                    retval = 0;
                }

                if (value.SourceTimestamp <= _aggregationContext.StartTime) {
                    retval = 1;
                }
            }
            else {
                if (value.SourceTimestamp >= _aggregationContext.StartTime) {
                    retval = 0;
                }

                if (value.SourceTimestamp >= _aggregationContext.EndTime) {
                    retval = 1;
                }
            }
            return retval;
        }

        /// <summary>
        /// Accept one raw data value.
        /// </summary>
        /// <param name="rawData"></param>
        public void AddRawData(DataValue rawData) {
            if (rawData == null) {
                throw new ArgumentException("Attempted to add null value instead of valid DataValue");
            }

            LatestTimestamp = rawData.SourceTimestamp;
            LatestStatus = rawData.StatusCode;
            var relevance = RawValueInWindow(rawData);
            if (RawValueIsGood(rawData)) {
                switch (relevance) {
                    case -1:
                        PriorPoint = EarlyPoint;
                        PriorBadPoints = CurrentBadPoints;
                        EarlyPoint = rawData;
                        CurrentBadPoints = new List<DataValue>();
                        break;
                    case 0:
                        if (EarlyPoint == null) {
                            PriorBadPoints = CurrentBadPoints;
                            EarlyPoint = rawData;
                            CurrentBadPoints = new List<DataValue>();
                            _aggregationActor.UpdateProcessedData(rawData, this);
                        }
                        else {
                            LatePoint = rawData;
                            _aggregationActor.UpdateProcessedData(rawData, this);
                            PriorPoint = EarlyPoint;
                            PriorBadPoints = CurrentBadPoints;
                            EarlyPoint = rawData;
                            LatePoint = null;
                            CurrentBadPoints = new List<DataValue>();
                        }
                        break;
                    case 1:
                        if (LatePoint == null) {
                            LatePoint = rawData;
                        }

                        _aggregationActor.UpdateProcessedData(rawData, this);
                        break;
                    default:
                        break;
                }
            }
            else {
                if (LatePoint == null) {
                    CurrentBadPoints.Add(rawData);
                    if (relevance >= 0) {
                        _aggregationActor.UpdateProcessedData(rawData, this);
                    }
                }
            }
        }

        /// <summary>
        /// Call once to indicate that the end of the sequence of raw data points has been
        /// reached.
        /// </summary>
        public void EndOfData() {
            HasTerminated = true;
            LatestTimestamp = DateTime.MaxValue;
            LatestStatus = StatusCodes.GoodNoData;
            _aggregationActor.UpdateProcessedData(null, this);
        }
    }
}
