// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Identity.Runtime {
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IIoTHubConfig,
        IIdentityTokenUpdaterConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;
        /// <inheritdoc/>
        public int TokenLength => _service.TokenLength;
        /// <inheritdoc/>
        public TimeSpan TokenLifetime => _service.TokenLifetime;
        /// <inheritdoc/>
        public TimeSpan TokenStaleInterval => _service.TokenStaleInterval;
        /// <inheritdoc/>
        public TimeSpan UpdateInterval => _service.UpdateInterval;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _hub = new IoTHubConfig(configuration);
            _service = new IdentityTokenUpdaterConfig(configuration);
        }

        private readonly IoTHubConfig _hub;
        private readonly IdentityTokenUpdaterConfig _service;
    }
}
