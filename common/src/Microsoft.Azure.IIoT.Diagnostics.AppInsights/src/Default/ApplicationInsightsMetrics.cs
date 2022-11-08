// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using System.Collections.Generic;

    /// <summary>
    /// Metric logger
    /// </summary>
    public sealed class ApplicationInsightsMetrics : IMetricsLogger  {

        /// <summary>
        /// Create metric logger
        /// </summary>
        /// <param name="config"></param>
        public ApplicationInsightsMetrics(IDiagnosticsConfig config) {
#pragma warning disable CS0618 // Type or member is obsolete
            _telemetryClient = new TelemetryClient(
                new TelemetryConfiguration(config.InstrumentationKey));
#pragma warning restore CS0618 // Type or member is obsolete
        }

         /// <inheritdoc/>
        public void TrackEvent(string name) {
            _telemetryClient.GetMetric("trackEvent-" + name).TrackValue(1);
        }

        /// <inheritdoc/>
        public void TrackValue(string name, int value) {
            _telemetryClient.GetMetric("trackValue-" + name).TrackValue(value);
        }

        /// <inheritdoc/>
        public void TrackDuration(string name, double milliseconds) {
            var metrics = new Dictionary<string, double>
                {{"processingTime-" + name, milliseconds}};
            _telemetryClient.TrackEvent("processingTime-" + name, null, metrics);
        }

        private readonly TelemetryClient _telemetryClient;
    }
}