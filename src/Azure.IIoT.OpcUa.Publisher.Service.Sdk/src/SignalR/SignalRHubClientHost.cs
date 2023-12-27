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
    using System.Diagnostics;
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
        public string? ConnectionId => _connection?.ConnectionId;

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
            INewtonsoftSerializerSettingsProvider? jsonSettings,
            IMessagePackFormatterResolverProvider? msgPack)
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
        public IDisposable Register(Func<object?[], object, Task> handler,
            object thiz, string method, Type[] arguments)
        {
            if (!_started.IsCompleted)
            {
                // This should not happen if this was created when retrieving the hub
                _logger.LogWarning("No blocking to start connection. " +
                    "You should await the host connection before registering.");
                try
                {
                    // Wait 10 seconds to establish the connection and then register
                    // If that does not work, throw, user should have awaited!
                    if (!_started.Wait(TimeSpan.FromSeconds(10)))
                    {
                        throw new InvalidOperationException(
                            "Trying to register inside a connection without " +
                            "an established connection.");
                    }
                }
                catch (OperationCanceledException) when (_isDisposed)
                {
                    ObjectDisposedException.ThrowIf(_isDisposed, this);
                }
            }

            Debug.Assert(_connection != null);
            return _connection.On(method, arguments, handler, thiz);
        }

        /// <inheritdoc/>
        public IAwaiter<SignalRHubClientHost> GetAwaiter()
        {
            return _started.AsAwaiter(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            try
            {
                // Cancel any retrying
                _retryPolicy.Cancel();

                // Ensure started
                await _started.ConfigureAwait(false);

                // Dispose connection
                _logger.LogDebug("Stopping SignalR client host...");
                await DisposeAsync(_connection).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { } // Cancelled during start
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping SignalR client host.");
            }
            finally
            {
                _connection = null;
                _retryPolicy.Dispose();
                _logger.LogInformation("SignalR client host stopped.");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Start signal host client
        /// </summary>
        /// <returns></returns>
        private async Task StartAsync()
        {
            var context = new RetryContext();
            while (true)
            {
                _retryPolicy.Token.ThrowIfCancellationRequested();
                try
                {
                    _logger.LogDebug("Starting SignalR client host...");
                    _connection = await OpenAsync(_retryPolicy.Token).ConfigureAwait(false);
                    _logger.LogInformation("SignalR client host started.");
                    return;
                }
                catch (Exception ex)
                {
                    context.PreviousRetryCount++;
                    context.RetryReason = ex;
                    var delay = _retryPolicy.NextRetryDelay(context);
                    if (delay.HasValue)
                    {
                        _logger.LogError(ex,
                            "Error starting SignalR client host - retrying after {Delay}.", delay);
                        await Task.Delay(delay.Value, _retryPolicy.Token).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Open connection
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<HubConnection> OpenAsync(CancellationToken ct)
        {
            var builder = new HubConnectionBuilder()
                .WithAutomaticReconnect(_retryPolicy);
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
                        options.AccessTokenProvider = async () =>
                        {
                            var token = await _options.Value.TokenProvider().ConfigureAwait(false);
                            if (token?.StartsWith("Bearer ", StringComparison.Ordinal) == true)
                            {
                                // Strip bearer identifier from the token it is added by signalr.
                                token = token.Substring(7);
                            }
                            return token;
                        };
                    }
                })
                .Build();
            connection.Closed += ex =>
            {
                _logger.LogInformation(ex, "Connection closed!");
                return Task.CompletedTask;
            };
            connection.Reconnecting += ex =>
            {
                _logger.LogInformation(ex, "Connection Reconnecting...");
                return Task.CompletedTask;
            };
            connection.Reconnected += _ =>
            {
                _logger.LogInformation("Connection Reconnected!");
                return Task.CompletedTask;
            };
            await connection.StartAsync(ct).ConfigureAwait(false);
            return connection;
        }

        /// <summary>
        /// Close connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private static async Task DisposeAsync(HubConnection? connection)
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
        /// Linear retry policy
        /// </summary>
        private sealed class Retry : IRetryPolicy, IDisposable
        {
            /// <summary>
            /// Cancellation of retry
            /// </summary>
            public CancellationToken Token => _cts.Token;

            /// <inheritdoc/>
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                return _cts.IsCancellationRequested ? null :
                    TimeSpan.FromSeconds(Math.Min(60, retryContext.PreviousRetryCount));
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _cts.Dispose();
            }

            /// <summary>
            /// Cancel retrying
            /// </summary>
            internal void Cancel()
            {
                _cts.Cancel();
            }

            private readonly CancellationTokenSource _cts = new();
        }

        private readonly Retry _retryPolicy = new();
        private readonly INewtonsoftSerializerSettingsProvider? _jsonSettings;
        private readonly IMessagePackFormatterResolverProvider? _msgPack;
        private readonly Uri _endpointUri;
        private readonly bool _useMessagePack;
        private readonly IOptions<ServiceSdkOptions> _options;
        private readonly ILogger _logger;
        private readonly Task _started;
        private bool _isDisposed;
        private HubConnection? _connection;
    }
}
