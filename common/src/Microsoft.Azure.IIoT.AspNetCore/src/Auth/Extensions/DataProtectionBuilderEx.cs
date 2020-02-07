// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Crypto.KeyVault;
    using Microsoft.Azure.IIoT.Crypto.KeyVault.Runtime;
    using Microsoft.Azure.IIoT.Storage.Blob;
    using Microsoft.Azure.IIoT.Storage.Blob.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Azure.KeyVault.WebKey;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.DataProtection;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Add data protection using azure blob storage and keyvault
    /// </summary>
    public static class DataProtectionBuilderEx {

        /// <summary>
        /// Add azure data protection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddAzureDataProtection(
            this IServiceCollection services, IConfiguration configuration = null) {
            if (configuration == null) {
                configuration = services.BuildServiceProvider()
                    .GetRequiredService<IConfiguration>();
            }
            services.AddDataProtection()
                .AddAzureBlobKeyStorage(configuration)
                .AddAzureKeyVaultDataProtection(configuration);
        }

        /// <summary>
        /// Add Keyvault protection
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static IDataProtectionBuilder AddAzureKeyVaultDataProtection(
            this IDataProtectionBuilder builder, IConfiguration configuration = null) {
            if (configuration == null) {
                configuration = builder.Services.BuildServiceProvider()
                    .GetRequiredService<IConfiguration>();
            }

            var config = new DataProtectionConfig(configuration);
            if (string.IsNullOrEmpty(config.KeyVaultBaseUrl)) {
                return builder;
            }
            var keyName = config.KeyVaultKeyDataProtection;
            var client = TryKeyVaultClientAsync(config.KeyVaultBaseUrl,
                config, keyName).Result;
            if (client == null) {
                throw new UnauthorizedAccessException("Cannot access keyvault");
            }
            var identifier = $"{config.KeyVaultBaseUrl.TrimEnd('/')}/keys/{keyName}";
            return builder.ProtectKeysWithAzureKeyVault(client, identifier);
        }

        /// <summary>
        /// Add blob key storage
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static IDataProtectionBuilder AddAzureBlobKeyStorage(
            this IDataProtectionBuilder builder, IConfiguration configuration = null) {
            if (configuration == null) {
                configuration = builder.Services.BuildServiceProvider()
                    .GetRequiredService<IConfiguration>();
            }

            var storage = new DataProtectionConfig(configuration);
            var containerName = storage.BlobStorageContainerDataProtection;
            if (string.IsNullOrEmpty(storage.BlobStorageConnString)) {
                return builder;
            }

            var storageAccount = CloudStorageAccount.Parse(storage.BlobStorageConnString);
            var relativePath = $"{containerName}/keys.xml";
            var uriBuilder = new UriBuilder(storageAccount.BlobEndpoint);
            uriBuilder.Path = uriBuilder.Path.TrimEnd('/') + "/" + relativePath.TrimStart('/');
            var block = new CloudBlockBlob(uriBuilder.Uri, storageAccount.Credentials);
            Try.Op(() => block.Container.Create());
            return builder.PersistKeysToAzureBlobStorage(block);
        }

        /// <summary>
        /// Try create new keyvault client using provided configuration. Will
        /// try several combinations including managed service identity and
        /// if allowed, visual studio tooling access.
        /// </summary>
        /// <param name="vaultUri"></param>
        /// <param name="client"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        private static async Task<KeyVaultClient> TryKeyVaultClientAsync(string vaultUri,
            IClientConfig client, string keyName) {
            KeyVaultClient keyVault;

            // Try reading with app and secret if available.
            if (!string.IsNullOrEmpty(client.AppId) &&
                !string.IsNullOrEmpty(client.AppSecret) &&
                !string.IsNullOrEmpty(client.TenantId)) {
                var connectionString =
                    $"RunAs=App; " +
                    $"AppId={client.AppId}; " +
                    $"AppKey={client.AppSecret}; " +
                    $"TenantId={client.TenantId}";
                keyVault = await TryInititalizeKeyAsync("Application",
                    vaultUri, keyName, connectionString);
                if (keyVault != null) {
                    return keyVault;
                }
            }

            // Try using aims
            keyVault = await TryInititalizeKeyAsync("Managed Service Identity",
                vaultUri, keyName);
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
        /// <param name="keyName"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static async Task<KeyVaultClient> TryInititalizeKeyAsync(
            string method, string vaultUri, string keyName, string connectionString = null) {
            var tokenProvider = new AzureServiceTokenProvider(connectionString);
            try {
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(
                        tokenProvider.KeyVaultTokenCallback));

                try {
                    var key = await keyVaultClient.GetKeyAsync(vaultUri, keyName);
                }
                catch {
                    // Try create key
                    await keyVaultClient.CreateKeyAsync(vaultUri, keyName, new NewKeyParameters {
                        KeySize = 2048,
                        Kty = JsonWebKeyType.Rsa,
                        KeyOps = new List<string> {
                            JsonWebKeyOperation.Wrap, JsonWebKeyOperation.Unwrap
                        }
                    });
                }
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
        /// Data protection default configuration
        /// </summary>
        internal sealed class DataProtectionConfig : ConfigBase, IClientConfig,
            IKeyVaultConfig, IStorageConfig {

            private const string kTenantIdDefault = "common";
            private const string kKeyVaultKeyDataProtectionDefault = "dataprotection";
            private const string kBlobStorageContainerDataProtectionDefault = "dataprotection";

            /// <summary>Application id</summary>
            public string AppId => GetStringOrDefault(PcsVariable.PCS_KEYVAULT_APPID,
                Environment.GetEnvironmentVariable(PcsVariable.PCS_KEYVAULT_APPID))?.Trim();
            /// <summary>App secret</summary>
            public string AppSecret => GetStringOrDefault(PcsVariable.PCS_KEYVAULT_SECRET,
                Environment.GetEnvironmentVariable(PcsVariable.PCS_KEYVAULT_SECRET))?.Trim();
            /// <summary>Optional tenant</summary>
            public string TenantId => GetStringOrDefault(PcsVariable.PCS_AUTH_TENANT,
                Environment.GetEnvironmentVariable(PcsVariable.PCS_AUTH_TENANT) ??
                    kTenantIdDefault).Trim();

            /// <summary>Aad instance url</summary>
            public string InstanceUrl => null;
            /// <summary>Aad domain</summary>
            public string Domain => null;

            /// <inheritdoc/>
            public string BlobStorageConnString => _stg.BlobStorageConnString;
            /// <inheritdoc/>
            public string KeyVaultBaseUrl => _kv.KeyVaultBaseUrl;
            /// <inheritdoc/>
            public string KeyVaultResourceId => _kv.KeyVaultResourceId;
            /// <inheritdoc/>
            public bool KeyVaultIsHsm => _kv.KeyVaultIsHsm;

            /// <summary>Key (in KeyVault) to be used for encription of keys</summary>
            public string KeyVaultKeyDataProtection =>
                GetStringOrDefault(PcsVariable.PCS_KEYVAULT_KEY_DATAPROTECTION,
                    Environment.GetEnvironmentVariable(PcsVariable.PCS_KEYVAULT_KEY_DATAPROTECTION) ??
                    kKeyVaultKeyDataProtectionDefault).Trim();

            /// <summary>Blob Storage Container that holds encrypted keys</summary>
            public string BlobStorageContainerDataProtection =>
                GetStringOrDefault(PcsVariable.PCS_STORAGE_CONTAINER_DATAPROTECTION,
                    Environment.GetEnvironmentVariable(PcsVariable.PCS_STORAGE_CONTAINER_DATAPROTECTION) ??
                    kBlobStorageContainerDataProtectionDefault).Trim();

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
