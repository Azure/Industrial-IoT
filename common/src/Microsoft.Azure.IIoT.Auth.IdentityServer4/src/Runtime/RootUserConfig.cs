// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Root user configuration
    /// </summary>
    public class RootUserConfig : ConfigBase, IRootUserConfig {

        /// <summary>
        /// Root configuration
        /// </summary>
        private const string kUserName = "Root:UserName";
        private const string kPassword = "Root:Password";

        /// <inheritdoc/>
        public string UserName => GetStringOrDefault(kUserName,
            () => GetStringOrDefault(PcsVariable.PCS_ROOT_USERID));
        /// <inheritdoc/>
        public string Password => GetStringOrDefault(kPassword,
            () => GetStringOrDefault(PcsVariable.PCS_ROOT_PASSWORD));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public RootUserConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
