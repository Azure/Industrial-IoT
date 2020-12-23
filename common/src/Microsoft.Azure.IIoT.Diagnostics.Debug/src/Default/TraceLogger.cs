// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;
    using Serilog.Events;

    /// <summary>
    /// Trace logger
    /// </summary>
    public class TraceLogger : ILoggerProvider {

        /// <inheritdoc/>
        public ILogger Logger { get; }

        /// <summary>
        /// Create console logger
        /// </summary>
        /// <param name="config"></param>
        /// <param name="addConsole"></param>
        public TraceLogger(LoggerConfiguration config = null, bool addConsole = true) {
            Logger = (config ?? new LoggerConfiguration())
#if DEBUG
                .Debug(addConsole)
#else
                .Trace(addConsole)
#endif
                .CreateLogger();
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
