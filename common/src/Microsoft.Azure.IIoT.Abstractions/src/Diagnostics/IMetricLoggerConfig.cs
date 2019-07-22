// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Diagnostics {

    /// <summary>
    /// Metric logger configuration
    /// </summary>
    public interface IMetricLoggerConfig {

        /// <summary>
        /// Application Insights Instrumentation key
        /// </summary>
        string ApplicationInsightsInstrumentationKey { get; }
    }
}
