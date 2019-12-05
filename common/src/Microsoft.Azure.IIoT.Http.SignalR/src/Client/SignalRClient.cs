// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.SignalR {
    using Microsoft.Azure.IIoT.Http.SignalR.Services;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Messaging;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// Subscriber factory for signalr service
    /// </summary>
    public sealed class SignalRClient : ICallbackClient, IDisposable {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public SignalRClient(ISignalRClientConfig config, ILogger logger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clients = new Dictionary<string, SignalRClientRegistrar>();
            _lock = new SemaphoreSlim(1, 1);

            // Garbage collect every 30 seconds
            _timer = new Timer(_ => TryGcAsync(), null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <inheritdoc/>
        public async Task<ICallbackRegistrar> GetRegistrarAsync(string userId) {
            await _lock.WaitAsync();
            try {
                if (string.IsNullOrEmpty(userId)) {
                    userId = _config.SignalRUserId;
                    if (string.IsNullOrEmpty(userId)) {
                        userId = Guid.NewGuid().ToString();
                    }
                }
                if (!_clients.TryGetValue(userId, out var client)) {
                    client = await SignalRClientRegistrar.CreateAsync(
                        _config.SignalREndpointUrl, _config.SignalRHubName,
                        userId, _logger);
                    _clients.Add(userId, client);
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

            private SignalRClientRegistrar(SignalRClientHost client) {
                _client = client;
                _handles = new HashSet<SignalRRegistrarHandle>();
            }

            /// <summary>
            /// Create instance by creating client host and starting it.
            /// </summary>
            /// <param name="endpointUrl"></param>
            /// <param name="hubName"></param>
            /// <param name="userId"></param>
            /// <param name="logger"></param>
            /// <returns></returns>
            internal static async Task<SignalRClientRegistrar> CreateAsync(string endpointUrl,
                string hubName, string userId, ILogger logger) {
                if (string.IsNullOrEmpty(endpointUrl)) {
                    throw new ArgumentException(nameof(endpointUrl));
                }
                if (string.IsNullOrEmpty(userId)) {
                    throw new ArgumentException(nameof(userId));
                }
                var host = new SignalRClientHost(endpointUrl, hubName, userId,
                    logger.ForContext<SignalRClientHost>());
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
                public string UserId {
                    get {
                        if (_outer._disposed) {
                            throw new ObjectDisposedException(nameof(SignalRRegistrarHandle));
                        }
                        return _outer._client.UserId;
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
            private readonly SignalRClientHost _client;
            private readonly HashSet<SignalRRegistrarHandle> _handles;
        }

        private readonly ISignalRClientConfig _config;
        private readonly Dictionary<string, SignalRClientRegistrar> _clients;
        private readonly SemaphoreSlim _lock;
        private readonly Timer _timer;
        private readonly ILogger _logger;
    }
}