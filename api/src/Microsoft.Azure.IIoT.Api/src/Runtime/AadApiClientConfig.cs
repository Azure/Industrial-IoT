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
        private const string kAuth_TenantIdKey = "Auth:TenantId";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";
        private const string kAuth_AudienceKey = "Auth:Audience";

        /// <inheritdoc/>
        public bool IsValid => ClientId != null && Audience != null;
        /// <inheritdoc/>
        public string Provider => AuthProvider.AzureAD;
        /// <inheritdoc/>
        public string Resource => Http.Resource.Platform;
        /// <inheritdoc/>
        public string ClientId => GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_PUBLIC_CLIENT_APPID,
                () => null))?.Trim();
        /// <inheritdoc/>
        public string ClientSecret => null;
        /// <inheritdoc/>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_TENANT,
                () => "common"))?.Trim();
        /// <inheritdoc/>
        public string InstanceUrl => GetStringOrDefault(kAuth_InstanceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_INSTANCE,
                () => "https://login.microsoftonline.com"))?.Trim();
        /// <inheritdoc/>
        public string Audience => GetStringOrDefault(kAuth_AudienceKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_AUDIENCE,
                () => null))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AadApiClientConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
