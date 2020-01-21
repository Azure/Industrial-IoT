// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Azure.IIoT.Utils;

    /// <summary>
    /// Metric logger extensions
    /// </summary>
    public static class MetricsLoggerEx {

        /// <summary>
        /// Create time logger
        /// </summary>
        /// <param name="metricLogger"></param>
        /// <param name="watchName"></param>
        /// <returns></returns>
        public static TimeLogger TrackDuration(this IMetricsLogger metricLogger,
            string watchName) {
            return new TimeLogger(watchName, metricLogger);
        }
    }
}
