// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Runtime {
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;

    /// <summary>
    /// Secret manager
    /// </summary>
    public class PrefixKeyVaultSecretManager : IKeyVaultSecretManager {
        private readonly string _prefix;

        /// <summary>
        /// Create secret manager
        /// </summary>
        /// <param name="prefix"></param>
        public PrefixKeyVaultSecretManager(string prefix) {
            _prefix = $"{prefix}-";
        }

        /// <inheritdoc/>
        public bool Load(SecretItem secret) {
            // Load a vault secret when its secret name starts with the
            // prefix. Other secrets won't be loaded.
            return secret.Identifier.Name.StartsWith(_prefix, System.StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public string GetKey(SecretBundle secret) {
            // Remove the prefix from the secret name and replace two
            // dashes in any name with the KeyDelimiter, which is the
            // delimiter used in configuration (usually a colon). Azure
            // Key Vault doesn't allow a colon in secret names.
            return secret.SecretIdentifier.Name
                .Substring(_prefix.Length)
                .Replace("--", ConfigurationPath.KeyDelimiter);
        }
    }
}
