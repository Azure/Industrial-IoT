// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Events.v2 {
#if ZOMBIE

    /// <summary>
    /// Discovery progress listener
    /// </summary>
    public class DiscoveryProgressEventBusSubscriber : IEventHandler<DiscoveryProgressModel> {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="listeners"></param>
        public DiscoveryProgressEventBusSubscriber(IEnumerable<IDiscoveryProgressProcessor> listeners) {
            _listeners = listeners?.ToList() ?? new List<IDiscoveryProgressProcessor>();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(DiscoveryProgressModel eventData) {
            await Task.WhenAll(_listeners
                .Select(l => l.OnDiscoveryProgressAsync(eventData)
                .ContinueWith(t => Task.CompletedTask)));
        }

        private readonly List<IDiscoveryProgressProcessor> _listeners;
    }
#endif
}
