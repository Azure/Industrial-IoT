// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry change listener
    /// </summary>
    public class EndpointEventBusSubscriber : IEventHandler<EndpointEventModel> {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="listeners"></param>
        public EndpointEventBusSubscriber(IEnumerable<IEndpointRegistryListener> listeners) {
            _listeners = listeners?.ToList() ?? new List<IEndpointRegistryListener>();
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
                            eventData.Context, eventData.Endpoint)
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

        private readonly List<IEndpointRegistryListener> _listeners;
    }
}
