// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class ApiConfig : ClientConfig, ITwinConfig, IRegistryConfig, IVaultConfig {

        /// <summary>
        /// Twin configuration
        /// </summary>
        private const string kOpcUaTwinServiceUrlKey = "OpcTwinServiceUrl";
        private const string kOpcUaTwinServiceIdKey = "OpcTwinServiceResourceId";

        /// <summary>OPC twin endpoint url</summary>
        public string OpcUaTwinServiceUrl => GetStringOrDefault(
            kOpcUaTwinServiceUrlKey, GetStringOrDefault(
                "PCS_TWIN_SERVICE_URL", $"http://{_hostName}:9041"));
        /// <summary>OPC twin service audience</summary>
        public string OpcUaTwinServiceResourceId => GetStringOrDefault(
            kOpcUaTwinServiceIdKey, GetStringOrDefault(
                "OPC_TWIN_APP_ID", Audience));

        /// <summary>
        /// Vault configuration
        /// </summary>
        private const string kOpcUaVaultServiceUrlKey = "OpcVaultServiceUrl";
        private const string kOpcUaVaultServiceIdKey = "OpcVaultServiceResourceId";

        /// <summary>OPC vault endpoint url</summary>
        public string OpcUaVaultServiceUrl => GetStringOrDefault(
            kOpcUaVaultServiceUrlKey, GetStringOrDefault(
                "PCS_VAULT_SERVICE_URL", $"http://{_hostName}:9044"));
        /// <summary>OPC vault audience</summary>
        public string OpcUaVaultServiceResourceId => GetStringOrDefault(
            kOpcUaVaultServiceIdKey, GetStringOrDefault(
                "OPC_VAULT_APP_ID", Audience));

        /// <summary>
        /// Registry configuration
        /// </summary>
        private const string kOpcUaRegistryServiceUrlKey = "OpcRegistryServiceUrl";
        private const string kOpcUaRegistryServiceIdKey = "OpcRegistryServiceResourceId";

        /// <summary>OPC registry endpoint url</summary>
        public string OpcUaRegistryServiceUrl => GetStringOrDefault(
            kOpcUaRegistryServiceUrlKey, GetStringOrDefault(
                "PCS_TWIN_REGISTRY_URL", $"http://{_hostName}:9042"));
        /// <summary>OPC registry audience</summary>
        public string OpcUaRegistryServiceResourceId => GetStringOrDefault(
            kOpcUaRegistryServiceIdKey, GetStringOrDefault(
                "OPC_REGISTRY_APP_ID", Audience));

        /// <inheritdoc/>
        public ApiConfig(IConfigurationRoot configuration) :
            base(configuration) {
            _hostName = GetStringOrDefault("_HOST", System.Net.Dns.GetHostName());
        }

        private readonly string _hostName;
    }
}
