// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Auth api client configuration
    /// </summary>
    public class ApiClientConfig : DiagnosticsConfig, IClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Auth:AppId";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        private const string kAuth_TenantIdKey = "Auth:TenantId";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";
        private const string kAuth_DomainKey = "Auth:Domain";

        /// <summary>Application id</summary>
        public string AppId => GetStringOrDefault(kAuth_AppIdKey,
            GetStringOrDefault("PCS_AUTH_CLIENT_APPID",
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_APPID")))?.Trim();
        /// <summary>App secret</summary>
        public string AppSecret => GetStringOrDefault(kAuth_AppSecretKey,
            GetStringOrDefault("PCS_AUTH_CLIENT_SECRET",
            GetStringOrDefault("PCS_APPLICATION_SECRET")))?.Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            GetStringOrDefault("PCS_AUTH_TENANT",
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_TENANT", "common")))?.Trim();
        /// <summary>Aad instance url</summary>
        public string InstanceUrl => GetStringOrDefault(kAuth_InstanceUrlKey,
            GetStringOrDefault("PCS_AUTH_INSTANCE",
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_AUTHORITY",
                "https://login.microsoftonline.com")))?.Trim();
        /// <summary>Aad domain</summary>
        public string Domain => GetStringOrDefault(kAuth_DomainKey,
            GetStringOrDefault("PCS_AUTH_DOMAIN", Try.Op(() =>
            new Uri(GetStringOrDefault("PCS_AUTH_AUDIENCE")).DnsSafeHost)))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ApiClientConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
