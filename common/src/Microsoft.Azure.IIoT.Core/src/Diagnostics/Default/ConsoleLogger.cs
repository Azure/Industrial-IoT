// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Serilog;
    using Serilog.Events;

    /// <summary>
    /// Console logger
    /// </summary>
    public class ConsoleLogger : ILoggerProvider {

        /// <inheritdoc/>
        public ILogger Logger { get; }

        /// <summary>
        /// Create console logger
        /// </summary>
        /// <param name="config"></param>
        public ConsoleLogger(LoggerConfiguration config = null) {
            Logger = (config ?? new LoggerConfiguration()).Console().CreateLogger();
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <returns></returns>
        public static ILogger Create(LogEventLevel? level = null) {
            if (level == null) {
#if DEBUG
                level = LogEventLevel.Debug;
#else
                level = LogEventLevel.Information;
#endif
            }
            return new ConsoleLogger(new LoggerConfiguration()
                .MinimumLevel.Is(level.Value)).Logger;
        }
    }
}
