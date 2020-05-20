// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using Microsoft.Azure.IIoT.Crypto.KeyVault;
    using Microsoft.Azure.IIoT.Crypto.KeyVault.Runtime;
    using Microsoft.Azure.IIoT.Storage.Datalake;
    using Microsoft.Azure.IIoT.Storage.Datalake.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Auth.KeyVault;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Azure.KeyVault.WebKey;
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
            this IDataProtectionBuilder builder, IConfiguration configuration) {
            var config = new DataProtectionConfig(configuration);
            if (string.IsNullOrEmpty(config.KeyVaultBaseUrl)) {
                throw new InvalidConfigurationException(
                    "Keyvault base url is missing in your configuration " +
                    "for dataprotection to be able to store the root key.");
            }
            var keyName = config.KeyVaultKeyDataProtection;
            var keyVault = new KeyVaultClientBootstrap(configuration);
            if (!TryInititalizeKeyAsync(keyVault.Client, config.KeyVaultBaseUrl, keyName).Result) {
                throw new UnauthorizedAccessException("Cannot access keyvault");
            }
            var identifier = $"{config.KeyVaultBaseUrl.TrimEnd('/')}/keys/{keyName}";
            return builder.ProtectKeysWithAzureKeyVault(keyVault.Client, identifier);
        }

        /// <summary>
        /// Add blob key storage
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static IDataProtectionBuilder AddAzureBlobKeyStorage(
            this IDataProtectionBuilder builder, IConfiguration configuration) {

            var storage = new DataProtectionConfig(configuration);
            var containerName = storage.BlobStorageContainerDataProtection;
            var connectionString = storage.GetStorageConnString();
            if (string.IsNullOrEmpty(connectionString)) {
               throw new InvalidConfigurationException(
                   "Storage configuration is missing in your configuration for " +
                   "dataprotection to store all keys across all instances.");
            }
            var storageAccount = CloudStorageAccount.Parse(storage.GetStorageConnString());
            var relativePath = $"{containerName}/keys.xml";
            var uriBuilder = new UriBuilder(storageAccount.BlobEndpoint);
            uriBuilder.Path = uriBuilder.Path.TrimEnd('/') + "/" + relativePath.TrimStart('/');
            var block = new CloudBlockBlob(uriBuilder.Uri, storageAccount.Credentials);
            Try.Op(() => block.Container.Create());
            return builder.PersistKeysToAzureBlobStorage(block);
        }

        /// <summary>
        /// Read configuration secret
        /// </summary>
        /// <param name="keyVaultClient"></param>
        /// <param name="vaultUri"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        private static async Task<bool> TryInititalizeKeyAsync(
            KeyVaultClient keyVaultClient, string vaultUri, string keyName) {
            try {
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
                return true;
            }
            catch (Exception ex) {
                Log.Logger.Debug(ex, "Failed to authenticate to keyvault {url}.",
                    vaultUri);
                return false;
            }
        }

        /// <summary>
        /// Data protection default configuration
        /// </summary>
        internal sealed class DataProtectionConfig : ConfigBase, IKeyVaultConfig, IBlobConfig  {

            private const string kKeyVaultKeyDataProtectionDefault = "dataprotection";
            private const string kBlobStorageContainerDataProtectionDefault = "dataprotection";

            /// <inheritdoc/>
            public string KeyVaultBaseUrl => _kv.KeyVaultBaseUrl;
            /// <inheritdoc/>
            public bool KeyVaultIsHsm => _kv.KeyVaultIsHsm;

            /// <summary>Key (in KeyVault) to be used for encription of keys</summary>
            public string KeyVaultKeyDataProtection =>
                GetStringOrDefault(PcsVariable.PCS_KEYVAULT_KEY_DATAPROTECTION,
                    () => Environment.GetEnvironmentVariable(
                        PcsVariable.PCS_KEYVAULT_KEY_DATAPROTECTION) ??
                        kKeyVaultKeyDataProtectionDefault).Trim();

            /// <summary>Blob Storage Container that holds encrypted keys</summary>
            public string BlobStorageContainerDataProtection =>
                GetStringOrDefault(PcsVariable.PCS_STORAGE_CONTAINER_DATAPROTECTION,
                    () => Environment.GetEnvironmentVariable(
                        PcsVariable.PCS_STORAGE_CONTAINER_DATAPROTECTION) ??
                        kBlobStorageContainerDataProtectionDefault).Trim();

            /// <inheritdoc/>
            public string EndpointSuffix => _stg.EndpointSuffix;
            /// <inheritdoc/>
            public string AccountName => _stg.AccountName;
            /// <inheritdoc/>
            public string AccountKey => _stg.AccountKey;

            /// <summary>
            /// Configuration constructor
            /// </summary>
            /// <param name="configuration"></param>
            public DataProtectionConfig(IConfiguration configuration) :
                base(configuration) {
                _stg = new BlobConfig(configuration);
                _kv = new KeyVaultConfig(configuration);
            }

            private readonly BlobConfig _stg;
            private readonly KeyVaultConfig _kv;
        }
    }
}
