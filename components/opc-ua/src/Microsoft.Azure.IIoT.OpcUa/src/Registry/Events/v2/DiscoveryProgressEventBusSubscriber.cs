// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Discovery progress listener
    /// </summary>
    public class DiscoveryProgressEventBusSubscriber : IEventHandler<DiscoveryProgressModel>,
        IDisposable {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="listeners"></param>
        public DiscoveryProgressEventBusSubscriber(IEventBus bus,
            IEnumerable<IDiscoveryProgressProcessor> listeners) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _listeners = listeners?.ToList() ?? new List<IDiscoveryProgressProcessor>();
            _token = _bus.RegisterAsync(this).Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _bus.UnregisterAsync(_token).Wait();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(DiscoveryProgressModel eventData) {
            await Task.WhenAll(_listeners
                .Select(l => l.OnDiscoveryProgressAsync(eventData)
                .ContinueWith(t => Task.CompletedTask)));
        }

        private readonly IEventBus _bus;
        private readonly List<IDiscoveryProgressProcessor> _listeners;
        private readonly string _token;
    }
}
