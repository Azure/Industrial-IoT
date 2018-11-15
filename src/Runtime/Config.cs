// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using Microsoft.Azure.IIoT.Auth.Clients;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.Auth.Server;
using Microsoft.Azure.IIoT.Auth.Runtime;
using Microsoft.Azure.IIoT.Services.Cors;
using Microsoft.Azure.IIoT.Services.Cors.Runtime;
using Microsoft.Azure.IIoT.Services.Swagger;
using Microsoft.Azure.IIoT.Services.Swagger.Runtime;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime
{
    /// <summary>Web service configuration</summary>
    public class Config : LogConfig, IAuthConfig,
        ICorsConfig, IClientConfig, ISwaggerConfig
    {
        // services config
        private const string OpcVaultKey = "OpcVault";
        private const string SwaggerKey = "Swagger";

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) :
            this(Uptime.ProcessId, ServiceInfo.ID, configuration)
        {
        }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="serviceId"></param>
        /// <param name="configuration"></param>
        internal Config(string processId, string serviceId,
            IConfigurationRoot configuration) :
            base(processId, configuration) {
            ServicesConfig = new ServicesConfig();
            configuration.Bind(OpcVaultKey, ServicesConfig);
            SwaggerConfig = new SwaggerConfig();
            configuration.Bind(SwaggerKey, SwaggerConfig);
            _auth = new AuthConfig(configuration, serviceId);
            _cors = new CorsConfig(configuration);
        }

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
        public bool UIEnabled => SwaggerConfig.Enabled;
        /// <inheritdoc/>
        public bool WithAuth => !String.IsNullOrEmpty(_auth.AppId);
        /// <inheritdoc/>
        public string SwaggerAppId => SwaggerConfig.AppId;
        /// <inheritdoc/>
        public string SwaggerAppSecret => SwaggerConfig.AppSecret;
        /// <inheritdoc/>
        public bool AuthRequired => _auth.AuthRequired;
        /// <inheritdoc/>
        public string TrustedIssuer => _auth.TrustedIssuer;
        /// <inheritdoc/>
        public TimeSpan AllowedClockSkew => _auth.AllowedClockSkew;

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig { get; }
        public SwaggerConfig SwaggerConfig { get; }

        private readonly AuthConfig _auth;
        private readonly CorsConfig _cors;
    }
}

