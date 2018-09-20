// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Logger implementation
    /// </summary>
    public class TraceLogger : BaseLogger {

        /// <summary>
        /// Create logger
        /// </summary>
        public TraceLogger() :
            this(null) {
        }

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="config"></param>
        public TraceLogger(ILogConfig config) :
            base(config?.ProcessId) {
        }

        /// <inheritdoc/>
        protected override sealed void Debug(Func<string> message) =>
            Trace.WriteLine(message());

        /// <inheritdoc/>
        protected override sealed void Info(Func<string> message) =>
            Trace.TraceInformation(message());

        /// <inheritdoc/>
        protected override sealed void Warn(Func<string> message) =>
            Trace.TraceWarning(message());

        /// <inheritdoc/>
        protected override sealed void Error(Func<string> message) =>
            Trace.TraceError(message());
    }
}
