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
    /// All aggregators implement this interface. It describes the relationship between the
    /// aggregator and any TimeSlice instances processed by it.
    /// </summary>
    public interface IAggregator {

        /// <summary>
        /// Compute a processed value from raw values in a slice of time.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bucket"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        DataValue Compute(IAggregationContext context, TimeSlice bucket, AggregateState state);

        /// <summary>
        /// Determine whether there is sufficient data in a TimeSlice with respect to the
        /// AggregateState to permit reliable computation of a processed value. This decision
        /// is largely governed by the requirements for interpolation or extrapolation.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        bool WaitForMoreData(TimeSlice bucket, AggregateState state);

        /// <summary>
        /// Take snapshot data from the AggregationState in order to determine bounding values
        /// for the TimeSlice.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="state"></param>
        void UpdateBoundingValues(TimeSlice bucket, AggregateState state);
    }
}
