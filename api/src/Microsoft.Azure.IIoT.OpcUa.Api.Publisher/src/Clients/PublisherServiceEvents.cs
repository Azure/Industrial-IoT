// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher service events
    /// </summary>
    public class PublisherServiceEvents : IPublisherServiceEvents {

        /// <summary>
        /// Event client
        /// </summary>
        /// <param name="api"></param>
        /// <param name="client"></param>
        public PublisherServiceEvents(IPublisherServiceApi api, ICallbackClient client) {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> NodePublishSubscribeByEndpointAsync(string endpointId,
            string userId, Func<MonitoredItemMessageApiModel, Task> callback) {

            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var registrar = await _client.GetRegistrarAsync(userId);
            try {
                var registration = registrar.Register(EventTargets.PublisherSampleTarget, callback);
                try {
                    await _api.NodePublishSubscribeByEndpointAsync(endpointId, registrar.UserId);
                    return new AsyncDisposable(registration,
                        () => _api.NodePublishUnsubscribeByEndpointAsync(endpointId,
                            registrar.UserId));
                }
                catch {
                    registration.Dispose();
                    throw;
                }
            }
            catch {
                registrar.Dispose();
                throw;
            }
        }

        private readonly IPublisherServiceApi _api;
        private readonly ICallbackClient _client;
    }
}
