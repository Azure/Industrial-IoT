// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Auth api client configuration
    /// </summary>
    public class AadApiClientConfig : DiagnosticsConfig, IOAuthClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Auth:AppId";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        private const string kAuth_TenantIdKey = "Auth:TenantId";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";

        /// <summary>Scheme</summary>
        public string Scheme => AuthScheme.Aad;
        /// <summary>Audience</summary>
        public string Audience => null;

        /// <summary>Application id</summary>
        public string AppId => GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_CLIENT_APPID,
            () => GetStringOrDefault("PCS_WEBUI_AUTH_AAD_APPID")))?.Trim();
        /// <summary>App secret</summary>
        public string AppSecret => GetStringOrDefault(kAuth_AppSecretKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_CLIENT_SECRET,
            () => GetStringOrDefault("PCS_APPLICATION_SECRET")))?.Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_TENANT,
            () => GetStringOrDefault("PCS_WEBUI_AUTH_AAD_TENANT",
                () => "common")))?.Trim();
        /// <summary>Aad instance url</summary>
        public string InstanceUrl => GetStringOrDefault(kAuth_InstanceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_INSTANCE,
            () => GetStringOrDefault("PCS_WEBUI_AUTH_AAD_AUTHORITY",
                () => "https://login.microsoftonline.com")))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AadApiClientConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
