// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System;

    /// <summary>
    /// Identity token update configuration
    /// </summary>
    public interface IIdentityTokenUpdaterConfig {

        /// <summary>
        /// Length of token to create
        /// </summary>
        int TokenLength { get; }

        /// <summary>
        /// Lifetime
        /// </summary>
        TimeSpan TokenLifetime { get; }

        /// <summary>
        /// Staleness interval
        /// </summary>
        TimeSpan TokenStaleInterval { get; }

        /// <summary>
        /// Update interval
        /// </summary>
        TimeSpan UpdateInterval { get; }
    }
}