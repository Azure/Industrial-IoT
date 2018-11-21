// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Collections.Concurrent;

    /// <summary>
    /// Wraps a session object to provide serialized access and connection and
    /// user identity management.
    /// </summary>
    public class ClientSession : IClientSession {

        /// <inheritdoc/>
        public X509Certificate2 Certificate {
            get => _cert ?? _factory?.Invoke();
            set => _cert = value;
        }

        /// <inheritdoc/>
        public bool Inactive => DateTime.UtcNow > _lastActivity + _timeout;

        /// <inheritdoc/>
        public int Pending => _queue.Count;

        /// <summary>
        /// Create client session
        /// </summary>
        /// <param name="config"></param>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        /// <param name="sessionName"></param>
        /// <param name="timeout"></param>
        /// <param name="keepAlive"></param>
        /// <param name="endpoint"></param>
        public ClientSession(ApplicationConfiguration config, EndpointModel endpoint,
            Func<X509Certificate2> factory, ILogger logger, string sessionName = null,
            TimeSpan? timeout = null, TimeSpan? keepAlive = null) {
            _logger = logger;
            _factory = factory;
            _endpoint = endpoint;
            _config = config;
            _timeout = timeout ?? TimeSpan.FromMilliseconds(
                config.ClientConfiguration.DefaultSessionTimeout);
            _keepAlive = keepAlive ?? TimeSpan.FromSeconds(5);
            _lastActivity = DateTime.UtcNow;
            _sessionName = sessionName ?? Guid.NewGuid().ToString();
            _processor = Task.Run(() => RunAsync());
        }

        /// <inhjeritdoc/>
        public void Dispose() => CloseAsync().Wait();

        /// <inheritdoc/>
        public async Task CloseAsync() {
            if (!_cts.IsCancellationRequested) {
                _cts.Cancel();
                await _processor;
            }
            // Cancel all remaining outstanding operations
            while (true) {
                var op = await _queue.TakeAsync(TimeSpan.Zero, null,
                    CancellationToken.None);
                if (op == null) {
                    break;
                }
                op.Dispose();
            }
        }

        /// <inheritdoc/>
        public bool TryScheduleServiceCall<T>(Func<Session, Task<T>> serviceCall,
            Func<Exception, bool> handler, CredentialModel elevation,
            out Task<T> completion) {
            if (!_cts.IsCancellationRequested) {
                var op = new ScheduledOperation<T>(serviceCall, handler, elevation);
                if (_queue.TryAdd(op)) {
                    completion = op.Completed;
                    return true;
                }
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
            try {
                await RunAsync(_cts.Token);
            }
            catch (OperationCanceledException) {
                // Expected on cancellation
            }
            catch (Exception ex){
                _logger.Error("Session operation processor exited with exception", ex);
            }
            finally {
                _session?.Close();
                _session = null;
            }
        }

        /// <summary>
        /// Process operations and manage session and handle cancellation
        /// </summary>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct) {

            // Create a static keep alive operation object for the the session
            var keepAlive = new KeepAlive(_keepAlive);

            // We cache the last operation if operation should be retried next loop
            SessionOperation operation = null;
            var reconnect = false;
            var recreate = false;
            var retryCount = 0;

            // Save identity and certificate to update session if there are changes.
            var identity = _endpoint.User.ToStackModel();
            var certificate = Certificate;

            while (!ct.IsCancellationRequested) {
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
                        _logger.Debug("Creating new session ...");
                        _session = await CreateSessionAsync(certificate, identity);
                        _logger.Debug("Session created.");
                    }
                    catch (Exception e) {
                        _logger.Error("Failed to create session - retry.", e);
                        ex = e;
                    }
                }
                if (recreate) {
                    // Try recreate session from current one
                    try {
                        _logger.Debug("Recreating session ...");
                        var session = await Task.Run(() => Session.Recreate(_session), ct);
                        _logger.Debug("Session recreated.");
                        _session.Close();
                        _session = session;
                        recreate = false;
                    }
                    catch (Exception e) {
                        ex = e;
                        _logger.Error("Failed to recreate session - create new one.", e);
                        _session?.Close();
                        _session = null;
                    }
                }
                if (reconnect) {
                    // Try reconnect the session
                    try {
                        _logger.Debug("Reconnecting session ...");
                        await Task.Run(() => _session.Reconnect(), ct);
                        _logger.Debug("Session reconnected.");
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
                                if (retryCount < kMaxReconnectAttempts) {
                                    _logger.Error("Failed to reconnect session - retry.", e);
                                    recreate = false;
                                    reconnect = true; // Try again
                                }
                            }
                        }
                        _logger.Error("Failed to reconnect - recreate session...", e);
                    }
                }

                if (recreate || reconnect || _session == null) {
                    if (ex is ServiceResultException sre) {
                        ex = sre.ToTypedException();
                    }
                    foreach (var op in _queue) {
                        if (ex != null && (!op.ShouldRetry(ex) || retryCount > 2)) {
                            op.Fail(ex);
                        }
                        op.CheckTimeout();
                    }
                    ++retryCount;
                    // Try again to connect with exponential delay
                    await Task.Delay(Retry.Exponential(retryCount, null), ct);
                    continue;
                }

                // We have a session that should work for us, get next operation and complete it.
                retryCount = 0;
                recreate = false;
                reconnect = false;

                if (operation == null) {
                    operation = await _queue.TakeAsync(_keepAlive, keepAlive, _cts.Token);
                    if (ct.IsCancellationRequested) {
                        continue;
                    }
                }
                operation.CheckTimeout();
                if (operation.IsCompleted()) {
                    operation = null; // Already completed, get next
                    continue;
                }
                if (operation == keepAlive) {
                    _logger.Verbose("Sending keep alive message...");
                }
                else {
                    _lastActivity = DateTime.UtcNow;
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
                                _logger.Debug("Failed updating session identity", sre);
                                operation.Fail(sre.ToTypedException());
                                operation = null;
                                continue;
                            }
                            throw;
                        }
                    }
                    await operation.Complete(_session).ConfigureAwait(false);
                    _logger.Verbose("Session operation completed.");
                    operation = null;
                }
                catch (Exception e) {
                    // Process exception - first convert sre into non opc exception
                    if (e is ServiceResultException sre) {
                        e = sre.ToTypedException();
                    }
                    // See which ones we can retry, and which ones we cannot
                    switch (e) {
                        case TimeoutException te:
                        case ServerBusyException sb:
                            if (operation.ShouldRetry(e)) {
                                _logger.Info("Server busy or timeout, try again later...", e);
                                _queue.TryAdd(operation);
                                operation = null;
                            }
                            else {
                                _logger.Error("Service call timeout - fail operation.", e);
                                operation.Fail(e);
                                operation = null;
                            }
                            break;
                        case ConnectionException cn:
                        case ProtocolException pe:
                        case CommunicationException ce:
                            if (operation.ShouldRetry(e)) {
                                _logger.Info("Reconnect and try operation again...", e);
                                // Reconnect session
                                reconnect = true;
                            }
                            else {
                                _logger.Error("Server communication error - fail operation", e);
                                operation.Fail(e);
                                operation = null;
                            }
                            break;
                        default:
                            // App error - fail and continue
                            _logger.Debug("Application error - fail operation", e);
                            operation.Fail(e);
                            operation = null;
                            break;
                    }
                    if (!reconnect) {
                        // Always reconnect on keep alive errors
                        reconnect = operation is KeepAlive;
                    }
                }
            }
            if (operation != null) {
                _queue.TryAdd(operation);
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
                _logger.Warn("Using unsecure connection.");
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
            configuration.OperationTimeout = 5000;

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
            /// Timeout the operation if needed
            /// </summary>
            public virtual void CheckTimeout() { }

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
                                AttributeId = Attributes.Value,
                                DataEncoding = null,
                                IndexRange = null
                            }
                        });
                    _failures = 0;
                }
                catch {
                    _failures++;
                    throw;
                }
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
            public ScheduledOperation(Func<Session, Task<T>> operation,
                Func<Exception, bool> handler, CredentialModel elevation) {
                _operation = operation;
                _handler = handler;
                Identity = elevation.ToStackModel();
                _cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            }

            /// <inheritdoc/>
            public Task<T> Completed => _tcs.Task;

            /// <inheritdoc/>
            public override void Dispose() =>
                _tcs.TrySetCanceled();

            /// <inheritdoc/>
            public override void Fail(Exception ex) =>
                _tcs.TrySetException(ex);

            /// <inheritdoc/>
            public override bool ShouldRetry(Exception ex) =>
                _handler?.Invoke(ex) ?? false;

            /// <inheritdoc/>
            public override void CheckTimeout() {
                if (!_cts.IsCancellationRequested) {
                    return;
                }
                _tcs.TrySetException(new TimeoutException("Operation timeout"));
            }

            /// <inheritdoc/>
            public override bool IsCompleted() => _tcs.Task.IsCompleted;

            /// <inheritdoc/>
            public override async Task Complete(Session session) {
                var result = await _operation(session).ConfigureAwait(false);
                _tcs.TrySetResult(result);
            }

            private readonly Func<Session, Task<T>> _operation;
            private readonly Func<Exception, bool> _handler;
            private readonly TaskCompletionSource<T> _tcs =
                new TaskCompletionSource<T>();
            private readonly CancellationTokenSource _cts;
        }

        private const int kMaxReconnectAttempts = 6;

        private readonly ILogger _logger;
        private readonly Func<X509Certificate2> _factory;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _keepAlive;
        private readonly ApplicationConfiguration _config;
        private readonly EndpointModel _endpoint;

        private readonly Task _processor;
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly AsyncQueue<SessionOperation> _queue =
            new AsyncQueue<SessionOperation>();
        private readonly string _sessionName;
        private DateTime _lastActivity;
        private X509Certificate2 _cert;
        private Session _session;
    }
}
