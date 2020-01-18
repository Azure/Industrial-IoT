// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth {
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Crypto.KeyVault;
    using Microsoft.Azure.IIoT.Crypto.KeyVault.Runtime;
    using Microsoft.Azure.IIoT.Storage.Blob;
    using Microsoft.Azure.IIoT.Storage.Blob.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Azure.Storage;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Add data protection
    /// </summary>
    public static class DataProtectionBuilderEx {

        /// <summary>
        /// Add Keyvault protection
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static IDataProtectionBuilder AddAzureKeyVaultDataProtection(
            this IDataProtectionBuilder builder, IConfiguration configuration) {
            var config = new DataProtectionConfig(configuration);
            if (config.KeyVaultBaseUrl == null) {
                return builder;
            }
            var client = TryKeyVaultClientAsync(config.KeyVaultBaseUrl, config).Result;
            if (client == null) {
                throw new UnauthorizedAccessException("Cannot access keyvault");
            }
            return builder.ProtectKeysWithAzureKeyVault(client, "keys");
        }

        /// <summary>
        /// Add blob key storage
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static IDataProtectionBuilder AddAzureBlobKeyStorage(
            this IDataProtectionBuilder builder, IConfiguration configuration) {
            var storage = new DataProtectionConfig(configuration);
            if (storage?.BlobStorageConnString == null) {
                return builder;
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

        /// <summary>
        /// Dataprotection default configuration
        /// </summary>
        internal sealed class DataProtectionConfig : ConfigBase, IClientConfig,
            IKeyVaultConfig, IStorageConfig {

            /// <summary>Application id</summary>
            public string AppId => GetStringOrDefault("PCS_KEYVAULT_APPID",
                Environment.GetEnvironmentVariable("PCS_KEYVAULT_APPID"))?.Trim();
            /// <summary>App secret</summary>
            public string AppSecret => GetStringOrDefault("PCS_KEYVAULT_SECRET",
                Environment.GetEnvironmentVariable("PCS_KEYVAULT_SECRET"))?.Trim();
            /// <summary>Optional tenant</summary>
            public string TenantId => GetStringOrDefault("PCS_AUTH_TENANT",
                Environment.GetEnvironmentVariable("PCS_AUTH_TENANT") ?? "common").Trim();

            /// <summary>Aad instance url</summary>
            public string InstanceUrl => null;
            /// <summary>Aad domain</summary>
            public string Domain => null;

            /// <inheritdoc/>
            public string BlobStorageConnString { get; }
            /// <inheritdoc/>
            public string KeyVaultBaseUrl => _kv.KeyVaultBaseUrl;
            /// <inheritdoc/>
            public string KeyVaultResourceId => _kv.KeyVaultResourceId;
            /// <inheritdoc/>
            public bool KeyVaultIsHsm => _kv.KeyVaultIsHsm;

            /// <summary>
            /// Configuration constructor
            /// </summary>
            /// <param name="configuration"></param>
            public DataProtectionConfig(IConfiguration configuration) :
                base(configuration) {
                _stg = new StorageConfig(configuration);
                _kv = new KeyVaultConfig(configuration);
            }

            private readonly StorageConfig _stg;
            private readonly KeyVaultConfig _kv;
        }
    }
}
