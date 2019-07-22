// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Diagnostics {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Metric logger configuration
    /// </summary>
    public class MetricLoggerConfig : ConfigBase, IMetricLoggerConfig {

        private const string InstrumentationKey = "PCS_APPINSIGHTS_INSTRUMENTATIONKEY";

        /// <summary> ApplicationInsightsInstrumentationKey </summary>
        public string ApplicationInsightsInstrumentationKey => GetStringOrDefault(InstrumentationKey, null);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public MetricLoggerConfig(IConfigurationRoot configuration) :
        base(configuration) {
        }
    }
}
