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

    /// <summary>
    /// An interface that allows the basic information about an aggregate query to be
    /// communicated
    /// </summary>
    public interface IAggregationContext {
        /// <summary>
        /// The start of the time window we are aggregating over. Note this may be later
        /// than the EndTime.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// The end time of the window we are aggregating over, if known. Note this may be
        /// earlier than the StartTime.
        /// </summary>
        DateTime EndTime { get; }

        /// <summary>
        /// The size (in milliseconds) of each sampling interval in the time window. If this
        /// is zero, then the entire window is treated as one sampling interval.
        /// </summary>
        double ProcessingInterval { get; }

        /// <summary>
        /// Indicates that the time window for aggregation has a start time later than its
        /// end time, and that raw data will be presented in reverse order.
        /// This value is computed from StartTime and EndTime, however EndTime will be null
        /// if the aggregation is used as a filter in a subscription.
        /// </summary>
        bool IsReverseAggregation { get; }

        /// <summary>
        /// The maximum percentage of points in a sampling interval that may be bad  for
        /// the processed value to have a non-bad status
        /// </summary>
        byte PercentDataBad { get; }

        /// <summary>
        /// The minimum percentage of points in a sampling interval that must be good
        /// for the processed value to have a good status
        /// </summary>
        byte PercentDataGood { get; }

        /// <summary>
        /// Indicator thet determines whether stepped or sloped extrapolation should
        /// be used
        /// </summary>
        bool UseSlopedExtrapolation { get; }

        /// <summary>
        /// Indicator that determines whether stepped or sloped interpolation should
        /// be used
        /// </summary>
        bool SteppedVariable { get; }

        /// <summary>
        /// Indicates that raw data points with status Uncertain should be handled as if they
        /// were bad points rather than as good points.
        /// </summary>
        bool TreatUncertainAsBad { get; }
    }
}
