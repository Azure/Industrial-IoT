// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {

    /// <summary>
    /// Log level setting for logger
    /// </summary>
    public enum LogLevel {

        /// <summary>
        /// Verbose output
        /// </summary>
        Verbose = 0,

        /// <summary>
        /// Trace output
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Informational messages
        /// </summary>
        Info = 2,

        /// <summary>
        /// Warnings
        /// </summary>
        Warn = 3,

        /// <summary>
        /// Errors
        /// </summary>
        Error = 4,

        /// <summary>
        /// Fatal errors
        /// </summary>
        Fatal = 5
    }
}
