// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Azure.IIoT.Utils;
    using System;

    /// <summary>
    /// Logger base implementation
    /// </summary>
    public abstract class BaseLogger : ILogger {

        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="name"></param>
        /// <param name="loggingLevel"></param>
        /// <param name="processId">A unique id identifying
        /// the process for which the derived logger is
        /// logging output.</param>
        protected BaseLogger(string name, LogLevel loggingLevel,
            string processId = null) {
            _processId = processId ?? Guid.NewGuid().ToString();
            _loggingLevel = loggingLevel;
            Name = name;
        }

        /// <inheritdoc/>
        public abstract ILogger Create(string name);

        /// <inheritdoc/>
        public void Log(string method, string file, int lineNumber,
            LogLevel level, Exception exception, string message,
            params object[] parameters) {
            if (_loggingLevel > level) {
                return;
            }
            var time = DateTimeOffset.UtcNow.ToString("u");
            var preamble = $"[{level}][{Name}][{time}][{method}]";
            Try.Op(() => WriteLine(preamble, message, parameters));
        }

        /// <summary>
        /// Write output
        /// </summary>
        /// <param name="preamble"></param>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        protected abstract void WriteLine(string preamble, string message,
            object[] parameters);

        /// <summary>Process id to be used by derived class if needed</summary>
        protected readonly string _processId;
        /// <summary>Loglevel to be used by derived class if needed</summary>
        protected readonly LogLevel _loggingLevel;
    }
}
