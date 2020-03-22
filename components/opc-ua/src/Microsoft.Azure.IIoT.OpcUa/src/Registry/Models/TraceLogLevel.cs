// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    /// <summary>
    /// Log level
    /// </summary>
    public enum TraceLogLevel {

        /// <summary>
        /// Error only
        /// </summary>
        Error = 0,

        /// <summary>
        /// Default
        /// </summary>
        Information = 1,

        /// <summary>
        /// Debug log
        /// </summary>
        Debug = 2,

        /// <summary>
        /// Verbose
        /// </summary>
        Verbose = 3
    }
}
