// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Exceptions;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
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
        /// The reconnect periodic retry delay to use in ms.
        /// </summary>
        public TimeSpan? ReconnectPeriod { get; set; }

        /// <summary>
        /// The session lifetime.
        /// </summary>
        public TimeSpan? SessionTimeout { get; set; }

        /// <summary>
        /// Retries
        /// </summary>
        public int NumberOfConnectRetries { get; private set; }

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
        /// <param name="sessionName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OpcUaClient(ApplicationConfiguration configuration, ConnectionIdentifier connection,
            IJsonSerializer serializer, ILoggerFactory loggerFactory, IMetricsContext metrics,
            EventHandler<EndpointConnectivityState>? notifier, string? sessionName = null)
        {
            if (connection?.Connection?.Endpoint == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            _connection = connection.Connection;
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            InitializeMetrics();

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _notifier = notifier;

            _logger = _loggerFactory.CreateLogger<OpcUaClient>();
            _lastState = EndpointConnectivityState.Disconnected;
            _sessionName = sessionName ?? connection.ToString();
            _subscriptions = ImmutableHashSet<ISubscriptionHandle>.Empty;

            _cts = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<ConnectionEvent>();
            _disconnectLock = _lock.WriterLock(_cts.Token);
            _sessionManager = Task.Factory.StartNew(() => ManageSessionStateMachineAsync(_cts.Token),
                _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Release();
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return $"{_sessionName} [state:{_lastState}";
        }

        /// <inheritdoc/>
        public ISessionHandle GetSessionHandle()
        {
            return new LockedHandle(_lock.ReaderLock(), this);
        }

        /// <inheritdoc/>
        public async ValueTask<ISessionHandle> GetSessionHandleAsync(CancellationToken ct)
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
            lock (this)
            {
                if (_subscriptions.Contains(subscription))
                {
                    return;
                }
                _subscriptions = _subscriptions.Add(subscription);
                _channel.Writer.TryWrite(ConnectionEvent.SubscriptionChange);
            }
            AddRef();
        }

        /// <inheritdoc/>
        public void UnregisterSubscription(ISubscriptionHandle subscription)
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

        /// <summary>
        /// Safely invoke the service call and retry if the session
        /// disconnected during call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        internal async Task<T> RunAsync<T>(Func<IOpcUaSession, Task<T>> service,
            CancellationToken ct)
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
                        return await service(_session).ConfigureAwait(false);
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
        /// Safely invoke the service call and retry if the session
        /// disconnected during call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        internal async Task<T> RunAsync<T>(Func<IOpcUaSession, Task<(T, bool)>> service,
            CancellationToken ct)
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
                        var (result, keep) = await service(_session).ConfigureAwait(false);

                        // Hold the client session alive for a while
                        // TODO: This could be more elegant...
                        if (keep)
                        {
                            AddRef();
                            _ = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ => Release());
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
            Stack<Func<IOpcUaSession, ValueTask<IEnumerable<T>>>> stack,
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
                        results = await stack.Peek()(_session).ConfigureAwait(false);

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

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(_sessionName);
            }

            _disposed = true;
            _cts.Cancel();

            await _sessionManager.ConfigureAwait(false);

            _lastState = EndpointConnectivityState.Disconnected;
            _subscriptions.Clear();

            UnsetSession();
        }

        /// <summary>
        /// Increment the reference count.
        /// </summary>
        internal void AddRef()
        {
            if (Interlocked.Increment(ref _refCount) == 1)
            {
                // Post connection request
                _channel.Writer.TryWrite(ConnectionEvent.Connect);
            }
        }

        /// <summary>
        /// Release reference count
        /// </summary>
        internal void Release()
        {
            // Decrement reference count
            if (Interlocked.Decrement(ref _refCount) == 0)
            {
                // Post disconnect request
                _channel.Writer.TryWrite(ConnectionEvent.Disconnect);
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

            using var connectTimer = new Timer(_ =>
                _channel.Writer.TryWrite(ConnectionEvent.ConnectRetry));
            var reconnectPeriod = ReconnectPeriod ?? TimeSpan.FromSeconds(3);

            SessionReconnectHandler? reconnectHandler = null;
            try
            {
                await foreach (var trigger in _channel.Reader.ReadAllAsync(ct))
                {
                    _logger.LogDebug("Processing event {Event} in State {State}...",
                        trigger, currentSessionState);

                    switch (trigger)
                    {
                        case ConnectionEvent.ConnectRetry:
                        case ConnectionEvent.Connect:
                            switch (currentSessionState)
                            {
                                case SessionState.Disconnected:
                                    Debug.Assert(reconnectHandler == null);
                                    Debug.Assert(_disconnectLock != null);
                                    Debug.Assert(_session == null);

                                    if (!await TryConnectAsync(ct).ConfigureAwait(false))
                                    {
                                        // Reschedule connecting. Todo: Exponential retry
                                        connectTimer.Change(reconnectPeriod, Timeout.InfiniteTimeSpan);
                                        break;
                                    }

                                    Debug.Assert(_session != null);

                                    // Allow access to session now
                                    _disconnectLock.Dispose();
                                    _disconnectLock = null;

                                    currentSubscriptions = _subscriptions;
                                    await ApplySubscriptionAsync(currentSubscriptions, true).ConfigureAwait(false);

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
                            switch (currentSessionState)
                            {
                                case SessionState.Connected:
                                    // Apply new ones to the current session
                                    var subscriptions = _subscriptions;
                                    var diff = currentSubscriptions.Except(subscriptions);
                                    await ApplySubscriptionAsync(diff, true).ConfigureAwait(false);
                                    currentSubscriptions = subscriptions;
                                    break;
                            }
                            break;

                        case ConnectionEvent.StartReconnect: // sent by the keep alive timeout path
                            switch (currentSessionState)
                            {
                                case SessionState.Connected: // only valid when connected.
                                    Debug.Assert(reconnectHandler == null);

                                    await ApplySubscriptionAsync(currentSubscriptions, false).ConfigureAwait(false);

                                    // Ensure no more access to the session through reader locks
                                    Debug.Assert(_disconnectLock == null);
                                    _disconnectLock = await _lock.WriterLockAsync(ct);

                                    _logger.LogInformation("Reconnecting session {Name} in {Period} ms.",
                                        _sessionName, reconnectPeriod);

                                    reconnectHandler = new SessionReconnectHandler(true);
                                    reconnectHandler.BeginReconnect(_session!.Session,
                                        (int)reconnectPeriod.TotalMilliseconds, (sender, evt) =>
                                        {
                                            // ignore callbacks from discarded objects.
                                            if (ReferenceEquals(sender, reconnectHandler))
                                            {
                                                _channel.Writer.TryWrite(ConnectionEvent.ReconnectComplete);
                                            }
                                        });

                                    // Unset session - do not dispose the session while reconnecting.
                                    UnsetSession(true);
                                    Debug.Assert(_session == null);
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
                            var newSession = reconnectHandler?.Session as Session;
                            reconnectHandler?.Dispose();
                            reconnectHandler = null;

                            switch (currentSessionState)
                            {
                                case SessionState.Reconnecting:
                                    if (newSession?.Connected != true)
                                    {
                                        // Failed to reconnect.
                                        _logger.LogInformation("--- SESSION {Name} failed reconnecting. ---",
                                            _sessionName);

                                        // Schedule a full reconnect immediately
                                        newSession?.Dispose();
                                        currentSessionState = SessionState.Disconnected;
                                        _channel.Writer.TryWrite(ConnectionEvent.Connect);
                                        break;
                                    }

                                    SetSession(newSession);
                                    _logger.LogInformation("--- SESSION {Name} RECONNECTED ---", _sessionName);
                                    NumberOfConnectRetries++;
                                    NotifyConnectivityStateChange(EndpointConnectivityState.Ready);

                                    // Allow access to session again
                                    Debug.Assert(_disconnectLock != null);
                                    _disconnectLock.Dispose();
                                    _disconnectLock = null;

                                    currentSubscriptions = _subscriptions;
                                    await ApplySubscriptionAsync(currentSubscriptions, true).ConfigureAwait(false);

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
                            reconnectHandler?.Dispose();
                            reconnectHandler = null;

                            await ApplySubscriptionAsync(currentSubscriptions, false).ConfigureAwait(false);
                            currentSubscriptions = ImmutableHashSet<ISubscriptionHandle>.Empty;

                            // if not already disconnected, aquire writer lock
                            if (_disconnectLock == null)
                            {
                                _disconnectLock = await _lock.WriterLockAsync(ct);
                            }

                            NumberOfConnectRetries = 0;

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
                            UnsetSession();
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
                reconnectHandler?.Dispose();
                reconnectHandler = null;
            }

            async ValueTask ApplySubscriptionAsync(ImmutableHashSet<ISubscriptionHandle> subscriptions,
                bool online)
            {
                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        if (online)
                        {
                            await subscription.ReapplyToSessionAsync(_session!).ConfigureAwait(false);
                        }
                        subscription.OnSubscriptionStateChanged(online);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to apply subscription to session.");
                    }
                }
            }
        }

        /// <summary>
        /// Connect client
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<bool> TryConnectAsync(CancellationToken ct)
        {
            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);

            var endpointUrlCandidates = _connection.Endpoint!.Url.YieldReturn();
            if (_connection.Endpoint.AlternativeUrls != null)
            {
                endpointUrlCandidates = endpointUrlCandidates.Concat(
                    _connection.Endpoint.AlternativeUrls);
            }

            _logger.LogInformation("--- SESSION {Name} CONNECTING to {EndpointUrl} ---",
                _sessionName, _connection.Endpoint.Url);
            var attempt = 0;
            foreach (var endpointUrl in endpointUrlCandidates)
            {
                UnsetSession(); // Ensure any previous session is disposed here.
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
                        "#{Attempt}: Creating session {Name} for endpoint {EndpointUrl}...",
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
                    var session = await Opc.Ua.Client.Session.Create(_configuration,
                        reverseConnectManager: null, endpoint,
                        updateBeforeConnect: true, // Udpate endpoint through discovery
                        checkDomain: false, // Domain must match on connect
                        _sessionName,
                        (uint)sessionTimeout.TotalMilliseconds,
                        userIdentity, preferredLocales, ct).ConfigureAwait(false);
                    session.KeepAliveInterval =
                        (int)(KeepAliveInterval ?? TimeSpan.FromSeconds(5)).TotalMilliseconds;

                    // Assign the created session
                    SetSession(session);
                    _logger.LogInformation(
                        "New Session {Name} created with endpoint {EndpointUrl} ({Original}).",
                        _sessionName, endpointUrl, _connection.Endpoint.Url);

                    NumberOfConnectRetries++;

                    _logger.LogInformation("--- SESSION {Name} CONNECTED to {EndpointUrl}---",
                        _sessionName, endpointUrl);
                    return true;
                }
                catch (Exception ex)
                {
                    NotifyConnectivityStateChange(ToConnectivityState(ex));
                    NumberOfConnectRetries++;
                    _logger.LogInformation(
                        "#{Attempt}: Failed to create session {Name} to {EndpointUrl}: {Message}...",
                        ++attempt, _sessionName, endpointUrl, ex.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Handles a keep alive event from a session and triggers a reconnect if necessary.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            try
            {
                // check for events from discarded sessions.
                if (!ReferenceEquals(session, _session))
                {
                    return;
                }

                // start reconnect sequence on communication error.
                if (ServiceResult.IsBad(e.Status))
                {
                    _channel.Writer.TryWrite(ConnectionEvent.StartReconnect);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnKeepAlive for client {Client}.", this);
            }
        }

        /// <summary>
        /// Update session state
        /// </summary>
        /// <param name="session"></param>
        private void SetSession(Session session)
        {
            if (ReferenceEquals(_session?.Session, session))
            {
                return;
            }

            session.KeepAliveInterval = (int)(KeepAliveInterval ?? TimeSpan.FromSeconds(5)).TotalMilliseconds;

            UnsetSession();

            _session = new OpcUaSession(session, _sessionName, Session_KeepAlive, _serializer,
                _loggerFactory.CreateLogger<OpcUaSession>());
            NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
            kSessions.Add(1, _metrics.TagList);
        }

        /// <summary>
        /// Unset and dispose existing session
        /// </summary>
        /// <param name="noDispose"></param>
        private void UnsetSession(bool noDispose = false)
        {
            if (_session == null)
            {
                return;
            }

            _session.DoNotDisposeSessionWhenDisposing = noDispose;

            _session.Dispose();
            _session = null;
            kSessions.Add(-1, _metrics.TagList);
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

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            Diagnostics.Meter.CreateObservableUpDownCounter("iiot_edge_publisher_connection_retries",
                () => new Measurement<long>(NumberOfConnectRetries, _metrics.TagList), "Connection attempts",
                "OPC UA connect retries.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_is_connection_ok",
                () => new Measurement<int>(IsConnected ? 1 : 0, _metrics.TagList), "",
                "OPC UA connection success flag.");
        }
        private static readonly UpDownCounter<int> kSessions = Diagnostics.Meter.CreateUpDownCounter<int>(
            "iiot_edge_publisher_session_count", "Number of active sessions.");

        private enum ConnectionEvent
        {
            Connect,
            ConnectRetry,
            Disconnect,
            StartReconnect,
            ReconnectComplete,
            SubscriptionChange
        }

        private enum SessionState
        {
            Disconnected,
            Connected,
            Reconnecting
        }

        private OpcUaSession? _session;
        private IDisposable? _disconnectLock;
        private EndpointConnectivityState _lastState;
        private ImmutableHashSet<ISubscriptionHandle> _subscriptions;
        private bool _disposed;
        private int _refCount;
        private readonly AsyncReaderWriterLock _lock = new();
        private readonly ApplicationConfiguration _configuration;
        private readonly IJsonSerializer _serializer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _sessionName;
        private readonly ConnectionModel _connection;
        private readonly IMetricsContext _metrics;
        private readonly EventHandler<EndpointConnectivityState>? _notifier;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;
        private readonly Channel<ConnectionEvent> _channel;
        private readonly Task _sessionManager;
    }
}
