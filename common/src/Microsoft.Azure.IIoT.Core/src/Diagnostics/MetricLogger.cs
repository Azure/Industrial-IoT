// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Diagnostics {
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System.Collections.Generic;

    /// <summary>
    /// Metric logger
    /// </summary>
    public sealed class MetricLogger : IMetricLogger  {
        private readonly TelemetryClient telemetryClient;
        private readonly TelemetryConfiguration telemetryConfiguration;

        /// <summary>
        /// Create metric logger
        /// </summary>
        /// <param name="config"></param>
        public MetricLogger(IMetricLoggerConfig config) {
            telemetryConfiguration = TelemetryConfiguration.Active;
            telemetryConfiguration.InstrumentationKey = config.ApplicationInsightsInstrumentationKey;

            telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

         /// <inheritdoc/>
        public void Count(string name) {
            telemetryClient.GetMetric("counter-" + name).TrackValue(1);
            telemetryClient.Flush();
        }

        /// <inheritdoc/>
        public void Store(string name, int value) {
            telemetryClient.GetMetric("gauge-" + name).TrackValue(value);
            telemetryClient.Flush();
        }

        /// <inheritdoc/>
        public void TimeIt(string name, double milliseconds) {
            var metrics = new Dictionary<string, double>
                {{"processingTime-" + name, milliseconds}};
            telemetryClient.TrackEvent("processingTime-" + name, null, metrics);
            telemetryClient.Flush();
        }
    }
}