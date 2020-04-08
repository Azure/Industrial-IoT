// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Auth client to service configuration
    /// </summary>
    public class AuthServiceApiClientConfig : ApiConfigBase, IOAuthClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_IsDisabledKey = "Auth:IsDisabled";
        private const string kAuth_AppIdKey = "Auth:AppId";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";

        /// <summary>Auth server disabled or not</summary>
        protected bool IsDisabled => GetBoolOrDefault(kAuth_IsDisabledKey,
            () => GetBoolOrDefault(PcsVariable.PCS_AUTH_SERVICE_DISABLED,
                () => false));

        /// <summary>Application id</summary>
        public string AppId => GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_SERVICE_APPID))?.Trim();
        /// <summary>App secret</summary>
        public string AppSecret => GetStringOrDefault(kAuth_AppSecretKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_SERVICE_SECRET))?.Trim();
        /// <summary>Auth server instance url</summary>
        public string InstanceUrl => IsDisabled ? null :
            GetStringOrDefault(kAuth_InstanceUrlKey,
                () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_URL,
                    () => GetDefaultUrl("9090", "auth")));

        /// <summary>Optional tenant</summary>
        public string TenantId => null;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AuthServiceApiClientConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }

}
