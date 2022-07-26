// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;
    using Serilog.Events;
    using System;

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

            LogEventLevel logEventLevel;

            if (config?.LogLevel != null && Enum.IsDefined(typeof(LogEventLevel), config.LogLevel))
                logEventLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), config.LogLevel);
            else
                logEventLevel = LogEventLevel.Information;

#pragma warning disable CS0618 // Type or member is obsolete
            Logger = (log ?? new LoggerConfiguration()).Configure((c, m) => c
                .WriteTo.ApplicationInsights(new ApplicationInsights.Extensibility.TelemetryConfiguration(config?.InstrumentationKey),
                    TelemetryConverter.Traces,
                    logEventLevel), addConsole)
                .CreateLogger();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
