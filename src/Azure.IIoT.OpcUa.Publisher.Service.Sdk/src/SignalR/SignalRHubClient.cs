// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR
{
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
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
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="jsonSettings"></param>
        /// <param name="messageHandler"></param>
        public SignalRHubClient(ISignalRClientConfig config,
            ILogger<SignalRHubClient> logger,
            INewtonsoftSerializerSettingsProvider jsonSettings = null,
            HttpMessageHandler messageHandler = null)
        {
            _jsonSettings = jsonSettings;
            _messageHandler = messageHandler;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clients = new Dictionary<string, SignalRClientRegistrar>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public async Task<ICallbackRegistrar> GetHubAsync(string endpointUrl)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SignalRHubClient));
            }
            if (string.IsNullOrEmpty(endpointUrl))
            {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            await _lock.WaitAsync().ConfigureAwait(false);
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
                    client = await SignalRClientRegistrar.CreateAsync(_config,
                        endpointUrl, _logger, _jsonSettings, null,
                        _messageHandler).ConfigureAwait(false);
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
            public string ConnectionId
            {
                get
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(nameof(SignalRClientRegistrar));
                    }
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
            /// <param name="config"></param>
            /// <param name="endpointUrl"></param>
            /// <param name="logger"></param>
            /// <param name="jsonSettings"></param>
            /// <param name="msgPack"></param>
            /// <param name="messageHandler"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentNullException"></exception>
            internal static async Task<SignalRClientRegistrar> CreateAsync(
                ISignalRClientConfig config, string endpointUrl, ILogger logger,
                INewtonsoftSerializerSettingsProvider jsonSettings,
                IMessagePackFormatterResolverProvider msgPack,
                HttpMessageHandler messageHandler)
            {
                if (string.IsNullOrEmpty(endpointUrl))
                {
                    throw new ArgumentNullException(nameof(endpointUrl));
                }

                var host = new SignalRHubClientHost(endpointUrl,
                    config.UseMessagePackProtocol,
                    logger, // TODO: should use logger factory here
                    config.TokenProvider, jsonSettings, msgPack, messageHandler);

                return new SignalRClientRegistrar(await host);
            }

            /// <inheritdoc/>
            public IDisposable Register(Func<object[], object, Task> handler,
                object thiz, string method, Type[] arguments)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(SignalRClientRegistrar));
                }
                return _client.Register(handler, thiz, method, arguments);
            }

            /// <summary>
            /// Dispose
            /// </summary>
            /// <returns></returns>
            /// <exception cref="ObjectDisposedException"></exception>
            public async ValueTask DisposeAsync()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(SignalRClientRegistrar));
                }
                _disposed = true;
                await _client.DisposeAsync().ConfigureAwait(false);
            }

            private bool _disposed;
            private readonly SignalRHubClientHost _client;
        }

        private readonly INewtonsoftSerializerSettingsProvider _jsonSettings;
        private readonly HttpMessageHandler _messageHandler;
        private readonly ISignalRClientConfig _config;
        private readonly Dictionary<string, SignalRClientRegistrar> _clients;
        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private bool _disposed;
    }
}
