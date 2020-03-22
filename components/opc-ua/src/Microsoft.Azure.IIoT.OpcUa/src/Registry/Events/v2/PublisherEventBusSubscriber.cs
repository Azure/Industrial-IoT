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
    /// Publisher registry change listener
    /// </summary>
    public class PublisherEventBusSubscriber : IEventHandler<PublisherEventModel>, IDisposable {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="listeners"></param>
        public PublisherEventBusSubscriber(IEventBus bus,
            IEnumerable<IPublisherRegistryListener> listeners) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _listeners = listeners?.ToList() ?? new List<IPublisherRegistryListener>();
            _token = _bus.RegisterAsync(this).Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _bus.UnregisterAsync(_token).Wait();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(PublisherEventModel eventData) {
            switch (eventData.EventType) {
                case PublisherEventType.New:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnPublisherNewAsync(
                            eventData.Context, eventData.Publisher)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case PublisherEventType.Updated:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnPublisherUpdatedAsync(
                            eventData.Context, eventData.Publisher, eventData.IsPatch ?? false)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case PublisherEventType.Deleted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnPublisherDeletedAsync(
                            eventData.Context, eventData.Id)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
            }
        }

        private readonly IEventBus _bus;
        private readonly List<IPublisherRegistryListener> _listeners;
        private readonly string _token;
    }
}
