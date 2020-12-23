// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Application registry change listener
    /// </summary>
    public class ApplicationEventBusSubscriber : IEventHandler<ApplicationEventModel>{

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="listeners"></param>
        public ApplicationEventBusSubscriber(IEnumerable<IApplicationRegistryListener> listeners) {
            _listeners = listeners?.ToList() ?? new List<IApplicationRegistryListener>();
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
                            eventData.Context, eventData.Id, eventData.Application)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
            }
        }

        private readonly List<IApplicationRegistryListener> _listeners;
    }
}
