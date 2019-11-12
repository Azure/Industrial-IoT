// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR.Clients {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.AspNetCore.SignalR.Client;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Subscriber for signalr service
    /// </summary>
    public class SignalRClientHost : ICallbackRegistration, IHost {

        /// <inheritdoc/>
        public string UserId { get; }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public SignalRClientHost(ISignalRClientConfig config, ILogger logger) :
            this (config.SignalREndpointUrl, config.SignalRHubName, config.SignalRUserId, logger){
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="hubName"></param>
        /// <param name="userId"></param>
        /// <param name="logger"></param>
        public SignalRClientHost(string endpointUrl, string hubName,
            string userId, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(endpointUrl)) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            if (string.IsNullOrEmpty(userId)) {
                UserId = Guid.NewGuid().ToString();
            }
            else {
                UserId = userId;
            }
            if (string.IsNullOrEmpty(hubName)) {
                hubName = "default";
            }
            _endpointUri = new UriBuilder(endpointUrl.TrimEnd('/') + "/" + hubName) {
                Query = $"user={UserId}"
            }.Uri;
        }

        /// <inheritdoc/>
        public IDisposable Register(Func<object[], object, Task> handler,
            object thiz, string method, Type[] arguments) {
            _lock.Wait();
            try {
                if (!_started) {
                    // Lazy start on first event registration
                    _started = true;
                    try {
                        _connection = OpenAsync().Result;
                    }
                    catch {
                        _started = false;
                        throw;
                    }
                }
                return _connection.On(method, arguments, handler, thiz);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_started) {
                    _logger.Debug("SignalR client host already running.");
                    return;
                }
                _logger.Debug("Starting SignalR client host...");
                _started = true;
                _connection = await OpenAsync();
                _logger.Information("SignalR client host started.");
            }
            catch (Exception ex) {
                _started = false;
                _logger.Error(ex, "Error starting SignalR client host.");
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
                if (!_started) {
                    return;
                }
                _started = false;
                _logger.Debug("Stopping SignalR client host...");
                await CloseAsync(_connection);
                _connection = null;
                _logger.Information("SignalR client host stopped.");
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Error stopping SignalR client host.");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _started = false;
            Try.Op(() => CloseAsync(_connection).Wait());
            _connection = null;
            _lock.Dispose();
        }

        /// <summary>
        /// Open connection
        /// </summary>
        /// <returns></returns>
        private async Task<HubConnection> OpenAsync() {
            var connection = new HubConnectionBuilder()
                .WithUrl(_endpointUri)
                .Build();
            connection.Closed += ex => OnClosedAsync(connection, ex);
            await connection.StartAsync();
            return connection;
        }

        /// <summary>
        /// Close connection
        /// </summary>
        /// <returns></returns>
        private async Task CloseAsync(HubConnection connection) {
            if (connection == null) {
                return;
            }
            await Try.Async(() => connection?.StopAsync());
            await Try.Async(() => connection?.DisposeAsync());
        }

        /// <summary>
        /// Handle close event
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private async Task OnClosedAsync(HubConnection connection, Exception ex) {
            _logger.Error(ex, "Disconnected!");
            await CloseAsync(connection);
            if (_started) {
                // Reconnect
                _connection = await OpenAsync();
                _logger.Information("Reconnecting...");
            }
        }

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly Uri _endpointUri;
        private readonly ILogger _logger;
        private HubConnection _connection;
        private bool _started;
    }
}