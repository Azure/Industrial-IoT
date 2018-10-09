// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Diagnostics {
    using Microsoft.Extensions.Logging;
    using System;

    using ICommonLogger = IIoT.Diagnostics.ILogger;
    using CommonLogLevel = IIoT.Diagnostics.LogLevel;
    using ICommonLogConfig = IIoT.Diagnostics.ILogConfig;

    /// <summary>
    /// Logger implementation
    /// </summary>
    public class ExtensionLogger : ICommonLogger {

        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="factory"></param>
        public ExtensionLogger(ILoggerFactory factory) :
            this("", factory) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="config"></param>
        public ExtensionLogger(ILoggerFactory factory,
            ICommonLogConfig config) :
            this(config?.ProcessId ?? "", factory) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="name"></param>
        internal ExtensionLogger(string name, ILoggerFactory factory) {
            _factory = factory ??
                throw new ArgumentNullException(nameof(factory));
            _logger = factory.CreateLogger(name);
        }

        /// <inheritdoc/>
        public ICommonLogger Create(string name) =>
            new ExtensionLogger(Name + "_" + name, _factory);

        /// <inheritdoc/>
        public void Log(string method, string file, int lineNumber,
            CommonLogLevel level, Exception exception, string message,
            params object[] parameters) {
            _logger.Log((LogLevel)level, exception, message, parameters);
        }

        private readonly ILogger _logger;
        private readonly ILoggerFactory _factory;
    }
}
