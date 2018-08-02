// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System;

    /// <summary>
    /// Logger implementation
    /// </summary>
    public class ConsoleLogger : BaseLogger {

        /// <summary>
        /// Create logger
        /// </summary>
        public ConsoleLogger() :
            this(Guid.NewGuid().ToString(), LogLevel.Debug) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="loggingLevel"></param>
        public ConsoleLogger(string processId, LogLevel loggingLevel) :
            base(processId) {
            _logLevel = loggingLevel;
        }

        /// <summary>
        /// Log debug
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Debug(Func<string> message) =>
            Write(_logLevel <= LogLevel.Debug, "[DEBUG]", message);

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Info(Func<string> message) =>
             Write(_logLevel <= LogLevel.Info, " [INFO]", message);

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Warn(Func<string> message) =>
             Write(_logLevel <= LogLevel.Warn, " [WARN]", message);

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Error(Func<string> message) =>
            Write(_logLevel <= LogLevel.Error, "[ERROR]", message);

        /// <summary>
        /// Write message to console
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        private void Write(bool enabled, string level, Func<string> message) {
            if (enabled) {
                Console.WriteLine($"{level}{message()}");
            }
        }

        protected readonly LogLevel _logLevel;
    }
}
