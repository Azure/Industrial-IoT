// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discoverer registry event discoverer
    /// </summary>
    public class DiscovererEventBusPublisher : IDiscovererRegistryListener {

        /// <summary>
        /// Create event publisher
        /// </summary>
        /// <param name="bus"></param>
        public DiscovererEventBusPublisher(IEventBus bus) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <inheritdoc/>
        public Task OnDiscovererDeletedAsync(RegistryOperationContextModel context,
            string discovererId) {
            return _bus.PublishAsync(Wrap(DiscovererEventType.Deleted, context,
                discovererId, null));
        }

        /// <inheritdoc/>
        public Task OnDiscovererNewAsync(RegistryOperationContextModel context,
            DiscovererModel discoverer) {
            return _bus.PublishAsync(Wrap(DiscovererEventType.New, context,
                discoverer.Id, discoverer));
        }

        /// <inheritdoc/>
        public Task OnDiscovererUpdatedAsync(RegistryOperationContextModel context,
            DiscovererModel discoverer) {
            return _bus.PublishAsync(Wrap(DiscovererEventType.Updated, context,
                discoverer.Id, discoverer));
        }

        /// <summary>
        /// Create discoverer event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="discovererId"></param>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        private static DiscovererEventModel Wrap(DiscovererEventType type,
            RegistryOperationContextModel context, string discovererId,
            DiscovererModel discoverer) {
            return new DiscovererEventModel {
                EventType = type,
                Context = context,
                Id = discovererId,
                Discoverer = discoverer
            };
        }

        private readonly IEventBus _bus;
    }
}
