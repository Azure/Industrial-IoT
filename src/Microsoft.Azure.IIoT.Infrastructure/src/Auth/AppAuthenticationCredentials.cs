// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Auth {
    using Microsoft.Azure.IIoT.Auth.Azure;

    /// <summary>
    /// Injectable service credentials
    /// </summary>
    public class AppAuthenticationCredentials : TokenProviderCredentials {

        /// <summary>
        /// Create credential provider
        /// </summary>
        /// <param name="config"></param>
        public AppAuthenticationCredentials(IClientConfig config) :
            base(new AppAuthenticationProvider(config), config) {
        }
    }
}
