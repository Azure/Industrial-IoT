// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class VaultConfig : ApiConfigBase, IVaultConfig {

        /// <summary>
        /// Vault configuration
        /// </summary>
        private const string kOpcUaVaultServiceUrlKey = "OpcVaultServiceUrl";
        private const string kOpcUaVaultServiceIdKey = "OpcVaultServiceResourceId";

        /// <summary>OPC vault service endpoint url</summary>
        public string OpcUaVaultServiceUrl => GetStringOrDefault(
            kOpcUaVaultServiceUrlKey, GetStringOrDefault(
                "PCS_VAULT_SERVICE_URL", GetDefaultUrl("9044", "vault")));
        /// <summary>OPC vault audience</summary>
        public string OpcUaVaultServiceResourceId => GetStringOrDefault(
            kOpcUaVaultServiceIdKey, GetStringOrDefault("OPC_VAULT_APP_ID",
                GetStringOrDefault("PCS_AUTH_AUDIENCE", null)));

        /// <inheritdoc/>
        public VaultConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
