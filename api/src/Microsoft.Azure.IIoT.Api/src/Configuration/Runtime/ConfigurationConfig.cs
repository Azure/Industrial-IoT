// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Configuration.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class ConfigurationConfig : ApiConfigBase, IConfigurationConfig {

        /// <summary>
        /// Configuration configuration
        /// </summary>
        private const string kConfigurationServiceUrlKey = "ConfigurationServiceUrl";
        private const string kConfigurationServiceIdKey = "ConfigurationServiceResourceId";

        /// <summary>Configuration configuration endpoint</summary>
        public string ConfigurationServiceUrl => GetStringOrDefault(
            kConfigurationServiceUrlKey, GetStringOrDefault(
                "PCS_CONFIGURATION_SERVICE_URL", GetDefaultUrl("9050", "configuration")));
        /// <summary>Configuration service audience</summary>
        public string ConfigurationServiceResourceId => GetStringOrDefault(
            kConfigurationServiceIdKey, GetStringOrDefault("CONFIGURATION_APP_ID",
                GetStringOrDefault("PCS_AUTH_AUDIENCE", null)));

        /// <inheritdoc/>
        public ConfigurationConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
