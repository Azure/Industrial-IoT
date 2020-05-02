// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Auth api web application configuration
    /// </summary>
    public class AadApiWebConfig : DiagnosticsConfig, IOAuthClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Auth:AppId";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        private const string kAuth_TenantIdKey = "Auth:TenantId";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";
        private const string kAuth_AudienceKey = "Auth:Audience";

        /// <inheritdoc/>
        public bool IsValid => ClientId != null && Audience != null;
        /// <summary>Provider</summary>
        public string Provider => AuthProvider.AzureAD;
        /// <summary>Resource</summary>
        public string Resource => Http.Resource.Platform;
        /// <summary>Application id</summary>
        public string ClientId => GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_CONFIDENTIAL_CLIENT_APPID,
            () => GetStringOrDefault("PCS_WEBUI_AUTH_AAD_APPID",
                () => null)))?.Trim();
        /// <summary>App secret</summary>
        public string ClientSecret => GetStringOrDefault(kAuth_AppSecretKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_CONFIDENTIAL_CLIENT_SECRET,
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
        /// <summary>Audience</summary>
        public string Audience => GetStringOrDefault(kAuth_AudienceKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_AUDIENCE,
                () => null))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AadApiWebConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
