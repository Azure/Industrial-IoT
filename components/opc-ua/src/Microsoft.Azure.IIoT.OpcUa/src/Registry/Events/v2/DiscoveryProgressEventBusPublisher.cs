// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discoverer progress processor
    /// </summary>
    public class DiscoveryProgressEventBusPublisher : IDiscoveryProgressProcessor {

        /// <summary>
        /// Create event discoverer
        /// </summary>
        /// <param name="bus"></param>
        public DiscoveryProgressEventBusPublisher(IEventBus bus) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <inheritdoc/>
        public Task OnDiscoveryProgressAsync(DiscoveryProgressModel message) {
            return _bus.PublishAsync(message);
        }

        private readonly IEventBus _bus;
    }
}
