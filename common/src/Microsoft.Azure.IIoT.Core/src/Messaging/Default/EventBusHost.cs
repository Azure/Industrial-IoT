// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.Default {
    using Microsoft.Azure.IIoT.Messaging;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Event bus host to auto inject handlers
    /// </summary>
    public class EventBusHost : IHostProcess {

        /// <summary>
        /// Auto registers handlers in client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public EventBusHost(IEventBus client, IEnumerable<IHandler> handlers, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers.ToDictionary(h => h, k => (string)null);
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_handlers.Any(h => h.Value != null)) {
                    _logger.Debug("Event bus host already running.");
                    return;
                }
                var register = _client.GetType().GetMethod(nameof(IEventBus.RegisterAsync));
                foreach (var handler in _handlers.Keys.ToList()) {
                    var type = handler.GetType();
                    foreach (var itf in type.GetInterfaces()) {
                        try {
                            var eventType = itf.GetGenericArguments().FirstOrDefault();
                            if (eventType == null) {
                                continue;
                            }
                            var method = register.MakeGenericMethod(eventType);
                            _logger.Debug("Starting Event bus bridge for {type}...",
                                type.Name);
                            var token = await (Task<string>)method.Invoke(
                                _client, new object[] { handler });
                            _handlers[handler] = token; // Store token to unregister
                            _logger.Information("Event bus bridge for {type} started.",
                                type.Name);
                        }
                        catch (Exception ex) {
                            _logger.Error(ex, "Failed to start Event bus host for {type}.",
                                type.Name);
                            throw;
                        }
                    }
                }
                _logger.Information("Event bus host running.");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                foreach (var token in _handlers.Where(x => x.Value != null).ToList()) {
                    try {
                        // Unregister using stored token
                        await _client.UnregisterAsync(token.Value);
                        _handlers[token.Key] = null;
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed to stop Event bus host using token {token}.",
                            token);
                        throw;
                    }
                }
                _handlers.Clear();
                _logger.Information("Event bus host stopped.");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            StopAsync().Wait();
            _lock.Dispose();
        }

        private readonly IEventBus _client;
        private readonly ILogger _logger;
        private readonly Dictionary<IHandler, string> _handlers;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    }
}