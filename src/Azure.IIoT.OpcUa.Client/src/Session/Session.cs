// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Redaction;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages a session with a server.
    /// </summary>
    public class Session : SessionBase, IComplexTypeContext, INodeCacheContext
    {
        /// <inheritdoc/>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the endpoint used to connect to the server.
        /// </summary>
        public ConfiguredEndpoint ConfiguredEndpoint { get; }

        /// <summary>
        /// Gets the name assigned to the session.
        /// </summary>
        public string SessionName { get; }

        /// <summary>
        /// Gets the user identity currently used for the session.
        /// </summary>
        public IUserIdentity Identity { get; }

        /// <summary>
        /// Override the client message context with a stable session wide
        /// message context.
        /// </summary>
        public new IServiceMessageContext MessageContext { get; }

        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        public NamespaceTable NamespaceUris => MessageContext.NamespaceUris;

        /// <summary>
        /// Server uris
        /// </summary>
        private StringTable ServerUris => MessageContext.ServerUris;

        /// <summary>
        /// Gets the system context for use with the session.
        /// </summary>
        public ISystemContext SystemContext => _systemContext;

        /// <summary>
        /// Gets the factory used to create encodeable objects that
        /// the server understands.
        /// </summary>
        public IEncodeableFactory Factory => MessageContext.Factory;

        /// <summary>
        /// Gets the cache of nodes fetched from the server.
        /// </summary>
        public INodeCache NodeCache => _nodeCache;

        /// <summary>
        /// Gets the period for wich the server will maintain the
        /// session if there is no communication from the client.
        /// </summary>
        public TimeSpan SessionTimeout { get; private set; }

        /// <summary>
        /// The operation timeout to use for the session
        /// </summary>
        public new TimeSpan OperationTimeout
        {
            get
            {
                var operationTimeout = base.OperationTimeout;
                if (operationTimeout == 0 &&
                    ConfiguredEndpoint.Configuration != null)
                {
                    operationTimeout =
                        ConfiguredEndpoint.Configuration.OperationTimeout;
                }
                if (operationTimeout == 0)
                {
                    return kDefaultOperationTimeout;
                }
                return TimeSpan.FromMilliseconds(operationTimeout);
            }
            set
            {
                var operationTimeout = (int)value.TotalMilliseconds;
                if (operationTimeout == 0)
                {
                    return;
                }
                if (ConfiguredEndpoint.Configuration != null)
                {
                    ConfiguredEndpoint.Configuration.OperationTimeout =
                        operationTimeout;
                }
                base.OperationTimeout = operationTimeout;
            }
        }

        /// <summary>
        /// Gets the subscriptions owned by the session.
        /// </summary>
        public IEnumerable<Subscription> Subscriptions
        {
            get
            {
                lock (SyncRoot)
                {
                    return new ReadOnlyList<Subscription>(_subscriptions);
                }
            }
        }

        /// <summary>
        /// Gets the number of subscriptions owned by the session.
        /// </summary>
        public int SubscriptionCount
        {
            get
            {
                lock (SyncRoot)
                {
                    return _subscriptions.Count;
                }
            }
        }

        /// <summary>
        /// If the subscriptions are deleted when a session is closed.
        /// </summary>
        /// <remarks>
        /// Default <c>true</c>, set to <c>false</c> if subscriptions
        /// need to be transferred or for durable subscriptions.
        /// </remarks>
        public bool DeleteSubscriptionsOnClose { get; set; } = true;

        /// <summary>
        /// If the subscriptions are transferred when a session is
        /// recreated.
        /// </summary>
        /// <remarks>
        /// TODO: Cleanup - Not reconnected. Wrong name, but leaving
        /// for backcompat.
        /// Default <c>false</c>, set to <c>true</c> if subscriptions
        /// should be transferred after recreating the session.
        /// Service must be supported by server.
        /// </remarks>
        public bool TransferSubscriptionsOnRecreate { get; set; }

        /// <summary>
        /// Gets or Sets how frequently the server is pinged to see if
        /// communication is still working.
        /// </summary>
        /// <remarks>
        /// This interval controls how much time elaspes before a
        /// communication error is detected.
        /// If everything is ok the KeepAlive event will be raised
        /// each time this period elapses.
        /// </remarks>
        public TimeSpan KeepAliveInterval
        {
            get => _keepAliveInterval;
            set
            {
                _keepAliveInterval = value;
                StartKeepAliveTimer();
            }
        }

        /// <summary>
        /// Returns true if the session is not receiving keep alives.
        /// </summary>
        /// <remarks>
        /// Set to true if the server does not respond for 2 times
        /// the KeepAliveInterval or if another error was reported.
        /// Set to false is communication is ok or recovered.
        /// </remarks>
        public bool KeepAliveStopped { get; private set; }

        /// <summary>
        /// Gets the TickCount in ms of the last keep alive based
        /// on <see cref="HiResClock.TickCount"/>.
        /// Independent of system time changes.
        /// </summary>
        public int LastKeepAliveTickCount { get; private set; }

        /// <summary>
        /// Gets the number of successfully working publish
        /// workers.
        /// </summary>
        public int GoodPublishRequestCount => _goodPublishRequestCount;

        /// <summary>
        /// Publish workers in failed state
        /// </summary>
        public int BadPublishRequestCount => _badPublishRequestCount;

        /// <summary>
        /// Gets and sets the minimum number of publish requests to be
        /// used in the session.
        /// </summary>
        public int MinPublishWorkerCount { get; set; } = 1;

        /// <summary>
        /// Gets and sets the maximum number of publish requests to
        /// be used in the session.
        /// </summary>
        public int MaxPublishWorkerCount { get; set; } = 15;

        /// <summary>
        /// Get the number of current publishing workers
        /// </summary>
        public int PublishWorkerCount { get; private set; }

        /// <summary>
        /// Session is in the process of connecting as indicated
        /// by the connect lock being taken
        /// </summary>
        private bool Connecting => _connecting.CurrentCount == 0;

        /// <summary>
        /// Constructs a new instance of the <see cref="Session"/> class. The application
        /// configuration is used to look up the certificate if none is provided.
        /// </summary>
        /// <param name="sessionName">The name to assign to the session.</param>
        /// <param name="configuration">The configuration for the client application.</param>
        /// <param name="endpoint">The endpoint used to initialize the channel.</param>
        /// <param name="loggerFactory">A logger factory to use</param>
        /// <param name="identity">The user identity to use for the initial session
        /// open call. Default will be anonymous user</param>
        /// <param name="preferredLocales">The list of preferred locales.</param>
        /// <param name="clientCertificate">The certificate to use for the session. If
        /// set no certificates will be loaded from the certificate store</param>
        /// <param name="sessionTimeout">The session timeout.</param>
        /// <param name="checkDomain">If set to <c>true</c> then the domain in the server
        /// certificate must match the endpoint that was used.</param>
        /// <param name="availableEndpoints">The list of available endpoints returned by
        /// server in GetEndpoints response.</param>
        /// <param name="discoveryProfileUris">The value of profileUris that was used
        /// the call to GetEndpoints request.</param>
        /// <param name="reverseConnectManager">Reverse connect manager if the session
        /// established over a reverse connection</param>
        /// <param name="channel">The channel that should be used to communicate with the
        /// server in the initial open call.</param>
        /// <param name="connection">An established reverse connection that should be
        /// used on initial open call</param>
        public Session(string sessionName, ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint, ILoggerFactory loggerFactory,
            IUserIdentity? identity = null, IReadOnlyList<string>? preferredLocales = null,
            X509Certificate2? clientCertificate = null, TimeSpan? sessionTimeout = null,
            bool checkDomain = true, EndpointDescriptionCollection? availableEndpoints = null,
            StringCollection? discoveryProfileUris = null,
            ReverseConnectManager? reverseConnectManager = null,
            ITransportChannel? channel = null, ITransportWaitingConnection? connection = null)
            : base(channel)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            if (configuration.ClientConfiguration == null ||
                configuration.SecurityConfiguration == null ||
                configuration.CertificateValidator == null)
            {
                throw new ServiceResultException(StatusCodes.BadConfigurationError,
                    "The application configuration for the session is missing fields.");
            }

            MessageContext = channel?.MessageContext ?? configuration.CreateMessageContext();
            LoggerFactory = loggerFactory;
            ConfiguredEndpoint = endpoint;
            SessionName = sessionName;
            Identity = identity ?? new UserIdentity();

            _logger = LoggerFactory.CreateLogger<Session>();
            _keepAliveTimer = new Timer(_ => _keepAliveTrigger.Set());
            _keepAliveWorker = KeepAliveWorkerAsync(_cts.Token);

            _connection = connection;
            _configuration = configuration;
            _instanceCertificate = clientCertificate;
            _reverseConnectManager = reverseConnectManager;
            _checkDomain = checkDomain;
            _discoveryServerEndpoints = availableEndpoints;
            _discoveryProfileUris = discoveryProfileUris;
            if (sessionTimeout.HasValue && sessionTimeout != TimeSpan.Zero)
            {
                SessionTimeout = sessionTimeout.Value;
            }
            else
            {
                SessionTimeout = TimeSpan.FromMilliseconds(
                    configuration.ClientConfiguration.DefaultSessionTimeout);
            }
            _preferredLocales = preferredLocales ?? new[] { CultureInfo.CurrentCulture.Name };
            _nodeCache = new NodeCache(this);
            _systemContext = new SystemContext
            {
                SystemHandle = this,
                EncodeableFactory = Factory,
                NamespaceUris = NamespaceUris,
                ServerUris = ServerUris,
                TypeTable = new Obsolete.TypeTree(_nodeCache),
                PreferredLocales = null,
                SessionId = null,
                UserIdentity = null
            };

            _publishController = PublishControllerAsync(_cts.Token);
            _acks = Channel.CreateUnboundedPrioritized<SubscriptionAcknowledgement>(
                new UnboundedPrioritizedChannelOptions<SubscriptionAcknowledgement>
                {
                    Comparer = Comparer<SubscriptionAcknowledgement>
                        .Create((x, y) => x.SequenceNumber.CompareTo(y.SequenceNumber))
                });
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is Session session)
            {
                if (!ConfiguredEndpoint.Equals(session.Endpoint))
                {
                    return false;
                }

                if (!SessionName.Equals(session.SessionName,
                    StringComparison.Ordinal))
                {
                    return false;
                }

                if (!SessionId.Equals(session.SessionId))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(ConfiguredEndpoint, SessionName, SessionId);
        }

        /// <inheritdoc/>
        public async ValueTask OpenAsync(CancellationToken ct)
        {
            var securityPolicyUri = ConfiguredEndpoint.Description.SecurityPolicyUri;
            // catch security policies which are not supported by core
            if (SecurityPolicies.GetDisplayName(securityPolicyUri) == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadSecurityChecksFailed,
                    "The chosen security policy is not supported by the " +
                    "client to connect to the server.");
            }

            // get identity token.
            var identityToken = Identity.GetIdentityToken();
            // check that the user identity is supported by the endpoint.
            var identityPolicy = ConfiguredEndpoint.Description.FindUserTokenPolicy(
                identityToken.PolicyId);

            if (identityPolicy == null)
            {
                // try looking up by TokenType if the policy id was not found.
                identityPolicy = ConfiguredEndpoint.Description.FindUserTokenPolicy(
                    Identity.TokenType, Identity.IssuedTokenType);
                if (identityPolicy == null)
                {
                    throw ServiceResultException.Create(StatusCodes.BadUserAccessDenied,
                        "Endpoint does not support the user identity type provided.");
                }
                identityToken.PolicyId = identityPolicy.PolicyId;
            }

            var requireEncryption = securityPolicyUri != SecurityPolicies.None;
            if (!requireEncryption)
            {
                requireEncryption =
                    identityPolicy.SecurityPolicyUri != SecurityPolicies.None &&
                    !string.IsNullOrEmpty(identityPolicy.SecurityPolicyUri);
            }

            await _connecting.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                _connected.Reset();
                StopKeepAliveTimer();
                var previousSessionId = SessionId;
                var previousAuthenticationToken = AuthenticationToken;

                _logger.LogInformation("{Action} session {Session} ({Id})...",
                    SessionId != null ? "Recreating" : "Opening", SessionName,
                    SessionId);

                // Ensure channel and optionally a reverse connection exists
                await WaitForReverseConnectIfNeededAsync(ct).ConfigureAwait(false);
                var transportChannel = NullableTransportChannel;
                if (transportChannel == null)
                {
                    _logger.LogInformation("Creating new channel for session {Name}",
                        SessionName);
                    TransportChannel = await CreateChannelAsync(ct).ConfigureAwait(false);
                }

                await CheckCertificatesAreLoadedAsync(ct).ConfigureAwait(false);

                // validate the server certificate /certificate chain.
                X509Certificate2? serverCertificate = null;
                var certificateData = ConfiguredEndpoint.Description.ServerCertificate;

                if (certificateData?.Length > 0)
                {
                    var serverCertificateChain = Utils.ParseCertificateChainBlob(certificateData);
                    if (serverCertificateChain.Count > 0)
                    {
                        serverCertificate = serverCertificateChain[0];
                    }
                    if (requireEncryption)
                    {
                        // validation skipped until IOP isses are resolved.
                        // ValidateServerCertificateApplicationUri(serverCertificate);
                        if (_checkDomain)
                        {
                            await _configuration.CertificateValidator.ValidateAsync(
                                serverCertificateChain, ConfiguredEndpoint,
                                ct).ConfigureAwait(false);
                        }
                        else
                        {
                            await _configuration.CertificateValidator.ValidateAsync(
                                serverCertificateChain, ct).ConfigureAwait(false);
                        }
                    }
                }

                // create a nonce.
                var length = (uint)_configuration.SecurityConfiguration.NonceLength;
                var clientNonce = Utils.Nonce.CreateNonce(length);

                // send the application instance certificate for the client.
                var clientCertificateData = _instanceCertificate?.RawData;
                byte[]? clientCertificateChainData = null;

                if (_instanceCertificateChain?.Count > 0 &&
                    _configuration.SecurityConfiguration.SendCertificateChain)
                {
                    var clientCertificateChain = new List<byte>();
                    for (var i = 0; i < _instanceCertificateChain.Count; i++)
                    {
                        clientCertificateChain.AddRange(_instanceCertificateChain[i].RawData);
                    }
                    clientCertificateChainData = clientCertificateChain.ToArray();
                }

                var clientDescription = new ApplicationDescription
                {
                    ApplicationUri = _configuration.ApplicationUri,
                    ApplicationName = _configuration.ApplicationName,
                    ApplicationType = ApplicationType.Client,
                    ProductUri = _configuration.ProductUri
                };

                var successCreateSession = false;
                CreateSessionResponse? response = null;

                // if security none, first try to connect without certificate
                if (ConfiguredEndpoint.Description.SecurityPolicyUri == SecurityPolicies.None)
                {
                    // first try to connect with client certificate NULL
                    try
                    {
                        response = await CreateSessionAsync(null, clientDescription,
                            ConfiguredEndpoint.Description.Server.ApplicationUri,
                            ConfiguredEndpoint.EndpointUrl.ToString(), SessionName, clientNonce,
                            null, SessionTimeout.TotalMilliseconds,
                            (uint)MessageContext.MaxMessageSize, ct).ConfigureAwait(false);
                        successCreateSession = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(
                            "Create session failed with client certificate NULL. {Error}",
                            ex.Message);
                        successCreateSession = false;
                    }
                }

                if (!successCreateSession)
                {
                    response = await CreateSessionAsync(null, clientDescription,
                        ConfiguredEndpoint.Description.Server.ApplicationUri,
                        ConfiguredEndpoint.EndpointUrl.ToString(), SessionName, clientNonce,
                        clientCertificateChainData ?? clientCertificateData,
                        SessionTimeout.TotalMilliseconds, (uint)MessageContext.MaxMessageSize,
                        ct).ConfigureAwait(false);
                }

                Debug.Assert(response != null);

                var sessionId = response.SessionId;
                var authenticationToken = response.AuthenticationToken;
                var serverNonce = response.ServerNonce;
                var serverCertificateData = response.ServerCertificate;
                var serverSignature = response.ServerSignature;
                var serverEndpoints = response.ServerEndpoints;
                var serverSoftwareCertificates = response.ServerSoftwareCertificates;

                SessionTimeout = TimeSpan.FromMilliseconds(response.RevisedSessionTimeout);
                _maxRequestMessageSize = response.MaxRequestMessageSize;
                // save session id and cookie in base
                base.SessionCreated(sessionId, authenticationToken);

                _logger.LogInformation("Revised session timeout value: {Timeout}.",
                    SessionTimeout);
                _logger.LogInformation("Max response message size value: {MaxResponseSize}.",
                    MessageContext.MaxMessageSize);
                _logger.LogInformation("Max request message size: {MaxRequestSize}.",
                    _maxRequestMessageSize);

                // we need to call CloseSession if CreateSession was successful
                // but some other exception is thrown
                try
                {
                    // verify that the server returned the same instance certificate.
                    ValidateServerCertificateData(serverCertificateData);
                    ValidateServerEndpoints(serverEndpoints);
                    ValidateServerSignature(serverCertificate, serverSignature,
                        clientCertificateData, clientCertificateChainData, clientNonce);

                    HandleSignedSoftwareCertificates(serverSoftwareCertificates);

                    // create the client signature.
                    var dataToSign = Utils.Append(serverCertificate?.RawData, serverNonce);
                    var clientSignature = SecurityPolicies.Sign(_instanceCertificate,
                        securityPolicyUri, dataToSign);

                    // select the security policy for the user token.
                    securityPolicyUri = identityPolicy.SecurityPolicyUri;
                    if (string.IsNullOrEmpty(securityPolicyUri))
                    {
                        securityPolicyUri = ConfiguredEndpoint.Description.SecurityPolicyUri;
                    }

                    var previousServerNonce = NullableTransportChannel?.CurrentToken?.ServerNonce
                        ?? Array.Empty<byte>();

                    // validate server nonce and security parameters for user identity.
                    ValidateServerNonce(Identity, serverNonce, securityPolicyUri,
                        previousServerNonce, ConfiguredEndpoint.Description.SecurityMode);

                    // sign data with user token.
                    var userTokenSignature = identityToken.Sign(dataToSign, securityPolicyUri);
                    // encrypt token.
                    identityToken.Encrypt(serverCertificate, serverNonce, securityPolicyUri);
                    // send the software certificates assigned to the client.
                    var clientSoftwareCertificates = GetSoftwareCertificates();

                    // activate session.
                    var activateResponse = await ActivateSessionAsync(null, clientSignature,
                        clientSoftwareCertificates, new StringCollection(_preferredLocales),
                        new ExtensionObject(identityToken), userTokenSignature,
                        ct).ConfigureAwait(false);

                    serverNonce = activateResponse.ServerNonce;
                    var certificateResults = activateResponse.Results;
                    var certificateDiagnosticInfos = activateResponse.DiagnosticInfos;

                    if (certificateResults != null)
                    {
                        for (var i = 0; i < certificateResults.Count; i++)
                        {
                            _logger.LogInformation(
                                "ActivateSession result[{Index}] = {Result}", i,
                                certificateResults[i]);
                        }
                    }

                    if (clientSoftwareCertificates?.Count > 0 &&
                        (certificateResults == null || certificateResults.Count == 0))
                    {
                        _logger.LogInformation(
                            "Empty results were received for the ActivateSession call.");
                    }

                    // save nonces and update system context.
                    _previousServerNonce = previousServerNonce;
                    _serverNonce = serverNonce;
                    _serverCertificate = serverCertificate;
                    _systemContext.PreferredLocales = new StringCollection(_preferredLocales);
                    _systemContext.SessionId = SessionId;
                    _systemContext.UserIdentity = Identity;

                    _connected.Set();
                    NodeCache.Clear();

                    // fetch namespaces.
                    await FetchNamespaceTablesAsync(ct).ConfigureAwait(false);

                    // fetch operation limits
                    await FetchOperationLimitsAsync(ct).ConfigureAwait(false);

                    await RecreateSubscriptionsAsync(previousSessionId,
                        ct).ConfigureAwait(false);

                    // call session created callback, which was already set in base class only.
                    SessionCreated(sessionId, authenticationToken);
                }
                catch (Exception)
                {
                    _connected.Reset();
                    try
                    {
                        await base.CloseSessionAsync(null, false,
                            CancellationToken.None).ConfigureAwait(false);
                        await CloseChannelAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Cleanup: CloseSessionAsync() or CloseChannelAsync() " +
                            "raised exception {Error}.", e.Message);
                    }
                    finally
                    {
                        SessionCreated(null, null);
                    }
                    throw;
                }
            }
            finally
            {
                _connecting.Release();
            }
            TriggerPublishController();
            StartKeepAliveTimer();
        }

        /// <summary>
        /// Loops with a retry policy
        /// </summary>
        /// <param name="retryPolicy"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask ReconnectAsync(Func<int, TimeSpan>? retryPolicy,
            CancellationToken ct)
        {
            // try a reconnect.
            var tryReconnect = true;
            var attempt = 0;
            retryPolicy ??= x => TimeSpan.FromSeconds(Math.Min(x, 30));
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var tryRecreate = true;
                if (tryReconnect)
                {
                    try
                    {
                        await ReconnectAsync(ct).ConfigureAwait(false);
                        // monitored items should start updating on their own.
                        return;
                    }
                    catch (Exception exception)
                    {
                        tryReconnect = false;

                        // recreate the session if it has been closed.
                        if (exception is ServiceResultException sre)
                        {
                            _logger.LogWarning("Reconnect failed. Reason={Reason}.", sre.Result);

                            // check if the server endpoint could not be reached.
                            switch (sre.StatusCode)
                            {
                                case StatusCodes.BadTcpInternalError:
                                case StatusCodes.BadCommunicationError:
                                case StatusCodes.BadRequestTimeout:
                                case StatusCodes.BadTimeout:
                                    // check if reactivating is still an option.
                                    var timeout = SessionTimeout.Milliseconds -
                                        (HiResClock.TickCount - LastKeepAliveTickCount);
                                    if (timeout <= 0)
                                    {
                                        DetachChannel();
                                        break;
                                    }
                                    _logger.LogInformation(
                                        "Retry to reconnect, est. session timeout in {Timeout} ms.",
                                        timeout);
                                    tryReconnect = true;
                                    tryRecreate = false;
                                    break;
                                // check if the security configuration may have changed
                                case StatusCodes.BadSecurityChecksFailed:
                                case StatusCodes.BadCertificateInvalid:
                                    _updateFromServer = true;
                                    _logger.LogInformation("Reconnect failed due to security check. " +
                                        "Request endpoint update from server. {Message}", sre.Message);
                                    break;

                                case StatusCodes.BadNotConnected:
                                case StatusCodes.BadSecureChannelClosed:
                                case StatusCodes.BadSecureChannelIdInvalid:
                                case StatusCodes.BadServerHalted:
                                    DetachChannel();
                                    break;
                            }
                        }
                        else
                        {
                            _logger.LogError(exception, "Reconnect failed.");
                        }
                    }
                }

                if (tryRecreate)
                {
                    // re-create the session. If the channel was not detached it will be re-opened
                    // on the existing channel. If that fails for whatever reason we detach and
                    // retry.
                    try
                    {
                        await OpenAsync(ct).ConfigureAwait(false);
                        return;
                    }
                    catch (ServiceResultException sre)
                    {
                        if (sre.InnerResult?.StatusCode == StatusCodes.BadSecurityChecksFailed ||
                            sre.InnerResult?.StatusCode == StatusCodes.BadCertificateInvalid)
                        {
                            // schedule endpoint update and retry
                            _updateFromServer = true;
                            attempt /= 2;
                            _logger.LogError("Could not reconnect due to failed security check. " +
                                "Request endpoint update from server. {Message}", Redact.Create(sre));
                        }
                        else
                        {
                            _logger.LogError("Could not reconnect the Session. {Message}",
                                Redact.Create(sre));
                            switch (sre.StatusCode)
                            {
                                case StatusCodes.BadTcpInternalError:
                                case StatusCodes.BadCommunicationError:
                                case StatusCodes.BadNotConnected:
                                case StatusCodes.BadSecureChannelClosed:
                                case StatusCodes.BadSecureChannelIdInvalid:
                                case StatusCodes.BadServerHalted:
                                    // We can just detach, not need to close
                                    DetachChannel();
                                    break;
                                default:
                                    await SafeCloseChannelAsync(ct).ConfigureAwait(false);
                                    break;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        await SafeCloseChannelAsync(ct).ConfigureAwait(false);
                        _logger.LogError("Could not reconnect the Session. {Message}",
                            Redact.Create(exception));
                    }
                }

                await Task.Delay(retryPolicy(++attempt), ct).ConfigureAwait(false);

                async ValueTask SafeCloseChannelAsync(CancellationToken ct)
                {
                    try
                    {
                        await CloseChannelAsync(ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to close channel, detaching.");
                        DetachChannel();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask<StatusCode> CloseAsync(bool closeChannel,
            CancellationToken ct)
        {
            // check if already called.
            if (Disposed)
            {
                return StatusCodes.Good;
            }
            await _connecting.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                _connected.Reset();
                // stop the keep alive timer.
                StopKeepAliveTimer();

                // check if correctly connected.
                var connected = Connected;

                StatusCode result = StatusCodes.Good;
                // close the session with the server.
                if (connected && !KeepAliveStopped)
                {
                    try
                    {
                        // close the session and delete all subscriptions if
                        // specified.
                        var timeout = closeChannel ?
                            TimeSpan.FromSeconds(2) : _keepAliveInterval;
                        var requestHeader = new RequestHeader()
                        {
                            TimeoutHint = timeout > TimeSpan.Zero ?
                                (uint)timeout.TotalMilliseconds :
                                (uint)OperationTimeout.TotalMilliseconds
                        };
                        var response = await base.CloseSessionAsync(requestHeader,
                            DeleteSubscriptionsOnClose, ct).ConfigureAwait(false);
                        // raised notification indicating the session is closed.
                        SessionCreated(null, null);
                    }
                    // don't throw errors on disconnect, but return them
                    // so the caller can log the error.
                    catch (ServiceResultException sre)
                    {
                        _logger.LogDebug(sre, "Error closing session.");
                        result = sre.StatusCode;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex, "Unexpected error closing session.");
                        result = StatusCodes.Bad;
                    }
                }

                if (closeChannel)
                {
                    try
                    {
                        await CloseChannelAsync(ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // eat this - we do not care about it.
                        _logger.LogDebug(ex, "Error closing channel.");
                    }
                }
                return result;
            }
            finally
            {
                _connecting.Release();
            }
        }

        /// <inheritdoc/>
        public sealed override Task<StatusCode> CloseAsync(CancellationToken ct)
        {
            return CloseAsync(true, ct).AsTask();
        }

        /// <inheritdoc/>
        public sealed override void DetachChannel()
        {
            // Overriding to remove any existing connection that was used
            // to create the channel
            _connection = null;
            base.DetachChannel();
        }

        /// <inheritdoc/>
        public async ValueTask ReconnectAsync(CancellationToken ct)
        {
            await _connecting.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                _connected.Reset();
                StopKeepAliveTimer();
                _logger.LogInformation("RECONNECT {Session} starting.", SessionId);
                await CheckCertificatesAreLoadedAsync(ct).ConfigureAwait(false);

                // create the client signature.
                var dataToSign = Utils.Append(_serverCertificate?.RawData, _serverNonce);
                var endpoint = ConfiguredEndpoint.Description;
                var clientSignature = SecurityPolicies.Sign(_instanceCertificate,
                    endpoint.SecurityPolicyUri, dataToSign);

                var identityPolicy = ConfiguredEndpoint.Description.FindUserTokenPolicy(
                    Identity.PolicyId);
                if (identityPolicy == null)
                {
                    throw ServiceResultException.Create(StatusCodes.BadUserAccessDenied,
                        "Endpoint does not support the user identity type provided.");
                }

                // select the security policy for the user token.
                var securityPolicyUri = identityPolicy.SecurityPolicyUri;

                if (string.IsNullOrEmpty(securityPolicyUri))
                {
                    securityPolicyUri = endpoint.SecurityPolicyUri;
                }

                // validate server nonce and security parameters for user identity.
                ValidateServerNonce(Identity, _serverNonce, securityPolicyUri,
                    _previousServerNonce, ConfiguredEndpoint.Description.SecurityMode);

                // sign data with user token.
                var identityToken = Identity.GetIdentityToken();
                identityToken.PolicyId = identityPolicy.PolicyId;
                var userTokenSignature = identityToken.Sign(dataToSign, securityPolicyUri);

                // encrypt token.
                identityToken.Encrypt(_serverCertificate, _serverNonce, securityPolicyUri);

                // send the software certificates assigned to the client.
                var clientSoftwareCertificates = GetSoftwareCertificates();

                _logger.LogInformation("REPLACING channel for {Session}.", SessionId);
                var channel = NullableTransportChannel;

                // check if the channel supports reconnect.
                if (channel != null &&
                    (channel.SupportedFeatures & TransportChannelFeatures.Reconnect) != 0)
                {
                    channel.Reconnect(_connection);
                }
                else
                {
                    // re-create channel, disposes the existing channel.
                    TransportChannel = await CreateChannelAsync(ct).ConfigureAwait(false);
                }

                _logger.LogInformation("RE-ACTIVATING {Session}.", SessionId);
                var header = new RequestHeader
                {
                    TimeoutHint = (uint)kReconnectTimeout.TotalMilliseconds
                };
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(kReconnectTimeout / 2);
                try
                {
                    var activation = await base.ActivateSessionAsync(header, clientSignature,
                        clientSoftwareCertificates, new StringCollection(_preferredLocales),
                        new ExtensionObject(identityToken), userTokenSignature,
                        cts.Token).ConfigureAwait(false);

                    var serverNonce = activation.ServerNonce;
                    var certificateResult = activation.Results;
                    var diagnostic = activation.DiagnosticInfos;

                    _previousServerNonce = _serverNonce;
                    _serverNonce = serverNonce;

                    _logger.LogInformation("RECONNECT {Session} completed successfully.",
                        SessionId);
                    _connected.Set();
                }
                catch (OperationCanceledException e)
                {
                    _logger.LogWarning(
                        "ACTIVATE SESSION timed out. {Good}/{Outstanding}",
                        GoodPublishRequestCount, PublishWorkerCount);
                    throw ServiceResultException.Create(StatusCodes.BadRequestInterrupted, e,
                        "Timeout during activation");
                }
                catch (ServiceResultException sre)
                {
                    _logger.LogWarning(
                        "WARNING: ACTIVATE SESSION failed due to {Error}. {Good}/{Outstanding}",
                        sre.StatusCode, GoodPublishRequestCount, PublishWorkerCount);
                    throw;
                }
            }
            finally
            {
                _connecting.Release();
            }
            TriggerPublishController();
            StartKeepAliveTimer();
        }

        /// <inheritdoc/>
        public async Task FetchNamespaceTablesAsync(CancellationToken ct)
        {
            var (values, errors) = await ReadValuesAsync(null, new[]
            {
                VariableIds.Server_NamespaceArray,
                VariableIds.Server_ServerArray
            }, ct).ConfigureAwait(false);

            // validate namespace array.
            if ((errors.Count == 0 || !ServiceResult.IsBad(errors[0])) &&
                values[0].Value is string[] namespaces)
            {
                NamespaceUris.Update(namespaces);
            }
            else
            {
                _logger.LogError(
                    "Session {Id}: Failed to read NamespaceArray: {Status}",
                    SessionId, errors[0]);
            }
            if ((errors.Count == 0 || !ServiceResult.IsBad(errors[1])) &&
                values[1].Value is string[] serverUris)
            {
                ServerUris.Update(serverUris);
            }
            else
            {
                _logger.LogError(
                    "Session {Id}: Failed to read ServerArray node: {Status} ",
                    SessionId, errors[1]);
            }
        }

        /// <inheritdoc/>
        public async Task<ResultSet<Node>> ReadNodesAsync(IReadOnlyList<NodeId> nodeIds,
            CancellationToken ct)
        {
            if (nodeIds.Count == 0)
            {
                return ResultSet.Empty<Node>();
            }

            var nodeCollection = new NodeCollection(nodeIds.Count);
            // first read only nodeclasses for nodes from server.
            var itemsToRead = new ReadValueIdCollection(nodeIds
                .Select(nodeId => new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.NodeClass
                }));

            var readResponse = await ReadAsync(null, 0, TimestampsToReturn.Neither,
                itemsToRead, ct).ConfigureAwait(false);

            var nodeClassValues = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;
            ClientBase.ValidateResponse(nodeClassValues, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            // second determine attributes to read per nodeclass
            var attributesPerNodeId = new List<IDictionary<uint, DataValue?>?>(
                nodeIds.Count);

            var serviceResults = new List<ServiceResult>(nodeIds.Count);
            var attributesToRead = new ReadValueIdCollection();
            var responseHeader = readResponse.ResponseHeader;
            int? nodeClass;
            for (var index = 0; index < itemsToRead.Count; index++)
            {
                var node = new Node
                {
                    NodeId = itemsToRead[index].NodeId
                };
                if (!DataValue.IsGood(nodeClassValues[index]))
                {
                    nodeCollection.Add(node);
                    serviceResults.Add(new ServiceResult(
                        nodeClassValues[index].StatusCode, index, diagnosticInfos,
                        responseHeader.StringTable));
                    attributesPerNodeId.Add(null);
                    continue;
                }

                // check for valid node class.
                nodeClass = nodeClassValues[index].Value as int?;

                if (nodeClass == null)
                {
                    nodeCollection.Add(node);
                    serviceResults.Add(ServiceResult.Create(StatusCodes.BadUnexpectedError,
                        "Node does not have a valid value for NodeClass: {0}.",
                        nodeClassValues[index].Value));
                    attributesPerNodeId.Add(null);
                    continue;
                }

                node.NodeClass = (NodeClass)nodeClass;

                var attributes = CreateAttributes(node.NodeClass);
                foreach (var attributeId in attributes.Keys)
                {
                    var itemToRead = new ReadValueId
                    {
                        NodeId = node.NodeId,
                        AttributeId = attributeId
                    };
                    attributesToRead.Add(itemToRead);
                }

                nodeCollection.Add(node);
                serviceResults.Add(ServiceResult.Good);
                attributesPerNodeId.Add(attributes);
            }

            if (attributesToRead.Count > 0)
            {
                readResponse = await ReadAsync(null, 0, TimestampsToReturn.Neither,
                    attributesToRead, ct).ConfigureAwait(false);

                var values = readResponse.Results;
                diagnosticInfos = readResponse.DiagnosticInfos;
                ClientBase.ValidateResponse(values, attributesToRead);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, attributesToRead);
                responseHeader = readResponse.ResponseHeader;
                var readIndex = 0;
                for (var index = 0; index < nodeCollection.Count; index++)
                {
                    var attributes = attributesPerNodeId[index];
                    if (attributes == null)
                    {
                        continue;
                    }

                    var readCount = attributes.Count;
                    var subRangeAttributes = new ReadValueIdCollection(
                        attributesToRead.GetRange(readIndex, readCount));
                    var subRangeValues = new DataValueCollection(
                        values.GetRange(readIndex, readCount));
                    var subRangeDiagnostics = diagnosticInfos.Count > 0 ?
                        new DiagnosticInfoCollection(diagnosticInfos.GetRange(readIndex, readCount)) :
                        diagnosticInfos;
                    try
                    {
                        nodeCollection[index] = ProcessReadResponse(responseHeader, attributes,
                            subRangeAttributes, subRangeValues, subRangeDiagnostics);
                        serviceResults[index] = ServiceResult.Good;
                    }
                    catch (ServiceResultException sre)
                    {
                        serviceResults[index] = sre.Result;
                    }
                    readIndex += readCount;
                }
            }
            return new ResultSet<Node>(nodeCollection, serviceResults);
        }

        /// <inheritdoc/>
        public async Task<Node> ReadNodeAsync(NodeId nodeId, CancellationToken ct)
        {
            // build list of attributes.
            var attributes = CreateAttributes();
            var itemsToRead = new ReadValueIdCollection(attributes.Keys
                .Select(attributeId => new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = attributeId
                }));
            // read from server.
            var readResponse = await ReadAsync(null, 0, TimestampsToReturn.Neither,
                itemsToRead, ct).ConfigureAwait(false);
            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;
            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);
            return ProcessReadResponse(readResponse.ResponseHeader, attributes,
                itemsToRead, values, diagnosticInfos);
        }

        /// <inheritdoc/>
        public async Task<ReferenceDescriptionCollection> FetchReferencesAsync(
            NodeId nodeId, CancellationToken ct)
        {
            var (descriptions, _) = await BrowseAsync(null, null,
                (IReadOnlyList<NodeId>)(new[] { nodeId }),
                0, BrowseDirection.Both, ReferenceTypeIds.References, true, 0,
                ct).ConfigureAwait(false);
            return descriptions[0];
        }

        /// <inheritdoc/>
        public Task<ResultSet<ReferenceDescriptionCollection>> FetchReferencesAsync(
            IReadOnlyList<NodeId> nodeIds, CancellationToken ct)
        {
            return BrowseAsync(null, null, nodeIds, 0, BrowseDirection.Both,
                ReferenceTypeIds.References, true, 0, ct);
        }

        /// <inheritdoc/>
        public async ValueTask<DataValue> ReadValueAsync(RequestHeader? header,
            NodeId nodeId, CancellationToken ct)
        {
            var itemsToRead = new ReadValueIdCollection
            {
                new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value
                }
            };
            // read from server.
            var readResponse = await ReadAsync(header, 0, TimestampsToReturn.Both,
                itemsToRead, ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;
            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            if (StatusCode.IsBad(values[0].StatusCode))
            {
                var result = ClientBase.GetResult(values[0].StatusCode, 0,
                    diagnosticInfos, readResponse.ResponseHeader);
                throw new ServiceResultException(result);
            }
            return values[0];
        }

        /// <inheritdoc/>
        public async ValueTask<ResultSet<DataValue>> ReadValuesAsync(RequestHeader? header,
            IReadOnlyList<NodeId> nodeIds, CancellationToken ct)
        {
            if (nodeIds.Count == 0)
            {
                return ResultSet.Empty<DataValue>();
            }

            // read all values from server.
            var itemsToRead = new ReadValueIdCollection(
                nodeIds.Select(nodeId => new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value
                }));

            // read from server.
            var errors = new List<ServiceResult>(itemsToRead.Count);

            var readResponse = await ReadAsync(header, 0, TimestampsToReturn.Both,
                itemsToRead, ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            foreach (var value in values)
            {
                var result = ServiceResult.Good;
                if (StatusCode.IsBad(value.StatusCode))
                {
                    result = ClientBase.GetResult(values[0].StatusCode, 0,
                        diagnosticInfos, readResponse.ResponseHeader);
                }
                errors.Add(result);
            }
            return new ResultSet<DataValue>(values, errors);
        }

        /// <inheritdoc/>
        public async Task<(
            ResponseHeader responseHeader,
            ByteStringCollection continuationPoints,
            IList<ReferenceDescriptionCollection> referencesList,
            IList<ServiceResult> errors
            )> BrowseAsync(RequestHeader? requestHeader, ViewDescription? view,
            IList<NodeId> nodesToBrowse, uint maxResultsToReturn,
            BrowseDirection browseDirection, NodeId referenceTypeId,
            bool includeSubtypes, uint nodeClassMask, CancellationToken ct)
        {
            var browseDescriptions = new BrowseDescriptionCollection();
            foreach (var nodeToBrowse in nodesToBrowse)
            {
                var description = new BrowseDescription
                {
                    NodeId = nodeToBrowse,
                    BrowseDirection = browseDirection,
                    ReferenceTypeId = referenceTypeId,
                    IncludeSubtypes = includeSubtypes,
                    NodeClassMask = nodeClassMask,
                    ResultMask = (uint)BrowseResultMask.All
                };

                browseDescriptions.Add(description);
            }

            var browseResponse = await BrowseAsync(requestHeader, view,
                maxResultsToReturn, browseDescriptions, ct).ConfigureAwait(false);
            ClientBase.ValidateResponse(browseResponse.ResponseHeader);
            var results = browseResponse.Results;
            var diagnosticInfos = browseResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(results, browseDescriptions);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, browseDescriptions);

            var index = 0;
            var errors = new List<ServiceResult>();
            var continuationPoints = new ByteStringCollection();
            var referencesList = new List<ReferenceDescriptionCollection>();
            foreach (var result in results)
            {
                if (StatusCode.IsBad(result.StatusCode))
                {
                    errors.Add(new ServiceResult(result.StatusCode, index, diagnosticInfos,
                        browseResponse.ResponseHeader.StringTable));
                }
                else
                {
                    errors.Add(ServiceResult.Good);
                }
                continuationPoints.Add(result.ContinuationPoint);
                referencesList.Add(result.References);
                index++;
            }

            return (browseResponse.ResponseHeader, continuationPoints, referencesList, errors);
        }

        private async Task<ResultSet<ReferenceDescriptionCollection>> BrowseAsync(
            RequestHeader? requestHeader, ViewDescription? view,
            IReadOnlyList<NodeId> nodesToBrowse, uint maxResultsToReturn,
            BrowseDirection browseDirection, NodeId referenceTypeId,
            bool includeSubtypes, uint nodeClassMask, CancellationToken ct)
        {
            var count = nodesToBrowse.Count;
            var result = new List<ReferenceDescriptionCollection>(count);
            var errors = new List<ServiceResult>(count);

            // first attempt for implementation: create the references for
            // the output in advance. optimize later, when everything works
            // fine.
            for (var i = 0; i < nodesToBrowse.Count; i++)
            {
                result.Add(new ReferenceDescriptionCollection());
                errors.Add(new ServiceResult(StatusCodes.Good));
            }

            try
            {
                // in the first pass, we browse all nodes from the input.
                // Some nodes may need to be browsed again, these are then
                // fed into the next pass.
                var nodesToBrowseForPass = new List<NodeId>(count);
                nodesToBrowseForPass.AddRange(nodesToBrowse);

                var resultForPass = new List<ReferenceDescriptionCollection>(count);
                resultForPass.AddRange(result);
                var errorsForPass = new List<ServiceResult>(count);
                errorsForPass.AddRange(errors);
                var passCount = 0;
                do
                {
                    var badNoCPErrorsPerPass = 0;
                    var badCPInvalidErrorsPerPass = 0;
                    var otherErrorsPerPass = 0;
                    var maxNodesPerBrowse = OperationLimits.MaxNodesPerBrowse;

                    if (OperationLimits.MaxBrowseContinuationPoints > 0)
                    {
                        maxNodesPerBrowse =
                            OperationLimits.MaxBrowseContinuationPoints < maxNodesPerBrowse ?
                            OperationLimits.MaxBrowseContinuationPoints : maxNodesPerBrowse;
                    }

                    // split input into batches
                    var batchOffset = 0;

                    var nodesToBrowseForNextPass = new List<NodeId>();
                    var referenceDescriptionsForNextPass =
                        new List<ReferenceDescriptionCollection>();
                    var errorsForNextPass = new List<ServiceResult>();

                    // loop over the batches
                    foreach (var nodesToBrowseBatch in nodesToBrowseForPass
                        .Batch<NodeId, List<NodeId>>(maxNodesPerBrowse))
                    {
                        var nodesToBrowseBatchCount = nodesToBrowseBatch.Count;
                        var (resultForBatch, errorsForBatch) = await BrowseNextAsync(
                            requestHeader, view, nodesToBrowseBatch,
                            maxResultsToReturn, browseDirection, referenceTypeId,
                            includeSubtypes, nodeClassMask, ct).ConfigureAwait(false);
                        var resultOffset = batchOffset;
                        for (var index = 0; index < nodesToBrowseBatchCount; index++)
                        {
                            var statusCode = errorsForBatch[index].StatusCode;
                            if (StatusCode.IsBad(statusCode))
                            {
                                var addToNextPass = false;
                                switch (statusCode.CodeBits)
                                {
                                    case StatusCodes.BadNoContinuationPoints:
                                        addToNextPass = true;
                                        badNoCPErrorsPerPass++;
                                        break;
                                    case StatusCodes.BadContinuationPointInvalid:
                                        addToNextPass = true;
                                        badCPInvalidErrorsPerPass++;
                                        break;
                                    default:
                                        otherErrorsPerPass++;
                                        break;
                                }

                                if (addToNextPass)
                                {
                                    nodesToBrowseForNextPass.Add(
                                        nodesToBrowseForPass[resultOffset]);
                                    referenceDescriptionsForNextPass.Add(
                                        resultForPass[resultOffset]);
                                    errorsForNextPass.Add(
                                        errorsForPass[resultOffset]);
                                }
                            }

                            resultForPass[resultOffset].Clear();
                            resultForPass[resultOffset].AddRange(resultForBatch[index]);
                            errorsForPass[resultOffset] = errorsForBatch[index];
                            resultOffset++;
                        }
                        batchOffset += nodesToBrowseBatchCount;
                    }

                    resultForPass = referenceDescriptionsForNextPass;
                    referenceDescriptionsForNextPass =
                        new List<ReferenceDescriptionCollection>();

                    errorsForPass = errorsForNextPass;
                    errorsForNextPass = new List<ServiceResult>();

                    nodesToBrowseForPass = nodesToBrowseForNextPass;
                    nodesToBrowseForNextPass = new List<NodeId>();

                    const string aggregatedErrorMessage =
                        "ManagedBrowse: in pass {Count}, Errors={ErrorsInPass} occured with a status code {Status}.";
                    if (badCPInvalidErrorsPerPass > 0)
                    {
                        _logger.LogDebug(aggregatedErrorMessage, passCount, badCPInvalidErrorsPerPass,
                            nameof(StatusCodes.BadContinuationPointInvalid));
                    }
                    if (badNoCPErrorsPerPass > 0)
                    {
                        _logger.LogDebug(aggregatedErrorMessage, passCount, badNoCPErrorsPerPass,
                            nameof(StatusCodes.BadNoContinuationPoints));
                    }
                    if (otherErrorsPerPass > 0)
                    {
                        _logger.LogDebug(aggregatedErrorMessage, passCount, otherErrorsPerPass,
                            $"different from {nameof(StatusCodes.BadNoContinuationPoints)} or {nameof(StatusCodes.BadContinuationPointInvalid)}");
                    }
                    if (otherErrorsPerPass == 0 && badCPInvalidErrorsPerPass == 0 && badNoCPErrorsPerPass == 0)
                    {
                        _logger.LogTrace("ManagedBrowse completed with no errors.");
                    }

                    passCount++;
                } while (nodesToBrowseForPass.Count > 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ManagedBrowse failed");
            }
            return new ResultSet<ReferenceDescriptionCollection>(result, errors);
        }

        /// <summary>
        /// Used to pass on references to the Service results in the
        /// loop in ManagedBrowseAsync.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class ReferenceWrapper<T>
        {
            public required T Reference { get; set; }
        }

        /// <summary>
        /// Call the browse service asynchronously and call browse next,
        /// if applicable, immediately afterwards. Observe proper treatment
        /// of specific service results, specifically
        /// BadNoContinuationPoint and BadContinuationPointInvalid
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodeIds"></param>
        /// <param name="maxResultsToReturn"></param>
        /// <param name="browseDirection"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="nodeClassMask"></param>
        /// <param name="ct"></param>
        private async Task<ResultSet<ReferenceDescriptionCollection>> BrowseNextAsync(
            RequestHeader? requestHeader, ViewDescription? view, List<NodeId> nodeIds,
            uint maxResultsToReturn, BrowseDirection browseDirection, NodeId referenceTypeId,
            bool includeSubtypes, uint nodeClassMask, CancellationToken ct)
        {
            if (requestHeader != null)
            {
                requestHeader.RequestHandle = 0;
            }

            var result = new List<ReferenceDescriptionCollection>(nodeIds.Count);
            var (_, continuationPoints, referenceDescriptions, errors) = await BrowseAsync(
                requestHeader, view, (IList<NodeId>)nodeIds, maxResultsToReturn, browseDirection,
                referenceTypeId, includeSubtypes, nodeClassMask, ct).ConfigureAwait(false);
            result.AddRange(referenceDescriptions);

            // process any continuation point.
            var previousResults = result;
            var errorAnchors = new List<ReferenceWrapper<ServiceResult>>();
            var previousErrors = new List<ReferenceWrapper<ServiceResult>>();
            foreach (var error in errors)
            {
                previousErrors.Add(new ReferenceWrapper<ServiceResult>
                {
                    Reference = error
                });
                errorAnchors.Add(previousErrors[^1]);
            }

            var nextContinuationPoints = new ByteStringCollection();
            var nextResults = new List<ReferenceDescriptionCollection>();
            var nextErrors = new List<ReferenceWrapper<ServiceResult>>();
            for (var index = 0; index < nodeIds.Count; index++)
            {
                if (continuationPoints[index] != null &&
                    !StatusCode.IsBad(previousErrors[index].Reference.StatusCode))
                {
                    nextContinuationPoints.Add(continuationPoints[index]);
                    nextResults.Add(previousResults[index]);
                    nextErrors.Add(previousErrors[index]);
                }
            }
            while (nextContinuationPoints.Count > 0)
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }
                var (_, revisedContinuationPoints, browseNextResults, browseNextErrors)
                    = await BrowseNextAsync(requestHeader, nextContinuationPoints,
                    false, ct).ConfigureAwait(false);
                for (var index = 0; index < browseNextResults.Count; index++)
                {
                    nextResults[index].AddRange(browseNextResults[index]);
                    nextErrors[index].Reference = browseNextErrors[index];
                }

                previousResults = nextResults;
                previousErrors = nextErrors;

                nextResults = new List<ReferenceDescriptionCollection>();
                nextErrors = new List<ReferenceWrapper<ServiceResult>>();
                nextContinuationPoints = new ByteStringCollection();

                for (var index = 0; index < revisedContinuationPoints.Count; index++)
                {
                    if (revisedContinuationPoints[index] != null &&
                        !StatusCode.IsBad(browseNextErrors[index].StatusCode))
                    {
                        nextContinuationPoints.Add(revisedContinuationPoints[index]);
                        nextResults.Add(previousResults[index]);
                        nextErrors.Add(previousErrors[index]);
                    }
                }
            }
            var finalErrors = new List<ServiceResult>(errorAnchors.Count);
            foreach (var errorReference in errorAnchors)
            {
                finalErrors.Add(errorReference.Reference);
            }
            return new ResultSet<ReferenceDescriptionCollection>(result, finalErrors);
        }

        private async Task<(
            ResponseHeader responseHeader,
            ByteStringCollection revisedContinuationPoints,
            IList<ReferenceDescriptionCollection> referencesList,
            IList<ServiceResult> errors
            )> BrowseNextAsync(RequestHeader? requestHeader,
            ByteStringCollection continuationPoints, bool releaseContinuationPoint,
            CancellationToken ct)
        {
            var response = await base.BrowseNextAsync(
                requestHeader,
                releaseContinuationPoint,
                continuationPoints,
                ct).ConfigureAwait(false);

            ClientBase.ValidateResponse(response.ResponseHeader);
            var results = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;

            ClientBase.ValidateResponse(results, continuationPoints);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);
            var index = 0;
            var errors = new List<ServiceResult>();
            var revisedContinuationPoints = new ByteStringCollection();
            var referencesList = new List<ReferenceDescriptionCollection>();
            foreach (var result in results)
            {
                if (StatusCode.IsBad(result.StatusCode))
                {
                    errors.Add(new ServiceResult(result.StatusCode,
                        index, diagnosticInfos, response.ResponseHeader.StringTable));
                }
                else
                {
                    errors.Add(ServiceResult.Good);
                }
                revisedContinuationPoints.Add(result.ContinuationPoint);
                referencesList.Add(result.References);
                index++;
            }

            return (response.ResponseHeader, revisedContinuationPoints,
                referencesList, errors);
        }

        /// <inheritdoc/>
        public bool AddSubscription(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription);
            lock (SyncRoot)
            {
                if (subscription.Session != this)
                {
                    return false;
                }

                if (_subscriptions.Contains(subscription))
                {
                    return false;
                }

                _subscriptions.Add(subscription);
            }
            TriggerPublishController();
            return true;
        }

        /// <summary>
        /// Get subscription with the specified id
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        public Subscription? GetSubscription(uint subscriptionId)
        {
            lock (SyncRoot)
            {
                // find the subscription.
                foreach (var subscription in _subscriptions)
                {
                    if (subscription.Id == subscriptionId)
                    {
                        return subscription;
                    }
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveSubscriptionAsync(Subscription subscription,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(subscription);
            if (subscription.Created)
            {
                await subscription.DeleteAsync(false, ct).ConfigureAwait(false);
            }
            lock (SyncRoot)
            {
                if (!_subscriptions.Remove(subscription))
                {
                    return false;
                }
            }
            TriggerPublishController();
            return true;
        }

        /// <summary>
        /// Trigger publish controller
        /// </summary>
        public void TriggerPublishController()
        {
            _publishControl.Set();
        }

        /// <summary>
        /// Subscriptionss queue acknoledgements for completed
        /// notifications as soon as they are dispatched / handled.
        /// </summary>
        /// <param name="ack"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal ValueTask QueueAcknowledgementAsync(
            SubscriptionAcknowledgement ack, CancellationToken ct)
        {
            return _acks.Writer.WriteAsync(ack, ct);
        }

        /// <summary>
        /// Closes the session and the underlying channel.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    StopKeepAliveTimer();
                    _cts.Cancel();
                    _publishControl.Set();
                    _keepAliveTrigger.Set();
                    _nodeCache.Clear();

                    List<Subscription>? subscriptions = null;
                    lock (SyncRoot)
                    {
                        subscriptions = new List<Subscription>(_subscriptions);
                        _subscriptions.Clear();
                    }

                    foreach (var subscription in subscriptions)
                    {
                        subscription.Dispose();
                    }
                    subscriptions.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during dispose");
                }
            }

            base.Dispose(disposing);

            if (disposing)
            {
                try
                {
                    // Should not throw
                    _keepAliveWorker.GetAwaiter().GetResult();
                    _publishController.GetAwaiter().GetResult();
                    _connected.Reset();
                }
                finally
                {
                    _keepAliveTimer.Dispose();
                    _connecting.Dispose();
                    _cts.Dispose();
                }
            }
        }

        /// <summary>
        /// Handle keep alive
        /// </summary>
        /// <param name="serviceResult"></param>
        /// <param name="serverState"></param>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        protected virtual void OnKeepAlive(ServiceResult serviceResult, ServerState serverState,
            DateTime currentTime)
        {
            // TODO: Implement automatic reconnect handling
        }

        /// <summary>
        /// Returns the software certificates assigned to the application.
        /// </summary>
        protected virtual SignedSoftwareCertificateCollection GetSoftwareCertificates()
        {
            return new SignedSoftwareCertificateCollection();
        }

        /// <summary>
        /// Handles an error when validating software certificates provided by the server.
        /// </summary>
        /// <param name="signedCertificate"></param>
        /// <param name="result"></param>
        /// <exception cref="ServiceResultException"></exception>
        protected virtual void OnSoftwareCertificateError(
            SignedSoftwareCertificate signedCertificate, ServiceResult result)
        {
            throw new ServiceResultException(result);
        }

        /// <summary>
        /// Inspects the software certificates provided by the server.
        /// </summary>
        /// <param name="softwareCertificates"></param>
        protected virtual void ValidateSoftwareCertificates(
            IReadOnlyList<SoftwareCertificate> softwareCertificates)
        {
            // always accept valid certificates.
        }

        /// <inheritdoc/>
        protected sealed override void RequestCompleted(IServiceRequest request,
            IServiceResponse response, string serviceName)
        {
            var sr = response?.ResponseHeader?.ServiceResult;
            if (sr != null && ServiceResult.IsGood(sr))
            {
                LastKeepAliveTickCount = HiResClock.TickCount;
                _keepAliveTimer.Change(_keepAliveCounter, _keepAliveCounter);
            }
            base.RequestCompleted(request, response, serviceName);
        }

        /// <summary>
        /// Starts a timer to check that the connection to the server
        /// is still available.
        /// </summary>
        private void StartKeepAliveTimer()
        {
            LastKeepAliveTickCount = HiResClock.TickCount;
            _keepAliveTimer.Change(_keepAliveCounter, _keepAliveCounter);
        }

        /// <summary>
        /// Stops the keep alive timer.
        /// </summary>
        private void StopKeepAliveTimer()
        {
            _keepAliveTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Sends a keep alive by reading from the server.
        /// </summary>
        /// <param name="ct"></param>
        private async Task KeepAliveWorkerAsync(CancellationToken ct)
        {
            try
            {
                long _lastKeepAliveTime = 0;
                ServiceResult lastKeepAliveError;
                while (!ct.IsCancellationRequested)
                {
                    await _keepAliveTrigger.WaitAsync(ct).ConfigureAwait(false);
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    if (!Connected || Connecting)
                    {
                        _logger.LogDebug("Session {Id}: KeepAlive ignored while (re-)connecting.",
                            SessionId);
                        continue;
                    }
                    try
                    {
                        var serverState = await ReadValueAsync(new RequestHeader
                        {
                            RequestHandle = Utils.IncrementIdentifier(ref _keepAliveCounter),
                            TimeoutHint = (uint)(KeepAliveInterval.TotalMilliseconds * 2),
                            ReturnDiagnostics = 0
                        }, VariableIds.Server_ServerStatus_State, ct).ConfigureAwait(false);

                        if (serverState.Value is not int state)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadDataUnavailable,
                                "Keep alive returned invalid server state");
                        }

                        lastKeepAliveError = ServiceResult.Good;
                        _lastKeepAliveTime = DateTime.UtcNow.Ticks;
                        LastKeepAliveTickCount = HiResClock.TickCount;

                        // send notification that keep alive completed.
                        KeepAliveHandler(serverState.ServerTimestamp, (ServerState)state);
                    }
                    catch (ServiceResultException sre)
                    {
                        lastKeepAliveError = sre.Result;
                        if (sre.StatusCode == StatusCodes.BadNoCommunication)
                        {
                            //keep alive read timed out
                            var delta = HiResClock.TickCount - LastKeepAliveTickCount;
                            _logger.LogInformation("KEEP ALIVE LATE: {Late}ms, " +
                                "EndpointUrl={Url}, RequestCount={Good}/{Outstanding}",
                                delta, Endpoint?.EndpointUrl, GoodPublishRequestCount,
                                PublishWorkerCount);
                        }
                        KeepAliveHandler(DateTime.UtcNow, ServerState.Unknown);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Could not send keep alive request: {ErrorType} {Message}",
                            e.GetType().FullName, e.Message);
                    }

                    void KeepAliveHandler(DateTime currentTime, ServerState serverState)
                    {
                        var lastKeepAliveErrorStatusCode = lastKeepAliveError;
                        if (ServiceResult.IsGood(lastKeepAliveError) ||
                            lastKeepAliveError.StatusCode == StatusCodes.BadNoCommunication)
                        {
                            var delta = TimeSpan.FromMilliseconds(
                                HiResClock.TickCount - LastKeepAliveTickCount);

                            // add a guard band to allow for network lag.
                            KeepAliveStopped = (_keepAliveInterval + kKeepAliveGuardBand) <= delta;
                        }
                        else
                        {
                            // another error was reported which caused keep alive to stop.
                            KeepAliveStopped = true;
                        }

                        OnKeepAlive(lastKeepAliveError, serverState, currentTime);
                    }
                }
            }
            catch (OperationCanceledException) { }
            _logger.LogTrace("Session {Id}: KeepAlive Worker exit.", SessionId);
        }

        /// <summary>
        /// Creates a Node based on the read response.
        /// </summary>
        /// <param name="responseHeader"></param>
        /// <param name="attributes"></param>
        /// <param name="itemsToRead"></param>
        /// <param name="values"></param>
        /// <param name="diagnosticInfos"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static Node ProcessReadResponse(ResponseHeader responseHeader,
            IDictionary<uint, DataValue?> attributes, ReadValueIdCollection itemsToRead,
            DataValueCollection values, DiagnosticInfoCollection diagnosticInfos)
        {
            // process results.
            var nodeClass = 0;
            for (var index = 0; index < itemsToRead.Count; index++)
            {
                var attributeId = itemsToRead[index].AttributeId;

                // the node probably does not exist if the node class is not found.
                if (attributeId == Attributes.NodeClass)
                {
                    if (!DataValue.IsGood(values[index]))
                    {
                        throw ServiceResultException.Create(values[index].StatusCode,
                            index, diagnosticInfos, responseHeader.StringTable);
                    }

                    // check for valid node class.
                    if (values[index].Value is not int nc)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Node does not have a valid value for NodeClass: {0}.",
                            values[index].Value);
                    }
                    nodeClass = nc;
                }
                else
                {
                    if (!DataValue.IsGood(values[index]))
                    {
                        // check for unsupported attributes.
                        if (values[index].StatusCode == StatusCodes.BadAttributeIdInvalid)
                        {
                            continue;
                        }

                        // ignore errors on optional attributes
                        if (StatusCode.IsBad(values[index].StatusCode) &&
                            attributeId is
                                Attributes.AccessRestrictions or
                                Attributes.Description or
                                Attributes.RolePermissions or
                                Attributes.UserRolePermissions or
                                Attributes.UserWriteMask or
                                Attributes.WriteMask or
                                Attributes.AccessLevelEx or
                                Attributes.ArrayDimensions or
                                Attributes.DataTypeDefinition or
                                Attributes.InverseName or
                                Attributes.MinimumSamplingInterval)
                        {
                            continue;
                        }

                        // all supported attributes must be readable.
                        if (attributeId != Attributes.Value)
                        {
                            throw ServiceResultException.Create(values[index].StatusCode,
                                index, diagnosticInfos, responseHeader.StringTable);
                        }
                    }
                }
                attributes[attributeId] = values[index];
            }

            Node node;
            DataValue? value;
            switch ((NodeClass)nodeClass)
            {
                case NodeClass.Object:
                    var objectNode = new ObjectNode();
                    value = attributes[Attributes.EventNotifier];

                    if (value == null || value.Value is null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Object does not support the EventNotifier attribute.");
                    }

                    objectNode.EventNotifier = (byte)value.GetValue(typeof(byte));
                    node = objectNode;
                    break;
                case NodeClass.ObjectType:
                    var objectTypeNode = new ObjectTypeNode();
                    value = attributes[Attributes.IsAbstract];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "ObjectType does not support the IsAbstract attribute.");
                    }

                    objectTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));
                    node = objectTypeNode;
                    break;
                case NodeClass.Variable:
                    var variableNode = new VariableNode();
                    // DataType Attribute
                    value = attributes[Attributes.DataType];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Variable does not support the DataType attribute.");
                    }

                    variableNode.DataType = (NodeId)value.GetValue(typeof(NodeId));
                    // ValueRank Attribute
                    value = attributes[Attributes.ValueRank];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Variable does not support the ValueRank attribute.");
                    }

                    variableNode.ValueRank = (int)value.GetValue(typeof(int));

                    // ArrayDimensions Attribute
                    value = attributes[Attributes.ArrayDimensions];

                    if (value != null)
                    {
                        if (value.Value == null)
                        {
                            variableNode.ArrayDimensions = Array.Empty<uint>();
                        }
                        else
                        {
                            variableNode.ArrayDimensions = (uint[])value.GetValue(typeof(uint[]));
                        }
                    }

                    // AccessLevel Attribute
                    value = attributes[Attributes.AccessLevel];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Variable does not support the AccessLevel attribute.");
                    }

                    variableNode.AccessLevel = (byte)value.GetValue(typeof(byte));

                    // UserAccessLevel Attribute
                    value = attributes[Attributes.UserAccessLevel];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Variable does not support the UserAccessLevel attribute.");
                    }

                    variableNode.UserAccessLevel = (byte)value.GetValue(typeof(byte));

                    // Historizing Attribute
                    value = attributes[Attributes.Historizing];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Variable does not support the Historizing attribute.");
                    }

                    variableNode.Historizing = (bool)value.GetValue(typeof(bool));

                    // MinimumSamplingInterval Attribute
                    value = attributes[Attributes.MinimumSamplingInterval];

                    if (value != null)
                    {
                        variableNode.MinimumSamplingInterval = Convert.ToDouble(
                            attributes[Attributes.MinimumSamplingInterval]?.Value,
                            CultureInfo.InvariantCulture);
                    }

                    // AccessLevelEx Attribute
                    value = attributes[Attributes.AccessLevelEx];

                    if (value != null)
                    {
                        variableNode.AccessLevelEx = (uint)value.GetValue(typeof(uint));
                    }

                    node = variableNode;
                    break;
                case NodeClass.VariableType:
                    var variableTypeNode = new VariableTypeNode();

                    // IsAbstract Attribute
                    value = attributes[Attributes.IsAbstract];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "VariableType does not support the IsAbstract attribute.");
                    }

                    variableTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));

                    // DataType Attribute
                    value = attributes[Attributes.DataType];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "VariableType does not support the DataType attribute.");
                    }

                    variableTypeNode.DataType = (NodeId)value.GetValue(typeof(NodeId));

                    // ValueRank Attribute
                    value = attributes[Attributes.ValueRank];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "VariableType does not support the ValueRank attribute.");
                    }

                    variableTypeNode.ValueRank = (int)value.GetValue(typeof(int));

                    // ArrayDimensions Attribute
                    value = attributes[Attributes.ArrayDimensions];

                    if (value?.Value != null)
                    {
                        variableTypeNode.ArrayDimensions = (uint[])value.GetValue(typeof(uint[]));
                    }
                    node = variableTypeNode;
                    break;
                case NodeClass.Method:
                    var methodNode = new MethodNode();
                    // Executable Attribute
                    value = attributes[Attributes.Executable];
                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Method does not support the Executable attribute.");
                    }

                    methodNode.Executable = (bool)value.GetValue(typeof(bool));

                    // UserExecutable Attribute
                    value = attributes[Attributes.UserExecutable];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "Method does not support the UserExecutable attribute.");
                    }

                    methodNode.UserExecutable = (bool)value.GetValue(typeof(bool));
                    node = methodNode;
                    break;
                case NodeClass.DataType:
                    var dataTypeNode = new DataTypeNode();

                    // IsAbstract Attribute
                    value = attributes[Attributes.IsAbstract];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "DataType does not support the IsAbstract attribute.");
                    }

                    dataTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));

                    // DataTypeDefinition Attribute
                    value = attributes[Attributes.DataTypeDefinition];

                    if (value != null)
                    {
                        dataTypeNode.DataTypeDefinition = value.Value as ExtensionObject;
                    }

                    node = dataTypeNode;
                    break;
                case NodeClass.ReferenceType:
                    var referenceTypeNode = new ReferenceTypeNode();
                    // IsAbstract Attribute
                    value = attributes[Attributes.IsAbstract];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "ReferenceType does not support the IsAbstract attribute.");
                    }

                    referenceTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));

                    // Symmetric Attribute
                    value = attributes[Attributes.Symmetric];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "ReferenceType does not support the Symmetric attribute.");
                    }

                    referenceTypeNode.Symmetric = (bool)value.GetValue(typeof(bool));

                    // InverseName Attribute
                    value = attributes[Attributes.InverseName];

                    if (value?.Value != null)
                    {
                        referenceTypeNode.InverseName =
                            (LocalizedText)value.GetValue(typeof(LocalizedText));
                    }

                    node = referenceTypeNode;
                    break;
                case NodeClass.View:
                    var viewNode = new ViewNode();
                    // EventNotifier Attribute
                    value = attributes[Attributes.EventNotifier];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "View does not support the EventNotifier attribute.");
                    }

                    viewNode.EventNotifier = (byte)value.GetValue(typeof(byte));

                    // ContainsNoLoops Attribute
                    value = attributes[Attributes.ContainsNoLoops];

                    if (value == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                            "View does not support the ContainsNoLoops attribute.");
                    }

                    viewNode.ContainsNoLoops = (bool)value.GetValue(typeof(bool));
                    node = viewNode;
                    break;
                default:
                    throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                        "Node does not have a valid value for NodeClass: {0}.", nodeClass);
            }

            // NodeId Attribute
            value = attributes[Attributes.NodeId];
            if (value == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                    "Node does not support the NodeId attribute.");
            }

            node.NodeId = (NodeId)value.GetValue(typeof(NodeId));
            node.NodeClass = (NodeClass)nodeClass;

            // BrowseName Attribute
            value = attributes[Attributes.BrowseName];
            if (value == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                    "Node does not support the BrowseName attribute.");
            }

            node.BrowseName = (QualifiedName)value.GetValue(typeof(QualifiedName));

            // DisplayName Attribute
            value = attributes[Attributes.DisplayName];
            if (value == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                    "Node does not support the DisplayName attribute.");
            }
            node.DisplayName = (LocalizedText)value.GetValue(typeof(LocalizedText));

            // all optional attributes follow
            // Description Attribute
            if (attributes.TryGetValue(Attributes.Description, out value) &&
                value?.Value != null)
            {
                node.Description = (LocalizedText)value.GetValue(typeof(LocalizedText));
            }

            // WriteMask Attribute
            if (attributes.TryGetValue(Attributes.WriteMask, out value) &&
                value != null)
            {
                node.WriteMask = (uint)value.GetValue(typeof(uint));
            }

            // UserWriteMask Attribute
            if (attributes.TryGetValue(Attributes.UserWriteMask, out value) &&
                value != null)
            {
                node.UserWriteMask = (uint)value.GetValue(typeof(uint));
            }

            // RolePermissions Attribute
            if (attributes.TryGetValue(Attributes.RolePermissions, out value) &&
                value != null)
            {
                if (value.Value is ExtensionObject[] rolePermissions)
                {
                    node.RolePermissions = new RolePermissionTypeCollection();
                    foreach (var rolePermission in rolePermissions)
                    {
                        node.RolePermissions.Add(rolePermission.Body as RolePermissionType);
                    }
                }
            }

            // UserRolePermissions Attribute
            if (attributes.TryGetValue(Attributes.UserRolePermissions, out value) &&
                value != null)
            {
                if (value.Value is ExtensionObject[] userRolePermissions)
                {
                    node.UserRolePermissions = new RolePermissionTypeCollection();
                    foreach (var rolePermission in userRolePermissions)
                    {
                        node.UserRolePermissions.Add(rolePermission.Body as RolePermissionType);
                    }
                }
            }

            // AccessRestrictions Attribute
            if (attributes.TryGetValue(Attributes.AccessRestrictions, out value) &&
                value != null)
            {
                node.AccessRestrictions = (ushort)value.GetValue(typeof(ushort));
            }
            return node;
        }

        /// <summary>
        /// Create a dictionary of attributes to read for a nodeclass.
        /// </summary>
        /// <param name="nodeclass"></param>
        /// <param name="optionalAttributes"></param>
        private static SortedDictionary<uint, DataValue?> CreateAttributes(
            NodeClass nodeclass = NodeClass.Unspecified, bool optionalAttributes = true)
        {
            // Attributes to read for all types of nodes
            var attributes = new SortedDictionary<uint, DataValue?>()
            {
                { Attributes.NodeId, null },
                { Attributes.NodeClass, null },
                { Attributes.BrowseName, null },
                { Attributes.DisplayName, null }
            };
            switch (nodeclass)
            {
                case NodeClass.Object:
                    attributes.Add(Attributes.EventNotifier, null);
                    break;
                case NodeClass.Variable:
                    attributes.Add(Attributes.DataType, null);
                    attributes.Add(Attributes.ValueRank, null);
                    attributes.Add(Attributes.ArrayDimensions, null);
                    attributes.Add(Attributes.AccessLevel, null);
                    attributes.Add(Attributes.UserAccessLevel, null);
                    attributes.Add(Attributes.Historizing, null);
                    attributes.Add(Attributes.MinimumSamplingInterval, null);
                    attributes.Add(Attributes.AccessLevelEx, null);
                    break;
                case NodeClass.Method:
                    attributes.Add(Attributes.Executable, null);
                    attributes.Add(Attributes.UserExecutable, null);
                    break;
                case NodeClass.ObjectType:
                    attributes.Add(Attributes.IsAbstract, null);
                    break;
                case NodeClass.VariableType:
                    attributes.Add(Attributes.IsAbstract, null);
                    attributes.Add(Attributes.DataType, null);
                    attributes.Add(Attributes.ValueRank, null);
                    attributes.Add(Attributes.ArrayDimensions, null);
                    break;
                case NodeClass.ReferenceType:
                    attributes.Add(Attributes.IsAbstract, null);
                    attributes.Add(Attributes.Symmetric, null);
                    attributes.Add(Attributes.InverseName, null);
                    break;
                case NodeClass.DataType:
                    attributes.Add(Attributes.IsAbstract, null);
                    attributes.Add(Attributes.DataTypeDefinition, null);
                    break;
                case NodeClass.View:
                    attributes.Add(Attributes.EventNotifier, null);
                    attributes.Add(Attributes.ContainsNoLoops, null);
                    break;
                default:
                    attributes.Add(Attributes.DataType, null);
                    attributes.Add(Attributes.ValueRank, null);
                    attributes.Add(Attributes.ArrayDimensions, null);
                    attributes.Add(Attributes.AccessLevel, null);
                    attributes.Add(Attributes.UserAccessLevel, null);
                    attributes.Add(Attributes.MinimumSamplingInterval, null);
                    attributes.Add(Attributes.Historizing, null);
                    attributes.Add(Attributes.EventNotifier, null);
                    attributes.Add(Attributes.Executable, null);
                    attributes.Add(Attributes.UserExecutable, null);
                    attributes.Add(Attributes.IsAbstract, null);
                    attributes.Add(Attributes.InverseName, null);
                    attributes.Add(Attributes.Symmetric, null);
                    attributes.Add(Attributes.ContainsNoLoops, null);
                    attributes.Add(Attributes.DataTypeDefinition, null);
                    attributes.Add(Attributes.AccessLevelEx, null);
                    break;
            }
            if (optionalAttributes)
            {
                attributes.Add(Attributes.Description, null);
                attributes.Add(Attributes.WriteMask, null);
                attributes.Add(Attributes.UserWriteMask, null);
                attributes.Add(Attributes.RolePermissions, null);
                attributes.Add(Attributes.UserRolePermissions, null);
                attributes.Add(Attributes.AccessRestrictions, null);
            }
            return attributes;
        }

        /// <summary>
        /// Creates a secure channel to the specified endpoint.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        private async ValueTask<ITransportChannel> CreateChannelAsync(CancellationToken ct)
        {
            var endpoint = ConfiguredEndpoint;

            // update endpoint description using the discovery endpoint.
            if (_connection == null && (_updateFromServer || endpoint.UpdateBeforeConnect))
            {
                await endpoint.UpdateFromServerAsync(ct).ConfigureAwait(false);
                _updateFromServer = false;
            }

            // checks the domains in the certificate.
            if (_checkDomain &&
                endpoint.Description.ServerCertificate?.Length > 0)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                _configuration.CertificateValidator?.ValidateDomains(
                    new X509Certificate2(endpoint.Description.ServerCertificate),
                    endpoint);
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            if (endpoint.Description.SecurityPolicyUri != SecurityPolicies.None)
            {
                await CheckCertificatesAreLoadedAsync(ct).ConfigureAwait(false);
            }

            var clientCertificate = _instanceCertificate;
            var clientCertificateChain =
                _configuration.SecurityConfiguration.SendCertificateChain ?
                _instanceCertificateChain : null;

            // initialize the channel which will be created with the server.
            if (_connection != null)
            {
                return SessionChannel.CreateUaBinaryChannel(_configuration,
                    _connection, endpoint.Description, endpoint.Configuration,
                    clientCertificate, clientCertificateChain, MessageContext);
            }

            return SessionChannel.Create(_configuration, endpoint.Description,
                 endpoint.Configuration, clientCertificate, clientCertificateChain,
                 MessageContext);
        }

        /// <summary>
        /// Create a connection using reverse connect manager if configured.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask WaitForReverseConnectIfNeededAsync(CancellationToken ct)
        {
            if (_reverseConnectManager == null
                // || ConfiguredEndpoint.ReverseConnect?.Enabled != true
                )
            {
                return;
            }
            var endpoint = ConfiguredEndpoint;
            var updateFromEndpoint = endpoint.UpdateBeforeConnect || _updateFromServer;
            while (!IsConnected(_connection))
            {
                ct.ThrowIfCancellationRequested();
                _connection = await _reverseConnectManager.WaitForConnectionAsync(
                    endpoint.EndpointUrl, endpoint.ReverseConnect?.ServerUri,
                    ct).ConfigureAwait(false);
                if (updateFromEndpoint)
                {
                    await endpoint.UpdateFromServerAsync(endpoint.EndpointUrl,
                        _connection, endpoint.Description.SecurityMode,
                        endpoint.Description.SecurityPolicyUri, ct).ConfigureAwait(false);
                    _updateFromServer = updateFromEndpoint = false;
                }
            }

            static bool IsConnected(ITransportWaitingConnection? connection)
            {
                var socket = connection?.Handle as Bindings.IMessageSocket;
                return socket?.RemoteEndpoint != null;
            }
        }

        /// <summary>
        /// Read operation limits
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask FetchOperationLimitsAsync(CancellationToken ct)
        {
            // First we read the node read max to optimize the second read.
            var nodeIds = new[]
            {
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerRead
            };
            var (values, errors) = await ReadValuesAsync(null, nodeIds, ct).ConfigureAwait(false);
            var index = 0;
            OperationLimits.MaxNodesPerRead = Get<uint>(ref index, values, errors);

            nodeIds = new[]
            {
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryReadData,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryReadEvents,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerWrite,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerRead,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryUpdateData,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryUpdateEvents,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerMethodCall,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerBrowse,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerRegisterNodes,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerNodeManagement,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxMonitoredItemsPerCall,
        VariableIds.Server_ServerCapabilities_OperationLimits_MaxNodesPerTranslateBrowsePathsToNodeIds,
        VariableIds.Server_ServerCapabilities_MaxBrowseContinuationPoints,
        VariableIds.Server_ServerCapabilities_MaxHistoryContinuationPoints,
        VariableIds.Server_ServerCapabilities_MaxQueryContinuationPoints,
        VariableIds.Server_ServerCapabilities_MaxStringLength,
        VariableIds.Server_ServerCapabilities_MaxArrayLength,
        VariableIds.Server_ServerCapabilities_MaxByteStringLength,
        VariableIds.Server_ServerCapabilities_MinSupportedSampleRate
            };

            (values, errors) = await ReadValuesAsync(null, nodeIds, ct).ConfigureAwait(false);
            index = 0;
            OperationLimits.MaxNodesPerHistoryReadData = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerHistoryReadEvents = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerWrite = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerRead = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerHistoryUpdateData = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerHistoryUpdateEvents = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerMethodCall = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerBrowse = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerRegisterNodes = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerNodeManagement = Get<uint>(ref index, values, errors);
            OperationLimits.MaxMonitoredItemsPerCall = Get<uint>(ref index, values, errors);
            OperationLimits.MaxNodesPerTranslatePathsToNodeIds = Get<uint>(ref index, values, errors);
            OperationLimits.MaxBrowseContinuationPoints = Get<uint>(ref index, values, errors);
            OperationLimits.MaxHistoryContinuationPoints = Get<uint>(ref index, values, errors);
            OperationLimits.MaxQueryContinuationPoints = Get<uint>(ref index, values, errors);
            OperationLimits.MaxStringLength = Get<uint>(ref index, values, errors);
            OperationLimits.MaxArrayLength = Get<uint>(ref index, values, errors);
            OperationLimits.MaxByteStringLength = Get<uint>(ref index, values, errors);
            OperationLimits.MinSupportedSampleRate = Get<double>(ref index, values, errors);

            // Helper extraction
            static T Get<T>(ref int index, IReadOnlyList<DataValue> values,
                IReadOnlyList<ServiceResult> errors) where T : struct
            {
                var value = values[index];
                var error = errors.Count > 0 ? errors[index] : ServiceResult.Good;
                index++;
                if (ServiceResult.IsNotBad(error) && value.Value is T retVal)
                {
                    return retVal;
                }
                return default;
            }
        }

        /// <summary>
        /// Controls the publish workers.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task PublishControllerAsync(CancellationToken ct)
        {
            var publishWorkers = new List<PublishWorker>();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var desiredWorkerCount = GetDesiredPublishWorkerCount();
                    if (publishWorkers.Count > desiredWorkerCount)
                    {
                        // Too many workers, reduce
                        foreach (var worker in publishWorkers[desiredWorkerCount..])
                        {
                            _logger.LogInformation("Removing publish worker {Index}",
                                worker.Index);
                            await worker.DisposeAsync().ConfigureAwait(false);
                        }
                        publishWorkers = publishWorkers[..desiredWorkerCount];
                    }
                    else if (desiredWorkerCount > publishWorkers.Count)
                    {
                        // Not enough workers increase
                        publishWorkers.AddRange(Enumerable
                            .Range(publishWorkers.Count, desiredWorkerCount)
                            .Select(index => new PublishWorker(this, index)));
                    }
                    PublishWorkerCount = publishWorkers.Count;
                    var waiting = publishWorkers
                        .Select(w => w.Task)
                        .Prepend(_publishControl.WaitAsync(ct))
                        .ToArray();
                    await Task.WhenAny(waiting).ConfigureAwait(false);
                    var index = 0;
                    foreach (var item in waiting.Skip(1)) // Skip wait handle
                    {
                        if (item.IsCompleted)
                        {
                            var worker = publishWorkers[index];
                            _logger.LogInformation("Publish worker {Index} exited",
                                worker.Index);
                            await worker.DisposeAsync().ConfigureAwait(false);
                            publishWorkers.RemoveAt(index);
                            continue;
                        }
                        index++;
                    }

                    // Now lower the max publish request if we got any too
                    // many requests errors
                    if (publishWorkers.Any(w => w.TooManyPublishRequests))
                    {
                        if (MaxPublishWorkerCount > 1)
                        {
                            MaxPublishWorkerCount--;
                        }
                    }
                    PublishWorkerCount = publishWorkers.Count;
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Controller exits, clean up all workers
                foreach (var worker in publishWorkers)
                {
                    await worker.DisposeAsync().ConfigureAwait(false);
                }
            }

            int GetDesiredPublishWorkerCount()
            {
                var publishCount = SubscriptionCount;
                if (publishCount != 0)
                {
                    //
                    // Limit resulting to a number between min and max
                    // request count. If max is below min, we honor the
                    // min publish request count.
                    //
                    if (publishCount > MaxPublishWorkerCount)
                    {
                        publishCount = MaxPublishWorkerCount;
                    }
                    if (publishCount < MinPublishWorkerCount)
                    {
                        publishCount = MinPublishWorkerCount;
                    }
                    if (publishCount <= 0)
                    {
                        publishCount = 1;
                    }
                }
                return publishCount;
            }
        }

        /// <summary>
        /// Recreate subscriptions
        /// </summary>
        /// <param name="previousSessionId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RecreateSubscriptionsAsync(NodeId? previousSessionId,
            CancellationToken ct)
        {
            Debug.Assert(Connecting);
            IReadOnlyList<Subscription> subscriptions = Subscriptions.ToList();
            if (subscriptions.Count == 0)
            {
                // Nothing to do
                return;
            }
            if (TransferSubscriptionsOnRecreate && previousSessionId != null)
            {
                subscriptions = await TransferSubscriptionsAsync(subscriptions,
                    false, ct).ConfigureAwait(false);
            }
            // Force creation of the subscriptions which were not transferred.
            foreach (var subscription in subscriptions)
            {
                var force = previousSessionId != null && subscription.Created;
                await subscription.CreateAsync(force, ct).ConfigureAwait(false);
            }

            // Helper to try and transfer the subscriptions
            async Task<IReadOnlyList<Subscription>> TransferSubscriptionsAsync(
                IReadOnlyList<Subscription> subscriptions, bool sendInitialValues,
                CancellationToken ct)
            {
                var remaining = subscriptions.Where(s => !s.Created).ToList();
                subscriptions = subscriptions.Where(s => s.Created).ToList();
                if (subscriptions.Count == 0)
                {
                    return remaining;
                }
                var subscriptionIds = new UInt32Collection(subscriptions
                    .Select(s => s.Id));
                var response = await base.TransferSubscriptionsAsync(null,
                    subscriptionIds, sendInitialValues, ct).ConfigureAwait(false);

                var responseHeader = response.ResponseHeader;
                if (!StatusCode.IsGood(responseHeader.ServiceResult))
                {
                    if (responseHeader.ServiceResult == StatusCodes.BadServiceUnsupported)
                    {
                        TransferSubscriptionsOnRecreate = false;
                        _logger.LogWarning("Transfer subscription unsupported, " +
                            "TransferSubscriptionsOnReconnect set to false.");
                    }
                    else
                    {
                        _logger.LogError(
                            "Transfer subscriptions failed with error {Error}.",
                            responseHeader.ServiceResult);
                    }
                    remaining.AddRange(subscriptions);
                    return remaining;
                }

                var transferResults = response.Results;
                var diagnosticInfos = response.DiagnosticInfos;
                ClientBase.ValidateResponse(transferResults, subscriptionIds);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, subscriptionIds);
                for (var index = 0; index < subscriptions.Count; index++)
                {
                    if (transferResults[index].StatusCode == StatusCodes.BadNothingToDo)
                    {
                        _logger.LogDebug(
                            "SubscriptionId {Id} is already member of the session.",
                            subscriptionIds[index]);
                        // Done
                        continue;
                    }
                    else if (!StatusCode.IsGood(transferResults[index].StatusCode))
                    {
                        _logger.LogError(
                            "SubscriptionId {Id} failed to transfer, StatusCode={Status}",
                            subscriptionIds[index], transferResults[index].StatusCode);
                        remaining.Add(subscriptions[index]);
                        continue;
                    }
                    var transfered = await subscriptions[index].TransferAsync(
                        transferResults[index].AvailableSequenceNumbers, null,
                        ct).ConfigureAwait(false);
                    if (!transfered)
                    {
                        // Recreate the subscription instead
                        remaining.Add(subscriptions[index]);
                        continue;
                    }
                }
                return remaining;
            }
        }

        /// <summary>
        /// Validates the server certificate returned.
        /// </summary>
        /// <param name="serverCertificateData"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidateServerCertificateData(byte[] serverCertificateData)
        {
            if (serverCertificateData != null &&
                ConfiguredEndpoint.Description.ServerCertificate != null &&
                !Utils.IsEqual(serverCertificateData, ConfiguredEndpoint.Description.ServerCertificate))
            {
                try
                {
                    // verify for certificate chain in endpoint.
                    var serverCertificateChain = Utils.ParseCertificateChainBlob(
                        ConfiguredEndpoint.Description.ServerCertificate);

                    if (serverCertificateChain.Count > 0 && !Utils.IsEqual(
                        serverCertificateData, serverCertificateChain[0].RawData))
                    {
                        throw ServiceResultException.Create(StatusCodes.BadCertificateInvalid,
                            "Server did not return the certificate used to create the secure channel.");
                    }
                }
                catch (Exception)
                {
                    throw ServiceResultException.Create(StatusCodes.BadCertificateInvalid,
                        "Server did not return the certificate used to create the secure channel.");
                }
            }
        }

        /// <summary>
        /// Validates the server signature created with the client nonce.
        /// </summary>
        /// <param name="serverCertificate"></param>
        /// <param name="serverSignature"></param>
        /// <param name="clientCertificateData"></param>
        /// <param name="clientCertificateChainData"></param>
        /// <param name="clientNonce"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidateServerSignature(X509Certificate2? serverCertificate,
            SignatureData? serverSignature, byte[]? clientCertificateData,
            byte[]? clientCertificateChainData, byte[] clientNonce)
        {
            if (serverSignature == null || serverSignature.Signature == null)
            {
                _logger.LogInformation("Server signature is null or empty.");

                //throw ServiceResultException.Create(
                //    StatusCodes.BadSecurityChecksFailed,
                //    "Server signature is null or empty.");
            }

            // validate the server's signature.
            var dataToSign = Utils.Append(clientCertificateData, clientNonce);

            if (!SecurityPolicies.Verify(serverCertificate,
                ConfiguredEndpoint.Description.SecurityPolicyUri, dataToSign,
                serverSignature))
            {
                // validate the signature with complete chain if the check with
                // leaf certificate failed.
                if (clientCertificateChainData != null)
                {
                    dataToSign = Utils.Append(clientCertificateChainData, clientNonce);

                    if (!SecurityPolicies.Verify(serverCertificate,
                        ConfiguredEndpoint.Description.SecurityPolicyUri, dataToSign,
                        serverSignature))
                    {
                        throw ServiceResultException.Create(StatusCodes.BadApplicationSignatureInvalid,
                            "Server did not provide a correct signature for the nonce " +
                            "data provided by the client.");
                    }
                }
                else
                {
                    throw ServiceResultException.Create(StatusCodes.BadApplicationSignatureInvalid,
                       "Server did not provide a correct signature for the nonce data " +
                       "provided by the client.");
                }
            }
        }

        /// <summary>
        /// Validates the server endpoints returned.
        /// </summary>
        /// <param name="serverEndpoints"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidateServerEndpoints(EndpointDescriptionCollection serverEndpoints)
        {
            if (_discoveryServerEndpoints?.Count > 0)
            {
                // Compare EndpointDescriptions returned at GetEndpoints with values
                // returned at CreateSession
                EndpointDescriptionCollection expectedServerEndpoints;
                if (_discoveryProfileUris?.Count > 0)
                {
                    // Select EndpointDescriptions with a transportProfileUri that matches the
                    // profileUris specified in the original GetEndpoints() request.
                    expectedServerEndpoints = new EndpointDescriptionCollection();

                    foreach (var serverEndpoint in serverEndpoints)
                    {
                        if (_discoveryProfileUris.Contains(serverEndpoint.TransportProfileUri))
                        {
                            expectedServerEndpoints.Add(serverEndpoint);
                        }
                    }
                }
                else
                {
                    expectedServerEndpoints = serverEndpoints;
                }

                if (expectedServerEndpoints == null ||
                    _discoveryServerEndpoints.Count != expectedServerEndpoints.Count)
                {
                    throw ServiceResultException.Create(StatusCodes.BadSecurityChecksFailed,
                        "Server did not return a number of ServerEndpoints that matches the " +
                        "one from GetEndpoints.");
                }

                for (var index = 0; index < expectedServerEndpoints.Count; index++)
                {
                    var serverEndpoint = expectedServerEndpoints[index];
                    var expectedServerEndpoint = _discoveryServerEndpoints[index];

                    if (serverEndpoint.SecurityMode != expectedServerEndpoint.SecurityMode ||
                        serverEndpoint.SecurityPolicyUri != expectedServerEndpoint.SecurityPolicyUri ||
                        serverEndpoint.TransportProfileUri != expectedServerEndpoint.TransportProfileUri ||
                        serverEndpoint.SecurityLevel != expectedServerEndpoint.SecurityLevel)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadSecurityChecksFailed,
                            "The list of ServerEndpoints returned at CreateSession does not match " +
                            "the list from GetEndpoints.");
                    }

                    if (serverEndpoint.UserIdentityTokens.Count !=
                        expectedServerEndpoint.UserIdentityTokens.Count)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadSecurityChecksFailed,
                            "The list of ServerEndpoints returned at CreateSession does not match " +
                            "the one from GetEndpoints.");
                    }

                    for (var i = 0; i < serverEndpoint.UserIdentityTokens.Count; i++)
                    {
                        if (!serverEndpoint.UserIdentityTokens[i].IsEqual(
                            expectedServerEndpoint.UserIdentityTokens[i]))
                        {
                            throw ServiceResultException.Create(StatusCodes.BadSecurityChecksFailed,
                            "The list of ServerEndpoints returned at CreateSession does not match" +
                            " the one from GetEndpoints.");
                        }
                    }
                }
            }

            // find the matching description (TBD - check domains against certificate).
            var found = false;

            var foundDescription = Find(serverEndpoints,
                ConfiguredEndpoint.Description, true);
            if (foundDescription != null)
            {
                found = true;
                // ensure endpoint has up to date information.
                Update(ConfiguredEndpoint.Description, foundDescription);
            }
            else
            {
                foundDescription = Find(serverEndpoints,
                    ConfiguredEndpoint.Description, false);
                if (foundDescription != null)
                {
                    found = true;
                    // ensure endpoint has up to date information.
                    Update(ConfiguredEndpoint.Description, foundDescription);
                }
            }

            // could be a security risk.
            if (!found)
            {
                throw ServiceResultException.Create(StatusCodes.BadSecurityChecksFailed,
                    "Server did not return an EndpointDescription that matched the one " +
                    "used to create the secure channel.");
            }

            EndpointDescription? Find(EndpointDescriptionCollection endpointDescriptions,
                EndpointDescription match, bool matchPort)
            {
                var expectedUrl = Utils.ParseUri(match.EndpointUrl);
                for (var index = 0; index < endpointDescriptions.Count; index++)
                {
                    var serverEndpoint = endpointDescriptions[index];
                    var actualUrl = Utils.ParseUri(serverEndpoint.EndpointUrl);

                    if (actualUrl != null &&
                        actualUrl.Scheme == expectedUrl.Scheme &&
                        (!matchPort || actualUrl.Port == expectedUrl.Port) &&
                        serverEndpoint.SecurityPolicyUri ==
                            ConfiguredEndpoint.Description.SecurityPolicyUri &&
                        serverEndpoint.SecurityMode == ConfiguredEndpoint.Description.SecurityMode)
                    {
                        return serverEndpoint;
                    }
                }
                return null;
            }

            static void Update(EndpointDescription target, EndpointDescription source)
            {
                target.Server.ApplicationName = source.Server.ApplicationName;
                target.Server.ApplicationUri = source.Server.ApplicationUri;
                target.Server.ApplicationType = source.Server.ApplicationType;
                target.Server.ProductUri = source.Server.ProductUri;
                target.TransportProfileUri = source.TransportProfileUri;
                target.UserIdentityTokens = source.UserIdentityTokens;
            }
        }

        /// <summary>
        /// Handles the validation of server software certificates and application callback.
        /// </summary>
        /// <param name="serverSoftwareCertificates"></param>
        private void HandleSignedSoftwareCertificates(
            SignedSoftwareCertificateCollection serverSoftwareCertificates)
        {
            // get a validator to check certificates provided by server.
            var validator = _configuration.CertificateValidator;

            // validate software certificates.
            var softwareCertificates = new List<SoftwareCertificate>();

            foreach (var signedCertificate in serverSoftwareCertificates)
            {
                var result = SoftwareCertificate.Validate(validator,
                    signedCertificate.CertificateData, out var softwareCertificate);
                if (ServiceResult.IsBad(result))
                {
                    OnSoftwareCertificateError(signedCertificate, result);
                }

                softwareCertificates.Add(softwareCertificate);
            }

            // check if software certificates meet application requirements.
            ValidateSoftwareCertificates(softwareCertificates);
        }

        /// <summary>
        /// Validates the server nonce and security parameters of user identity.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="serverNonce"></param>
        /// <param name="securityPolicyUri"></param>
        /// <param name="previousServerNonce"></param>
        /// <param name="channelSecurityMode"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidateServerNonce(IUserIdentity identity, byte[] serverNonce,
            string securityPolicyUri, byte[] previousServerNonce,
            MessageSecurityMode channelSecurityMode = MessageSecurityMode.None)
        {
            // skip validation if server nonce is not used for encryption.
            if (string.IsNullOrEmpty(securityPolicyUri) ||
                securityPolicyUri == SecurityPolicies.None)
            {
                return;
            }

            if (identity == null || identity.TokenType == UserTokenType.Anonymous)
            {
                return;
            }

            // the server nonce should be validated if the token includes a secret.
            if (!Utils.Nonce.ValidateNonce(serverNonce,
                MessageSecurityMode.SignAndEncrypt,
                (uint)_configuration.SecurityConfiguration.NonceLength))
            {
                if (channelSecurityMode == MessageSecurityMode.SignAndEncrypt ||
                    _configuration.SecurityConfiguration.SuppressNonceValidationErrors)
                {
                    _logger.LogWarning(
                        "The server nonce has not the correct length or is not random " +
                        "enough. The error is suppressed by user setting or because " +
                        "the channel is encrypted.");
                }
                else
                {
                    throw ServiceResultException.Create(StatusCodes.BadNonceInvalid,
                        "The server nonce has not the correct length or is not " +
                        "random enough.");
                }
            }

            // check that new nonce is different from the previously returned
            // server nonce.
            if (previousServerNonce != null &&
                Utils.CompareNonce(serverNonce, previousServerNonce))
            {
                if (channelSecurityMode == MessageSecurityMode.SignAndEncrypt ||
                    _configuration.SecurityConfiguration.SuppressNonceValidationErrors)
                {
                    _logger.LogWarning(
                        "The Server nonce is equal with previously returned nonce. " +
                        "The error is suppressed by user setting or because the " +
                        "channel is encrypted.");
                }
                else
                {
                    throw ServiceResultException.Create(StatusCodes.BadNonceInvalid,
                        "Server nonce is equal with previously returned nonce.");
                }
            }
        }

        /// <summary>
        /// Ensure the certificate and certificate chain are loaded if needed.
        /// </summary>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        private async Task CheckCertificatesAreLoadedAsync(CancellationToken ct)
        {
            if (ConfiguredEndpoint.Description.SecurityPolicyUri != SecurityPolicies.None)
            {
                // update client certificate.
                if (_instanceCertificate?.HasPrivateKey != true ||
                    _instanceCertificate.NotAfter < DateTime.UtcNow)
                {
                    // load the application instance certificate.
                    var cert = _configuration.SecurityConfiguration.ApplicationCertificate;
                    if (cert == null)
                    {
                        throw new ServiceResultException(StatusCodes.BadConfigurationError,
                            "The client configuration does not specify an application " +
                            "instance certificate.");
                    }

                    ct.ThrowIfCancellationRequested();
                    _instanceCertificate = await cert.Find(true).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();

                    // check for valid certificate.
                    if (_instanceCertificate == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                            "Cannot find the application instance certificate. " +
                            "Store={0}, SubjectName={1}, Thumbprint={2}.",
                            cert.StorePath, cert.SubjectName, cert.Thumbprint);
                    }
                    // check for private key.
                    if (!_instanceCertificate.HasPrivateKey)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                            "No private key for the application instance certificate. " +
                            "Subject={0}, Thumbprint={1}.",
                            _instanceCertificate.Subject, _instanceCertificate.Thumbprint);
                    }
                    if (_instanceCertificate.NotAfter < DateTime.UtcNow)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                             "Application instance certificate has expired. " +
                             "Store={0}, SubjectName={1}, Thumbprint={2}.",
                             cert.StorePath, cert.SubjectName, cert.Thumbprint);
                    }
                    _instanceCertificateChain = null;
                }

                if (_instanceCertificateChain == null)
                {
                    // load certificate chain.
                    _instanceCertificateChain = new X509Certificate2Collection(_instanceCertificate);
                    var issuers = new List<CertificateIdentifier>();
                    await _configuration.CertificateValidator.GetIssuers(_instanceCertificate,
                        issuers).ConfigureAwait(false);

                    for (var i = 0; i < issuers.Count; i++)
                    {
                        _instanceCertificateChain.Add(issuers[i].Certificate);
                    }
                }
            }
        }

        /// <summary>
        /// Worker object that manages the publish worker tasks inside
        /// the controller.
        /// </summary>
        private sealed class PublishWorker : IAsyncDisposable
        {
            /// <summary>
            /// Worker id
            /// </summary>
            public int Index { get; }

            /// <summary>
            /// Task to wait until worker exits
            /// </summary>
            public Task Task { get; }

            /// <summary>
            /// Signal too many publish requests running
            /// </summary>
            public bool TooManyPublishRequests { get; private set; }

            /// <inheritdoc/>
            public PublishWorker(Session session, int index)
            {
                Index = index;
                _cts = new CancellationTokenSource();
                _session = session;
                _logger = _session.LoggerFactory.CreateLogger<PublishWorker>();
                Task = PublishWorkerAsync(_cts.Token);
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    await _cts.CancelAsync().ConfigureAwait(false);
                    if (!Task.IsCompleted)
                    {
                        try
                        {
                            await Task.ConfigureAwait(false);
                        }
                        catch { } // Ignore
                    }
                }
                finally
                {
                    _cts.Dispose();
                }
            }

            /// <summary>
            /// Represents a continously running publish forwarder that forwards
            /// publish responses to the subscriptions contained in this session.
            /// The publish worker tasks have a controller that reduces or
            /// increases the number of workers as new subscriptions are added
            /// or subscriptions are removed. Once the message has been delivered
            /// to the subscription, the subscription will queue acknowledges
            /// to the worker which it will send.
            /// </summary>
            /// <param name="ct"></param>
            private async Task PublishWorkerAsync(CancellationToken ct)
            {
                var timeoutHint = 0u;
                var moreNotifications = true; // Dont wait first time we enter the loop.
                _logger.LogInformation("PUBLISH #{Handle} - publish worker STARTED.", Index);
                while (!ct.IsCancellationRequested)
                {
                    if (!_session._connected.IsSet)
                    {
                        _logger.LogInformation(
                            "PUBLISH #{Handle} - publish worker PAUSED.", Index);
                        await _session._connected.WaitAsync(ct).ConfigureAwait(false);
                        _logger.LogInformation(
                            "PUBLISH #{Handle} - publish worker RESUMED.", Index);
                    }
                    var minPublishInterval = RevisePublishTimeout(ref timeoutHint);
                    var acks = GetAcksReadyToSend();
                    if (acks.Count == 0 && !moreNotifications && minPublishInterval != 0)
                    {
                        // Throttle publishing as we wait for acks to arrive
                        acks = await WaitForAcksAsync(minPublishInterval, ct).ConfigureAwait(false);
                    }
                    var publishCounter = Utils.IncrementIdentifier(ref _session._publishCounter);
                    try
                    {
                        var response = await _session.PublishAsync(new RequestHeader
                        {
                            TimeoutHint = timeoutHint,
                            ReturnDiagnostics = (uint)(int)_session.ReturnDiagnostics,
                            RequestHandle = publishCounter
                        }, acks, ct).ConfigureAwait(false);

                        moreNotifications = response.MoreNotifications;
                        var subscriptionId = response.SubscriptionId;
                        var notificationMessage = response.NotificationMessage;
                        var availableSequenceNumbers = response.AvailableSequenceNumbers;

                        var acknowledgeResults = response.Results;
                        var acknowledgeDiagnosticInfos = response.DiagnosticInfos;
                        ClientBase.ValidateResponse(acknowledgeResults, acks);
                        ClientBase.ValidateDiagnosticInfos(acknowledgeDiagnosticInfos, acks);
                        TooManyPublishRequests = false;

                        // Get the subscription with the provided identifier
                        var subscription = _session.GetSubscription(subscriptionId);
                        if (subscription != null)
                        {
                            // deliver to subscription
                            await subscription.OnPublishReceivedAsync(availableSequenceNumbers,
                                notificationMessage, response.ResponseHeader.StringTable).ConfigureAwait(false);
                            Interlocked.Increment(ref _session._goodPublishRequestCount);
                        }
                        else
                        {
                            // ignore messages with a subscription that has been deleted.
                            // Do not delete publish requests of stale subscriptions
                            _logger.LogInformation(
                                "PUBLISH #{Handle}-{Id} - Received Publish Response " +
                                "for Unknown SubscriptionId={SubscriptionId}. Ignored.",
                                Index, publishCounter, subscriptionId);
                            Interlocked.Increment(ref _session._badPublishRequestCount);
                            moreNotifications = true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        // raise an error event.
                        var error = new ServiceResult(e);
                        Interlocked.Increment(ref _session._badPublishRequestCount);
                        // Rollback acks we collected
                        acks.ForEach(ack => _session._acks.Writer.TryWrite(ack));

                        // ignore errors if reconnecting.
                        if (_session.Connecting)
                        {
                            _logger.LogWarning(
                                "PUBLISH #{Handle}-{Id} - Publish abandoned after error due to reconnect: {Message}",
                                Index, publishCounter, e.Message);
                            continue;
                        }

                        // don't send another publish for these errors,
                        // or throttle to avoid server overload.
                        switch (error.Code)
                        {
                            case StatusCodes.BadTooManyPublishRequests:
                                TooManyPublishRequests = true;
                                break;
                            case StatusCodes.BadNoSubscription:
                            case StatusCodes.BadSessionClosed:
                            case StatusCodes.BadSecurityChecksFailed:
                            case StatusCodes.BadCertificateInvalid:
                            case StatusCodes.BadServerHalted:
                                break;
                            // may require a reconnect or activate to recover
                            case StatusCodes.BadSessionIdInvalid:
                            case StatusCodes.BadSecureChannelIdInvalid:
                            case StatusCodes.BadSecureChannelClosed:
                                // TODO
                                // OnKeepAliveError(error);
                                break;
                            // Servers may return this error when overloaded
                            case StatusCodes.BadTooManyOperations:
                            case StatusCodes.BadTcpServerTooBusy:
                            case StatusCodes.BadServerTooBusy:
                                // throttle the next publish to reduce server load
                                _logger.LogDebug(
                                    "PUBLISH #{Handle}-{Id} - Server busy, throttling worker.",
                                    Index, publishCounter);
                                moreNotifications = false; // throttle
                                break;
                            case StatusCodes.BadTimeout:
                            case StatusCodes.BadRequestTimeout:
                                // Timed out - retry with larger timeout
                                timeoutHint += 1000; // Increase by seconds
                                _logger.LogDebug(
                                    "PUBLISH #{Handle}-{Id} - Timed out, increasing timeout to {Timeout}.",
                                    Index, publishCounter, timeoutHint);
                                moreNotifications = true;
                                break;
                            default:
                                _logger.LogError(e,
                                    "PUBLISH #{Handle}-{Id} - Unhandled error {Status} during Publish.",
                                    Index, publishCounter, error.StatusCode);
                                break;
                        }
                    }
                }
                _logger.LogInformation("PUBLISH #{Handle} - publish worker STOPPED.", Index);
            }

            /// <summary>
            /// Wait until acks arrive and return them
            /// </summary>
            /// <param name="maxWaitTime"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task<SubscriptionAcknowledgementCollection> WaitForAcksAsync(
                int maxWaitTime, CancellationToken ct)
            {
                Debug.Assert(maxWaitTime != 0, "Checked before entering");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var sw = Stopwatch.StartNew();
                if (maxWaitTime != Timeout.Infinite)
                {
                    var workers = _session.PublishWorkerCount;
                    if (workers == 0)
                    {
                        Debug.Fail("Must have at least this worker here.");
                        workers = 1;
                    }
                    maxWaitTime /= workers;
                    _logger.LogInformation(
                        "PUBLISH #{Handle} - Waiting max {Time}ms for acks to arrive.",
                        Index, maxWaitTime);
                    cts.CancelAfter(maxWaitTime);
                }
                else
                {
                    _logger.LogDebug("PUBLISH #{Handle} - Waiting for acks to arrive.",
                        Index);
                }
                try
                {
                    var firstAck = await _session._acks.Reader.ReadAsync(
                        cts.Token).ConfigureAwait(false);
                    var acks = GetAcksReadyToSend();
                    acks.Insert(0, firstAck);
                    _logger.LogInformation(
                        "PUBLISH #{Handle} - Publish {Count} acks after pausing {Duration}.",
                        Index, acks.Count, sw.Elapsed);
                    return acks;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "PUBLISH #{Handle} - Publish with no acks after waiting {Duration}.",
                        Index, sw.Elapsed);
                    return new SubscriptionAcknowledgementCollection();
                }
            }

            /// <summary>
            /// Get acks that are ready to send
            /// </summary>
            /// <returns></returns>
            private SubscriptionAcknowledgementCollection GetAcksReadyToSend()
            {
                var acks = new SubscriptionAcknowledgementCollection();

                // TODO: Is this something that we can get from ops limit?
                var available = _session._acks.Reader.Count;
                var maxAcks = available / Math.Max(_session.PublishWorkerCount, 1);
                for (var i = 0; i < maxAcks
                    && _session._acks.Reader.TryRead(out var ack); i++)
                {
                    acks.Add(ack);
                }
                if (acks.Count != 0)
                {
                    _logger.LogDebug(
                        "PUBLISH #{Handle} - Acknoledging {Count} of {Total} messages.",
                        Index, acks.Count, available);
                }
                return acks;
            }

            /// <summary>
            /// Calculate the publish timeout to use
            /// </summary>
            /// <param name="currentTimeout"></param>
            /// <returns>Max time to throttle the publish</returns>
            private int RevisePublishTimeout(ref uint currentTimeout)
            {
                lock (_session.SyncRoot)
                {
                    if (_session._subscriptions.Count == 0)
                    {
                        return 0;
                    }

                    var timeout = TimeSpan.Zero;
                    var minPublishInterval = Timeout.Infinite;
                    foreach (var s in _session._subscriptions)
                    {
                        var publishingInterval = s.CurrentPublishingInterval;
                        var keepAlive = publishingInterval * s.CurrentKeepAliveCount;
                        if (timeout < keepAlive)
                        {
                            timeout = keepAlive;
                        }

                        var pi = (int)publishingInterval.TotalMilliseconds;
                        if (pi <= 0)
                        {
                            continue;
                        }
                        if (minPublishInterval > pi ||
                            minPublishInterval == Timeout.Infinite)
                        {
                            minPublishInterval = pi;
                        }
                    }
                    //
                    // The timeout while publishing should be twice the
                    // value for PublishingInterval * KeepAliveCount
                    // TODO: Validate this against spec
                    //
                    timeout *= 2;
                    if (timeout < _session.OperationTimeout)
                    {
                        timeout = _session.OperationTimeout;
                    }
                    if (timeout > kMaxOperationTimeout)
                    {
                        timeout = kMaxOperationTimeout;
                    }
                    var newTimeout = (uint)timeout.TotalMilliseconds;
                    if (newTimeout > currentTimeout)
                    {
                        currentTimeout = newTimeout;
                    }
                    return minPublishInterval;
                }
            }

            private readonly ILogger _logger;
            private readonly Session _session;
            private readonly CancellationTokenSource _cts;
        }

        private static readonly TimeSpan kDefaultOperationTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan kDefaultKeepAliveInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan kMaxOperationTimeout = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan kKeepAliveGuardBand = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan kReconnectTimeout = TimeSpan.FromSeconds(15);
        private X509Certificate2? _instanceCertificate;
        private X509Certificate2Collection? _instanceCertificateChain;
        private byte[] _serverNonce = Array.Empty<byte>();
        private byte[] _previousServerNonce = Array.Empty<byte>();
        private X509Certificate2? _serverCertificate;
        private uint _maxRequestMessageSize;
        private long _publishCounter;
        private long _keepAliveCounter;
        private TimeSpan _keepAliveInterval = kDefaultKeepAliveInterval;
