// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Runtime {
    using Microsoft.Azure.IIoT.Services.Swagger;
    using Microsoft.Azure.IIoT.Services.Swagger.Runtime;
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Azure.IIoT.Services.Cors.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Runtime;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : ConfigBase, IAuthConfig, IIoTHubConfig,
        ICorsConfig, IClientConfig, ISwaggerConfig, IEventHubConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;
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
        public int HttpsRedirectPort => _auth.HttpsRedirectPort;
        /// <inheritdoc/>
        public bool AuthRequired => _auth.AuthRequired;
        /// <inheritdoc/>
        public string TrustedIssuer => _auth.TrustedIssuer;
        /// <inheritdoc/>
        public TimeSpan AllowedClockSkew => _auth.AllowedClockSkew;
        /// <inheritdoc/>
        public bool UIEnabled => _swagger.UIEnabled;
        /// <inheritdoc/>
        public bool WithAuth => _swagger.WithAuth;
        /// <inheritdoc/>
        public bool WithHttpScheme => _swagger.WithHttpScheme;
        /// <inheritdoc/>
        public string SwaggerAppId => _swagger.SwaggerAppId;
        /// <inheritdoc/>
        public string SwaggerAppSecret => _swagger.SwaggerAppSecret;
        /// <inheritdoc/>
        public string EventHubConnString => _eh.EventHubConnString;
        /// <inheritdoc/>
        public string EventHubPath => _eh.EventHubPath;
        /// <inheritdoc/>
        public bool UseWebsockets => _eh.UseWebsockets;
        /// <inheritdoc/>
        public string ConsumerGroup => _eh.ConsumerGroup;

        /// <summary>
        /// Whether to use role based access
        /// </summary>
        public bool UseRoles => GetBoolOrDefault("PCS_AUTH_ROLES");

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) :
            base(configuration) {

            _swagger = new SwaggerConfig(configuration);
            _auth = new AuthConfig(configuration);
            _hub = new IoTHubConfig(configuration);
            _cors = new CorsConfig(configuration);
            _eh = new EventHubConfig(configuration);
        }

        private readonly SwaggerConfig _swagger;
        private readonly AuthConfig _auth;
        private readonly CorsConfig _cors;
        private readonly EventHubConfig _eh;
        private readonly IoTHubConfig _hub;
    }
}
