// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Application registry change listener
    /// </summary>
    public class ApplicationEventSubscriber : IEventHandler<ApplicationEventModel>, IDisposable {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="listeners"></param>
        public ApplicationEventSubscriber(IEventBus bus,
            IEnumerable<IApplicationRegistryListener> listeners) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _listeners = listeners?.ToList() ?? new List<IApplicationRegistryListener>();
            _token = _bus.RegisterAsync(this).Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _bus.UnregisterAsync(_token).Wait();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(ApplicationEventModel eventData) {
            switch (eventData.EventType) {
                case ApplicationEventType.New:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnApplicationNewAsync(
                            eventData.Context, eventData.Application)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case ApplicationEventType.Enabled:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnApplicationEnabledAsync(
                            eventData.Context, eventData.Application)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case ApplicationEventType.Disabled:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnApplicationDisabledAsync(
                            eventData.Context, eventData.Application)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case ApplicationEventType.Updated:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnApplicationUpdatedAsync(
                            eventData.Context, eventData.Application)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case ApplicationEventType.Deleted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnApplicationDeletedAsync(
                            eventData.Context, eventData.Application)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
            }
        }

        private readonly IEventBus _bus;
        private readonly List<IApplicationRegistryListener> _listeners;
        private readonly string _token;
    }
}
