// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Publisher service event client
    /// </summary>
    public class PublisherServiceEvents : IPublisherServiceEvents {

        /// <inheritdoc/>
        public string UserId => _subscriber.UserId;

        /// <summary>
        /// Event client
        /// </summary>
        /// <param name="api"></param>
        /// <param name="subscriber"></param>
        public PublisherServiceEvents(IPublisherServiceApi api, ICallbackRegistration subscriber) {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        /// <inheritdoc/>
        public IEventSource<MonitoredItemMessageApiModel> Endpoint(string endpointId) {
            return _endpoints.GetOrAdd(endpointId, id =>
                new EventSource<MonitoredItemMessageApiModel> {
                    Subscriber = _subscriber,
                    Method = EventTargets.PublisherSampleTarget,
                    Add = () => _api.NodePublishSubscribeByEndpointAsync(id, UserId).Wait(),
                    Remove = () => _api.NodePublishUnsubscribeByEndpointAsync(id, UserId).Wait(),
                });
        }

        private readonly IPublisherServiceApi _api;
        private readonly ICallbackRegistration _subscriber;
        private readonly ConcurrentDictionary<string,
            IEventSource<MonitoredItemMessageApiModel>> _endpoints =
            new ConcurrentDictionary<string, IEventSource<MonitoredItemMessageApiModel>>();
    }
}
