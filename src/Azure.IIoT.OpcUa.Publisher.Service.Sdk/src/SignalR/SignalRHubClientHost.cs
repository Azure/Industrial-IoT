// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR
{
    using Furly;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using MessagePack.Resolvers;
    using Microsoft.AspNetCore.SignalR.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// SignalR hub client
    /// </summary>
    internal sealed class SignalRHubClientHost : ICallbackRegistrar,
        IAwaitable<SignalRHubClientHost>, IAsyncDisposable, IDisposable
    {
        /// <inheritdoc/>
        public string ConnectionId => _connection.ConnectionId;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="jsonSettings"></param>
        /// <param name="msgPack"></param>
        public SignalRHubClientHost(string endpointUrl,
            IOptions<ServiceSdkOptions> options, ILogger logger,
            INewtonsoftSerializerSettingsProvider jsonSettings,
            IMessagePackFormatterResolverProvider msgPack)
        {
            if (string.IsNullOrEmpty(endpointUrl))
            {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _jsonSettings = jsonSettings;
            _msgPack = msgPack;
            _endpointUri = new Uri(endpointUrl);

            _useMessagePack = options.Value.UseMessagePackProtocol && _msgPack != null;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _started = StartAsync();
        }

        /// <inheritdoc/>
        public IDisposable Register(Func<object[], object, Task> handler,
            object thiz, string method, Type[] arguments)
        {
            _lock.Wait();
            try
            {
                if (_connection == null)
                {
                    throw new InvalidOperationException("Must start before registering");
                }
                return _connection.On(method, arguments, handler, thiz);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public IAwaiter<SignalRHubClientHost> GetAwaiter()
        {
            return _started.AsAwaiter(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
                _logger.LogDebug("Stopping SignalR client host...");
                await DisposeAsync(_connection).ConfigureAwait(false);
                _connection = null;
                _logger.LogInformation("SignalR client host stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping SignalR client host.");
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lock.Wait();
            try
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
                if (_connection != null)
                {
                    _logger.LogTrace("SignalR client was not stopped before disposing.");
                    Try.Op(() => DisposeAsync(_connection).Wait());
                    _connection = null;
                }
            }
            finally
            {
                _lock.Release();
            }
            _lock.Dispose();
        }

        /// <summary>
        /// Start signal host client
        /// </summary>
        /// <returns></returns>
        private async Task StartAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                _logger.LogDebug("Starting SignalR client host...");
                _connection = await OpenAsync().ConfigureAwait(false);
                _logger.LogInformation("SignalR client host started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting SignalR client host.");
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Open connection
        /// </summary>
        /// <returns></returns>
        private async Task<HubConnection> OpenAsync()
        {
            var builder = new HubConnectionBuilder()
                .WithAutomaticReconnect();
            if (_useMessagePack && _msgPack != null)
            {
                builder = builder.AddMessagePackProtocol(options =>
                {
                    options.SerializerOptions = options.SerializerOptions.WithResolver(
                        CompositeResolver.Create(_msgPack.GetResolvers().ToArray()));
                });
            }
            else
            {
                var jsonSettings = _jsonSettings?.Settings;
                if (jsonSettings != null)
                {
                    builder = builder.AddNewtonsoftJsonProtocol(options =>
                    options.PayloadSerializerSettings = jsonSettings);
                }
            }
            var connection = builder
                .WithUrl(_endpointUri, options =>
                {
                    if (_options.Value.HttpMessageHandler != null)
                    {
                        options.HttpMessageHandlerFactory = _options.Value.HttpMessageHandler;
                    }
                    if (_options.Value.TokenProvider != null)
                    {
                        options.AccessTokenProvider = _options.Value.TokenProvider;
                    }
                })
                .Build();
            connection.Closed += ex => OnClosedAsync(connection, ex);
            await connection.StartAsync().ConfigureAwait(false);
            return connection;
        }

        /// <summary>
        /// Close connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private static async Task DisposeAsync(HubConnection connection)
        {
            if (connection == null)
            {
                return;
            }
            await Try.Async(() => connection?.StopAsync() ??
                Task.CompletedTask).ConfigureAwait(false);
            await Try.Async(() => connection?.DisposeAsync().AsTask() ??
                Task.CompletedTask).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle close event
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private async Task OnClosedAsync(HubConnection connection, Exception ex)
        {
            _logger.LogError(ex, "SignalR client host Disconnected!");
            await DisposeAsync(connection).ConfigureAwait(false);
            if (!_isDisposed)
            {
                // Reconnect
                _connection = await OpenAsync().ConfigureAwait(false);
                _logger.LogInformation("SignalR client host reconnecting...");
            }
        }

        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly INewtonsoftSerializerSettingsProvider _jsonSettings;
        private readonly IMessagePackFormatterResolverProvider _msgPack;
        private readonly Uri _endpointUri;
        private readonly bool _useMessagePack;
        private readonly IOptions<ServiceSdkOptions> _options;
        private readonly ILogger _logger;
        private readonly Task _started;
        private bool _isDisposed;
        private HubConnection _connection;
    }
}
