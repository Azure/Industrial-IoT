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
    using Microsoft.Extensions.Caching.Memory;
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
    using static System.Collections.Specialized.BitVector32;

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
        /// Create client
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="connection"></param>
        /// <param name="serializer"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="metrics"></param>
        /// <param name="notifier"></param>
        /// <param name="cache"></param>
        /// <param name="sessionFactory"></param>
        /// <param name="maxReconnectPeriod"></param>
        /// <param name="sessionName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OpcUaClient(ApplicationConfiguration configuration,
            ConnectionIdentifier connection, IJsonSerializer serializer,
            ILoggerFactory loggerFactory, IMetricsContext metrics,
            EventHandler<EndpointConnectivityState>? notifier,
            IMemoryCache cache, ISessionFactory sessionFactory,
            TimeSpan? maxReconnectPeriod = null, string? sessionName = null)
        {
            if (connection?.Connection?.Endpoint == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            _connection = connection.Connection;

            _metrics = metrics ??
                throw new ArgumentNullException(nameof(metrics));
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));
            _cache = cache ??
                throw new ArgumentNullException(nameof(cache));
            _sessionFactory = sessionFactory ??
                throw new ArgumentNullException(nameof(sessionFactory));
            _notifier = notifier;

            _logger = _loggerFactory.CreateLogger<OpcUaClient>();
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
                        return;
                    }
                    _subscriptions = _subscriptions.Remove(subscription);
                }
                Release();
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

                if (_session != null)
                {
                    await _session.CloseAsync(default).ConfigureAwait(false);
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
        /// Connect
        /// </summary>
        /// <param name="token"></param>
        /// <param name="expiresAfter"></param>
        /// <returns></returns>
        internal void AddRef(string? token = null, TimeSpan? expiresAfter = null)
        {
            if (Interlocked.Increment(ref _refCount) == 1)
            {
                // Post connection request
                TriggerConnectionEvent(ConnectionEvent.Connect);
            }
            if (token != null)
            {
                // _cache.Remove(token);
                using var entry = _cache.CreateEntry(token)
                    .SetValue(this)
                    .SetAbsoluteExpiration(expiresAfter ?? TimeSpan.FromSeconds(10))
                    .SetPriority(CacheItemPriority.NeverRemove)
                    .SetSize(1)
                    .RegisterPostEvictionCallback(
                        (_, o, _, _) => (o as OpcUaClient)?.Release())
                    ;
            }
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        /// <param name="token"></param>
        internal void Release(string? token = null)
        {
            if (token != null)
            {
                _cache.Remove(token);
                return;
            }

            // Decrement reference count
            if (Interlocked.Decrement(ref _refCount) == 0)
            {
                // Post disconnect request
                TriggerConnectionEvent(ConnectionEvent.Disconnect);
            }
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
            using var reconnectTimer =
                new Timer(_ => TriggerConnectionEvent(ConnectionEvent.ConnectRetry));
            try
            {
                await foreach (var (trigger, context) in _channel.Reader.ReadAllAsync(ct))
                {
                    _logger.LogDebug("Processing event {Event} in State {State}...", trigger,
                        currentSessionState);

                    switch (trigger)
                    {
                        case ConnectionEvent.Connect:
                        case ConnectionEvent.ConnectRetry:
                            reconnectPeriod = trigger == ConnectionEvent.Connect ? GetMinReconnectPeriod() :
                                _reconnectHandler.JitteredReconnectPeriod(reconnectPeriod);
                            switch (currentSessionState)
                            {
                                case SessionState.Disconnected:
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
                                    await ApplySubscriptionAsync(currentSubscriptions,
                                        true, ct).ConfigureAwait(false);

                                    currentSessionState = SessionState.Connected;
                                    break;
                                case SessionState.Connected:
                                    // Nothing to do, already connected
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
                                    var diff = ImmutableHashSet.Create<ISubscriptionHandle>(item);
                                    await ApplySubscriptionAsync(diff, cancellationToken: ct).ConfigureAwait(false);
                                    break;
                            }
                            break;

                        case ConnectionEvent.StartReconnect: // sent by the keep alive timeout path
                            switch (currentSessionState)
                            {
                                case SessionState.Connected: // only valid when connected.
                                    Debug.Assert(_reconnectHandler.State == SessionReconnectHandler.ReconnectState.Ready);

                                    await ApplySubscriptionAsync(currentSubscriptions, false,
                                        ct).ConfigureAwait(false);

                                    // Ensure no more access to the session through reader locks
                                    Debug.Assert(_disconnectLock == null);
                                    _disconnectLock = await _lock.WriterLockAsync(ct);

                                    _logger.LogInformation("Reconnecting session {Name}...", _sessionName);
                                    var state = _reconnectHandler.BeginReconnect(_session!.Session,
                                        GetMinReconnectPeriod(), (sender, evt) =>
                                        {
                                            // ignore callbacks from discarded objects.
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
                                case SessionState.Disconnected:
                                case SessionState.Reconnecting:
                                    // Nothing to do
                                    break;
                            }
                            break;

                        case ConnectionEvent.ReconnectComplete:
                            // if session recovered, Session property is not null
                            var newSession = _reconnectHandler.Session as Session;

                            switch (currentSessionState)
                            {
                                case SessionState.Reconnecting:
                                    if (newSession?.Connected != true)
                                    {
                                        // Failed to reconnect.
                                        _logger.LogInformation(
                                            "Client {Client} failed to reconnect. Creating new connection...",
                                            this);

                                        // Schedule a full reconnect immediately
                                        newSession?.Dispose();
                                        await CloseSessionAsync().ConfigureAwait(false);
                                        currentSessionState = SessionState.Disconnected;
                                        TriggerConnectionEvent(ConnectionEvent.Connect);
                                        break;
                                    }

                                    var isNew = await UpdateSessionAsync(newSession).ConfigureAwait(false);
                                    Debug.Assert(_session != null);
                                    Debug.Assert(_reconnectingSession == null);
                                    if (isNew)
                                    {
                                        _logger.LogInformation("Client {Client} RECONNECTED!", this);
                                        _numberOfConnectRetries++;
                                    }
                                    else
                                    {
                                        _logger.LogInformation("Client {Client} RECOVERED!", this);
                                    }
                                    NotifyConnectivityStateChange(EndpointConnectivityState.Ready);

                                    // Allow access to session again
                                    Debug.Assert(_disconnectLock != null);
                                    _disconnectLock.Dispose();
                                    _disconnectLock = null;

                                    currentSubscriptions = _subscriptions;
                                    await ApplySubscriptionAsync(currentSubscriptions, true,
                                        ct).ConfigureAwait(false);

                                    currentSessionState = SessionState.Connected;
                                    break;

                                case SessionState.Connected:
                                    Debug.Fail("Should not signal reconnected when already connected.");
                                    break;
                                case SessionState.Disconnected:
                                    newSession?.Dispose();
                                    break;
                            }
                            break;

                        case ConnectionEvent.Disconnect:

                            // If currently reconnecting, dispose the reconnect handler
                            _reconnectHandler.CancelReconnect();

                            await ApplySubscriptionAsync(currentSubscriptions, false,
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

                    _logger.LogDebug("Event {Event} in State {State} processed.",
                        trigger, currentSessionState);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Client {Client} connection manager exited unexpectedly...", this);
            }
            finally
            {
                _reconnectHandler.CancelReconnect();
            }

            async ValueTask ApplySubscriptionAsync(ImmutableHashSet<ISubscriptionHandle> subscriptions,
                bool? online = null, CancellationToken cancellationToken = default)
            {
                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        if (online != false)
                        {
                            await subscription.SyncWithSessionAsync(_session!, false,
                                cancellationToken).ConfigureAwait(false);
                        }
                        if (online != null)
                        {
                            subscription.OnSubscriptionStateChanged(online.Value,
                                _numberOfConnectRetries);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to apply subscription to session.");
                    }
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

            var endpointUrlCandidates = _connection.Endpoint!.Url.YieldReturn();
            if (_connection.Endpoint.AlternativeUrls != null)
            {
                endpointUrlCandidates = endpointUrlCandidates.Concat(
                    _connection.Endpoint.AlternativeUrls);
            }

            _logger.LogInformation("Connecting Client {Client} to {EndpointUrl}...",
                this, _connection.Endpoint.Url);
            var attempt = 0;
            foreach (var endpointUrl in endpointUrlCandidates)
            {
                await CloseSessionAsync().ConfigureAwait(false); // Ensure any previous session is disposed here.
                ct.ThrowIfCancellationRequested();
                try
                {
                    //
                    // Get the endpoint by connecting to server's discovery endpoint.
                    // Try to find the first endpoint with security.
                    //
                    var endpointDescription = CoreClientUtils.SelectEndpoint(
                        _configuration, endpointUrl,
                        _connection.Endpoint.SecurityMode != SecurityMode.None);
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
                            "{EndpointUrl}. An endpoint with no security will be used.",
                            endpointUrl);
                    }

                    _logger.LogInformation(
                        "#{Attempt}: Creating session {Name} with endpoint {EndpointUrl}...",
                        ++attempt, _sessionName, endpointUrl);
                    var userIdentity = _connection.User.ToStackModel()
                        ?? new UserIdentity(new AnonymousIdentityToken());

                    // Create the session with english as default and current language
                    // locale as backup
                    var preferredLocales = new HashSet<string>
                    {
                        "en-US",
                        CultureInfo.CurrentCulture.Name
                    }.ToList();

                    var sessionTimeout = SessionTimeout ?? TimeSpan.FromSeconds(30);
                    var session = await _sessionFactory.CreateAsync(_configuration,
                        reverseConnectManager: null, endpoint,
                        updateBeforeConnect: false, // Udpate endpoint through discovery
                        checkDomain: false, // Domain must match on connect
                        _sessionName,
                        (uint)sessionTimeout.TotalMilliseconds,
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
            e.DeferredAcknowledgementsToSend.AddRange(acks.Except(
                e.AcknowledgementsToSend));

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Sending {Acks} acks and deferring {Deferrals} acks.",
                    ToString(e.AcknowledgementsToSend),
                    ToString(e.DeferredAcknowledgementsToSend));
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
                    TriggerConnectionEvent(ConnectionEvent.StartReconnect);
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
                Session_PublishSequenceNumbersToAcknowledge);

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
                //
                // Notify the subscription so it can reflect close state
                // in the original session.
                //
                foreach (var subscription in _subscriptions)
                {
                    await subscription.SyncWithSessionAsync(session, true,
                        default).ConfigureAwait(false);
                    subscription.OnSubscriptionStateChanged(false,
                        _numberOfConnectRetries);
                }
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
        /// Convert exception to connectivity state
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="reconnecting"></param>
        /// <returns></returns>
        public EndpointConnectivityState ToConnectivityState(Exception ex, bool reconnecting = true)
        {
            var state = EndpointConnectivityState.Error;
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
            Connected,
            Reconnecting
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
        private readonly IMemoryCache _cache;
        private readonly ISessionFactory _sessionFactory;
        private readonly AsyncReaderWriterLock _lock = new();
        private readonly ApplicationConfiguration _configuration;
        private readonly IJsonSerializer _serializer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _sessionName;
        private readonly ConnectionModel _connection;
        private readonly IMetricsContext _metrics;
        private readonly ILogger _logger;
        private readonly SessionReconnectHandler _reconnectHandler;
        private readonly TimeSpan _maxReconnectPeriod;
        private readonly CancellationTokenSource _cts;
        private readonly Channel<(ConnectionEvent, object?)> _channel;
        private readonly EventHandler<EndpointConnectivityState>? _notifier;
        private readonly Task _sessionManager;
    }
}
