// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class RegistryConfig : ApiConfigBase, IRegistryConfig {

        /// <summary>
        /// Registry configuration
        /// </summary>
        private const string kOpcUaRegistryServiceUrlKey = "OpcRegistryServiceUrl";
        private const string kOpcUaRegistryServiceIdKey = "OpcRegistryServiceResourceId";

        /// <summary>OPC registry endpoint url</summary>
        public string OpcUaRegistryServiceUrl => GetStringOrDefault(
            kOpcUaRegistryServiceUrlKey, GetStringOrDefault(
                "PCS_TWIN_REGISTRY_URL", GetDefaultUrl("9042", "registry")));
        /// <summary>OPC registry audience</summary>
        public string OpcUaRegistryServiceResourceId => GetStringOrDefault(
            kOpcUaRegistryServiceIdKey, GetStringOrDefault("OPC_REGISTRY_APP_ID",
                GetStringOrDefault("PCS_AUTH_AUDIENCE", null)));

        /// <inheritdoc/>
        public RegistryConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
