// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4 {
    using global::IdentityServer4.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Identity server configuration
    /// </summary>
    public interface IIdentityServerConfig {

        /// <summary>
        /// Clients
        /// </summary>
        IEnumerable<Client> Clients { get; }

        /// <summary>
        /// Api resources
        /// </summary>
        IEnumerable<ApiResource> Apis { get; }

        /// <summary>
        /// Identity resources
        /// </summary>
        IEnumerable<IdentityResource> Ids { get; }
    }
}