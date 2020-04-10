// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Runtime {
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Azure.IIoT.Hosting;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Runtime;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration aggregation
    /// </summary>
    public class Config : ApiConfig, IOAuthClientConfig, ISignalRServiceConfig,
        IWebHostConfig, IForwardedHeadersConfig {

        /// <inheritdoc/>
        public string Scheme => _client.Scheme;
        /// <inheritdoc/>
        public string AppId => _client.AppId;
        /// <inheritdoc/>
        public string AppSecret => _client.AppSecret;
        /// <inheritdoc/>
        public string TenantId => _client.TenantId;
        /// <inheritdoc/>
        public string InstanceUrl => _client.InstanceUrl;
        /// <summary>Audience</summary>
        public string Audience => _client.Audience;

        /// <inheritdoc/>
        public string SignalRConnString => _sr.SignalRConnString;

        /// <inheritdoc/>
        public int HttpsRedirectPort => _host.HttpsRedirectPort;
        /// <inheritdoc/>
        public string ServicePathBase => GetStringOrDefault(
            PcsVariable.PCS_FRONTEND_APP_SERVICE_PATH_BASE,
                () => _host.ServicePathBase);

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

            _client = new AadApiClientConfig(configuration);
            _host = new WebHostConfig(configuration);
            _fh = new ForwardedHeadersConfig(configuration);
            _sr = new SignalRServiceConfig(configuration);
        }

        private readonly AadApiClientConfig _client;
        private readonly SignalRServiceConfig _sr;
        private readonly WebHostConfig _host;
        private readonly ForwardedHeadersConfig _fh;
    }
}
