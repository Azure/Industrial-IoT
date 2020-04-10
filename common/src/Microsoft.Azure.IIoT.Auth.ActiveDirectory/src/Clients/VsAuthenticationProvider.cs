// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Serilog;

    /// <summary>
    /// Uses developer tool authentication
    /// </summary>
    public class VsAuthenticationProvider : AppAuthenticationProvider {

        /// <inheritdoc/>
        public VsAuthenticationProvider(ILogger logger) :
            base(logger) {
        }

        /// <inheritdoc/>
        public VsAuthenticationProvider(IClientAuthConfig config, ILogger logger) :
            base(config, logger) {
        }

        /// <inheritdoc/>
        protected override string NoClientIdRunAs() {
            return "RunAs=Developer; DeveloperTool=VisualStudio";
        }
    }
}
