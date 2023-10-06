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
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// OPC UA Client based on official ua client reference sample.
    /// </summary>
    internal sealed class OpcUaClient : IAsyncDisposable, IOpcUaClient,
        ISessionAccessor
    {
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
        /// The linger timeout.
        /// </summary>
        public TimeSpan? LingerTimeout { get; set; }

        /// <summary>
        /// Is reconnecting
        /// </summary>
        internal bool IsConnected => _session?.Session.Connected ?? false;

        /// <summary>
        /// Disable complex type preloading.
        /// </summary>
        public bool? DisableComplexTypePreloading { get; set; }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="connection"></param>
        /// <param name="serializer"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="meter"></param>
        /// <param name="metrics"></param>
        /// <param name="notifier"></param>
        /// <param name="sessionFactory"></param>
        /// <param name="reverseConnectManager"></param>
        /// <param name="maxReconnectPeriod"></param>
        /// <param name="sessionName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OpcUaClient(ApplicationConfiguration configuration,
            ConnectionIdentifier connection, IJsonSerializer serializer,
            ILoggerFactory loggerFactory, Meter meter, IMetricsContext metrics,
            EventHandler<EndpointConnectivityState>? notifier,
            ISessionFactory sessionFactory, ReverseConnectManager? reverseConnectManager,
            TimeSpan? maxReconnectPeriod = null, string? sessionName = null)
        {
            if (connection?.Connection?.Endpoint?.Url == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            _connection = connection.Connection;
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
            _sessionFactory = sessionFactory ??
                throw new ArgumentNullException(nameof(sessionFactory));
            _notifier = notifier;

            InitializeMetrics();

            _logger = _loggerFactory.CreateLogger<OpcUaClient>();
            _tokens = new Dictionary<string, CancellationTokenSource>();
            _lastState = EndpointConnectivityState.Disconnected;
            _sessionName = sessionName ?? connection.ToString();
            _subscriptions = ImmutableHashSet<ISubscriptionHandle>.Empty;
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
            _sessionManager = Task.Factory.StartNew(
                () => ManageSessionStateMachineAsync(_cts.Token),
                _cts.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
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
        public ISessionHandle GetSessionHandle()
        {
            return new LockedHandle(_lock.ReaderLock(), this);
        }

        /// <inheritdoc/>
        public async ValueTask<ISessionHandle> GetSessionHandleAsync(
            CancellationToken ct)
        {
            return new LockedHandle(await _lock.ReaderLockAsync(ct), this);
        }

        /// <inheritdoc/>
        public bool TryGetSession([NotNullWhen(true)] out ISession? session)
        {
            session = _session?.Session;
            return session != null;
        }

        /// <inheritdoc/>
        public void RegisterSubscription(ISubscriptionHandle subscription)
        {
            try
            {
                lock (this)
                {
                    if (_subscriptions.Contains(subscription))
                    {
                        return;
                    }
                    _subscriptions = _subscriptions.Add(subscription);
                }
                AddRef();
            }
            finally
            {
                TriggerConnectionEvent(ConnectionEvent.SubscriptionChange,
                    subscription);
            }
        }

        /// <inheritdoc/>
        public void ManageSubscription(ISubscriptionHandle subscription)
        {
            TriggerConnectionEvent(ConnectionEvent.SubscriptionManage,
                    subscription);
        }

        /// <inheritdoc/>
        public void UnregisterSubscription(ISubscriptionHandle subscription)
        {
            try
            {
                lock (this)
                {
                    if (!_subscriptions.Contains(subscription))
                    {
                        _logger.LogWarning("Subscription {Subscription} not found in {Client}.",
                            subscription, this);
                        return;
                    }
                    _subscriptions = _subscriptions.Remove(subscription);
                }
                Release();

                _logger.LogDebug(
                    "Subscription {Subscription} unregistered from {Client} (remaining:{Now}).",
                    subscription, this, _subscriptions.Count);
            }
            finally
            {
                TriggerConnectionEvent(ConnectionEvent.SubscriptionChange);
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(_sessionName);
            }
            try
            {
                _logger.LogDebug("Closing client {Client}...", this);
                _disposed = true;
                _cts.Cancel();

                await _sessionManager.ConfigureAwait(false);
                _reconnectHandler.Dispose();

                foreach (var sampler in _samplers.Values)
                {
                    await sampler.DisposeAsync().ConfigureAwait(false);
                }

                _samplers.Clear();

                if (_session != null)
                {
                    await CloseSessionAsync().ConfigureAwait(false);
                }

                _lastState = EndpointConnectivityState.Disconnected;

                _logger.LogInformation("Successfully closed client {Client}.", this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close client {Client}.", this);
            }
        }

        /// <summary>
        /// Safely invoke the service call and retry if the session
        /// disconnected during call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        internal async Task<T> RunAsync<T>(
            Func<ServiceCallContext, Task<T>> service, CancellationToken ct)
        {
            while (true)
            {
                if (_disposed)
                {
                    throw new ConnectionException($"Session {_sessionName} was closed.");
                }
                try
                {
                    using var readerlock = await _lock.ReaderLockAsync(ct).ConfigureAwait(false);
                    if (_session != null)
                    {
                        // Ensure type system is loaded
                        if (!_session.IsTypeSystemLoaded && DisableComplexTypePreloading != true)
                        {
                            await _session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                        }

                        var context = new ServiceCallContext(_session);
                        var result = await service(context).ConfigureAwait(false);

                        if (context.TrackedToken != null)
                        {
                            AddRef(context.TrackedToken);
                        }
                        else if (LingerTimeout != null)
                        {
                            AddRef(_sessionName, LingerTimeout);
                        }
                        if (context.UntrackedToken != null)
                        {
                            Release(context.UntrackedToken);
                        }
                        return result;
                    }
                }
                catch (Exception ex) when (!IsConnected)
                {
                    _logger.LogInformation("Session disconnected during service call " +
                        "with message {Message}, retrying.", ex.Message);
                }
                ct.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Safely invoke a streaming service and retry if the session
        /// disconnected during an operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stack"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        internal async IAsyncEnumerable<T> RunAsync<T>(
            Stack<Func<ServiceCallContext, ValueTask<IEnumerable<T>>>> stack,
            [EnumeratorCancellation] CancellationToken ct)
        {
            while (stack.Count > 0)
            {
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
                        if (!_session.IsTypeSystemLoaded && DisableComplexTypePreloading != true)
                        {
                            await _session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                        }

                        var context = new ServiceCallContext(_session);
                        results = await stack.Peek()(context).ConfigureAwait(false);

                        // Success
                        stack.Pop();
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (Exception ex) when (!IsConnected)
                {
                    _logger.LogInformation("Session disconnected during service call " +
                        "with message {Message}, retrying.", ex.Message);
                    continue;
                }

                ct.ThrowIfCancellationRequested();
                foreach (var result in results)
                {
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Register sampling of values through this client.
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="item"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        internal IAsyncDisposable RegisterSampler(TimeSpan samplingRate, ReadValueId item,
            Action<uint, DataValue> callback)
        {
            lock (_samplers)
            {
                if (!_samplers.TryGetValue(samplingRate, out var sampler))
                {
                    sampler = new Sampler(this, samplingRate, item, callback);
                }
                else
                {
                    sampler.Add(item, callback);
                }

                // Remove sampler
                return Nito.Disposables.AsyncDisposable.Create(async () =>
                {
                    lock (_samplers)
                    {
                        if (!sampler.Remove(item))
                        {
                            return;
                        }
                        _samplers.Remove(samplingRate);
                    }
                    await sampler.DisposeAsync().ConfigureAwait(false);
                });
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
            var currentSubscriptions = ImmutableHashSet<ISubscriptionHandle>.Empty;

            var reconnectPeriod = 0;
            using var reconnectTimer = new Timer(_ => TriggerConnectionEvent(ConnectionEvent.ConnectRetry));
            try
            {
                await foreach (var (trigger, context) in _channel.Reader.ReadAllAsync(ct))
                {
                    _logger.LogDebug("Processing event {Event} in State {State}...", trigger,
                        currentSessionState);

                    switch (trigger)
                    {
                        case ConnectionEvent.Connect:
                            if (currentSessionState == SessionState.Disconnected)
                            {
                                // Start connecting
                                reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                                currentSessionState = SessionState.Connecting;
                            }
                            goto case ConnectionEvent.ConnectRetry;
                        case ConnectionEvent.ConnectRetry:
                            reconnectPeriod = trigger == ConnectionEvent.Connect ? GetMinReconnectPeriod() :
                                _reconnectHandler.JitteredReconnectPeriod(reconnectPeriod);
                            switch (currentSessionState)
                            {
                                case SessionState.Connecting:
                                    Debug.Assert(_reconnectHandler.State == SessionReconnectHandler.ReconnectState.Ready);
                                    Debug.Assert(_disconnectLock != null);
                                    Debug.Assert(_session == null);

                                    if (!await TryConnectAsync(ct).ConfigureAwait(false))
                                    {
                                        // Reschedule connecting
                                        var retryDelay = _reconnectHandler.CheckedReconnectPeriod(reconnectPeriod);
                                        reconnectTimer.Change(retryDelay, Timeout.Infinite);
                                        break;
                                    }

                                    Debug.Assert(_session != null);

                                    // Allow access to session now
                                    _disconnectLock.Dispose();
                                    _disconnectLock = null;

                                    currentSubscriptions = _subscriptions;
                                    await ApplySubscriptionAsync(currentSubscriptions, true, true,
                                        ct).ConfigureAwait(false);

                                    currentSessionState = SessionState.Connected;
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

                        case ConnectionEvent.SubscriptionChange: // Sent when the subscription list changed
                            var changedSubscription = context as ISubscriptionHandle;
                            var subscriptions = _subscriptions; // Snapshot the list of subscriptions
                            switch (currentSessionState)
                            {
                                case SessionState.Connected:
                                    // What changed?
                                    var diff = currentSubscriptions.Except(subscriptions);
                                    if (changedSubscription != null &&
                                        subscriptions.Contains(changedSubscription) &&
                                        !diff.Contains(changedSubscription))
                                    {
                                        diff = diff.Add(changedSubscription);
                                    }
                                    await ApplySubscriptionAsync(diff, cancellationToken: ct).ConfigureAwait(false);
                                    currentSubscriptions = subscriptions;
                                    break;
                            }
                            break;

                        case ConnectionEvent.SubscriptionManage:
                            switch (currentSessionState)
                            {
                                case SessionState.Connected:
                                    var item = context as ISubscriptionHandle;
                                    Debug.Assert(item != null);
                                    var diff = ImmutableHashSet.Create(item);
                                    await ApplySubscriptionAsync(diff, cancellationToken: ct).ConfigureAwait(false);
                                    break;
                            }
                            break;

                        case ConnectionEvent.StartReconnect: // sent by the keep alive timeout path
                            switch (currentSessionState)
                            {
                                case SessionState.Connected: // only valid when connected.
                                    Debug.Assert(_reconnectHandler.State == SessionReconnectHandler.ReconnectState.Ready);

                                    await ApplySubscriptionAsync(currentSubscriptions, false, false,
                                        ct).ConfigureAwait(false);

                                    // Ensure no more access to the session through reader locks
                                    Debug.Assert(_disconnectLock == null);
                                    _disconnectLock = await _lock.WriterLockAsync(ct);

                                    _logger.LogInformation("Reconnecting session {Session} due to error {Error}...",
                                        _sessionName, context as ServiceResult);
                                    var state = _reconnectHandler.BeginReconnect(_session!.Session,
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
                                    if (reconnected == null)
                                    {
                                        Debug.Assert(_reconnectingSession != null);
                                        reconnected = _reconnectingSession?.Session;
                                    }

                                    Debug.Assert(reconnected != null, "reconnected should never be null");
                                    Debug.Assert(reconnected.Connected, "reconnected should always be connected");

                                    // Handles all 3 cases above.
                                    var isNew = await UpdateSessionAsync(reconnected).ConfigureAwait(false);

                                    Debug.Assert(_session != null);
                                    Debug.Assert(_reconnectingSession == null);
                                    if (!isNew)
                                    {
                                        // Case 1) and 2)
                                        _logger.LogInformation("Client {Client} RECOVERED!", this);
                                    }
                                    else
                                    {
                                        // Case 3)
                                        _logger.LogInformation("Client {Client} RECONNECTED!", this);
                                        _numberOfConnectRetries++;
                                    }

                                    // If not already ready, signal we are ready again and ...
                                    NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
                                    // ... allow access to the client again
                                    Debug.Assert(_disconnectLock != null);
                                    _disconnectLock.Dispose();
                                    _disconnectLock = null;

                                    currentSubscriptions = _subscriptions;
                                    await ApplySubscriptionAsync(currentSubscriptions, true, isNew,
                                        ct).ConfigureAwait(false);

                                    currentSessionState = SessionState.Connected;
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
                            reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);

                            await ApplySubscriptionAsync(currentSubscriptions, false, false,
                                ct).ConfigureAwait(false);
                            currentSubscriptions = ImmutableHashSet<ISubscriptionHandle>.Empty;

                            // if not already disconnected, aquire writer lock
                            if (_disconnectLock == null)
                            {
                                _disconnectLock = await _lock.WriterLockAsync(ct);
                            }

                            _numberOfConnectRetries = 0;

                            if (_session != null)
                            {
                                try
                                {
                                    await _session.CloseAsync(ct).ConfigureAwait(false);
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    _logger.LogError(ex, "Failed to close session {Name}.",
                                        _sessionName);
                                }
                            }

                            // Clean up
                            await CloseSessionAsync().ConfigureAwait(false);
                            Debug.Assert(_session == null);

                            NotifyConnectivityStateChange(EndpointConnectivityState.Disconnected);
                            currentSessionState = SessionState.Disconnected;
                            break;
                    }

                    _logger.LogDebug("Event {Event} in State {State} processed.", trigger,
                        currentSessionState);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client {Client} connection manager exited unexpectedly...", this);
            }
            finally
            {
                _reconnectHandler.CancelReconnect();
            }

            async ValueTask ApplySubscriptionAsync(ImmutableHashSet<ISubscriptionHandle> subscriptions,
                bool? online = null, bool newSession = false, CancellationToken cancellationToken = default)
            {
                _logger.LogDebug("Applying changes to {Count} subscriptions...", subscriptions.Count);
                var sw = Stopwatch.StartNew();

                var session = _session;
                Debug.Assert(session != null || online == false, $"Session is null but online is {online}");

                await Task.WhenAll(subscriptions.Select(async subscription =>
                {
                    try
                    {
                        if (online != false && session != null)
                        {
                            await subscription.SyncWithSessionAsync(session, newSession,
                                cancellationToken).ConfigureAwait(false);
                        }
                        if (online != null)
                        {
                            subscription.OnSubscriptionStateChanged(online.Value,
                                _numberOfConnectRetries);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to apply subscription to session.");
                    }
                })).ConfigureAwait(false);

                if (subscriptions.Count > 1)
                {
                    // Clear the node cache - TODO: we should have a real node cache here
                    session?.NodeCache.Clear();

                    _logger.LogInformation("Applying changes to {Count} subscription(s) took {Duration}.",
                        subscriptions.Count, sw.Elapsed);
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
        /// Connect client
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<bool> TryConnectAsync(CancellationToken ct)
        {
            var timeout = CreateSessionTimeout ?? TimeSpan.FromSeconds(10);

            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);
            Debug.Assert(_connection.Endpoint != null);

            _logger.LogInformation("Connecting Client {Client} to {EndpointUrl}...",
                this, _connection.Endpoint.Url);
            var attempt = 0;
            foreach (var endpointUrl in _connection.GetEndpointUrls())
            {
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
                    var endpointDescription = await SelectEndpointAsync(endpointUrl,
                        connection, _connection.Endpoint.SecurityMode ?? SecurityMode.Best,
                        _connection.Endpoint.SecurityPolicy).ConfigureAwait(false);
                    if (endpointDescription == null)
                    {
                        _logger.LogWarning(
                            "No endpoint found that matches connection of session {Name}.",
                            _sessionName);
                        continue;
                    }
                    var endpointConfiguration = EndpointConfiguration.Create(
                        _configuration);
                    endpointConfiguration.OperationTimeout =
                        (int)timeout.TotalMilliseconds;
                    var endpoint = new ConfiguredEndpoint(null, endpointDescription,
                        endpointConfiguration);

                    if (_connection.Endpoint.SecurityMode.HasValue &&
                        _connection.Endpoint.SecurityMode != SecurityMode.None &&
                        endpointDescription.SecurityMode == MessageSecurityMode.None)
                    {
                        _logger.LogWarning("Although the use of security was configured, " +
                            "there was no security-enabled endpoint available at url " +
                            "{EndpointUrl}. An endpoint with no security will be used " +
                            "for session {Name}.",
                            endpointUrl, _sessionName);
                    }

                    var userIdentity = await _connection.User.ToUserIdentityAsync(
                        _configuration).ConfigureAwait(false);

                    var identityPolicy = endpoint.Description.FindUserTokenPolicy(
                        userIdentity.TokenType, userIdentity.IssuedTokenType);
                    if (identityPolicy == null)
                    {
                        _logger.LogWarning(
                            "No UserTokenPolicy for {TokenType}/{IssuedTokenType} " +
                            "found on endpoint {EndpointUrl} (session: {Name}).",
                            userIdentity.TokenType, userIdentity.IssuedTokenType,
                            endpointUrl, _sessionName);
                        continue;
                    }
                    _logger.LogInformation(
                        "#{Attempt}: Creating session {Name} with endpoint {EndpointUrl}...",
                        ++attempt, _sessionName, endpointUrl);
                    // Create the session with english as default and current language
                    // locale as backup
                    var preferredLocales = new HashSet<string>
                    {
                        "en-US",
                        CultureInfo.CurrentCulture.Name
                    }.ToList();

                    var sessionTimeout = SessionTimeout ?? TimeSpan.FromSeconds(30);
                    var session = await _sessionFactory.CreateAsync(_configuration,
                        _reverseConnectManager, endpoint,
                        // Update endpoint through discovery
                        updateBeforeConnect: _reverseConnectManager != null,
                        checkDomain: false, // Domain must match on connect
                        _sessionName, (uint)sessionTimeout.TotalMilliseconds,
                        userIdentity, preferredLocales, ct).ConfigureAwait(false);
                    // Assign the created session
                    var isNew = await UpdateSessionAsync(session).ConfigureAwait(false);
                    Debug.Assert(isNew);
                    _logger.LogInformation(
                        "New Session {Name} created with endpoint {EndpointUrl} ({Original}).",
                        _sessionName, endpointUrl, _connection.Endpoint.Url);

                    _numberOfConnectRetries++;

                    _logger.LogInformation("Client {Client} CONNECTED to {EndpointUrl}!",
                        this, endpointUrl);
                    return true;
                }
                catch (Exception ex)
                {
                    NotifyConnectivityStateChange(ToConnectivityState(ex));
                    _numberOfConnectRetries++;
                    _logger.LogInformation(
                        "#{Attempt}: Failed to connect {Client} to {EndpointUrl}: {Message}...",
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
        private void Session_HandlePublishError(ISession session, PublishErrorEventArgs e)
        {
            switch (e.Status.Code)
            {
                case StatusCodes.BadConnectionClosed:
                    if (_reconnectingSession == null)
                    {
                        // Ensure we reconnect
                        TriggerConnectionEvent(ConnectionEvent.StartReconnect, e.Status);
                    }
                    break;
                case StatusCodes.BadTooManyOperations:
                    SetCode(e.Status, StatusCodes.BadServerHalted);
                    break;
            }

            // Reach into the private field and update it.
            static void SetCode(ServiceResult status, uint fixup)
            {
                typeof(ServiceResult).GetField("m_code",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)?.SetValue(status, fixup);
            }
        }

        /// <summary>
        /// Feed back acknoledgements
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void Session_PublishSequenceNumbersToAcknowledge(ISession session,
            PublishSequenceNumbersToAcknowledgeEventArgs e)
        {
            var acks = e.AcknowledgementsToSend
                .Concat(e.DeferredAcknowledgementsToSend)
                .ToHashSet();
            if (acks.Count == 0)
            {
                return;
            }
            e.AcknowledgementsToSend.Clear();
            e.DeferredAcknowledgementsToSend.Clear();
            foreach (var subscription in _subscriptions)
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
                _logger.LogDebug(
                    "#{ThreadId}: Sending {Acks} acks and deferring {Deferrals} acks. ({Requests})",
                    Environment.CurrentManagedThreadId, ToString(e.AcknowledgementsToSend),
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
        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            try
            {
                // check for events from discarded sessions.
                if (!ReferenceEquals(session, _session?.Session))
                {
                    return;
                }

                // start reconnect sequence on communication error.
                if (ServiceResult.IsBad(e.Status))
                {
                    TriggerConnectionEvent(ConnectionEvent.StartReconnect, e.Status);

                    _logger.LogInformation(
                        "Got Keep Alive error: {Error} ({TimeStamp}:{ServerState}",
                        e.Status, e.CurrentTime, e.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnKeepAlive for client {Client}.", this);
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
            if (_session == null)
            {
                _session = _reconnectingSession;
                _reconnectingSession = null;
            }

            Debug.Assert(_reconnectingSession == null);
            if (ReferenceEquals(_session?.Session, session))
            {
                // Not a new session
                return false;
            }

            await CloseSessionAsync().ConfigureAwait(false);
            _session = new OpcUaSession(session, Session_KeepAlive,
                KeepAliveInterval ?? TimeSpan.FromSeconds(5),
                OperationTimeout ?? TimeSpan.FromMinutes(1),
                _serializer, _loggerFactory.CreateLogger<OpcUaSession>(),
                Session_HandlePublishError, Session_PublishSequenceNumbersToAcknowledge,
                DisableComplexTypePreloading != true);

            NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
            kSessions.Add(1, _metrics.TagList);
            return true;
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

            async ValueTask DisposeAsync(OpcUaSession session)
            {
                await session.CloseAsync(default).ConfigureAwait(false);
                session.Dispose();
                kSessions.Add(-1, _metrics.TagList);
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
                    "Error, connection to {Endpoint} - leaving state at {Previous}.",
                    _connection.Endpoint!.Url, previous);
                return;
            }

            _lastState = state;
            _logger.LogInformation(
                "Session {Name} with {Endpoint} changed from {Previous} to {State}",
                _sessionName, _connection.Endpoint!.Url, previous, state);
            try
            {
                _notifier?.Invoke(this, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during state callback");
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

            using (DiscoveryClient client = connection != null ?
                DiscoveryClient.Create(_configuration, connection, endpointConfiguration) :
                DiscoveryClient.Create(_configuration, discoveryUrl, endpointConfiguration))
            {
                var uri = new Uri(endpointUrl ?? client.Endpoint.EndpointUrl);
                var endpoints = await client.GetEndpointsAsync(null).ConfigureAwait(false);

                _logger.LogInformation("Selecting endpoint {EndpointUri} with SecurityMode " +
                    "{SecurityMode} and {SecurityPolicy} SecurityPolicyUri from:\n{Endpoints}",
                    uri, securityMode, securityPolicy ?? "any", endpoints.Select(
                        ep => "      " + ToString(ep)).Aggregate((a, b) => $"{a}\n{b}"));

                var filtered = endpoints
                    .Where(ep =>
                        SecurityPolicies.GetDisplayName(ep.SecurityPolicyUri) != null &&
                        (securityMode == SecurityMode.Best ||
                            ep.SecurityMode == securityMode.ToStackType()) &&
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
                        _logger.LogInformation("Endpoint {Endpoint} selected!",
                            ToString(selected));
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
                // Adjust the host name from the host name that was use to
                // successfully connect the discovery client
                //
                var selectedUrl = Utils.ParseUri(selected.EndpointUrl);
                discoveryUrl = Utils.ParseUri(client.Endpoint.EndpointUrl);
                if (selectedUrl != null && discoveryUrl != null &&
                    selectedUrl.Scheme == discoveryUrl.Scheme)
                {
                    selected.EndpointUrl = new UriBuilder(selectedUrl)
                    {
                        Host = discoveryUrl.DnsSafeHost
                    }.ToString();
                }

                _logger.LogInformation("Endpoint {Endpoint} selected!",
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
                    _logger.LogDebug("{Result} => {State}", sre.Result, state);
                    break;
                default:
                    state = EndpointConnectivityState.Error;
                    _logger.LogDebug("{Message} => {State}", ex.Message, state);
                    break;
            }
            return state;
        }

        private enum ConnectionEvent
        {
            Connect,
            ConnectRetry,
            Disconnect,
            StartReconnect,
            ReconnectComplete,
            SubscriptionChange,
            SubscriptionManage
        }

        private enum SessionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting
        }

        /// <summary>
        /// A locked handle
        /// </summary>
        private sealed class LockedHandle : ISessionHandle
        {
            /// <inheritdoc/>
            public IOpcUaSession Handle => _client._session!;

            /// <inheritdoc/>
            public LockedHandle(IDisposable readerLock, OpcUaClient client)
            {
                _readerLock = readerLock;
                _client = client;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _readerLock.Dispose();
            }

            private readonly IDisposable _readerLock;
            private readonly OpcUaClient _client;
        }

        /// <summary>
        /// A set of client sampled values
        /// </summary>
        private sealed class Sampler : IAsyncDisposable
        {
            /// <summary>
            /// Creates the sampler
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="samplingRate"></param>
            /// <param name="initialValue"></param>
            /// <param name="callback"></param>
            public Sampler(OpcUaClient outer, TimeSpan samplingRate,
                ReadValueId initialValue, Action<uint, DataValue> callback)
            {
                initialValue.Handle = callback;
                _values = ImmutableHashSet<ReadValueId>.Empty.Add(initialValue);

                _outer = outer;
                _cts = new CancellationTokenSource();
                _samplingRate = samplingRate;
                _timer = new PeriodicTimer(_samplingRate);
                _sampler = RunAsync(_cts.Token);
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    _cts.Cancel();
                    _timer.Dispose();
                    await _sampler.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _cts.Dispose();
                }
            }

            /// <summary>
            /// Add value to sampler
            /// </summary>
            /// <param name="value"></param>
            /// <param name="callback"></param>
            public Sampler Add(ReadValueId value, Action<uint, DataValue> callback)
            {
                value.Handle = callback;
                _values = _values.Add(value);
                return this;
            }

            /// <summary>
            /// Remove value
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool Remove(ReadValueId value)
            {
                _values = _values.Remove(value);
                return _values.Count == 0;
            }

            /// <summary>
            /// Run sampling of values on the periodic timer
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task RunAsync(CancellationToken ct)
            {
                for (var sequenceNumber = 1u; !ct.IsCancellationRequested; sequenceNumber++)
                {
                    if (sequenceNumber == 0u)
                    {
                        continue;
                    }

                    var nodesToRead = new ReadValueIdCollection(_values);
                    try
                    {
                        // Wait until period completed
                        if (!await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                        {
                            continue;
                        }

                        // Grab the current session
                        var session = _outer._session;
                        if (session == null)
                        {
                            NotifyAll(sequenceNumber, nodesToRead, StatusCodes.BadNotConnected);
                            continue;
                        }

                        // Ensure type system is loaded
                        if (!session.IsTypeSystemLoaded && _outer.DisableComplexTypePreloading != true)
                        {
                            await session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                        }

                        // Perform the read.
                        var timeout = _samplingRate.TotalMilliseconds / 2;
                        var response = await session.ReadAsync(new RequestHeader
                        {
                            Timestamp = DateTime.UtcNow,
                            TimeoutHint = (uint)timeout,
                            ReturnDiagnostics = 0
                        }, 0.0, Opc.Ua.TimestampsToReturn.Both, nodesToRead,
                            ct).ConfigureAwait(false);

                        var values = response.Validate(response.Results,
                            r => r.StatusCode, response.DiagnosticInfos, nodesToRead);
                        if (values.ErrorInfo != null)
                        {
                            NotifyAll(sequenceNumber, nodesToRead, values.ErrorInfo.StatusCode);
                            continue;
                        }

                        // Notify clients of the values
                        values.ForEach(i => ((Action<uint, DataValue>)i.Request.Handle)(
                            sequenceNumber, i.Result));
                    }
                    catch (OperationCanceledException) { }
                    catch (ServiceResultException sre)
                    {
                        NotifyAll(sequenceNumber, nodesToRead, sre.StatusCode);
                    }
                    catch (Exception ex)
                    {
                        var error = new ServiceResult(ex).StatusCode;
                        NotifyAll(sequenceNumber, nodesToRead, error.Code);
                    }
                }
                static void NotifyAll(uint seq, ReadValueIdCollection nodesToRead, uint statusCode)
                {
                    var dataValue = new DataValue(statusCode);
                    nodesToRead.ForEach(i => ((Action<uint, DataValue>)i.Handle)(seq, dataValue));
                }
            }

            private ImmutableHashSet<ReadValueId> _values;
            private readonly CancellationTokenSource _cts;
            private readonly Task _sampler;
            private readonly OpcUaClient _outer;
            private readonly TimeSpan _samplingRate;
            private readonly PeriodicTimer _timer;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableGauge("iiot_edge_publisher_client_connectivity_state",
                () => new Measurement<int>((int)_lastState, _metrics.TagList),
                "EndpointConnectivityState", "Client connectivity state.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_subscription_count",
                () => new Measurement<int>(_subscriptions.Count, _metrics.TagList),
                "Subscriptions", "Number of client managed subscriptions.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_connectivity_retry_count",
                () => new Measurement<int>(_numberOfConnectRetries, _metrics.TagList),
                "Retries", "Number of connectivity retries on this connection.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_ref_count",
                () => new Measurement<int>(_refCount, _metrics.TagList), "References",
                "Number of references to this client.");
        }

        private static readonly UpDownCounter<int> kSessions = Diagnostics.Meter.CreateUpDownCounter<int>(
            "iiot_edge_publisher_session_count", "Number of active sessions.");

        private OpcUaSession? _session;
        private OpcUaSession? _reconnectingSession;
        private IDisposable? _disconnectLock;
        private EndpointConnectivityState _lastState;
        private ImmutableHashSet<ISubscriptionHandle> _subscriptions;
        private int _numberOfConnectRetries;
        private bool _disposed;
        private int _refCount;
        private readonly ReverseConnectManager? _reverseConnectManager;
        private readonly ISessionFactory _sessionFactory;
        private readonly AsyncReaderWriterLock _lock = new();
        private readonly ApplicationConfiguration _configuration;
        private readonly IJsonSerializer _serializer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Meter _meter;
        private readonly string _sessionName;
        private readonly ConnectionModel _connection;
        private readonly IMetricsContext _metrics;
        private readonly ILogger _logger;
        private readonly SessionReconnectHandler _reconnectHandler;
        private readonly TimeSpan _maxReconnectPeriod;
        private readonly CancellationTokenSource _cts;
        private readonly Channel<(ConnectionEvent, object?)> _channel;
        private readonly EventHandler<EndpointConnectivityState>? _notifier;
        private readonly Dictionary<TimeSpan, Sampler> _samplers = new();
        private readonly Dictionary<string, CancellationTokenSource> _tokens;
        private readonly Task _sessionManager;
    }
}
