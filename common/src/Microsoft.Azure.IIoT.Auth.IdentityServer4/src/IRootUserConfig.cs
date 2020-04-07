// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4 {

    /// <summary>
    /// Root user configuration
    /// </summary>
    public interface IRootUserConfig {

        /// <summary>
        /// User name
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Password
        /// </summary>
        string Password { get; }
    }
}