// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Exceptions;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Nito.AsyncEx;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Opc.Ua.Extensions;

    /// <summary>
    /// OPC UA Client based on official ua client reference sample.
    /// </summary>
    internal sealed partial class OpcUaClient : DefaultSessionFactory, IOpcUaClient,
        IOpcUaClientDiagnostics
    {
        /// <summary>
        /// Client namespace
        /// </summary>
        internal const string Namespace = "http://opcfoundation.org/UA/Client/Types.xsd";

        /// <summary>
        /// The session keepalive interval to be used in ms.
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; }

        /// <summary>
        /// Min time to wait until reconnect.
        /// </summary>
        public TimeSpan? MinReconnectDelay { get; set; }

        /// <summary>
        /// Timeout for session creation
        /// </summary>
        public TimeSpan? CreateSessionTimeout { get; set; }

        /// <summary>
        /// The session lifetime.
        /// </summary>
        public TimeSpan? SessionTimeout { get; set; }

        /// <summary>
        /// Session operation timeout
        /// </summary>
        public TimeSpan? OperationTimeout { get; set; }

        /// <summary>
        /// Service call timeout
        /// </summary>
        public TimeSpan? ServiceCallTimeout { get; set; }

        /// <summary>
        /// Connect timeout for service calls
        /// </summary>
        public TimeSpan? ConnectTimeout { get; set; }

        /// <summary>
        /// Minimum number of publish requests to queue
        /// </summary>
        public int? MinPublishRequests { get; set; }

        /// <summary>
        /// Max number of publish requests to ever queue
        /// </summary>
        public int? MaxPublishRequests { get; set; }

        /// <summary>
        /// Percentage ratio of publish requests per subscription
        /// </summary>
        public int? PublishRequestsPerSubscriptionPercent { get; set; }

        /// <summary>
        /// The linger timeout.
        /// </summary>
        public TimeSpan? LingerTimeout { get; set; }

        /// <summary>
        /// Disable complex type preloading.
        /// </summary>
        public bool DisableComplexTypePreloading { get; set; }

        /// <summary>
        /// Do active error handling on the publish path
        /// </summary>
        public bool ActivePublishErrorHandling { get; set; }

        /// <summary>
        /// Operation limits to use in the sessions
        /// </summary>
        internal OperationLimits? LimitOverrides { get; set; }

        /// <summary>
        /// Last diagnostic information on this client
        /// </summary>
        internal ChannelDiagnosticModel LastDiagnostics => _lastDiagnostics;

        /// <summary>
        /// No complex type loading ever
        /// </summary>
        public bool DisableComplexTypeLoading
            => _connection.Options.HasFlag(ConnectionOptions.NoComplexTypeSystem);

        /// <summary>
        /// Transfer subscription on reconnect
        /// </summary>
        public bool DisableTransferSubscriptionOnReconnect
            => _connection.Options.HasFlag(ConnectionOptions.NoSubscriptionTransfer);

        /// <summary>
        /// Client is connected
        /// </summary>
        public bool IsConnected
            => _session?.Connected ?? false;

        /// <inheritdoc/>
        public EndpointConnectivityState State
            => _lastState;

        /// <inheritdoc/>
        public int BadPublishRequestCount
            => _session?.DefunctRequestCount ?? 0;

        /// <inheritdoc/>
        public int GoodPublishRequestCount
            => _session?.GoodPublishRequestCount ?? 0;

        /// <inheritdoc/>
        public int OutstandingRequestCount
            => _session?.OutstandingRequestCount ?? 0;

        /// <inheritdoc/>
        public int SubscriptionCount
            => _session?.Subscriptions.Count(s => s.Created) ?? 0;

        /// <inheritdoc/>
        public int MinPublishRequestCount
            => _session?.MinPublishRequestCount ?? 0;

        /// <inheritdoc/>
        public int ReconnectCount => _numberOfConnectRetries;

        /// <summary>
        /// Disconnected state
        /// </summary>
        internal static IOpcUaClientDiagnostics Disconnected { get; }
            = new DisconnectState();

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="connection"></param>
        /// <param name="serializer"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="timeProvider"></param>
        /// <param name="meter"></param>
        /// <param name="metrics"></param>
        /// <param name="notifier"></param>
        /// <param name="reverseConnectManager"></param>
        /// <param name="diagnosticsCallback"></param>
        /// <param name="maxReconnectPeriod"></param>
        /// <param name="sessionName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OpcUaClient(ApplicationConfiguration configuration,
            ConnectionIdentifier connection, IJsonSerializer serializer,
            ILoggerFactory loggerFactory, TimeProvider timeProvider,
            Meter meter, IMetricsContext metrics,
            EventHandler<EndpointConnectivityStateEventArgs>? notifier,
            ReverseConnectManager? reverseConnectManager,
            Action<ChannelDiagnosticModel> diagnosticsCallback,
            TimeSpan? maxReconnectPeriod = null, string? sessionName = null)
        {
            _timeProvider = timeProvider;
            if (connection?.Connection?.Endpoint?.Url == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            _connection = connection.Connection;
            _diagnosticsCb = diagnosticsCallback;
            _lastDiagnostics = new ChannelDiagnosticModel
            {
                Connection = _connection,
                TimeStamp = _timeProvider.GetUtcNow()
            };
            Debug.Assert(_connection.GetEndpointUrls().Any());
            _reverseConnectManager = reverseConnectManager;

            _meter = meter ??
                throw new ArgumentNullException(nameof(meter));
            _metrics = metrics ??
                throw new ArgumentNullException(nameof(metrics));
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));
            _notifier = notifier;

            InitializeMetrics();

            _logger = _loggerFactory.CreateLogger<OpcUaClient>();
            _tokens = new Dictionary<string, CancellationTokenSource>();
            _lastState = EndpointConnectivityState.Disconnected;
            _sessionName = sessionName ?? connection.ToString();
            _maxReconnectPeriod = maxReconnectPeriod ?? TimeSpan.Zero;
            if (_maxReconnectPeriod == TimeSpan.Zero)
            {
                _maxReconnectPeriod = TimeSpan.FromSeconds(30);
            }
            _reconnectHandler = new SessionReconnectHandler(true,
                (int)_maxReconnectPeriod.TotalMilliseconds);
            _cts = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<(ConnectionEvent, object?)>();
            _disconnectLock = _lock.WriterLock(_cts.Token);
            _channelMonitor = _timeProvider.CreateTimer(_ => OnUpdateConnectionDiagnostics(),
                null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _sessionManager = ManageSessionStateMachineAsync(_cts.Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Release();
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return $"{_sessionName} [state:{_lastState}|refs:{_refCount}]";
        }

        /// <inheritdoc/>
        public void ManageSubscription(IOpcUaSubscription subscription, bool closeSubscription)
        {
            TriggerConnectionEvent(closeSubscription ?
                ConnectionEvent.SubscriptionClose : ConnectionEvent.SubscriptionManage, subscription);
        }

        /// <inheritdoc/>
        public override Session Create(ISessionChannel channel, ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint)
        {
            return new OpcUaSession(this, _serializer, _loggerFactory.CreateLogger<OpcUaSession>(),
                _timeProvider, (ITransportChannel)channel, configuration, endpoint);
        }

        /// <inheritdoc/>
        public override Session Create(ITransportChannel channel, ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint, X509Certificate2? clientCertificate,
            EndpointDescriptionCollection? availableEndpoints,
            StringCollection? discoveryProfileUris)
        {
            return new OpcUaSession(this, _serializer, _loggerFactory.CreateLogger<OpcUaSession>(),
                _timeProvider, channel, configuration, endpoint,
                clientCertificate, availableEndpoints, discoveryProfileUris);
        }

        /// <inheritdoc/>
        public override Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint, bool updateBeforeConnect, string sessionName,
            uint sessionTimeout, IUserIdentity identity, IList<string> preferredLocales,
            CancellationToken ct)
        {
            return CreateAsync(configuration, endpoint, updateBeforeConnect, false,
                sessionName, sessionTimeout, identity, preferredLocales, ct);
        }

        /// <inheritdoc/>
        public async override Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint, bool updateBeforeConnect, bool checkDomain,
            string sessionName, uint sessionTimeout, IUserIdentity identity, IList<string> preferredLocales,
            CancellationToken ct)
        {
            return await Session.Create(this, configuration, (ITransportWaitingConnection?)null, endpoint,
                updateBeforeConnect, checkDomain, sessionName, sessionTimeout,
                identity, preferredLocales, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async override Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ITransportWaitingConnection connection, ConfiguredEndpoint endpoint, bool updateBeforeConnect,
            bool checkDomain, string sessionName, uint sessionTimeout, IUserIdentity identity,
            IList<string> preferredLocales, CancellationToken ct)
        {
            return await Session.Create(this, configuration, connection, endpoint, updateBeforeConnect,
                checkDomain, sessionName, sessionTimeout, identity, preferredLocales, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override ISession Create(ApplicationConfiguration configuration, ITransportChannel channel,
            ConfiguredEndpoint endpoint, X509Certificate2 clientCertificate,
            EndpointDescriptionCollection? availableEndpoints, StringCollection? discoveryProfileUris)
        {
            return Session.Create(this, configuration, channel, endpoint, clientCertificate,
                availableEndpoints, discoveryProfileUris);
        }

        /// <inheritdoc/>
        public async override Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ReverseConnectManager? reverseConnectManager, ConfiguredEndpoint endpoint, bool updateBeforeConnect,
            bool checkDomain, string sessionName, uint sessionTimeout, IUserIdentity userIdentity,
            IList<string> preferredLocales, CancellationToken ct)
        {
            if (reverseConnectManager == null)
            {
                return await CreateAsync(configuration, endpoint, updateBeforeConnect,
                    checkDomain, sessionName, sessionTimeout, userIdentity, preferredLocales,
                    ct).ConfigureAwait(false);
            }
            ITransportWaitingConnection? connection;
            do
            {
                connection = await reverseConnectManager.WaitForConnection(endpoint.EndpointUrl,
                    endpoint.ReverseConnect?.ServerUri, ct).ConfigureAwait(false);
                if (updateBeforeConnect)
                {
                    await endpoint.UpdateFromServerAsync(endpoint.EndpointUrl, connection,
                        endpoint.Description.SecurityMode, endpoint.Description.SecurityPolicyUri,
                        ct).ConfigureAwait(false);
                    updateBeforeConnect = false;
                    connection = null;
                }
            } while (connection == null);
            return await CreateAsync(configuration, connection, endpoint, false, checkDomain,
                sessionName, sessionTimeout, userIdentity, preferredLocales, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IOpcUaBrowser Browse(TimeSpan rebrowsePeriod, string subscriptionName)
        {
            return Browser.Register(this, rebrowsePeriod, subscriptionName);
        }

        /// <inheritdoc/>
        public IAsyncDisposable Sample(TimeSpan samplingRate, ReadValueId item,
            string subscriptionName, uint clientHandle)
        {
            return Sampler.Register(this, samplingRate, item, subscriptionName, clientHandle);
        }

        /// <summary>
        /// Reset the client
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal Task ResetAsync(CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var tcs = new TaskCompletionSource();
            try
            {
                ct.Register(() => tcs.TrySetCanceled());
                _logger.LogDebug("{Client}: Resetting...", this);
                TriggerConnectionEvent(ConnectionEvent.Reset, tcs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Failed to reset.", this);
                tcs.TrySetException(ex);
            }
            return tcs.Task;
        }

        /// <summary>
        /// Get session diagnostics
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task<SessionDiagnosticsModel?> GetSessionDiagnosticsAsync(
            CancellationToken ct = default)
        {
            if (_session?.Connected == true)
            {
                return await _session.GetServerDiagnosticAsync(ct).ConfigureAwait(false);
            }
            return null;
        }

        /// <summary>
        /// Close client
        /// </summary>
        /// <returns></returns>
        internal async ValueTask CloseAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            try
            {
                _disposed = true;

                _logger.LogDebug("{Client}: Closing...", this);
                await _cts.CancelAsync().ConfigureAwait(false);

                await _sessionManager.ConfigureAwait(false);
                _reconnectHandler.Dispose();

                foreach (var sampler in _samplers.Values)
                {
                    await sampler.DisposeAsync().ConfigureAwait(false);
                }

                _samplers.Clear();

                foreach (var browser in _browsers.Values)
                {
                    await browser.DisposeAsync().ConfigureAwait(false);
                }

                _browsers.Clear();

                await CloseSessionAsync().ConfigureAwait(false);

                _lastState = EndpointConnectivityState.Disconnected;

                _logger.LogInformation("{Client}: Successfully closed.", this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Failed to close.", this);
            }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <summary>
        /// Safely invoke the service call and retry if the session
        /// disconnected during call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="TimeoutException"></exception>
        internal async Task<T> RunAsync<T>(Func<ServiceCallContext, Task<T>> service,
            int? connectTimeout, int? serviceCallTimeout, CancellationToken cancellationToken)
        {
            var timeout = GetConnectCallTimeout(connectTimeout, serviceCallTimeout);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = cts.Token;
            cts.CancelAfter(timeout); // wait max timeout on the reader lock/session
            while (true)
            {
                if (_disposed)
                {
                    throw new ConnectionException($"Session {_sessionName} was closed.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var readerlock = await _lock.ReaderLockAsync(ct).ConfigureAwait(false);
                    if (_session != null)
                    {
                        if (!DisableComplexTypeLoading && !_session.IsTypeSystemLoaded)
                        {
                            // Ensure type system is loaded
                            cts.CancelAfter(timeout);
                            await _session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                        }

                        var context = new ServiceCallContext(_session, ct);
                        cts.CancelAfter(GetServiceCallTimeout(serviceCallTimeout));
                        var result = await service(context).ConfigureAwait(false);

                        //
                        // Check wether tracked and untracked token are the same. This is the case
                        // with kepserver, which uses the same token for all continuations. If it
                        // is the same, it is already tracked. If it is different, we need to untrack
                        // and track the new one
                        //
                        if (context.TrackedToken != null && context.TrackedToken != context.UntrackedToken)
                        {
                            AddRef(context.TrackedToken);
                        }
                        else if (LingerTimeout != null)
                        {
                            AddRef(_sessionName, LingerTimeout);
                        }
                        if (context.UntrackedToken != null && context.TrackedToken != context.UntrackedToken)
                        {
                            Release(context.UntrackedToken);
                        }
                        return result;
                    }
                    // We are not resetting the timeout here since we have not yet been
                    // able to obtain a session in the current timeout.
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        "Connecting to the endpoint or the request itself timed out.");
                }
                catch (Exception ex) when (!IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("{Client}: Session disconnected during service call " +
                        "with message {Message}, retrying.", this, ex.Message);

                    cts.CancelAfter(timeout); // Reset timeout again to wait again for session
                }
            }
        }

        /// <summary>
        /// Safely invoke a streaming service and retry if the session
        /// disconnected during an operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stack"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="TimeoutException"></exception>
        internal async IAsyncEnumerable<T> RunAsync<T>(
            Stack<Func<ServiceCallContext, ValueTask<IEnumerable<T>>>> stack,
            int? connectTimeout, int? serviceCallTimeout,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var timeout = GetConnectCallTimeout(connectTimeout, serviceCallTimeout);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = cts.Token;
            cts.CancelAfter(timeout); // wait max timeout on the reader lock/session
            while (stack.Count > 0)
            {
                if (_disposed)
                {
                    throw new ConnectionException($"Session {_sessionName} was closed.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (_disposed)
                {
                    throw new ConnectionException($"Session {_sessionName} was closed.");
                }
                IEnumerable<T> results;
                try
                {
                    using var readerlock = await _lock.ReaderLockAsync(ct).ConfigureAwait(false);
                    if (_session != null)
                    {
                        // Ensure type system is loaded
                        if (!DisableComplexTypeLoading && !_session.IsTypeSystemLoaded)
                        {
                            cts.CancelAfter(timeout);
                            await _session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                        }

                        var context = new ServiceCallContext(_session, ct);
                        cts.CancelAfter(GetServiceCallTimeout(serviceCallTimeout));
                        results = await stack.Peek()(context).ConfigureAwait(false);

                        // Success
                        stack.Pop();
                    }
                    else
                    {
                        // We are not resetting the timeout here since we have not yet been
                        // able to obtain a session in the current timeout.
                        continue;
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        "Connecting to the endpoint or a request operation timed out.");
                }
                catch (Exception ex) when (!IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("{Client}: Session disconnected during service call " +
                        "with message {Message}, retrying.", this, ex.Message);

                    cts.CancelAfter(timeout); // Reset timeout again to wait again for session
                    continue;
                }

                foreach (var result in results)
                {
                    yield return result;
                }
                cts.CancelAfter(timeout); // Reset timeout now to wait max timeout for session
            }
        }

        /// <summary>
        /// Connect
        /// </summary>
        /// <param name="token"></param>
        /// <param name="expiresAfter"></param>
        /// <returns></returns>
        internal void AddRef(string? token = null, TimeSpan? expiresAfter = null)
        {
            // If token provided create a registered release
            CancellationTokenSource? cts = null;
            if (token != null)
            {
                lock (_tokens)
                {
                    //
                    // Get any registered token and see if we can just re-arm
                    // the timer (when the cancellation has not been requested
                    // yet. If so we do not need to add ref at all and just
                    // re-use the existing registered token. If there is a
                    // token id collision then there is the potential that
                    // tokens get removed too early using the release(token)
                    // api. Since that is only used in the case of continuation
                    // tokens we expect that not to happen on the same session.
                    //
                    if (_tokens.TryGetValue(token, out cts))
                    {
                        cts.CancelAfter(expiresAfter ?? TimeSpan.FromSeconds(10));

                        if (!cts.IsCancellationRequested)
                        {
                            // Re-armed the current timer no need to add ref
                            return;
                        }

                        _tokens.Remove(token);
                    }

                    cts = new CancellationTokenSource();
                    _tokens.Add(token, cts);
                }
            }

            if (Interlocked.Increment(ref _refCount) == 1)
            {
                // Post connection request
                TriggerConnectionEvent(ConnectionEvent.Connect);
            }

            if (cts != null)
            {
                //
                // Now that we took a reference register callback that removes
                // registered token source under lock if it is the current one
                // and then cancel and call release. After release dispose the
                // token source to free the timer.
                //
                cts.Token.Register(() =>
                {
                    Debug.Assert(token != null, "Captured token should not be null");
                    lock (_tokens)
                    {
                        if (_tokens.TryGetValue(token, out var registered) &&
                            registered == cts)
                        {
                            _tokens.Remove(token);
                        }
                    }

                    Release();
                    cts.Dispose();
                });
                //
                // Start the cancellation token source timer now and
                // take a new reference.
                //
                cts.CancelAfter(expiresAfter ?? TimeSpan.FromSeconds(10));
            }
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        /// <param name="token"></param>
        internal void Release(string? token = null)
        {
            if (token == null)
            {
                // Decrement reference count
                if (Interlocked.Decrement(ref _refCount) == 0)
                {
                    // Post disconnect request
                    TriggerConnectionEvent(ConnectionEvent.Disconnect);
                }
            }
            else if (_tokens.TryGetValue(token, out var cts))
            {
                //
                // Cancel will callback back into here, unregister from
                // tokens cache, release the reference (above) and then
                // dispose the tracked cancellation token source.
                //
                cts.Cancel();
            }

            // No token so we either expired or never registered
            // the first is expected, the second is a bug, we cannot
            // tell the difference though.
        }

        /// <summary>
        /// Manages the underlying session state machine.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ManageSessionStateMachineAsync(CancellationToken ct)
        {
            var currentSessionState = SessionState.Disconnected;
            IReadOnlyList<IOpcUaSubscription> currentSubscriptions;
            var queuedSubscriptions = new HashSet<IOpcUaSubscription>();

            var reconnectPeriod = 0;
            var reconnectTimer = _timeProvider.CreateTimer(
                _ => TriggerConnectionEvent(ConnectionEvent.ConnectRetry), null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            currentSubscriptions = Array.Empty<IOpcUaSubscription>();
            try
            {
                await using (reconnectTimer.ConfigureAwait(false))
                {
                    try
                    {
                        await foreach (var (trigger, context) in _channel.Reader.ReadAllAsync(ct))
                        {
                            _logger.LogDebug("{Client}: Processing event {Event} in State {State}...",
                                this, trigger, currentSessionState);

                            switch (trigger)
                            {
                                case ConnectionEvent.Reset:
                                    if (currentSessionState != SessionState.Connected)
                                    {
                                        (context as TaskCompletionSource)?.TrySetResult();
                                        break;
                                    }
                                    // If currently reconnecting, dispose the reconnect handler
                                    _reconnectHandler.CancelReconnect();
                                    //
                                    // Close bypassing everything but keep channel open then trigger a
                                    // reconnect. The reconnect will recreate the session and subscriptions
                                    //
                                    Debug.Assert(_session != null);
                                    await _session.CloseAsync(false, default).ConfigureAwait(false);
                                    goto case ConnectionEvent.StartReconnect;
                                case ConnectionEvent.Connect:
                                    if (currentSessionState == SessionState.Disconnected)
                                    {
                                        // Start connecting
                                        reconnectTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                                        currentSessionState = SessionState.Connecting;
                                        reconnectPeriod = GetMinReconnectPeriod();
                                    }
                                    goto case ConnectionEvent.ConnectRetry;
                                case ConnectionEvent.ConnectRetry:
                                    switch (currentSessionState)
                                    {
                                        case SessionState.Connecting:
                                            Debug.Assert(_reconnectHandler.State == SessionReconnectHandler.ReconnectState.Ready);
                                            Debug.Assert(_disconnectLock != null);
                                            Debug.Assert(_session == null);

                                            if (!await TryConnectAsync(ct).ConfigureAwait(false))
                                            {
                                                // Reschedule connecting
                                                Debug.Assert(reconnectPeriod != 0, "Reconnect period should not be 0.");
                                                var retryDelay = TimeSpan.FromMilliseconds(
                                                    _reconnectHandler.CheckedReconnectPeriod(reconnectPeriod));
                                                _logger.LogInformation("{Client}: Retrying connecting session in {RetryDelay}...",
                                                    this, retryDelay);
                                                reconnectTimer.Change(retryDelay, Timeout.InfiniteTimeSpan);
                                                reconnectPeriod = _reconnectHandler.JitteredReconnectPeriod(reconnectPeriod);
                                                break;
                                            }

                                            Debug.Assert(_session != null);

                                            // Allow access to session now
                                            Debug.Assert(_disconnectLock != null);
                                            _disconnectLock.Dispose();
                                            _disconnectLock = null;

                                            currentSubscriptions = _session.SubscriptionHandles;
                                            //
                                            // Equality is through subscriptionidentifer therefore only subscriptions
                                            // that are not yet createdSubscriptions inside the session remain in queued state.
                                            //
                                            queuedSubscriptions.ExceptWith(currentSubscriptions);
                                            await ApplySubscriptionAsync(currentSubscriptions, queuedSubscriptions,
                                                ct).ConfigureAwait(false);

                                            currentSessionState = SessionState.Connected;
                                            currentSubscriptions.ForEach(h => h.NotifySessionConnectionState(false));
                                            break;
                                        case SessionState.Disconnected:
                                        case SessionState.Connected:
                                            // Nothing to do, already disconnected or connected
                                            break;
                                        case SessionState.Reconnecting:
                                            Debug.Fail("Should not be connecting during reconnecting.");
                                            break;
                                    }
                                    break;

                                case ConnectionEvent.SubscriptionManage:
                                    var item = context as IOpcUaSubscription;
                                    Debug.Assert(item != null);
                                    switch (currentSessionState)
                                    {
                                        case SessionState.Connected:
                                            queuedSubscriptions.Remove(item);
                                            await ApplySubscriptionAsync(new[] { item }, queuedSubscriptions,
                                                cancellationToken: ct).ConfigureAwait(false);
                                            break;
                                        case SessionState.Disconnected:
                                            break;
                                        default:
                                            queuedSubscriptions.Add(item);
                                            break;
                                    }
                                    break;

                                case ConnectionEvent.SubscriptionClose:
                                    var sub = context as IOpcUaSubscription;
                                    Debug.Assert(sub != null);
                                    queuedSubscriptions.Remove(sub);
                                    await sub.CloseInSessionAsync(_session, ct).ConfigureAwait(false);
                                    break;

                                case ConnectionEvent.StartReconnect: // sent by the keep alive timeout path
                                    switch (currentSessionState)
                                    {
                                        case SessionState.Connected: // only valid when connected.
                                            Debug.Assert(_reconnectHandler.State == SessionReconnectHandler.ReconnectState.Ready);

                                            // Ensure no more access to the session through reader locks
                                            Debug.Assert(_disconnectLock == null);
                                            _disconnectLock = await _lock.WriterLockAsync(ct);

                                            _logger.LogInformation("{Client}: Reconnecting session {Session} due to {Reason}...",
                                                this, _sessionName, (context is ServiceResult sr) ? "error " + sr : "RESET");
                                            var state = _reconnectHandler.BeginReconnect(_session,
                                                _reverseConnectManager, GetMinReconnectPeriod(), (sender, evt) =>
                                                {
                                                    if (ReferenceEquals(sender, _reconnectHandler))
                                                    {
                                                        TriggerConnectionEvent(ConnectionEvent.ReconnectComplete,
                                                            _reconnectHandler.Session);
                                                    }
                                                });

                                            // Save session while reconnecting.
                                            Debug.Assert(_reconnectingSession == null);
                                            _reconnectingSession = _session;
                                            _session = null;
                                            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);
                                            currentSessionState = SessionState.Reconnecting;
                                            _reconnectingSession?.SubscriptionHandles
                                                .ForEach(h => h.NotifySessionConnectionState(true));
                                            (context as TaskCompletionSource)?.TrySetResult();
                                            break;
                                        case SessionState.Connecting:
                                        case SessionState.Disconnected:
                                        case SessionState.Reconnecting:
                                            // Nothing to do in this state
                                            break;
                                    }
                                    break;

                                case ConnectionEvent.ReconnectComplete:
                                    // if session recovered, Session property is not null
                                    var reconnected = _reconnectHandler.Session;
                                    switch (currentSessionState)
                                    {
                                        case SessionState.Reconnecting:
                                            //
                                            // Behavior of the reconnect handler is as follows:
                                            // 1) newSession == null
                                            //  => then the old session is still good, we missed keep alive.
                                            // 2) newSession != null but equal to previous session
                                            //  => new channel was opened but the existing session was reactivated
                                            // 3) newSession != previous Session
                                            //  => everything reconnected and new session was activated.
                                            //
                                            reconnected ??= _reconnectingSession;

                                            Debug.Assert(reconnected != null, "reconnected should never be null");
                                            Debug.Assert(reconnected.Connected, "reconnected should always be connected");

                                            // Handles all 3 cases above.
                                            var isNew = await UpdateSessionAsync(reconnected).ConfigureAwait(false);

                                            Debug.Assert(_session != null);
                                            Debug.Assert(_reconnectingSession == null);
                                            if (!isNew)
                                            {
                                                // Case 1) and 2)
                                                _logger.LogInformation("{Client}: Client RECOVERED!", this);
                                            }
                                            else
                                            {
                                                // Case 3)
                                                _logger.LogInformation("{Client}: Client RECONNECTED!", this);
                                                _numberOfConnectRetries++;
                                            }

                                            // If not already ready, signal we are ready again and ...
                                            NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
                                            // ... allow access to the client again
                                            Debug.Assert(_disconnectLock != null);
                                            _disconnectLock.Dispose();
                                            _disconnectLock = null;

                                            currentSubscriptions = _session.SubscriptionHandles;
                                            //
                                            // Equality is through subscriptionidentifer therefore only subscriptions
                                            // that are not yet createdSubscriptions inside the session remain in queued state.
                                            //
                                            queuedSubscriptions.ExceptWith(currentSubscriptions);
                                            await ApplySubscriptionAsync(currentSubscriptions, queuedSubscriptions,
                                                ct).ConfigureAwait(false);

                                            _reconnectRequired = 0;
                                            reconnectPeriod = GetMinReconnectPeriod();
                                            currentSessionState = SessionState.Connected;
                                            currentSubscriptions.ForEach(h => h.NotifySessionConnectionState(false));
                                            break;

                                        case SessionState.Connected:
                                            Debug.Fail("Should not signal reconnected when already connected.");
                                            break;
                                        case SessionState.Connecting:
                                        case SessionState.Disconnected:
                                            Debug.Assert(_reconnectingSession == null);
                                            reconnected?.Dispose();
                                            break;
                                    }
                                    break;

                                case ConnectionEvent.Disconnect:

                                    // If currently reconnecting, dispose the reconnect handler and stop timer
                                    _reconnectHandler.CancelReconnect();
                                    reconnectTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                                    queuedSubscriptions.Clear();
                                    currentSubscriptions = Array.Empty<IOpcUaSubscription>();

                                    // if not already disconnected, aquire writer lock
                                    _disconnectLock ??= await _lock.WriterLockAsync(ct);

                                    _numberOfConnectRetries = 0;
                                    reconnectPeriod = 0;
                                    if (_session != null)
                                    {
                                        try
                                        {
                                            await _session.CloseAsync(ct).ConfigureAwait(false);
                                        }
                                        catch (Exception ex) when (ex is not OperationCanceledException)
                                        {
                                            _logger.LogError(ex, "{Client}: Failed to close session {Name}.",
                                                this, _sessionName);
                                        }
                                    }

                                    NotifyConnectivityStateChange(EndpointConnectivityState.Disconnected);
                                    _session?.SubscriptionHandles
                                        .ForEach(h => h.NotifySessionConnectionState(true));

                                    // Clean up
                                    await CloseSessionAsync().ConfigureAwait(false);
                                    Debug.Assert(_session == null);

                                    currentSessionState = SessionState.Disconnected;
                                    break;
                            }

                            _logger.LogDebug("{Client}: Event {Event} in State {State} processed.", trigger,
                                this, currentSessionState);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{Client}: Connection manager exited unexpectedly...", this);
                    }
                    finally
                    {
                        _reconnectHandler.CancelReconnect();
                    }
                }
            }
            finally
            {
                foreach (var queuedSubscription in queuedSubscriptions)
                {
                    await queuedSubscription.CloseInSessionAsync(_session, ct).ConfigureAwait(false);
                    (queuedSubscription as IDisposable)?.Dispose();
                }
                _logger.LogDebug("{Client}: Exiting client management loop.", this);
            }

            async ValueTask ApplySubscriptionAsync(IReadOnlyList<IOpcUaSubscription> subscriptions,
                HashSet<IOpcUaSubscription> extra, CancellationToken cancellationToken = default)
            {
                var numberOfSubscriptions = subscriptions.Count + extra.Count;
                _logger.LogDebug("{Client}: Applying changes to {Count} subscriptions...",
                    this, numberOfSubscriptions);
                var sw = Stopwatch.StartNew();

                var session = _session;
                Debug.Assert(session != null, "Session is null");

                try
                {
                    // Reload namespace tables should they have changed...
                    var oldTable = session.NamespaceUris.ToArray();
                    await session.FetchNamespaceTablesAsync(ct).ConfigureAwait(false);
                    var newTable = session.NamespaceUris.ToArray();
                    LogNamespaceTableChanges(oldTable, newTable);
                }
                catch (ServiceResultException sre)
                {
                    _logger.LogWarning(sre, "{Client}: Failed to fetch namespace table...", this);
                }

                if (!DisableComplexTypeLoading && !session.IsTypeSystemLoaded)
                {
                    // Ensure type system is loaded
                    await session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                }

                await Task.WhenAll(subscriptions.Concat(extra).Select(async subscription =>
                {
                    try
                    {
                        await subscription.SyncWithSessionAsync(session, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "{Client}: Failed to apply subscription {Subscription} to session.",
                            this, subscription);
                    }
                })).ConfigureAwait(false);

                UpdatePublishRequestCounts();

                if (numberOfSubscriptions > 1)
                {
                    // Clear the node cache - TODO: we should have a real node cache here
                    session?.NodeCache.Clear();

                    _logger.LogInformation(
                        "{Client}: Applying changes to {Count} subscription(s) took {Duration}.",
                        this, numberOfSubscriptions, sw.Elapsed);
                }
            }

            int GetMinReconnectPeriod()
            {
                var reconnectPeriod = MinReconnectDelay ?? TimeSpan.Zero;
                if (reconnectPeriod == TimeSpan.Zero)
                {
                    reconnectPeriod = TimeSpan.FromSeconds(1);
                }
                if (reconnectPeriod > _maxReconnectPeriod)
                {
                    reconnectPeriod = _maxReconnectPeriod;
                }
                return (int)reconnectPeriod.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Log namespace table changes
        /// </summary>
        /// <param name="oldTable"></param>
        /// <param name="newTable"></param>
        private void LogNamespaceTableChanges(string[] oldTable, string[] newTable)
        {
            var tableChanged = false;
            for (var i = 0; i < Math.Max(oldTable.Length, newTable.Length); i++)
            {
                if (i < oldTable.Length && i < newTable.Length)
                {
                    if (oldTable[i] == newTable[i])
                    {
                        continue;
                    }
                    tableChanged = true;
                    _logger.LogWarning(
                        "{Client}: Namespace index #{Index} changed from {Old} to {New}",
                        this, i, oldTable[i], newTable[i]);
                }
                else if (i < oldTable.Length)
                {
                    tableChanged = true;
                    _logger.LogWarning(
                        "{Client}: Namespace index #{Index} removed {Old}",
                        this, i, oldTable[i]);
                }
                else
                {
                    tableChanged = true;
                    _logger.LogWarning(
                        "{Client}: Namespace index #{Index} added {New}",
                        this, i, newTable[i]);
                }
            }
            if (tableChanged)
            {
                Interlocked.Increment(ref _namespaceTableChanges);
            }
        }

        private const int kMinPublishRequestCount = 2;
        private const int kMaxPublishRequestCount = 10;
        private const int kPublishTimeoutsMultiplier = 3;

        /// <summary>
        /// Ensure min publish requests are configured correctly
        /// </summary>
        private void UpdatePublishRequestCounts()
        {
            var session = _session;
            if (session == null)
            {
                return;
            }

            var minPublishRequests = MinPublishRequests ?? 0;
            if (minPublishRequests <= 0)
            {
                minPublishRequests = kMinPublishRequestCount;
            }

            var maxPublishRequests = MaxPublishRequests ?? kMaxPublishRequestCount;
            if (maxPublishRequests <= 0 || maxPublishRequests > ushort.MaxValue)
            {
                maxPublishRequests = ushort.MaxValue;
            }

            var createdSubscriptions = SubscriptionCount;
            if (PublishRequestsPerSubscriptionPercent.HasValue)
            {
                var percentage = PublishRequestsPerSubscriptionPercent ?? 100;
                var minPublishOverride = percentage == 100 || percentage <= 0 ?
                    createdSubscriptions :
                    (int)Math.Ceiling(createdSubscriptions * (percentage / 100.0));
                if (minPublishRequests < minPublishOverride)
                {
                    minPublishRequests = minPublishOverride;
                }
            }

            Debug.Assert(minPublishRequests > 0);
            Debug.Assert(maxPublishRequests > 0);

            if (minPublishRequests > maxPublishRequests)
            {
                // Dont allow min to be higher than max
                minPublishRequests = maxPublishRequests;
            }

            //
            // The stack will choose a value based on the subscription
            // count that is between min and max.
            //
            session.MinPublishRequestCount = minPublishRequests;
            session.MaxPublishRequestCount = maxPublishRequests;

            if (createdSubscriptions > 0 && minPublishRequests > OutstandingRequestCount)
            {
                session.StartPublishing(session.OperationTimeout, false);
            }
        }

        /// <summary>
        /// Connect client
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<bool> TryConnectAsync(CancellationToken ct)
        {
            var timeout = CreateSessionTimeout ?? TimeSpan.FromSeconds(10);

            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);
            Debug.Assert(_connection.Endpoint != null);

            _logger.LogInformation("{Client}: Connecting to {EndpointUrl}...",
                this, _connection.Endpoint.Url);
            var attempt = 0;
            foreach (var nextUrl in _connection.GetEndpointUrls())
            {
                var endpointUrl = nextUrl;

                // Ensure any previous session is disposed here.
                await CloseSessionAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                try
                {
                    ITransportWaitingConnection? connection = null;
                    if (_reverseConnectManager != null)
                    {
                        connection = await _reverseConnectManager.WaitForConnection(
                            endpointUrl, null, ct).ConfigureAwait(false);
                    }

                    //
                    // Get the endpoint by connecting to server's discovery endpoint.
                    // Try to find the first endpoint with security.
                    //
                    var securityMode = _connection.Endpoint.SecurityMode ?? SecurityMode.NotNone;
                    var securityProfile = _connection.Endpoint.SecurityPolicy;

                    var endpointDescription = await SelectEndpointAsync(endpointUrl,
                        connection, securityMode, securityProfile).ConfigureAwait(false);
                    if (endpointDescription == null)
                    {
                        _logger.LogWarning(
                            "{Client}: No endpoint found that matches connection of session {Name}.",
                            this, _sessionName);
                        continue;
                    }

                    endpointUrl = Utils.ParseUri(endpointDescription.EndpointUrl);
                    var endpointConfiguration = EndpointConfiguration.Create(
                        _configuration);
                    endpointConfiguration.OperationTimeout =
                        (int)timeout.TotalMilliseconds;
                    var endpoint = new ConfiguredEndpoint(null, endpointDescription,
                        endpointConfiguration);

                    var credential = _connection.User;
                    if (securityMode == SecurityMode.Best &&
                        endpointDescription.SecurityMode == MessageSecurityMode.None)
                    {
                        _logger.LogWarning("{Client}: Although the use of best security was " +
                            "configured, there was no security-enabled endpoint available at " +
                            "url {EndpointUrl}. An endpoint with no security will be used " +
                            "for session {Name} but no credentials will be sent over it.",
                            this, endpointUrl, _sessionName);

                        credential = null;
                    }

                    var userIdentity = await credential.ToUserIdentityAsync(
                        _configuration).ConfigureAwait(false);

                    var identityPolicy = endpoint.Description.FindUserTokenPolicy(
                        userIdentity.TokenType, userIdentity.IssuedTokenType);
                    if (identityPolicy == null)
                    {
                        _logger.LogWarning(
                            "{Client}: No UserTokenPolicy for {TokenType}/{IssuedTokenType} " +
                            "found on endpoint {EndpointUrl} (session: {Name}).",
                            this, userIdentity.TokenType, userIdentity.IssuedTokenType,
                            endpointUrl, _sessionName);
                        continue;
                    }
                    _logger.LogInformation(
                        "#{Attempt} - {Client}: Creating session {Name} with endpoint {EndpointUrl}...",
                        ++attempt, this, _sessionName, endpointUrl);

                    var preferredLocales = _connection.Locales?.ToList() ?? new List<string>();
                    if (preferredLocales.Count == 0)
                    {
                        // Create the session with english as default
                        preferredLocales.Add("en-US");
                        if (CultureInfo.CurrentCulture.Name != preferredLocales[0])
                        {
                            // and current language locale as backup
                            preferredLocales.Add(CultureInfo.CurrentCulture.Name);
                        }
                    }
                    var sessionTimeout = SessionTimeout ?? TimeSpan.FromSeconds(30);
                    var session = await CreateAsync(_configuration,
                        _reverseConnectManager, endpoint,
                        // Update endpoint through discovery
                        updateBeforeConnect: _reverseConnectManager != null,
                        checkDomain: false, // Domain must match on connect
                        _sessionName, (uint)sessionTimeout.TotalMilliseconds,
                        userIdentity, preferredLocales, ct).ConfigureAwait(false);

                    session.RenewUserIdentity += (_, _) => userIdentity;

                    // Assign the createdSubscriptions session
                    var isNew = await UpdateSessionAsync(session).ConfigureAwait(false);
                    Debug.Assert(isNew);
                    _logger.LogInformation(
                        "{Client}: New Session {Name} created with endpoint {EndpointUrl} ({Original}).",
                        this, _sessionName, endpointUrl, _connection.Endpoint.Url);

                    _logger.LogInformation("Client {Client} CONNECTED to {EndpointUrl}!",
                        this, endpointUrl);
                    return true;
                }
                catch (Exception ex)
                {
                    NotifyConnectivityStateChange(ToConnectivityState(ex));
                    _numberOfConnectRetries++;
                    _logger.LogInformation(
                        "#{Attempt} - {Client}: Failed to connect to {EndpointUrl}: {Message}...",
                        ++attempt, this, endpointUrl, ex.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Handle publish errors
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
#pragma warning disable IDE0060 // Remove unused parameter
        internal void Session_HandlePublishError(ISession session, PublishErrorEventArgs e)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (session.Connected)
            {
                _logger.LogInformation("{Client}: Publish error: {Error} (Actively handled: {Active})...",
                    this, e.Status, ActivePublishErrorHandling);
            }
            else
            {
                _logger.LogDebug(
                    "{Client}: Disconnected - publish error: {Error} (Actively handled: {Active})...",
                    this, e.Status, ActivePublishErrorHandling);
            }

            if (!ActivePublishErrorHandling)
            {
                return;
            }

            switch (e.Status.Code)
            {
                case StatusCodes.BadSessionIdInvalid:
                case StatusCodes.BadSecureChannelClosed:
                case StatusCodes.BadSessionClosed:
                case StatusCodes.BadConnectionClosed:
                case StatusCodes.BadNoCommunication:
                    TriggerReconnect(e.Status);
                    break;
                case StatusCodes.BadRequestTimeout:
                case StatusCodes.BadTimeout:
                    var threshold = MinPublishRequestCount * kPublishTimeoutsMultiplier;
                    if (Interlocked.Increment(ref _publishTimeoutCounter) > threshold)
                    {
                        _logger.LogError(
                            "{Client}: {Count} Timeouts (> {Threshold}) during publishing. Reconnecting...",
                            this, _publishTimeoutCounter, threshold);
                        TriggerReconnect(e.Status);
                    }
                    return;
            }
            // Reset timeout counter - we only care about subsequent timeouts
            _publishTimeoutCounter = 0;
        }

        /// <summary>
        /// Feed back acknoledgements
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        internal void Session_PublishSequenceNumbersToAcknowledge(ISession session,
            PublishSequenceNumbersToAcknowledgeEventArgs e)
        {
            // Reset timeout counter
            _publishTimeoutCounter = 0;

            var acks = e.AcknowledgementsToSend
                .Concat(e.DeferredAcknowledgementsToSend)
                .ToHashSet();
            if (acks.Count == 0)
            {
                return;
            }
            e.AcknowledgementsToSend.Clear();
            e.DeferredAcknowledgementsToSend.Clear();

            foreach (var subscription in ((OpcUaSession)session).SubscriptionHandles)
            {
                if (!subscription.TryGetCurrentPosition(out var sid, out var seq))
                {
                    // No deferrals
                    e.AcknowledgementsToSend.AddRange(acks.Where(a => a.SubscriptionId == sid));
                }
                else
                {
                    // Ack all messages before this one for the subscriptoin
                    e.AcknowledgementsToSend.AddRange(acks.Where(a => a.SubscriptionId == sid
                        && (int)a.SequenceNumber <= (int)seq));
                }
            }
            e.DeferredAcknowledgementsToSend.AddRange(acks.Except(e.AcknowledgementsToSend));

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogTrace(
                    "{Client}: #{ThreadId} - Sending {Acks} acks and deferring {Deferrals} acks. ({Requests})",
                    this, Environment.CurrentManagedThreadId, ToString(e.AcknowledgementsToSend),
                    ToString(e.DeferredAcknowledgementsToSend), session.GoodPublishRequestCount);
                static string ToString(SubscriptionAcknowledgementCollection acks)
                {
                    return acks.Count == 0 ? "no" : acks
                        .OrderBy(a => a.SubscriptionId)
                        .Select(a => $"{a.SubscriptionId}:{a.SequenceNumber}")
                        .Aggregate((a, b) => $"{a}, {b}");
                }
            }
        }

        /// <summary>
        /// Handles a keep alive event from a session and triggers a reconnect
        /// if necessary.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        internal void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                // check for events from discarded sessions.
                if (!ReferenceEquals(session, _session))
                {
                    return;
                }

                // start reconnect sequence on communication error.
                Interlocked.Increment(ref _keepAliveCounter);
                if (ServiceResult.IsBad(e.Status))
                {
                    _keepAliveCounter = 0;
                    TriggerReconnect(e.Status);

                    _logger.LogInformation(
                        "{Client}: Got Keep Alive error: {Error} ({TimeStamp}:{ServerState}",
                        this, e.Status, e.CurrentTime, e.Status);
                }
                else if (SubscriptionCount > 0 && GoodPublishRequestCount == 0)
                {
                    UpdatePublishRequestCounts();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Error in OnKeepAlive.", this);
            }
        }

        /// <summary>
        /// Trigger reconnect
        /// </summary>
        /// <param name="sr"></param>
        void TriggerReconnect(ServiceResult sr)
        {
            if (Interlocked.Increment(ref _reconnectRequired) == 1)
            {
                // Ensure we reconnect
                TriggerConnectionEvent(ConnectionEvent.StartReconnect, sr);
            }
        }

        /// <summary>
        /// Trigger connection event
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="context"></param>
        private void TriggerConnectionEvent(ConnectionEvent evt, object? context = null)
        {
            _channel.Writer.TryWrite((evt, context));
        }

        /// <summary>
        /// Update session state
        /// </summary>
        /// <param name="session"></param>
        private async ValueTask<bool> UpdateSessionAsync(ISession session)
        {
            _publishTimeoutCounter = 0;
            Debug.Assert(session is OpcUaSession);

            if (_session == null)
            {
                _session = _reconnectingSession;
                _reconnectingSession = null;
            }

            var oldTable = _session?.NamespaceUris.ToArray();
            Debug.Assert(_reconnectingSession == null);
            var isNewSession = false;
            if (!ReferenceEquals(_session, session))
            {
                await CloseSessionAsync().ConfigureAwait(false);
                _session = (OpcUaSession)session;
                isNewSession = true;
                kSessions.Add(1, _metrics.TagList);
            }

            UpdatePublishRequestCounts();
            NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
            UpdateNamespaceTableAndSessionDiagnostics(_session, oldTable);
            return isNewSession;

            void UpdateNamespaceTableAndSessionDiagnostics(OpcUaSession session,
                string[]? oldTable)
            {
                if (oldTable != null)
                {
                    var newTable = session.NamespaceUris.ToArray();
                    LogNamespaceTableChanges(oldTable, newTable);
                }

                lock (_channelLock)
                {
                    UpdateConnectionDiagnosticFromSession(session);
                }
            }
        }

        /// <summary>
        /// Update diagnostic if the channel has changed
        /// </summary>
        private void OnUpdateConnectionDiagnostics()
        {
            if (_session != null)
            {
                lock (_channelLock)
                {
                    UpdateConnectionDiagnosticFromSession(_session);
                }
            }
        }

        /// <summary>
        /// Update session diagnostics
        /// </summary>
        /// <param name="session"></param>
        private void UpdateConnectionDiagnosticFromSession(OpcUaSession session)
        {
            // Called under lock

            var channel = session.TransportChannel;
            var token = channel?.CurrentToken;

            var now = _timeProvider.GetUtcNow();

            var lastDiagnostics = _lastDiagnostics;
            var elapsed = now - lastDiagnostics.TimeStamp;

            var channelChanged = false;
            if (token != null)
            {
                //
                // Monitor channel's token lifetime and update diagnostics
                // Check wether the token or channel changed. If so set a
                // timer to monitor the new token lifetime, if not then
                // try again after the remaining lifetime or every second
                // until it changed unless the token is then later gone.
                //
                channelChanged = !(lastDiagnostics != null &&
                    lastDiagnostics.ChannelId == token.ChannelId &&
                    lastDiagnostics.TokenId == token.TokenId &&
                    lastDiagnostics.CreatedAt == token.CreatedAt);

                var lifetime = TimeSpan.FromMilliseconds(token.Lifetime);
                if (channelChanged)
                {
                    _channelMonitor.Change(lifetime, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    //
                    // Token has not yet been updated, let's retry later
                    // It is also assumed that the port/ip are still the same
                    //
                    if (lifetime > elapsed)
                    {
                        _channelMonitor.Change(lifetime - elapsed,
                            Timeout.InfiniteTimeSpan);
                    }
                    else
                    {
                        _channelMonitor.Change(TimeSpan.FromSeconds(1),
                            Timeout.InfiniteTimeSpan);
                    }
                }
            }

            var sessionId = session.SessionId?.AsString(session.MessageContext,
                NamespaceFormat.Index);

            // Get effective ip address and port
            var socket = (channel as UaSCUaBinaryTransportChannel)?.Socket;
            var remoteIpAddress = socket?.RemoteEndpoint?.GetIPAddress()?.ToString();
            var remotePort = socket?.RemoteEndpoint?.GetPort();
            var localIpAddress = socket?.LocalEndpoint?.GetIPAddress()?.ToString();
            var localPort = socket?.LocalEndpoint?.GetPort();

            if (_lastDiagnostics.SessionCreated == session.CreatedAt &&
                _lastDiagnostics.SessionId == sessionId &&
                _lastDiagnostics.RemoteIpAddress == remoteIpAddress &&
                _lastDiagnostics.RemotePort == remotePort &&
                _lastDiagnostics.LocalIpAddress == localIpAddress &&
                _lastDiagnostics.LocalPort == localPort &&
                !channelChanged)
            {
                return;
            }

            _lastDiagnostics = new ChannelDiagnosticModel
            {
                Connection = _connection,
                TimeStamp = now,
                SessionCreated = session.CreatedAt,
                SessionId = sessionId,
                RemoteIpAddress = remoteIpAddress,
                RemotePort = remotePort == -1 ? null : remotePort,
                LocalIpAddress = localIpAddress,
                LocalPort = localPort == -1 ? null : localPort,
                ChannelId = token?.ChannelId,
                TokenId = token?.TokenId,
                CreatedAt = token?.CreatedAt,
                Lifetime = token == null ? null :
                    TimeSpan.FromMilliseconds(token.Lifetime),
                Client = ToChannelKey(token?.ClientInitializationVector,
                    token?.ClientEncryptingKey, token?.ClientSigningKey),
                Server = ToChannelKey(token?.ServerInitializationVector,
                    token?.ServerEncryptingKey, token?.ServerSigningKey)
            };
            _diagnosticsCb(_lastDiagnostics);
            _logger.LogDebug("{Client}: Diagnostics information updated.", this);

            static ChannelKeyModel? ToChannelKey(byte[]? iv, byte[]? key, byte[]? sk)
            {
                if (iv == null || key == null || sk == null ||
                    iv.Length == 0 || key.Length == 0 || sk.Length == 0)
                {
                    return null;
                }
                return new ChannelKeyModel
                {
                    Iv = iv,
                    Key = key,
                    SigLen = sk.Length
                };
            }
        }

        /// <summary>
        /// Unset and dispose existing session
        /// </summary>
        private async ValueTask CloseSessionAsync()
        {
            if (_reconnectingSession != null)
            {
                await DisposeAsync(_reconnectingSession).ConfigureAwait(false);
                _reconnectingSession = null;
            }

            if (_session != null)
            {
                await DisposeAsync(_session).ConfigureAwait(false);
                _session = null;
            }

            _publishTimeoutCounter = 0;

            async ValueTask DisposeAsync(OpcUaSession session)
            {
                try
                {
                    await session.CloseAsync(CancellationToken.None).ConfigureAwait(false);

                    _logger.LogDebug("{Client}: Successfully closed session {Session}.",
                        this, session);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Client}: Failed to close session {Session}.",
                        this, session);
                }
                finally
                {
                    session.Dispose();
                    kSessions.Add(-1, _metrics.TagList);
                }
                Debug.Assert(session.SubscriptionCount == 0);
            }
        }

        /// <summary>
        /// Notify about new connectivity state using any status callback registered.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private void NotifyConnectivityStateChange(EndpointConnectivityState state)
        {
            var previous = _lastState;
            if (previous == state)
            {
                return;
            }
            if (previous != EndpointConnectivityState.Connecting &&
                previous != EndpointConnectivityState.Ready &&
                state == EndpointConnectivityState.Error)
            {
                // Do not change state to generic error once we have
                // a specific error state already set...
                _logger.LogDebug(
                    "{Client}: Error, connection to {Endpoint} - leaving state at {Previous}.",
                    this, _connection.Endpoint!.Url, previous);
                return;
            }
            _lastState = state;

            if (state == EndpointConnectivityState.Ready)
            {
                lock (_browsers)
                {
                    foreach (var browser in _browsers.Values)
                    {
                        browser.OnConnected();
                    }
                }
            }

            _logger.LogInformation(
                "{Client}: Session {Name} with {Endpoint} changed from {Previous} to {State}",
                this, _sessionName, _connection.Endpoint!.Url, previous, state);
            try
            {
                _notifier?.Invoke(this, new EndpointConnectivityStateEventArgs(state));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Exception during state callback", this);
            }
        }

        /// <summary>
        /// Select the endpoint to use
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <param name="connection"></param>
        /// <param name="securityMode"></param>
        /// <param name="securityPolicy"></param>
        /// <param name="endpointUrl"></param>
        /// <returns></returns>
        internal async Task<EndpointDescription?> SelectEndpointAsync(Uri? discoveryUrl,
            ITransportWaitingConnection? connection, SecurityMode securityMode,
            string? securityPolicy, string? endpointUrl = null)
        {
            var endpointConfiguration = EndpointConfiguration.Create();
            endpointConfiguration.OperationTimeout =
                (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

            // needs to add the /discovery onto http urls
            if (connection == null)
            {
                if (discoveryUrl == null)
                {
                    return null;
                }
                if (discoveryUrl.Scheme == Utils.UriSchemeHttp &&
                    !discoveryUrl.AbsolutePath.EndsWith("/discovery",
                        StringComparison.OrdinalIgnoreCase))
                {
                    discoveryUrl = new UriBuilder(discoveryUrl)
                    {
                        Path = discoveryUrl.AbsolutePath.TrimEnd('/') + "/discovery"
                    }.Uri;
                }
            }

            using (var client = connection != null ?
                DiscoveryClient.Create(_configuration, connection, endpointConfiguration) :
                DiscoveryClient.Create(_configuration, discoveryUrl, endpointConfiguration))
            {
                var uri = new Uri(endpointUrl ?? client.Endpoint.EndpointUrl);
                var endpoints = await client.GetEndpointsAsync(null).ConfigureAwait(false);
                discoveryUrl ??= uri;

                _logger.LogInformation("{Client}: Discovery endpoint {DiscoveryUrl} returned endpoints. " +
                    "Selecting endpoint {EndpointUri} with SecurityMode " +
                    "{SecurityMode} and {SecurityPolicy} SecurityPolicyUri from:\n{Endpoints}",
                    this, discoveryUrl, uri, securityMode, securityPolicy ?? "any", endpoints.Select(
                        ep => "      " + ToString(ep)).Aggregate((a, b) => $"{a}\n{b}"));

                var filtered = endpoints
                    .Where(ep =>
                        SecurityPolicies.GetDisplayName(ep.SecurityPolicyUri) != null &&
                        ep.SecurityMode.IsSame(securityMode) &&
                        (securityPolicy == null || string.Equals(ep.SecurityPolicyUri,
                            securityPolicy, StringComparison.OrdinalIgnoreCase)))
                    //
                    // The security level is a relative measure assigned by the server
                    // to the endpoints that it returns. Clients should always pick the
                    // highest level unless they have a reason not too. Some servers
                    // however, mess this up a bit. So group SecurityLevel also by
                    // security mode and then pick the highest in that group.
                    //
                    .OrderByDescending(ep => ((int)ep.SecurityMode << 8) | ep.SecurityLevel)
                    .ToList();

                //
                // Try to find endpoint that matches scheme and endpoint url path
                // but fall back to match just the scheme. We need to match only
                // scheme to support the reverse connect (indicated by connection
                // being not null here).
                //
                var selected = filtered.Find(ep => Match(ep, uri, true, true))
                            ?? filtered.Find(ep => Match(ep, uri, true, false));
                if (connection != null)
                {
                    //
                    // Only allow same uri scheme (which must also be opc.tcp)
                    // for when reverse connection is used.
                    //
                    if (selected != null)
                    {
                        _logger.LogInformation(
                            "{Client}: Endpoint {Endpoint} selected via reverse connect!",
                            this, ToString(selected));
                    }
                    return selected;
                }

                if (selected == null)
                {
                    //
                    // Fall back to first supported endpoint matching absolute path
                    // then fall back to first endpoint (backwards compatibilty)
                    //
                    selected = filtered.Find(ep => Match(ep, uri, false, true))
                            ?? filtered.Find(ep => Match(ep, uri, false, false));

                    if (selected == null)
                    {
                        return null;
                    }
                }

                //
                // Adjust the host name and port to the host name and port
                // that was use to successfully connect the discovery client
                //
                var selectedUrl = Utils.ParseUri(selected.EndpointUrl);
                if (selectedUrl != null && discoveryUrl != null &&
                    selectedUrl.Scheme == discoveryUrl.Scheme)
                {
                    selected.EndpointUrl = new UriBuilder(selectedUrl)
                    {
                        Host = discoveryUrl.DnsSafeHost,
                        Port = discoveryUrl.Port
                    }.ToString();
                }

                _logger.LogInformation("{Client}: Endpoint {Endpoint} selected!", this,
                    ToString(selected));
                return selected;

                static string ToString(EndpointDescription ep) =>
    $"#{ep.SecurityLevel:000}: {ep.EndpointUrl}|{ep.SecurityMode} [{ep.SecurityPolicyUri}]";
                // Match endpoint returned against desired endpoint url
                static bool Match(EndpointDescription endpointDescription,
                    Uri endpointUrl, bool includeScheme, bool includePath)
                {
                    var url = Utils.ParseUri(endpointDescription.EndpointUrl);
                    return url != null &&
                        (!includeScheme || string.Equals(url.Scheme,
                            endpointUrl.Scheme, StringComparison.OrdinalIgnoreCase)) &&
                        (!includePath || string.Equals(url.AbsolutePath,
                            endpointUrl.AbsolutePath, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        /// <summary>
        /// Convert exception to connectivity state
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="reconnecting"></param>
        /// <returns></returns>
        public EndpointConnectivityState ToConnectivityState(Exception ex, bool reconnecting = true)
        {
            EndpointConnectivityState state;
            switch (ex)
            {
                case ServiceResultException sre:
                    switch (sre.StatusCode)
                    {
                        case StatusCodes.BadNoContinuationPoints:
                        case StatusCodes.BadLicenseLimitsExceeded:
                        case StatusCodes.BadTcpServerTooBusy:
                        case StatusCodes.BadTooManySessions:
                        case StatusCodes.BadTooManyOperations:
                            state = EndpointConnectivityState.Busy;
                            break;
                        case StatusCodes.BadCertificateRevocationUnknown:
                        case StatusCodes.BadCertificateIssuerRevocationUnknown:
                        case StatusCodes.BadCertificateRevoked:
                        case StatusCodes.BadCertificateIssuerRevoked:
                        case StatusCodes.BadCertificateChainIncomplete:
                        case StatusCodes.BadCertificateIssuerUseNotAllowed:
                        case StatusCodes.BadCertificateUseNotAllowed:
                        case StatusCodes.BadCertificateUriInvalid:
                        case StatusCodes.BadCertificateTimeInvalid:
                        case StatusCodes.BadCertificateIssuerTimeInvalid:
                        case StatusCodes.BadCertificateInvalid:
                        case StatusCodes.BadCertificateHostNameInvalid:
                        case StatusCodes.BadNoValidCertificates:
                            state = EndpointConnectivityState.CertificateInvalid;
                            break;
                        case StatusCodes.BadCertificateUntrusted:
                        case StatusCodes.BadSecurityChecksFailed:
                            state = EndpointConnectivityState.NoTrust;
                            break;
                        case StatusCodes.BadSecureChannelClosed:
                            state = reconnecting ? EndpointConnectivityState.NoTrust :
                                EndpointConnectivityState.Error;
                            break;
                        case StatusCodes.BadRequestTimeout:
                        case StatusCodes.BadNotConnected:
                            state = EndpointConnectivityState.NotReachable;
                            break;
                        case StatusCodes.BadUserAccessDenied:
                        case StatusCodes.BadUserSignatureInvalid:
                            state = EndpointConnectivityState.Unauthorized;
                            break;
                        default:
                            state = EndpointConnectivityState.Error;
                            break;
                    }
                    _logger.LogDebug("{Client}: {Result} => {State}", this, sre.Result, state);
                    break;
                default:
                    state = EndpointConnectivityState.Error;
                    _logger.LogDebug("{Client}: {Message} => {State}", this, ex.Message, state);
                    break;
            }
            return state;
        }

        /// <summary>
        /// Get the real timeout for the service call
        /// </summary>
        /// <param name="serviceCallTimeout"></param>
        /// <returns></returns>
        private TimeSpan GetServiceCallTimeout(int? serviceCallTimeout)
        {
            if (serviceCallTimeout > 0)
            {
                return TimeSpan.FromMilliseconds(serviceCallTimeout.Value);
            }
            if (ServiceCallTimeout.HasValue)
            {
                return ServiceCallTimeout.Value;
            }
            if (OperationTimeout.HasValue && OperationTimeout > kDefaultServiceCallTimeout)
            {
                return OperationTimeout.Value;
            }
            return kDefaultServiceCallTimeout;
        }

        /// <summary>
        /// Get the real timeout for the connectivity of session
        /// </summary>
        /// <param name="connectTimeout"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <returns></returns>
        private TimeSpan GetConnectCallTimeout(int? connectTimeout, int? serviceCallTimeout)
        {
            if (connectTimeout > 0)
            {
                return TimeSpan.FromMilliseconds(connectTimeout.Value);
            }
            if (ConnectTimeout.HasValue)
            {
                return ConnectTimeout.Value;
            }
            if (serviceCallTimeout > 0)
            {
                return TimeSpan.FromMilliseconds(serviceCallTimeout.Value);
            }
            return ServiceCallTimeout ?? kDefaultConnectTimeout;
        }

        private enum ConnectionEvent
        {
            Connect,
            ConnectRetry,
            Disconnect,
            StartReconnect,
            ReconnectComplete,
            Reset,
            SubscriptionManage,
            SubscriptionClose
        }

        private enum SessionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting
        }

        /// <summary>
        /// Disconnected state
        /// </summary>
        private sealed class DisconnectState : IOpcUaClientDiagnostics
        {
            /// <inheritdoc/>
            public int BadPublishRequestCount
                => 0;
            /// <inheritdoc/>
            public int GoodPublishRequestCount
                => 0;
            /// <inheritdoc/>
            public int OutstandingRequestCount
                => 0;
            /// <inheritdoc/>
            public int SubscriptionCount
                => 0;
            /// <inheritdoc/>
            public EndpointConnectivityState State
                => EndpointConnectivityState.Disconnected;
            /// <inheritdoc/>
            public int ReconnectCount
                => 0;
            /// <inheritdoc/>
            public int MinPublishRequestCount
                => 0;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableGauge("iiot_edge_publisher_client_connectivity_state",
                () => new Measurement<int>((int)_lastState, _metrics.TagList),
                description: "Client connectivity state.");
            _meter.CreateObservableGauge("iiot_edge_publisher_client_keep_alive_counter",
                () => new Measurement<int>(_keepAliveCounter, _metrics.TagList),
                description: "Number of successful keep alives since last keep alive error.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_namespace_change_count",
                () => new Measurement<int>(_namespaceTableChanges, _metrics.TagList),
                description: "Number of namespace table changes detected by the client.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_subscription_count",
                () => new Measurement<int>(SubscriptionCount, _metrics.TagList),
                description: "Number of client managed subscriptions.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_sampler_count",
                () => new Measurement<int>(_samplers.Count, _metrics.TagList),
                description: "Number of active client samplers.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_browser_count",
                () => new Measurement<int>(_browsers.Count, _metrics.TagList),
                description: "Number of active browsers.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_connectivity_retry_count",
                () => new Measurement<int>(_numberOfConnectRetries, _metrics.TagList),
                description: "Number of connectivity retries on this connection.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_ref_count",
                () => new Measurement<int>(_refCount, _metrics.TagList),
                description: "Number of references to this client.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_good_publish_requests_count",
                () => new Measurement<int>(GoodPublishRequestCount, _metrics.TagList),
                description: "Number of good publish requests.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_bad_publish_requests_count",
                () => new Measurement<int>(BadPublishRequestCount, _metrics.TagList),
                description: "Number of bad publish requests.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_min_publish_requests_count",
                () => new Measurement<int>(MinPublishRequestCount, _metrics.TagList),
                description: "Number of min publish requests that should be queued.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_outstanding_requests_count",
                () => new Measurement<int>(OutstandingRequestCount, _metrics.TagList),
                description: "Number of outstanding requests.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_publish_timeout_count",
                () => new Measurement<int>(_publishTimeoutCounter, _metrics.TagList),
                description: "Number of timed out requests.");
        }

        private static readonly UpDownCounter<int> kSessions = Diagnostics.Meter.CreateUpDownCounter<int>(
            "iiot_edge_publisher_session_count", description: "Number of active sessions.");

        private OpcUaSession? _reconnectingSession;
        private int _reconnectRequired;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private OpcUaSession? _session;
        private IDisposable? _disconnectLock;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private EndpointConnectivityState _lastState;
        private int _numberOfConnectRetries;
        private bool _disposed;
        private int _refCount;
        private int _publishTimeoutCounter;
        private int _keepAliveCounter;
        private int _namespaceTableChanges;
        private ChannelDiagnosticModel _lastDiagnostics;
        private readonly ReverseConnectManager? _reverseConnectManager;
        private readonly AsyncReaderWriterLock _lock = new();
        private readonly ApplicationConfiguration _configuration;
        private readonly IJsonSerializer _serializer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Meter _meter;
        private readonly string _sessionName;
        private readonly ConnectionModel _connection;
        private readonly IMetricsContext _metrics;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private readonly object _channelLock = new();
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly ITimer _channelMonitor;
        private readonly SessionReconnectHandler _reconnectHandler;
        private readonly CancellationTokenSource _cts;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly TimeSpan _maxReconnectPeriod;
        private readonly Channel<(ConnectionEvent, object?)> _channel;
        private readonly Action<ChannelDiagnosticModel> _diagnosticsCb;
        private readonly EventHandler<EndpointConnectivityStateEventArgs>? _notifier;
        private readonly Dictionary<(string, TimeSpan), Sampler> _samplers = new();
        private readonly Dictionary<(string, TimeSpan), Browser> _browsers = new();
        private readonly Dictionary<string, CancellationTokenSource> _tokens;
        private readonly Task _sessionManager;
        private static readonly TimeSpan kDefaultServiceCallTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan kDefaultConnectTimeout = TimeSpan.FromMinutes(1);
    }
}
