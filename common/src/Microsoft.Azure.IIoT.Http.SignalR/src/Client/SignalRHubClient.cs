// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.SignalR {
    using Microsoft.Azure.IIoT.Http.SignalR.Services;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Auth;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Hub client factory for signalr
    /// </summary>
    public sealed class SignalRHubClient : ICallbackClient, IDisposable, IAsyncDisposable {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="provider"></param>
        /// <param name="jsonSettings"></param>
        public SignalRHubClient(ISignalRClientConfig config, ILogger logger,
            ITokenProvider provider = null, IJsonSerializerSettingsProvider jsonSettings = null) {
            _jsonSettings = jsonSettings;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _provider = provider;
            _clients = new Dictionary<string, SignalRClientRegistrar>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public async Task<ICallbackRegistrar> GetHubAsync(string endpointUrl,
            string resourceId) {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(SignalRHubClient));
            }
            if (string.IsNullOrEmpty(endpointUrl)) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            await _lock.WaitAsync();
            try {
                var lookup = endpointUrl;
                if (!string.IsNullOrEmpty(resourceId)) {
                    lookup += resourceId;
                }
                if (!_clients.TryGetValue(lookup, out var client) ||
                    client.ConnectionId == null) {
                    if (client != null) {
                        await client.DisposeAsync();
                        _clients.Remove(lookup);
                    }
                    client = await SignalRClientRegistrar.CreateAsync(_config,
                        endpointUrl, _logger, resourceId, _provider, _jsonSettings);
                    _clients.Add(lookup, client);
                }
                return client;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync() {
            if (_disposed) {
                return;
            }
            await _lock.WaitAsync();
            try {
                foreach (var client in _clients.Values) {
                    await client.DisposeAsync();
                }
                _clients.Clear();
            }
            finally {
                _lock.Release();
                _disposed = true;
            }
            _lock.Dispose();
        }

        /// <summary>
        /// SignalR client registry that manages consumed handles to it
        /// </summary>
        private sealed class SignalRClientRegistrar : ICallbackRegistrar {

            /// <inheritdoc/>
            public string ConnectionId {
                get {
                    if (_disposed) {
                        throw new ObjectDisposedException(nameof(SignalRClientRegistrar));
                    }
                    return _client.ConnectionId;
                }
            }

            private SignalRClientRegistrar(SignalRHubClientHost client) {
                _client = client;
            }

            /// <summary>
            /// Create instance by creating client host and starting it.
            /// </summary>
            /// <param name="config"></param>
            /// <param name="jsonSettings"></param>
            /// <param name="endpointUrl"></param>
            /// <param name="logger"></param>
            /// <param name="resourceId"></param>
            /// <param name="provider"></param>
            /// <returns></returns>
            internal static async Task<SignalRClientRegistrar> CreateAsync(
                ISignalRClientConfig config, string endpointUrl, ILogger logger,
                string resourceId, ITokenProvider provider,
                IJsonSerializerSettingsProvider jsonSettings = null) {

                if (string.IsNullOrEmpty(endpointUrl)) {
                    throw new ArgumentException(nameof(endpointUrl));
                }
                var host = new SignalRHubClientHost(endpointUrl,
                    config.UseMessagePackProtocol,
                    logger.ForContext<SignalRHubClientHost>(),
                    resourceId, provider, jsonSettings);

                await host.StartAsync().ConfigureAwait(false);
                return new SignalRClientRegistrar(host);
            }

            /// <inheritdoc/>
            public IDisposable Register(Func<object[], object, Task> handler,
                object thiz, string method, Type[] arguments) {
                if (_disposed) {
                    throw new ObjectDisposedException(nameof(SignalRClientRegistrar));
                }
                return _client.Register(handler, thiz, method, arguments);
            }

            /// <summary>
            /// Dispose
            /// </summary>
            /// <returns></returns>
            public async Task DisposeAsync() {
                if (_disposed) {
                    throw new ObjectDisposedException(nameof(SignalRClientRegistrar));
                }
                _disposed = true;
                await _client.StopAsync();
            }

            private bool _disposed;
            private readonly SignalRHubClientHost _client;
        }

        private readonly IJsonSerializerSettingsProvider _jsonSettings;
        private readonly ISignalRClientConfig _config;
        private readonly Dictionary<string, SignalRClientRegistrar> _clients;
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private readonly ITokenProvider _provider;
        private bool _disposed;
    }
}