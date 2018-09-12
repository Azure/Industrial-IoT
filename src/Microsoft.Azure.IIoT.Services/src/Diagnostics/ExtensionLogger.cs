// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System;
    using Microsoft.Extensions.Logging;
    using IAspLogger = Extensions.Logging.ILogger;

    /// <summary>
    /// Logger implementation
    /// </summary>
    public class ExtensionLogger : BaseLogger {

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="factory"></param>
        public ExtensionLogger(ILoggerFactory factory) :
            this(factory, null) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="config"></param>
        public ExtensionLogger(ILoggerFactory factory, ILogConfig config) :
            this(config?.ProcessId ?? "log", factory) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="category"></param>
        internal ExtensionLogger(string category, ILoggerFactory factory) :
            base(category) {
            if (factory == null) {
                throw new ArgumentNullException(nameof(factory));
            }
            _logger = factory.CreateLogger(category);
        }

        /// <summary>
        /// Log debug
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Debug(Func<string> message) =>
            _logger.LogDebug(message());

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Info(Func<string> message) =>
            _logger.LogInformation(message());

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Warn(Func<string> message) =>
            _logger.LogWarning(message());

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Error(Func<string> message) =>
            _logger.LogError(message());

        private readonly IAspLogger _logger;
    }
}
