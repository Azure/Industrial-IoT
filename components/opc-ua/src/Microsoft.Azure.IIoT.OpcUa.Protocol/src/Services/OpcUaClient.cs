// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Utils;
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
    public class OpcUaClient : IDisposable, ISessionHandle, IMetricsContext {

        /// <inheritdoc/>
        public ISession Session => _session;

        /// <inheritdoc/>
        public TagList TagList { get; }

        /// <inheritdoc/>
        public ConnectionModel Connection { get; }

        /// <inheritdoc/>
        public EndpointConnectivityState State { get; private set; }

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
        internal bool IsConnected => _session != null && _session.Connected;

        /// <summary>
        /// Whether the connect operation is in progress
        /// </summary>
        internal bool IsConnecting => _connecting.CurrentCount == 0;

        /// <summary>
        /// Whether the connect operation is in progress
        /// </summary>
        internal bool HasSubscriptions => !_subscriptions.IsEmpty;

        /// <summary>
        /// Check if session is active
        /// </summary>
        public bool IsActive => HasSubscriptions ||
            _created + TimeSpan.FromSeconds(SessionLifeTime) <= DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public OpcUaClient(ApplicationConfiguration configuration,
            ConnectionIdentifier connection, ILogger logger, IMetricsContext metrics,
            string sessionName = null) : this (metrics, connection) {

            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));

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
                _logger.Error(ex, "Create Session Error");
                _session?.Dispose();
                _session = null;
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
            }

            NotifySubscriptionStateChange(connected);
            return connected;
        }

        /// <inheritdoc/>
        public void Dispose() {
            Session session;
            List<ISubscription> subscriptions;
            _lock.Wait();
            try {
                _reconnectHandler?.Dispose();
                subscriptions = _subscriptions.Values.ToList();
                _subscriptions.Clear();

                session = _session;
                _session = null;
                if (session != null) {
                    session.Handle = null;
                    session.KeepAlive -= Session_KeepAlive;
                }
            }
            finally {
                _lock.Release();
                NumberOfConnectRetries = 0;
            }

            try {
                _logger.Information("Disconnecting session {Name}...", _sessionName);

                if (subscriptions.Count > 0) {
                    //
                    // Close all subscriptions. Since this might call back into
                    // the session manager, queue this to the thread pool
                    //
                    ThreadPool.QueueUserWorkItem(_ => {
                        foreach (var subscription in _subscriptions.Values) {
                            Try.Op(() => subscription.Dispose());
                        }
                    });
                }

                session.Close();
                session.Dispose();
                // Log Session Disconnected event
                _logger.Debug("Session {Name} disconnected.", _sessionName);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Disconnect Error for session {Name}.", _sessionName);
            }
            finally {
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
                return false;
            }

            if (IsConnected) {
                // Nothing to do
                return true;
            }

            //
            // Get the endpoint by connecting to server's discovery endpoint.
            // Try to find the first endpoint with security.
            //
            var endpointDescription = CoreClientUtils.SelectEndpoint(_configuration,
                _connection.Endpoint.Url,
                _connection.Endpoint.SecurityMode != SecurityMode.None);
            var endpointConfiguration = EndpointConfiguration.Create(_configuration);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription,
                endpointConfiguration);

            if (_connection.Endpoint.SecurityMode.HasValue &&
                _connection.Endpoint.SecurityMode != SecurityMode.None &&
                endpointDescription.SecurityMode == MessageSecurityMode.None) {
                _logger.Warning("Although the use of security was configured, " +
                    "there was no security-enabled endpoint available at url " +
                    "{endpointUrl}. An endpoint with no security will be used.",
                    _connection.Endpoint.Url);
            }

            _logger.Information("Creating session '{Name}' for endpoint '{endpointUrl}'...",
                _sessionName, _connection.Endpoint.Url);
            var userIdentity = _connection.User.ToStackModel()
                ?? new UserIdentity(new AnonymousIdentityToken());

            // Create the session
            var session = await Opc.Ua.Client.Session.Create(_configuration, endpoint,
                false, false, _sessionName, SessionLifeTime, userIdentity,
                null).ConfigureAwait(false);

            // Assign the created session
            SetSession(session);

            // Session created successfully.
            _logger.Debug("New Session created with name {Name}", _sessionName);
            NumberOfConnectRetries++;
            return true;
        }

        /// <summary>
        /// Load complex type system
        /// </summary>
        /// <returns></returns>
        public async ValueTask<ComplexTypeSystem> GetComplexTypeSystemAsync() {
            await _lock.WaitAsync();
            try {
                if (_complexTypeSystem == null) {
                    if (_session != null && _session.Connected) {
                        _complexTypeSystem = new ComplexTypeSystem(_session);
                        await _complexTypeSystem.Load().ConfigureAwait(false);
                        _logger.Information("Session {Name} complex type system loaded",
                            _sessionName);
                    }
                }
                return _complexTypeSystem;
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
                SetSession(_reconnectHandler.Session as Session);
            }
            if (!IsConnected) {
                // Failed to reconnect.
                return;
            }

            _logger.Information("--- SESSION {Name} RECONNECTED ---", _sessionName);
            NumberOfConnectRetries++;

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
                if (_session != null) {
                    _complexTypeSystem = null;
                    _session.Handle = null;
                    _session.KeepAlive -= Session_KeepAlive;
                    _session.Dispose();
                }

                // override keep alive interval
                _session = session;
                _session.KeepAliveInterval = KeepAliveInterval;

                // set up keep alive callback.
                _session.KeepAlive += Session_KeepAlive;
                _session.Handle = this;
            }
            finally {
                _lock.Release();
            }
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

            Diagnostics.Meter.CreateObservableUpDownCounter("iiot_edge_publisher_connection_retries",
                () => new Measurement<int>(NumberOfConnectRetries, TagList), "Connection attempts",
                "OPC UA connect retries.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_is_connection_ok",
                () => new Measurement<int>(IsConnected ? 1 : 0, TagList), "",
                "OPC UA connection success flag.");
        }

        private readonly ConcurrentDictionary<string, ISubscription> _subscriptions
            = new ConcurrentDictionary<string, ISubscription>();
        private SemaphoreSlim _connecting = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private ApplicationConfiguration _configuration;
        private readonly string _sessionName;
        private readonly ConnectionModel _connection;
        private SessionReconnectHandler _reconnectHandler;
        private Session _session;
        private ComplexTypeSystem _complexTypeSystem;
        private readonly ILogger _logger;
        private readonly DateTime _created = DateTime.UtcNow;
    }
}