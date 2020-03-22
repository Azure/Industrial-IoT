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
    using System.Linq;

    /// <summary>
    /// Hub client factory for signalr
    /// </summary>
    public sealed class SignalRHubClient : ICallbackClient, IDisposable {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="provider"></param>
        /// <param name="jsonSettings"></param>
        public SignalRHubClient(ISignalRClientConfig config, ILogger logger,
            ITokenProvider provider = null,
            IJsonSerializerSettingsProvider jsonSettings = null) {
            _jsonSettings = jsonSettings;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _provider = provider;
            _clients = new Dictionary<string, SignalRClientRegistrar>();
            _lock = new SemaphoreSlim(1, 1);

            // Garbage collect every 30 seconds
            _timer = new Timer(_ => TryGcAsync(), null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <inheritdoc/>
        public async Task<ICallbackRegistrar> GetHubAsync(string endpointUrl,
            string resourceId) {
            if (string.IsNullOrEmpty(endpointUrl)) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            await _lock.WaitAsync();
            try {
                var lookup = endpointUrl;
                if (!string.IsNullOrEmpty(resourceId)) {
                    lookup += resourceId;
                }
                if (!_clients.TryGetValue(lookup, out var client)) {
                    client = await SignalRClientRegistrar.CreateAsync(
                        _config, endpointUrl, _logger, resourceId, _provider, _jsonSettings);
                    _clients.Add(lookup, client);
                }
                return client.GetHandle();
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _timer.Dispose();
            _lock.Wait();
            try {
                if (_clients.Count > 0) {
                    Task.WaitAll(_clients.Values
                        .Select(c => c.DisposeIfEmptyAsync(true))
                        .ToArray());
                    _clients.Clear();
                }
            }
            finally {
                _lock.Release();
            }
            _lock.Dispose();
        }

        /// <summary>
        /// Garbage collect unused clients
        /// </summary>
        private async void TryGcAsync() {
            try {
                await _lock.WaitAsync();
                try {
                    foreach (var client in _clients.ToList()) {
                        if (await client.Value.DisposeIfEmptyAsync()) {
                            _clients.Remove(client.Key);
                        }
                    }
                }
                finally {
                    _lock.Release();
                }
            }
            catch (Exception ex) {
                _logger.Verbose(ex, "Failed to check.");
            }
        }

        /// <summary>
        /// SignalR client registry that manages consumed handles to it
        /// </summary>
        private sealed class SignalRClientRegistrar {

            private SignalRClientRegistrar(SignalRHubClientHost client) {
                _client = client;
                _handles = new HashSet<SignalRRegistrarHandle>();
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
                ISignalRClientConfig config, string endpointUrl,
                ILogger logger, string resourceId, ITokenProvider provider,
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

            /// <summary>
            /// Try close
            /// </summary>
            /// <param name="force"></param>
            /// <returns></returns>
            internal async Task<bool> DisposeIfEmptyAsync(bool force = false) {
                lock (_handles) {
                    if (_handles.Count == 0 && !_disposed) {
                        _disposed = true;
                        force = true;
                    }
                    else if (force && !_disposed) {
                        _disposed = true;
                        _handles.Clear();
                    }
                    else {
                        force = false;
                    }
                }
                if (force) {
                    // Refcount is 0 or forced dispose, stop.
                    await _client.StopAsync();
                    _client.Dispose();
                }
                return force;
            }

            /// <summary>
            /// Get a new handle
            /// </summary>
            /// <returns></returns>
            internal ICallbackRegistrar GetHandle() {
                lock (_handles) {
                    if (_disposed) {
                        throw new ObjectDisposedException(nameof(SignalRClientRegistrar));
                    }
                    var handle = new SignalRRegistrarHandle(this);
                    _handles.Add(handle);
                    return handle;
                }
            }

            /// <summary>
            /// Disposable SignalR Client handle
            /// </summary>
            private sealed class SignalRRegistrarHandle : ICallbackRegistrar {

                /// <inheritdoc/>
                public string ConnectionId {
                    get {
                        if (_outer._disposed) {
                            throw new ObjectDisposedException(nameof(SignalRRegistrarHandle));
                        }
                        return _outer._client.ConnectionId;
                    }
                }

                /// <summary>
                /// Create client
                /// </summary>
                /// <param name="outer"></param>
                public SignalRRegistrarHandle(SignalRClientRegistrar outer) {
                    _outer = outer;
                }

                /// <inheritdoc/>
                public IDisposable Register(Func<object[], object, Task> handler,
                    object thiz, string method, Type[] arguments) {
                    if (_outer._disposed) {
                        throw new ObjectDisposedException(nameof(SignalRRegistrarHandle));
                    }
                    return _outer._client.Register(handler, thiz, method, arguments);
                }

                /// <inheritdoc/>
                public void Dispose() {
                    _outer.Dispose(this);
                }

                private readonly SignalRClientRegistrar _outer;
            }

            /// <summary>
            /// Remove client handle from handle list
            /// </summary>
            /// <param name="signalRClient"></param>
            private void Dispose(SignalRRegistrarHandle signalRClient) {
                lock (_handles) {
                    _handles.Remove(signalRClient);
                }
            }

            private bool _disposed;
            private readonly SignalRHubClientHost _client;
            private readonly HashSet<SignalRRegistrarHandle> _handles;
        }

        private readonly IJsonSerializerSettingsProvider _jsonSettings;
        private readonly ISignalRClientConfig _config;
        private readonly Dictionary<string, SignalRClientRegistrar> _clients;
        private readonly SemaphoreSlim _lock;
        private readonly Timer _timer;
        private readonly ILogger _logger;
        private readonly ITokenProvider _provider;
    }
}