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
    /// Discoverer registry change listener
    /// </summary>
    public class DiscovererEventBusSubscriber : IEventHandler<DiscovererEventModel>,
        IDisposable {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="listeners"></param>
        public DiscovererEventBusSubscriber(IEventBus bus,
            IEnumerable<IDiscovererRegistryListener> listeners) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _listeners = listeners?.ToList() ?? new List<IDiscovererRegistryListener>();
            _token = _bus.RegisterAsync(this).Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _bus.UnregisterAsync(_token).Wait();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(DiscovererEventModel eventData) {
            switch (eventData.EventType) {
                case DiscovererEventType.New:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnDiscovererNewAsync(
                            eventData.Context, eventData.Discoverer)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case DiscovererEventType.Updated:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnDiscovererUpdatedAsync(
                            eventData.Context, eventData.Discoverer)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case DiscovererEventType.Deleted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnDiscovererDeletedAsync(
                            eventData.Context, eventData.Id)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
            }
        }

        private readonly IEventBus _bus;
        private readonly List<IDiscovererRegistryListener> _listeners;
        private readonly string _token;
    }
}
