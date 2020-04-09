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
    public class IdentityConfig : ApiConfigBase, IIdentityConfig {

        /// <summary>
        /// Identitymanager configuration
        /// </summary>
        private const string kIdentityServiceUrlKey = "IdentityServiceUrl";
        private const string kIdentityServiceIdKey = "IdentityServiceResourceId";

        /// <summary>Identitymanager service endpoint url</summary>
        public string IdentityServiceUrl => GetStringOrDefault(
            kIdentityServiceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_TWIN_SERVICE_URL,
                () => GetDefaultUrl("9048", "identitymanager")));
        /// <summary>Identitymanager service audience</summary>
        public string IdentityServiceResourceId => GetStringOrDefault(
            kIdentityServiceIdKey,
            () => GetStringOrDefault("USERS_APP_ID",
                () => GetStringOrDefault(PcsVariable.PCS_AUTH_AUDIENCE,
                    () => null)));

        /// <inheritdoc/>
        public IdentityConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
