// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Identity.Runtime {
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IClientConfig,
        IItemContainerConfig, ICosmosDbConfig, IForwardedHeadersConfig {

        /// <inheritdoc/>
        public string AppId => _auth.AppId;
        /// <inheritdoc/>
        public string AppSecret => _auth.AppSecret;
        /// <inheritdoc/>
        public string TenantId => _auth.TenantId;
        /// <inheritdoc/>
        public string InstanceUrl => _auth.InstanceUrl;
        /// <inheritdoc/>
        public string Domain => _auth.Domain;

        /// <inheritdoc/>
        public string DbConnectionString => _cosmos.DbConnectionString;
        /// <inheritdoc/>
        public int? ThroughputUnits => _cosmos.ThroughputUnits;
        /// <inheritdoc/>
        public string ContainerName => "iiot_opc";
        /// <inheritdoc/>
        public string DatabaseName => "iiot_opc";

        /// <inheritdoc/>
        public int HttpsRedirectPort => _host.HttpsRedirectPort;
        /// <inheritdoc/>
        public string ServicePathBase => GetStringOrDefault(
            PcsVariable.PCS_JOBS_SERVICE_PATH_BASE,
            () => _host.ServicePathBase);

        /// <summary>
        /// Whether to use role based access
        /// </summary>
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

            _auth = new AuthConfig(configuration);
            _host = new HostConfig(configuration);
            _cosmos = new CosmosDbConfig(configuration);
            _fh = new ForwardedHeadersConfig(configuration);
        }

        private readonly HostConfig _host;
        private readonly ClientConfig _auth;
        private readonly CosmosDbConfig _cosmos;
        private readonly ForwardedHeadersConfig _fh;
    }
}
