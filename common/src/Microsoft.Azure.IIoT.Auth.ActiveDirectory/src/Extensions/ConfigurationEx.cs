// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Configuration {
    using Microsoft.Extensions.Primitives;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ConfigurationEx {

        /// <summary>
        /// Add configuration from azure keyvault.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="singleton"></param>
        /// <param name="allowDeveloperAccess"></param>
        /// <param name="keyVaultUrlVarName"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddFromKeyVault(
            this IConfigurationBuilder builder, bool singleton = true,
            bool allowDeveloperAccess = false, string keyVaultUrlVarName = null) {

            var provider = KeyVaultConfigurationProvider.CreateInstanceAsync(
                singleton, builder.Build(), keyVaultUrlVarName, allowDeveloperAccess)
                    .Result;
            if (provider != null) {
                builder.Add(provider);
            }
            else {
                Log.Logger.Information(
                    "If you want to read configuration from keyvault, make sure" +
                    "you are signed in to Visual Studio or Azure CLI on this " +
                    "machine and that you have been given access to this KeyVault." +
                    "Continuing to use existing environment settings.");
            }
            return builder;
        }

        /// <summary>
        /// Keyvault auth principal configuration
        /// </summary>
        internal sealed class KeyVaultClientConfig : ConfigBase, IClientConfig {

            /// <summary>Application id</summary>
            public string AppId => GetStringOrDefault("PCS_KEYVAULT_APPID")?.Trim();
            /// <summary>App secret</summary>
            public string AppSecret => GetStringOrDefault("PCS_KEYVAULT_SECRET")?.Trim();
            /// <summary>Optional tenant</summary>
            public string TenantId => GetStringOrDefault("PCS_AUTH_TENANT",
                "common").Trim();

            /// <summary>Aad instance url</summary>
            public string InstanceUrl => null;
            /// <summary>Aad domain</summary>
            public string Domain => null;

            /// <summary>
            /// Configuration constructor
            /// </summary>
            /// <param name="configuration"></param>
            public KeyVaultClientConfig(IConfiguration configuration) :
                base(configuration) {
            }
        }

        /// <summary>
        /// Lazy Keyvault configuration provider. This is more efficient than
        /// reading all secrets and filtering out.  Instead the provider will
        /// load on demand and cache the result for further lookup.  There is
        /// no overhead if a previous provider can provide the configuration.
        /// This is at the expense of providing nested configuration.
        /// </summary>
        internal sealed class KeyVaultConfigurationProvider : IConfigurationSource,
            IConfigurationProvider {

            /// <summary>
            /// Create keyvault provider
            /// </summary>
            /// <param name="keyVault"></param>
            /// <param name="keyVaultUri"></param>
            private KeyVaultConfigurationProvider(KeyVaultClient keyVault,
                string keyVaultUri) {
                _keyVault = keyVault;
                _keyVaultUri = keyVaultUri;
                _cache = new ConcurrentDictionary<string, Task<string>>();
                _reloadToken = new ConfigurationReloadToken();
            }

            /// <inheritdoc/>
            public IConfigurationProvider Build(IConfigurationBuilder builder) {
                return this;
            }

            /// <inheritdoc/>
            public bool TryGet(string key, out string value) {
                value = null;
                if (!key.StartsWith("PCS_", StringComparison.InvariantCultureIgnoreCase)) {
                    return false;
                }
                try {
                    value = _cache.GetOrAdd(key, async k => {
                        using (new PerfMarker(Log.Logger, key)) {
                            var bundle = await _keyVault.GetSecretAsync(_keyVaultUri,
                                GetSecretNameForKey(k));
                            return bundle.Value;
                        }
                    }).Result;
                    return true;
                }
                catch {
                    return false;
                }
            }

            /// <inheritdoc/>
            public void Set(string key, string value) {
                if (!key.StartsWith("PCS_", StringComparison.InvariantCultureIgnoreCase)) {
                    return;
                }
                _cache.AddOrUpdate(key, Task.FromResult(value),
                    (k, v) => Task.FromResult(value));
            }

            /// <inheritdoc/>
            public IChangeToken GetReloadToken() {
                return _reloadToken;
            }

            /// <inheritdoc/>
            public void Load() {
                // _cache.Clear();
            }

            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys,
                string parentPath) {
                // Not supported
                return Enumerable.Empty<string>();
            }

            /// <summary>
            /// Create configuration provider
            /// </summary>
            /// <param name="singleton"></param>
            /// <param name="configuration"></param>
            /// <param name="keyVaultUrlVarName"></param>
            /// <param name="allowDeveloperAccess"></param>
            /// <returns></returns>
            public static async Task<KeyVaultConfigurationProvider> CreateInstanceAsync(
                bool singleton, IConfigurationRoot configuration, string keyVaultUrlVarName,
                bool allowDeveloperAccess) {
                if (string.IsNullOrEmpty(keyVaultUrlVarName)) {
                    keyVaultUrlVarName = "PCS_KEYVAULT_URL";
                }
                if (singleton) {
                    // Save singleton creation
                    if (_singleton == null) {
                        lock (_lock) {
                            if (_singleton == null) {
                                // Create instance
                                _singleton = CreateInstanceAsync(configuration,
                                    keyVaultUrlVarName, allowDeveloperAccess);
                            }
                        }
                    }
                    return await _singleton;
                }
                // Create new instance
                return await CreateInstanceAsync(configuration, keyVaultUrlVarName,
                    allowDeveloperAccess);
            }

            /// <summary>
            /// Create new instance
            /// </summary>
            /// <param name="configuration"></param>
            /// <param name="keyVaultUrlVarName"></param>
            /// <param name="allowDeveloperAccess"></param>
            /// <returns></returns>
            private static async Task<KeyVaultConfigurationProvider> CreateInstanceAsync(
                IConfigurationRoot configuration, string keyVaultUrlVarName,
                bool allowDeveloperAccess) {
                var vaultUri = configuration.GetValue<string>(keyVaultUrlVarName, null);
                if (string.IsNullOrEmpty(vaultUri)) {
                    return null;
                }
                var keyVault = await TryKeyVaultClientAsync(vaultUri,
                    configuration, keyVaultUrlVarName, allowDeveloperAccess);
                if (keyVault == null) {
                    return null;
                }
                return new KeyVaultConfigurationProvider(keyVault, vaultUri);
            }

            /// <summary>
            /// Try create new keyvault client using provided configuration. Will
            /// try several combinations including managed service identity and
            /// if allowed, visual studio tooling access.
            /// </summary>
            /// <param name="vaultUri"></param>
            /// <param name="configuration"></param>
            /// <param name="variableName"></param>
            /// <param name="allowDeveloperAccess"></param>
            /// <returns></returns>
            private static async Task<KeyVaultClient> TryKeyVaultClientAsync(string vaultUri,
                IConfigurationRoot configuration, string variableName, bool allowDeveloperAccess) {
                KeyVaultClient keyVault;

                var client = new KeyVaultClientConfig(configuration);
                // Try reading with app and secret if available.
                if (!string.IsNullOrEmpty(client.AppId) &&
                    !string.IsNullOrEmpty(client.AppSecret) &&
                    !string.IsNullOrEmpty(client.TenantId)) {
                    keyVault = await TryCredentialsToReadSecretAsync(vaultUri,
                        variableName, $"RunAs=App; AppId={client.AppId}; " +
                            $"AppKey={client.AppSecret}; TenantId={client.TenantId}");
                    if (keyVault != null) {
                        return keyVault;
                    }
                }

                // Try using aims
                keyVault = TryCredentialsToReadSecretAsync(vaultUri, variableName).Result;
                if (keyVault != null) {
                    return keyVault;
                }
                if (allowDeveloperAccess) {
                    // Try logged on user if we cannot get anywhere
                    keyVault = TryCredentialsToReadSecretAsync(vaultUri,
                        variableName, "RunAs=Developer; DeveloperTool=VisualStudio").Result;
                    if (keyVault != null) {
                        return keyVault;
                    }
                }
                return null;
            }

            /// <summary>
            /// Read configuration secret
            /// </summary>
            /// <param name="vaultUri"></param>
            /// <param name="secretName"></param>
            /// <param name="connectionString"></param>
            /// <returns></returns>
            private static async Task<KeyVaultClient> TryCredentialsToReadSecretAsync(
                string vaultUri, string secretName, string connectionString = null) {
                var tokenProvider = new AzureServiceTokenProvider(connectionString);
                try {
                    var keyVaultClient = new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(
                            tokenProvider.KeyVaultTokenCallback));

                    var secret = await keyVaultClient.GetSecretAsync(vaultUri,
                        GetSecretNameForKey(secretName));

                    // Worked - we have a working keyvault client.
                    return keyVaultClient;
                }
                catch (Exception ex) {
                    Log.Logger.Information(ex, "Failed to authenticate to keyvault {url}. " +
                        "Cannot retrieve configuration secret '{name}'. ",
                        vaultUri, secretName);
                    return null;
                }
            }

            /// <summary>
            /// Get secret key for key value. Replace any upper case
            /// letters with lower case and _ with -.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            private static string GetSecretNameForKey(string key) {
                return key.Replace("_", "-").ToLowerInvariant();
            }

            private static readonly object _lock = new object();
            private static Task<KeyVaultConfigurationProvider> _singleton;
            private readonly KeyVaultClient _keyVault;
            private readonly string _keyVaultUri;
            private readonly ConcurrentDictionary<string, Task<string>> _cache;
            private readonly ConfigurationReloadToken _reloadToken;
        }
    }
}
