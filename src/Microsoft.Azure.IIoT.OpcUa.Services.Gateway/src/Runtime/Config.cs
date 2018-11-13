// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Gateway.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Transport;
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Azure.IIoT.Services.Cors.Runtime;
    using Microsoft.Azure.IIoT.Storage.Azure;
    using Microsoft.Azure.IIoT.Storage.Azure.Runtime;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Runtime;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IdentityModel.Selectors;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : LogConfig, IAuthConfig, IIoTHubConfig,
        ICorsConfig, IClientConfig, ICosmosDbConfig, ITcpListenerConfig,
        IWebListenerConfig, ISessionServicesConfig {

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
        public bool AuthRequired => _auth.AuthRequired;
        /// <inheritdoc/>
        public string TrustedIssuer => _auth.TrustedIssuer;
        /// <inheritdoc/>
        public TimeSpan AllowedClockSkew => _auth.AllowedClockSkew;
        /// <inheritdoc/>
        public string DbConnectionString => _db.DbConnectionString;
        /// <inheritdoc/>
        public string DatabaseId => _db.DatabaseId;
        /// <inheritdoc/>
        public string CollectionId => _db.CollectionId;

        /// <inheritdoc/>
        public string[] ListenUrls => null;
        /// <inheritdoc/>
        public X509Certificate2 TcpListenerCertificate => null;
        /// <inheritdoc/>
        public X509Certificate2Collection TcpListenerCertificateChain => null;
        /// <inheritdoc/>
        public X509CertificateValidator CertificateValidator => null;
        /// <inheritdoc/>
        public string PublicDnsAddress => null;
        /// <inheritdoc/>
        public int Port => 51111; //  Utils.UaTcpDefaultPort;
        /// <inheritdoc/>
        public TimeSpan MaxRequestAge => _sessions.MaxRequestAge;
        /// <inheritdoc/>
        public int NonceLength => _sessions.NonceLength;
        /// <inheritdoc/>
        public int MaxSessionCount => _sessions.MaxSessionCount;
        /// <inheritdoc/>
        public TimeSpan MaxSessionTimeout => _sessions.MaxSessionTimeout;
        /// <inheritdoc/>
        public TimeSpan MinSessionTimeout => _sessions.MinSessionTimeout;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="serviceId"></param>
        /// <param name="configuration"></param>
        public Config(string processId, string serviceId,
            IConfigurationRoot configuration) :
            base(processId, configuration) {

            _auth = new AuthConfig(configuration, serviceId);
            _hub = new IoTHubConfig(configuration, serviceId);
            _cors = new CorsConfig(configuration);
            _db = new CosmosDbConfig(configuration);
            _sessions = new SessionServicesConfig(configuration);
        }

        private readonly AuthConfig _auth;
        private readonly CorsConfig _cors;
        private readonly CosmosDbConfig _db;
        private readonly IoTHubConfig _hub;
        private readonly SessionServicesConfig _sessions;
    }
}
