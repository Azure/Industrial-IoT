// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {

    /// <summary>
    /// Configuration for oauth flows
    /// </summary>
    public interface IOAuthConfig {

        /// <summary>
        /// The configuration is valid
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// The provider type.
        /// One of <see cref="AuthProvider"/>.
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// The instance url is the base address of the
        /// authentication server.
        /// </summary>
        string InstanceUrl { get; }

        /// <summary>
        /// Tenant id if any
        /// </summary>
        string TenantId { get; }
    }
}
