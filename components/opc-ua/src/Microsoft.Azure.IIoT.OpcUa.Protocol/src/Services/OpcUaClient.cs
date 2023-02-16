// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using Nito.AsyncEx;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// OPC UA Client based on official ua client reference sample.
    /// </summary>
    public class OpcUaClient : IAsyncDisposable, ISessionHandle, IMetricsContext {

        /// <inheritdoc/>
        public event EventHandler<EndpointConnectivityState> OnConnectionStateChange;

        /// <inheritdoc/>
        public ISession Session => _session;

        /// <inheritdoc/>
        public TagList TagList { get; }

        /// <summary>
        /// The session keepalive interval to be used in ms.
        /// </summary>
        public int KeepAliveInterval { get; set; } = 5000;

        /// <summary>
        /// The reconnect period to be used in ms.
        /// </summary>
        public int ReconnectPeriod { get; set; } = 1000;

        /// <summary>
        /// The session lifetime.
        /// </summary>
        public uint SessionLifeTime { get; set; } = 30 * 1000;

        /// <summary>
        /// The file to use for log output.
        /// </summary>
        public int NumberOfConnectRetries { get; internal set; }

        /// <summary>
        /// Is reconnecting
        /// </summary>
        public bool IsReconnecting => _reconnectHandler != null;

        /// <summary>
        /// Is reconnecting
        /// </summary>
        internal bool IsConnected => _lastState == EndpointConnectivityState.Ready;

        /// <summary>
        /// Whether the connect operation is in progress
        /// </summary>
        internal bool HasSubscriptions => !_subscriptions.IsEmpty;

        /// <summary>
        /// Check if session is active
        /// </summary>
        public bool IsActive => HasSubscriptions ||
            _lastActivity + TimeSpan.FromSeconds(SessionLifeTime) > DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public OpcUaClient(ApplicationConfiguration configuration,
            ConnectionIdentifier connection, ILogger logger, IMetricsContext metrics,
            string sessionName = null) : this(metrics, connection) {

            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _connected = new AsyncManualResetEvent();
            _lastState = EndpointConnectivityState.Connecting;
            _sessionName = sessionName ?? connection.ToString();
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void RegisterSubscription(ISubscription subscription) {
            var id = new ConnectionIdentifier(subscription.Connection);
            _lock.Wait();
            try {
                _subscriptions.AddOrUpdate(subscription.Name, subscription, (_, _) => subscription);
                _logger.Information(
                    "Subscription {subscriptionId} registered/updated in session {id}.",
                    subscription.Name, id);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to register subscription");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void UnregisterSubscription(ISubscription subscription) {
            _lock.Wait();
            try {
                if (_subscriptions.TryRemove(subscription.Name, out _)) {
                    _logger.Information(
                        "Subscription {subscriptionId} unregistered from session {id}.",
                        subscription.Name, _sessionName);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Connect the client. Returns true if connection was established.
        /// </summary>
        /// <param name="reapplySubscriptionState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask<bool> ConnectAsync(bool reapplySubscriptionState = false,
            CancellationToken ct = default) {
            if (!await _connecting.WaitAsync(0, ct)) {
                // If already connecting
                return false;
            }
            bool connected;
            try {
                connected = await ConnectAsyncCore().ConfigureAwait(false);
            }
            catch (Exception ex) {
                // Log Error
                _logger.Error(ex, "Error creating session {Name}.", _sessionName);
                _session?.Dispose();
                _session = null;
                _complexTypeSystem = null;
                connected = false;
            }
            finally {
                _connecting.Release();
            }

            if (connected && reapplySubscriptionState) {
                //
                // Apply subscription settings for existing subscriptions
                // This will take the subscription lock, since the connect
                // can be called under it the default should be false.
                // Only if the manager task calls connect we should do this.
                //
                foreach (var subscription in _subscriptions.Values) {
                    await subscription.ReapplyToSessionAsync(this);
                }
                _logger.Information("Reapplied all subscriptions to session {Name}.",
                    _sessionName);
            }

            NotifySubscriptionStateChange(connected);
            return connected;
        }

        /// <inheritdoc/>
        public async Task<T> RunAsync<T>(Func<ISession, Task<T>> service,
            CancellationToken ct) {
            while (true) {
                await _connected.WaitAsync(ct);
                if (_disposed) {
                    throw new ConnectionException(
                        $"Session {_sessionName} was closed.");
                }
                if (_session == null || !_session.Connected) {
                    _logger.Information(
                        "Connected signaled but not connected, retry in 5 seconds...");
                    // Delay and try again
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    continue;
                }
                try {
                    return await service(_session);
                }
                catch (Exception ex) when (!_session.Connected) {
                    _logger.Information("Session disconnected during service call " +
                        "with message {message}, retrying.", ex.Message);
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            if (_disposed) {
                throw new ObjectDisposedException(_sessionName);
            }
            _disposed = true;
            Session session;
            List<ISubscription> subscriptions;
            await _lock.WaitAsync();
            try {
                NotifyConnectivityStateChange(EndpointConnectivityState.Disconnected);
                _reconnectHandler?.Dispose();
                subscriptions = _subscriptions.Values.ToList();
                _subscriptions.Clear();
                session = _session;
                UnsetSession(true);
                _connected.Set(); // Release any waiting tasks with exception
            }
            finally {
                _lock.Release();
                NumberOfConnectRetries = 0;
            }

            try {
                _logger.Information("Closing session {Name}...", _sessionName);

                if (subscriptions.Count > 0) {
                    //
                    // Close all subscriptions. Since this might call back into
                    // the session manager and we are under the lock, queue this
                    // to the thread pool to execute after
                    //
                    ThreadPool.QueueUserWorkItem(_ => {
                        foreach (var subscription in _subscriptions.Values) {
                            Try.Op(() => subscription.Dispose());
                        }
                    });
                }

                await session.CloseAsync();

                // Log Session Disconnected event
                _logger.Debug("Session {Name} closed.", _sessionName);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Error during closing of session {Name}.", _sessionName);
            }
            finally {
                session.Dispose();
                // Clean up resources
                _lock.Dispose();
            }
        }

        /// <summary>
        /// Connect client (no lock)
        /// </summary>
        /// <returns></returns>
        private async ValueTask<bool> ConnectAsyncCore() {
            if (IsReconnecting) {
                // Cannot connect while reconnecting.
                _logger.Information("Session {Name} is reconnecting. Not connecting.",
                    _sessionName);
                return false;
            }

            if (IsConnected) {
                // Nothing to do but try ensure complex type system is loaded
                if (!_complexTypeSystem.IsCompleted) {
                    await _complexTypeSystem;
                }
                return true;
            }

            UnsetSession(); // Ensure any previous session is disposed here.
            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);
            _logger.Debug("Initializing session '{Name}'...", _sessionName);

            var endpointUrlCandidates = _connection.Endpoint.Url.YieldReturn();
            if (_connection.Endpoint.AlternativeUrls != null) {
                endpointUrlCandidates = endpointUrlCandidates.Concat(
                    _connection.Endpoint.AlternativeUrls);
            }
            var attempt = 0;
            foreach (var endpointUrl in endpointUrlCandidates) {
                try {
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
                        endpointDescription.SecurityMode == MessageSecurityMode.None) {
                        _logger.Warning("Although the use of security was configured, " +
                            "there was no security-enabled endpoint available at url " +
                            "{endpointUrl}. An endpoint with no security will be used.",
                            endpointUrl);
                    }

                    _logger.Information(
                        "#{Attempt}: Creating session {Name} for endpoint {endpointUrl}...",
                        ++attempt, _sessionName, endpointUrl);
                    var userIdentity = _connection.User.ToStackModel()
                        ?? new UserIdentity(new AnonymousIdentityToken());

                    // Create the session
                    var session = await Opc.Ua.Client.Session.Create(_configuration, endpoint,
                        false, false, _sessionName, SessionLifeTime, userIdentity,
                        Array.Empty<string>()).ConfigureAwait(false);

                    // Assign the created session
                    SetSession(session);
                    _logger.Information(
                        "New Session {Name} created with endpoint {endpointUrl} ({Original}).",
                        _sessionName, endpointUrl, _connection.Endpoint.Url);

                    // Try and load type system - does not throw but logs error
                    await _complexTypeSystem;
                    NumberOfConnectRetries++;
                    return true;
                }
                catch (Exception ex) {
                    NotifyConnectivityStateChange(ToConnectivityState(ex));
                    NumberOfConnectRetries++;
                    _logger.Information(
                        "#{Attempt}: Failed to create session {Name} to {endpointUrl}: {message}...",
                        ++attempt, _sessionName, endpointUrl, ex.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Load complex type system
        /// </summary>
        /// <returns></returns>
        public async ValueTask<ComplexTypeSystem> GetComplexTypeSystemAsync() {
            await _lock.WaitAsync();
            try {
                if (_complexTypeSystem == null) {
                    _complexTypeSystem = LoadComplexTypeSystemAsync();
                }
                return await _complexTypeSystem.ConfigureAwait(false);
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Handles a keep alive event from a session and triggers a reconnect if necessary.
        /// </summary>
        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e) {
            try {
                // check for events from discarded sessions.
                if (!ReferenceEquals(session, _session)) {
                    return;
                }

                // start reconnect sequence on communication error.
                if (ServiceResult.IsBad(e.Status)) {
                    if (ReconnectPeriod <= 0) {
                        _logger.Warning(
                            "KeepAlive status {Status} for session {Name}, but reconnect is disabled.",
                            e.Status, _sessionName);
                        return;
                    }

                    _lock.Wait();
                    try {
                        if (_reconnectHandler == null) {
                            _logger.Information(
                                "KeepAlive status {Status} for session {Name}, reconnecting in {Period}ms.",
                                e.Status, _sessionName, ReconnectPeriod);
                            _reconnectHandler = new SessionReconnectHandler(true);
                            _reconnectHandler.BeginReconnect(_session,
                                ReconnectPeriod, Client_ReconnectComplete);
                            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);
                        }
                        else {
                            _logger.Debug(
                                "KeepAlive status {Status} for session {Name}, reconnect in progress.",
                                e.Status, _sessionName);
                        }
                    }
                    finally {
                        _lock.Release();
                    }

                    // Go offline
                    NotifySubscriptionStateChange(false);
                }
            }
            catch (Exception ex) {
                Utils.LogError(ex, "Error in OnKeepAlive for session {Name}.", _sessionName);
            }
        }

        /// <summary>
        /// Called when the reconnect attempt was successful.
        /// </summary>
        private void Client_ReconnectComplete(object sender, EventArgs e) {
            // ignore callbacks from discarded objects.
            if (!ReferenceEquals(sender, _reconnectHandler)) {
                return;
            }

            Session newSession;
            _lock.Wait();
            try {
                // if session recovered, Session property is not null
                newSession = _reconnectHandler.Session as Session;
                _reconnectHandler.Dispose();
                _reconnectHandler = null;
            }
            finally {
                _lock.Release();
            }

            if (newSession != null) {
                SetSession(newSession);
            }
            if (!IsConnected) {
                // Failed to reconnect.
                return;
            }

            _logger.Information("--- SESSION {Name} RECONNECTED ---", _sessionName);
            NumberOfConnectRetries++;

            NotifyConnectivityStateChange(EndpointConnectivityState.Ready);

            // Go back online
            NotifySubscriptionStateChange(IsConnected);
        }

        /// <summary>
        /// Update session state
        /// </summary>
        /// <param name="session"></param>
        private void SetSession(Session session) {
            if (session == null || !session.Connected) {
                _logger.Information("Session not connected.");
                return;
            }

            if (_session == session) {
                Debug.Assert(session.Handle == this);
                return;
            }
            _lock.Wait();
            try {
                UnsetSession();

                // override keep alive interval
                _session = session;
                _session.KeepAliveInterval = KeepAliveInterval;
                _complexTypeSystem = LoadComplexTypeSystemAsync();

                // set up keep alive callback.
                _session.KeepAlive += Session_KeepAlive;
                _session.Handle = this;
                NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Unset and dispose existing session
        /// </summary>
        private void UnsetSession(bool noDispose = false) {
            var session = _session;
            _session = null;
            _complexTypeSystem = null;

            if (session == null) {
                return;
            }
            session.KeepAlive -= Session_KeepAlive;
            session.Handle = null;
            if (noDispose) {
                return;
            }
            session.Dispose();
            _logger.Debug("Session {Name} disposed.", _sessionName);
        }

        /// <summary>
        /// Load complex type system
        /// </summary>
        /// <returns></returns>
        private async Task<ComplexTypeSystem> LoadComplexTypeSystemAsync() {
            try {
                if (_session != null && _session.Connected) {
                    var complexTypeSystem = new ComplexTypeSystem(_session);
                    await complexTypeSystem.Load().ConfigureAwait(false);
                    _logger.Information("Session {Name} complex type system loaded",
                        _sessionName);
                    return complexTypeSystem;
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to load complex type system.");
            }
            return null;
        }

        /// <summary>
        /// Queue a notification state change
        /// </summary>
        /// <param name="online"></param>
        private void NotifySubscriptionStateChange(bool online) {
            ThreadPool.QueueUserWorkItem(_ => {
                foreach (var subscription in _subscriptions.Values) {
                    subscription.OnSubscriptionStateChanged(online);
                }
            });
        }

        /// <summary>
        /// Notify about new connectivity state using any status callback registered.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private void NotifyConnectivityStateChange(EndpointConnectivityState state) {
            var previous = _lastState;
            if (previous == state) {
                return;
            }
            if (previous != EndpointConnectivityState.Connecting &&
                previous != EndpointConnectivityState.Ready &&
                state == EndpointConnectivityState.Error) {
                // Do not change state to generic error once we have
                // a specific error state already set...
                _logger.Debug(
                    "Error, connection to {endpoint} - leaving state at {previous}.",
                    _connection.Endpoint.Url, previous);
                return;
            }

            _lastState = state;
            if (_lastState == EndpointConnectivityState.Ready) {
                _connected.Set();
            }
            else {
                _connected.Reset();
            }
            _logger.Information(
                "Connecting to {endpoint} changed from {previous} to {state}",
                _connection.Endpoint.Url, previous, state);
            try {
                OnConnectionStateChange?.Invoke(this, state);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Exception during state callback");
            }
        }

        /// <summary>
        /// Convert exception to connectivity state
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="reconnecting"></param>
        /// <returns></returns>
        public EndpointConnectivityState ToConnectivityState(Exception ex, bool reconnecting = true) {
            var state = EndpointConnectivityState.Error;
            switch (ex) {
                case ServiceResultException sre:
                    switch (sre.StatusCode) {
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
                    _logger.Debug("{result} => {state}", sre.Result, state);
                    break;
                default:
                    state = EndpointConnectivityState.Error;
                    _logger.Debug("{message} => {state}", ex.Message, state);
                    break;
            }
            return state;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="connection"></param>
        private OpcUaClient(IMetricsContext metrics, ConnectionIdentifier connection) {
            if (connection?.Connection?.Endpoint == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (metrics == null) {
                throw new ArgumentNullException(nameof(metrics));
            }

            _connection = connection.Connection;
            TagList = new TagList(metrics.TagList.ToArray().AsSpan()) {
                { "EndpointUrl", _connection.Endpoint.Url },
                { "SecurityMode", _connection.Endpoint.SecurityMode }
            };

            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_connection_retries",
                () => new Measurement<long>(NumberOfConnectRetries, TagList), "Connection attempts",
                "OPC UA connect retries.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_is_connection_ok",
                () => new Measurement<int>(IsConnected ? 1 : 0, TagList), "",
                "OPC UA connection success flag.");
        }

        private SessionReconnectHandler _reconnectHandler;
        private Session _session;
        private Task<ComplexTypeSystem> _complexTypeSystem;
        private EndpointConnectivityState _lastState;
        private bool _disposed;
        private readonly SemaphoreSlim _connecting = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ApplicationConfiguration _configuration;
        private readonly AsyncManualResetEvent _connected;
        private readonly string _sessionName;
        private readonly ConnectionModel _connection;
        private readonly ILogger _logger;
        private readonly DateTime _lastActivity = DateTime.UtcNow;
        private readonly ConcurrentDictionary<string, ISubscription> _subscriptions
            = new ConcurrentDictionary<string, ISubscription>();
    }
}