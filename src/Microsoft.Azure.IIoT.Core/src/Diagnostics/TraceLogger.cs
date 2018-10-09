// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Newtonsoft.Json;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Simple trace Logger implementation
    /// </summary>
    public class TraceLogger : ILogger {

        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>
        /// Create logger
        /// </summary>
        public TraceLogger() :
            this(null) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="config">Log configuration</param>
        public TraceLogger(ILogConfig config) :
            this("", config?.LogLevel ?? LogLevel.Debug, config?.ProcessId) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="name"></param>
        /// <param name="loggingLevel"></param>
        /// <param name="processId">A unique id identifying
        /// the process for which the derived logger is
        /// logging output.</param>
        internal TraceLogger(string name, LogLevel loggingLevel,
            string processId = null) {
            _processId = processId ?? Guid.NewGuid().ToString();
            _loggingLevel = loggingLevel;
            Name = name;
        }

        /// <inheritdoc/>
        public ILogger Create(string name) =>
            new TraceLogger(name, _loggingLevel, _processId);

        /// <inheritdoc/>
        public void Log(string method, string file, int lineNumber,
            LogLevel level, Exception exception, string message,
            params object[] parameters) {
            var time = DateTimeOffset.UtcNow.ToString("u");
            message = $"[{level}][{_processId}][{time}][{method}] {message}";
            if (parameters != null && parameters.Length != 0) {
                message += $" ({JsonConvertEx.SerializeObject(parameters)})";
            }
            Trace.WriteLineIf(_loggingLevel <= level, message, level.ToString());
        }

        private readonly string _processId;
        private readonly LogLevel _loggingLevel;
    }
}
