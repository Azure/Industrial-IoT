// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Default {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Tasks;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    /// <summary>
    /// Supervisor event broker - publishes locally, and also
    /// all event versions to event bus
    /// </summary>
    public sealed class SupervisorEventBroker :
        IRegistryEventBroker<ISupervisorRegistryListener>,
        IRegistryEvents<ISupervisorRegistryListener> {

        /// <summary>
        /// Create broker
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="processor"></param>
        public SupervisorEventBroker(IEventBus bus, ITaskProcessor processor = null) {
            _processor = processor;
            _listeners = new ConcurrentDictionary<string, ISupervisorRegistryListener>();

            _listeners.TryAdd("v2", new Events.v2.SupervisorEventBusPublisher(bus));
            // ...
        }

        /// <inheritdoc/>
        public Action Register(ISupervisorRegistryListener listener) {
            var token = Guid.NewGuid().ToString();
            _listeners.TryAdd(token, listener);
            return () => _listeners.TryRemove(token, out var _);
        }

        /// <inheritdoc/>
        public Task NotifyAllAsync(Func<ISupervisorRegistryListener, Task> evt) {
            Task task() => Task
                .WhenAll(_listeners.Values.Select(l => evt(l)).ToArray());
            if (_processor == null || !_processor.TrySchedule(task)) {
                return task().ContinueWith(t => Task.CompletedTask);
            }
            return Task.CompletedTask;
        }

        private readonly ITaskProcessor _processor;
        private readonly ConcurrentDictionary<string, ISupervisorRegistryListener> _listeners;
    }
}
