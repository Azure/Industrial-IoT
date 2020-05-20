// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session manager
    /// </summary>
    public class DefaultSessionManager : ISessionManager {

        /// <inheritdoc/>
        public int SessionCount => _sessions.Count;

        /// <summary>
        /// Create session manager
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        public DefaultSessionManager(IClientServicesConfig clientConfig,
            IIdentity identity, ILogger logger) {
            _clientConfig = clientConfig;
            _logger = logger;
            _identity = identity;
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public async Task<Session> GetOrCreateSessionAsync(ConnectionModel connection,
            bool createIfNotExists, bool forceActivation) {

            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            await _lock.WaitAsync();
            try {
                // try to get an existing session
                if (_sessions.TryGetValue(id, out var wrapper)) {
                    if (wrapper != null && 
                        (forceActivation || !wrapper.Session.Connected || 
                        wrapper.MissedKeepAlives >= wrapper.MaxKeepAlives)) {
                        // attempt to reactivate 
                        bool dispose = true;
                        try {
                            wrapper.Session.Reconnect();
                        }
                        catch (ServiceResultException ex) {
                            if (ex.StatusCode == StatusCodes.BadNotConnected) {
                                dispose = false;
                            }
                            else {
                                _logger.Warning("Failed to reconnect session {sessionName}. Dispose and create new",
                                    wrapper.Session.SessionName, ex.Message);
                            }
                        }
                        catch (Exception ex){
                            _logger.Warning("Failed to reconnect session {sessionName}. Dispose and create new",
                                wrapper.Session.SessionName, ex.Message);
                        }
                        finally {
                            if (dispose) {
                                //  todo check status codes
                                _sessions.Remove(id);
                                // Remove subscriptions
                                if (wrapper.Session.SubscriptionCount > 0) {
                                    foreach (var subscription in wrapper.Session.Subscriptions) {
                                        Try.Op(() => subscription.RemoveItems(subscription.MonitoredItems));
                                        Try.Op(() => subscription.DeleteItems());
                                    }
                                    Try.Op(() => wrapper.Session.RemoveSubscriptions(wrapper.Session.Subscriptions));
                                }
                                Try.Op(wrapper.Session.Close);
                                Try.Op(wrapper.Session.Dispose);
                                wrapper = null;
                            }
                        }
                    }
                }

                if (wrapper == null && createIfNotExists) {
                    var endpointUrlCandidates = id.Connection.Endpoint.Url.YieldReturn();
                    if (id.Connection.Endpoint.AlternativeUrls != null) {
                        endpointUrlCandidates = endpointUrlCandidates.Concat(
                            id.Connection.Endpoint.AlternativeUrls);
                    }
                    var exceptions = new List<Exception>();
                    foreach (var endpointUrl in endpointUrlCandidates) {
                        try {
                            wrapper = await CreateSessionAsync(endpointUrl, id);
                            if (wrapper?.Session != null) {
                                _logger.Information("Connected on {endpointUrl}", endpointUrl);
                                _sessions.Add(id, wrapper);
                                return wrapper?.Session;
                            }
                        }
                        catch (Exception ex) {
                            _logger.Debug("Failed to connect on {endpointUrl}: {message} - try again...",
                                endpointUrl, ex.Message);
                            exceptions.Add(ex);
                        }
                    }
                    throw new AggregateException(exceptions);
                }
                return wrapper?.Session;
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to get or create session.");
                return null;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Create session against endpoint
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<SessionWrapper> CreateSessionAsync(string endpointUrl,
            ConnectionIdentifier id) {
            var sessionName = id.ToString();

            // Validate certificates
            void OnValidate(CertificateValidator sender, CertificateValidationEventArgs e) {
                if (!e.Accept && e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                    // Validate thumbprint provided
                    if (e.Certificate.RawData != null &&
                        id.Connection.Endpoint.Certificate != null &&
                        e.Certificate.Thumbprint == id.Connection.Endpoint.Certificate) {
                        // Validate
                        e.Accept = true;
                    }
                    else if(_clientConfig.AutoAcceptUntrustedCertificates) {
                        _logger.Warning("Publisher is configured to accept untrusted certs.  " +
                            "Accepting untrusted certificate on endpoint {endpointUrl}",
                            endpointUrl);
                        e.Accept = true;
                    }
                }
            };

            var applicationConfiguration = await _clientConfig.
                ToApplicationConfigurationAsync(_identity, true, OnValidate);
            var endpointConfiguration = _clientConfig.ToEndpointConfiguration();

            var endpointDescription = SelectEndpoint(endpointUrl,
                id.Connection.Endpoint.SecurityMode, id.Connection.Endpoint.SecurityPolicy,
                (int)(id.Connection.OperationTimeout.HasValue ?
                    id.Connection.OperationTimeout.Value.TotalMilliseconds :
                    kDefaultOperationTimeout));

            if (endpointDescription == null) {
                throw new EndpointNotAvailableException(endpointUrl,
                    id.Connection.Endpoint.SecurityMode, id.Connection.Endpoint.SecurityPolicy);
            }

            if (id.Connection.Endpoint.SecurityMode.HasValue &&
                id.Connection.Endpoint.SecurityMode != SecurityMode.None &&
                endpointDescription.SecurityMode == MessageSecurityMode.None) {
                _logger.Warning("Although the use of security was configured, " +
                    "there was no security-enabled endpoint available at url " +
                    "{endpointUrl}. An endpoint with no security will be used.",
                    endpointUrl);
            }

            var configuredEndpoint = new ConfiguredEndpoint(
                null, endpointDescription, endpointConfiguration);

            _logger.Information("Trying to create session {sessionName}...",
                sessionName);
            using (new PerfMarker(_logger, sessionName)) {
                var userIdentity = id.Connection.User.ToStackModel() ??
                    new UserIdentity(new AnonymousIdentityToken());
                var session = await Session.Create(
                    applicationConfiguration, configuredEndpoint,
                    true, sessionName, _clientConfig.DefaultSessionTimeout,
                    userIdentity, null);

                if (sessionName != session.SessionName) {
                    _logger.Warning("Session '{sessionName}' created with a revised name '{name}'",
                        sessionName, session.SessionName);
                }
                _logger.Information("Session '{sessionName}' created.", sessionName);
                
                _logger.Information("Loading Complex Type System....");
                try {
                    var complexTypeSystem = new ComplexTypeSystem(session);
                    await complexTypeSystem.Load();
                    _logger.Information("Complex Type system loaded.");
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to load Complex Type System");
                }

                if (_clientConfig.KeepAliveInterval > 0) {
                    session.KeepAliveInterval = _clientConfig.KeepAliveInterval;
                    session.KeepAlive += Session_KeepAlive;
                    session.Notification += Session_Notification;
                }
                var wrapper = new SessionWrapper {
                    MissedKeepAlives = 0,
                    MaxKeepAlives = _clientConfig.MaxKeepAliveCount,
                    Session = session
                };
                return wrapper;
            }
        }

        /// <inheritdoc/>
        public async Task RemoveSessionAsync(ConnectionModel connection, bool onlyIfEmpty = true) {
            var key = new ConnectionIdentifier(connection);
            await _lock.WaitAsync();
            try {
                if (!_sessions.TryGetValue(key, out var wrapper) || wrapper?.Session == null) {
                    return;
                }
                var session = wrapper.Session;
                if (onlyIfEmpty && session.SubscriptionCount > 0) {
                    return;
                }
                _sessions.Remove(key);

                // Remove subscriptions
                if (session.SubscriptionCount > 0) {
                    foreach (var subscription in session.Subscriptions){
                        Try.Op(() => subscription.RemoveItems(subscription.MonitoredItems));
                        Try.Op(() => subscription.DeleteItems());
                    }
                    Try.Op(() => session.RemoveSubscriptions(session.Subscriptions));
                }
                Try.Op(session.Close);
                Try.Op(session.Dispose);
            }
            finally {
                _lock.Release();
            }
        }
        /// <summary>
        /// callback to report session's notifications
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void Session_Notification(Session session, NotificationEventArgs e) {
            _logger.Debug("Notification for session: {Session}, subscription {Subscription} -sequence# {Sequence}-{PublishTime}",
                session.SessionName, e.Subscription?.DisplayName, e.NotificationMessage?.SequenceNumber,
                e.NotificationMessage.PublishTime);
            if (e.NotificationMessage.IsEmpty || e.NotificationMessage.NotificationData.Count() == 0) {
                var keepAlive = new DataChangeNotification() {
                    MonitoredItems = new MonitoredItemNotificationCollection() {
                        new MonitoredItemNotification() {
                            ClientHandle = 0,
                            Value = null,
                            Message = e.NotificationMessage
                        }
                    }
                };
                e.Subscription.FastDataChangeCallback.Invoke(e.Subscription, keepAlive, e.StringTable);
            }
        }

        /// <summary>
        /// Handle keep alives
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void Session_KeepAlive(Session session, KeepAliveEventArgs e) {
            _logger.Debug("Keep Alive received from session {name}, state: {state}.",
                session.SessionName, e.CurrentState);
            _lock.Wait();
            try {
                foreach (var entry in _sessions
                    .Where(s => s.Value.Session.SessionId == session.SessionId).ToList()) {
                    if (ServiceResult.IsGood(e.Status)) {
                        entry.Value.MissedKeepAlives = 0;
                    }
                    else {
                        entry.Value.MissedKeepAlives++;
                        if (entry.Value.MissedKeepAlives >= entry.Value.MaxKeepAlives) {
                            _logger.Warning("Session '{name}' exceeded max keep alive count. " +
                                "Disconnecting and removing session...", session.SessionName);
                            // TODO - implement a session level retry mechanism.
                            // for now rely on the subscription's reconnection logic
                            /*
                            _sessions.Remove(entry.Key);
                            // Remove subscriptions
                            if (session.SubscriptionCount > 0) {
                                foreach (var subscription in session.Subscriptions) {
                                    Try.Op(() => subscription.RemoveItems(subscription.MonitoredItems));
                                    Try.Op(() => subscription.DeleteItems());
                                }
                                Try.Op(() => session.RemoveSubscriptions(session.Subscriptions));
                            }
                            Try.Op(session.Close);
                            Try.Op(session.Dispose);
                            */
                        }
                        _logger.Warning("{missedKeepAlives}/{_maxKeepAlives} missed keep " +
                            "alives from session '{name}'...",
                            entry.Value.MissedKeepAlives, entry.Value.MaxKeepAlives, session.SessionName);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        private static MessageSecurityMode? ToMessageSecurityMode(SecurityMode? securityMode) {
            if (!securityMode.HasValue) {
                return null;
            }
            switch (securityMode.Value) {
                case SecurityMode.Best:
                    throw new NotSupportedException("The security mode 'best' is not supported.");
                case SecurityMode.None:
                    return MessageSecurityMode.None;
                case SecurityMode.Sign:
                    return MessageSecurityMode.Sign;
                case SecurityMode.SignAndEncrypt:
                    return MessageSecurityMode.SignAndEncrypt;
                default:
                    throw new NotSupportedException($"The security mode '{securityMode}' is not implemented.");
            }
        }

        /// <summary>
        /// Selects an endpoint based on the given url and security parameters.
        /// </summary>
        /// <param name="discoveryUrl">The discovery url of the server.</param>
        /// <param name="securityMode">The requested message security mode.</param>
        /// <param name="securityPolicyUri">The requested securityPolicyUrl.</param>
        /// <param name="operationTimeout">Operation timeout</param>
        /// <returns>Endpoint with the selected security settings or null of none
        /// available.</returns>
        private static EndpointDescription SelectEndpoint(string discoveryUrl,
            SecurityMode? securityMode, string securityPolicyUri, int operationTimeout = -1) {
            // if no security settings are specified or is set to 'Best', we use the best
            // available. However, this can result in an endpoint with SecurityMode = None when no
            // security enabled endpoint is available.
            if ((!securityMode.HasValue && string.IsNullOrWhiteSpace(securityPolicyUri)) ||
                securityMode == SecurityMode.Best) {
                return CoreClientUtils.SelectEndpoint(discoveryUrl, true, operationTimeout);
            }
            else if (securityMode == SecurityMode.None || securityPolicyUri == SecurityPolicies.None) {
                return CoreClientUtils.SelectEndpoint(discoveryUrl, false, operationTimeout);
            }
            else {
                return SelectEndpoint(discoveryUrl, ToMessageSecurityMode(securityMode),
                    securityPolicyUri, operationTimeout);
            }
        }

        /// <summary>
        /// Selects an endpoint based on the given url and security parameters.
        /// </summary>
        /// <param name="discoveryUrl">The discovery url of the server.</param>
        /// <param name="messageSecurityMode">The requested message security mode.</param>
        /// <param name="securityPolicyUri">The requested securityPolicyUrl.</param>
        /// <param name="operationTimeout">Operation timeout</param>
        /// <returns>Endpoint with the selected security settings or null of none available.</returns>
        private static EndpointDescription SelectEndpoint(string discoveryUrl,
            MessageSecurityMode? messageSecurityMode, string securityPolicyUri, int operationTimeout = -1) {
            if (messageSecurityMode == MessageSecurityMode.None || securityPolicyUri == SecurityPolicies.None) {
                return CoreClientUtils.SelectEndpoint(discoveryUrl, false, operationTimeout);
            }

            // needs to add the '/discovery' back onto non-UA TCP URLs.
            if (discoveryUrl.StartsWith(Utils.UriSchemeHttps)) {
                if (!discoveryUrl.EndsWith("/discovery")) {
                    discoveryUrl += "/discovery";
                }
            }

            // parse the selected URL.
            var uri = new Uri(discoveryUrl);

            var configuration = EndpointConfiguration.Create();
            if (operationTimeout > 0) {
                configuration.OperationTimeout = operationTimeout;
            }

            // Connect to the server's discovery endpoint and find the available configuration.
            using (var client = DiscoveryClient.Create(uri, configuration)) {
                var endpoints = client.GetEndpoints(null);

                IEnumerable<EndpointDescription> filteredEndpoints = endpoints.ToArray();

                if (messageSecurityMode.HasValue) {
                    filteredEndpoints = filteredEndpoints
                        .Where(e => e.SecurityMode == messageSecurityMode.Value);
                }

                if (!string.IsNullOrWhiteSpace(securityPolicyUri)) {
                    filteredEndpoints = filteredEndpoints
                        .Where(e => e.SecurityPolicyUri == securityPolicyUri);
                }

                return filteredEndpoints.OrderByDescending(e => e.SecurityLevel).FirstOrDefault();
            }
        }

        /// <summary>
        /// Wraps a session and keep alive information
        /// </summary>
        private sealed class SessionWrapper {

            /// <summary>
            /// Session
            /// </summary>
            public Session Session { get; set; }

            /// <summary>
            /// Missed keep alives
            /// </summary>
            public int MissedKeepAlives { get; set; }

            /// <summary>
            /// Missed keep alives
            /// </summary>
            public uint MaxKeepAlives { get; set; }
        }

        /// <summary>
        /// Get endpoint from session
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static EndpointIdentifier GetEndpointId(SessionClient session) {
            var endpointModel = new EndpointModel {
                Url = session.Endpoint.EndpointUrl.TrimEnd('/'),
                SecurityPolicy = session.Endpoint.SecurityPolicyUri
            };
            switch (session.Endpoint.SecurityMode) {
                case MessageSecurityMode.Invalid:
                    throw new Exception("Invalid security mode: invalid");
                case MessageSecurityMode.None:
                    endpointModel.SecurityMode = SecurityMode.None;
                    break;
                case MessageSecurityMode.Sign:
                    endpointModel.SecurityMode = SecurityMode.Sign;
                    break;
                case MessageSecurityMode.SignAndEncrypt:
                    endpointModel.SecurityMode = SecurityMode.SignAndEncrypt;
                    break;
            }
            return new EndpointIdentifier(endpointModel);
        }

        private readonly ILogger _logger;
        private readonly IClientServicesConfig _clientConfig;
        private readonly IIdentity _identity;
        private readonly Dictionary<ConnectionIdentifier, SessionWrapper> _sessions =
            new Dictionary<ConnectionIdentifier, SessionWrapper>();
        private readonly SemaphoreSlim _lock;
        private const int kDefaultOperationTimeout = 15000;
    }
}