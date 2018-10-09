// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Console Logger implementation
    /// </summary>
    public class ConsoleLogger : BaseLogger {

        /// <summary>
        /// Create logger
        /// </summary>
        public ConsoleLogger() :
            this(null) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="config">Log configuration</param>
        public ConsoleLogger(ILogConfig config) :
            this("", config?.LogLevel ?? LogLevel.Debug, config?.ProcessId) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="name"></param>
        /// <param name="processId"></param>
        /// <param name="loggingLevel"></param>
        public ConsoleLogger(string name, LogLevel loggingLevel,
            string processId = null) :
            base(name, loggingLevel, processId) {
            _logLevel = loggingLevel;
        }

        /// <inheritdoc/>
        public override ILogger Create(string name) =>
            new ConsoleLogger(name, _loggingLevel, _processId);

        /// <inheritdoc/>
        protected override void WriteLine(string preamble, string message,
            object[] parameters) {
            message = $"{preamble} {message}";
            if (parameters != null && parameters.Length != 0) {
                message += $" ({JsonConvertEx.SerializeObject(parameters)})";
            }
            Console.WriteLine(message);
        }

        /// <summary>Enabled min log level</summary>
        protected readonly LogLevel _logLevel;
    }
}
