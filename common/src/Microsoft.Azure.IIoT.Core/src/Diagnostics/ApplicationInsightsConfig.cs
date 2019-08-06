// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Metric logger configuration
    /// </summary>
    public class ApplicationInsightsConfig : ConfigBase, IApplicationInsightsConfig {

        private const string InstrumentationKey = "PCS_APPINSIGHTS_INSTRUMENTATIONKEY";

        /// <summary> Telemetry configuration </summary>
        public TelemetryConfiguration TelemetryConfiguration => GetTelemetryConfiguration();

        private TelemetryConfiguration GetTelemetryConfiguration() {
            return new TelemetryConfiguration(GetStringOrDefault(InstrumentationKey));
        }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ApplicationInsightsConfig(IConfigurationRoot configuration) :
            base(configuration) {
        }
    }
}
