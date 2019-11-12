// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {

    /// <summary>
    /// Metric logger interface
    /// </summary>
    public interface IMetricsLogger {

        /// <summary>
        /// Define a new metric with the provided name that
        /// can be used to track number of times something has happened.
        /// </summary>
        /// <param name="name">The name of the metric to define</param>
        void TrackEvent(string name);

        /// <summary>
        /// Define a new metric with the provided name that can
        /// be used to track increase/decrease in values for that metric.
        /// </summary>
        /// <param name="name">The name of the metric to define</param>
        /// <param name="value">The name of the metric to define</param>
        void TrackValue(string name, int value);

        /// <summary>
        /// Define a new metric with the provided name that can be
        /// used to measure the duration of a type of event/metric.
        /// </summary>
        /// <param name="name">The name of the metric to define</param>
        /// <param name="milliseconds">The name of the metric to define</param>
        void TrackDuration(string name, double milliseconds);
    }
}
