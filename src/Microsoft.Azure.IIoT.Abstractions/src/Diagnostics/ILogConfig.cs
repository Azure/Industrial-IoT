// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {

    /// <summary>
    /// Logging configuration
    /// </summary>
    public interface ILogConfig {

        /// <summary>
        /// Min log level to output
        /// </summary>
        LogLevel LogLevel { get; }

        /// <summary>
        /// Process id to log as additional context.
        /// </summary>
        string ProcessId { get; }
    }
}
