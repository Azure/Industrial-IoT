// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using Serilog;

    /// <summary>
    /// Application registry event publisher
    /// </summary>
    public class RegistryEventPublisherHost : IHostProcess, IDisposable,
        IEventHandler<ApplicationEventModel>, IEventHandler<EndpointEventModel> {

        /// <summary>
        /// Create event publisher
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="callback"></param>
        /// <param name="logger"></param>
        public RegistryEventPublisherHost(IEventBus bus, ICallbackInvoker callback, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_endpoints != null || _applications != null) {
                    _logger.Debug("Registry event publisher host already running.");
                    return;
                }
                _logger.Debug("Starting registry event publisher host...");

                _endpoints = await _bus.RegisterAsync(
                    (IEventHandler<EndpointEventModel>)this);
                _applications = await _bus.RegisterAsync(
                    (IEventHandler<ApplicationEventModel>)this);

                // ...
                _logger.Information("Registry event publisher host started.");
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to start registry event publisher host.");
                throw ex;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                _logger.Debug("Stopping registry event publisher host...");
                if (!string.IsNullOrEmpty(_endpoints)) {
                    await Try.Async(() => _bus.UnregisterAsync(_endpoints));
                    _endpoints = null;
                }
                if (!string.IsNullOrEmpty(_applications)) {
                    await Try.Async(() => _bus.UnregisterAsync(_applications));
                    _applications = null;
                }
                _logger.Information("Registry event publisher host stopped.");
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Failed to stop registry event publisher host.");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task HandleAsync(ApplicationEventModel eventData) {
            // Send to supervisor listeners
            var arguments = new object[] { eventData.ToApiModel() };
            return _callback.MulticastAsync(EventTargets.Applications,
                EventTargets.ApplicationEventTarget, arguments);
        }

        /// <inheritdoc/>
        public Task HandleAsync(EndpointEventModel eventData) {
            // Send to supervisor listeners
            var arguments = new object[] { eventData.ToApiModel() };
            return _callback.MulticastAsync(EventTargets.Endpoints,
                EventTargets.EndpointEventTarget, arguments);
        }

        /// <inheritdoc/>
        public void Dispose() {
            StopAsync().Wait();
            _lock.Dispose();
        }

        private readonly ILogger _logger;
        private readonly IEventBus _bus;
        private readonly ICallbackInvoker _callback;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private string _endpoints;
        private string _applications;
    }
}
