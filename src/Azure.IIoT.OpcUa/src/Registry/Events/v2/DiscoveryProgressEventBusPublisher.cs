// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Events.v2 {
#if ZOMBIE

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
            if (message.TimeStamp + TimeSpan.FromSeconds(10) < DateTime.UtcNow) {
                // Do not forward stale events - todo make configurable / add metric
                return Task.CompletedTask;
            }
            return _bus.PublishAsync(message);
        }

        private readonly IEventBus _bus;
    }
#endif
}
