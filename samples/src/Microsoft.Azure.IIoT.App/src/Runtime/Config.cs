// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Runtime {
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging.SignalR.Runtime;
    using Microsoft.Extensions.Configuration;
    /// <summary>
    /// Configuration aggregation
    /// </summary>
    public class Config : ApiConfig, IClientConfig, ISignalRServiceConfig {

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
        public string SignalRConnString => _sr.SignalRConnString;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {
            _auth = new ApiClientConfig(configuration);
            _sr = new SignalRServiceConfig(configuration);
        }

        private readonly ApiClientConfig _auth;
        private readonly SignalRServiceConfig _sr;
    }
}
