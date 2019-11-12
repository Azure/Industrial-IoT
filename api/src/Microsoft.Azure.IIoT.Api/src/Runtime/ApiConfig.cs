// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Runtime {
    using Microsoft.Azure.IIoT.Api.Jobs;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.History;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class ApiConfig : ClientConfig, ITwinConfig, IRegistryConfig, IJobsServiceConfig,
        IVaultConfig, IHistoryConfig, IPublisherConfig, ISignalRClientConfig {

        /// <summary>
        /// Twin configuration
        /// </summary>
        private const string kOpcUaTwinServiceUrlKey = "OpcTwinServiceUrl";
        private const string kOpcUaTwinServiceIdKey = "OpcTwinServiceResourceId";

        /// <summary>OPC twin service endpoint url</summary>
        public string OpcUaTwinServiceUrl => GetStringOrDefault(
            kOpcUaTwinServiceUrlKey, GetStringOrDefault(
                "PCS_TWIN_SERVICE_URL", GetDefaultUrl("9041", "twin")));
        /// <summary>OPC twin service audience</summary>
        public string OpcUaTwinServiceResourceId => GetStringOrDefault(
            kOpcUaTwinServiceIdKey, GetStringOrDefault(
                "OPC_TWIN_APP_ID", Audience));

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
            kOpcUaRegistryServiceIdKey, GetStringOrDefault(
                "OPC_REGISTRY_APP_ID", Audience));

        /// <summary>
        /// History configuration
        /// </summary>
        private const string kOpcUaHistoryServiceUrlKey = "OpcHistoryServiceUrl";
        private const string kOpcUaHistoryServiceIdKey = "OpcHistoryServiceResourceId";

        /// <summary>OPC history service endpoint url</summary>
        public string OpcUaHistoryServiceUrl => GetStringOrDefault(
            kOpcUaHistoryServiceUrlKey, GetStringOrDefault(
                "PCS_HISTORY_SERVICE_URL", GetDefaultUrl("9043", "vault")));
        /// <summary>OPC vault audience</summary>
        public string OpcUaHistoryServiceResourceId => GetStringOrDefault(
            kOpcUaHistoryServiceIdKey, GetStringOrDefault(
                "OPC_HISTORY_APP_ID", Audience));

        /// <summary>
        /// Vault configuration
        /// </summary>
        private const string kOpcUaVaultServiceUrlKey = "OpcVaultServiceUrl";
        private const string kOpcUaVaultServiceIdKey = "OpcVaultServiceResourceId";

        /// <summary>OPC vault service endpoint url</summary>
        public string OpcUaVaultServiceUrl => GetStringOrDefault(
            kOpcUaVaultServiceUrlKey, GetStringOrDefault(
                "PCS_VAULT_SERVICE_URL", GetDefaultUrl("9044", "vault")));
        /// <summary>OPC vault audience</summary>
        public string OpcUaVaultServiceResourceId => GetStringOrDefault(
            kOpcUaVaultServiceIdKey, GetStringOrDefault(
                "OPC_VAULT_APP_ID", Audience));

        /// <summary>
        /// Publisher configuration
        /// </summary>
        private const string kOpcUaPublisherServiceUrlKey = "OpcPublisherServiceUrl";
        private const string kOpcUaPublisherServiceIdKey = "OpcPublisherServiceResourceId";

        /// <summary>OPC publisher service endpoint url</summary>
        public string OpcUaPublisherServiceUrl => GetStringOrDefault(
            kOpcUaPublisherServiceUrlKey, GetStringOrDefault(
                "PCS_PUBLISHER_SERVICE_URL", GetDefaultUrl("9045", "publisher")));
        /// <summary>OPC twin service audience</summary>
        public string OpcUaPublisherServiceResourceId => GetStringOrDefault(
            kOpcUaPublisherServiceIdKey, GetStringOrDefault(
                "OPC_PUBLISHER_APP_ID", Audience));

        /// <summary>
        /// Jobs configuration
        /// </summary>
        private const string kJobServiceUrlKey = "JobServiceUrl";
        private const string kJobServiceIdKey = "JobServiceResourceId";

        /// <summary>Jobs service endpoint url</summary>
        public string JobServiceUrl => GetStringOrDefault(
            kJobServiceUrlKey, GetStringOrDefault(
                "PCS_JOBS_SERVICE_URL", GetDefaultUrl("9046", "jobs")));
        /// <summary>Jobs service audience</summary>
        public string JobServiceResourceId => GetStringOrDefault(
            kJobServiceIdKey, GetStringOrDefault(
                "JOBS_APP_ID", Audience));

        /// <summary>
        /// SignalR configuration
        /// </summary>
        private const string kManagementServiceUrlKey = "ManagementEndpointUrl";
        private const string kUserIdKey = "UserId";

        /// <summary>Management configuration endpoint</summary>
        public string SignalREndpointUrl => GetStringOrDefault(
            kManagementServiceUrlKey, GetStringOrDefault(
                "PCS_CONFIGURATION_SERVICE_URL", GetDefaultUrl("9050", "configuration")));
        /// <summary>Management client id</summary>
        public string SignalRUserId => GetStringOrDefault(
            kUserIdKey, GetStringOrDefault(
                "PCS_USER_ID", Guid.NewGuid().ToString()));
        /// <summary>SignalR Hubname</summary>
        public string SignalRHubName => null;

        /// <inheritdoc/>
        public ApiConfig(IConfiguration configuration) :
            base(configuration) {
            _hostName = GetStringOrDefault("_HOST", System.Net.Dns.GetHostName());
        }

        /// <summary>
        /// Make endpoint url from configruation
        /// </summary>
        /// <param name="port"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetDefaultUrl(string port, string path) {
            var cloudEndpoint = GetStringOrDefault("PCS_SERVICE_URL");
            if (string.IsNullOrEmpty(cloudEndpoint)) {
                return $"http://{_hostName}:{port}";
            }
            return $"{cloudEndpoint}/{path}";
        }

        private readonly string _hostName;
    }
}
