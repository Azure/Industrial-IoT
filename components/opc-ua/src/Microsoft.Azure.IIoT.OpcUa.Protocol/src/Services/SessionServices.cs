// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Serilog;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Collections.Concurrent;

    /// <summary>
    /// A generic session manager services for servers.
    /// </summary>
    public class SessionServices : ISessionServices {

        /// <summary>
        /// Create session manager object
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public SessionServices(ISessionServicesConfig configuration,
            ILogger logger) {
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public event EventHandler SessionCreated {
            add {
                _sessionCreated += value;
            }
            remove {
                _sessionCreated -= value;
            }
        }

        /// <inheritdoc/>
        public event EventHandler SessionActivated {
            add {
                _sessionActivated += value;
            }
            remove {
                _sessionActivated -= value;
            }
        }

        /// <inheritdoc/>
        public event EventHandler SessionClosing {
            add {
                _sessionClosing += value;
            }
            remove {
                _sessionClosing -= value;
            }
        }

        /// <inheritdoc/>
        public event EventHandler SessionTimeout {
            add {
                _sessionTimeout += value;
            }
            remove {
                _sessionTimeout -= value;
            }
        }

        /// <inheritdoc/>
        public event UserIdentityHandler ValidateUser {
            add {
                _validateUser += value;
            }
            remove {
                _validateUser -= value;
            }
        }

        /// <inheritdoc/>
        public IList<IServerSession> GetSessions() {
            return new List<IServerSession>(_sessions.Values);
        }

        /// <inheritdoc/>
        public void Dispose() {
            // dispose of all session objects.
            while (true) {
                NodeId id;
                lock (_lock) {
                    id = _sessions.FirstOrDefault().Value?.Id;
                    if (id == null) {
                        break;
                    }
                }
                CloseSession(id); // Removes session from session list
            }
            _shutdownEvent.Dispose();
        }

        /// <inheritdoc/>
        public IServerSession CreateSession(RequestContextModel context,
            EndpointDescription endpoint, X509Certificate2 serverCertificate,
            byte[] clientNonce, X509Certificate2 clientCertificate, double requestedTimeout,
            out NodeId sessionId, out NodeId authenticationToken,
            out byte[] serverNonce, out double revisedTimeout) {

            GatewaySession session = null;
            lock (_lock) {
                // check session count.
                if (_configuration.MaxSessionCount > 0 &&
                    _sessions.Count >= _configuration.MaxSessionCount) {
                    throw new ServiceResultException(StatusCodes.BadTooManySessions);
                }

                // check for same Nonce in another session
                if (clientNonce != null) {
                    foreach (var sessionIterator in _sessions.Values) {
                        if (Utils.CompareNonce(sessionIterator.ClientNonce, clientNonce)) {
                            throw new ServiceResultException(StatusCodes.BadNonceInvalid);
                        }
                    }
                }

                // Create session
                sessionId = new NodeId(Guid.NewGuid());
                authenticationToken = new NodeId(Utils.Nonce.CreateNonce(kDefaultNonceLength));
                var nonceLength = (uint)_configuration.NonceLength;
                serverNonce = Utils.Nonce.CreateNonce(nonceLength == 0 ? kDefaultNonceLength :
                    nonceLength);

                var maxSessionTimeout = _configuration.MaxSessionTimeout.TotalMilliseconds;
                var minSessionTimeout = _configuration.MinSessionTimeout.TotalMilliseconds;
                if (requestedTimeout > maxSessionTimeout) {
                    revisedTimeout = maxSessionTimeout;
                }
                else if (requestedTimeout < minSessionTimeout) {
                    revisedTimeout = minSessionTimeout;
                }
                else {
                    revisedTimeout = requestedTimeout;
                }

                // Add session to list
                session = new GatewaySession(this, context, sessionId, endpoint,
                    clientCertificate, clientNonce, serverCertificate, serverNonce,
                    TimeSpan.FromMilliseconds(revisedTimeout), _configuration.MaxRequestAge,
                    _validateUser);
                if (!_sessions.TryAdd(authenticationToken, session)) {
                    throw new ServiceResultException(StatusCodes.BadInternalError);
                }
            }

            _sessionCreated?.Invoke(session, null);
            return session;
        }

        /// <inheritdoc/>
        public virtual bool ActivateSession(RequestContextModel context,
            NodeId authenticationToken, SignatureData clientSignature,
            List<SoftwareCertificate> clientSoftwareCertificates,
            ExtensionObject userIdentityToken, SignatureData userTokenSignature,
            StringCollection localeIds, out byte[] serverNonce) {

            // find session.
            GatewaySession session = null;
            lock (_lock) {
                if (!_sessions.TryGetValue(authenticationToken, out session)) {
                    throw new ServiceResultException(StatusCodes.BadSessionClosed);
                }
            }

            if (session.IsClosed) {
                throw new ServiceResultException(StatusCodes.BadSessionClosed);
            }

            // Activate session.
            var nonceLength = (uint)_configuration.NonceLength;
            serverNonce = Utils.Nonce.CreateNonce(nonceLength);
            var contextChanged = session.Activate(context, clientSignature,
                clientSoftwareCertificates, userIdentityToken, userTokenSignature,
                serverNonce);
            if (contextChanged) {
                _sessionActivated?.Invoke(session, null);
            }

            // indicates that the identity context for the session has changed.
            return contextChanged;
        }

        /// <inheritdoc/>
        public virtual void CloseSession(NodeId sessionId) {
            var authenticationToken = _sessions.FirstOrDefault(
                s => s.Value.Id == sessionId);
            // If not found, key is null
            if (_sessions.TryRemove(authenticationToken.Key, out var session)) {
                // raise session related event.
                _sessionClosing?.Invoke(session, null);
                Utils.SilentDispose(session);
            }
        }

        /// <inheritdoc/>
        public virtual RequestContextModel GetContext(RequestHeader requestHeader,
            RequestType requestType) {
            if (requestHeader == null) {
                throw new ArgumentNullException(nameof(requestHeader));
            }
            GatewaySession session = null;
            try {
                lock (_lock) {
                    // check for create / activate session request which are handled differently.
                    if (requestType == RequestType.CreateSession) {
                        return new RequestContextModel(requestHeader, requestType);
                    }
                    // find session.
                    if (_sessions.TryGetValue(requestHeader.AuthenticationToken, out session)) {
                        // validate request header in context of this session.
                        session.ValidateRequest(requestHeader, requestType);
                        return new RequestContextModel(requestHeader, requestType, session);
                    }
                    // No session, validate the request as a session less request
                    var identity = ValidateSessionLessRequest(requestHeader.AuthenticationToken,
                        requestType);
                    return new RequestContextModel(requestHeader, requestType, identity);
                }
            }
            catch (Exception e) {
                if (e is ServiceResultException sre &&
                    sre.StatusCode == StatusCodes.BadSessionNotActivated) {
                    if (session != null) {
                        CloseSession(session.Id);
                    }
                }
                throw new ServiceResultException(e, StatusCodes.BadUnexpectedError);
            }
        }

        /// <summary>
        /// Validate session less request
        /// </summary>
        /// <param name="authenticationToken"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        protected virtual IUserIdentity ValidateSessionLessRequest(NodeId authenticationToken,
            RequestType requestType) {
            // Not supported!
            throw new ServiceResultException(StatusCodes.BadSessionIdInvalid);
        }

        /// <summary>
        /// Represents the session
        /// </summary>
        private class GatewaySession : IServerSession, IDisposable {

            /// <inheritdoc/>
            public NodeId Id { get; }

            /// <inheritdoc/>
            public IServiceMessageContext MessageContext { get; }

            /// <inheritdoc/>
            public EndpointDescription Endpoint { get; private set; }

            /// <inheritdoc/>
            public List<IUserIdentity> Identities { get; private set; }

            /// <summary>
            /// The client nonce associated with the session.
            /// </summary>
            public byte[] ClientNonce { get; }

            /// <summary>
            /// Check whether the is still valid
            /// </summary>
            /// <returns></returns>
            internal bool IsClosed => _cts.IsCancellationRequested;

            /// <summary>
            /// Create session
            /// </summary>
            /// <param name="manager"></param>
            /// <param name="context"></param>
            /// <param name="id"></param>
            /// <param name="endpoint"></param>
            /// <param name="clientCertificate"></param>
            /// <param name="clientNonce"></param>
            /// <param name="serverCertificate"></param>
            /// <param name="serverNonce"></param>
            /// <param name="timeout"></param>
            /// <param name="maxRequestAge"></param>
            /// <param name="validator"></param>
            public GatewaySession(SessionServices manager,
                RequestContextModel context, NodeId id, EndpointDescription endpoint,
                X509Certificate2 clientCertificate, byte[] clientNonce,
                X509Certificate2 serverCertificate, byte[] serverNonce,
                TimeSpan timeout, TimeSpan maxRequestAge, UserIdentityHandler validator) {

                if (context == null) {
                    throw new ArgumentNullException(nameof(context));
                }
                if (context.ChannelContext == null) {
                    throw new ServiceResultException(StatusCodes.BadSecureChannelIdInvalid);
                }
                _validator = validator;
                _secureChannelId = context.ChannelContext.SecureChannelId;
                _serverNonce = serverNonce;
                _serverCertificate = serverCertificate;
                _clientCertificate = clientCertificate;
                _maxRequestAge = maxRequestAge;
                Endpoint = endpoint;
                Identities = new List<IUserIdentity>();

                Id = id;
                ClientNonce = clientNonce;
                MessageContext = new ServiceMessageContext();

                _cts = new CancellationTokenSource();
                _timeout = timeout;
                _timeoutTimer = new Timer(o => OnTimeout(manager), null,
                    timeout, Timeout.InfiniteTimeSpan);
            }

            /// <inheritdoc/>
            public void Dispose() {
                _cts.Cancel();
                _timeoutTimer?.Dispose();
                _cts.Dispose();
                _serverCertificate.Dispose();
            }

            /// <inheritdoc/>
            public void ValidateRequest(RequestHeader requestHeader, RequestType requestType) {
                if (requestHeader == null) {
                    throw new ArgumentNullException(nameof(requestHeader));
                }
                lock (_lock) {
                    // get the request context for the current thread.
                    var context = SecureChannelContext.Current;
                    if (context == null || context.SecureChannelId != _secureChannelId) {
                        throw new ServiceResultException(StatusCodes.BadSecureChannelIdInvalid);
                    }
                    // verify that session has been activated.
                    if (!_activated) {
                        if (requestType != RequestType.CloseSession &&
                            requestType != RequestType.ActivateSession) {
                            throw new ServiceResultException(StatusCodes.BadSessionNotActivated);
                        }
                    }
                    // verify timestamp.
                    var maxAge = _maxRequestAge.TotalMilliseconds;
                    if (requestHeader.Timestamp.AddMilliseconds(maxAge) < DateTime.UtcNow) {
                        throw new ServiceResultException(StatusCodes.BadInvalidTimestamp);
                    }
                    _timeoutTimer.Change(_timeout, Timeout.InfiniteTimeSpan);
                }
            }

            /// <summary>
            /// Activates the session and binds it to the current secure channel.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="clientSignature"></param>
            /// <param name="clientSoftwareCertificates"></param>
            /// <param name="userIdentityToken"></param>
            /// <param name="userTokenSignature"></param>
            /// <param name="serverNonce"></param>
            /// <returns></returns>
            internal bool Activate(RequestContextModel context, SignatureData clientSignature,
                List<SoftwareCertificate> clientSoftwareCertificates, ExtensionObject userIdentityToken,
                SignatureData userTokenSignature, byte[] serverNonce) {

                lock (_lock) {
                    var changed = ValidateActivation(context, clientSignature, clientSoftwareCertificates,
                        userIdentityToken, userTokenSignature);
                    if (!_activated) {
                        _activated = true;
                    }
                    else {
                        // Reactivation = bind to the new secure channel.
                        _secureChannelId = context.ChannelContext.SecureChannelId;
                    }

                    // update server nonce.
                    _serverNonce = serverNonce;
                    // build list of signed certificates for audit event.
                    var signedSoftwareCertificates = new List<SignedSoftwareCertificate>();
                    if (clientSoftwareCertificates != null) {
                        foreach (var softwareCertificate in clientSoftwareCertificates) {
                            var item = new SignedSoftwareCertificate {
                                CertificateData = softwareCertificate.SignedCertificate.RawData
                            };
                            signedSoftwareCertificates.Add(item);
                        }
                    }
                    _timeoutTimer.Change(_timeout, Timeout.InfiniteTimeSpan);
                    return changed;
                }
            }

            /// <summary>
            /// Validate before activation
            /// </summary>
            /// <param name="context"></param>
            /// <param name="clientSignature"></param>
            /// <param name="clientSoftwareCertificates"></param>
            /// <param name="userIdentityToken"></param>
            /// <param name="userTokenSignature"></param>
            /// <returns></returns>
            private bool ValidateActivation(RequestContextModel context, SignatureData clientSignature,
                List<SoftwareCertificate> clientSoftwareCertificates, ExtensionObject userIdentityToken,
                SignatureData userTokenSignature) {

                // verify that a secure channel was specified.
                if (context.ChannelContext == null) {
                    throw new ServiceResultException(StatusCodes.BadSecureChannelIdInvalid);
                }

                // verify that the same security policy has been used.
                var endpoint = context.ChannelContext.EndpointDescription;

                if (endpoint.SecurityPolicyUri != Endpoint.SecurityPolicyUri ||
                    endpoint.SecurityMode != Endpoint.SecurityMode) {
                    throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected);
                }

                // verify the client signature.
                if (_clientCertificate != null) {
                    VerifyClientSignature(clientSignature);
                }

                if (!_activated) {
                    // must active the session on the channel that was used to create it.
                    if (_secureChannelId != context.ChannelContext.SecureChannelId) {
                        throw new ServiceResultException(StatusCodes.BadSecureChannelIdInvalid);
                    }
                }
                else {
                    // cannot change the certificates after activation.
                    if (clientSoftwareCertificates != null && clientSoftwareCertificates.Count > 0) {
                        throw new ServiceResultException(StatusCodes.BadInvalidArgument);
                    }
                }
                // validate the user identity token.
                return ValidateUserIdentityToken(userIdentityToken, userTokenSignature);
            }

            /// <summary>
            /// Validates the identity token supplied by the client.
            /// </summary>
            /// <param name="identityToken"></param>
            /// <param name="userTokenSignature"></param>
            /// <returns></returns>
            private bool ValidateUserIdentityToken(ExtensionObject identityToken,
                SignatureData userTokenSignature) {

                UserIdentityToken token = null;
                UserTokenPolicy policy;
                if (identityToken == null || identityToken.Body == null) {
                    if (_activated) {
                        // not changing the token if already activated.
                        return false;
                    }
                    policy = Endpoint.UserIdentityTokens?
                        .FirstOrDefault(t => t.TokenType == UserTokenType.Anonymous);
                    if (policy == null) {
                        throw ServiceResultException.Create(StatusCodes.BadUserAccessDenied,
                            "Anonymous user token policy not supported.");
                    }
                    // create an anonymous token to use for subsequent validation.
                    token = new AnonymousIdentityToken {
                        PolicyId = policy.PolicyId
                    };
                }
                else if (!typeof(UserIdentityToken).IsInstanceOfType(identityToken.Body)) {
                    // Decode identity token from binary.
                    token = DecodeUserIdentityToken(identityToken, out policy);
                }
                else {
                    token = (UserIdentityToken)identityToken.Body;
                    // find the user token policy.
                    policy = Endpoint.FindUserTokenPolicy(token.PolicyId);
                    if (policy == null) {
                        throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid,
                            "User token policy not supported.");
                    }
                }

                // determine the security policy uri.
                var securityPolicyUri = policy.SecurityPolicyUri;
                if (string.IsNullOrEmpty(securityPolicyUri)) {
                    securityPolicyUri = Endpoint.SecurityPolicyUri;
                }

                if (securityPolicyUri != SecurityPolicies.None) {
                    // decrypt the user identity token.
                    if (_serverCertificate == null) {
                        _serverCertificate = CertificateFactory.Create(
                            Endpoint.ServerCertificate, true);
                        // check for valid certificate.
                        if (_serverCertificate == null) {
                            throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                                "ApplicationCertificate cannot be found.");
                        }
                    }
                    try {
                        token.Decrypt(_serverCertificate, _serverNonce, securityPolicyUri);
                    }
                    catch (ServiceResultException) {
                        throw;
                    }
                    catch (Exception e) {
                        throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid,
                            e, "Could not decrypt identity token.");
                    }
                    // ... and verify the signature if any.
                    VerifyUserTokenSignature(userTokenSignature, token, securityPolicyUri);
                }

                // We have a valid token - validate it through the handler chain.
                var arg = new UserIdentityHandlerArgs {
                    CurrentIdentities = Identities,
                    Token = token
                };
                _validator?.Invoke(this, arg);
                if (arg.ValidationException != null) {
                    throw arg.ValidationException;
                }
                if (arg.NewIdentities != null) {
                    Identities = arg.NewIdentities;
                    return true;
                }
                return false; // No new identities
            }

            /// <summary>
            /// Decode user identity token from binary extension object.
            /// </summary>
            /// <param name="identityToken"></param>
            /// <param name="policy"></param>
            /// <returns></returns>
            private UserIdentityToken DecodeUserIdentityToken(ExtensionObject identityToken,
                out UserTokenPolicy policy) {
                if (identityToken.Encoding != ExtensionObjectEncoding.Binary ||
                    !typeof(byte[]).IsInstanceOfType(identityToken.Body)) {
                    throw ServiceResultException.Create(StatusCodes.BadUserAccessDenied,
                        "Invalid user identity token provided.");
                }
                if (!(BaseVariableState.DecodeExtensionObject(null, typeof(UserIdentityToken),
                    identityToken, false) is UserIdentityToken token)) {
                    throw ServiceResultException.Create(StatusCodes.BadUserAccessDenied,
                        "Invalid user identity token provided.");
                }
                policy = Endpoint.FindUserTokenPolicy(token.PolicyId);
                if (policy == null) {
                    throw ServiceResultException.Create(StatusCodes.BadUserAccessDenied,
                        "User token policy not supported.", "ValidateUserIdentityToken");
                }
                switch (policy.TokenType) {
                    case UserTokenType.Anonymous:
                        return BaseVariableState.DecodeExtensionObject(null,
                            typeof(AnonymousIdentityToken), identityToken, true)
                                as AnonymousIdentityToken;
                    case UserTokenType.UserName:
                        return BaseVariableState.DecodeExtensionObject(null,
                            typeof(UserNameIdentityToken), identityToken, true)
                                as UserNameIdentityToken;
                    case UserTokenType.Certificate:
                        return BaseVariableState.DecodeExtensionObject(null,
                            typeof(X509IdentityToken), identityToken, true)
                                as X509IdentityToken;
                    case UserTokenType.IssuedToken:
                        return BaseVariableState.DecodeExtensionObject(null,
                            typeof(IssuedIdentityToken), identityToken, true)
                                as IssuedIdentityToken;
                    default:
                        throw ServiceResultException.Create(StatusCodes.BadUserAccessDenied,
                            "Invalid user identity token provided.");
                }
            }

            /// <summary>
            /// Verify the signature supplied by client
            /// </summary>
            /// <param name="clientSignature"></param>
            private void VerifyClientSignature(SignatureData clientSignature) {
                var dataToSign = Utils.Append(_serverCertificate.RawData, _serverNonce);
                // Verify with leaf certificate
                if (SecurityPolicies.Verify(_clientCertificate, Endpoint.SecurityPolicyUri,
                    dataToSign, clientSignature)) {
                    return;
                }
                // verify entire certificate chain in endpoint.
                var serverCertificateChain = Utils.ParseCertificateChainBlob(
                    Endpoint.ServerCertificate);
                if (serverCertificateChain.Count <= 1) {
                    throw new ServiceResultException(StatusCodes.BadApplicationSignatureInvalid);
                }
                var serverCertificateChainList = new List<byte>();
                for (var i = 0; i < serverCertificateChain.Count; i++) {
                    serverCertificateChainList.AddRange(serverCertificateChain[i].RawData);
                }
                var serverCertificateChainData = serverCertificateChainList.ToArray();
                dataToSign = Utils.Append(serverCertificateChainData, _serverNonce);
                if (!SecurityPolicies.Verify(_clientCertificate,
                    Endpoint.SecurityPolicyUri, dataToSign, clientSignature)) {
                    throw new ServiceResultException(StatusCodes.BadApplicationSignatureInvalid);
                }
            }

            /// <summary>
            /// Verify user token signature
            /// </summary>
            /// <param name="userTokenSignature"></param>
            /// <param name="token"></param>
            /// <param name="securityPolicyUri"></param>
            private void VerifyUserTokenSignature(SignatureData userTokenSignature,
                UserIdentityToken token, string securityPolicyUri) {

                // Verify with leaf certificate
                var dataToSign = Utils.Append(_serverCertificate.RawData, _serverNonce);
                if (token.Verify(dataToSign, userTokenSignature, securityPolicyUri)) {
                    return;
                }
                // Validate the signature with complete chain
                var serverCertificateChain = Utils.ParseCertificateChainBlob(
                    Endpoint.ServerCertificate);
                if (serverCertificateChain.Count <= 1) {
                    throw new ServiceResultException(StatusCodes.BadUserSignatureInvalid,
                        "Invalid user signature!");
                }
                var serverCertificateChainList = new List<byte>();
                for (var i = 0; i < serverCertificateChain.Count; i++) {
                    serverCertificateChainList.AddRange(
                        serverCertificateChain[i].RawData);
                }
                var serverCertificateChainData = serverCertificateChainList.ToArray();
                dataToSign = Utils.Append(serverCertificateChainData, _serverNonce);
                if (!token.Verify(dataToSign, userTokenSignature, securityPolicyUri)) {
                    throw new ServiceResultException(StatusCodes.BadUserSignatureInvalid,
                        "Invalid user signature!");
                }
            }

            /// <summary>
            /// Timer expired, cleanup session
            /// </summary>
            /// <param name="manager"></param>
            private void OnTimeout(SessionServices manager) {
                manager._sessionTimeout?.Invoke(this, null);
                manager.CloseSession(Id); // This calls dispose
            }

            private readonly object _lock = new object();
            private readonly TimeSpan _maxRequestAge;
            private readonly TimeSpan _timeout;
            private readonly X509Certificate2 _clientCertificate;
            private readonly Timer _timeoutTimer;
            private readonly CancellationTokenSource _cts;
            private readonly UserIdentityHandler _validator;
            private bool _activated;
            private byte[] _serverNonce;
            private string _secureChannelId;
            private X509Certificate2 _serverCertificate;
        }

        private const int kDefaultNonceLength = 32;

        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<NodeId, GatewaySession> _sessions =
            new ConcurrentDictionary<NodeId, GatewaySession>();
        private readonly ManualResetEvent _shutdownEvent =
            new ManualResetEvent(true);
        private readonly ISessionServicesConfig _configuration;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ILogger _logger;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning disable IDE1006 // Naming Styles
        private event EventHandler _sessionCreated;
        private event EventHandler _sessionActivated;
        private event EventHandler _sessionTimeout;
        private event EventHandler _sessionClosing;
        private event UserIdentityHandler _validateUser;
#pragma warning restore IDE1006 // Naming Styles
    }
}
