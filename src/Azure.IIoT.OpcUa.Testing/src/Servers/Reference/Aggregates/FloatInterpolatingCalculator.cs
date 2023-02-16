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
    /// Calculates aggreates based on the point values.
    /// </summary>
    public abstract class FloatInterpolatingCalculator : InterpolatingCalculator {
        /// <summary>
        /// Updates the bounding values for the time slice.
        /// </summary>
        public override void UpdateBoundingValues(TimeSlice bucket, AggregateState state) {
            var EarlyBound = bucket.EarlyBound;
            var LateBound = bucket.LateBound;
            if (bucket.ExactMatch(state.LatestTimestamp) && StatusCode.IsGood(state.LatestStatus)) {
                EarlyBound.RawPoint = state.LatePoint ?? state.EarlyPoint;
                EarlyBound.DerivationType = BoundingValueType.Raw;
            }
            else {
                if (EarlyBound.DerivationType != BoundingValueType.Raw) {
                    if (EarlyBound.EarlyPoint == null) {
                        if ((state.EarlyPoint != null) && (state.EarlyPoint.SourceTimestamp < bucket.From)) {
                            EarlyBound.EarlyPoint = state.EarlyPoint;
                        }
                    }
                    if (EarlyBound.LatePoint == null) {
                        if ((state.LatePoint != null) && (state.LatePoint.SourceTimestamp >= bucket.From)) {
                            EarlyBound.LatePoint = state.LatePoint;
                            if (SteppedVariable) {
                                EarlyBound.CurrentBadPoints = new List<DataValue>();
                                foreach (var dv in state.CurrentBadPoints) {
                                    if (dv.SourceTimestamp < EarlyBound.Timestamp) {
                                        EarlyBound.CurrentBadPoints.Add(dv);
                                    }
                                }
                            }
                            else {
                                EarlyBound.CurrentBadPoints = state.CurrentBadPoints;
                            }
                            EarlyBound.DerivationType = SteppedVariable ? BoundingValueType.SteppedInterpolation : BoundingValueType.SlopedInterpolation;
                        }
                    }
                }
                if (state.HasTerminated && (state.LatePoint == null)) {
                    if (SteppedVariable) {
                        EarlyBound.CurrentBadPoints = new List<DataValue>();
                        foreach (var dv in state.CurrentBadPoints) {
                            if (dv.SourceTimestamp < EarlyBound.Timestamp) {
                                EarlyBound.CurrentBadPoints.Add(dv);
                            }
                        }
                    }
                    else {
                        EarlyBound.CurrentBadPoints = state.CurrentBadPoints;
                    }
                }
            }

            if (bucket.EndMatch(state.LatestTimestamp) && StatusCode.IsGood(state.LatestStatus)) {
                LateBound.RawPoint = state.LatePoint ?? state.EarlyPoint;
                LateBound.DerivationType = BoundingValueType.Raw;
            }
            else {
                if (LateBound.DerivationType != BoundingValueType.Raw) {
                    if ((state.EarlyPoint != null) && (state.EarlyPoint.SourceTimestamp < bucket.To)) {
                        LateBound.EarlyPoint = state.EarlyPoint;
                    }

                    if (LateBound.LatePoint == null) {
                        if ((state.LatePoint != null) && (state.LatePoint.SourceTimestamp >= bucket.To)) {
                            LateBound.LatePoint = state.LatePoint;
                            if (SteppedVariable) {
                                LateBound.CurrentBadPoints = new List<DataValue>();
                                foreach (var dv in state.CurrentBadPoints) {
                                    if (dv.SourceTimestamp < LateBound.Timestamp) {
                                        LateBound.CurrentBadPoints.Add(dv);
                                    }
                                }
                            }
                            else {
                                LateBound.CurrentBadPoints = state.CurrentBadPoints;
                            }
                            LateBound.DerivationType = SteppedVariable ? BoundingValueType.SteppedInterpolation : BoundingValueType.SlopedInterpolation;
                        }
                    }
                }
                if (state.HasTerminated && (state.LatePoint == null)) {
                    if (SteppedVariable) {
                        LateBound.CurrentBadPoints = new List<DataValue>();
                        foreach (var dv in state.CurrentBadPoints) {
                            if (dv.SourceTimestamp < LateBound.Timestamp) {
                                LateBound.CurrentBadPoints.Add(dv);
                            }
                        }
                    }
                    else {
                        LateBound.CurrentBadPoints = state.CurrentBadPoints;
                    }
                }
                UpdatePriorPoint(LateBound, state);
            }
        }
    }
}
