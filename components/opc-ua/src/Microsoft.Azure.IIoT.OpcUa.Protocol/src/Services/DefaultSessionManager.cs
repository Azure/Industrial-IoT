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
            _applicationConfiguration = _clientConfig
                .BuildApplicationConfigurationAsync(
                    _identity,
                    OnValidate,
                    _logger)
                .GetAwaiter()
                .GetResult();
            _endpointConfiguration = _clientConfig.ToEndpointConfiguration();

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
        public bool IsConnectionOk(ConnectionModel connection) {
            var key = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                if (!_sessions.TryGetValue(key, out var wrapper)) {
                    return false;
                }
                return wrapper.State == SessionState.Running;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public ISession GetOrCreateSession(ConnectionModel connection,
            bool ensureWorkingSession) {

            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                // try to get an existing session
                if (!_sessions.TryGetValue(id, out var wrapper)) {
                    if (!ensureWorkingSession) {
                        return null;
                    }
                    wrapper = new SessionWrapper() {
                        Id = id.ToString(),
                        MissedKeepAlives = 0,
                        // TODO this seem not to be the appropriate configuration argument
                        MaxKeepAlives = (int)_clientConfig.MaxKeepAliveCount,
                        State = SessionState.Init,
                        Session = null,
                        ReportedStatus = StatusCodes.Good,
                        IdleCount = 0
                    };
                    _sessions.AddOrUpdate(id, wrapper);
                    TriggerKeepAlive();
                }
                if (!ensureWorkingSession) {
                    return wrapper.Session;
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
                _logger.Error(ex, "Failed to get/create session for Id '{id}'", id);
            }
            finally {
                _lock.Release();
            }
            return null;
        }

        /// <inheritdoc/>
        public ComplexTypeSystem GetComplexTypeSystem(ISession session) {
            return (session?.Handle as SessionWrapper)?.ComplexTypeSystem;
        }

        /// <inheritdoc/>
        public async Task RemoveSessionAsync(ConnectionModel connection, bool onlyIfEmpty = true) {
            var key = new ConnectionIdentifier(connection);
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                if (!_sessions.TryGetValue(key, out var wrapper)) {
                    return;
                }
                if (onlyIfEmpty && wrapper._subscriptions.IsEmpty) {
                    wrapper.State = SessionState.Disconnect;
                    TriggerKeepAlive();
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
                        Id = id.ToString(),
                        MissedKeepAlives = 0,
                        // TODO this seem not to be the appropriate configuration argument
                        MaxKeepAlives = (int)_clientConfig.MaxKeepAliveCount,
                        State = SessionState.Init,
                        Session = null,
                        IdleCount = 0
                    };
                    _sessions.AddOrUpdate(id, wrapper);
                }

                wrapper._subscriptions.AddOrUpdate(subscription.Name, subscription);
                _logger.Information("Subscription '{subscriptionId}' registered/updated in session '{id}' in state {state}",
                    subscription.Name, id, wrapper.State);
                if (wrapper.State == SessionState.Running) {
                    wrapper.State = SessionState.Refresh;
                }
                TriggerKeepAlive();
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
            var id = new ConnectionIdentifier(subscription.Connection);
            _lock.Wait();
            try {
                if (!_sessions.TryGetValue(id, out var wrapper)) {
                    return;
                }
                if (wrapper._subscriptions.TryRemove(subscription.Name, out _)) {
                    _logger.Information("Subscription '{subscriptionId}' unregistered from session '{sessionId}' in state {state}",
                        subscription.Name, id, wrapper.State);
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
        public async Task StopAsync() {
            var processingTasks = new List<Task>();
            try {
                _logger.Information("Stopping all sessions");
                foreach (var session in _sessions.ToList()) {
                    session.Value.State = SessionState.Disconnect;
                    processingTasks.Add(session.Value.Processing);
                }
                TriggerKeepAlive();
                Try.Op(() => _cts?.Cancel());
                await _runner.ConfigureAwait(false);
                _logger.Information("Successfully stopped all sessions");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) {
                _logger.Error(ex, "Unexpected exception stopping processor thread");
            }
            finally {
                await Task.WhenAll(processingTasks);
            }
        }

        /// <summary>
        /// Session manager's connection management runner task
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct) {

            while (!ct.IsCancellationRequested) {
                _triggerKeepAlive = new TaskCompletionSource<bool>();
                foreach (var sessionWrapper in _sessions.ToList()) {
                    var wrapper = sessionWrapper.Value;
                    var id = sessionWrapper.Key;
                    try {
                        switch (wrapper.State) {
                            case SessionState.Refresh:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleRefreshAsync(id, wrapper, ct), ct);
                                }
                                break;
                            case SessionState.Running:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleRefreshAsync(id, wrapper, ct), ct);
                                }
                                break;
                            case SessionState.Retry:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleRetryAsync(id, wrapper, ct), ct);
                                }
                                break;
                            case SessionState.Init:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleInitAsync(id, wrapper, ct), ct);
                                }
                                break;
                            case SessionState.Failed:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleFailedAsync(id, wrapper, ct), ct);
                                }
                                break;
                            case SessionState.Disconnect:
                                if (wrapper.Processing == null || wrapper.Processing.IsCompleted) {
                                    wrapper.Processing = Task.Run(() => HandleDisconnectAsync(id, wrapper), ct);
                                }
                                break;
                            default:
                                throw new InvalidOperationException($"Illegal SessionState ({wrapper.State})");
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Failed to process statemachine for session Id '{id}'", sessionWrapper.Key);
                    }
                }

                var delay = Task.Delay(_clientConfig.KeepAliveInterval, ct);
                await Task.WhenAny(delay, _triggerKeepAlive.Task).ConfigureAwait(false);
                _logger.Debug("Runner Keepalive reset due to {delay} {trigger}",
                    delay.IsCompleted ? "checkAlive" : string.Empty,
                    _triggerKeepAlive.Task.IsCompleted ? "triggerKeepAlive" : string.Empty);
            }
        }

        /// <summary>
        /// Handle retry state of a session
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wrapper"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task HandleRetryAsync(ConnectionIdentifier id,
            SessionWrapper wrapper, CancellationToken ct) {
            try {

                if (!wrapper._subscriptions.Any()) {
                    // if the session is idle, just drop it immediately
                    _logger.Information("Idle expired session '{id}' set to {disconnect} state from {state}",
                        id, SessionState.Disconnect, wrapper.State);
                    wrapper.State = SessionState.Disconnect;
                    await HandleDisconnectAsync(id, wrapper).ConfigureAwait(false);
                    return;
                }
                else {
                    wrapper.IdleCount = 0;
                }

                wrapper.MissedKeepAlives++;
                _logger.Information("Session '{id}' missed {keepAlives} Keepalive(s) due to {status}, " +
                        "waiting to reconnect...", id, wrapper.MissedKeepAlives, wrapper.ReportedStatus);
                if (!ct.IsCancellationRequested) {
                    wrapper.Session.Reconnect();
                    wrapper.ReportedStatus = StatusCodes.Good;
                    wrapper.State = SessionState.Running;
                    wrapper.MissedKeepAlives = 0;

                    // reactivate all subscriptions
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
                        case StatusCodes.BadInvalidState:
                        case StatusCodes.BadRequestTimeout:
                            _logger.Warning("Failed to reconnect session '{id}' due to {exception}, " +
                                "will retry later", id, e.Message);
                            if (wrapper.MissedKeepAlives < wrapper.MaxKeepAlives) {
                                // retry later
                                return;
                            }
                            break;
                        default:
                            break;
                    }
                }
                _logger.Warning("Failed to reconnect session '{id}' due to {exception}, " +
                    " disposing and trying to create new", id, e.Message);
            }

            // cleanup the session
            if (wrapper.Session.SubscriptionCount > 0) {
                foreach (var subscription in wrapper.Session.Subscriptions) {
                    Try.Op(() => subscription.DeleteItems());
                    Try.Op(() => subscription.Delete(true));
                }
                Try.Op(() => wrapper.Session.RemoveSubscriptions(wrapper.Session.Subscriptions.ToList()));
            }
            Try.Op(wrapper.Session.Close);
            Try.Op(wrapper.Session.Dispose);
            wrapper.Session = null;
            wrapper.MissedKeepAlives = 0;
            wrapper.ReportedStatus = StatusCodes.Good;
            wrapper.State = SessionState.Failed;

            await HandleInitAsync(id, wrapper, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the failed state of the session
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wrapper"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task HandleFailedAsync(ConnectionIdentifier id,
            SessionWrapper wrapper, CancellationToken ct) {
            try {
                // check if session requires cleanup
                if (!wrapper._subscriptions.Any()) {
                    if (wrapper.IdleCount < wrapper.MaxKeepAlives) {
                        wrapper.IdleCount++;
                    }
                    else {
                        _logger.Information("Session '{id}' set to {disconnect} state from {state}",
                               id, SessionState.Disconnect, wrapper.State);
                        wrapper.State = SessionState.Disconnect;
                        await HandleDisconnectAsync(id, wrapper).ConfigureAwait(false);
                        return;
                    }
                }
                else {
                    wrapper.IdleCount = 0;
                }
                if (!ct.IsCancellationRequested) {
                    await HandleInitAsync(id, wrapper, ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to reinitiate failed session");
            }
        }

        /// <summary>
        /// Handles the initialization state of the session
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wrapper"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task HandleInitAsync(ConnectionIdentifier id,
        SessionWrapper wrapper, CancellationToken ct) {
            try {
                if (wrapper.Session != null) {
                    _logger.Warning("Session '{id}' still attached to wrapper in {state}",
                        id, wrapper.State);
                    Try.Op(wrapper.Session.Dispose);
                    wrapper.Session = null;
                }
                if (!wrapper._subscriptions.Any()) {
                    // The session is idle, stop initialization phase
                    _logger.Information("Idle failed session '{id}' set to {disconnect} state from {state}",
                        wrapper.Id, SessionState.Disconnect, wrapper.State);
                    wrapper.State = SessionState.Disconnect;
                    TriggerKeepAlive();
                    return;
                }
                _logger.Debug("Initializing session '{id}'...", id);
                var endpointUrlCandidates = id.Connection.Endpoint.Url.YieldReturn();
                if (id.Connection.Endpoint.AlternativeUrls != null) {
                    endpointUrlCandidates = endpointUrlCandidates.Concat(
                        id.Connection.Endpoint.AlternativeUrls);
                }
                var exceptions = new List<Exception>();
                foreach (var endpointUrl in endpointUrlCandidates) {
                    try {
                        if (!ct.IsCancellationRequested) {
                            var (session, complexTypeSystem) = await CreateSessionAsync(endpointUrl, id, ct).ConfigureAwait(false);
                            if (session != null) {
                                _logger.Information("Connected to '{endpointUrl}'", endpointUrl);
                                session.Handle = wrapper;
                                wrapper.Session = session;
                                wrapper.ComplexTypeSystem = complexTypeSystem;
                                foreach (var subscription in wrapper._subscriptions.Values) {
                                    await subscription.EnableAsync(wrapper.Session).ConfigureAwait(false);
                                }
                                foreach (var subscription in wrapper._subscriptions.Values) {
                                    await subscription.ActivateAsync(wrapper.Session).ConfigureAwait(false);
                                }
                                wrapper.State = SessionState.Running;
                                _logger.Debug("Session '{id}' successfully initialized", id);
                                return;
                            }
                        }
                    }
                    catch (Exception ex) {
                        _logger.Debug("Failed to connect to {endpointUrl}: {message} - try again...",
                            endpointUrl, ex.Message);
                        exceptions.Add(ex);
                    }
                }
                throw new AggregateException(exceptions);
            }
            catch (ServiceResultException sre) {
                _logger.Warning("Failed to create session '{id}' due to {exception}",
                    id, sre.StatusCode.ToString());
            }
            catch (AggregateException aex) {
                _logger.Warning("Failed to create session '{id}' due to {exception}",
                    id, aex.Message);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to create session '{id}'", id);
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
                _logger.Debug("Refreshing session '{id}'", id);
                if (wrapper.Session != null) {
                    if (StatusCode.IsGood(wrapper.ReportedStatus)) {
                        if (wrapper.Session.Connected &&
                            !wrapper.Session.KeepAliveStopped) {
                            // Fetch namespaces in case, there was some new Uris added to the server
                            wrapper.Session.FetchNamespaceTables();
                            foreach (var subscription in wrapper._subscriptions.Values) {
                                if (!ct.IsCancellationRequested) {
                                    await subscription.ActivateAsync(wrapper.Session).ConfigureAwait(false);
                                }
                            }
                            _logger.Debug("Refreshing done for session '{id}'", id);
                            wrapper.State = SessionState.Running;
                            return;
                        }
                        wrapper.ReportedStatus = StatusCodes.BadNoCommunication;
                    }
                    wrapper.State = SessionState.Retry;
                    await HandleRetryAsync(id, wrapper, ct).ConfigureAwait(false);
                }
                else {
                    wrapper.State = SessionState.Failed;
                    await HandleInitAsync(id, wrapper, ct).ConfigureAwait(false);
                }
            }
            catch (Exception e) {
                _logger.Error(e, "Failed to refresh session '{id}'", id);
            }
        }

        /// <summary>
        /// Handles the disconnect state of a session
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        private async Task HandleDisconnectAsync(ConnectionIdentifier id, SessionWrapper wrapper) {
            _logger.Debug("Removing session '{id}'", id);
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _sessions.Remove(id, out _);
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
                _logger.Error(ex, "Failed to remove session '{id}'", id);
            }
        }

        // Validate certificates
        private void OnValidate(CertificateValidator sender, CertificateValidationEventArgs e) {
            if (e.Accept == true) {
                return;
            }
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                if (_applicationConfiguration.SecurityConfiguration.AutoAcceptUntrustedCertificates) {
                    _logger.Warning("Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                        "due to AutoAccept(UntrustedCertificates) set!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);
                    e.Accept = true;
                }

                // Validate thumbprint
                if (e.Certificate.RawData != null && !string.IsNullOrWhiteSpace(e.Certificate.Thumbprint)) {

                    if (_sessions.Keys.Any(id => id?.Connection?.Endpoint?.Certificate != null &&
                        e.Certificate.Thumbprint == id.Connection.Endpoint.Certificate)) {
                        e.Accept = true;

                        _logger.Information("Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                            "since it was specified in the endpoint!",
                            e.Certificate.Thumbprint, e.Certificate.Subject);

                        // add the certificate to trusted store
                        _applicationConfiguration.SecurityConfiguration.AddTrustedPeer(e.Certificate.RawData);
                        try {
                            var store = _applicationConfiguration.
                                SecurityConfiguration.TrustedPeerCertificates.OpenStore();

                            try {
                                store.Delete(e.Certificate.Thumbprint);
                                store.Add(e.Certificate);
                            }
                            finally {
                                store.Close();
                            }
                        }
                        catch (Exception ex) {
                            _logger.Warning(ex,"Failed to add peer certificate {Thumbprint}, '{Subject}' " +
                                "to trusted store", e.Certificate.Thumbprint, e.Certificate.Subject);
                        }
                    }
                }
            }
            if (!e.Accept) {
                _logger.Information("Rejecting peer certificate {Thumbprint}, '{Subject}' " +
                    "because of {Status}.", e.Certificate.Thumbprint, e.Certificate.Subject,
                    e.Error.StatusCode);
            }
        }

        /// <summary>
        /// Create session against endpoint
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<(Session, ComplexTypeSystem)> CreateSessionAsync(string endpointUrl, ConnectionIdentifier id,
            CancellationToken ct) {
            var sessionName = $"Azure IIoT {id}";

            var endpointDescription = SelectEndpoint(
                endpointUrl,
                id.Connection.Endpoint.SecurityMode,
                id.Connection.Endpoint.SecurityPolicy,
                _clientConfig.OperationTimeout);

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
                null, endpointDescription, _endpointConfiguration);

            _logger.Information("Creating session '{id}' for endpoint '{endpointUrl}'...", id, endpointUrl);
            using (new PerfMarker(_logger, sessionName)) {
                var userIdentity = id.Connection.User.ToStackModel() ??
                    new UserIdentity(new AnonymousIdentityToken());
                ComplexTypeSystem complexTypeSystem = null;
                var session = await Session.Create(
                    _applicationConfiguration, configuredEndpoint,
                    true, sessionName, _clientConfig.DefaultSessionTimeout,
                    userIdentity, null).ConfigureAwait(false);
                session.OperationTimeout = _clientConfig.OperationTimeout;
                session.KeepAliveInterval = _clientConfig.KeepAliveInterval;

                // TODO - store the created session id (node id)?
                if (sessionName != session.SessionName) {
                    _logger.Warning("Session '{id}' created with a revised name '{name}'",
                        id, session.SessionName);
                }

                try {
                    if (!ct.IsCancellationRequested) {
                        _logger.Information("Session '{id}' created, loading complex type system ... ", id);
                        complexTypeSystem = new ComplexTypeSystem(session);
                        await complexTypeSystem.Load().ConfigureAwait(false);
                        _logger.Information("Session '{id}' complex type system loaded", id);
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to load complex type system for session '{id}'", id);
                }
                finally {
                    // now that the complex type is handled, register the stack notifiers.
                    session.KeepAlive += Session_KeepAlive;
                    session.Notification += Session_Notification;
                }

                return (session, complexTypeSystem);
            }
        }

        /// <summary>
        /// callback to report session's notifications
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void Session_Notification(ISession session, NotificationEventArgs e) {
            try {
                _logger.Debug("Notification for session '{id}', subscription '{displayName}' - sequence# {sequence}-{publishTime}",
                    session?.Handle is SessionWrapper wrapper ? wrapper?.Id : session?.SessionName,
                    e.Subscription?.DisplayName, e?.NotificationMessage?.SequenceNumber,
                    e.NotificationMessage?.PublishTime);
                if (e.NotificationMessage.IsEmpty || e.NotificationMessage.NotificationData.Count == 0) {
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
            catch (Exception ex) {
                _logger.Error(ex, "Failed to process notifications for session '{name}'", session.SessionName);
            }
        }

        /// <summary>
        /// Handle keep alives
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e) {
            try {
                if (session?.Handle is SessionWrapper wrapper) {

                    _logger.Debug("Keepalive received from session '{id}': server current state: {state}",
                        wrapper.Id, e.CurrentState);

                    if (ServiceResult.IsGood(e.Status)) {
                        wrapper.ReportedStatus = StatusCodes.Good;
                        wrapper.MissedKeepAlives = 0;

                        if (!wrapper._subscriptions.Any()) {
                            if (wrapper.IdleCount < wrapper.MaxKeepAlives) {
                                wrapper.IdleCount++;
                            }
                            else {
                                _logger.Information("Idle expired session '{id}' set to {disconnect} state from {state}",
                                    wrapper.Id, SessionState.Disconnect, wrapper.State);
                                wrapper.State = SessionState.Disconnect;
                                TriggerKeepAlive();
                            }
                        }
                        else {
                            wrapper.IdleCount = 0;
                        }
                    }
                    else {
                        wrapper.ReportedStatus = e.Status.Code;
                        wrapper.MissedKeepAlives++;
                        _logger.Information("Session '{id}' missed {keepAlives} Keepalive(s) due to bad {status}, " +
                                "waiting to recover...", wrapper.Id, wrapper.MissedKeepAlives, e.Status);
                        if (wrapper.State == SessionState.Running) {
                            _logger.Information("Session '{id}' set to refresh due to bad keepalive status {status}",
                                wrapper.Id, e.Status);
                            wrapper.State = SessionState.Refresh;
                        }
                        if (!wrapper._subscriptions.Any()) {
                            if (wrapper.IdleCount < wrapper.MaxKeepAlives) {
                                wrapper.IdleCount++;
                            }
                            else {
                                _logger.Information("Idle expired session '{id}' set to {disconnect} state from {state}",
                                   wrapper.Id, SessionState.Disconnect, wrapper.State);
                                wrapper.State = SessionState.Disconnect;
                            }
                        }
                        else {
                            wrapper.IdleCount = 0;
                        }
                        TriggerKeepAlive();
                    }
                }
                else {
                    _logger.Warning("Keepalive received from unidentified session '{name}', server's current state is {state}",
                        session?.SessionName, e.CurrentState);
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to process Keepalive for session '{name}'", session.SessionName);
            }
        }

        private static MessageSecurityMode? ToMessageSecurityMode(SecurityMode? securityMode) {
            if (!securityMode.HasValue) {
                return null;
            }
            switch (securityMode.Value) {
                case SecurityMode.Best:
                    throw new NotSupportedException("The security mode 'best' is not supported");
                case SecurityMode.None:
                    return MessageSecurityMode.None;
                case SecurityMode.Sign:
                    return MessageSecurityMode.Sign;
                case SecurityMode.SignAndEncrypt:
                    return MessageSecurityMode.SignAndEncrypt;
                default:
                    throw new NotSupportedException($"The security mode '{securityMode}' is not implemented");
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
                return CoreClientUtils.SelectEndpoint(discoveryUrl, true, discoverTimeout: operationTimeout);
            }
            else if (securityMode == SecurityMode.None || securityPolicyUri == SecurityPolicies.None) {
                return CoreClientUtils.SelectEndpoint(discoveryUrl, false, discoverTimeout: operationTimeout);
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
            /// Session's identifier
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// Session
            /// </summary>
            public Session Session { get; internal set; }

            /// <summary>
            /// Complex type system
            /// </summary>
            public ComplexTypeSystem ComplexTypeSystem { get; internal set; }

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
            public ConcurrentDictionary<string, ISubscription> _subscriptions { get; }
                = new ConcurrentDictionary<string, ISubscription>();
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

        /// <summary>
        /// Triggers the keep alive runner immediate execution
        /// </summary>
        private void TriggerKeepAlive() {
            _triggerKeepAlive?.TrySetResult(true);
        }

        private readonly ILogger _logger;
        private readonly IClientServicesConfig _clientConfig;
        private readonly IIdentity _identity;
        private readonly ConcurrentDictionary<ConnectionIdentifier, SessionWrapper> _sessions =
            new ConcurrentDictionary<ConnectionIdentifier, SessionWrapper>();
        private readonly SemaphoreSlim _lock;

        private readonly Task _runner;
        private readonly CancellationTokenSource _cts;
        private TaskCompletionSource<bool> _triggerKeepAlive;

        private readonly ApplicationConfiguration _applicationConfiguration;
        private readonly EndpointConfiguration _endpointConfiguration;
    }
}
