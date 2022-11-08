// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Edge.Runtime {
    using Microsoft.Azure.IIoT.AspNetCore.Cors;
    using Microsoft.Azure.IIoT.AspNetCore.Cors.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.RateLimiting;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs.Runtime;
    using Microsoft.Azure.IIoT.Agent.Framework.Storage.Database;
    using Microsoft.Azure.IIoT.Hosting;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;
    using Microsoft.Azure.IIoT.AspNetCore.RateLimiting.Runtime;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IWebHostConfig, IIoTHubConfig,
        ICorsConfig, IOpenApiConfig, IJobOrchestratorConfig, ICosmosDbConfig,
        IJobDatabaseConfig, IWorkerDatabaseConfig, IForwardedHeadersConfig,
        IRoleConfig, IRateLimitingConfig {

        /// <inheritdoc/>
        public string DbConnectionString => _cosmos.DbConnectionString;
        /// <inheritdoc/>
        public int? ThroughputUnits => _cosmos.ThroughputUnits;
        /// <inheritdoc/>
        public string ContainerName => "iiot_opc";
        /// <inheritdoc/>
        public string DatabaseName => "iiot_opc";

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;

        /// <inheritdoc/>
        public int HttpsRedirectPort => _host.HttpsRedirectPort;
        /// <inheritdoc/>
        public string ServicePathBase => GetStringOrDefault(
            PcsVariable.PCS_PUBLISHER_ORCHESTRATOR_SERVICE_PATH_BASE,
                () => _host.ServicePathBase);

        /// <inheritdoc/>
        public TimeSpan JobStaleTime => _jobs.JobStaleTime;

        /// <inheritdoc/>
        public string CorsWhitelist => _cors.CorsWhitelist;
        /// <inheritdoc/>
        public bool CorsEnabled => _cors.CorsEnabled;

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
        public bool UseRoles => GetBoolOrDefault(PcsVariable.PCS_AUTH_ROLES);

        /// <inheritdoc/>
        public bool AspNetCoreForwardedHeadersEnabled =>
            _fh.AspNetCoreForwardedHeadersEnabled;
        /// <inheritdoc/>
        public int AspNetCoreForwardedHeadersForwardLimit =>
            _fh.AspNetCoreForwardedHeadersForwardLimit;
        /// <inheritdoc/>
        public int AspNetCoreRateLimitingMaxConcurrentRequests =>
            _rc.AspNetCoreRateLimitingMaxConcurrentRequests;
        /// <inheritdoc/>
        public string AspNetCoreRateLimitingPathException => "/healthz";

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
            _cosmos = new CosmosDbConfig(configuration);
            _jobs = new JobOrchestratorConfig(configuration);
            _fh = new ForwardedHeadersConfig(configuration);
            _rc = new RateLimitingConfig(configuration);
        }

        private readonly OpenApiConfig _openApi;
        private readonly WebHostConfig _host;
        private readonly CorsConfig _cors;
        private readonly CosmosDbConfig _cosmos;
        private readonly JobOrchestratorConfig _jobs;
        private readonly IoTHubConfig _hub;
        private readonly ForwardedHeadersConfig _fh;
        private readonly RateLimitingConfig _rc;
    }
}
