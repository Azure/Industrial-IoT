// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Auth service principal to storage configuration
    /// </summary>
    public class AadSpStorageConfig : ConfigBase, IOAuthClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Aad:AppId";
        private const string kAuth_AppSecretKey = "Aad:AppSecret";
        private const string kAuth_TenantIdKey = "Aad:TenantId";
        private const string kAuth_AuthorityUrlKey = "Aad:AuthorityUrl";

        /// <summary>Audience</summary>
        public string Audience => "https://storage.azure.com";
        /// <summary>Resource</summary>
        public string Resource => Http.Resource.Storage;
        /// <inheritdoc/>
        public bool IsValid =>
            ClientId != null && ClientSecret != null && TenantId != null;
        /// <summary>Provider</summary>
        public string Provider => AuthProvider.AzureAD;
        /// <summary>Application id</summary>
        public string ClientId => GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_SERVICE_APPID,
                () => null))?.Trim();
        /// <summary>App secret</summary>
        public string ClientSecret => GetStringOrDefault(kAuth_AppSecretKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_SERVICE_SECRET,
                () => null))?.Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_TENANT,
                () => null)).Trim();
        /// <summary>Authority url</summary>
        public string InstanceUrl => GetStringOrDefault(kAuth_AuthorityUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_INSTANCE,
                () => "https://login.microsoftonline.com")).Trim();


        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AadSpStorageConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
