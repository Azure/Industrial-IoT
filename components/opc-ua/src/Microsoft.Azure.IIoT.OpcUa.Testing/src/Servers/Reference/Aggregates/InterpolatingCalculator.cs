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
    /// <summary>
    /// Calculates aggregates with interpolation.
    /// </summary>
    public abstract class InterpolatingCalculator : BaseAggregateCalculator {
        /// <summary>
        /// Returns true if more data is required for the next interval.
        /// </summary>
        public override bool WaitForMoreData(TimeSlice bucket, AggregateState state) {
            if (!state.HasTerminated) {
                if (bucket.ContainsTime(state.LatestTimestamp)) {
                    return true;
                }

                if (IsReverseAggregation) {
                    if (state.LatestTimestamp < bucket.To) {
                        return false;
                    }
                }
                else {
                    if (state.LatestTimestamp > bucket.To) {
                        return false;
                    }
                }

                if ((bucket.EarlyBound.Value == null) || (bucket.LateBound.Value == null)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates the status for the time slice.
        /// </summary>
        protected override StatusCode ComputeStatus(IAggregationContext context, int numGood, int numBad, TimeSlice bucket) {
            var code = (bucket.EarlyBound.Value == null && numGood + numBad == 0) ? // no inital bound, do not extrapolate
                StatusCodes.BadNoData : base.ComputeStatus(context, numGood, numBad, bucket);
            return code;
        }

        /// <summary>
        /// Determines the best good point before the end bound.
        /// </summary>
        protected void UpdatePriorPoint(BoundingValue bound, AggregateState state) {
            if (state.HasTerminated && (state.LatePoint == null) && bound.PriorPoint == null) {
                bound.PriorPoint = state.PriorPoint;
                bound.PriorBadPoints = state.PriorBadPoints;
                bound.DerivationType = UseSlopedExtrapolation ? BoundingValueType.SlopedExtrapolation : BoundingValueType.SteppedExtrapolation;
            }
        }
    }
}
