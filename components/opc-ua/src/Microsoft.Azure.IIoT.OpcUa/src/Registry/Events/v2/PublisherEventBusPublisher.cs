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
    /// Publisher registry event publisher
    /// </summary>
    public class PublisherEventBusPublisher : IPublisherRegistryListener {

        /// <summary>
        /// Create event publisher
        /// </summary>
        /// <param name="bus"></param>
        public PublisherEventBusPublisher(IEventBus bus) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <inheritdoc/>
        public Task OnPublisherDeletedAsync(RegistryOperationContextModel context,
            string publisherId) {
            return _bus.PublishAsync(Wrap(PublisherEventType.Deleted, context,
                publisherId, null, false));
        }

        /// <inheritdoc/>
        public Task OnPublisherNewAsync(RegistryOperationContextModel context,
            PublisherModel publisher) {
            return _bus.PublishAsync(Wrap(PublisherEventType.New, context,
                publisher.Id, publisher, false));
        }

        /// <inheritdoc/>
        public Task OnPublisherUpdatedAsync(RegistryOperationContextModel context,
            PublisherModel publisher, bool isPatch) {
            return _bus.PublishAsync(Wrap(PublisherEventType.Updated, context,
                publisher.Id, publisher, isPatch));
        }

        /// <summary>
        /// Create publisher event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="publisherId"></param>
        /// <param name="publisher"></param>
        /// <param name="isPatch"></param>
        /// <returns></returns>
        private static PublisherEventModel Wrap(PublisherEventType type,
            RegistryOperationContextModel context, string publisherId,
            PublisherModel publisher, bool isPatch) {
            return new PublisherEventModel {
                EventType = type,
                Context = context,
                Id = publisherId,
                Publisher = publisher,
                IsPatch = isPatch == true ? true : (bool?)null
            };
        }

        private readonly IEventBus _bus;
    }
}
