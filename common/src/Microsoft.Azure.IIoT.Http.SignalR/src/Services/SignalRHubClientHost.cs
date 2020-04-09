// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.SignalR.Services {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.AspNetCore.SignalR.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// SignalR hub client
    /// </summary>
    public class SignalRHubClientHost : ICallbackRegistrar, IHostProcess {

        /// <inheritdoc/>
        public string ConnectionId => _connection.ConnectionId;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="useMessagePack"></param>
        /// <param name="jsonSettings"></param>
        /// <param name="msgPack"></param>
        /// <param name="logger"></param>
        /// <param name="resourceId"></param>
        /// <param name="tokenProvider"></param>
        public SignalRHubClientHost(string endpointUrl,
            bool? useMessagePack, ILogger logger, string resourceId,
            ITokenProvider tokenProvider = null,
            IJsonSerializerSettingsProvider jsonSettings = null,
            IMessagePackFormatterResolverProvider msgPack = null) {
            if (string.IsNullOrEmpty(endpointUrl)) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            _jsonSettings = jsonSettings;
            _msgPack = msgPack;
            _endpointUri = new Uri(endpointUrl);
            _resourceId = resourceId ?? endpointUrl;
            _useMessagePack = (useMessagePack ?? false) && _msgPack != null;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenProvider = tokenProvider;
        }

        /// <inheritdoc/>
        public IDisposable Register(Func<object[], object, Task> handler,
            object thiz, string method, Type[] arguments) {
            _lock.Wait();
            try {
                if (!_started) {
                    throw new InvalidOperationException("Must start before registering");
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
                await DisposeAsync(_connection);
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
            _lock.Wait();
            try {
                if (_connection != null) {
                    _logger.Verbose("SignalR client was not stopped before disposing.");
                    Try.Op(() => DisposeAsync(_connection).Wait());
                    _connection = null;
                }
                _started = false;
            }
            finally {
                _lock.Release();
            }
            _lock.Dispose();
        }

        /// <summary>
        /// Open connection
        /// </summary>
        /// <returns></returns>
        private async Task<HubConnection> OpenAsync() {
            var builder = new HubConnectionBuilder()
                .WithAutomaticReconnect();
            if (_useMessagePack && _msgPack != null) {
                builder = builder.AddMessagePackProtocol(options => {
                    options.FormatterResolvers = _msgPack.GetResolvers().ToList();
                });
            }
            else {
                var jsonSettings = _jsonSettings?.Settings;
                if (jsonSettings != null) {
                    builder = builder.AddNewtonsoftJsonProtocol(options => {
                        options.PayloadSerializerSettings = jsonSettings;
                    });
                }
            }
            var connection = builder
                .WithUrl(_endpointUri, options => {
                    if (_tokenProvider != null) {
                        options.AccessTokenProvider = async () => {
                            try {
                                var token = await _tokenProvider.GetTokenForAsync(
                                    _resourceId);
                                return token?.RawToken;
                            }
                            catch {
                                return null;
                            }
                        };
                    }
                })
                .Build();
            connection.Closed += ex => OnClosedAsync(connection, ex);
            await connection.StartAsync();
            return connection;
        }

        /// <summary>
        /// Close connection
        /// </summary>
        /// <returns></returns>
        private async Task DisposeAsync(HubConnection connection) {
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
            _logger.Error(ex, "SignalR client host Disconnected!");
            await DisposeAsync(connection);
            if (_started) {
                // Reconnect
                _connection = await OpenAsync();
                _logger.Information("SignalR client host reconnecting...");
            }
        }

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IJsonSerializerSettingsProvider _jsonSettings;
        private readonly IMessagePackFormatterResolverProvider _msgPack;
        private readonly Uri _endpointUri;
        private readonly bool _useMessagePack;
        private readonly ILogger _logger;
        private readonly string _resourceId;
        private readonly ITokenProvider _tokenProvider;
        private HubConnection _connection;
        private bool _started;
    }
}