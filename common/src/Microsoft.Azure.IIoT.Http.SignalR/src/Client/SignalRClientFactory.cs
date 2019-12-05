// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.SignalR {
    using Microsoft.Azure.IIoT.Http.SignalR.Services;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscriber factory for signalr service
    /// </summary>
    public sealed class SignalRClientFactory : ISubscriberFactory {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public SignalRClientFactory(ISignalRClientConfig config, ILogger logger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="userId"></param>
        public async Task<ICallbackRegistration> CreateAsync(string userId) {
            var client = new SignalRClientHost(_config.SignalREndpointUrl,
                _config.SignalRHubName, userId ?? _config.SignalRUserId, _logger);
            await client.StartAsync().ConfigureAwait(false);
            return client;
        }

        private readonly ISignalRClientConfig _config;
        private readonly ILogger _logger;
    }
}