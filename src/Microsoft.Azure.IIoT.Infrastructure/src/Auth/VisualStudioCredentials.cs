// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Auth {
    using Microsoft.Azure.IIoT.Auth.Azure;

    /// <summary>
    /// Injectable visual studio integrated credentials
    /// </summary>
    public class VisualStudioCredentials : TokenProviderCredentials {

        /// <summary>
        /// Create credential provider
        /// </summary>
        public VisualStudioCredentials() :
            base(new VsAuthenticationProvider(), null) {
        }
    }
}
