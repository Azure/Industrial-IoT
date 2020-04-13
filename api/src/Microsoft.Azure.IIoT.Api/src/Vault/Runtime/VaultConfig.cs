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

        /// <summary>OPC vault service endpoint url</summary>
        public string OpcUaVaultServiceUrl => GetStringOrDefault(
            kOpcUaVaultServiceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_VAULT_SERVICE_URL,
                () => GetDefaultUrl("9044", "vault")));

        /// <inheritdoc/>
        public VaultConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
