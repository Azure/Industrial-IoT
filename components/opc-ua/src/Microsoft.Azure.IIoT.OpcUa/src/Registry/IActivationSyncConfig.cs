// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using System;

    /// <summary>
    /// Endpoint sync configuration
    /// </summary>
    public interface IActivationSyncConfig {

        /// <summary>
        /// Update interval
        /// </summary>
        TimeSpan SyncInterval { get; }
    }
}
