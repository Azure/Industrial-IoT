// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth.Runtime {
    using Microsoft.Azure.IIoT.Auth.Azure;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Auth client configuration
    /// </summary>
    public class ClientConfig : ConfigBase, IClientConfig {

        /// <summary>
        /// Application configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Auth:AppId";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        private const string kAuth_TenantIdKey = "Auth:TenantId";
        private const string kAuth_AuthorityKey = "Auth:Authority";

        /// <summary>Application id</summary>
        public string AppId => GetStringOrDefault(kAuth_AppIdKey, GetStringOrDefault(
            _serviceId + "_APP_ID", GetStringOrDefault("PCS_WEBUI_AUTH_AAD_APPID"))).Trim();
        /// <summary>App secret for example for behalf of flow</summary>
        public string AppSecret => GetStringOrDefault(kAuth_AppSecretKey, GetStringOrDefault(
            _serviceId + "_APP_KEY", GetStringOrDefault("PCS_APPLICATION_SECRET"))).Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_TENANT"));
        /// <summary>Authority</summary>
        public string Authority => GetStringOrDefault(kAuth_AuthorityKey,
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_AUTHORITY", string.IsNullOrEmpty(TenantId) ?
                null : $"https://login.windows.net/{TenantId}/"));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="serviceId"></param>
        public ClientConfig(IConfigurationRoot configuration, string serviceId = "") :
            base(configuration) {
            _serviceId = serviceId?.ToUpperInvariant() ??
                throw new ArgumentNullException(nameof(serviceId));
        }

        private readonly string _serviceId;
    }
}
