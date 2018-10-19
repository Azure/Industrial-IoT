// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using Microsoft.Azure.IIoT.Auth.Azure;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.Services.Auth;
using Microsoft.Azure.IIoT.Services.Auth.Runtime;
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
            _swagger = new SwaggerConfig(configuration, serviceId);
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
        public string Authority => _auth.Authority;
        /// <inheritdoc/>
        public string Audience => _auth.Audience;
        /// <inheritdoc/>
        public bool UIEnabled => _swagger.UIEnabled;
        /// <inheritdoc/>
        public bool WithAuth => _swagger.WithAuth;
        /// <inheritdoc/>
        public string SwaggerAppId => _swagger.SwaggerAppId;
        /// <inheritdoc/>
        public string SwaggerAppSecret => _swagger.SwaggerAppSecret;
        /// <inheritdoc/>
        public bool AuthRequired => _auth.AuthRequired;
        /// <inheritdoc/>
        public string TrustedIssuer => _auth.TrustedIssuer;
        /// <inheritdoc/>
        public TimeSpan AllowedClockSkew => _auth.AllowedClockSkew;

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig { get; }

        private readonly SwaggerConfig _swagger;
        private readonly AuthConfig _auth;
        private readonly CorsConfig _cors;
    }
}

