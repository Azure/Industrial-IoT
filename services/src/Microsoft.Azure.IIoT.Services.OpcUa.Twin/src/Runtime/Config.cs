// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Runtime {
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Cors;
    using Microsoft.Azure.IIoT.AspNetCore.Cors.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Deploy;
    using Microsoft.Azure.IIoT.Deploy.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hosting;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IWebHostConfig, IIoTHubConfig,
        ICorsConfig, IOpenApiConfig, IForwardedHeadersConfig, IRoleConfig,
        IContainerRegistryConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string CorsWhitelist => _cors.CorsWhitelist;
        /// <inheritdoc/>
        public bool CorsEnabled => _cors.CorsEnabled;

        /// <inheritdoc/>
        public int HttpsRedirectPort => _host.HttpsRedirectPort;
        /// <inheritdoc/>
        public string ServicePathBase => GetStringOrDefault(
            PcsVariable.PCS_TWIN_SERVICE_PATH_BASE,
            () => _host.ServicePathBase);

        /// <inheritdoc/>
        public bool UIEnabled => _openApi.UIEnabled;
        /// <inheritdoc/>
        public bool WithAuth => _openApi.WithAuth;
        /// <inheritdoc/>
        public string OpenApiAppId => _openApi.OpenApiAppId;
        /// <inheritdoc/>
        public string OpenApiAppSecret => _openApi.OpenApiAppSecret;
        /// <inheritdoc/>
        public string OpenApiAuthorizationUrl => _openApi.OpenApiAuthorizationUrl;
        /// <inheritdoc/>
        public bool UseV2 => _openApi.UseV2;
        /// <inheritdoc/>
        public string OpenApiServerHost => _openApi.OpenApiServerHost;

        /// <inheritdoc/>
        public string DockerServer => _cr.DockerServer;
        /// <inheritdoc/>
        public string DockerUser => _cr.DockerUser;
        /// <inheritdoc/>
        public string DockerPassword => _cr.DockerPassword;
        /// <inheritdoc/>
        public string ImagesNamespace => _cr.ImagesNamespace;
        /// <inheritdoc/>
        public string ImagesTag => _cr.ImagesTag;

        /// <inheritdoc/>
        public bool UseRoles => GetBoolOrDefault(PcsVariable.PCS_AUTH_ROLES);

        /// <inheritdoc/>
        public bool AspNetCoreForwardedHeadersEnabled =>
            _fh.AspNetCoreForwardedHeadersEnabled;
        /// <inheritdoc/>
        public int AspNetCoreForwardedHeadersForwardLimit =>
            _fh.AspNetCoreForwardedHeadersForwardLimit;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _openApi = new OpenApiConfig(configuration);
            _host = new WebHostConfig(configuration);
            _hub = new IoTHubConfig(configuration);
            _cors = new CorsConfig(configuration);
            _fh = new ForwardedHeadersConfig(configuration);
            _cr = new ContainerRegistryConfig(configuration);
        }

        private readonly ContainerRegistryConfig _cr;
        private readonly OpenApiConfig _openApi;
        private readonly WebHostConfig _host;
        private readonly CorsConfig _cors;
        private readonly IoTHubConfig _hub;
        private readonly ForwardedHeadersConfig _fh;
    }
}
