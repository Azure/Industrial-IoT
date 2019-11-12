// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Messaging.Default;
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Registry service event client
    /// </summary>
    public class RegistryServiceEvents : IRegistryServiceEvents {

        /// <inheritdoc/>
        public string UserId => _subscriber.UserId;

        /// <inheritdoc/>
        public IEventSource<ApplicationEventApiModel> Applications { get; }

        /// <inheritdoc/>
        public IEventSource<EndpointEventApiModel> Endpoints { get; }

        /// <inheritdoc/>
        public IEventSource<SupervisorEventApiModel> Supervisors { get; }

        /// <inheritdoc/>
        public IEventSource<PublisherEventApiModel> Publishers { get; }

        /// <summary>
        /// Event client
        /// </summary>
        /// <param name="api"></param>
        /// <param name="subscriber"></param>
        public RegistryServiceEvents(IRegistryServiceApi api, ICallbackRegistration subscriber) {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));

            Applications = new EventSource<ApplicationEventApiModel> {
                Subscriber = _subscriber,
                Method = EventTargets.ApplicationEventTarget,
                Add = () => _api.SubscribeApplicationEventsAsync(UserId).Wait(),
                Remove = () => _api.UnsubscribeApplicationEventsAsync(UserId).Wait(),
            };
            Endpoints = new EventSource<EndpointEventApiModel> {
                Subscriber = _subscriber,
                Method = EventTargets.EndpointEventTarget,
                Add = () => _api.SubscribeEndpointEventsAsync(UserId).Wait(),
                Remove = () => _api.UnsubscribeEndpointEventsAsync(UserId).Wait(),
            };
            Supervisors= new EventSource<SupervisorEventApiModel> {
                Subscriber = _subscriber,
                Method = EventTargets.SupervisorEventTarget,
                Add = () => _api.SubscribeSupervisorEventsAsync(UserId).Wait(),
                Remove = () => _api.UnsubscribeSupervisorEventsAsync(UserId).Wait(),
            };
            Publishers = new EventSource<PublisherEventApiModel> {
                Subscriber = _subscriber,
                Method = EventTargets.PublisherEventTarget,
                Add = () => _api.SubscribePublisherEventsAsync(UserId).Wait(),
                Remove = () => _api.UnsubscribePublisherEventsAsync(UserId).Wait(),
            };
        }

        /// <inheritdoc/>
        public IEventSource<DiscoveryProgressApiModel> Supervisor(string supervisorId) {
            return _supervisors.GetOrAdd(supervisorId, id =>
                new EventSource<DiscoveryProgressApiModel> {
                    Subscriber = _subscriber,
                    Method = EventTargets.DiscoveryProgressTarget,
                    Add = () => _api.SubscribeDiscoveryProgressBySupervisorsIdAsync(id, UserId).Wait(),
                    Remove = () => _api.UnsubscribeDiscoveryProgressBySupervisorsIdAsync(id, UserId).Wait(),
                });
        }

        /// <inheritdoc/>
        public IEventSource<DiscoveryProgressApiModel> Discovery(string requestId) {
            return _requests.GetOrAdd(requestId, id =>
                new EventSource<DiscoveryProgressApiModel> {
                    Subscriber = _subscriber,
                    Method = EventTargets.DiscoveryProgressTarget,
                    Add = () => _api.SubscribeDiscoveryProgressByRequestIdAsync(id, UserId).Wait(),
                    Remove = () => _api.UnsubscribeDiscoveryProgressByRequestIdAsync(id, UserId).Wait(),
                });
        }

        private readonly IRegistryServiceApi _api;
        private readonly ICallbackRegistration _subscriber;
        private readonly ConcurrentDictionary<string,
            IEventSource<DiscoveryProgressApiModel>> _supervisors =
            new ConcurrentDictionary<string, IEventSource<DiscoveryProgressApiModel>>();
        private readonly ConcurrentDictionary<string,
            IEventSource<DiscoveryProgressApiModel>> _requests =
            new ConcurrentDictionary<string, IEventSource<DiscoveryProgressApiModel>>();
    }
}
