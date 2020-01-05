// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Configuration.Runtime {
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi.Runtime;
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Azure.IIoT.Services.Cors.Runtime;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Runtime;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus;
    using Microsoft.Azure.IIoT.Messaging.ServiceBus.Runtime;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IAuthConfig, IServiceBusConfig, ICorsConfig,
        IClientConfig, IOpenApiConfig, ISignalRServiceConfig {

        /// <inheritdoc/>
        public string CorsWhitelist => _cors.CorsWhitelist;
        /// <inheritdoc/>
        public bool CorsEnabled => _cors.CorsEnabled;

        /// <inheritdoc/>
        public int HttpsRedirectPort => _host.HttpsRedirectPort;

        /// <inheritdoc/>
        public string AppId => _auth.AppId;
        /// <inheritdoc/>
        public string AppSecret => _auth.AppSecret;
        /// <inheritdoc/>
        public string TenantId => _auth.TenantId;
        /// <inheritdoc/>
        public string InstanceUrl => _auth.InstanceUrl;
        /// <inheritdoc/>
        public bool AuthRequired => _auth.AuthRequired;
        /// <inheritdoc/>
        public string Audience => _auth.Audience;
        /// <inheritdoc/>
        public string Domain => _auth.Domain;
        /// <inheritdoc/>
        public string TrustedIssuer => _auth.TrustedIssuer;
        /// <inheritdoc/>
        public TimeSpan AllowedClockSkew => _auth.AllowedClockSkew;

        /// <inheritdoc/>
        public bool UIEnabled => _openApi.UIEnabled;
        /// <inheritdoc/>
        public bool WithAuth => _openApi.WithAuth;
        /// <inheritdoc/>
        public string OpenApiAppId => _openApi.OpenApiAppId;
        /// <inheritdoc/>
        public string OpenApiAppSecret => _openApi.OpenApiAppSecret;
        /// <inheritdoc/>
        public bool UseV2 => _openApi.UseV2;

        /// <inheritdoc/>
        public string ServiceBusConnString => _sb.ServiceBusConnString;

        /// <inheritdoc/>
        public string SignalRConnString => _sr.SignalRConnString;
        /// <inheritdoc/>
        public string SignalRHubName => _sr.SignalRHubName;

        /// <summary>
        /// Whether to use role based access
        /// </summary>
        public bool UseRoles => GetBoolOrDefault("PCS_AUTH_ROLES");

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _openApi = new OpenApiConfig(configuration);
            _host = new HostConfig(configuration);
            _auth = new AuthConfig(configuration);
            _cors = new CorsConfig(configuration);
            _sb = new ServiceBusConfig(configuration);
            _sr = new SignalRServiceConfig(configuration);
        }

        private readonly OpenApiConfig _openApi;
        private readonly AuthConfig _auth;
        private readonly HostConfig _host;
        private readonly CorsConfig _cors;
        private readonly ServiceBusConfig _sb;
        private readonly SignalRServiceConfig _sr;
    }
}
