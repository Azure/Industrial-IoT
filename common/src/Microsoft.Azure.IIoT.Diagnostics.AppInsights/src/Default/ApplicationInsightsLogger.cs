// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;

    /// <summary>
    /// Application Insights logger
    /// </summary>
    public class ApplicationInsightsLogger : ILoggerProvider {

        /// <inheritdoc/>
        public ILogger Logger { get; }

        /// <summary>
        /// Create telemetry client logger
        /// </summary>
        /// <param name="config"></param>
        /// <param name="log"></param>
        /// <param name="addConsole"></param>
        public ApplicationInsightsLogger(IDiagnosticsConfig config,
            LoggerConfiguration log = null, bool addConsole = true) {
            Logger = (log ?? new LoggerConfiguration()).Configure((c, m) => c
                .WriteTo.ApplicationInsights(config?.InstrumentationKey,
                    TelemetryConverter.Traces), addConsole)
                .CreateLogger();
        }
    }
}
