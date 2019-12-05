// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.SignalR.Clients {
    using Microsoft.Azure.IIoT.Http.SignalR.Services;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscriber for signalr service
    /// </summary>
    public sealed class SignalRClient : ICallbackRegistration {

        /// <inheritdoc/>
        public string UserId => _client.UserId;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public SignalRClient(ISignalRClientConfig config, ILogger logger) :
            this (config.SignalREndpointUrl, config.SignalRHubName,
                config.SignalRUserId, logger){
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="hubName"></param>
        /// <param name="userId"></param>
        /// <param name="logger"></param>
        public SignalRClient(string endpointUrl, string hubName,
            string userId, ILogger logger) {
            _client = new SignalRClientHost(endpointUrl, hubName, userId, logger);
            _client.StartAsync().Wait();
        }

        /// <inheritdoc/>
        public IDisposable Register(Func<object[], object, Task> handler,
            object thiz, string method, Type[] arguments) {
            return _client.Register(handler, thiz, method, arguments);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _client.Dispose();
        }

        private readonly SignalRClientHost _client;
    }
}