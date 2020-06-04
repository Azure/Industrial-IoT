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
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session manager
    /// </summary>
    public class DefaultSessionManager : ISessionManager, IDisposable {

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
            _cts = new CancellationTokenSource();
            _runner = Task.Run(() => RunAsync(_cts.Token));
        }

        /// <inheritdoc/>
        public int GetNumberOfConnectionRetries(ConnectionModel connection) {

            var key = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                if (!_sessions.TryGetValue(key, out var wrapper)) {
                    return 0;
                }
                return wrapper.NumberOfConnectRetries;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Session GetOrCreateSession(ConnectionModel connection,
            bool createIfNotExists) {

            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                // try to get an existing session
                if (!_sessions.TryGetValue(id, out var wrapper)) {
                    if (!createIfNotExists) {
                        return null;
                    }
                    wrapper = new SessionWrapper() {
                        MissedKeepAlives = 0,
                        MaxKeepAlives = (int)_clientConfig.MaxKeepAliveCount,
                        State = SessionState.Init,
                        Session = null,
                        ReportedStatus = StatusCodes.Good,
                        IdleCount = 0
                    };
                    _sessions.Add(id, wrapper);
                    _reset?.TrySetResult(true);
                }
                switch (wrapper.State) {
                    case SessionState.Running:
                    case SessionState.Refresh:
                        return wrapper.Session;
                    case SessionState.Retry:
                    case SessionState.Init:
                    case SessionState.Failed:
                    case SessionState.Disconnect:
                        break;
                    default:
                        throw new InvalidOperationException($"Illegal SessionState ({wrapper.State})");
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to get/create as session for Id {id}.", id);
            }
            finally {
                _lock.Release();
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task RemoveSessionAsync(ConnectionModel connection, bool onlyIfEmpty = true) {
            var key = new ConnectionIdentifier(connection);
            await _lock.WaitAsync();
            try {
                if (!_sessions.TryGetValue(key, out var wrapper)) {
                    return;
                }
                if (onlyIfEmpty && wrapper._subscriptions?.Count == 0) {
                    wrapper.State = SessionState.Disconnect;
                    _reset?.TrySetResult(true);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void RegisterSubscription(ISubscription subscription) {
            var id = new ConnectionIdentifier(subscription.Connection);
            _lock.Wait();
            try {
                if (!_sessions.TryGetValue(id, out var wrapper)) {
                    wrapper = new SessionWrapper() {
                        MissedKeepAlives = 0,
                        MaxKeepAlives = (int)_clientConfig.MaxKeepAliveCount,
                        State = SessionState.Init,
                        Session = null,
                        IdleCount = 0
                    };
                    _sessions.Add(id, wrapper);
                }
                wrapper._subscriptions.AddOrUpdate(subscription.Id, subscription);
                _logger.Information("Subscription registration in state {state}", wrapper.State);
                if (wrapper.State == SessionState.Running) {
                    wrapper.State = SessionState.Refresh;
                }
                _reset?.TrySetResult(true);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void UnregisterSubscription(ISubscription subscription) {
            var id = new ConnectionIdentifier(subscription.Connection);
            _lock.Wait();
            try {
                if (!_sessions.TryGetValue(id, out var wrapper)) {
                    return;
                }
                if (wrapper._subscriptions.TryRemove(subscription.Id, out _)) {
                    _logger.Information("Subscription unregistration in state {state}", wrapper.State);
                    if (wrapper.State == SessionState.Running) {
                        wrapper.State = SessionState.Refresh;
                    }
                    _reset?.TrySetResult(true);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() {
            Try.Async(StopAsync).Wait();

            // Dispose
            _cts.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        /// stop all sessions
        /// </summary>
        /// <returns></returns>
        private async Task StopAsync() {

            Try.Op(() => _cts?.Cancel());
            try {
                await _runner;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) {
                _logger.Error(ex, "Unexpected exception stopping processor thread.");
            }

            foreach (var session in _sessions.ToList()) {
                if (!session.Value.Processing.IsCompleted) {
                    await session.Value.Processing;
                }
                await HandleDisconnectAsync(session.Key, session.Value).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Session manager's conmnection management runner task
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct) {

            while (!ct.IsCancellationRequested) {
                _reset = new TaskCompletionSource<bool>();
                foreach (var sessionWrapper in _sessions.ToList()) {
                    var wrapper = sessionWrapper.Value;
                    var id = sessionWrapper.Key;
                    try {
                        switch (wrapper.State) {
                            case SessionState.Refresh:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleRefreshAsync(id, wrapper, ct));
                                }
                                break;
                            case SessionState.Running:
                                // nothing to do
                                break;
                            case SessionState.Retry:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleRetryAsync(id, wrapper, ct));
                                }
                                break;
                            case SessionState.Init:
                            case SessionState.Failed:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleInitAsync(id, wrapper, ct));
                                }
                                break;
                            case SessionState.Disconnect:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleDisconnectAsync(id, wrapper));
                                }
                                break;
                            default:
                                throw new InvalidOperationException($"Illegal SessionState ({wrapper.State})");
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed to process statemachine for Session Id {id}.", sessionWrapper.Key);
                    }
                }

                var delay = Task.Delay(60000, ct);
                await Task.WhenAny(delay, _reset.Task);
                _logger.Information("runner reset delay: {delay} reset:{reset}", delay.IsCompleted, _reset.Task.IsCompleted);
            }
        }

        /// <summary>
        /// Handle retry state of a session
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wrapper"></param>
        /// <param name="ct"></param>
        /// <returns>continue processing</returns>
        private async Task HandleRetryAsync(ConnectionIdentifier id,
            SessionWrapper wrapper, CancellationToken ct) {
            try {
                wrapper.MissedKeepAlives++;
                _logger.Information("Session '{name}' missed {keepAlives} keep alive(s) due to {status}." +
                        " Awaiting for reconnect...", wrapper.Session.SessionName,
                        wrapper.MissedKeepAlives, wrapper.ReportedStatus);
                if (!ct.IsCancellationRequested) {
                    wrapper.Session.Reconnect();
                    wrapper.ReportedStatus = StatusCodes.Good;
                    wrapper.State = SessionState.Running;
                    wrapper.MissedKeepAlives = 0;

                    // reactivate all subscriptons
                    foreach (var subscription in wrapper._subscriptions.Values) {
                        if (!ct.IsCancellationRequested) {
                            await subscription.ActivateAsync(wrapper.Session).ConfigureAwait(false);
                        }
                    }
                }
                return;
            }
            catch (Exception e) {
                wrapper.NumberOfConnectRetries++;
                if (e is ServiceResultException sre) {
                    switch (sre.StatusCode) {
                        case StatusCodes.BadNotConnected:
                        case StatusCodes.BadNoCommunication:
                        case StatusCodes.BadSessionNotActivated:
                        case StatusCodes.BadServerHalted:
                        case StatusCodes.BadServerNotConnected:
                            _logger.Warning("Failed to reconnect session {sessionName}." +
                                " Retry reconnection later.", wrapper.Session.SessionName);
                            if (wrapper.MissedKeepAlives < wrapper.MaxKeepAlives) {
                                // retry later
                                return;
                            }
                            break;
                        default:
                            break;
                    }
                }
                _logger.Warning("Failed to reconnect session {sessionName} due to {exception}." +
                    " Disposing and trying create new.", wrapper.Session.SessionName, e.Message);
            }

            // cleanup the session
            if (wrapper.Session.SubscriptionCount > 0) {
                foreach (var subscription in wrapper.Session.Subscriptions) {
                    Try.Op(() => subscription.DeleteItems());
                    Try.Op(() => subscription.Delete(true));
                }
                Try.Op(() => wrapper.Session.RemoveSubscriptions(wrapper.Session.Subscriptions));
            }
            Try.Op(wrapper.Session.Close);
            Try.Op(wrapper.Session.Dispose);
            wrapper.Session = null;
            wrapper.MissedKeepAlives = 0;
            wrapper.ReportedStatus = StatusCodes.Good;
            wrapper.State = SessionState.Failed;

            await HandleInitAsync(id, wrapper, ct);
        }

        /// <summary>
        /// Handles the initialization state of the session
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wrapper"></param>
        /// <param name="ct"></param>
        /// <returns>continue processing</returns>
        private async Task HandleInitAsync(ConnectionIdentifier id,
            SessionWrapper wrapper, CancellationToken ct) {

            try {
                if (wrapper.Session != null) {
                    _logger.Warning("Session {sessionName} still attached to wrapper in {state}",
                        wrapper.Session.SessionName, wrapper.State);
                    Try.Op(wrapper.Session.Dispose);
                    wrapper.Session = null;
                }
                _logger.Debug("Initializing Session Id '{id}'", id);
                var endpointUrlCandidates = id.Connection.Endpoint.Url.YieldReturn();
                if (id.Connection.Endpoint.AlternativeUrls != null) {
                    endpointUrlCandidates = endpointUrlCandidates.Concat(
                        id.Connection.Endpoint.AlternativeUrls);
                }
                var exceptions = new List<Exception>();
                foreach (var endpointUrl in endpointUrlCandidates) {
                    try {
                        if (!ct.IsCancellationRequested) {
                            var session = await CreateSessionAsync(endpointUrl, id);
                            if (session != null) {
                                _logger.Information("Connected on {endpointUrl}", endpointUrl);
                                session.Handle = wrapper;
                                wrapper.Session = session;
                                foreach (var subscription in wrapper._subscriptions.Values) {
                                    await subscription.EnableAsync(wrapper.Session).ConfigureAwait(false);
                                }
                                foreach (var subscription in wrapper._subscriptions.Values) {
                                    await subscription.ActivateAsync(wrapper.Session).ConfigureAwait(false);
                                }
                                wrapper.State = SessionState.Running;
                                _logger.Debug("Session Id '{id}' successfully initialized", id);
                                return;
                            }
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
            catch (ServiceResultException sre) {
                _logger.Warning("Failed to get or create session {id} due to {exception}.",
                    id, sre.StatusCode.ToString());
            }
            catch (AggregateException aex) {
                _logger.Warning("Failed to get or create session {id} due to {exception}.",
                    id, aex.Message);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to get or create session.");
            }
            wrapper.NumberOfConnectRetries++;
            wrapper.State = SessionState.Failed;
        }

        /// <summary>
        /// Handles the refresh state of a session
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wrapper"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task HandleRefreshAsync(ConnectionIdentifier id,
            SessionWrapper wrapper, CancellationToken ct) {
            try {
                _logger.Debug("Refreshing Session '{name}'", wrapper.Session.SessionName);
                if (wrapper.Session != null) {
                    if (StatusCode.IsGood(wrapper.ReportedStatus)) {
                        if (wrapper.Session.Connected &&
                            !wrapper.Session.KeepAliveStopped) {
                            foreach (var subscription in wrapper._subscriptions.Values) {
                                if (!ct.IsCancellationRequested) {
                                    await subscription.ActivateAsync(wrapper.Session).ConfigureAwait(false);
                                }
                            }
                            _logger.Debug("Refreshing done for Session '{name}'", wrapper.Session.SessionName);
                            return;
                        }
                        wrapper.ReportedStatus = StatusCodes.BadNoCommunication;
                    }
                    wrapper.State = SessionState.Retry;
                    await HandleRetryAsync(id, wrapper, ct);
                }
                else {
                    wrapper.State = SessionState.Failed;
                    await HandleInitAsync(id, wrapper, ct);
                }
            }
            catch (Exception e) {
                _logger.Error(e, "failed to refresh subscription");
            }
        }

        /// <summary>
        /// Handles the disconnect state of a session
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wrapper"></param>
        /// <returns>continue processing</returns>
        private async Task HandleDisconnectAsync(ConnectionIdentifier id, SessionWrapper wrapper) {
            _logger.Debug("Removing idle Session '{name}'", wrapper?.Session?.SessionName);
            await _lock.WaitAsync();
            try {
                _sessions.Remove(id);
            }
            finally {
                _lock.Release();
            }
            try {
                if (wrapper != null && wrapper.Session != null) {
                    wrapper.Session.Handle = null;
                    // Remove subscriptions
                    if (wrapper.Session.SubscriptionCount > 0) {
                        foreach (var subscription in wrapper.Session.Subscriptions) {
                            Try.Op(() => subscription.DeleteItems());
                        }
                        Try.Op(() => wrapper.Session.RemoveSubscriptions(wrapper.Session.Subscriptions));
                    }
                    // close the session
                    Try.Op(wrapper.Session.Close);
                    Try.Op(wrapper.Session.Dispose);
                    wrapper.Session = null;
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Session '{id}' removal failure.", id);
            }
        }


        /// <summary>
        /// Create session against endpoint
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<Session> CreateSessionAsync(string endpointUrl, ConnectionIdentifier id) {

            var sessionName = $"Azure IIoT {id}";

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
                    else if (_clientConfig.AutoAcceptUntrustedCertificates) {
                        _logger.Warning("Publisher is configured to accept untrusted certs.  " +
                            "Accepting untrusted certificate on endpoint {endpointUrl}",
                            endpointUrl);
                        e.Accept = true;
                    }
                }
            };

            var applicationConfiguration = await _clientConfig.
                ToApplicationConfigurationAsync(_identity, true, OnValidate).ConfigureAwait(false);
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
                    userIdentity, null).ConfigureAwait(false);

                if (sessionName != session.SessionName) {
                    _logger.Warning("Session '{sessionName}' created with a revised name '{name}'",
                        sessionName, session.SessionName);
                }
                _logger.Information("Session '{sessionName}' created.", sessionName);

                _logger.Information("Loading Complex Type System....");
                try {
                    var complexTypeSystem = new ComplexTypeSystem(session);
                    await complexTypeSystem.Load().ConfigureAwait(false);
                    _logger.Information("Complex Type system loaded.");
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to load Complex Type System");
                }

                if (_clientConfig.KeepAliveInterval > 0) {
                    session.KeepAliveInterval = _clientConfig.KeepAliveInterval;
                }
                else {
                    // TODO - what happens when KeepAliveInterval is 0???
                    session.KeepAliveInterval = 10000;
                }

                session.KeepAlive += Session_KeepAlive;
                session.Notification += Session_Notification;

                return session;
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
            try {
                if (session.Handle is SessionWrapper wrapper) {
                    if (ServiceResult.IsGood(e.Status)) {
                        wrapper.MissedKeepAlives = 0;
                    }
                    else {
                        wrapper.ReportedStatus = e.Status.Code;
                        wrapper.State = SessionState.Refresh;
                        _reset?.TrySetResult(true);
                        _logger.Information("Session '{name}' schedule to refresh", session.SessionName);
                    }
                    if (session.SubscriptionCount == 0) {
                        if (wrapper.IdleCount < 10) {
                            wrapper.IdleCount++;
                        }
                        else {
                            _logger.Information("Idle Session '{name}' schedule to remove.",
                                   session.SessionName);
                            wrapper.State = SessionState.Disconnect;
                            _reset?.TrySetResult(true);
                        }
                    }
                    else {
                        wrapper.IdleCount = 0;
                    }
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Session '{name}' KeepAlive processing failure.", session.SessionName);
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
            public int MaxKeepAlives { get; set; }

            /// <summary>
            /// the number of connect retries
            /// </summary>
            public int NumberOfConnectRetries { get; set; }

            /// <summary>
            /// Reconnecting
            /// </summary>
            public SessionState State { get; set; }

            /// <summary>
            /// Error status notified
            /// </summary>
            public StatusCode ReportedStatus { get; set; }

            /// <summary>
            /// Idle counter
            /// </summary>
            public int IdleCount { get; set; }

            /// <summary>
            /// currently processing
            /// </summary>
            public Task Processing { get; set; }

            /// <summary>
            /// registered subscriptions
            /// </summary>
            public readonly ConcurrentDictionary<string, ISubscription> _subscriptions =
                new ConcurrentDictionary<string, ISubscription>();
        }

        /// <summary>
        /// The Session state
        /// </summary>
        private enum SessionState {
            Init,
            Failed,
            Running,
            Refresh,
            Retry,
            Disconnect
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

        private Task _runner;
        private TaskCompletionSource<bool> _reset;
        private CancellationTokenSource _cts;

    }
}
