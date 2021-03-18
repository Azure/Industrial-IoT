// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics.AppInsights.Default {
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Application Insights telemetry initializer that uses IProcessIdentity to set cloud role name.
    /// </summary>
    public class ApplicationInsightsTelemetryInitializer : ITelemetryInitializer {

        private readonly IProcessIdentity _processIdentity;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="processIdentity"></param>
        public ApplicationInsightsTelemetryInitializer(IProcessIdentity processIdentity = null) {
            _processIdentity = processIdentity;
        }

        /// <inheritdoc/>
        public void Initialize(ITelemetry telemetry) {
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName)) {
                //set custom role name here
                telemetry.Context.Cloud.RoleName = _processIdentity?.Name;
            }
        }
    }
}
