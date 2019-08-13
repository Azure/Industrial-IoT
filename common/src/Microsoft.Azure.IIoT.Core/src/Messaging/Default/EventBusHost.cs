// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.Default {
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Event bus host to auto inject handlers
    /// </summary>
    public class EventBusHost : IHost {

        /// <summary>
        /// Auto registers handlers in client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="handlers"></param>
        public EventBusHost(IEventBus client, IEnumerable<IHandler> handlers) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _handlers = handlers.ToDictionary(h => h, k => (string)null);
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            var register = _client.GetType().GetMethod(nameof(IEventBus.RegisterAsync));
            foreach (var handler in _handlers.Keys.ToList()) {
                foreach (var itf in handler.GetType().GetInterfaces()) {
                    try {
                        var eventType = itf.GetGenericArguments().FirstOrDefault();
                        if (eventType == null) {
                            continue;
                        }
                        var method = register.MakeGenericMethod(eventType);
                        var token = await (Task<string>)method.Invoke(
                            _client, new object[] { handler });
                        _handlers[handler] = token; // Store token to unregister
                    }
                    catch {
                        continue;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            foreach (var token in _handlers.ToList()) {
                // Unregister using stored token
                await _client.UnregisterAsync(token.Value);
                _handlers[token.Key] = null;
            }
        }

        private readonly IEventBus _client;
        private readonly Dictionary<IHandler, string> _handlers;
    }
}