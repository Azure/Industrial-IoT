// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Configuration {
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ConfigurationEx {

        /// <summary>
        /// Add environment from keyvault through managed service identity.
        /// It is assumed that the configuration builder contains the configuration
        /// of the keyvault, hence it must be in a source ahead of this one.
        /// </summary>
        /// <param name="configurationBuilder"></param>
        /// <param name="configurationSecretName"></param>
        /// <param name="keyVaultUrlVarName"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddEnvironmentFromKeyVault(
            this IConfigurationBuilder configurationBuilder, string configurationSecretName = null,
            string keyVaultUrlVarName = null) {

            if (string.IsNullOrEmpty(keyVaultUrlVarName)) {
                keyVaultUrlVarName = PcsVariable.PCS_KEYVAULT_URL;
            }
            if (string.IsNullOrEmpty(configurationSecretName)) {
                configurationSecretName = "configuration";
            }

            var configuration = configurationBuilder.Build();
            var url = configuration.GetValue<string>(keyVaultUrlVarName);
            if (string.IsNullOrEmpty(url)) {
                return configurationBuilder;
            }
            try {
                var tokenProvider = new AzureServiceTokenProvider();
                var keyVault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                    tokenProvider.KeyVaultTokenCallback));

                // TODO: Retry?
                var secretResult = keyVault.GetSecretAsync(url, configurationSecretName).Result;

                if (secretResult.ContentType.EqualsIgnoreCase(ContentMimeType.Json)) {
                    var env = JsonConvertEx.DeserializeObject<Dictionary<string, string>>(
                        secretResult.Value);

                    // Only add missing values, so we do not override values from previous sources.
                    var missing = env
                        .Where(kv => configuration.GetValue(typeof(object), kv.Key) == null);
                    configurationBuilder.AddInMemoryCollection(missing);
                }
            }
            catch (Exception ex) {
                Log.Error(ex, "Failed to read configuration from keyvault.");
            }
            return configurationBuilder;
        }
    }
}
