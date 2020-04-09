// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

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
}
