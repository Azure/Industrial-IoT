// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Azure {

    /// <summary>
    /// Configuration for AAD auth
    /// </summary>
    public interface IClientConfig {

        /// <summary>
        /// The AAD application id for the client.
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// AAD Client / Application secret (optional)
        /// </summary>
        string ClientSecret { get; }

        /// <summary>
        /// Tenant id if any (optional)
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// Instance or authority (optional)
        /// </summary>
        string Authority { get; }
    }
}