// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {

    /// <summary>
    /// Diagnostic configuration
    /// </summary>
    public interface IDiagnosticsConfig {

        /// <summary>
        /// Instrumentation key if exists
        /// </summary>
        string InstrumentationKey { get; }

        /// <summary>
        /// Application Insights minimum log level
        /// </summary>
        string LogLevel { get; }
    }
}
