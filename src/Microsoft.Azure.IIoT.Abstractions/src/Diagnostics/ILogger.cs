// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System;

    /// <summary>
    /// Logging interface that combines a logger factory
    /// and logging output and supports structural logging.
    /// </summary>
    public interface ILogger {

        /// <summary>
        /// Name of the logger
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Create new logger with named context
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ILogger Create(string name);

        /// <summary>
        /// Log a new message with context
        /// </summary>
        /// <param name="method"></param>
        /// <param name="level"></param>
        /// <param name="file"></param>
        /// <param name="lineNumber"></param>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        void Log(string method, string file, int lineNumber,
            LogLevel level, Exception exception, string message,
            params object[] parameters);
    }
}
