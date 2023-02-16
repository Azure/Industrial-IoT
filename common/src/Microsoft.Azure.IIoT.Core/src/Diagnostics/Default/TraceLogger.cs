// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;
    using Serilog.Events;
    using System;

    /// <summary>
    /// Trace logger
    /// </summary>
    public class TraceLogger : ILoggerProvider {

        /// <inheritdoc/>
        public ILogger Logger { get; }

        /// <summary>
        /// Create console logger
        /// </summary>
        /// <param name="loggerConfiguration"></param>
        /// <param name="addConsole"></param>
        /// <param name="config"></param>
        public TraceLogger(LoggerConfiguration loggerConfiguration = null, bool addConsole = true,
            IDiagnosticsConfig config = null) {

            Logger = (loggerConfiguration ?? new LoggerConfiguration())
#if DEBUG
                .Debug(addConsole)
#else
                .Trace(addConsole)
#endif
                .CreateLogger();

            var configLevel = config?.LogLevel ?? Environment.GetEnvironmentVariable("LOG_LEVEL");
            if (!string.IsNullOrEmpty(configLevel) && Enum.IsDefined(typeof(LogEventLevel), configLevel)) {
                LogControl.Level.MinimumLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), configLevel);
            }
            else {
#if DEBUG
                LogControl.Level.MinimumLevel = LogEventLevel.Debug;
#else
                LogControl.Level.MinimumLevel = LogEventLevel.Information;
#endif
            }
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="level"></param>
        /// <param name="addConsole"></param>
        /// <returns></returns>
        public static ILogger Create(LogEventLevel? level = null, bool addConsole = true) {
            if (level == null) {
#if DEBUG
                level = LogEventLevel.Debug;
#else
                level = LogEventLevel.Information;
#endif
            }
            return new TraceLogger(new LoggerConfiguration()
                .MinimumLevel.Is(level.Value), addConsole).Logger;
        }
    }
}
