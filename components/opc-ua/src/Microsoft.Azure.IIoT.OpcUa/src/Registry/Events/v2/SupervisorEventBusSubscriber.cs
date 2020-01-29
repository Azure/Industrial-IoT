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
    /// Supervisor registry change listener
    /// </summary>
    public class SupervisorEventBusSubscriber : IEventHandler<SupervisorEventModel>, IDisposable {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="listeners"></param>
        public SupervisorEventBusSubscriber(IEventBus bus,
            IEnumerable<ISupervisorRegistryListener> listeners) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _listeners = listeners?.ToList() ?? new List<ISupervisorRegistryListener>();
            _token = _bus.RegisterAsync(this).Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _bus.UnregisterAsync(_token).Wait();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(SupervisorEventModel eventData) {
            switch (eventData.EventType) {
                case SupervisorEventType.New:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnSupervisorNewAsync(
                            eventData.Context, eventData.Supervisor)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case SupervisorEventType.Updated:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnSupervisorUpdatedAsync(
                            eventData.Context, eventData.Supervisor, eventData.IsPatch ?? false)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case SupervisorEventType.Deleted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnSupervisorDeletedAsync(
                            eventData.Context, eventData.Id)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
            }
        }

        private readonly IEventBus _bus;
        private readonly List<ISupervisorRegistryListener> _listeners;
        private readonly string _token;
    }
}
