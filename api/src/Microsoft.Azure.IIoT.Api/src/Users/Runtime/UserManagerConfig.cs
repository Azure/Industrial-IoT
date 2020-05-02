// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Identity.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class UserManagerConfig : ApiConfigBase, IUserManagerConfig {

        /// <summary>
        /// Identitymanager configuration
        /// </summary>
        private const string kIdentityServiceUrlKey = "IdentityServiceUrl";

        /// <summary>Identitymanager service endpoint url</summary>
        public string IdentityServiceUrl => GetStringOrDefault(
            kIdentityServiceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_USERS_SERVICE_URL,
                () => GetDefaultUrl("9048", "users")));

        /// <inheritdoc/>
        public UserManagerConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
