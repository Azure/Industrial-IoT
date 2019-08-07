// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Runtime {
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Azure.IIoT.Services.Cors.Runtime;
    using Microsoft.Azure.IIoT.Services.Swagger;
    using Microsoft.Azure.IIoT.Services.Swagger.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Crypto.KeyVault;
    using Microsoft.Azure.IIoT.Crypto.KeyVault.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Vault;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Runtime;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Web service configuration
    /// </summary>
    public class Config : ConfigBase, IAuthConfig, IIoTHubConfig, ICorsConfig,
        IClientConfig, ISwaggerConfig, IVaultConfig, ICosmosDbConfig,
        IItemContainerConfig, IKeyVaultConfig, IServiceBusConfig, IRegistryConfig, IApplicationInsightsConfig {

        /// <summary>
        /// Whether to use role based access
        /// </summary>
        public bool UseRoles => GetBoolOrDefault("PCS_AUTH_ROLES", true);

        /// <inheritdoc/>
        public string CorsWhitelist => _cors.CorsWhitelist;
        /// <inheritdoc/>
        public bool CorsEnabled => _cors.CorsEnabled;
        /// <inheritdoc/>
        public string AppId => _auth.AppId;
        /// <inheritdoc/>
        public string AppSecret => _auth.AppSecret;
        /// <inheritdoc/>
        public string TenantId => _auth.TenantId;
        /// <inheritdoc/>
        public string InstanceUrl => _auth.InstanceUrl;
        /// <inheritdoc/>
        public string Audience => _auth.Audience;
        /// <inheritdoc/>
        public bool UIEnabled => _swagger.UIEnabled;
        /// <inheritdoc/>
        public bool WithAuth => !string.IsNullOrEmpty(_auth.AppId) && _swagger.WithAuth;
        /// <inheritdoc/>
        public string SwaggerAppId => _swagger.AppId;
        /// <inheritdoc/>
        public string SwaggerAppSecret => _swagger.AppSecret;
        /// <inheritdoc/>
        public bool WithHttpScheme => _swagger.WithHttpScheme;
        /// <inheritdoc/>
        public bool AuthRequired => _auth.AuthRequired;
        /// <inheritdoc/>
        public string TrustedIssuer => _auth.TrustedIssuer;
        /// <inheritdoc/>
        public int HttpsRedirectPort => _auth.HttpsRedirectPort;
        /// <inheritdoc/>
        public TimeSpan AllowedClockSkew => _auth.AllowedClockSkew;
        /// <inheritdoc/>
        public bool AutoApprove => _vault.AutoApprove;
        /// <inheritdoc/>
        public string KeyVaultBaseUrl => _keyVault.KeyVaultBaseUrl;
        /// <inheritdoc/>
        public string KeyVaultResourceId => _keyVault.KeyVaultResourceId;
        /// <inheritdoc/>
        public bool KeyVaultIsHsm => _keyVault.KeyVaultIsHsm;
        /// <inheritdoc/>
        public string DbConnectionString => _cosmos.DbConnectionString;
        /// <inheritdoc/>
        public string ContainerName => "iiot_opc";
        /// <inheritdoc/>
        public string DatabaseName => "iiot_opc";
        /// <inheritdoc/>
        public string ServiceBusConnString => _sb.ServiceBusConnString;
        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;
        /// <inheritdoc/>
        public string OpcUaRegistryServiceUrl => _registry.OpcUaRegistryServiceUrl;
        /// <inheritdoc/>
        public string OpcUaRegistryServiceResourceId => _registry.OpcUaRegistryServiceResourceId;
        /// <inheritdoc/>
        public TelemetryConfiguration TelemetryConfiguration => _ai.TelemetryConfiguration;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        internal Config(IConfigurationRoot configuration) :
            base(configuration) {
            _vault = new VaultConfig(configuration);
            _keyVault = new KeyVaultConfig(configuration);
            _cosmos = new CosmosDbConfig(configuration);
            _swagger = new SwaggerConfig(configuration);
            _auth = new AuthConfig(configuration);
            _cors = new CorsConfig(configuration);
            _sb = new ServiceBusConfig(configuration);
            _hub = new IoTHubConfig(configuration);
            _registry = new ApiConfig(configuration);
            _ai = new ApplicationInsightsConfig(configuration);
        }

        private readonly IVaultConfig _vault;
        private readonly KeyVaultConfig _keyVault;
        private readonly ICosmosDbConfig _cosmos;
        private readonly SwaggerConfig _swagger;
        private readonly AuthConfig _auth;
        private readonly CorsConfig _cors;
        private readonly ServiceBusConfig _sb;
        private readonly IoTHubConfig _hub;
        private readonly IRegistryConfig _registry;
        private readonly ApplicationInsightsConfig _ai;
    }
}

