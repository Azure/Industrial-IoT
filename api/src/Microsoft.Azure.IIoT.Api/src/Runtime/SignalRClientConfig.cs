// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR.Runtime {
    using Microsoft.Azure.IIoT.Api.Configuration.Runtime;
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class SignalRClientConfig : ApiConfigBase, ISignalRClientConfig {

        /// <summary>Management configuration endpoint</summary>
        public string SignalREndpointUrl => _config.ConfigurationServiceUrl;

        /// <summary>Dedicated Management client id</summary>
        public string SignalRUserId => null;

        /// <summary>SignalR Hubname</summary>
        public string SignalRHubName => null;

        /// <inheritdoc/>
        public SignalRClientConfig(IConfiguration configuration) :
            base(configuration) {
            _config = new ConfigurationConfig(configuration);
        }

        private readonly ConfigurationConfig _config;
    }
}
