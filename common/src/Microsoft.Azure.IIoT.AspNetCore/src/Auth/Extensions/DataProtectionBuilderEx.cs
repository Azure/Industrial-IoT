// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth {
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Crypto.KeyVault;
    using Microsoft.Azure.IIoT.Storage.Blob;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Azure.Storage;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Add data protection
    /// </summary>
    public static class DataProtectionBuilderEx {

        /// <summary>
        /// Helper to add jwt bearer authentication
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <param name="keyVault"></param>
        /// <param name="keyId"></param>
        public static IDataProtectionBuilder AddAzureKeyVaultDataProtection(
            this IDataProtectionBuilder builder, IClientConfig config,
            IKeyVaultConfig keyVault, string keyId = "keys") {
            if (keyVault?.KeyVaultBaseUrl == null) {
                throw new ArgumentNullException(nameof(keyVault));
            }
            var client = TryKeyVaultClientAsync(keyVault.KeyVaultBaseUrl, config).Result;
            if (client == null) {
                throw new UnauthorizedAccessException("Cannot access keyvault");
            }
            return builder.ProtectKeysWithAzureKeyVault(client, keyId);
        }

        /// <summary>
        /// Add blob key storage
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="storage"></param>
        public static IDataProtectionBuilder AddAzureBlobKeyStorage(
            this IDataProtectionBuilder builder, IStorageConfig storage) {

            if (storage?.BlobStorageConnString == null) {
                throw new ArgumentNullException(nameof(storage));
            }
            return builder.PersistKeysToAzureBlobStorage(
                CloudStorageAccount.Parse(storage.BlobStorageConnString), "keys");
        }

        /// <summary>
        /// Try create new keyvault client using provided configuration. Will
        /// try several combinations including managed service identity and
        /// if allowed, visual studio tooling access.
        /// </summary>
        /// <param name="vaultUri"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        private static async Task<KeyVaultClient> TryKeyVaultClientAsync(string vaultUri,
            IClientConfig client) {
            KeyVaultClient keyVault;

            // Try reading with app and secret if available.
            if (!string.IsNullOrEmpty(client.AppId) &&
                !string.IsNullOrEmpty(client.AppSecret) &&
                !string.IsNullOrEmpty(client.TenantId)) {
                keyVault = await TryCredentialsToReadSecretAsync("Application",
                    vaultUri, $"RunAs=App; AppId={client.AppId}; " +
                        $"AppKey={client.AppSecret}; TenantId={client.TenantId}");
                if (keyVault != null) {
                    return keyVault;
                }
            }

            // Try using aims
            keyVault = TryCredentialsToReadSecretAsync("Managed Service Identity",
                vaultUri).Result;
            if (keyVault != null) {
                return keyVault;
            }
            return null;
        }

        /// <summary>
        /// Read configuration secret
        /// </summary>
        /// <param name="method"></param>
        /// <param name="vaultUri"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static async Task<KeyVaultClient> TryCredentialsToReadSecretAsync(
            string method, string vaultUri, string connectionString = null) {
            var tokenProvider = new AzureServiceTokenProvider(connectionString);
            try {
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(
                        tokenProvider.KeyVaultTokenCallback));

                var secrets = await keyVaultClient.GetSecretsAsync(vaultUri, 1);

                // Worked - we have a working keyvault client.
                return keyVaultClient;
            }
            catch (Exception ex) {
                Log.Logger.Debug("Failed to authenticate to keyvault {url} using " +
                    "{method}: {message}",
                    vaultUri, method, ex.Message);
                return null;
            }
        }
    }
}
