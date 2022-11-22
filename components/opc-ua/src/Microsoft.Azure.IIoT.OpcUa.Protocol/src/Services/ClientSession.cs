// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wraps a session object to provide serialized access and connection and
    /// user identity management.
    /// </summary>
    internal sealed class ClientSession : IClientSession {

        /// <inheritdoc/>
        public bool Inactive => _handles.Count == 0 && Pending == 0 &&
            DateTime.UtcNow > _lastActivity + _timeout;

        /// <inheritdoc/>
        public int Pending => _queue.Count
            + (_curOperation is null || _curOperation is KeepAlive ? 0 : 1);

        private ClientSession(ApplicationConfiguration config, ConnectionModel connection,
            ILogger logger, Func<ConnectionModel, EndpointConnectivityState, Task> statusCb,
            TimeSpan? maxOpTimeout, string sessionName, TimeSpan? timeout,
            TimeSpan? keepAlive) {
            _sessionName = sessionName ?? Guid.NewGuid().ToString();
            _logger = (logger ?? Log.Logger).ForContext("SourceContext", new {
                name = _sessionName,
                sessionId = Interlocked.Increment(ref _sessionCounter),
                url = connection.Endpoint.Url
            }, true);
            _connection = connection.Clone();
            _config = config;
            _config.CertificateValidator.CertificateValidation += OnValidate;
            _timeout = timeout ?? TimeSpan.FromMilliseconds(
                _config.ClientConfiguration.DefaultSessionTimeout);
            _statusCb = statusCb;
            _cts = new CancellationTokenSource();
            _lastState = EndpointConnectivityState.Connecting;
            _keepAlive = keepAlive ?? TimeSpan.FromSeconds(5);
            _lastActivity = DateTime.UtcNow;
            // Align the default device method timeout
            _opTimeout = maxOpTimeout ?? TimeSpan.FromMinutes(8);
            _session = null;
            _acquired = new TaskCompletionSource<Session>();
            _urlQueue = new ConcurrentQueue<string>(_connection.Endpoint.GetAllUrls());
            _queue = new System.Collections.Concurrent.PriorityQueue<int, SessionOperation>();
            _enqueueEvent = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable RECS0002 // Convert anonymous method to method group
            _processor = Task.Factory.StartNew(() => RunAsync(), _cts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
#pragma warning restore RECS0002 // Convert anonymous method to method group
            _logger.Information("Session created.");
        }

        /// <summary>
        /// Create client session
        /// </summary>
        /// <param name="config">Application configuration</param>
        /// <param name="connection">Endpoint to connect to</param>
        /// <param name="logger">Logger</param>
        /// <param name="statusCb">Status callback for reporting</param>
        /// <param name="maxOpTimeout"></param>
        /// <param name="sessionName">Optional session name</param>
        /// <param name="timeout">Session timeout</param>
        /// <param name="keepAlive">Keep alive interval</param>
        public static IClientSession Create(ApplicationConfiguration config,
            ConnectionModel connection, ILogger logger, Func<ConnectionModel,
                EndpointConnectivityState, Task> statusCb,
            TimeSpan? maxOpTimeout = null, string sessionName = null,
            TimeSpan? timeout = null, TimeSpan? keepAlive = null) {

            return new ClientSession(config, connection, logger, statusCb,
                maxOpTimeout, sessionName, timeout, keepAlive);
        }

        /// <summary>
        /// Create client session and return session and a handle
        /// </summary>
        /// <param name="config">Application configuration</param>
        /// <param name="connection">Endpoint to connect to</param>
        /// <param name="logger">Logger</param>
        /// <param name="statusCb">Status callback for reporting</param>
        /// <param name="maxOpTimeout"></param>
        /// <param name="sessionName">Optional session name</param>
        /// <param name="timeout">Session timeout</param>
        /// <param name="keepAlive">Keep alive interval</param>
        public static (IClientSession, ISessionHandle) CreateWithHandle(
            ApplicationConfiguration config, ConnectionModel connection,
            ILogger logger, Func<ConnectionModel, EndpointConnectivityState, Task> statusCb,
            TimeSpan? maxOpTimeout = null, string sessionName = null,
            TimeSpan? timeout = null, TimeSpan? keepAlive = null) {

            var session = new ClientSession(config, connection, logger, statusCb,
                maxOpTimeout, sessionName, timeout, keepAlive);
            var handle = session.GetSafeHandle();
            return (session, handle);
        }

        /// <inheritdoc/>
        public void Dispose() {
            CloseAsync().Wait();
            _cts.Dispose();
            _config.CertificateValidator.CertificateValidation -= OnValidate;
            _logger.Information("Session closed.");
        }

        /// <inheritdoc/>
        public async Task CloseAsync() {
            if (!_cts.IsCancellationRequested) {
                _logger.Debug("Closing processor {processor}@{status}...",
                    _processor.Id, _processor.Status);
                // Cancel operations
                _cts.Cancel();
                // Unblock keep alives and retries.
                _enqueueEvent.TrySetResult(true);
                // Wait for processor to finish
                await Try.Async(() => _processor);
                _logger.Verbose("Processor closed.");
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
                _lastActivity = DateTime.UtcNow;
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

        /// <inheritdoc/>
        public ISessionHandle GetSafeHandle() {
            return new ClientSessionHandle(this);
        }

        /// <summary>
        /// Process operations and manage session
        /// </summary>
        /// <returns></returns>
        private async Task RunAsync() {

            // Create a static keep alive operation object for the the session
            var keepAlive = (0, new KeepAlive(_keepAlive));

            // We cache the current operation as part of sessions state and the priority
            // here if operation should be retried next loop
            var priority = 0;

            var reconnect = false;
            var recreate = false;
            var retryCount = 0;

            // Save identity and certificate to update session if there are changes.
            var defaultIdentity = new UserIdentity(_connection.User.ToUserIdentityToken());
            try {
                while (!_cts.Token.IsCancellationRequested) {

                    Exception ex = null;
                    if (_session == null) {
                        // Try create session
                        recreate = false;
                        reconnect = false;
                        try {
                            try {
                                if (_curOperation == null && _queue.TryDequeue(out var next)) {
                                    _curOperation = next.Item2;
                                }
                                var identity = _curOperation?.Identity ?? defaultIdentity;
                                _logger.Debug("Creating new session via {endpoint} using {identity}...",
                                    _endpointUrl, identity.DisplayName);
                                _session = await CreateSessionAsync(identity);
                                _logger.Debug("Session via {endpoint} created.", _endpointUrl);
                            }
                            catch (ServiceResultException sre) {
                                var ce = sre.ToTypedException();
                                if (ce is UnauthorizedAccessException uae && _curOperation != null) {
                                    // the operation identity is not working to establish connection
                                    _curOperation.Fail(uae);
                                    _curOperation = null;
                                    _lastActivity = DateTime.UtcNow;
                                }
                                throw;
                            }
                        }
                        catch (Exception e) {
                            _logger.Information(
                                "{message} creating session via {endpoint} - retry.",
                                e.Message, _endpointUrl);
                            _logger.Debug(e, "Error connecting - retry.");
                            ex = e;
                        }
                    }
                    if (recreate) {
                        // Try recreate session from current one
                        try {
                            _logger.Debug("Recreating session via {endpoint}...",
                                _endpointUrl);
                            var session = await Task.Run(() => Session.Recreate(_session), _cts.Token);
                            _logger.Debug("Session recreated via {endpoint}.",
                                _endpointUrl);

                            Try.Op(() => _session.Close());
                            _session = session;
                            recreate = false;
                        }
                        catch (Exception e) {
                            ex = e;
                            _logger.Information("{message} while recreating session " +
                                "via {endpoint} - create new one.",
                                e.Message, _endpointUrl);
                            _logger.Debug(e, "Error connecting - create new session.");
                            _session?.Close();
                            _session = null;
                        }
                    }
                    if (reconnect) {
                        // Try reconnect the session
                        try {
                            _logger.Debug("Reconnecting session via {endpoint}...",
                                _endpointUrl);
#pragma warning disable RECS0002 // Convert anonymous method to method group
                            await Task.Run(() => _session.Reconnect(), _cts.Token);
#pragma warning restore RECS0002 // Convert anonymous method to method group
                            _logger.Debug("Session reconnected via {endpoint}.",
                                _endpointUrl);
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
                                        _logger.Information("{message} while reconnecting session" +
                                            " via {endpoint} - retry...",
                                            sre.Message, _endpointUrl);
                                        recreate = false;
                                        reconnect = true; // Try again
                                    }
                                }
                            }
                            _logger.Debug(e,
                                "Error reconnecting via {endpoint} - recreating session...",
                                _endpointUrl);
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
                        // Try again to connect with an exponential delay
                        var delay = Retry.GetExponentialDelay(retryCount,
                            kMaxReconnectDelayWhenPendingOperations / 2, kMaxRetries);
                        if (delay > kMaxReconnectDelayWhenPendingOperations) {
                            //
                            // If pending operations, do not delay longer than 5 seconds
                            // otherwise wait for 2 hours or until new operation is queued.
                            //
                            delay = Pending != 0 ? kMaxReconnectDelayWhenPendingOperations :
                                kMaxReconnectDelayWhenNoPendingOperations;
                        }
                        _logger.Information("Try to connect via {endpoint} in {delay} ms...",
                            _endpointUrl, delay);
                        // Wait for either the retry delay to pass or until new operation is added
                        await WaitForNewlyEnqueuedOperationAsync(delay);
                        continue;
                    }

                    // We have a session that should work for us, get next operation and
                    // complete it.
                    retryCount = 0;
                    recreate = false;
                    reconnect = false;

                    if (_curOperation == null) {
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
                        (priority, _curOperation) = next;
                        System.Diagnostics.Debug.Assert(_curOperation != null);
                    }
                    if (_curOperation.IsCompleted()) {
                        _curOperation = null; // Already completed because timeout or cancellation, get next
                        continue;
                    }
                    try {
                        if (_curOperation is KeepAlive) {
                            _logger.Debug("Sending keep alive message...");
                        }
                        else {
                            // Check if the desired user identity is the same as the current one
                            if (!Utils.IsEqual((_curOperation.Identity ?? defaultIdentity).GetIdentityToken(),
                                    _session.Identity.GetIdentityToken())) {
                                // Try Elevate or de-elevate session
                                try {
                                    _logger.Verbose("Updating session user identity...");
                                    await Task.Run(() => _session.UpdateSession(_curOperation.Identity,
                                        _session.PreferredLocales));
                                    _logger.Debug("Updated session user identity.");
                                }
                                catch (ServiceResultException sre) {
                                    if (StatusCodeEx.IsSecurityError(sre.StatusCode)) {
                                        _logger.Debug(sre, "Failed updating session identity");
                                        await NotifyConnectivityStateChangeAsync(ToConnectivityState(sre));
                                        _curOperation.Fail(sre.ToTypedException());
                                        _curOperation = null;
                                        continue;
                                    }
                                    throw;
                                }
                            }
                        }
                        await Task.Run(() => _curOperation.Complete(_session), _cts.Token);
                        var isKeepAlive = _curOperation is KeepAlive;
                        if (!isKeepAlive) {
                            //
                            // Only mark completed non keep alives as activity.
                            // Close this session if there was no activity for
                            // the duration of inactivity timeout.
                            //
                            _lastActivity = DateTime.UtcNow;
                        }
                        if (!isKeepAlive || _lastState != EndpointConnectivityState.Unauthorized) {
                            await NotifyConnectivityStateChangeAsync(EndpointConnectivityState.Ready);
                        }
                        _curOperation = null;
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
                                _logger.Debug(e, "Server timeout error.");
                                if (_curOperation.ShouldRetry(e)) {
                                    _logger.Information("Timeout error talking {endpoint} " +
                                        "- {error} - try again later...",
                                        _endpointUrl, e.Message);
                                    _queue.Enqueue(priority, _curOperation);
                                    _curOperation = null;
                                }
                                else {
                                    reconnect = _curOperation is KeepAlive;
                                    if (!reconnect) {
                                        _logger.Error("Timeout error  talking to {endpoint} " +
                                            "- {error} - fail user operation.",
                                            _endpointUrl, e.Message);
                                    }
                                    _curOperation.Fail(e);
                                    _curOperation = null;
                                }
                                break;
                            case ConnectionException cn:
                            case ProtocolException pe:
                            case CommunicationException ce:
                                _logger.Debug(e, "Server communication error.");
                                if (_curOperation.ShouldRetry(e)) {
                                    _logger.Information("Communication error talking to {endpoint} " +
                                        "- {error} - Reconnect and try again...",
                                        _endpointUrl, e.Message);
                                    // Reconnect session
                                    reconnect = true;
                                }
                                else {
                                    reconnect = _curOperation is KeepAlive;
                                    if (!reconnect) {
                                        _logger.Error("Communication error talking to {endpoint} " +
                                            "- {error} - fail user operation.",
                                            _endpointUrl, e.Message);
                                    }
                                    _curOperation.Fail(e);
                                    _curOperation = null;
                                }
                                break;
                            default:
                                if (!_cts.IsCancellationRequested) {
                                    // App error - fail and continue
                                    _logger.Debug(e, "Application error occurred talking to {endpoint} " +
                                        "- fail operation.",
                                        _endpointUrl, e.Message);
                                    reconnect = _curOperation is KeepAlive;
                                }
                                else {
                                    // Session was closed while operation in progress - Set cancelled
                                    _logger.Error("Session via {endpoint} was closed " +
                                        "while operation in progress - cancel operation.",
                                        _endpointUrl, e.Message);
                                    reconnect = false;
                                }
                                _curOperation.Fail(e);
                                _curOperation = null;
                                break;
                        }
                        if (reconnect) {
                            await NotifyConnectivityStateChangeAsync(ToConnectivityState(oex, false));
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
                if (_curOperation != null && _curOperation != keepAlive.Item2) {
                    _queue.Enqueue(priority, _curOperation);
                }
                _lastActivity = DateTime.MinValue;

                _logger.Verbose("Closing session...");
                _session?.Close();
                _session = null;
                _logger.Debug("Session closed.");
                keepAlive.Item2?.Dispose();
            }
            _logger.Verbose("Processor stopped.");
        }

        /// <summary>
        /// Wait for a newly enqueued operation using a timeout.
        /// </summary>
        public Task<bool> WaitForNewlyEnqueuedOperationAsync(int timeout) {
            while (!_cts.IsCancellationRequested) {
                var tcs = _enqueueEvent;
                // Wait on a fresh task or on the not yet completed on
                if (tcs.Task.IsCompleted) {
                    var newEvent = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    if (Interlocked.CompareExchange(ref _enqueueEvent, newEvent, tcs) == tcs) {
                        // Exchanged safely now we can wait for it.
                        tcs = newEvent;
                    }
                }
                return Task.WhenAny(tcs.Task, Task.Delay(timeout, _cts.Token))
                    .ContinueWith(t => t.Result == tcs.Task);
            }
            return Task.FromException<bool>(new TaskCanceledException());
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
                _logger.Debug(
                    "Error, connection to {endpoint} - leaving state at {previous}.",
                    _endpointUrl, previous);
                return;
            }

            _lastState = state;
            _logger.Information(
                "Connecting to {endpoint} changed from {previous} to {state}",
                _endpointUrl, previous, state);
            try {
                await _statusCb?.Invoke(_connection, state);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Exception during state callback");
            }

            if (state == EndpointConnectivityState.Ready) {
                // Notify waiting threads that a session is ready
                _acquired.TrySetResult(_session);
            }
            else {
                // Keep any thread waiting
                if (_acquired.Task.IsCompleted) {
                    _acquired = new TaskCompletionSource<Session>();
                }
            }
        }

        /// <summary>
        /// Creates a new session
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        private async Task<Session> CreateSessionAsync(IUserIdentity identity) {

            if (_connection.Endpoint.SecurityMode != SecurityMode.SignAndEncrypt) {
                _logger.Warning("Establishing unencrypted connection.");
            }
            if (_urlQueue.TryDequeue(out var next)) {
                if (_endpointUrl != null && _endpointUrl != next) {
                    _urlQueue.Enqueue(_endpointUrl);
                }
                _endpointUrl = next;
                _logger.Information("Creating session via {endpoint}.", _endpointUrl);
            }
            var selectedEndpoint = await DiscoverEndpointsAsync(_config,
                _connection.Endpoint, new Uri(_endpointUrl), (server, endpoints, channel) =>
                    SelectServerEndpoint(server, endpoints, channel, true));
            if (selectedEndpoint == null) {
                throw new ConnectionException(
                    $"Unable to select secure endpoint on {_connection.Endpoint.Url} via {_endpointUrl}");
            }

            var configuredEndpoint = new ConfiguredEndpoint(selectedEndpoint.Server,
                EndpointConfiguration.Create(_config));
            configuredEndpoint.Update(selectedEndpoint);

            var session = await Session.Create(_config, configuredEndpoint, true, false,
                _sessionName, (uint)(_timeout.TotalMilliseconds * 1.2), identity, null);
            if (session == null) {
                throw new ExternalDependencyException(
                    $"Cannot establish session to {_connection.Endpoint.Url} via {_endpointUrl}.");
            }
            session.KeepAlive += (_, e) => e.CancelKeepAlive = true;
            session.KeepAliveInterval = -1; // No keep alives - we handle those ourselves.
            session.RenewUserIdentity += (_, user) => identity; // Reset back to default.

            try {
                var complexTypeSystem = new ComplexTypeSystem(session);
                await complexTypeSystem.Load();
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to load complex type system");
            }
            return session;
        }

        /// <summary>
        /// Validate session certificate against the endpoint certificate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnValidate(CertificateValidator sender, CertificateValidationEventArgs e) {
            if (!e.Accept && e.Error.StatusCode == StatusCodes.BadCertificateUntrusted &&
                e.Certificate.RawData != null && _connection.Endpoint.Certificate != null) {
                e.Accept = e.Certificate.Thumbprint == _connection.Endpoint.Certificate;
            }
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
                if ((haveCert && (endpoint.SecurityLevel > bestEndpoint.SecurityLevel)) ||
                    (!haveCert && (endpoint.SecurityLevel < bestEndpoint.SecurityLevel))) {
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
            configuration.OperationTimeout = kMaxReconnectDelayWhenPendingOperations;

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
            public virtual bool IsCompleted() {
                return false;
            }

            /// <summary>
            /// Whether to retry
            /// </summary>
            /// <param name="ex"></param>
            public virtual bool ShouldRetry(Exception ex) {
                return true;
            }

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
            public override bool ShouldRetry(Exception ex) {
                return _failures % 2 == 0;
            }

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
                        }, CancellationToken.None);
                    _failures = 0;
                }
                catch {
                    _failures++;
                    throw;
                }
            }

            /// <inheritdoc/>
            public override string ToString() {
                return "KEEP ALIVE";
            }

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
                _timeout = timeout;
                _cts = new CancellationTokenSource(timeout);
                ct?.Register(_cts.Cancel);
                _cts.Token.Register(OnCancellation);
                _ct = ct ?? CancellationToken.None;
            }

            /// <inheritdoc/>
            public Task<T> Completed => _tcs.Task;

            /// <inheritdoc/>
            public override void Dispose() {
                _tcs.TrySetCanceled();
                _cts.Dispose();
            }

            /// <inheritdoc/>
            public override bool IsCompleted() {
                return _tcs.Task.IsCompleted;
            }

            /// <inheritdoc/>
            public override async Task Complete(Session session) {
                // Complete the operation - this can throw - caller will "fail it"
                var result = await Task.Run(() => _operation(session), _cts.Token)
                    .ConfigureAwait(false);
                _tcs.TrySetResult(result);
            }
            /// <inheritdoc/>
            public override void Fail(Exception ex) {
                _tcs.TrySetException(ex);
            }

            /// <inheritdoc/>
            public override bool ShouldRetry(Exception ex) {
                return _handler?.Invoke(ex) ?? false;
            }

            /// <inheritdoc/>
            public override string ToString() {
                return _operation.ToString();
            }

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
                        new TimeoutException($"Operation timeout after {_timeout}"));
                }
            }

            private readonly Func<Session, Task<T>> _operation;
            private readonly Func<Exception, bool> _handler;
            private readonly TaskCompletionSource<T> _tcs =
                new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly CancellationTokenSource _cts;
            private readonly CancellationToken _ct;
            private readonly TimeSpan _timeout;
        }

        /// <summary>
        /// A session handle exposes the session to multiple owners. The session
        /// Handle is reference counted and contains the subscriptions managed
        /// for the owner.
        /// </summary>
        private sealed class ClientSessionHandle : ISessionHandle {

            /// <inheritdoc/>
            public ConnectionModel Connection => _outer._connection;

            /// <inheritdoc/>
            public Session Session => _outer._session;

            /// <inheritdoc/>
            public EndpointConnectivityState State => _outer._lastState;

            /// <inheritdoc/>
            internal ClientSessionHandle(ClientSession outer) {
                _outer = outer;
                lock (_outer._handles) {
                    _outer._handles.Add(this);
                }
            }

            /// <inheritdoc/>
            public Task<Session> AcquireSessionAsync() {
                if (_disposed) {
                    throw new ObjectDisposedException(nameof(ClientSessionHandle));
                }
                return _outer._acquired.Task;
            }

            /// <inheritdoc/>
            public void Dispose() {
                if (_disposed) {
                    throw new ObjectDisposedException(nameof(ClientSessionHandle));
                }
                _disposed = true;
                lock (_outer._handles) {
                    _outer._handles.Remove(this);
                }
            }

            private readonly ClientSession _outer;
            private bool _disposed;
        }

        private const int kMaxReconnectAttempts = 4;
        private const int kMaxRetries = 15;
        private const int kMaxReconnectDelayWhenPendingOperations = 5 * 1000;
        private const int kMaxReconnectDelayWhenNoPendingOperations = 300 * 1000;

        private readonly HashSet<ClientSessionHandle> _handles =
            new HashSet<ClientSessionHandle>();
        private static int _sessionCounter;
        private SessionOperation _curOperation;  // Only update from RunAsync task
        private DateTime _lastActivity;
        private Session _session;
        private TaskCompletionSource<Session> _acquired;
        private EndpointConnectivityState _lastState;
        private string _endpointUrl;
        private readonly TimeSpan _opTimeout;
        private readonly string _sessionName;
        private readonly ILogger _logger;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _keepAlive;
        private readonly ApplicationConfiguration _config;
        private readonly ConnectionModel _connection;
        private readonly Task _processor;
        private readonly CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _urlQueue;
        private readonly System.Collections.Concurrent.PriorityQueue<int, SessionOperation> _queue;
        private volatile TaskCompletionSource<bool> _enqueueEvent;
        private readonly Func<ConnectionModel, EndpointConnectivityState, Task> _statusCb;
    }
}
