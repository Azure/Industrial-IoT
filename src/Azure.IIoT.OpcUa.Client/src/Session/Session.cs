/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

#define PERIODIC_TIMER
#nullable disable

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Bindings;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages a session with a server.
    /// </summary>
    public class Session : SessionBase, ISession, ISessionInternal,
        IComplexTypeContext, INodeCacheContext
    {
        /// <summary>
        /// Raised when a keep alive arrives from the server or an error is detected.
        /// </summary>
        /// <remarks>
        /// Once a session is created a timer will periodically read the server state and current time.
        /// If this read operation succeeds this event will be raised each time the keep alive period elapses.
        /// If an error is detected (KeepAliveStopped == true) then this event will be raised as well.
        /// </remarks>
        public event KeepAliveEventHandler KeepAlive
        {
            add => _KeepAlive += value;
            remove => _KeepAlive -= value;
        }

        /// <summary>
        /// Raised when an exception occurs while processing a publish response.
        /// </summary>
        /// <remarks>
        /// Exceptions in a publish response are not necessarily fatal and the Session will
        /// attempt to recover by issuing Republish requests if missing messages are detected.
        /// That said, timeout errors may be a symptom of a OperationTimeout that is too short
        /// when compared to the shortest PublishingInterval/KeepAliveCount amount the current
        /// Subscriptions. The OperationTimeout should be twice the minimum value for
        /// PublishingInterval*KeepAliveCount.
        /// </remarks>
        public event PublishErrorEventHandler PublishError
        {
            add => _PublishError += value;
            remove => _PublishError -= value;
        }

        /// <inheritdoc/>
        public event RenewUserIdentityEventHandler RenewUserIdentity
        {
            add => _RenewUserIdentity += value;
            remove => _RenewUserIdentity -= value;
        }

        /// <inheritdoc/>
        public event PublishSequenceNumbersToAcknowledgeEventHandler PublishSequenceNumbersToAcknowledge
        {
            add => _PublishSequenceNumbersToAcknowledge += value;
            remove => _PublishSequenceNumbersToAcknowledge -= value;
        }

        /// <inheritdoc/>
        public event EventHandler SessionConfigurationChanged
        {
            add => _SessionConfigurationChanged += value;
            remove => _SessionConfigurationChanged -= value;
        }

        /// <summary>
        /// A session factory that was used to create the session.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }

        /// <inheritdoc/>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the endpoint used to connect to the server.
        /// </summary>
        public ConfiguredEndpoint ConfiguredEndpoint => _endpoint;

        /// <summary>
        /// Gets the name assigned to the session.
        /// </summary>
        public string SessionName => _sessionName;

        /// <summary>
        /// Gets the period for wich the server will maintain the session if there is no communication from the client.
        /// </summary>
        public double SessionTimeout => _sessionTimeout;

        /// <summary>
        /// Gets the local handle assigned to the session.
        /// </summary>
        public object Handle { get; set; }

        /// <summary>
        /// Gets the user identity currently used for the session.
        /// </summary>
        public IUserIdentity Identity => _identity;

        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        public NamespaceTable NamespaceUris { get; private set; }

        /// <summary>
        /// Gets the system context for use with the session.
        /// </summary>
        public ISystemContext SystemContext => _systemContext;

        /// <summary>
        /// Gets the factory used to create encodeable objects that the server understands.
        /// </summary>
        public IEncodeableFactory Factory { get; private set; }

        /// <summary>
        /// Gets the cache of nodes fetched from the server.
        /// </summary>
        public INodeCache NodeCache => _nodeCache;

        /// <summary>
        /// Gets the locales that the server should use when returning localized text.
        /// </summary>
        public StringCollection PreferredLocales => _preferredLocales;

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
        /// Default <c>true</c>, set to <c>false</c> if subscriptions need to
        /// be transferred or for durable subscriptions.
        /// </remarks>
        public bool DeleteSubscriptionsOnClose { get; set; }

        /// <summary>
        /// If the subscriptions are transferred when a session is reconnected.
        /// </summary>
        /// <remarks>
        /// Default <c>false</c>, set to <c>true</c> if subscriptions should
        /// be transferred after reconnect. Service must be supported by server.
        /// </remarks>
        public bool TransferSubscriptionsOnReconnect { get; set; }

        /// <summary>
        /// Gets or Sets how frequently the server is pinged to see if communication is still working.
        /// </summary>
        /// <remarks>
        /// This interval controls how much time elaspes before a communication error is detected.
        /// If everything is ok the KeepAlive event will be raised each time this period elapses.
        /// </remarks>
        public int KeepAliveInterval
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
        /// Set to true if the server does not respond for 2 times the KeepAliveInterval
        /// or if another error was reported.
        /// Set to false is communication is ok or recovered.
        /// </remarks>
        public bool KeepAliveStopped
        {
            get
            {
                var lastKeepAliveErrorStatusCode = _lastKeepAliveErrorStatusCode;
                if (StatusCode.IsGood(lastKeepAliveErrorStatusCode) || lastKeepAliveErrorStatusCode == StatusCodes.BadNoCommunication)
                {
                    var delta = HiResClock.TickCount - LastKeepAliveTickCount;

                    // add a guard band to allow for network lag.
                    return (_keepAliveInterval + kKeepAliveGuardBand) <= delta;
                }

                // another error was reported which caused keep alive to stop.
                return true;
            }
        }

        /// <summary>
        /// Gets the TickCount in ms of the last keep alive based on <see cref="HiResClock.TickCount"/>.
        /// Independent of system time changes.
        /// </summary>
        public int LastKeepAliveTickCount { get; private set; }

        /// <summary>
        /// Gets the number of outstanding publish or keep alive requests.
        /// </summary>
        public int OutstandingRequestCount
        {
            get
            {
                lock (_outstandingRequests)
                {
                    return _outstandingRequests.Count;
                }
            }
        }

        /// <summary>
        /// Gets the number of outstanding publish or keep alive requests which appear to be missing.
        /// </summary>
        public int DefunctRequestCount
        {
            get
            {
                lock (_outstandingRequests)
                {
                    var count = 0;

                    for (var ii = _outstandingRequests.First; ii != null; ii = ii.Next)
                    {
                        if (ii.Value.Defunct)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }
        }

        /// <summary>
        /// Gets the number of good outstanding publish requests.
        /// </summary>
        public int GoodPublishRequestCount
        {
            get
            {
                lock (_outstandingRequests)
                {
                    var count = 0;

                    for (var ii = _outstandingRequests.First; ii != null; ii = ii.Next)
                    {
                        if (!ii.Value.Defunct && ii.Value.RequestTypeId == DataTypes.PublishRequest)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }
        }

        /// <summary>
        /// Gets and sets the minimum number of publish requests to be used in the session.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int MinPublishRequestCount
        {
            get => _minPublishRequestCount;
            set
            {
                lock (SyncRoot)
                {
                    if (value >= kDefaultPublishRequestCount && value <= kMinPublishRequestCountMax)
                    {
                        _minPublishRequestCount = value;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(MinPublishRequestCount),
                            $"Minimum publish request count must be between {kDefaultPublishRequestCount} and {kMinPublishRequestCountMax}.");
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the maximum number of publish requests to be used in the session.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int MaxPublishRequestCount
        {
            get => Math.Max(_minPublishRequestCount, _maxPublishRequestCount);
            set
            {
                lock (SyncRoot)
                {
                    if (value >= kDefaultPublishRequestCount && value <= kMaxPublishRequestCountMax)
                    {
                        _maxPublishRequestCount = value;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(MaxPublishRequestCount),
                            $"Maximum publish request count must be between {kDefaultPublishRequestCount} and {kMaxPublishRequestCountMax}.");
                    }
                }
            }
        }

        /// <summary>
        /// Read from the Server capability MaxContinuationPointsPerBrowse when the Operation Limits are fetched
        /// </summary>
        public uint ServerMaxContinuationPointsPerBrowse { get; set; }

        /// <inheritdoc/>
        public ContinuationPointPolicy ContinuationPointPolicy { get; set; } = ContinuationPointPolicy.Default;

        /// <summary>
        /// Constructs a new instance of the <see cref="ISession"/> class.
        /// </summary>
        /// <remarks>
        /// The application configuration is used to look up the certificate
        /// if none is provided.
        /// The clientCertificate must have the private key. This will
        /// require that the certificate be loaded from a certicate store.
        /// Converting a DER encoded blob to a X509Certificate2 will not
        /// include a private key. The <i>availableEndpoints</i> and
        /// <i>discoveryProfileUris</i> parameters are used to validate
        /// that the list of EndpointDescriptions returned at GetEndpoints
        /// matches the list returned at CreateSession.
        /// </remarks>
        /// <param name="channel">The channel used to communicate with the
        /// server.</param>
        /// <param name="configuration">The configuration for the client
        /// application.</param>
        /// <param name="endpoint">The endpoint used to initialize the
        /// channel.</param>
        /// <param name="clientCertificate">The certificate to use for the
        /// client.</param>
        /// <param name="loggerFactory">A logger factory to use</param>
        /// <param name="availableEndpoints">The list of available endpoints
        /// returned by server in GetEndpoints() response.</param>
        /// <param name="discoveryProfileUris">The value of profileUris used
        /// in GetEndpoints() request.</param>
        public Session(ITransportChannel channel, ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint, X509Certificate2 clientCertificate,
            ILoggerFactory loggerFactory,
            EndpointDescriptionCollection availableEndpoints = null,
            StringCollection discoveryProfileUris = null)
            :
                base(channel)
        {
            LoggerFactory = loggerFactory;
            _logger = LoggerFactory.CreateLogger<Session>();
            Initialize(channel, configuration, endpoint, clientCertificate);
            _discoveryServerEndpoints = availableEndpoints;
            _discoveryProfileUris = discoveryProfileUris;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ISession"/> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="template">The template session.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c> the event
        /// handlers are copied.</param>
        public Session(ITransportChannel channel, Session template,
            bool copyEventHandlers)
            : base(channel)
        {
            LoggerFactory = template.LoggerFactory;
            _logger = LoggerFactory.CreateLogger<Session>();
            Initialize(channel, template._configuration, template.ConfiguredEndpoint, template._instanceCertificate);

            SessionFactory = template.SessionFactory;
            DeleteSubscriptionsOnClose = template.DeleteSubscriptionsOnClose;
            TransferSubscriptionsOnReconnect = template.TransferSubscriptionsOnReconnect;
            _sessionTimeout = template._sessionTimeout;
            _maxRequestMessageSize = template._maxRequestMessageSize;
            _minPublishRequestCount = template._minPublishRequestCount;
            _maxPublishRequestCount = template._maxPublishRequestCount;
            _preferredLocales = template.PreferredLocales;
            _sessionName = template.SessionName;
            Handle = template.Handle;
            _identity = template.Identity;
            _keepAliveInterval = template.KeepAliveInterval;
            _checkDomain = template._checkDomain;
            ContinuationPointPolicy = template.ContinuationPointPolicy;
            if (template.OperationTimeout > 0)
            {
                OperationTimeout = template.OperationTimeout;
            }

            if (copyEventHandlers)
            {
                _KeepAlive = template._KeepAlive;
                _Publish = template._Publish;
                _PublishError = template._PublishError;
                _PublishSequenceNumbersToAcknowledge = template._PublishSequenceNumbersToAcknowledge;
                _SubscriptionsChanged = template._SubscriptionsChanged;
                _SessionClosing = template._SessionClosing;
                _SessionConfigurationChanged = template._SessionConfigurationChanged;
            }

            foreach (var subscription in template.Subscriptions)
            {
                AddSubscription(subscription.CloneSubscription(copyEventHandlers));
            }
        }

        /// <summary>
        /// Initializes the channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="configuration"></param>
        /// <param name="endpoint"></param>
        /// <param name="clientCertificate"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void Initialize(
            ITransportChannel channel,
            ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint,
            X509Certificate2 clientCertificate)
        {
            SessionFactory = new DefaultSessionFactory(LoggerFactory);
            _sessionTimeout = 0;
            NamespaceUris = new NamespaceTable();
            _serverUris = new StringTable();
            Factory = EncodeableFactory.GlobalFactory;
            _configuration = null;
            _instanceCertificate = null;
            _endpoint = null;
            _subscriptions = new List<Subscription>();
            _acknowledgementsToSend = new SubscriptionAcknowledgementCollection();
            _acknowledgementsToSendLock = new object();
#if DEBUG_SEQUENTIALPUBLISHING
            _latestAcknowledgementsSent = new Dictionary<uint, uint>();
#endif
            _outstandingRequests = new LinkedList<AsyncRequestState>();
            _keepAliveInterval = 5000;
            _tooManyPublishRequests = 0;
            _minPublishRequestCount = kDefaultPublishRequestCount;
            _maxPublishRequestCount = kMaxPublishRequestCountMax;
            _sessionName = "";
            DeleteSubscriptionsOnClose = true;
            TransferSubscriptionsOnReconnect = false;
            _reconnecting = false;
            _reconnectLock = new SemaphoreSlim(1, 1);
            ServerMaxContinuationPointsPerBrowse = 0;

            ValidateClientConfiguration(configuration);

            // save configuration information.
            _configuration = configuration;
            _endpoint = endpoint;

            if (_endpoint.Description.SecurityPolicyUri != SecurityPolicies.None)
            {
                // update client certificate.
                _instanceCertificate = clientCertificate;

                if (clientCertificate == null)
                {
                    // load the application instance certificate.
                    if (_configuration.SecurityConfiguration.ApplicationCertificate == null)
                    {
                        throw new ServiceResultException(
                            StatusCodes.BadConfigurationError,
                            "The client configuration does not specify an application instance certificate.");
                    }

                    _instanceCertificate = _configuration.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
                }

                // check for valid certificate.
                if (_instanceCertificate == null)
                {
                    var cert = _configuration.SecurityConfiguration.ApplicationCertificate;
                    throw ServiceResultException.Create(
                        StatusCodes.BadConfigurationError,
                        "Cannot find the application instance certificate. Store={0}, SubjectName={1}, Thumbprint={2}.",
                        cert.StorePath, cert.SubjectName, cert.Thumbprint);
                }

                // check for private key.
                if (!_instanceCertificate.HasPrivateKey)
                {
                    throw ServiceResultException.Create(
                        StatusCodes.BadConfigurationError,
                        "No private key for the application instance certificate. Subject={0}, Thumbprint={1}.",
                        _instanceCertificate.Subject,
                        _instanceCertificate.Thumbprint);
                }

                // load certificate chain.
                _instanceCertificateChain = new X509Certificate2Collection(_instanceCertificate);
                var issuers = new List<CertificateIdentifier>();
                configuration.CertificateValidator.GetIssuers(_instanceCertificate, issuers).Wait();

                for (var i = 0; i < issuers.Count; i++)
                {
                    _instanceCertificateChain.Add(issuers[i].Certificate);
                }
            }

            // initialize the message context.
            var messageContext = channel.MessageContext;

            if (messageContext != null)
            {
                NamespaceUris = messageContext.NamespaceUris;
                _serverUris = messageContext.ServerUris;
                Factory = messageContext.Factory;
            }
            else
            {
                NamespaceUris = new NamespaceTable();
                _serverUris = new StringTable();
                Factory = new EncodeableFactory(EncodeableFactory.GlobalFactory);
            }

            // initialize the NodeCache late, it needs references to the namespaceUris
            _nodeCache = new NodeCache(this);

            // set the default preferred locales.
            _preferredLocales = new string[] { CultureInfo.CurrentCulture.Name };

            // create a context to use.
            _systemContext = new SystemContext
            {
                SystemHandle = this,
                EncodeableFactory = Factory,
                NamespaceUris = NamespaceUris,
                ServerUris = _serverUris,
                TypeTable = new Obsolete.TypeTree(_nodeCache),
                PreferredLocales = null,
                SessionId = null,
                UserIdentity = null
            };
        }



        /// <summary>
        /// An overrideable version of a session clone which is used
        /// internally to create new subclassed clones from a Session class.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="copyEventHandlers"></param>
        public virtual Session CloneSession(ITransportChannel channel,
            bool copyEventHandlers)
        {
            return new Session(channel, this, copyEventHandlers);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is Session session)
            {
                if (!_endpoint.Equals(session.Endpoint))
                {
                    return false;
                }

                if (!_sessionName.Equals(session.SessionName, StringComparison.Ordinal))
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
            return HashCode.Combine(_endpoint, _sessionName, SessionId);
        }

        /// <summary>
        /// Closes the session and the underlying channel.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopKeepAliveTimer();
                _keepAliveTimer?.Dispose();
                _keepAliveTimer = null;

                _nodeCache?.Clear();
                _nodeCache = null;

                List<Subscription> subscriptions = null;
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

                _reconnectLock.Dispose();
            }

            base.Dispose(disposing);

            if (disposing)
            {
                // suppress spurious events
                _KeepAlive = null;
                _Publish = null;
                _PublishError = null;
                _PublishSequenceNumbersToAcknowledge = null;
                _SubscriptionsChanged = null;
                _SessionClosing = null;
                _SessionConfigurationChanged = null;
            }
        }


        /// <inheritdoc/>
        public async Task OpenAsync(string sessionName, uint sessionTimeout,
            IUserIdentity identity, IList<string> preferredLocales, bool checkDomain,
            CancellationToken ct)
        {
            OpenValidateIdentity(ref identity, out var identityToken, out var identityPolicy, out var securityPolicyUri, out var requireEncryption);

            // validate the server certificate /certificate chain.
            X509Certificate2 serverCertificate = null;
            var certificateData = _endpoint.Description.ServerCertificate;

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
                    if (checkDomain)
                    {
                        await _configuration.CertificateValidator.ValidateAsync(serverCertificateChain, _endpoint, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await _configuration.CertificateValidator.ValidateAsync(serverCertificateChain, ct).ConfigureAwait(false);
                    }
                    // save for reconnect
                    _checkDomain = checkDomain;
                }
            }

            // create a nonce.
            var length = (uint)_configuration.SecurityConfiguration.NonceLength;
            var clientNonce = Utils.Nonce.CreateNonce(length);

            // send the application instance certificate for the client.
            BuildCertificateData(out var clientCertificateData, out var clientCertificateChainData);

            var clientDescription = new ApplicationDescription
            {
                ApplicationUri = _configuration.ApplicationUri,
                ApplicationName = _configuration.ApplicationName,
                ApplicationType = ApplicationType.Client,
                ProductUri = _configuration.ProductUri
            };

            if (sessionTimeout == 0)
            {
                sessionTimeout = (uint)_configuration.ClientConfiguration.DefaultSessionTimeout;
            }

            var successCreateSession = false;
            CreateSessionResponse response = null;

            //if security none, first try to connect without certificate
            if (_endpoint.Description.SecurityPolicyUri == SecurityPolicies.None)
            {
                //first try to connect with client certificate NULL
                try
                {
                    response = await base.CreateSessionAsync(
                        null,
                        clientDescription,
                        _endpoint.Description.Server.ApplicationUri,
                        _endpoint.EndpointUrl.ToString(),
                        sessionName,
                        clientNonce,
                        null,
                        sessionTimeout,
                        (uint)MessageContext.MaxMessageSize,
                        ct).ConfigureAwait(false);

                    successCreateSession = true;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Create session failed with client certificate NULL. {Error}", ex.Message);
                    successCreateSession = false;
                }
            }

            if (!successCreateSession)
            {
                response = await base.CreateSessionAsync(
                        null,
                        clientDescription,
                        _endpoint.Description.Server.ApplicationUri,
                        _endpoint.EndpointUrl.ToString(),
                        sessionName,
                        clientNonce,
                        clientCertificateChainData ?? clientCertificateData,
                        sessionTimeout,
                        (uint)MessageContext.MaxMessageSize,
                        ct).ConfigureAwait(false);
            }

            var sessionId = response.SessionId;
            var sessionCookie = response.AuthenticationToken;
            var serverNonce = response.ServerNonce;
            var serverCertificateData = response.ServerCertificate;
            var serverSignature = response.ServerSignature;
            var serverEndpoints = response.ServerEndpoints;
            var serverSoftwareCertificates = response.ServerSoftwareCertificates;

            _sessionTimeout = response.RevisedSessionTimeout;
            _maxRequestMessageSize = response.MaxRequestMessageSize;

            // save session id.
            lock (SyncRoot)
            {
                // save session id and cookie in base
                base.SessionCreated(sessionId, sessionCookie);
            }

            _logger.LogInformation("Revised session timeout value: {Timeout}. ", _sessionTimeout);
            _logger.LogInformation("Max response message size value: {MaxResponseSize}. Max request message size: {MaxRequestSize} ",
                MessageContext.MaxMessageSize, _maxRequestMessageSize);

            //we need to call CloseSession if CreateSession was successful but some other exception is thrown
            try
            {
                // verify that the server returned the same instance certificate.
                ValidateServerCertificateData(serverCertificateData);

                ValidateServerEndpoints(serverEndpoints);

                ValidateServerSignature(serverCertificate, serverSignature, clientCertificateData, clientCertificateChainData, clientNonce);

                HandleSignedSoftwareCertificates(serverSoftwareCertificates);

                // create the client signature.
                var dataToSign = Utils.Append(serverCertificate?.RawData, serverNonce);
                var clientSignature = SecurityPolicies.Sign(_instanceCertificate, securityPolicyUri, dataToSign);

                // select the security policy for the user token.
                securityPolicyUri = identityPolicy.SecurityPolicyUri;

                if (string.IsNullOrEmpty(securityPolicyUri))
                {
                    securityPolicyUri = _endpoint.Description.SecurityPolicyUri;
                }

                // save previous nonce
                var previousServerNonce = GetCurrentTokenServerNonce();

                // validate server nonce and security parameters for user identity.
                ValidateServerNonce(
                    identity,
                    serverNonce,
                    securityPolicyUri,
                    previousServerNonce,
                    _endpoint.Description.SecurityMode);

                // sign data with user token.
                var userTokenSignature = identityToken.Sign(dataToSign, securityPolicyUri);

                // encrypt token.
                identityToken.Encrypt(serverCertificate, serverNonce, securityPolicyUri);

                // send the software certificates assigned to the client.
                var clientSoftwareCertificates = GetSoftwareCertificates();

                // copy the preferred locales if provided.
                if (preferredLocales?.Count > 0)
                {
                    _preferredLocales = new StringCollection(preferredLocales);
                }

                // activate session.
                var activateResponse = await ActivateSessionAsync(
                    null,
                    clientSignature,
                    clientSoftwareCertificates,
                    _preferredLocales,
                    new ExtensionObject(identityToken),
                    userTokenSignature,
                    ct).ConfigureAwait(false);

                serverNonce = activateResponse.ServerNonce;
                var certificateResults = activateResponse.Results;
                var certificateDiagnosticInfos = activateResponse.DiagnosticInfos;

                if (certificateResults != null)
                {
                    for (var i = 0; i < certificateResults.Count; i++)
                    {
                        _logger.LogInformation("ActivateSession result[{Index}] = {Result}", i, certificateResults[i]);
                    }
                }

                if (clientSoftwareCertificates?.Count > 0 && (certificateResults == null || certificateResults.Count == 0))
                {
                    _logger.LogInformation("Empty results were received for the ActivateSession call.");
                }

                // fetch namespaces.
                await FetchNamespaceTablesAsync(ct).ConfigureAwait(false);

                lock (SyncRoot)
                {
                    // save nonces.
                    _sessionName = sessionName;
                    _identity = identity;
                    _previousServerNonce = previousServerNonce;
                    _serverNonce = serverNonce;
                    _serverCertificate = serverCertificate;

                    // update system context.
                    _systemContext.PreferredLocales = _preferredLocales;
                    _systemContext.SessionId = SessionId;
                    _systemContext.UserIdentity = identity;
                }

                // fetch operation limits
                await FetchOperationLimitsAsync(ct).ConfigureAwait(false);

                // start keep alive thread.
                StartKeepAliveTimer();

                // raise event that session configuration changed.
                IndicateSessionConfigurationChanged();

                // call session created callback, which was already set in base class only.
                SessionCreated(sessionId, sessionCookie);
            }
            catch (Exception)
            {
                try
                {
                    await base.CloseSessionAsync(null, false, CancellationToken.None).ConfigureAwait(false);
                    await CloseChannelAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError("Cleanup: CloseSessionAsync() or CloseChannelAsync() raised exception {Error}.", e.Message);
                }
                finally
                {
                    SessionCreated(null, null);
                }

                throw;
            }
        }

        /// <summary>
        /// Recreates a session based on a specified template.
        /// </summary>
        /// <param name="sessionTemplate">The Session object to use as template</param>
        /// <param name="ct"></param>
        /// <returns>The new session object.</returns>
        public static async Task<Session> RecreateAsync(Session sessionTemplate,
            CancellationToken ct = default)
        {
            var messageContext = sessionTemplate._configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the channel object used to connect to the server.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var channel = SessionChannel.Create(
                sessionTemplate._configuration,
                sessionTemplate.ConfiguredEndpoint.Description,
                sessionTemplate.ConfiguredEndpoint.Configuration,
                sessionTemplate._instanceCertificate,
                sessionTemplate._configuration.SecurityConfiguration.SendCertificateChain ?
                    sessionTemplate._instanceCertificateChain : null,
                messageContext);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // create the session object.
            var session = sessionTemplate.CloneSession(channel, true);

            try
            {
                // open the session.
                await session.OpenAsync(
                    sessionTemplate.SessionName,
                    (uint)sessionTemplate.SessionTimeout,
                    sessionTemplate.Identity,
                    sessionTemplate.PreferredLocales,
                    sessionTemplate._checkDomain,
                    ct).ConfigureAwait(false);

                await session.RecreateSubscriptionsAsync(sessionTemplate.Subscriptions, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                session.Dispose();
                ThrowCouldNotRecreateSessionException(e, sessionTemplate._sessionName);
            }

            return session;
        }

        /// <summary>
        /// Recreates a session based on a specified template.
        /// </summary>
        /// <param name="sessionTemplate">The Session object to use as template</param>
        /// <param name="connection">The waiting reverse connection.</param>
        /// <param name="ct"></param>
        /// <returns>The new session object.</returns>
        public static async Task<Session> RecreateAsync(Session sessionTemplate,
            ITransportWaitingConnection connection, CancellationToken ct = default)
        {
            var messageContext = sessionTemplate._configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the channel object used to connect to the server.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var channel = SessionChannel.Create(
                sessionTemplate._configuration,
                connection,
                sessionTemplate._endpoint.Description,
                sessionTemplate._endpoint.Configuration,
                sessionTemplate._instanceCertificate,
                sessionTemplate._configuration.SecurityConfiguration.SendCertificateChain ?
                    sessionTemplate._instanceCertificateChain : null,
                messageContext);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // create the session object.
            var session = sessionTemplate.CloneSession(channel, true);

            try
            {
                // open the session.
                await session.OpenAsync(
                    sessionTemplate._sessionName,
                    (uint)sessionTemplate._sessionTimeout,
                    sessionTemplate._identity,
                    sessionTemplate._preferredLocales,
                    sessionTemplate._checkDomain,
                    ct).ConfigureAwait(false);

                await session.RecreateSubscriptionsAsync(sessionTemplate.Subscriptions, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                session.Dispose();
                ThrowCouldNotRecreateSessionException(e, sessionTemplate._sessionName);
            }

            return session;
        }

        /// <summary>
        /// Recreates a session based on a specified template using the provided channel.
        /// </summary>
        /// <param name="sessionTemplate">The Session object to use as template</param>
        /// <param name="transportChannel">The waiting reverse connection.</param>
        /// <param name="ct"></param>
        /// <returns>The new session object.</returns>
        public static async Task<Session> RecreateAsync(Session sessionTemplate,
            ITransportChannel transportChannel, CancellationToken ct = default)
        {
            if (transportChannel == null)
            {
                return await Session.RecreateAsync(sessionTemplate, ct).ConfigureAwait(false);
            }

            var messageContext = sessionTemplate._configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the session object.
            var session = sessionTemplate.CloneSession(transportChannel, true);

            try
            {
                // open the session.
                await session.OpenAsync(
                    sessionTemplate._sessionName,
                    (uint)sessionTemplate._sessionTimeout,
                    sessionTemplate._identity,
                    sessionTemplate._preferredLocales,
                    sessionTemplate._checkDomain,
                    ct).ConfigureAwait(false);

                // create the subscriptions.
                foreach (var subscription in session.Subscriptions)
                {
                    await subscription.CreateAsync(ct).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                session.Dispose();
                ThrowCouldNotRecreateSessionException(e, sessionTemplate._sessionName);
            }

            return session;
        }

        /// <inheritdoc/>
        public override Task<StatusCode> CloseAsync(CancellationToken ct)
        {
            return CloseAsync(true, ct);
        }

        /// <inheritdoc/>
        public async Task<StatusCode> CloseAsync(bool closeChannel,
            CancellationToken ct)
        {
            // check if already called.
            if (Disposed)
            {
                return StatusCodes.Good;
            }

            // stop the keep alive timer.
            StopKeepAliveTimer();

            // check if correctly connected.
            var connected = Connected;

            // halt all background threads.
            if (connected && _SessionClosing != null)
            {
                try
                {
                    _SessionClosing(this, null);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        "Unexpected error raising SessionClosing event.");
                }
            }


            StatusCode result = StatusCodes.Good;
            // close the session with the server.
            if (connected && !KeepAliveStopped)
            {
                try
                {
                    // close the session and delete all subscriptions if
                    // specified.
                    var timeout = _keepAliveInterval;
                    var requestHeader = new RequestHeader()
                    {
                        TimeoutHint = timeout > 0 ? (uint)timeout : (uint)
                            (OperationTimeout > 0 ? OperationTimeout : 0)
                    };
                    var response = await base.CloseSessionAsync(requestHeader,
                        DeleteSubscriptionsOnClose, ct).ConfigureAwait(false);
                    if (closeChannel)
                    {
                        await CloseChannelAsync(ct).ConfigureAwait(false);
                    }

                    // raised notification indicating the session is closed.
                    SessionCreated(null, null);
                }
                // don't throw errors on disconnect, but return them
                // so the caller can log the error.
                catch (ServiceResultException sre)
                {
                    result = sre.StatusCode;
                }
                catch (Exception)
                {
                    result = StatusCodes.Bad;
                }
            }

            // clean up.
            if (closeChannel)
            {
                Dispose();
            }

            return result;
        }

        /// <inheritdoc/>
        public Task ReconnectAsync(CancellationToken ct)
            => ReconnectAsync(null, null, ct);

        /// <inheritdoc/>
        public Task ReconnectAsync(ITransportWaitingConnection connection,
            CancellationToken ct)
            => ReconnectAsync(connection, null, ct);

        /// <summary>
        /// Reconnects to the server after a network failure using a waiting connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transportChannel"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        private async Task ReconnectAsync(ITransportWaitingConnection connection,
            ITransportChannel transportChannel, CancellationToken ct)
        {
            var resetReconnect = false;
            await _reconnectLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var reconnecting = _reconnecting;
                _reconnecting = true;
                resetReconnect = true;
                _reconnectLock.Release();

                // check if already connecting.
                if (reconnecting)
                {
                    _logger.LogWarning("Session is already attempting to reconnect.");

                    throw ServiceResultException.Create(
                        StatusCodes.BadInvalidState,
                        "Session is already attempting to reconnect.");
                }

                StopKeepAliveTimer();

                var result = PrepareReconnectBeginActivate(
                    connection,
                    transportChannel);

                if (result is ChannelAsyncOperation<int> operation)
                {
                    try
                    {
                        _ = await operation.EndAsync(kReconnectTimeout / 2, true, ct).ConfigureAwait(false);
                    }
                    catch (ServiceResultException sre)
                    {
                        if (sre.StatusCode == StatusCodes.BadRequestInterrupted)
                        {
                            var error = ServiceResult.Create(StatusCodes.BadRequestTimeout,
                                "ACTIVATE SESSION ASYNC timed out. {0}/{1}",
                                GoodPublishRequestCount, OutstandingRequestCount);
                            _logger.LogWarning("WARNING: {Error}", error.ToString());
                            operation.Fault(false, error);
                        }
                    }
                }
                else if (!result.AsyncWaitHandle.WaitOne(kReconnectTimeout / 2))
                {
                    _logger.LogWarning("ACTIVATE SESSION ASYNC timed out. {Good}/{Outstanding}",
                        GoodPublishRequestCount, OutstandingRequestCount);
                }

                // reactivate session.

                EndActivateSession(
                    result,
                    out var serverNonce,
                    out var certificateResults,
                    out var certificateDiagnosticInfos);

                _logger.LogInformation("Session RECONNECT {Session} completed successfully.", SessionId);

                lock (SyncRoot)
                {
                    _previousServerNonce = _serverNonce;
                    _serverNonce = serverNonce;
                }

                await _reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                _reconnecting = false;
                resetReconnect = false;
                _reconnectLock.Release();

                StartPublishing(OperationTimeout, true);

                StartKeepAliveTimer();

                IndicateSessionConfigurationChanged();
            }
            finally
            {
                if (resetReconnect)
                {
                    await _reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                    _reconnecting = false;
                    _reconnectLock.Release();
                }
            }
        }



        /// <inheritdoc/>
        public async Task FetchNamespaceTablesAsync(CancellationToken ct = default)
        {
            var nodesToRead = new ReadValueIdCollection();

            // request namespace array.
            var valueId = new ReadValueId
            {
                NodeId = Variables.Server_NamespaceArray,
                AttributeId = Attributes.Value
            };

            nodesToRead.Add(valueId);

            // request server array.
            valueId = new ReadValueId
            {
                NodeId = Variables.Server_ServerArray,
                AttributeId = Attributes.Value
            };

            nodesToRead.Add(valueId);

            // read from server.
            var response = await ReadAsync(null, 0, TimestampsToReturn.Neither,
                nodesToRead, ct).ConfigureAwait(false);

            var values = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;
            var responseHeader = response.ResponseHeader;

            ValidateResponse(values, nodesToRead);
            ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

            // validate namespace array.
            var result = ValidateDataValue(values[0], typeof(string[]),
                0, diagnosticInfos, responseHeader);
            if (ServiceResult.IsBad(result))
            {
                _logger.LogError(
                    "FetchNamespaceTables: Cannot read NamespaceArray node: {Status}",
                    result.StatusCode);
            }
            else
            {
                NamespaceUris.Update((string[])values[0].Value);
            }

            // validate server array.
            result = ValidateDataValue(values[1], typeof(string[]), 1,
                diagnosticInfos, responseHeader);
            if (ServiceResult.IsBad(result))
            {
                _logger.LogError(
                    "FetchNamespaceTables: Cannot read ServerArray node: {Status} ",
                    result.StatusCode);
            }
            else
            {
                _serverUris.Update((string[])values[1].Value);
            }
        }

        /// <summary>
        /// Fetch the operation limits of the server.
        /// </summary>
        /// <param name="ct"></param>
        public async Task FetchOperationLimitsAsync(CancellationToken ct)
        {
            try
            {
                var operationLimitsProperties = typeof(OperationLimits)
                    .GetProperties().Select(p => p.Name).ToList();

                var nodeIds = new NodeIdCollection(
                    operationLimitsProperties.Select(name => (NodeId)typeof(VariableIds)
                    .GetField("Server_ServerCapabilities_OperationLimits_" + name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .GetValue(null))
                    );

                // add the server capability MaxContinuationPointPerBrowse. Add further capabilities
                // later (when support form them will be implemented and in a more generic fashion)
                nodeIds.Add(VariableIds.Server_ServerCapabilities_MaxBrowseContinuationPoints);
                var maxBrowseContinuationPointIndex = nodeIds.Count - 1;

                (var values, var errors) = await ReadValuesAsync(nodeIds, ct).ConfigureAwait(false);

                var configOperationLimits = _configuration?.ClientConfiguration?.OperationLimits ?? new OperationLimits();
                var operationLimits = new OperationLimits();

                for (var ii = 0; ii < operationLimitsProperties.Count; ii++)
                {
                    var property = typeof(OperationLimits).GetProperty(operationLimitsProperties[ii]);
                    var value = (uint)property.GetValue(configOperationLimits);
                    if (values[ii] != null &&
                        ServiceResult.IsNotBad(errors[ii]) &&
                        values[ii].Value is uint serverValue && serverValue > 0 &&
                           (value == 0 || serverValue < value))
                    {
                        value = serverValue;
                    }
                    property.SetValue(operationLimits, value);
                }

                OperationLimits = operationLimits;
                if (values[maxBrowseContinuationPointIndex].Value != null
                    && ServiceResult.IsNotBad(errors[maxBrowseContinuationPointIndex]))
                {
                    ServerMaxContinuationPointsPerBrowse = (ushort)values[maxBrowseContinuationPointIndex].Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read operation limits from server. Using configuration defaults.");
                var operationLimits = _configuration?.ClientConfiguration?.OperationLimits;
                if (operationLimits != null)
                {
                    OperationLimits = operationLimits;
                }
            }
        }





        /// <inheritdoc/>
        public async Task<ResultSet<Node>> ReadNodesAsync(
            IReadOnlyList<NodeId> nodeIds, CancellationToken ct)
        {
            if (nodeIds.Count == 0)
            {
                return ResultSet.Empty<Node>();
            }

            var nodeCollection = new NodeCollection(nodeIds.Count);
            // first read only nodeclasses for nodes from server.
            var itemsToRead = new ReadValueIdCollection(
                nodeIds.Select(nodeId => new ReadValueId
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
            var attributesPerNodeId = new List<IDictionary<uint, DataValue>>(
                nodeIds.Count);

            var serviceResults = new List<ServiceResult>(nodeIds.Count);
            var attributesToRead = new ReadValueIdCollection();
            var responseHeader = readResponse.ResponseHeader;
            int? nodeClass;
            for (var index = 0; index < itemsToRead.Count; index++)
            {
                var node = new Node();
                node.NodeId = itemsToRead[index].NodeId;
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
                    serviceResults.Add(ServiceResult.Create(
                        StatusCodes.BadUnexpectedError,
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
                    var subRangeAttributes = new ReadValueIdCollection(attributesToRead.GetRange(readIndex, readCount));
                    var subRangeValues = new DataValueCollection(values.GetRange(readIndex, readCount));
                    var subRangeDiagnostics = diagnosticInfos.Count > 0 ? new DiagnosticInfoCollection(diagnosticInfos.GetRange(readIndex, readCount)) : diagnosticInfos;
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
            var itemsToRead = new ReadValueIdCollection();
            foreach (var attributeId in attributes.Keys)
            {
                var itemToRead = new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = attributeId
                };
                itemsToRead.Add(itemToRead);
            }

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
                (IReadOnlyList<NodeId>)(new [] { nodeId }), 0, BrowseDirection.Both, null, true, 0,
                ct).ConfigureAwait(false);
            return descriptions[0];
        }

        /// <inheritdoc/>
        public Task<ResultSet<ReferenceDescriptionCollection>> FetchReferencesAsync(
            IReadOnlyList<NodeId> nodeIds, CancellationToken ct)
        {
            return BrowseAsync(null, null, nodeIds, 0, BrowseDirection.Both,
                null, true, 0, ct);
        }



        /// <inheritdoc/>
        public async Task<DataValue> ReadValueAsync(NodeId nodeId,
            CancellationToken ct)
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
            var readResponse = await ReadAsync(null, 0, TimestampsToReturn.Both,
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
        public async Task<(DataValueCollection, IList<ServiceResult>)> ReadValuesAsync(
            IList<NodeId> nodeIds, CancellationToken ct)
        {
            if (nodeIds.Count == 0)
            {
                return (new DataValueCollection(), new List<ServiceResult>());
            }

            // read all values from server.
            var itemsToRead = new ReadValueIdCollection(
                nodeIds.Select(nodeId =>
                    new ReadValueId
                    {
                        NodeId = nodeId,
                        AttributeId = Attributes.Value
                    }));

            // read from server.
            var errors = new List<ServiceResult>(itemsToRead.Count);

            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Both,
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
                    result = ClientBase.GetResult(values[0].StatusCode, 0, diagnosticInfos, readResponse.ResponseHeader);
                }
                errors.Add(result);
            }

            return (values, errors);
        }





        /// <inheritdoc/>
        public async Task<(
            ResponseHeader responseHeader,
            ByteStringCollection continuationPoints,
            IList<ReferenceDescriptionCollection> referencesList,
            IList<ServiceResult> errors
            )> BrowseAsync(RequestHeader requestHeader, ViewDescription view,
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

            var browseResponse = await BrowseAsync(
                requestHeader,
                view,
                maxResultsToReturn,
                browseDescriptions,
                ct).ConfigureAwait(false);

            ClientBase.ValidateResponse(browseResponse.ResponseHeader);
            var results = browseResponse.Results;
            var diagnosticInfos = browseResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(results, browseDescriptions);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, browseDescriptions);

            var ii = 0;
            var errors = new List<ServiceResult>();
            var continuationPoints = new ByteStringCollection();
            var referencesList = new List<ReferenceDescriptionCollection>();
            foreach (var result in results)
            {
                if (StatusCode.IsBad(result.StatusCode))
                {
                    errors.Add(new ServiceResult(result.StatusCode, ii, diagnosticInfos, browseResponse.ResponseHeader.StringTable));
                }
                else
                {
                    errors.Add(ServiceResult.Good);
                }
                continuationPoints.Add(result.ContinuationPoint);
                referencesList.Add(result.References);
                ii++;
            }

            return (browseResponse.ResponseHeader, continuationPoints, referencesList, errors);
        }



        private async Task<ResultSet<ReferenceDescriptionCollection>> BrowseAsync(
            RequestHeader requestHeader, ViewDescription view,
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

                    if (ContinuationPointPolicy == ContinuationPointPolicy.Balanced &&
                        ServerMaxContinuationPointsPerBrowse > 0)
                    {
                        maxNodesPerBrowse =
                            ServerMaxContinuationPointsPerBrowse < maxNodesPerBrowse ?
                            ServerMaxContinuationPointsPerBrowse : maxNodesPerBrowse;
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
                        for (var ii = 0; ii < nodesToBrowseBatchCount; ii++)
                        {
                            var statusCode = errorsForBatch[ii].StatusCode;
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
                            resultForPass[resultOffset].AddRange(resultForBatch[ii]);
                            errorsForPass[resultOffset] = errorsForBatch[ii];
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
            public T reference { get; set; }
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
            RequestHeader requestHeader, ViewDescription view, List<NodeId> nodeIds,
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
                    reference = error
                });
                errorAnchors.Add(previousErrors[^1]);
            }

            var nextContinuationPoints = new ByteStringCollection();
            var nextResults = new List<ReferenceDescriptionCollection>();
            var nextErrors = new List<ReferenceWrapper<ServiceResult>>();
            for (var ii = 0; ii < nodeIds.Count; ii++)
            {
                if (continuationPoints[ii] != null &&
                    !StatusCode.IsBad(previousErrors[ii].reference.StatusCode))
                {
                    nextContinuationPoints.Add(continuationPoints[ii]);
                    nextResults.Add(previousResults[ii]);
                    nextErrors.Add(previousErrors[ii]);
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
                for (var ii = 0; ii < browseNextResults.Count; ii++)
                {
                    nextResults[ii].AddRange(browseNextResults[ii]);
                    nextErrors[ii].reference = browseNextErrors[ii];
                }

                previousResults = nextResults;
                previousErrors = nextErrors;

                nextResults = new List<ReferenceDescriptionCollection>();
                nextErrors = new List<ReferenceWrapper<ServiceResult>>();
                nextContinuationPoints = new ByteStringCollection();

                for (var ii = 0; ii < revisedContinuationPoints.Count; ii++)
                {
                    if (revisedContinuationPoints[ii] != null &&
                        !StatusCode.IsBad(browseNextErrors[ii].StatusCode))
                    {
                        nextContinuationPoints.Add(revisedContinuationPoints[ii]);
                        nextResults.Add(previousResults[ii]);
                        nextErrors.Add(previousErrors[ii]);
                    }
                }
            }
            var finalErrors = new List<ServiceResult>(errorAnchors.Count);
            foreach (var errorReference in errorAnchors)
            {
                finalErrors.Add(errorReference.reference);
            }
            return new ResultSet<ReferenceDescriptionCollection>(result, finalErrors);
        }

        private async Task<(
            ResponseHeader responseHeader,
            ByteStringCollection revisedContinuationPoints,
            IList<ReferenceDescriptionCollection> referencesList,
            IList<ServiceResult> errors
            )> BrowseNextAsync(RequestHeader requestHeader,
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
            var ii = 0;
            var errors = new List<ServiceResult>();
            var revisedContinuationPoints = new ByteStringCollection();
            var referencesList = new List<ReferenceDescriptionCollection>();
            foreach (var result in results)
            {
                if (StatusCode.IsBad(result.StatusCode))
                {
                    errors.Add(new ServiceResult(result.StatusCode,
                        ii, diagnosticInfos, response.ResponseHeader.StringTable));
                }
                else
                {
                    errors.Add(ServiceResult.Good);
                }
                revisedContinuationPoints.Add(result.ContinuationPoint);
                referencesList.Add(result.References);
                ii++;
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
                if (_subscriptions.Contains(subscription))
                {
                    return false;
                }

                subscription.Session = this;
                _subscriptions.Add(subscription);
            }

            _SubscriptionsChanged?.Invoke(this, null);

            return true;
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
                subscription.Session = null;
            }
            _SubscriptionsChanged?.Invoke(this, null);
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveSubscriptionsAsync(
            IEnumerable<Subscription> subscriptions, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(subscriptions);
            var subscriptionsToDelete = new List<Subscription>();
            var removed = false;
            lock (SyncRoot)
            {
                foreach (var subscription in subscriptions)
                {
                    if (_subscriptions.Remove(subscription))
                    {
                        if (subscription.Created)
                        {
                            subscriptionsToDelete.Add(subscription);
                        }

                        removed = true;
                    }
                }
            }

            foreach (var subscription in subscriptionsToDelete)
            {
                await subscription.DeleteAsync(true, ct).ConfigureAwait(false);
            }

            if (removed)
            {
                _SubscriptionsChanged?.Invoke(this, null);
            }
            return removed;
        }

        /// <inheritdoc/>
        public async Task<bool> TransferSubscriptionsAsync(
            SubscriptionCollection subscriptions, bool sendInitialValues,
            CancellationToken ct)
        {
            var subscriptionIds = CreateSubscriptionIdsForTransfer(subscriptions);
            var failedSubscriptions = 0;

            if (subscriptionIds.Count > 0)
            {
                var reconnecting = false;
                await _reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    reconnecting = _reconnecting;
                    _reconnecting = true;

                    var response = await base.TransferSubscriptionsAsync(null, subscriptionIds, sendInitialValues, ct).ConfigureAwait(false);
                    var results = response.Results;
                    var diagnosticInfos = response.DiagnosticInfos;
                    var responseHeader = response.ResponseHeader;

                    if (!StatusCode.IsGood(responseHeader.ServiceResult))
                    {
                        _logger.LogError("TransferSubscription failed: {Result}", responseHeader.ServiceResult);
                        return false;
                    }

                    ClientBase.ValidateResponse(results, subscriptionIds);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, subscriptionIds);

                    for (var ii = 0; ii < subscriptions.Count; ii++)
                    {
                        if (StatusCode.IsGood(results[ii].StatusCode))
                        {
                            if (await subscriptions[ii].TransferAsync(this, subscriptionIds[ii], results[ii].AvailableSequenceNumbers, ct).ConfigureAwait(false))
                            {
                                lock (_acknowledgementsToSendLock)
                                {
                                    // create ack for available sequence numbers
                                    foreach (var sequenceNumber in results[ii].AvailableSequenceNumbers)
                                    {
                                        AddAcknowledgementToSend(_acknowledgementsToSend, subscriptionIds[ii], sequenceNumber);
                                    }
                                }
                            }
                        }
                        else if (results[ii].StatusCode == StatusCodes.BadNothingToDo)
                        {
                            _logger.LogInformation("SubscriptionId {Id} is already member of the session.", subscriptionIds[ii]);
                            failedSubscriptions++;
                        }
                        else
                        {
                            _logger.LogError("SubscriptionId {Id} failed to transfer, StatusCode={Status}", subscriptionIds[ii], results[ii].StatusCode);
                            failedSubscriptions++;
                        }
                    }

                    _logger.LogInformation("Session TRANSFER ASYNC of {Count} subscriptions completed. {Failed} failed.", subscriptions.Count, failedSubscriptions);
                }
                finally
                {
                    _reconnecting = reconnecting;
                    _reconnectLock.Release();
                }

                StartPublishing(OperationTimeout, false);
            }
            else
            {
                _logger.LogInformation("No subscriptions. TransferSubscription skipped.");
            }

            return failedSubscriptions == 0;
        }

        /// <inheritdoc/>
        public bool RemoveTransferredSubscription(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription);

            if (subscription.Session != this)
            {
                return false;
            }

            lock (SyncRoot)
            {
                if (!_subscriptions.Remove(subscription))
                {
                    return false;
                }

                subscription.Session = null;
            }

            _SubscriptionsChanged?.Invoke(this, null);

            return true;
        }

        /// <inheritdoc/>
        public async Task<IList<object>> CallAsync(NodeId objectId,
            NodeId methodId, CancellationToken ct = default, params object[] args)
        {
            var inputArguments = new VariantCollection();

            if (args != null)
            {
                for (var ii = 0; ii < args.Length; ii++)
                {
                    inputArguments.Add(new Variant(args[ii]));
                }
            }

            var request = new CallMethodRequest();

            request.ObjectId = objectId;
            request.MethodId = methodId;
            request.InputArguments = inputArguments;

            var requests = new CallMethodRequestCollection();
            requests.Add(request);

            CallMethodResultCollection results;
            DiagnosticInfoCollection diagnosticInfos;

            var response = await base.CallAsync(null, requests, ct).ConfigureAwait(false);

            results = response.Results;
            diagnosticInfos = response.DiagnosticInfos;

            ClientBase.ValidateResponse(results, requests);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, requests);

            if (StatusCode.IsBad(results[0].StatusCode))
            {
                throw ServiceResultException.Create(results[0].StatusCode, 0, diagnosticInfos, response.ResponseHeader.StringTable);
            }

            var outputArguments = new List<object>();

            foreach (var arg in results[0].OutputArguments)
            {
                outputArguments.Add(arg.Value);
            }

            return outputArguments;
        }

        /// <inheritdoc/>
        public async Task<(bool, ServiceResult)> RepublishAsync(uint subscriptionId,
            uint sequenceNumber, CancellationToken ct)
        {
            // send republish request.
            var requestHeader = new RequestHeader
            {
                TimeoutHint = (uint)OperationTimeout,
                ReturnDiagnostics = (uint)(int)ReturnDiagnostics,
                RequestHandle = Utils.IncrementIdentifier(ref _publishCounter)
            };

            try
            {
                _logger.LogInformation("Requesting RepublishAsync for {SubscriptionId}-{SeqNumber}", subscriptionId, sequenceNumber);

                // request republish.
                var response = await RepublishAsync(
                    requestHeader,
                    subscriptionId,
                    sequenceNumber,
                    ct).ConfigureAwait(false);
                var responseHeader = response.ResponseHeader;
                var notificationMessage = response.NotificationMessage;

                _logger.LogInformation("Received RepublishAsync for {SubscriptionId}-{SeqNumber}-{Result}", subscriptionId, sequenceNumber, responseHeader.ServiceResult);

                // process response.
                ProcessPublishResponse(
                    responseHeader,
                    subscriptionId,
                    null,
                    false,
                    notificationMessage);

                return (true, ServiceResult.Good);
            }
            catch (Exception e)
            {
                return ProcessRepublishResponseError(e, subscriptionId, sequenceNumber);
            }
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
            IList<SoftwareCertificate> softwareCertificates)
        {
            // always accept valid certificates.
        }

        /// <summary>
        /// Starts a timer to check that the connection to the server is still available.
        /// </summary>
        private void StartKeepAliveTimer()
        {
            var keepAliveInterval = _keepAliveInterval;

            _lastKeepAliveErrorStatusCode = StatusCodes.Good;
            Interlocked.Exchange(ref _lastKeepAliveTime, DateTime.UtcNow.Ticks);
            LastKeepAliveTickCount = HiResClock.TickCount;

            _serverState = ServerState.Unknown;

            var nodesToRead = new ReadValueIdCollection() {
                // read the server state.
                new ReadValueId {
                    NodeId = Variables.Server_ServerStatus_State,
                    AttributeId = Attributes.Value,
                    DataEncoding = null,
                    IndexRange = null
                }
            };

            // restart the publish timer.
            lock (SyncRoot)
            {
                StopKeepAliveTimer();

#if PERIODIC_TIMER
                // start periodic timer loop
                var keepAliveTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(keepAliveInterval));
                _ = Task.Run(() => OnKeepAliveAsync(keepAliveTimer, nodesToRead));
                _keepAliveTimer = keepAliveTimer;
            }
#else
                // start timer
                _keepAliveTimer = new Timer(OnKeepAlive, nodesToRead, keepAliveInterval, keepAliveInterval);
            }

            // send initial keep alive.
            OnKeepAlive(nodesToRead);
#endif
        }

        /// <summary>
        /// Stops the keep alive timer.
        /// </summary>
        private void StopKeepAliveTimer()
        {
            _keepAliveTimer?.Dispose();
            _keepAliveTimer = null;
        }

#if PERIODIC_TIMER
        /// <summary>
        /// Sends a keep alive by reading from the server.
        /// </summary>
        /// <param name="keepAliveTimer"></param>
        /// <param name="nodesToRead"></param>
        private async Task OnKeepAliveAsync(PeriodicTimer keepAliveTimer, ReadValueIdCollection nodesToRead)
        {
            // trigger first keep alive
            OnSendKeepAlive(nodesToRead);

            while (await keepAliveTimer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                OnSendKeepAlive(nodesToRead);
            }

            _logger.LogTrace("Session {Id}: KeepAlive PeriodicTimer exit.", SessionId);
        }
#else
        /// <summary>
        /// Sends a keep alive by reading from the server.
        /// </summary>
        private void OnKeepAlive(object state)
        {
            ReadValueIdCollection nodesToRead = (ReadValueIdCollection)state;
            OnSendKeepAlive(nodesToRead);
        }
#endif

        /// <summary>
        /// Sends a keep alive by reading from the server.
        /// </summary>
        /// <param name="nodesToRead"></param>
        private void OnSendKeepAlive(ReadValueIdCollection nodesToRead)
        {
            try
            {
                // check if session has been closed.
                if (!Connected || _keepAliveTimer == null)
                {
                    return;
                }

                // check if session has been closed.
                if (_reconnecting)
                {
                    _logger.LogWarning("Session {Id}: KeepAlive ignored while reconnecting.", SessionId);
                    return;
                }

                // raise error if keep alives are not coming back.
                if (KeepAliveStopped && !OnKeepAliveError(ServiceResult.Create(StatusCodes.BadNoCommunication, "Server not responding to keep alive requests.")))
                {
                    return;
                }

                var requestHeader = new RequestHeader
                {
                    RequestHandle = Utils.IncrementIdentifier(ref _keepAliveCounter),
                    TimeoutHint = (uint)(KeepAliveInterval * 2),
                    ReturnDiagnostics = 0
                };

                var result = BeginRead(
                    requestHeader,
                    0,
                    TimestampsToReturn.Neither,
                    nodesToRead,
                    OnKeepAliveComplete,
                    nodesToRead);

                AsyncRequestStarted(result, requestHeader.RequestHandle, DataTypes.ReadRequest);
            }
            catch (ServiceResultException sre) when (sre.StatusCode == StatusCodes.BadNotConnected)
            {
                // recover from error condition when secure channel is still alive
                OnKeepAliveError(sre.Result);
            }
            catch (Exception e)
            {
                _logger.LogError("Could not send keep alive request: {ErrorType} {Message}", e.GetType().FullName, e.Message);
            }
        }

        /// <summary>
        /// Checks if a notification has arrived. Sends a publish if it has not.
        /// </summary>
        /// <param name="result"></param>
        private void OnKeepAliveComplete(IAsyncResult result)
        {
            var nodesToRead = (ReadValueIdCollection)result.AsyncState;

            AsyncRequestCompleted(result, 0, DataTypes.ReadRequest);

            try
            {
                // read the server status.
                var values = new DataValueCollection();
                var diagnosticInfos = new DiagnosticInfoCollection();

                var responseHeader = EndRead(
                    result,
                    out values,
                    out diagnosticInfos);

                ValidateResponse(values, nodesToRead);
                ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

                // validate value returned.
                var error = ValidateDataValue(values[0], typeof(int), 0, diagnosticInfos, responseHeader);

                if (ServiceResult.IsBad(error))
                {
                    throw new ServiceResultException(error);
                }

                // send notification that keep alive completed.
                OnKeepAlive((ServerState)(int)values[0].Value, responseHeader.Timestamp);

                return;
            }
            catch (ServiceResultException sre)
            {
                // recover from error condition when secure channel is still alive
                OnKeepAliveError(sre.Result);
            }
            catch (Exception e)
            {
                _logger.LogError("Unexpected keep alive error occurred: {Message}", e.Message);
            }
        }

        /// <summary>
        /// Called when the server returns a keep alive response.
        /// </summary>
        /// <param name="currentState"></param>
        /// <param name="currentTime"></param>
        protected virtual void OnKeepAlive(ServerState currentState, DateTime currentTime)
        {
            // restart publishing if keep alives recovered.
            if (KeepAliveStopped)
            {
                // ignore if already reconnecting.
                if (_reconnecting)
                {
                    return;
                }

                _lastKeepAliveErrorStatusCode = StatusCodes.Good;
                Interlocked.Exchange(ref _lastKeepAliveTime, DateTime.UtcNow.Ticks);
                LastKeepAliveTickCount = HiResClock.TickCount;

                lock (_outstandingRequests)
                {
                    for (var ii = _outstandingRequests.First; ii != null; ii = ii.Next)
                    {
                        if (ii.Value.RequestTypeId == DataTypes.PublishRequest)
                        {
                            ii.Value.Defunct = true;
                        }
                    }
                }

                StartPublishing(OperationTimeout, false);
            }
            else
            {
                _lastKeepAliveErrorStatusCode = StatusCodes.Good;
                Interlocked.Exchange(ref _lastKeepAliveTime, DateTime.UtcNow.Ticks);
                LastKeepAliveTickCount = HiResClock.TickCount;
            }

            // save server state.
            _serverState = currentState;

            var callback = _KeepAlive;

            if (callback != null)
            {
                try
                {
                    callback(this, new KeepAliveEventArgs(null, currentState, currentTime));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Session: Unexpected error invoking KeepAliveCallback.");
                }
            }
        }

        /// <summary>
        /// Called when a error occurs during a keep alive.
        /// </summary>
        /// <param name="result"></param>
        protected virtual bool OnKeepAliveError(ServiceResult result)
        {
            _lastKeepAliveErrorStatusCode = result.StatusCode;
            if (result.StatusCode == StatusCodes.BadNoCommunication)
            {
                //keep alive read timed out
                var delta = HiResClock.TickCount - LastKeepAliveTickCount;
                _logger.LogInformation("KEEP ALIVE LATE: {Late}ms, " +
                    "EndpointUrl={Url}, RequestCount={Good}/{Outstanding}",
                    delta, Endpoint?.EndpointUrl, GoodPublishRequestCount,
                    OutstandingRequestCount);
            }

            var callback = _KeepAlive;
            if (callback != null)
            {
                try
                {
                    var args = new KeepAliveEventArgs(result,
                        ServerState.Unknown, DateTime.UtcNow);
                    callback(this, args);
                    return !args.CancelKeepAlive;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Unexpected error invoking KeepAliveCallback.");
                }
            }
            return true;
        }

        /// <summary>
        /// Removes a completed async request.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="requestId"></param>
        /// <param name="typeId"></param>
        private AsyncRequestState RemoveRequest(IAsyncResult result, uint requestId,
            uint typeId)
        {
            lock (_outstandingRequests)
            {
                for (var ii = _outstandingRequests.First; ii != null; ii = ii.Next)
                {
                    if (Object.ReferenceEquals(result, ii.Value.Result) || (requestId == ii.Value.RequestId && typeId == ii.Value.RequestTypeId))
                    {
                        var state = ii.Value;
                        _outstandingRequests.Remove(ii);
                        return state;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Adds a new async request.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="requestId"></param>
        /// <param name="typeId"></param>
        private void AsyncRequestStarted(IAsyncResult result, uint requestId,
            uint typeId)
        {
            lock (_outstandingRequests)
            {
                // check if the request completed asynchronously.
                var state = RemoveRequest(result, requestId, typeId);

                // add a new request.
                if (state == null)
                {
                    state = new AsyncRequestState();

                    state.Defunct = false;
                    state.RequestId = requestId;
                    state.RequestTypeId = typeId;
                    state.Result = result;
                    state.TickCount = HiResClock.TickCount;

                    _outstandingRequests.AddLast(state);
                }
            }
        }

        /// <summary>
        /// Removes a completed async request.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="requestId"></param>
        /// <param name="typeId"></param>
        private void AsyncRequestCompleted(IAsyncResult result, uint requestId, uint typeId)
        {
            lock (_outstandingRequests)
            {
                // remove the request.
                var state = RemoveRequest(result, requestId, typeId);

                if (state != null)
                {
                    // mark any old requests as default (i.e. the should have returned before this request).
                    const int maxAge = 1000;

                    for (var ii = _outstandingRequests.First; ii != null; ii = ii.Next)
                    {
                        if (ii.Value.RequestTypeId == typeId && (state.TickCount - ii.Value.TickCount) > maxAge)
                        {
                            ii.Value.Defunct = true;
                        }
                    }
                }

                // add a dummy placeholder since the begin request has not completed yet.
                if (state == null)
                {
                    state = new AsyncRequestState();

                    state.Defunct = true;
                    state.RequestId = requestId;
                    state.RequestTypeId = typeId;
                    state.Result = result;
                    state.TickCount = HiResClock.TickCount;

                    _outstandingRequests.AddLast(state);
                }
            }
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
        private static Node ProcessReadResponse(
            ResponseHeader responseHeader,
            IDictionary<uint, DataValue> attributes,
            ReadValueIdCollection itemsToRead,
            DataValueCollection values,
            DiagnosticInfoCollection diagnosticInfos)
        {
            // process results.
            int? nodeClass = null;

            for (var ii = 0; ii < itemsToRead.Count; ii++)
            {
                var attributeId = itemsToRead[ii].AttributeId;

                // the node probably does not exist if the node class is not found.
                if (attributeId == Attributes.NodeClass)
                {
                    if (!DataValue.IsGood(values[ii]))
                    {
                        throw ServiceResultException.Create(values[ii].StatusCode, ii, diagnosticInfos, responseHeader.StringTable);
                    }

                    // check for valid node class.
                    nodeClass = values[ii].Value as int?;

                    if (nodeClass == null)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Node does not have a valid value for NodeClass: {0}.", values[ii].Value);
                    }
                }
                else
                {
                    if (!DataValue.IsGood(values[ii]))
                    {
                        // check for unsupported attributes.
                        if (values[ii].StatusCode == StatusCodes.BadAttributeIdInvalid)
                        {
                            continue;
                        }

                        // ignore errors on optional attributes
                        if (StatusCode.IsBad(values[ii].StatusCode))
                        {
                            if (attributeId == Attributes.AccessRestrictions ||
                                attributeId == Attributes.Description ||
                                attributeId == Attributes.RolePermissions ||
                                attributeId == Attributes.UserRolePermissions ||
                                attributeId == Attributes.UserWriteMask ||
                                attributeId == Attributes.WriteMask ||
                                attributeId == Attributes.AccessLevelEx ||
                                attributeId == Attributes.ArrayDimensions ||
                                attributeId == Attributes.DataTypeDefinition ||
                                attributeId == Attributes.InverseName ||
                                attributeId == Attributes.MinimumSamplingInterval)
                            {
                                continue;
                            }
                        }

                        // all supported attributes must be readable.
                        if (attributeId != Attributes.Value)
                        {
                            throw ServiceResultException.Create(values[ii].StatusCode, ii, diagnosticInfos, responseHeader.StringTable);
                        }
                    }
                }

                attributes[attributeId] = values[ii];
            }

            Node node;
            DataValue value;
            switch ((NodeClass)nodeClass.Value)
            {
                default:
                    {
                        throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Node does not have a valid value for NodeClass: {0}.", nodeClass.Value);
                    }

                case NodeClass.Object:
                    {
                        var objectNode = new ObjectNode();

                        value = attributes[Attributes.EventNotifier];

                        if (value == null || value.Value is null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Object does not support the EventNotifier attribute.");
                        }

                        objectNode.EventNotifier = (byte)value.GetValue(typeof(byte));
                        node = objectNode;
                        break;
                    }

                case NodeClass.ObjectType:
                    {
                        var objectTypeNode = new ObjectTypeNode();

                        value = attributes[Attributes.IsAbstract];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "ObjectType does not support the IsAbstract attribute.");
                        }

                        objectTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));
                        node = objectTypeNode;
                        break;
                    }

                case NodeClass.Variable:
                    {
                        var variableNode = new VariableNode();

                        // DataType Attribute
                        value = attributes[Attributes.DataType];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Variable does not support the DataType attribute.");
                        }

                        variableNode.DataType = (NodeId)value.GetValue(typeof(NodeId));

                        // ValueRank Attribute
                        value = attributes[Attributes.ValueRank];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Variable does not support the ValueRank attribute.");
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
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Variable does not support the AccessLevel attribute.");
                        }

                        variableNode.AccessLevel = (byte)value.GetValue(typeof(byte));

                        // UserAccessLevel Attribute
                        value = attributes[Attributes.UserAccessLevel];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Variable does not support the UserAccessLevel attribute.");
                        }

                        variableNode.UserAccessLevel = (byte)value.GetValue(typeof(byte));

                        // Historizing Attribute
                        value = attributes[Attributes.Historizing];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Variable does not support the Historizing attribute.");
                        }

                        variableNode.Historizing = (bool)value.GetValue(typeof(bool));

                        // MinimumSamplingInterval Attribute
                        value = attributes[Attributes.MinimumSamplingInterval];

                        if (value != null)
                        {
                            variableNode.MinimumSamplingInterval = Convert.ToDouble(attributes[Attributes.MinimumSamplingInterval].Value, CultureInfo.InvariantCulture);
                        }

                        // AccessLevelEx Attribute
                        value = attributes[Attributes.AccessLevelEx];

                        if (value != null)
                        {
                            variableNode.AccessLevelEx = (uint)value.GetValue(typeof(uint));
                        }

                        node = variableNode;
                        break;
                    }

                case NodeClass.VariableType:
                    {
                        var variableTypeNode = new VariableTypeNode();

                        // IsAbstract Attribute
                        value = attributes[Attributes.IsAbstract];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "VariableType does not support the IsAbstract attribute.");
                        }

                        variableTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));

                        // DataType Attribute
                        value = attributes[Attributes.DataType];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "VariableType does not support the DataType attribute.");
                        }

                        variableTypeNode.DataType = (NodeId)value.GetValue(typeof(NodeId));

                        // ValueRank Attribute
                        value = attributes[Attributes.ValueRank];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "VariableType does not support the ValueRank attribute.");
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
                    }

                case NodeClass.Method:
                    {
                        var methodNode = new MethodNode();

                        // Executable Attribute
                        value = attributes[Attributes.Executable];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Method does not support the Executable attribute.");
                        }

                        methodNode.Executable = (bool)value.GetValue(typeof(bool));

                        // UserExecutable Attribute
                        value = attributes[Attributes.UserExecutable];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Method does not support the UserExecutable attribute.");
                        }

                        methodNode.UserExecutable = (bool)value.GetValue(typeof(bool));

                        node = methodNode;
                        break;
                    }

                case NodeClass.DataType:
                    {
                        var dataTypeNode = new DataTypeNode();

                        // IsAbstract Attribute
                        value = attributes[Attributes.IsAbstract];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "DataType does not support the IsAbstract attribute.");
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
                    }

                case NodeClass.ReferenceType:
                    {
                        var referenceTypeNode = new ReferenceTypeNode();

                        // IsAbstract Attribute
                        value = attributes[Attributes.IsAbstract];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "ReferenceType does not support the IsAbstract attribute.");
                        }

                        referenceTypeNode.IsAbstract = (bool)value.GetValue(typeof(bool));

                        // Symmetric Attribute
                        value = attributes[Attributes.Symmetric];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "ReferenceType does not support the Symmetric attribute.");
                        }

                        referenceTypeNode.Symmetric = (bool)value.GetValue(typeof(bool));

                        // InverseName Attribute
                        value = attributes[Attributes.InverseName];

                        if (value?.Value != null)
                        {
                            referenceTypeNode.InverseName = (LocalizedText)value.GetValue(typeof(LocalizedText));
                        }

                        node = referenceTypeNode;
                        break;
                    }

                case NodeClass.View:
                    {
                        var viewNode = new ViewNode();

                        // EventNotifier Attribute
                        value = attributes[Attributes.EventNotifier];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "View does not support the EventNotifier attribute.");
                        }

                        viewNode.EventNotifier = (byte)value.GetValue(typeof(byte));

                        // ContainsNoLoops Attribute
                        value = attributes[Attributes.ContainsNoLoops];

                        if (value == null)
                        {
                            throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "View does not support the ContainsNoLoops attribute.");
                        }

                        viewNode.ContainsNoLoops = (bool)value.GetValue(typeof(bool));

                        node = viewNode;
                        break;
                    }
            }

            // NodeId Attribute
            value = attributes[Attributes.NodeId];

            if (value == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Node does not support the NodeId attribute.");
            }

            node.NodeId = (NodeId)value.GetValue(typeof(NodeId));
            node.NodeClass = (NodeClass)nodeClass.Value;

            // BrowseName Attribute
            value = attributes[Attributes.BrowseName];

            if (value == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Node does not support the BrowseName attribute.");
            }

            node.BrowseName = (QualifiedName)value.GetValue(typeof(QualifiedName));

            // DisplayName Attribute
            value = attributes[Attributes.DisplayName];

            if (value == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadUnexpectedError, "Node does not support the DisplayName attribute.");
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
        private static SortedDictionary<uint, DataValue> CreateAttributes(NodeClass nodeclass = NodeClass.Unspecified, bool optionalAttributes = true)
        {
            // Attributes to read for all types of nodes
            var attributes = new SortedDictionary<uint, DataValue>() {
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
                    // build complete list of attributes.
                    attributes = new SortedDictionary<uint, DataValue> {
                        { Attributes.NodeId, null },
                        { Attributes.NodeClass, null },
                        { Attributes.BrowseName, null },
                        { Attributes.DisplayName, null },
                        //{ Attributes.Description, null },
                        //{ Attributes.WriteMask, null },
                        //{ Attributes.UserWriteMask, null },
                        { Attributes.DataType, null },
                        { Attributes.ValueRank, null },
                        { Attributes.ArrayDimensions, null },
                        { Attributes.AccessLevel, null },
                        { Attributes.UserAccessLevel, null },
                        { Attributes.MinimumSamplingInterval, null },
                        { Attributes.Historizing, null },
                        { Attributes.EventNotifier, null },
                        { Attributes.Executable, null },
                        { Attributes.UserExecutable, null },
                        { Attributes.IsAbstract, null },
                        { Attributes.InverseName, null },
                        { Attributes.Symmetric, null },
                        { Attributes.ContainsNoLoops, null },
                        { Attributes.DataTypeDefinition, null },
                        //{ Attributes.RolePermissions, null },
                        //{ Attributes.UserRolePermissions, null },
                        //{ Attributes.AccessRestrictions, null },
                        { Attributes.AccessLevelEx, null }
                    };
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
        /// Sends an additional publish request.
        /// </summary>
        /// <param name="timeout"></param>
        public IAsyncResult BeginPublish(int timeout)
        {
            // do not publish if reconnecting.
            if (_reconnecting)
            {
                _logger.LogWarning("Publish skipped due to reconnect");
                return null;
            }

            // get event handler to modify ack list
            var callback = _PublishSequenceNumbersToAcknowledge;

            // collect the current set if acknowledgements.
            SubscriptionAcknowledgementCollection acknowledgementsToSend = null;
            lock (_acknowledgementsToSendLock)
            {
                if (callback != null)
                {
                    try
                    {
                        var deferredAcknowledgementsToSend = new SubscriptionAcknowledgementCollection();
                        callback(this, new PublishSequenceNumbersToAcknowledgeEventArgs(_acknowledgementsToSend, deferredAcknowledgementsToSend));
                        acknowledgementsToSend = _acknowledgementsToSend;
                        _acknowledgementsToSend = deferredAcknowledgementsToSend;
                    }
                    catch (Exception e2)
                    {
                        _logger.LogError(e2, "Session: Unexpected error invoking PublishSequenceNumbersToAcknowledgeEventArgs.");
                    }
                }

                if (acknowledgementsToSend == null)
                {
                    // send all ack values, clear list
                    acknowledgementsToSend = _acknowledgementsToSend;
                    _acknowledgementsToSend = new SubscriptionAcknowledgementCollection();
                }
#if DEBUG_SEQUENTIALPUBLISHING
                foreach (var toSend in acknowledgementsToSend)
                {
                    _latestAcknowledgementsSent[toSend.SubscriptionId] = toSend.SequenceNumber;
                }
#endif
            }

            var timeoutHint = (uint)((timeout > 0) ? (uint)timeout : uint.MaxValue);
            timeoutHint = Math.Min((uint)(OperationTimeout / 2), timeoutHint);

            // send publish request.
            var requestHeader = new RequestHeader
            {
                // ensure the publish request is discarded before the timeout occurs to ensure the channel is dropped.
                TimeoutHint = timeoutHint,
                ReturnDiagnostics = (uint)(int)ReturnDiagnostics,
                RequestHandle = Utils.IncrementIdentifier(ref _publishCounter)
            };

            var state = new AsyncRequestState
            {
                RequestTypeId = DataTypes.PublishRequest,
                RequestId = requestHeader.RequestHandle,
                TickCount = HiResClock.TickCount
            };

            // CoreClientUtils.EventLog.PublishStart((int)requestHeader.RequestHandle);

            try
            {
                var result = BeginPublish(
                    requestHeader,
                    acknowledgementsToSend,
                    OnPublishComplete,
                    new object[] { SessionId, acknowledgementsToSend, requestHeader });

                AsyncRequestStarted(result, requestHeader.RequestHandle, DataTypes.PublishRequest);

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error sending publish request.");
                return null;
            }
        }

        /// <summary>
        /// Create the publish requests for the active subscriptions.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="fullQueue"></param>
        public void StartPublishing(int timeout, bool fullQueue)
        {
            var publishCount = GetDesiredPublishRequestCount(true);

            // refill pipeline. Send at least one publish request if subscriptions are active.
            if (publishCount > 0 && BeginPublish(timeout) != null)
            {
                var startCount = fullQueue ? 1 : GoodPublishRequestCount + 1;
                for (var ii = startCount; ii < publishCount; ii++)
                {
                    if (BeginPublish(timeout) == null)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Completes an asynchronous publish operation.
        /// </summary>
        /// <param name="result"></param>
        private void OnPublishComplete(IAsyncResult result)
        {
            // extract state information.
            var state = (object[])result.AsyncState;
            var sessionId = (NodeId)state[0];
            var acknowledgementsToSend = (SubscriptionAcknowledgementCollection)state[1];
            var requestHeader = (RequestHeader)state[2];
            uint subscriptionId = 0;

            AsyncRequestCompleted(result, requestHeader.RequestHandle, DataTypes.PublishRequest);

            // CoreClientUtils.EventLog.PublishStop((int)requestHeader.RequestHandle);

            try
            {
                // gate entry if transfer/reactivate is busy
                _reconnectLock.Wait();
                var reconnecting = _reconnecting;
                _reconnectLock.Release();

                // complete publish.

                var responseHeader = EndPublish(
                    result,
                    out subscriptionId,
                    out var availableSequenceNumbers,
                    out var moreNotifications,
                    out var notificationMessage,
                    out var acknowledgeResults,
                    out var acknowledgeDiagnosticInfos);

                var logLevel = LogLevel.Warning;
                foreach (var code in acknowledgeResults)
                {
                    if (StatusCode.IsBad(code) && code != StatusCodes.BadSequenceNumberUnknown)
                    {
                        _logger.Log(logLevel, "Publish Ack Response. ResultCode={ResultCode}; SubscriptionId={SubscriptionId}", code.ToString(), subscriptionId);
                        // only show the first error as warning
                        logLevel = LogLevel.Trace;
                    }
                }

                // nothing more to do if session changed.
                if (sessionId != SessionId)
                {
                    _logger.LogWarning("Publish response discarded because session id changed: Old {Old} != New {New}", sessionId, SessionId);
                    return;
                }

                // CoreClientUtils.EventLog.NotificationReceived((int)subscriptionId, (int)notificationMessage.SequenceNumber);

                // process response.
                ProcessPublishResponse(
                    responseHeader,
                    subscriptionId,
                    availableSequenceNumbers,
                    moreNotifications,
                    notificationMessage);

                // nothing more to do if reconnecting.
                if (reconnecting)
                {
                    _logger.LogWarning("No new publish sent because of reconnect in progress.");
                    return;
                }
            }
            catch (Exception e)
            {
                if (_subscriptions.Count == 0)
                {
                    // Publish responses with error should occur after deleting the last subscription.
                    _logger.LogError("Publish #{Handle}, Subscription count = 0, Error: {Message}", requestHeader.RequestHandle, e.Message);
                }
                else
                {
                    _logger.LogError("Publish #{Handle}, Reconnecting={Reconnecting}, Error: {Message}", requestHeader.RequestHandle, _reconnecting, e.Message);
                }

                // raise an error event.
                var error = new ServiceResult(e);

                if (error.Code != StatusCodes.BadNoSubscription)
                {
                    var callback = _PublishError;

                    if (callback != null)
                    {
                        try
                        {
                            callback(this, new PublishErrorEventArgs(error, subscriptionId, 0));
                        }
                        catch (Exception e2)
                        {
                            _logger.LogError(e2, "Session: Unexpected error invoking PublishErrorCallback.");
                        }
                    }
                }

                // ignore errors if reconnecting.
                if (_reconnecting)
                {
                    _logger.LogWarning("Publish abandoned after error due to reconnect: {Message}", e.Message);
                    return;
                }

                // nothing more to do if session changed.
                if (sessionId != SessionId)
                {
                    _logger.LogError("Publish abandoned after error because session id changed: Old {Old} != New {New}", sessionId, SessionId);
                    return;
                }

                // try to acknowledge the notifications again in the next publish.
                if (acknowledgementsToSend != null)
                {
                    lock (_acknowledgementsToSendLock)
                    {
                        _acknowledgementsToSend.AddRange(acknowledgementsToSend);
                    }
                }

                // don't send another publish for these errors,
                // or throttle to avoid server overload.
                switch (error.Code)
                {
                    case StatusCodes.BadTooManyPublishRequests:
                        var tooManyPublishRequests = GoodPublishRequestCount;
                        if (BelowPublishRequestLimit(tooManyPublishRequests))
                        {
                            _tooManyPublishRequests = tooManyPublishRequests;
                            _logger.LogInformation("PUBLISH - Too many requests, set limit to GoodPublishRequestCount={NewGood}.", _tooManyPublishRequests);
                        }
                        return;

                    case StatusCodes.BadNoSubscription:
                    case StatusCodes.BadSessionClosed:
                    case StatusCodes.BadSecurityChecksFailed:
                    case StatusCodes.BadCertificateInvalid:
                    case StatusCodes.BadServerHalted:
                        return;

                    // may require a reconnect or activate to recover
                    case StatusCodes.BadSessionIdInvalid:
                    case StatusCodes.BadSecureChannelIdInvalid:
                    case StatusCodes.BadSecureChannelClosed:
                        OnKeepAliveError(error);
                        return;

                    // Servers may return this error when overloaded
                    case StatusCodes.BadTooManyOperations:
                    case StatusCodes.BadTcpServerTooBusy:
                    case StatusCodes.BadServerTooBusy:
                        // throttle the next publish to reduce server load
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(100).ConfigureAwait(false);
                            QueueBeginPublish();
                        });
                        return;

                    case StatusCodes.BadTimeout:
                        break;

                    default:
                        _logger.LogError(e, "PUBLISH #{Handle} - Unhandled error {Status} during Publish.", requestHeader.RequestHandle, error.StatusCode);
                        goto case StatusCodes.BadServerTooBusy;

                }
            }

            QueueBeginPublish();
        }

        /// <summary>
        /// Queues a publish request if there are not enough outstanding requests.
        /// </summary>
        private void QueueBeginPublish()
        {
            var requestCount = GoodPublishRequestCount;

            var minPublishRequestCount = GetDesiredPublishRequestCount(false);

            if (requestCount < minPublishRequestCount)
            {
                BeginPublish(OperationTimeout);
            }
            else
            {
                _logger.LogDebug("PUBLISH - Did not send another publish request. GoodPublishRequestCount={Good}, MinPublishRequestCount={Min}", requestCount, minPublishRequestCount);
            }
        }

        /// <summary>
        /// Recreate the subscriptions in a reconnected session.
        /// Uses Transfer service if <see cref="TransferSubscriptionsOnReconnect"/> is set to <c>true</c>.
        /// </summary>
        /// <param name="subscriptionsTemplate">The template for the subscriptions.</param>
        /// <param name="ct"></param>
        private async Task RecreateSubscriptionsAsync(IEnumerable<Subscription> subscriptionsTemplate, CancellationToken ct)
        {
            var transferred = false;
            if (TransferSubscriptionsOnReconnect)
            {
                try
                {
                    transferred = await TransferSubscriptionsAsync(new SubscriptionCollection(subscriptionsTemplate), false, ct).ConfigureAwait(false);
                }
                catch (ServiceResultException sre)
                {
                    if (sre.StatusCode == StatusCodes.BadServiceUnsupported)
                    {
                        TransferSubscriptionsOnReconnect = false;
                        _logger.LogWarning("Transfer subscription unsupported, TransferSubscriptionsOnReconnect set to false.");
                    }
                    else
                    {
                        _logger.LogError(sre, "Transfer subscriptions failed.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected Transfer subscriptions error.");
                }
            }

            if (!transferred)
            {
                // Create the subscriptions which were not transferred.
                foreach (var subscription in Subscriptions)
                {
                    if (!subscription.Created)
                    {
                        await subscription.CreateAsync(ct).ConfigureAwait(false);
                    }
                }
            }
        }






        /// <summary>
        /// Validates  the identity for an open call.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="identityToken"></param>
        /// <param name="identityPolicy"></param>
        /// <param name="securityPolicyUri"></param>
        /// <param name="requireEncryption"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void OpenValidateIdentity(
            ref IUserIdentity identity,
            out UserIdentityToken identityToken,
            out UserTokenPolicy identityPolicy,
            out string securityPolicyUri,
            out bool requireEncryption)
        {
            // check connection state.
            lock (SyncRoot)
            {
                if (Connected)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidState, "Already connected to server.");
                }
            }

            securityPolicyUri = _endpoint.Description.SecurityPolicyUri;

            // catch security policies which are not supported by core
            if (SecurityPolicies.GetDisplayName(securityPolicyUri) == null)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadSecurityChecksFailed,
                    "The chosen security policy is not supported by the client to connect to the server.");
            }

            // get the identity token.
            identity ??= new UserIdentity();

            // get identity token.
            identityToken = identity.GetIdentityToken();

            // check that the user identity is supported by the endpoint.
            identityPolicy = _endpoint.Description.FindUserTokenPolicy(identityToken.PolicyId);

            if (identityPolicy == null)
            {
                // try looking up by TokenType if the policy id was not found.
                identityPolicy = _endpoint.Description.FindUserTokenPolicy(identity.TokenType, identity.IssuedTokenType);

                if (identityPolicy == null)
                {
                    throw ServiceResultException.Create(
                        StatusCodes.BadUserAccessDenied,
                        "Endpoint does not support the user identity type provided.");
                }

                identityToken.PolicyId = identityPolicy.PolicyId;
            }

            requireEncryption = securityPolicyUri != SecurityPolicies.None;

            if (!requireEncryption)
            {
                requireEncryption = identityPolicy.SecurityPolicyUri != SecurityPolicies.None &&
                    !string.IsNullOrEmpty(identityPolicy.SecurityPolicyUri);
            }
        }

        private void BuildCertificateData(out byte[] clientCertificateData, out byte[] clientCertificateChainData)
        {
            // send the application instance certificate for the client.
            clientCertificateData = (_instanceCertificate?.RawData);
            clientCertificateChainData = null;

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
        }

        /// <summary>
        /// Helper to throw a recreate session exception.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sessionName"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static void ThrowCouldNotRecreateSessionException(Exception e, string sessionName)
        {
            throw ServiceResultException.Create(StatusCodes.BadCommunicationError, e, "Could not recreate Session {Id}:{Message}", sessionName, e.Message);
        }

        /// <summary>
        /// Validates the server certificate returned.
        /// </summary>
        /// <param name="serverCertificateData"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidateServerCertificateData(byte[] serverCertificateData)
        {
            if (serverCertificateData != null &&
                _endpoint.Description.ServerCertificate != null &&
                !Utils.IsEqual(serverCertificateData, _endpoint.Description.ServerCertificate))
            {
                try
                {
                    // verify for certificate chain in endpoint.
                    var serverCertificateChain = Utils.ParseCertificateChainBlob(_endpoint.Description.ServerCertificate);

                    if (serverCertificateChain.Count > 0 && !Utils.IsEqual(serverCertificateData, serverCertificateChain[0].RawData))
                    {
                        throw ServiceResultException.Create(
                                    StatusCodes.BadCertificateInvalid,
                                    "Server did not return the certificate used to create the secure channel.");
                    }
                }
                catch (Exception)
                {
                    throw ServiceResultException.Create(
                            StatusCodes.BadCertificateInvalid,
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
        private void ValidateServerSignature(X509Certificate2 serverCertificate, SignatureData serverSignature,
            byte[] clientCertificateData, byte[] clientCertificateChainData, byte[] clientNonce)
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

            if (!SecurityPolicies.Verify(serverCertificate, _endpoint.Description.SecurityPolicyUri, dataToSign, serverSignature))
            {
                // validate the signature with complete chain if the check with leaf certificate failed.
                if (clientCertificateChainData != null)
                {
                    dataToSign = Utils.Append(clientCertificateChainData, clientNonce);

                    if (!SecurityPolicies.Verify(serverCertificate, _endpoint.Description.SecurityPolicyUri, dataToSign, serverSignature))
                    {
                        throw ServiceResultException.Create(
                            StatusCodes.BadApplicationSignatureInvalid,
                            "Server did not provide a correct signature for the nonce data provided by the client.");
                    }
                }
                else
                {
                    throw ServiceResultException.Create(
                       StatusCodes.BadApplicationSignatureInvalid,
                       "Server did not provide a correct signature for the nonce data provided by the client.");
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
                // Compare EndpointDescriptions returned at GetEndpoints with values returned at CreateSession
                EndpointDescriptionCollection expectedServerEndpoints = null;

                if (serverEndpoints != null &&
                    _discoveryProfileUris?.Count > 0)
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
                    throw ServiceResultException.Create(
                        StatusCodes.BadSecurityChecksFailed,
                        "Server did not return a number of ServerEndpoints that matches the one from GetEndpoints.");
                }

                for (var ii = 0; ii < expectedServerEndpoints.Count; ii++)
                {
                    var serverEndpoint = expectedServerEndpoints[ii];
                    var expectedServerEndpoint = _discoveryServerEndpoints[ii];

                    if (serverEndpoint.SecurityMode != expectedServerEndpoint.SecurityMode ||
                        serverEndpoint.SecurityPolicyUri != expectedServerEndpoint.SecurityPolicyUri ||
                        serverEndpoint.TransportProfileUri != expectedServerEndpoint.TransportProfileUri ||
                        serverEndpoint.SecurityLevel != expectedServerEndpoint.SecurityLevel)
                    {
                        throw ServiceResultException.Create(
                            StatusCodes.BadSecurityChecksFailed,
                            "The list of ServerEndpoints returned at CreateSession does not match the list from GetEndpoints.");
                    }

                    if (serverEndpoint.UserIdentityTokens.Count != expectedServerEndpoint.UserIdentityTokens.Count)
                    {
                        throw ServiceResultException.Create(
                            StatusCodes.BadSecurityChecksFailed,
                            "The list of ServerEndpoints returned at CreateSession does not match the one from GetEndpoints.");
                    }

                    for (var jj = 0; jj < serverEndpoint.UserIdentityTokens.Count; jj++)
                    {
                        if (!serverEndpoint.UserIdentityTokens[jj].IsEqual(expectedServerEndpoint.UserIdentityTokens[jj]))
                        {
                            throw ServiceResultException.Create(
                            StatusCodes.BadSecurityChecksFailed,
                            "The list of ServerEndpoints returned at CreateSession does not match the one from GetEndpoints.");
                        }
                    }
                }
            }

            // find the matching description (TBD - check domains against certificate).
            var found = false;

            var foundDescription = FindMatchingDescription(serverEndpoints, _endpoint.Description, true);
            if (foundDescription != null)
            {
                found = true;
                // ensure endpoint has up to date information.
                UpdateDescription(_endpoint.Description, foundDescription);
            }
            else
            {
                foundDescription = FindMatchingDescription(serverEndpoints, _endpoint.Description, false);
                if (foundDescription != null)
                {
                    found = true;
                    // ensure endpoint has up to date information.
                    UpdateDescription(_endpoint.Description, foundDescription);
                }
            }

            // could be a security risk.
            if (!found)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadSecurityChecksFailed,
                    "Server did not return an EndpointDescription that matched the one used to create the secure channel.");
            }
        }

        /// <summary>
        /// Find and return matching application description
        /// </summary>
        /// <param name="endpointDescriptions">The descriptions to search through</param>
        /// <param name="match">The description to match</param>
        /// <param name="matchPort">Match criteria includes port</param>
        /// <returns>Matching description or null if no description is matching</returns>
        private EndpointDescription FindMatchingDescription(EndpointDescriptionCollection endpointDescriptions,
            EndpointDescription match,
            bool matchPort)
        {
            var expectedUrl = Utils.ParseUri(match.EndpointUrl);
            for (var ii = 0; ii < endpointDescriptions.Count; ii++)
            {
                var serverEndpoint = endpointDescriptions[ii];
                var actualUrl = Utils.ParseUri(serverEndpoint.EndpointUrl);

                if (actualUrl != null &&
                    actualUrl.Scheme == expectedUrl.Scheme &&
                    (!matchPort || actualUrl.Port == expectedUrl.Port) &&
                    serverEndpoint.SecurityPolicyUri == _endpoint.Description.SecurityPolicyUri &&
                    serverEndpoint.SecurityMode == _endpoint.Description.SecurityMode)
                {
                    return serverEndpoint;
                }
            }

            return null;
        }

        /// <summary>
        /// Update the target description from the source description
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        private static void UpdateDescription(EndpointDescription target, EndpointDescription source)
        {
            target.Server.ApplicationName = source.Server.ApplicationName;
            target.Server.ApplicationUri = source.Server.ApplicationUri;
            target.Server.ApplicationType = source.Server.ApplicationType;
            target.Server.ProductUri = source.Server.ProductUri;
            target.TransportProfileUri = source.TransportProfileUri;
            target.UserIdentityTokens = source.UserIdentityTokens;
        }

        /// <summary>
        /// Helper to prepare the reconnect channel
        /// and signature data before activate.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transportChannel"></param>
        /// <exception cref="ServiceResultException"></exception>
        private IAsyncResult PrepareReconnectBeginActivate(
            ITransportWaitingConnection connection,
            ITransportChannel transportChannel
            )
        {
            _logger.LogInformation("Session RECONNECT {Session} starting.", SessionId);

            // create the client signature.
            var dataToSign = Utils.Append(_serverCertificate?.RawData, _serverNonce);
            var endpoint = _endpoint.Description;
            var clientSignature = SecurityPolicies.Sign(_instanceCertificate, endpoint.SecurityPolicyUri, dataToSign);

            var identityPolicy = _endpoint.Description.FindUserTokenPolicy(_identity.PolicyId);

            if (identityPolicy == null)
            {
                _logger.LogError("Reconnect: Endpoint does not support the user identity type provided.");

                throw ServiceResultException.Create(
                    StatusCodes.BadUserAccessDenied,
                    "Endpoint does not support the user identity type provided.");
            }

            // select the security policy for the user token.
            var securityPolicyUri = identityPolicy.SecurityPolicyUri;

            if (string.IsNullOrEmpty(securityPolicyUri))
            {
                securityPolicyUri = endpoint.SecurityPolicyUri;
            }

            // need to refresh the identity (reprompt for password, refresh token).
            if (_RenewUserIdentity != null)
            {
                _identity = _RenewUserIdentity(this, _identity);
            }

            // validate server nonce and security parameters for user identity.
            ValidateServerNonce(
                _identity,
                _serverNonce,
                securityPolicyUri,
                _previousServerNonce,
                _endpoint.Description.SecurityMode);

            // sign data with user token.
            var identityToken = _identity.GetIdentityToken();
            identityToken.PolicyId = identityPolicy.PolicyId;
            var userTokenSignature = identityToken.Sign(dataToSign, securityPolicyUri);

            // encrypt token.
            identityToken.Encrypt(_serverCertificate, _serverNonce, securityPolicyUri);

            // send the software certificates assigned to the client.
            var clientSoftwareCertificates = GetSoftwareCertificates();

            _logger.LogInformation("Session REPLACING channel for {Session}.", SessionId);

            if (connection != null)
            {
                var channel = NullableTransportChannel;

                // check if the channel supports reconnect.
                if (channel != null && (channel.SupportedFeatures & TransportChannelFeatures.Reconnect) != 0)
                {
                    channel.Reconnect(connection);
                }
                else
                {
                    // initialize the channel which will be created with the server.
                    channel = SessionChannel.Create(
                        _configuration,
                        connection,
                        _endpoint.Description,
                        _endpoint.Configuration,
                        _instanceCertificate,
                        _configuration.SecurityConfiguration.SendCertificateChain ? _instanceCertificateChain : null,
                        MessageContext);

                    // disposes the existing channel.
                    TransportChannel = channel;
                }
            }
            else if (transportChannel != null)
            {
                TransportChannel = transportChannel;
            }
            else
            {
                var channel = NullableTransportChannel;

                // check if the channel supports reconnect.
                if (channel != null && (channel.SupportedFeatures & TransportChannelFeatures.Reconnect) != 0)
                {
                    channel.Reconnect();
                }
                else
                {
                    // initialize the channel which will be created with the server.
                    channel = SessionChannel.Create(
                        _configuration,
                        _endpoint.Description,
                        _endpoint.Configuration,
                        _instanceCertificate,
                        _configuration.SecurityConfiguration.SendCertificateChain ? _instanceCertificateChain : null,
                        MessageContext);

                    // disposes the existing channel.
                    TransportChannel = channel;
                }
            }

            _logger.LogInformation("Session RE-ACTIVATING {Session}.", SessionId);

            var header = new RequestHeader() { TimeoutHint = kReconnectTimeout };
            return BeginActivateSession(
                header,
                clientSignature,
                null,
                _preferredLocales,
                new ExtensionObject(identityToken),
                userTokenSignature,
                null,
                null);
        }

        /// <summary>
        /// Process Republish error response.
        /// </summary>
        /// <param name="e">The exception that occurred during the republish operation.</param>
        /// <param name="subscriptionId">The subscription Id for which the republish was requested. </param>
        /// <param name="sequenceNumber">The sequencenumber for which the republish was requested.</param>
        private (bool, ServiceResult) ProcessRepublishResponseError(Exception e, uint subscriptionId, uint sequenceNumber)
        {
            var error = new ServiceResult(e);

            var result = true;
            switch (error.StatusCode.Code)
            {
                case StatusCodes.BadSubscriptionIdInvalid:
                case StatusCodes.BadMessageNotAvailable:
                    _logger.LogWarning("Message {SubscriptionId}-{SeqNumber} no longer available.", subscriptionId, sequenceNumber);
                    break;

                // if encoding limits are exceeded, the issue is logged and
                // the published data is acknowledged to prevent the endless republish loop.
                case StatusCodes.BadEncodingLimitsExceeded:
                    _logger.LogError(e, "Message {SubscriptionId}-{SeqNumber} exceeded size limits, ignored.", subscriptionId, sequenceNumber);
                    lock (_acknowledgementsToSendLock)
                    {
                        AddAcknowledgementToSend(_acknowledgementsToSend, subscriptionId, sequenceNumber);
                    }
                    break;

                default:
                    result = false;
                    _logger.LogError(e, "Unexpected error sending republish request.");
                    break;
            }

            var callback = _PublishError;

            // raise an error event.
            if (callback != null)
            {
                try
                {
                    var args = new PublishErrorEventArgs(
                        error,
                        subscriptionId,
                        sequenceNumber);

                    callback(this, args);
                }
                catch (Exception e2)
                {
                    _logger.LogError(e2, "Session: Unexpected error invoking PublishErrorCallback.");
                }
            }

            return (result, error);
        }

        /// <summary>
        /// If available, returns the current nonce or null.
        /// </summary>
        private byte[] GetCurrentTokenServerNonce()
        {
            var currentToken = NullableTransportChannel?.CurrentToken;
            return currentToken?.ServerNonce;
        }

        /// <summary>
        /// Handles the validation of server software certificates and application callback.
        /// </summary>
        /// <param name="serverSoftwareCertificates"></param>
        private void HandleSignedSoftwareCertificates(SignedSoftwareCertificateCollection serverSoftwareCertificates)
        {
            // get a validator to check certificates provided by server.
            var validator = _configuration.CertificateValidator;

            // validate software certificates.
            var softwareCertificates = new List<SoftwareCertificate>();

            foreach (var signedCertificate in serverSoftwareCertificates)
            {

                var result = SoftwareCertificate.Validate(
                    validator,
                    signedCertificate.CertificateData,
                    out var softwareCertificate);

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
        /// Processes the response from a publish request.
        /// </summary>
        /// <param name="responseHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="availableSequenceNumbers"></param>
        /// <param name="moreNotifications"></param>
        /// <param name="notificationMessage"></param>
        private void ProcessPublishResponse(
            ResponseHeader responseHeader,
            uint subscriptionId,
            UInt32Collection availableSequenceNumbers,
            bool moreNotifications,
            NotificationMessage notificationMessage)
        {
            Subscription subscription = null;

            // send notification that the server is alive.
            OnKeepAlive(_serverState, responseHeader.Timestamp);

            // collect the current set of acknowledgements.
            lock (_acknowledgementsToSendLock)
            {
                // clear out acknowledgements for messages that the server does not have any more.
                var acknowledgementsToSend = new SubscriptionAcknowledgementCollection();

                uint latestSequenceNumberToSend = 0;

                // create an acknowledgement to be sent back to the server.
                if (notificationMessage.NotificationData.Count > 0)
                {
                    AddAcknowledgementToSend(acknowledgementsToSend, subscriptionId, notificationMessage.SequenceNumber);
                    UpdateLatestSequenceNumberToSend(ref latestSequenceNumberToSend, notificationMessage.SequenceNumber);
                    _ = availableSequenceNumbers?.Remove(notificationMessage.SequenceNumber);
                }

                // match an acknowledgement to be sent back to the server.
                for (var ii = 0; ii < _acknowledgementsToSend.Count; ii++)
                {
                    var acknowledgement = _acknowledgementsToSend[ii];

                    if (acknowledgement.SubscriptionId != subscriptionId)
                    {
                        acknowledgementsToSend.Add(acknowledgement);
                    }
                    else if (availableSequenceNumbers?.Remove(acknowledgement.SequenceNumber) != false)
                    {
                        acknowledgementsToSend.Add(acknowledgement);
                        UpdateLatestSequenceNumberToSend(ref latestSequenceNumberToSend, acknowledgement.SequenceNumber);
                    }
                    // a publish response may by processed out of order,
                    // allow for a tolerance until the sequence number is removed.
                    else if (Math.Abs((int)(acknowledgement.SequenceNumber - latestSequenceNumberToSend)) < kPublishRequestSequenceNumberOutOfOrderThreshold)
                    {
                        acknowledgementsToSend.Add(acknowledgement);
                    }
                    else
                    {
                        _logger.LogWarning("SessionId {Id}, SubscriptionId {SubscriptionId}, Sequence number={SeqNumber} was not received in the available sequence numbers.", SessionId, subscriptionId, acknowledgement.SequenceNumber);
                    }
                }

                // Check for outdated sequence numbers. May have been not acked due to a network glitch.
                if (latestSequenceNumberToSend != 0 && availableSequenceNumbers?.Count > 0)
                {
                    foreach (var sequenceNumber in availableSequenceNumbers)
                    {
                        if ((int)(latestSequenceNumberToSend - sequenceNumber) > kPublishRequestSequenceNumberOutdatedThreshold)
                        {
                            AddAcknowledgementToSend(acknowledgementsToSend, subscriptionId, sequenceNumber);
                            _logger.LogWarning("SessionId {Id}, SubscriptionId {SubscriptionId}, Sequence number={SeqNumber} was outdated, acknowledged.", SessionId, subscriptionId, sequenceNumber);
                        }
                    }
                }

#if DEBUG_SEQUENTIALPUBLISHING
                // Checks for debug info only.
                // Once more than a single publish request is queued, the checks are invalid
                // because a publish response may not include the latest ack information yet.

                uint lastSentSequenceNumber = 0;
                if (availableSequenceNumbers != null)
                {
                    foreach (uint availableSequenceNumber in availableSequenceNumbers)
                    {
                        if (_latestAcknowledgementsSent.ContainsKey(subscriptionId))
                        {
                            lastSentSequenceNumber = _latestAcknowledgementsSent[subscriptionId];
                            // If the last sent sequence number is uint.Max do not display the warning; the counter rolled over
                            // If the last sent sequence number is greater or equal to the available sequence number (returned by the publish),
                            // a warning must be logged.
                            if (((lastSentSequenceNumber >= availableSequenceNumber) && (lastSentSequenceNumber != uint.MaxValue)) ||
                                (lastSentSequenceNumber == availableSequenceNumber) && (lastSentSequenceNumber == uint.MaxValue))
                            {
                                _logger.LogWarning("Received sequence number which was already acknowledged={SeqNumber}", availableSequenceNumber);
                            }
                        }
                    }
                }

                if (_latestAcknowledgementsSent.ContainsKey(subscriptionId))
                {
                    lastSentSequenceNumber = _latestAcknowledgementsSent[subscriptionId];

                    // If the last sent sequence number is uint.Max do not display the warning; the counter rolled over
                    // If the last sent sequence number is greater or equal to the notificationMessage's sequence number (returned by the publish),
                    // a warning must be logged.
                    if (((lastSentSequenceNumber >= notificationMessage.SequenceNumber) && (lastSentSequenceNumber != uint.MaxValue)) || (lastSentSequenceNumber == notificationMessage.SequenceNumber) && (lastSentSequenceNumber == uint.MaxValue))
                    {
                        _logger.LogWarning("Received sequence number which was already acknowledged={SeqNumber}", notificationMessage.SequenceNumber);
                    }
                }
#endif

                _acknowledgementsToSend = acknowledgementsToSend;

                if (notificationMessage.IsEmpty)
                {
                    _logger.LogTrace("Empty notification message received for SessionId {Id} with PublishTime {PublishTime}", SessionId, notificationMessage.PublishTime.ToLocalTime());
                }
            }

            lock (SyncRoot)
            {
                // find the subscription.
                foreach (var current in _subscriptions)
                {
                    if (current.Id == subscriptionId)
                    {
                        subscription = current;
                        break;
                    }
                }
            }

            // ignore messages with a subscription that has been deleted.
            if (subscription != null)
            {
                // Validate publish time and reject old values.
                if (notificationMessage.PublishTime.AddMilliseconds(subscription.CurrentPublishingInterval * subscription.CurrentLifetimeCount) < DateTime.UtcNow)
                {
                    _logger.LogTrace("PublishTime {PublishTime} in publish response is too old for SubscriptionId {SubscriptionId}.", notificationMessage.PublishTime.ToLocalTime(), subscription.Id);
                }

                // Validate publish time and reject old values.
                if (notificationMessage.PublishTime > DateTime.UtcNow.AddMilliseconds(subscription.CurrentPublishingInterval * subscription.CurrentLifetimeCount))
                {
                    _logger.LogTrace("PublishTime {PublishTime} in publish response is newer than actual time for SubscriptionId {SubscriptionId}.", notificationMessage.PublishTime.ToLocalTime(), subscription.Id);
                }

                // update subscription cache.
                subscription.SaveMessageInCache(
                    availableSequenceNumbers,
                    notificationMessage,
                    responseHeader.StringTable);

                // raise the notification.
                var publishEventHandler = _Publish;
                if (publishEventHandler != null)
                {
                    var args = new NotificationEventArgs(subscription, notificationMessage, responseHeader.StringTable);

                    Task.Run(() => OnRaisePublishNotification(publishEventHandler, args));
                }
            }
            else
            {
                if (DeleteSubscriptionsOnClose && !_reconnecting)
                {
                    // Delete abandoned subscription from server.
                    _logger.LogWarning("Received Publish Response for Unknown SubscriptionId={SubscriptionId}. Deleting abandoned subscription from server.", subscriptionId);

                    Task.Run(() => DeleteSubscriptionAsync(subscriptionId, default));
                }
                else
                {
                    // Do not delete publish requests of stale subscriptions
                    _logger.LogWarning("Received Publish Response for Unknown SubscriptionId={SubscriptionId}. Ignored.", subscriptionId);
                }
            }
        }

        /// <summary>
        /// Raises an event indicating that publish has returned a notification.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="args"></param>
        private void OnRaisePublishNotification(NotificationEventHandler callback, NotificationEventArgs args)
        {
            try
            {
                if (callback != null && args.Subscription.Id != 0)
                {
                    callback(this, args);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Session: Unexpected error while raising Notification event.");
            }
        }

        /// <summary>
        /// Invokes a DeleteSubscriptions call for the specified subscriptionId.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        private async Task DeleteSubscriptionAsync(uint subscriptionId, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Deleting server subscription for SubscriptionId={SubscriptionId}", subscriptionId);

                // delete the subscription.
                UInt32Collection subscriptionIds = new uint[] { subscriptionId };

                var response = await DeleteSubscriptionsAsync(null, subscriptionIds,
                    ct).ConfigureAwait(false);

                var results = response.Results;
                var diagnosticInfos = response.DiagnosticInfos;

                // validate response.
                ClientBase.ValidateResponse(results, subscriptionIds);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

                if (StatusCode.IsBad(results[0]))
                {
                    throw new ServiceResultException(ClientBase.GetResult(results[0], 0, diagnosticInfos, response.ResponseHeader));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "Session: Unexpected error while deleting subscription for SubscriptionId={SubscriptionId}.", subscriptionId);
            }
        }

        private void AddAcknowledgementToSend(SubscriptionAcknowledgementCollection acknowledgementsToSend, uint subscriptionId, uint sequenceNumber)
        {
            ArgumentNullException.ThrowIfNull(acknowledgementsToSend);

            Debug.Assert(Monitor.IsEntered(_acknowledgementsToSendLock));

            var acknowledgement = new SubscriptionAcknowledgement
            {
                SubscriptionId = subscriptionId,
                SequenceNumber = sequenceNumber
            };

            acknowledgementsToSend.Add(acknowledgement);
        }

        /// <summary>
        /// Returns true if the Bad_TooManyPublishRequests limit
        /// has not been reached.
        /// </summary>
        /// <param name="requestCount">The actual number of publish requests.</param>
        /// <returns>If the publish request limit was reached.</returns>
        private bool BelowPublishRequestLimit(int requestCount)
        {
            return (_tooManyPublishRequests == 0) ||
                (requestCount < _tooManyPublishRequests);
        }

        /// <summary>
        /// Returns the desired number of active publish request that should be used.
        /// </summary>
        /// <remarks>
        /// Returns 0 if there are no subscriptions.
        /// </remarks>
        /// <param name="createdOnly">False if call when re-queuing.</param>
        /// <returns>The number of desired publish requests for the session.</returns>
        protected virtual int GetDesiredPublishRequestCount(bool createdOnly)
        {
            lock (SyncRoot)
            {
                if (_subscriptions.Count == 0)
                {
                    return 0;
                }

                int publishCount;

                if (createdOnly)
                {
                    var count = 0;
                    foreach (var subscription in _subscriptions)
                    {
                        if (subscription.Created)
                        {
                            count++;
                        }
                    }

                    if (count == 0)
                    {
                        return 0;
                    }
                    publishCount = count;
                }
                else
                {
                    publishCount = _subscriptions.Count;
                }

                //
                // If a dynamic limit was set because of badTooManyPublishRequest error.
                // limit the number of publish requests to this value.
                //
                if (_tooManyPublishRequests > 0 && publishCount > _tooManyPublishRequests)
                {
                    publishCount = _tooManyPublishRequests;
                }

                //
                // Limit resulting to a number between min and max request count.
                // If max is below min, we honor the min publish request count.
                // See return from MinPublishRequestCount property which the max of both.
                //
                if (publishCount > _maxPublishRequestCount)
                {
                    publishCount = _maxPublishRequestCount;
                }
                if (publishCount < _minPublishRequestCount)
                {
                    publishCount = _minPublishRequestCount;
                }
                return publishCount;
            }
        }

        /// <summary>
        /// Creates and validates the subscription ids for a transfer.
        /// </summary>
        /// <param name="subscriptions">The subscriptions to transfer.</param>
        /// <returns>The subscription ids for the transfer.</returns>
        /// <exception cref="ServiceResultException">Thrown if a subscription is in invalid state.</exception>
        private UInt32Collection CreateSubscriptionIdsForTransfer(SubscriptionCollection subscriptions)
        {
            var subscriptionIds = new UInt32Collection();
            lock (SyncRoot)
            {
                foreach (var subscription in subscriptions)
                {
                    if (subscription.Created && SessionId.Equals(subscription.Session.SessionId))
                    {
                        throw new ServiceResultException(StatusCodes.BadInvalidState, Utils.Format("The SubscriptionId {Id} is already created.", subscription.Id));
                    }
                    if (subscription.TransferId == 0)
                    {
                        throw new ServiceResultException(StatusCodes.BadInvalidState, Utils.Format("A subscription can not be transferred due to missing transfer Id."));
                    }
                    subscriptionIds.Add(subscription.TransferId);
                }
            }
            return subscriptionIds;
        }

        /// <summary>
        /// Indicates that the session configuration has changed.
        /// </summary>
        private void IndicateSessionConfigurationChanged()
        {
            try
            {
                _SessionConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error calling SessionConfigurationChanged event handler.");
            }
        }

        /// <summary>
        /// Helper to update the latest sequence number to send.
        /// Handles wrap around of sequence numbers.
        /// </summary>
        /// <param name="latestSequenceNumberToSend"></param>
        /// <param name="sequenceNumber"></param>
        private static void UpdateLatestSequenceNumberToSend(ref uint latestSequenceNumberToSend, uint sequenceNumber)
        {
            // Handle wrap around with subtraction and test result is int.
            // Assume sequence numbers to ack do not differ by more than uint.Max / 2
            if (latestSequenceNumberToSend == 0 || ((int)(sequenceNumber - latestSequenceNumberToSend)) > 0)
            {
                latestSequenceNumberToSend = sequenceNumber;
            }
        }

        /// <summary>
        /// Check if all required configuration fields are populated.
        /// </summary>
        /// <param name="configuration"></param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ServiceResultException"></exception>
        private static void ValidateClientConfiguration(
            ApplicationConfiguration configuration)
        {
            string configurationField;
            ArgumentNullException.ThrowIfNull(configuration);
            if (configuration.ClientConfiguration == null)
            {
                configurationField = "ClientConfiguration";
            }
            else if (configuration.SecurityConfiguration == null)
            {
                configurationField = "SecurityConfiguration";
            }
            else if (configuration.CertificateValidator == null)
            {
                configurationField = "CertificateValidator";
            }
            else
            {
                return;
            }
            throw new ServiceResultException(
                StatusCodes.BadConfigurationError,
                $"The client configuration does not specify the {configurationField}.");
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
        /// The period for which the server will maintain the session if there is no communication from the client.
        /// </summary>
        protected double _sessionTimeout;

        /// <summary>
        /// The locales that the server should use when returning localized text.
        /// </summary>
        protected StringCollection _preferredLocales;

        /// <summary>
        /// The Application Configuration.
        /// </summary>
        protected ApplicationConfiguration _configuration;

        /// <summary>
        /// The endpoint used to connect to the server.
        /// </summary>
        protected ConfiguredEndpoint _endpoint;

        /// <summary>
        /// The Instance Certificate.
        /// </summary>
        protected X509Certificate2 _instanceCertificate;

        /// <summary>
        /// The Instance Certificate Chain.
        /// </summary>
        protected X509Certificate2Collection _instanceCertificateChain;

        /// <summary>
        /// If set to<c>true</c> then the domain in the certificate must match the endpoint used.
        /// </summary>
        protected bool _checkDomain;

        /// <summary>
        /// The name assigned to the session.
        /// </summary>
        protected string _sessionName;

        /// <summary>
        /// The user identity currently used for the session.
        /// </summary>
        protected IUserIdentity _identity;
        private SubscriptionAcknowledgementCollection _acknowledgementsToSend;
        private object _acknowledgementsToSendLock;
#if DEBUG_SEQUENTIALPUBLISHING
        private Dictionary<uint, uint> _latestAcknowledgementsSent;
#endif
        private List<Subscription> _subscriptions;
        private uint _maxRequestMessageSize;
        private StringTable _serverUris;
        private SystemContext _systemContext;
        private NodeCache _nodeCache;
        private byte[] _serverNonce;
        private byte[] _previousServerNonce;
        private X509Certificate2 _serverCertificate;
        private long _publishCounter;
        private int _tooManyPublishRequests;
        private long _lastKeepAliveTime;
        private StatusCode _lastKeepAliveErrorStatusCode;
        private ServerState _serverState;
        private int _keepAliveInterval;
#if PERIODIC_TIMER
        private PeriodicTimer _keepAliveTimer;
#else
        private Timer _keepAliveTimer;
#endif
        private long _keepAliveCounter;
        private bool _reconnecting;
        private SemaphoreSlim _reconnectLock;
        private int _minPublishRequestCount;
        private int _maxPublishRequestCount;
        private LinkedList<AsyncRequestState> _outstandingRequests;
        private readonly EndpointDescriptionCollection _discoveryServerEndpoints;
        private readonly StringCollection _discoveryProfileUris;
        private readonly ILogger _logger;

        private class AsyncRequestState
        {
            public uint RequestTypeId;
            public uint RequestId;
            public int TickCount;
            public IAsyncResult Result;
            public bool Defunct;
        }

        private event KeepAliveEventHandler _KeepAlive;
        private event NotificationEventHandler _Publish;
        private event PublishErrorEventHandler _PublishError;
        private event RenewUserIdentityEventHandler _RenewUserIdentity;
        private event PublishSequenceNumbersToAcknowledgeEventHandler _PublishSequenceNumbersToAcknowledge;
        private event EventHandler _SubscriptionsChanged;
        private event EventHandler _SessionClosing;
        private event EventHandler _SessionConfigurationChanged;

        private const int kReconnectTimeout = 15000;
        private const int kMinPublishRequestCountMax = 100;
        private const int kMaxPublishRequestCountMax = ushort.MaxValue;
        private const int kDefaultPublishRequestCount = 1;
        private const int kKeepAliveGuardBand = 1000;
        private const int kPublishRequestSequenceNumberOutOfOrderThreshold = 10;
        private const int kPublishRequestSequenceNumberOutdatedThreshold = 100;
    }
}
