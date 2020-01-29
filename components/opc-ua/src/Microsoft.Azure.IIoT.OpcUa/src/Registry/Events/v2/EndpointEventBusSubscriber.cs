// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry change listener
    /// </summary>
    public class EndpointEventBusSubscriber : IEventHandler<EndpointEventModel>, IDisposable {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="listeners"></param>
        public EndpointEventBusSubscriber(IEventBus bus,
            IEnumerable<IEndpointRegistryListener> listeners) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _listeners = listeners?.ToList() ?? new List<IEndpointRegistryListener>();
            _token = _bus.RegisterAsync(this).Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _bus.UnregisterAsync(_token).Wait();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(EndpointEventModel eventData) {
            switch (eventData.EventType) {
                case EndpointEventType.New:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnEndpointNewAsync(
                            eventData.Context, eventData.Endpoint)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case EndpointEventType.Enabled:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnEndpointEnabledAsync(
                            eventData.Context, eventData.Endpoint)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case EndpointEventType.Disabled:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnEndpointDisabledAsync(
                            eventData.Context, eventData.Endpoint)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case EndpointEventType.Deactivated:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnEndpointDeactivatedAsync(
                            eventData.Context, eventData.Endpoint)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case EndpointEventType.Activated:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnEndpointActivatedAsync(
                            eventData.Context, eventData.Endpoint)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case EndpointEventType.Updated:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnEndpointUpdatedAsync(
                            eventData.Context, eventData.Endpoint, eventData.IsPatch ?? false)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case EndpointEventType.Deleted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnEndpointDeletedAsync(
                            eventData.Context, eventData.Id, eventData.Endpoint)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
            }
        }

        private readonly IEventBus _bus;
        private readonly List<IEndpointRegistryListener> _listeners;
        private readonly string _token;
    }
}
