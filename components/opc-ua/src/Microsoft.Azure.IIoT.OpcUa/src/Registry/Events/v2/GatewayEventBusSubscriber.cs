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
    /// Gateway registry change listener
    /// </summary>
    public class GatewayEventBusSubscriber : IEventHandler<GatewayEventModel>, IDisposable {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="listeners"></param>
        public GatewayEventBusSubscriber(IEventBus bus,
            IEnumerable<IGatewayRegistryListener> listeners) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _listeners = listeners?.ToList() ?? new List<IGatewayRegistryListener>();
            _token = _bus.RegisterAsync(this).Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _bus.UnregisterAsync(_token).Wait();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(GatewayEventModel eventData) {
            switch (eventData.EventType) {
                case GatewayEventType.New:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnGatewayNewAsync(
                            eventData.Context, eventData.Gateway)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case GatewayEventType.Updated:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnGatewayUpdatedAsync(
                            eventData.Context, eventData.Gateway, eventData.IsPatch ?? false)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case GatewayEventType.Deleted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnGatewayDeletedAsync(
                            eventData.Context, eventData.Id)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
            }
        }

        private readonly IEventBus _bus;
        private readonly List<IGatewayRegistryListener> _listeners;
        private readonly string _token;
    }
}
