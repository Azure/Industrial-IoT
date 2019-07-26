// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Auth client configuration
    /// </summary>
    public class ClientConfig : ConfigBase, IClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Auth:AppId";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        private const string kAuth_TenantIdKey = "Auth:TenantId";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";
        private const string kAuth_AudienceKey = "Auth:Audience";

        /// <summary>Application id</summary>
        public string AppId => GetStringOrDefault(kAuth_AppIdKey,
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_APPID"))?.Trim();
        /// <summary>App secret</summary>
        public string AppSecret => GetStringOrDefault(kAuth_AppSecretKey,
            GetStringOrDefault("PCS_APPLICATION_SECRET"))?.Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_TENANT", "common")).Trim();
        /// <summary>Aad instance url</summary>
        public string InstanceUrl => GetStringOrDefault(kAuth_InstanceUrlKey,
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_INSTANCE",
                "https://login.microsoftonline.com")).Trim();
        /// <summary>Audience</summary>
        public string Audience => GetStringOrDefault(kAuth_AudienceKey,
            GetStringOrDefault("PCS_AUTH_AUDIENCE", null));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ClientConfig(IConfigurationRoot configuration) :
            base(configuration) {
        }
    }
}
