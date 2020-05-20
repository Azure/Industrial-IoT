// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// User principal to keyvault configuration
    /// </summary>
    public class AadUserKeyVaultConfig : ConfigBase, IOAuthClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Aad:AppId";
        private const string kAuth_TenantIdKey = "Aad:TenantId";
        private const string kAuth_AuthorityUrlKey = "Aad:AuthorityUrl";

        /// <summary>Audience</summary>
        public string Audience => "https://vault.azure.net";
        /// <summary>Resource</summary>
        public string Resource => Http.Resource.KeyVault;
        /// <inheritdoc/>
        public bool IsValid => ClientId != null && TenantId != null;
        /// <summary>Provider</summary>
        public string Provider => AuthProvider.AzureAD;
        /// <summary>Application id</summary>
        public string ClientId => GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_PUBLIC_CLIENT_APPID,
                () => null))?.Trim();
        /// <summary>App secret</summary>
        public string ClientSecret => null;
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_TENANT,
                () => null))?.Trim();
        /// <summary>Authority url</summary>
        public string InstanceUrl => GetStringOrDefault(kAuth_AuthorityUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_INSTANCE,
                () => "https://login.microsoftonline.com")).Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AadUserKeyVaultConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
