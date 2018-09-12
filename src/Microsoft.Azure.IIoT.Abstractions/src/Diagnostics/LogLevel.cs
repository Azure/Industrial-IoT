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
        /// Debug output
        /// </summary>
        Debug = 10,

        /// <summary>
        /// Informational messages
        /// </summary>
        Info = 20,

        /// <summary>
        /// Warnings
        /// </summary>
        Warn = 30,

        /// <summary>
        /// Errors
        /// </summary>
        Error = 40
    }
}
