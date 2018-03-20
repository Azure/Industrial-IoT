// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Diagnostics {
    using System;

    /// <summary>
    /// Logging interface
    /// </summary>
    public interface ILogger {

        /// <summary>
        /// Capture message and context
        /// </summary>
        /// <param name="message">debug message</param>
        /// <param name="context">
        /// method where the log message is generated
        /// </param>
        void Debug(string message, Action context);

        /// <summary>
        /// Capture message and context
        /// </summary>
        /// <param name="message">informational message
        /// </param>
        /// <param name="context">
        /// method where the log message is generated
        /// </param>
        void Info(string message, Action context);

        /// <summary>
        /// Capture message and context
        /// </summary>
        /// <param name="message">warning message</param>
        /// <param name="context">
        /// method where the log message is generated
        /// </param>
        void Warn(string message, Action context);

        /// <summary>
        /// Capture message and context
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="context">
        /// method where the log message is generated
        /// </param>
        void Error(string message, Action context);

        /// <summary>
        /// Capture message and some data
        /// </summary>
        /// <param name="message">debug message</param>
        /// <param name="context">
        /// method where the log message is generated
        /// </param>
        void Debug(string message, Func<object> context);

        /// <summary>
        /// Capture message and some data
        /// </summary>
        /// <param name="message">informational message
        /// </param>
        /// <param name="context">
        /// method where the log message is generated
        /// </param>
        void Info(string message, Func<object> context);

        /// <summary>
        /// Capture message and some data
        /// </summary>
        /// <param name="message">warning message</param>
        /// <param name="context">
        /// method where the log message is generated
        /// </param>
        void Warn(string message, Func<object> context);

        /// <summary>
        /// Capture message and some data
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="context">
        /// method where the log message is generated
        /// </param>
        void Error(string message, Func<object> context);
    }
}
