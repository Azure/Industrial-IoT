// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Security.Cryptography.X509Certificates;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wraps a session object to provide serialized access and connection and
    /// user identity management.
    /// </summary>
    internal sealed class ClientSession : IClientSession {

        /// <inheritdoc/>
        public X509Certificate2 Certificate {
            get => _cert ?? _factory?.Invoke();
            set => _cert = value;
        }

        /// <inheritdoc/>
        public bool Inactive => !_persistent && DateTime.UtcNow > _lastActivity + _timeout;

        /// <inheritdoc/>
        public int Pending => _queue.Count;

        /// <summary>
        /// Create client session
        /// </summary>
        /// <param name="config">Application configuration</param>
        /// <param name="factory">Certificate factory</param>
        /// <param name="endpoint">Endpoint to connect to</param>
        /// <param name="timeout">Session timeout</param>
        /// <param name="statusCb">Status callback for reporting</param>
        /// <param name="persistent">Persists until closed</param>
        /// <param name="maxOpTimeout"></param>
        /// <param name="logger">Logger</param>
        /// <param name="sessionName">Optional session name</param>
        /// <param name="keepAlive">Keep alive interval</param>
        public ClientSession(ApplicationConfiguration config, EndpointModel endpoint,
            Func<X509Certificate2> factory, ILogger logger,
            Func<EndpointModel, EndpointConnectivityState, Task> statusCb,
            bool persistent, TimeSpan? maxOpTimeout = null,
            string sessionName = null, TimeSpan? timeout = null,
            TimeSpan? keepAlive = null) {
            _logger = logger ?? Log.Logger;
            _factory = factory;
            _endpoint = endpoint;
            _config = config;
            _timeout = timeout ?? TimeSpan.FromMilliseconds(
                config.ClientConfiguration.DefaultSessionTimeout);
            _statusCb = statusCb;
            _cts = new CancellationTokenSource();
            _lastState = EndpointConnectivityState.Connecting;
            _keepAlive = keepAlive ?? TimeSpan.FromSeconds(5);
            _lastActivity = DateTime.UtcNow;
            _persistent = persistent;
            _sessionName = sessionName ?? Guid.NewGuid().ToString();
            _opTimeout = maxOpTimeout ?? TimeSpan.FromMilliseconds(
                config.TransportQuotas.OperationTimeout * 4);

            _queue = new PriorityQueue<int, SessionOperation>();
            _enqueueEvent = new TaskCompletionSource<bool>(
                TaskContinuationOptions.RunContinuationsAsynchronously);
#pragma warning disable RECS0002 // Convert anonymous method to method group
            _processor = Task.Run(() => RunAsync());
#pragma warning restore RECS0002 // Convert anonymous method to method group
        }

        /// <inhjeritdoc/>
        public void Dispose() => CloseAsync().Wait();

        /// <inheritdoc/>
        public async Task CloseAsync() {
            if (!_cts.IsCancellationRequested) {
                // Cancel operations
                _cts.Cancel();
                // Unblock keep alives and retries.
                _enqueueEvent.TrySetResult(true);
                // Wait for processor to finish
                await _processor;
            }
            // Clear queue and cancel all remaining outstanding operations
            while (_queue.TryDequeue(out var result)) {
                result.Item2.Dispose();
            }
            _queue.Clear();
        }

        /// <inheritdoc/>
        public bool TryScheduleServiceCall<T>(CredentialModel elevation, int priority,
            Func<Session, Task<T>> serviceCall, Func<Exception, bool> handler,
            TimeSpan? timeout, CancellationToken? ct, out Task<T> completion) {

            if (!_cts.IsCancellationRequested) {
                var op = new ScheduledOperation<T>(serviceCall, handler, elevation,
                    timeout ?? _opTimeout, ct);
                _queue.Enqueue(priority, op);
                // Notify of new operation enqueued.
                _enqueueEvent.TrySetResult(true);
                // Return completion event to wait for
                completion = op.Completed;
                return true;
            }
            // Session is closed - indicate to caller to schedule on different session.
            completion = null;
            return false;
        }

        /// <summary>
        /// Process operations and manage session
        /// </summary>
        /// <returns></returns>
        private async Task RunAsync() {

            // Create a static keep alive operation object for the the session
            var keepAlive = (0, new KeepAlive(_keepAlive));

            // We cache the last operation and priority if operation should be
            // retried next loop
            SessionOperation operation = null;
            var priority = 0;

            var reconnect = false;
            var recreate = false;
            var retryCount = 0;
            var everSuccessful = _persistent;

            // Save identity and certificate to update session if there are changes.
            var identity = _endpoint.User.ToStackModel();
            var certificate = Certificate;
            try {
                while (!_cts.Token.IsCancellationRequested) {
                    if (!certificate.EqualsSafe(Certificate)) {
                        certificate = Certificate;
                        _session.Close();
                        _session = null;
                    }
                    Exception ex = null;
                    if (_session == null) {
                        // Try create session
                        recreate = false;
                        reconnect = false;
                        try {
                            _logger.Debug("Creating new session to {Url}...", _endpoint.Url);
                            _session = await CreateSessionAsync(certificate, identity);
                            _logger.Debug("Session to {Url} created.", _endpoint.Url);
                        }
                        catch (Exception e) {
                            _logger.Debug(e,
                                "Failed to create session to {Url} - retry.", _endpoint.Url);
                            ex = e;
                        }
                    }
                    if (recreate) {
                        // Try recreate session from current one
                        try {
                            _logger.Debug("Recreating session to {Url}...", _endpoint.Url);
                            var session = await Task.Run(() => Session.Recreate(_session), _cts.Token);
                            _logger.Debug("Session recreated to {Url}.", _endpoint.Url);
                            _session.Close();
                            _session = session;
                            recreate = false;
                        }
                        catch (Exception e) {
                            ex = e;
                            _logger.Error(e,
                                "Failed to recreate session with {Url} - create new one." +
                                _endpoint.Url);
                            _session?.Close();
                            _session = null;
                        }
                    }
                    if (reconnect) {
                        // Try reconnect the session
                        try {
                            _logger.Debug("Reconnecting session to {Url}...", _endpoint.Url);
#pragma warning disable RECS0002 // Convert anonymous method to method group
                            await Task.Run(() => _session.Reconnect(), _cts.Token);
#pragma warning restore RECS0002 // Convert anonymous method to method group
                            _logger.Debug("Session reconnected to {Url}.", _endpoint.Url);
                            reconnect = false;
                        }
                        catch (Exception e) {
                            ex = e;
                            recreate = true;
                            reconnect = false;
                            if (e is ServiceResultException sre) {
                                if (sre.StatusCode == StatusCodes.BadTcpEndpointUrlInvalid ||
                                    sre.StatusCode == StatusCodes.BadTcpInternalError ||
                                    sre.StatusCode == StatusCodes.BadCommunicationError ||
                                    sre.StatusCode == StatusCodes.BadNotConnected) {
                                    if (retryCount < kMaxReconnectAttempts && Pending > 0) {
                                        _logger.Error(e,
                                            "Failed to reconnect session to {Url} - retry...",
                                            _endpoint.Url);
                                        recreate = false;
                                        reconnect = true; // Try again
                                    }
                                }
                            }
                            _logger.Debug(e,
                                "Failed to reconnect to {Url} - recreating session...",
                                _endpoint.Url);
                        }
                    }

                    // Failed to connect
                    if (recreate || reconnect || _session == null) {
                        await NotifyConnectivityStateChangeAsync(ToConnectivityState(ex));
                        if (ex is ServiceResultException sre) {
                            ex = sre.ToTypedException();
                        }

                        // Compress operations queue
                        var operations = new List<(int, SessionOperation)>();
                        while (_queue.TryDequeue(out var op)) {
                            if (op.Item2 == null) {
                                break;
                            }
                            operations.Add(op);
                        }
                        foreach (var op in operations) {
                            if (ex != null && !op.Item2.ShouldRetry(ex)) {
                                op.Item2.Fail(ex);
                            }
                            else if (!op.Item2.IsCompleted()) {
                                // Re-add the still valid ones...
                                _queue.Enqueue(op);
                            }
                        }

                        ++retryCount;
                        if (!_persistent && retryCount > kMaxEmptyReconnectAttempts && Pending == 0) {
                            _logger.Error("Give up on empty non-persistent session to {Url}...",
                                _endpoint.Url);
                            break;
                        }
                        // Try again to connect with an exponential delay
                        var delay = Retry.GetExponentialDelay(retryCount, kMaxReconnectDelay / 2,
                            kMaxRetries);
                        // If pending operations, do not delay longer than 5 seconds
                        if (Pending != 0 && delay > kMaxReconnectDelay) {
                            delay = kMaxReconnectDelay;
                        }
                        _logger.Debug("Try to connect to {Url} in {delay} ms...", _endpoint.Url, delay);
                        // Wait for either the retry delay to pass or until new operation is added
                        await WaitForNewlyEnqueuedOperationAsync(delay);
                        continue;
                    }

                    // We have a session that should work for us, get next operation and
                    // complete it.
                    retryCount = 0;
                    recreate = false;
                    reconnect = false;

                    if (operation == null) {
                        if (!_queue.TryDequeue(out var next)) {
                            // Wait for enqueue or keep alive timeout
                            var timeout = await WaitForNewlyEnqueuedOperationAsync(
                                (int)_keepAlive.TotalMilliseconds);
                            // Check cancellation
                            if (_cts.Token.IsCancellationRequested) {
                                continue;
                            }
                            if (timeout || !_queue.TryDequeue(out next)) {
                                next = keepAlive;
                            }
                        }
                        (priority, operation) = next;
                    }
                    if (operation.IsCompleted()) {
                        operation = null; // Already completed because timeout or cancellation, get next
                        continue;
                    }
                    if (operation is KeepAlive) {
                        _logger.Verbose("Sending keep alive message...");
                    }
                    try {
                        // Check if the desired user identity is the same as the current one
                        if (!Utils.IsEqual((operation.Identity ?? identity).GetIdentityToken(),
                                _session.Identity.GetIdentityToken())) {
                            // Try Elevate or de-elevate session
                            try {
                                _logger.Verbose("Updating session user identity...");
                                await Task.Run(() => _session.UpdateSession(operation.Identity,
                                    _session.PreferredLocales));
                                _logger.Debug("Updated session user identity.");
                            }
                            catch (ServiceResultException sre) {
                                if (StatusCodeEx.IsSecurityError(sre.StatusCode)) {
                                    _logger.Debug(sre, "Failed updating session identity");
                                    await NotifyConnectivityStateChangeAsync(ToConnectivityState(sre));
                                    operation.Fail(sre.ToTypedException());
                                    operation = null;
                                    continue;
                                }
                                throw;
                            }
                        }

                        await Task.Run(() => operation.Complete(_session), _cts.Token);
                        _lastActivity = DateTime.UtcNow;
                        await NotifyConnectivityStateChangeAsync(EndpointConnectivityState.Ready);
                        everSuccessful = true;
                        _logger.Verbose("Session operation completed.");
                        operation = null;
                    }
                    catch (Exception e) {
                        // Process exception - first convert sre into non opc exception
                        var oex = e;
                        if (e is ServiceResultException sre) {
                            e = sre.ToTypedException();
                        }
                        // See which ones we can retry, and which ones we cannot
                        switch (e) {
                            case TimeoutException te:
                            case ServerBusyException sb:
                                if (everSuccessful && operation.ShouldRetry(e)) {
                                    _logger.Information(e,
                                        "Server busy or timeout, try again later...");
                                    _queue.Enqueue(priority, operation);
                                    operation = null;
                                }
                                else {
                                    _logger.Error(e, "Service call timeout - fail operation.");
                                    operation.Fail(e);
                                    reconnect = operation is KeepAlive;
                                    operation = null;
                                }
                                break;
                            case ConnectionException cn:
                            case ProtocolException pe:
                            case CommunicationException ce:
                                if (everSuccessful && operation.ShouldRetry(e)) {
                                    _logger.Information(e, "Reconnect and try operation again...");
                                    // Reconnect session
                                    reconnect = true;
                                }
                                else {
                                    _logger.Error(e, "Server communication error - fail operation.");
                                    operation.Fail(e);
                                    reconnect = operation is KeepAlive;
                                    operation = null;
                                }
                                break;
                            default:
                                // App error - fail and continue
                                _logger.Debug(e, "Application error - fail operation.");
                                operation.Fail(e);
                                reconnect = operation is KeepAlive;
                                operation = null;
                                break;
                        }
                        if (reconnect || !everSuccessful) {
                            await NotifyConnectivityStateChangeAsync(ToConnectivityState(oex, false));
                        }
                        if (!everSuccessful) {
                            break; // Give up here - might have just been used to test endpoint
                        }
                    }
                } // end while
            }
            catch (OperationCanceledException) {
                // Expected on cancellation
            }
            catch (Exception ex) {
                _logger.Error(ex, "Session operation processor exited with exception");
            }
            finally {
                if (operation != null) {
                    _queue.Enqueue(priority, operation);
                }
                _lastActivity = DateTime.MinValue;

                _session?.Close();
                _session = null;
            }
        }

        /// <summary>
        /// Wait for a newly enqueued operation using a timeout.
        /// </summary>
        public Task<bool> WaitForNewlyEnqueuedOperationAsync(int timeout) {
            while (true) {
                var tcs = _enqueueEvent;
                // Wait on a fresh task or on the not yet completed on
                if (tcs.Task.IsCompleted) {
                    var newEvent = new TaskCompletionSource<bool>(
                        TaskContinuationOptions.RunContinuationsAsynchronously);
                    if (Interlocked.CompareExchange(ref _enqueueEvent, newEvent, tcs) == tcs) {
                        // Exchanged safely now we can wait for it.
                        tcs = newEvent;
                    }
                }
                return Task.WhenAny(tcs.Task, Task.Delay(timeout))
                    .ContinueWith(t => t.Result == tcs.Task);
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
                        case StatusCodes.BadNotConnected:
                            state = EndpointConnectivityState.NotReachable;
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
        /// Notify about new connectivity state using any status callback registered.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private async Task NotifyConnectivityStateChangeAsync(EndpointConnectivityState state) {
            var previous = _lastState;
            if (previous == state) {
                return;
            }
            if (previous != EndpointConnectivityState.Connecting &&
                previous != EndpointConnectivityState.Ready &&
                state == EndpointConnectivityState.Error) {
                // Do not change state to generic error once we have
                // a specific error state already set...
                _logger.Debug("Error, but leaving {Url} state at {previous}.",
                    _endpoint.Url, previous);
                return;
            }
            _lastState = state;
            _logger.Information("Endpoint {Url} changed from {previous} to {state}",
                _endpoint.Url, previous, state);
            try {
                await _statusCb?.Invoke(_endpoint, state);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Exception during state callback");
            }
        }

        /// <summary>
        /// Creates a new session
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        private async Task<Session> CreateSessionAsync(X509Certificate2 cert,
            IUserIdentity identity) {

            await _config.Validate(Opc.Ua.ApplicationType.Client);
            if (cert != null) {
                _config.SecurityConfiguration.ApplicationCertificate.Certificate =
                    cert;
                _config.ApplicationUri = Utils.GetApplicationUriFromCertificate(
                    cert);
                _config.CertificateValidator.CertificateValidation += (v, e) => {
                    if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                        // TODO: Validate against endpoint model certificate
                        e.Accept = true;
                    }
                };
            }
            else if (_endpoint.SecurityMode == SecurityMode.None) {
                _logger.Warning("Using unsecure connection.");
            }
            else {
                throw new CertificateInvalidException("Missing client certificate");
            }

            var selectedEndpoint = await DiscoverEndpointsAsync(_config,
                _endpoint, new Uri(_endpoint.Url), (server, endpoints, channel) =>
                    SelectServerEndpoint(server, endpoints, channel, cert != null));
            if (selectedEndpoint == null) {
                throw new ConnectionException("Unable to select secure endpoint");
            }

            var configuredEndpoint = new ConfiguredEndpoint(selectedEndpoint.Server,
                EndpointConfiguration.Create(_config));
            configuredEndpoint.Update(selectedEndpoint);

            var session = await Session.Create(_config, configuredEndpoint, true, false,
                _sessionName, (uint)(_timeout.TotalMilliseconds * 1.2), identity, null);
            if (session == null) {
                throw new ExternalDependencyException("Cannot establish session.");
            }
            session.KeepAlive += (_, e) => e.CancelKeepAlive = true;
            session.KeepAliveInterval = -1; // No keep alives - we handle those ourselves.
            session.RenewUserIdentity += (_, user) => identity; // Reset back to default.
            return session;
        }

        /// <summary>
        /// Select the endpoint based on the model
        /// </summary>
        /// <param name="server"></param>
        /// <param name="endpoints"></param>
        /// <param name="channel"></param>
        /// <param name="haveCert"></param>
        /// <returns></returns>
        private static EndpointDescription SelectServerEndpoint(EndpointModel server,
            IEnumerable<EndpointDescription> endpoints, ITransportChannel channel,
            bool haveCert) {

            Contract.Requires(channel != null);

            // Filter
            var filtered = endpoints
                .Where(e => e.TransportProfileUri == Profiles.UaTcpTransport)
                .Where(e => {
                    switch (server.SecurityMode) {
                        case SecurityMode.Best:
                            return true;
                        case SecurityMode.None:
                            return e.SecurityMode == MessageSecurityMode.None;
                        case SecurityMode.Sign:
                            return e.SecurityMode == MessageSecurityMode.Sign;
                        case SecurityMode.SignAndEncrypt:
                            return e.SecurityMode == MessageSecurityMode.SignAndEncrypt;
                    }
                    return true;
                })
                .Where(e => string.IsNullOrEmpty(server.SecurityPolicy) ||
                    server.SecurityPolicy == e.SecurityPolicyUri);

            var bestEndpoint = filtered.FirstOrDefault();
            foreach (var endpoint in filtered) {
                if (haveCert && (endpoint.SecurityLevel > bestEndpoint.SecurityLevel) ||
                    !haveCert && (endpoint.SecurityLevel < bestEndpoint.SecurityLevel)) {
                    bestEndpoint = endpoint;
                }
            }
            return bestEndpoint;
        }

        /// <summary>
        /// Discover and select endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <param name="server"></param>
        /// <param name="discoveryUrl"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        private static async Task<EndpointDescription> DiscoverEndpointsAsync(
            ApplicationConfiguration config, EndpointModel server,
            Uri discoveryUrl, Func<EndpointModel, IEnumerable<EndpointDescription>,
                ITransportChannel, EndpointDescription> selector) {

            // use a short timeout.
            var configuration = EndpointConfiguration.Create(config);
            configuration.OperationTimeout = kMaxReconnectDelay;

            using (var client = DiscoveryClient.Create(discoveryUrl, configuration)) {
                var response = await client.GetEndpointsAsync(null,
                    client.Endpoint.EndpointUrl, null /*TODO: Locale support*/, null);

                var endpoints = response?.Endpoints ??
                    new EndpointDescriptionCollection();
                ReplaceHostWithRemoteHost(endpoints, discoveryUrl);
                // Select best endpoint
                return selector(server, endpoints, client.TransportChannel);
            }
        }

        /// <summary>
        /// Replace host name with the one in the discovery url
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="discoveryUrl"></param>
        private static void ReplaceHostWithRemoteHost(
            EndpointDescriptionCollection endpoints, Uri discoveryUrl) {
            foreach (var endpoint in endpoints) {
                endpoint.EndpointUrl = new Uri(endpoint.EndpointUrl).ChangeHost(
                    discoveryUrl.DnsSafeHost).ToString();
                var updatedDiscoveryUrls = new StringCollection();
                foreach (var url in endpoint.Server.DiscoveryUrls) {
                    updatedDiscoveryUrls.Add(new Uri(url)
                        .ChangeHost(discoveryUrl.DnsSafeHost).ToString());
                }
                endpoint.Server.DiscoveryUrls = updatedDiscoveryUrls;
            }
        }

        private abstract class SessionOperation : IDisposable {

            /// <summary>
            /// User to use for operation or null for don't care.
            /// </summary>
            public IUserIdentity Identity { get; protected set; }

            /// <summary>
            /// Complete the operation
            /// </summary>
            /// <param name="session"></param>
            /// <returns></returns>
            public abstract Task Complete(Session session);

            /// <summary>
            /// Fail the operation
            /// </summary>
            /// <param name="ex"></param>
            public virtual void Fail(Exception ex) { }

            /// <summary>
            /// Whether the operation is completed
            /// </summary>
            public virtual bool IsCompleted() => false;

            /// <summary>
            /// Whether to retry
            /// </summary>
            /// <param name="ex"></param>
            public virtual bool ShouldRetry(Exception ex) => true;

            /// <inheritdoc/>
            public virtual void Dispose() { }
        }

        private sealed class KeepAlive : SessionOperation {

            /// <summary>
            /// Create keep alive operation
            /// </summary>
            /// <param name="timeout"></param>
            public KeepAlive(TimeSpan timeout) {
                _timeout = timeout;
            }

            /// <inheritdoc/>
            public override bool ShouldRetry(Exception ex) => _failures % 2 == 0;

            /// <inheritdoc/>
            public override async Task Complete(Session session) {
                try {
                    // read the server state.
                    await session.ReadAsync(new RequestHeader {
                        RequestHandle = ++_keepAliveCounter,
                        TimeoutHint = (uint)_timeout.TotalMilliseconds,
                        ReturnDiagnostics = 0
                    }, 0, TimestampsToReturn.Neither, new ReadValueIdCollection {
                            new ReadValueId {
                                NodeId = Variables.Server_ServerStatus_State,
                                AttributeId = Attributes.Value
                            }
                        });
                    _failures = 0;
                }
                catch {
                    _failures++;
                    throw;
                }
            }

            /// <inheritdoc/>
            public override string ToString() => "KEEP ALIVE";

            private readonly TimeSpan _timeout;
            private long _failures;
            private uint _keepAliveCounter;
        }

        /// <summary>
        /// Represents a single operation in a session that was scheduled
        /// using TryScheduleServiceCall method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class ScheduledOperation<T> : SessionOperation {

            /// <summary>
            /// create operation
            /// </summary>
            /// <param name="operation"></param>
            /// <param name="handler"></param>
            /// <param name="elevation"></param>
            /// <param name="timeout"></param>
            /// <param name="ct"></param>
            public ScheduledOperation(Func<Session, Task<T>> operation,
                Func<Exception, bool> handler, CredentialModel elevation,
                TimeSpan timeout, CancellationToken? ct) {
                _operation = operation;
                _handler = handler;
                Identity = elevation.ToStackModel();
                _cts = new CancellationTokenSource(timeout);
                ct?.Register(_cts.Cancel);
                _cts.Token.Register(OnCancellation);
                _ct = ct ?? CancellationToken.None;
            }

            /// <inheritdoc/>
            public Task<T> Completed => _tcs.Task;

            /// <inheritdoc/>
            public override void Dispose() =>
                _tcs.TrySetCanceled();


            /// <inheritdoc/>
            public override bool IsCompleted() => _tcs.Task.IsCompleted;

            /// <inheritdoc/>
            public override async Task Complete(Session session) {
                // Complete the operation - this can throw - caller will "fail it"
                var result = await Task.Run(() => _operation(session), _cts.Token)
                    .ConfigureAwait(false);
                _tcs.TrySetResult(result);
            }
            /// <inheritdoc/>
            public override void Fail(Exception ex) =>
                _tcs.TrySetException(ex);

            /// <inheritdoc/>
            public override bool ShouldRetry(Exception ex) =>
                _handler?.Invoke(ex) ?? false;

            /// <inheritdoc/>
            public override string ToString() => _operation.ToString();

            /// <summary>
            /// Cancellation token expired or operation was cancelled.
            /// </summary>
            private void OnCancellation() {
                if (_ct.IsCancellationRequested) {
                    _tcs.TrySetCanceled();
                }
                else {
                    // Timeout
                    _tcs.TrySetException(
                        new TimeoutException("Operation timeout"));
                }
            }

            private readonly Func<Session, Task<T>> _operation;
            private readonly Func<Exception, bool> _handler;
            private readonly TaskCompletionSource<T> _tcs =
                new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly CancellationTokenSource _cts;
            private readonly CancellationToken _ct;
        }

        private const int kMaxReconnectAttempts = 4;
        private const int kMaxEmptyReconnectAttempts = 2;
        private const int kMaxRetries = 15;
        private const int kMaxReconnectDelay = 5000;

        private DateTime _lastActivity;
        private X509Certificate2 _cert;
        private Session _session;
        private EndpointConnectivityState _lastState;
        private readonly bool _persistent;
        private readonly TimeSpan _opTimeout;
        private readonly string _sessionName;
        private readonly ILogger _logger;
        private readonly Func<X509Certificate2> _factory;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _keepAlive;
        private readonly ApplicationConfiguration _config;
        private readonly EndpointModel _endpoint;
        private readonly Task _processor;
        private readonly CancellationTokenSource _cts;
        private readonly PriorityQueue<int, SessionOperation> _queue;
        private volatile TaskCompletionSource<bool> _enqueueEvent;
        private readonly Func<EndpointModel, EndpointConnectivityState, Task> _statusCb;
    }
}
