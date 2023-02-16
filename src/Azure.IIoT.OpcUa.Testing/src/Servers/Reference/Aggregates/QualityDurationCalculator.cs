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
    using System.Collections.Generic;

    /// <summary>
    /// Calculates aggreates based on the quality or duration.
    /// </summary>
    public abstract class QualityDurationCalculator : InterpolatingCalculator {
        /// <summary>
        /// Checks if the point has the status that meets the aggregate criteria.
        /// </summary>
        protected abstract bool RightStatusCode(DataValue dv);

        /// <summary>
        /// Calculates the value for the time slice.
        /// </summary>
        public override DataValue Compute(IAggregationContext context, TimeSlice bucket, AggregateState state) {
            var retval = new DataValue { SourceTimestamp = bucket.From };
            StatusCode code = StatusCodes.Good;
            var previous = new DataValue { SourceTimestamp = bucket.From };
            if (bucket.EarlyBound.Value != null) {
                previous.StatusCode = (StatusCode)bucket.EarlyBound.Value.WrappedValue.Value;
            }
            else {
                previous.StatusCode = StatusCodes.Bad;
            }

            if (!RightStatusCode(previous)) {
                previous = null;
            }

            var total = 0.0;
            foreach (var v in bucket.Values) {
                if (previous != null) {
                    total += (v.SourceTimestamp - previous.SourceTimestamp).TotalMilliseconds;
                }

                if (RightStatusCode(v)) {
                    previous = v;
                }
                else {
                    previous = null;
                }
            }
            if (previous != null) {
                total += (bucket.To - previous.SourceTimestamp).TotalMilliseconds;
            }

            retval.Value = total;
            code.AggregateBits = AggregateBits.Calculated;
            if (bucket.Incomplete) {
                code.AggregateBits |= AggregateBits.Partial;
            }

            retval.StatusCode = code;
            return retval;
        }

        /// <summary>
        /// Updates the bounding values for the time slice.
        /// </summary>
        public override void UpdateBoundingValues(TimeSlice bucket, AggregateState state) {
            var EarlyBound = bucket.EarlyBound;
            var LateBound = bucket.LateBound;
            if (bucket.ExactMatch(state.LatestTimestamp)) {
                EarlyBound.RawPoint = state.LatePoint ?? state.EarlyPoint;
                EarlyBound.DerivationType = BoundingValueType.QualityRaw;
            }
            else {
                if (EarlyBound.DerivationType != BoundingValueType.QualityRaw) {
                    if (EarlyBound.EarlyPoint == null) {
                        if ((state.EarlyPoint != null) && (state.EarlyPoint.SourceTimestamp < bucket.From)) {
                            EarlyBound.EarlyPoint = state.EarlyPoint;
                        }
                    }
                    if (EarlyBound.LatePoint == null) {
                        if ((state.LatePoint != null) && (state.LatePoint.SourceTimestamp >= bucket.From)) {
                            EarlyBound.CurrentBadPoints = new List<DataValue>();
                            foreach (var dv in state.CurrentBadPoints) {
                                if (dv.SourceTimestamp < EarlyBound.Timestamp) {
                                    EarlyBound.CurrentBadPoints.Add(dv);
                                }
                            }

                            EarlyBound.DerivationType = BoundingValueType.QualityInterpolation;
                        }
                    }
                }
                if (state.HasTerminated && (state.LatePoint == null)) {
                    EarlyBound.CurrentBadPoints = new List<DataValue>();
                    foreach (var dv in state.CurrentBadPoints) {
                        if (dv.SourceTimestamp < EarlyBound.Timestamp) {
                            EarlyBound.CurrentBadPoints.Add(dv);
                        }
                    }

                    EarlyBound.DerivationType = BoundingValueType.QualityExtrapolation;
                }
            }

            if (bucket.EndMatch(state.LatestTimestamp)) {
                LateBound.RawPoint = state.LatePoint ?? state.EarlyPoint;
                LateBound.DerivationType = BoundingValueType.QualityRaw;
            }
            else {
                if (LateBound.DerivationType != BoundingValueType.QualityRaw) {
                    if ((state.EarlyPoint != null) && (state.EarlyPoint.SourceTimestamp < bucket.To)) {
                        LateBound.EarlyPoint = state.EarlyPoint;
                    }

                    if (LateBound.LatePoint == null) {
                        if ((state.LatePoint != null) && (state.LatePoint.SourceTimestamp >= bucket.To)) {
                            LateBound.CurrentBadPoints = new List<DataValue>();
                            foreach (var dv in state.CurrentBadPoints) {
                                if (dv.SourceTimestamp < LateBound.Timestamp) {
                                    LateBound.CurrentBadPoints.Add(dv);
                                }
                            }

                            LateBound.DerivationType = BoundingValueType.QualityInterpolation;
                        }
                    }
                }
                if (state.HasTerminated && (state.LatePoint == null)) {
                    LateBound.CurrentBadPoints = new List<DataValue>();
                    foreach (var dv in state.CurrentBadPoints) {
                        if (dv.SourceTimestamp < LateBound.Timestamp) {
                            LateBound.CurrentBadPoints.Add(dv);
                        }
                    }

                    LateBound.DerivationType = BoundingValueType.QualityExtrapolation;
                }
            }
        }
    }
}
