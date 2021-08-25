// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.Gateway.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Transport;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.Cors;
    using Microsoft.Azure.IIoT.AspNetCore.Cors.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hosting;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using Opc.Ua;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IWebHostConfig, IIoTHubConfig,
        ICorsConfig, ITcpListenerConfig, IWebListenerConfig,
        ISessionServicesConfig, IRegistryConfig, IForwardedHeadersConfig {

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
            PcsVariable.PCS_GATEWAY_SERVICE_PATH_BASE,
            () => _host.ServicePathBase);

        /// <inheritdoc/>
        public string[] ListenUrls => null;
        /// <inheritdoc/>
        public X509Certificate2 TcpListenerCertificate => null;
        /// <inheritdoc/>
        public X509Certificate2Collection TcpListenerCertificateChain => null;
        /// <inheritdoc/>
        public ICertificateValidator CertificateValidator => null;
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

        /// <inheritdoc/>
        public string OpcUaRegistryServiceUrl => _api.OpcUaRegistryServiceUrl;

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

            _host = new WebHostConfig(configuration);
            _hub = new IoTHubConfig(configuration);
            _cors = new CorsConfig(configuration);
            _sessions = new SessionServicesConfig(configuration);
            _api = new RegistryConfig(configuration);
            _fh = new ForwardedHeadersConfig(configuration);
        }

        private readonly WebHostConfig _host;
        private readonly CorsConfig _cors;
        private readonly IoTHubConfig _hub;
        private readonly SessionServicesConfig _sessions;
        private readonly RegistryConfig _api;
        private readonly ForwardedHeadersConfig _fh;
    }
}
