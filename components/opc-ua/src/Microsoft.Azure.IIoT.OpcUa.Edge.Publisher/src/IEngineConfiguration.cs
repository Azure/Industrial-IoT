// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {
    using System;

    /// <summary>
    /// Engine configuration
    /// </summary>
    public interface IEngineConfiguration {

        /// <summary>
        /// Batch size
        /// </summary>
        int? BatchSize { get; }

        /// <summary>
        /// Diagnostics interval
        /// </summary>
        TimeSpan? DiagnosticsInterval { get; }
    }
}