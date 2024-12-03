// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR
{
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Hub client factory for signalr
    /// </summary>
    public sealed class SignalRHubClient : ICallbackClient, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="jsonSettings"></param>
        public SignalRHubClient(IOptions<ServiceSdkOptions> options,
            ILogger<SignalRHubClient> logger, INewtonsoftSerializerSettingsProvider? jsonSettings = null)
        {
            _jsonSettings = jsonSettings;
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clients = [];
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public async Task<ICallbackRegistrar> GetHubAsync(string endpointUrl, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (string.IsNullOrEmpty(endpointUrl))
            {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var lookup = endpointUrl;
                if (!_clients.TryGetValue(lookup, out var client) ||
                    client.ConnectionId == null)
                {
                    if (client != null)
                    {
                        await client.DisposeAsync().ConfigureAwait(false);
                        _clients.Remove(lookup);
                    }
                    client = await SignalRClientRegistrar.CreateAsync(_options,
                        endpointUrl, _logger, _jsonSettings, null, ct).ConfigureAwait(false);
                    _clients.Add(lookup, client);
                }
                return client;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var client in _clients.Values)
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                _clients.Clear();
            }
            finally
            {
                _lock.Release();
                _disposed = true;
            }
            _lock.Dispose();
        }

        /// <summary>
        /// SignalR client registry that manages consumed handles to it
        /// </summary>
        private sealed class SignalRClientRegistrar : ICallbackRegistrar
        {
            /// <inheritdoc/>
            public string? ConnectionId
            {
                get
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    return _client.ConnectionId;
                }
            }

            private SignalRClientRegistrar(SignalRHubClientHost client)
            {
                _client = client;
            }

            /// <summary>
            /// Create instance by creating client host and starting it.
            /// </summary>
            /// <param name="options"></param>
            /// <param name="endpointUrl"></param>
            /// <param name="logger"></param>
            /// <param name="jsonSettings"></param>
            /// <param name="msgPack"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentNullException"></exception>
            internal static async Task<SignalRClientRegistrar> CreateAsync(
                IOptions<ServiceSdkOptions> options, string endpointUrl, ILogger logger,
                INewtonsoftSerializerSettingsProvider? jsonSettings,
                IMessagePackFormatterResolverProvider? msgPack, CancellationToken ct)
            {
                if (string.IsNullOrEmpty(endpointUrl))
                {
                    throw new ArgumentNullException(nameof(endpointUrl));
                }

                var host = new SignalRHubClientHost(endpointUrl, options,
                    logger, // TODO: should use logger factory here
                    jsonSettings, msgPack);

                await host.WaitAsync(ct).ConfigureAwait(false);
                return new SignalRClientRegistrar(host);
            }

            /// <inheritdoc/>
            public IDisposable Register(Func<object?[], object, Task> handler,
                object thiz, string method, Type[] arguments)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                return _client.Register(handler, thiz, method, arguments);
            }

            /// <summary>
            /// Dispose
            /// </summary>
            /// <returns></returns>
            /// <exception cref="ObjectDisposedException"></exception>
            public async ValueTask DisposeAsync()
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _disposed = true;
                await _client.DisposeAsync().ConfigureAwait(false);
            }

            private bool _disposed;
            private readonly SignalRHubClientHost _client;
        }

        private readonly INewtonsoftSerializerSettingsProvider? _jsonSettings;
        private readonly IOptions<ServiceSdkOptions> _options;
        private readonly Dictionary<string, SignalRClientRegistrar> _clients;
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private bool _disposed;
    }
}
