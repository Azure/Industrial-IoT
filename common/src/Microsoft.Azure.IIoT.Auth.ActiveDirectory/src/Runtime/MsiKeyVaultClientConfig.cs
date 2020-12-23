// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Managed service identity configuration
    /// </summary>
    public class MsiKeyVaultClientConfig : ConfigBase, IOAuthClientConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_AppIdKey = "Msi:AppId";
        private const string kAuth_TenantIdKey = "Msi:TenantId";

        /// <inheritdoc/>
        public string Audience => "https://vault.azure.net";
        /// <inheritdoc/>
        public string Resource => Http.Resource.KeyVault;
        /// <inheritdoc/>
        public bool IsValid => ClientId != null && TenantId != null;
        /// <inheritdoc/>
        public string Provider => AuthProvider.Msi;
        /// <inheritdoc/>
        public string ClientId => GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_MSI_APPID,
                () => null))?.Trim();
        /// <inheritdoc/>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_MSI_TENANT,
                () => null))?.Trim();
        /// <inheritdoc/>
        public string InstanceUrl => null;
        /// <inheritdoc/>
        public string ClientSecret => null;


        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public MsiKeyVaultClientConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
