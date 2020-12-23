// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Events {
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Simple in memory event bus
    /// </summary>
    public class TestEventBus : IEventBus, IEventProcessingHost {

        public Task PublishAsync<T>(T message) {
            var name = typeof(T).GetMoniker();
            _handlers.TryGetValue(name, out var handler);
            return ((IEventHandler<T>)handler).HandleAsync(message);
        }

        public Task<string> RegisterAsync<T>(IEventHandler<T> handler) {
            var token = typeof(T).GetMoniker();
            _handlers.AddOrUpdate(token, handler);
            return Task.FromResult(token);
        }

        public Task UnregisterAsync(string token) {
            _handlers.Remove(token);
            return Task.CompletedTask;
        }

        public Task StartAsync() {
            return Task.CompletedTask;
        }

        public Task StopAsync() {
            return Task.CompletedTask;
        }

        private readonly Dictionary<string, object> _handlers =
            new Dictionary<string, object>();
    }
}
