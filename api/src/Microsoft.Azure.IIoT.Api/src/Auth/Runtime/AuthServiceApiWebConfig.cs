// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Auth webapp to service configuration
    /// </summary>
    public class AuthServiceApiWebConfig : ApiConfigBase, IOAuthClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_IsDisabledKey = "Auth:IsDisabled";
        private const string kAuth_AppIdKey = "Auth:AppId";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";
        private const string kAuth_AudienceKey = "Auth:Audience";

        /// <inheritdoc/>
        public bool IsValid => GetBoolOrDefault(kAuth_IsDisabledKey,
            () => GetBoolOrDefault(PcsVariable.PCS_AUTH_SERVICE_DISABLED,
                () => false));
        /// <summary>Provider</summary>
        public string Provider => AuthProvider.AuthService;
        /// <summary>Applicable resource</summary>
        public string Resource => Http.Resource.Platform;
        /// <summary>Application id</summary>
        public string ClientId =>  GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_CLIENT_APPID,
                () => "F095B8821F4F4604B6E3AD1110EE58A4"))?.Trim();
        /// <summary>App secret</summary>
        public string ClientSecret => GetStringOrDefault(kAuth_AppSecretKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_CLIENT_SECRET,
                () => null))?.Trim();
        /// <summary>Auth server instance url</summary>
        public string InstanceUrl =>
            GetStringOrDefault(kAuth_InstanceUrlKey,
                () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_URL,
                    () => GetDefaultUrl("9090", "auth")));
        /// <summary>Valid audience</summary>
        public string Audience =>
            GetStringOrDefault(kAuth_AudienceKey,
                () => GetStringOrDefault(PcsVariable.PCS_SERVICE_NAME,
                    () => "iiot"))?.Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => null;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AuthServiceApiWebConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