#pragma warning disable IDE0032 // Use auto property
        private int _goodPublishRequestCount;
        private int _badPublishRequestCount;
#pragma warning restore IDE0032 // Use auto property
        private bool _updateFromServer;
        private ITransportWaitingConnection? _connection;
        private readonly Channel<SubscriptionAcknowledgement> _acks;
        private readonly Nito.AsyncEx.AsyncManualResetEvent _connected = new();
        private readonly Nito.AsyncEx.AsyncAutoResetEvent _publishControl = new();
        private readonly Task _publishController;
        private readonly Nito.AsyncEx.AsyncAutoResetEvent _keepAliveTrigger = new();
        private readonly Timer _keepAliveTimer;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _keepAliveWorker;
        private readonly ReverseConnectManager? _reverseConnectManager;
        private readonly List<Subscription> _subscriptions = new();
        private readonly IReadOnlyList<string> _preferredLocales;
        private readonly ApplicationConfiguration _configuration;
        private readonly bool _checkDomain;
        private readonly SystemContext _systemContext;
        private readonly SemaphoreSlim _connecting = new(1, 1);
        private readonly EndpointDescriptionCollection? _discoveryServerEndpoints;
        private readonly StringCollection? _discoveryProfileUris;
        private readonly NodeCache _nodeCache;
        private readonly ILogger _logger;
    }
}
