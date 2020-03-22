// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.Default {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using Serilog;

    /// <summary>
    /// Hub publisher invoker base
    /// </summary>
    public abstract class EventBusCallbackBridge<THub, TEvent> : IHostProcess,
        IDisposable, IEventHandler<TEvent> {

        /// <summary>
        /// Callback invoker
        /// </summary>
        protected ICallbackInvoker Callback { get; }

        /// <summary>
        /// Create event publisher
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="callback"></param>
        /// <param name="logger"></param>
        public EventBusCallbackBridge(IEventBus bus, ICallbackInvokerT<THub> callback,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_registration != null) {
                    _logger.Debug("Event bus bridge for {hub} already running.",
                    typeof(THub).Name);
                    return;
                }
                _logger.Debug("Starting Event bus bridge for {hub}...",
                    typeof(THub).Name);
                _registration = await _bus.RegisterAsync(this);
                _logger.Information("Event bus bridge for {hub} started.",
                    typeof(THub).Name);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to start Event bus bridge for {hub}.",
                    typeof(THub).Name);
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
                _logger.Debug("Stopping Event bus bridge for {hub}...",
                    typeof(THub).Name);
                if (!string.IsNullOrEmpty(_registration)) {
                    await Try.Async(() => _bus.UnregisterAsync(_registration));
                }
                _registration = null;
                _logger.Information("Event bus bridge for {hub} stopped.",
                    typeof(THub).Name);
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Failed to stop Event bus bridge for {hub}.",
                    typeof(THub).Name);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public abstract Task HandleAsync(TEvent eventData);

        /// <inheritdoc/>
        public void Dispose() {
            StopAsync().Wait();
            _lock.Dispose();
        }

        private string _registration;
        private readonly ILogger _logger;
        private readonly IEventBus _bus;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    }
}
