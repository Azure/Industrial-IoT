// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Application insights configuration
    /// </summary>
    public interface IApplicationInsightsConfig {

        /// <summary>
        /// Application insights telemetry configuration
        /// </summary>
        TelemetryConfiguration TelemetryConfiguration { get; }
    }
}
