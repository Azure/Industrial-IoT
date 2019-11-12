// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {

    /// <summary>
    /// Uses developer tool authentication
    /// </summary>
    public class VsAuthenticationProvider : AppAuthenticationProvider {
        /// <summary>
        /// Create auth provider
        /// </summary>
        public VsAuthenticationProvider() :
            base(null) {
        }

        /// <inheritdoc/>
        protected override string NoClientIdRunAs() {
            return "RunAs=Developer; DeveloperTool=VisualStudio";
        }
    }
}
