// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// OPC UA Client with examples of basic functionality.
    /// Keep in sync with Apollo
    /// </summary>
    public class OpcUaClient : IDisposable, ISessionHandle {

        /// <inheritdoc/>
        public ISession Session => _session;

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
        /// Complex type system
        /// </summary>
        public ComplexTypeSystem ComplexTypeSystem { get; internal set; }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public OpcUaClient(ApplicationConfiguration configuration,
            ConnectionIdentifier connection, ILogger logger, string sessionName = null) {
            if (connection?.Connection?.Endpoint == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            _connection = connection.Connection;
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
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> ConnectAsync(CancellationToken ct = default) {
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

            if (connected) {
                // Apply subscription settings for existing subscriptions
                foreach (var subscription in _subscriptions.Values) {
                    await subscription.ReapplyToSessionAsync(this);
                }
            }

            foreach (var subscription in _subscriptions.Values) {
                subscription.OnSubscriptionStateChanged(connected);
            }
            return connected;
        }

        /// <inheritdoc/>
        public void Dispose() {
            try {
                if (_session != null) {
                    _logger.Information("Disconnecting session {Name}...", _sessionName);

                    _lock.Wait();
                    try {
                        _session.KeepAlive -= Session_KeepAlive;
                        _reconnectHandler?.Dispose();
                    }
                    finally {
                        _lock.Release();
                    }

                    _session.Close();
                    _session.Dispose();
                    _session = null;

                    // Log Session Disconnected event
                    _logger.Debug("Session {Name} disconnected.", _sessionName);
                }
            }
            catch (Exception ex) {
                // Log Error
                _logger.Error(ex, "Disconnect Error for session {Name}.", _sessionName);
            }
            finally {
                _lock.Dispose();
                NumberOfConnectRetries = 0;
            }
        }

        /// <summary>
        /// Connect client (no lock)
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ConnectAsyncCore() {
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
            if (session != null && session.Connected) {
                _session = session;

                // override keep alive interval
                _session.KeepAliveInterval = KeepAliveInterval;

                // set up keep alive callback.
                _session.KeepAlive += Session_KeepAlive;

                _logger.Debug("Session {Name} created, loading complex type system ... ",
                    _sessionName);

                ComplexTypeSystem = new ComplexTypeSystem(session);
                await ComplexTypeSystem.Load().ConfigureAwait(false);

                _logger.Information("Session {Name} complex type system loaded",
                    _sessionName);
            }

            // Session created successfully.
            _logger.Debug("New Session created with name {Name}", _sessionName);
            NumberOfConnectRetries++;
            return true;
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
                    foreach (var subscription in _subscriptions.Values) {
                        subscription.OnSubscriptionStateChanged(false);
                    }
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

            _lock.Wait();
            try {
                // if session recovered, Session property is not null
                if (_reconnectHandler.Session != null) {
                    _session = _reconnectHandler.Session as Session;
                }

                _reconnectHandler.Dispose();
                _reconnectHandler = null;

                if (!IsConnected) {
                    // Failed to reconnect.
                    return;
                }

                _logger.Information("--- SESSION {Name} RECONNECTED ---", _sessionName);
                NumberOfConnectRetries++;
            }
            finally {
                _lock.Release();
            }

            // Go back online
            foreach (var subscription in _subscriptions.Values) {
                subscription.OnSubscriptionStateChanged(IsConnected);
            }
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
        private readonly ILogger _logger;
    }
}