// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.All.Runtime {
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IHostConfig,
        IForwardedHeadersConfig {

        /// <inheritdoc/>
        public int HttpsRedirectPort => _host.HttpsRedirectPort;
        /// <inheritdoc/>
        public string ServicePathBase => _host.ServicePathBase;

        /// <inheritdoc/>
        public bool AspNetCoreForwardedHeadersEnabled =>
            _fh.AspNetCoreForwardedHeadersEnabled;
        /// <inheritdoc/>
        public int AspNetCoreForwardedHeadersForwardLimit =>
            _fh.AspNetCoreForwardedHeadersForwardLimit;

        /// <inheritdoc/>
        public bool IsMinimumDeployment =>
            GetStringOrDefault(PcsVariable.PCS_DEPLOYMENT_LEVEL)
                .EqualsIgnoreCase("Minimum");

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _host = new HostConfig(configuration);
            _fh = new ForwardedHeadersConfig(configuration);
        }

        private readonly HostConfig _host;
        private readonly ForwardedHeadersConfig _fh;
    }
}
