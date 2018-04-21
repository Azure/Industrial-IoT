// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Common.Diagnostics {
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Logger implementation
    /// </summary>
    public class TraceLogger : BaseLogger {

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="processId"></param>
        public TraceLogger(string processId) :
            base(processId) {
        }

        /// <summary>
        /// Log debug
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Debug(Func<string> message) =>
            Trace.WriteLine(message());

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Info(Func<string> message) =>
            Trace.TraceInformation(message());

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Warn(Func<string> message) =>
            Trace.TraceWarning(message());

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="message"></param>
        protected override sealed void Error(Func<string> message) =>
            Trace.TraceError(message());
    }
}
