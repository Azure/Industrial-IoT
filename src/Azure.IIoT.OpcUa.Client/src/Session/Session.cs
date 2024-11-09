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
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Bindings;
    using Microsoft.Extensions.Logging;
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
    public class Session : SessionBase, ISession, ISessionInternal, IComplexTypeContext, INodeCacheContext
    {
        private const int kReconnectTimeout = 15000;
        private const int kMinPublishRequestCountMax = 100;
        private const int kMaxPublishRequestCountMax = ushort.MaxValue;
        private const int kDefaultPublishRequestCount = 1;
        private const int kKeepAliveGuardBand = 1000;
        private const int kPublishRequestSequenceNumberOutOfOrderThreshold = 10;
        private const int kPublishRequestSequenceNumberOutdatedThreshold = 100;

        /// <summary>
        /// Constructs a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="channel">The channel used to communicate with the server.</param>
        /// <param name="configuration">The configuration for the client application.</param>
        /// <param name="endpoint">The endpoint use to initialize the channel.</param>
        /// <param name="logger"></param>
        public Session(
            ISessionChannel channel,
            ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint,
            ILoggerFactory logger)
        :
            this(channel as ITransportChannel, configuration, endpoint, null, logger)
        {
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="ISession"/> class.
        /// </summary>
        /// <param name="channel">The channel used to communicate with the server.</param>
        /// <param name="configuration">The configuration for the client application.</param>
        /// <param name="endpoint">The endpoint used to initialize the channel.</param>
        /// <param name="clientCertificate">The certificate to use for the client.</param>
        /// <param name="logger"></param>
        /// <param name="availableEndpoints">The list of available endpoints returned by server in GetEndpoints() response.</param>
        /// <param name="discoveryProfileUris">The value of profileUris used in GetEndpoints() request.</param>
        /// <remarks>
        /// The application configuration is used to look up the certificate if none is provided.
        /// The clientCertificate must have the private key. This will require that the certificate
        /// be loaded from a certicate store. Converting a DER encoded blob to a X509Certificate2
        /// will not include a private key.
        /// The <i>availableEndpoints</i> and <i>discoveryProfileUris</i> parameters are used to validate
        /// that the list of EndpointDescriptions returned at GetEndpoints matches the list returned at CreateSession.
        /// </remarks>
        public Session(
            ITransportChannel channel,
            ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint,
            X509Certificate2 clientCertificate,
            ILoggerFactory logger,
            EndpointDescriptionCollection availableEndpoints = null,
            StringCollection discoveryProfileUris = null)
            :
                base(channel)
        {
            LoggerFactory = logger;
            m_logger = LoggerFactory.CreateLogger<Session>();
            Initialize(channel, configuration, endpoint, clientCertificate);
            m_discoveryServerEndpoints = availableEndpoints;
            m_discoveryProfileUris = discoveryProfileUris;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ISession"/> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="template">The template session.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c> the event handlers are copied.</param>
        public Session(ITransportChannel channel, Session template, bool copyEventHandlers)
        :
            base(channel)
        {
            LoggerFactory = template.LoggerFactory;
            m_logger = LoggerFactory.CreateLogger<Session>();
            Initialize(channel, template.m_configuration, template.ConfiguredEndpoint, template.m_instanceCertificate);

            SessionFactory = template.SessionFactory;
            DeleteSubscriptionsOnClose = template.DeleteSubscriptionsOnClose;
            TransferSubscriptionsOnReconnect = template.TransferSubscriptionsOnReconnect;
            m_sessionTimeout = template.m_sessionTimeout;
            m_maxRequestMessageSize = template.m_maxRequestMessageSize;
            m_minPublishRequestCount = template.m_minPublishRequestCount;
            m_maxPublishRequestCount = template.m_maxPublishRequestCount;
            m_preferredLocales = template.PreferredLocales;
            m_sessionName = template.SessionName;
            Handle = template.Handle;
            m_identity = template.Identity;
            m_keepAliveInterval = template.KeepAliveInterval;
            m_checkDomain = template.m_checkDomain;
            ContinuationPointPolicy = template.ContinuationPointPolicy;
            if (template.OperationTimeout > 0)
            {
                OperationTimeout = template.OperationTimeout;
            }

            if (copyEventHandlers)
            {
                m_KeepAlive = template.m_KeepAlive;
                m_Publish = template.m_Publish;
                m_PublishError = template.m_PublishError;
                m_PublishSequenceNumbersToAcknowledge = template.m_PublishSequenceNumbersToAcknowledge;
                m_SubscriptionsChanged = template.m_SubscriptionsChanged;
                m_SessionClosing = template.m_SessionClosing;
                m_SessionConfigurationChanged = template.m_SessionConfigurationChanged;
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
            Initialize();

            ValidateClientConfiguration(configuration);

            // save configuration information.
            m_configuration = configuration;
            m_endpoint = endpoint;

            if (m_endpoint.Description.SecurityPolicyUri != SecurityPolicies.None)
            {
                // update client certificate.
                m_instanceCertificate = clientCertificate;

                if (clientCertificate == null)
                {
                    // load the application instance certificate.
                    if (m_configuration.SecurityConfiguration.ApplicationCertificate == null)
                    {
                        throw new ServiceResultException(
                            StatusCodes.BadConfigurationError,
                            "The client configuration does not specify an application instance certificate.");
                    }

                    m_instanceCertificate = m_configuration.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
                }

                // check for valid certificate.
                if (m_instanceCertificate == null)
                {
                    var cert = m_configuration.SecurityConfiguration.ApplicationCertificate;
                    throw ServiceResultException.Create(
                        StatusCodes.BadConfigurationError,
                        "Cannot find the application instance certificate. Store={0}, SubjectName={1}, Thumbprint={2}.",
                        cert.StorePath, cert.SubjectName, cert.Thumbprint);
                }

                // check for private key.
                if (!m_instanceCertificate.HasPrivateKey)
                {
                    throw ServiceResultException.Create(
                        StatusCodes.BadConfigurationError,
                        "No private key for the application instance certificate. Subject={0}, Thumbprint={1}.",
                        m_instanceCertificate.Subject,
                        m_instanceCertificate.Thumbprint);
                }

                // load certificate chain.
                m_instanceCertificateChain = new X509Certificate2Collection(m_instanceCertificate);
                var issuers = new List<CertificateIdentifier>();
                configuration.CertificateValidator.GetIssuers(m_instanceCertificate, issuers).Wait();

                for (var i = 0; i < issuers.Count; i++)
                {
                    m_instanceCertificateChain.Add(issuers[i].Certificate);
                }
            }

            // initialize the message context.
            var messageContext = channel.MessageContext;

            if (messageContext != null)
            {
                m_namespaceUris = messageContext.NamespaceUris;
                m_serverUris = messageContext.ServerUris;
                m_factory = messageContext.Factory;
            }
            else
            {
                m_namespaceUris = new NamespaceTable();
                m_serverUris = new StringTable();
                m_factory = new EncodeableFactory(EncodeableFactory.GlobalFactory);
            }

            // initialize the NodeCache late, it needs references to the namespaceUris
            m_nodeCache = new NodeCache(this);

            // set the default preferred locales.
            m_preferredLocales = new string[] { CultureInfo.CurrentCulture.Name };

            // create a context to use.
            m_systemContext = new SystemContext
            {
                SystemHandle = this,
                EncodeableFactory = m_factory,
                NamespaceUris = m_namespaceUris,
                ServerUris = m_serverUris,
                TypeTable = new Obsolete.TypeTree(m_nodeCache),
                PreferredLocales = null,
                SessionId = null,
                UserIdentity = null
            };
        }

        /// <summary>
        /// Sets the object members to default values.
        /// </summary>
        private void Initialize()
        {
            SessionFactory = new DefaultSessionFactory(LoggerFactory);
            m_sessionTimeout = 0;
            m_namespaceUris = new NamespaceTable();
            m_serverUris = new StringTable();
            m_factory = EncodeableFactory.GlobalFactory;
            m_configuration = null;
            m_instanceCertificate = null;
            m_endpoint = null;
            m_subscriptions = new List<Subscription>();
            m_acknowledgementsToSend = new SubscriptionAcknowledgementCollection();
            m_acknowledgementsToSendLock = new object();
#if DEBUG_SEQUENTIALPUBLISHING
            m_latestAcknowledgementsSent = new Dictionary<uint, uint>();
#endif
            m_outstandingRequests = new LinkedList<AsyncRequestState>();
            m_keepAliveInterval = 5000;
            m_tooManyPublishRequests = 0;
            m_minPublishRequestCount = kDefaultPublishRequestCount;
            m_maxPublishRequestCount = kMaxPublishRequestCountMax;
            m_sessionName = "";
            DeleteSubscriptionsOnClose = true;
            TransferSubscriptionsOnReconnect = false;
            m_reconnecting = false;
            m_reconnectLock = new SemaphoreSlim(1, 1);
            ServerMaxContinuationPointsPerBrowse = 0;
        }

        /// <summary>
        /// Check if all required configuration fields are populated.
        /// </summary>
        /// <param name="configuration"></param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <c>null</c>.</exception>
        /// <exception cref="ServiceResultException"></exception>
        private static void ValidateClientConfiguration(ApplicationConfiguration configuration)
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
        private void ValidateServerNonce(
            IUserIdentity identity,
            byte[] serverNonce,
            string securityPolicyUri,
            byte[] previousServerNonce,
            MessageSecurityMode channelSecurityMode = MessageSecurityMode.None)
        {
            // skip validation if server nonce is not used for encryption.
            if (string.IsNullOrEmpty(securityPolicyUri) || securityPolicyUri == SecurityPolicies.None)
            {
                return;
            }

            if (identity != null && identity.TokenType != UserTokenType.Anonymous)
            {
                // the server nonce should be validated if the token includes a secret.
                if (!Utils.Nonce.ValidateNonce(serverNonce, MessageSecurityMode.SignAndEncrypt, (uint)m_configuration.SecurityConfiguration.NonceLength))
                {
                    if (channelSecurityMode == MessageSecurityMode.SignAndEncrypt ||
                        m_configuration.SecurityConfiguration.SuppressNonceValidationErrors)
                    {
                        m_logger.LogWarning("The server nonce has not the correct length or is not random enough. The error is suppressed by user setting or because the channel is encrypted.");
                    }
                    else
                    {
                        throw ServiceResultException.Create(StatusCodes.BadNonceInvalid, "The server nonce has not the correct length or is not random enough.");
                    }
                }

                // check that new nonce is different from the previously returned server nonce.
                if (previousServerNonce != null && Utils.CompareNonce(serverNonce, previousServerNonce))
                {
                    if (channelSecurityMode == MessageSecurityMode.SignAndEncrypt ||
                        m_configuration.SecurityConfiguration.SuppressNonceValidationErrors)
                    {
                        m_logger.LogWarning("The Server nonce is equal with previously returned nonce. The error is suppressed by user setting or because the channel is encrypted.");
                    }
                    else
                    {
                        throw ServiceResultException.Create(StatusCodes.BadNonceInvalid, "Server nonce is equal with previously returned nonce.");
                    }
                }
            }
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
                m_keepAliveTimer?.Dispose();
                m_keepAliveTimer = null;

                m_nodeCache?.Clear();
                m_nodeCache = null;

                List<Subscription> subscriptions = null;
                lock (SyncRoot)
                {
                    subscriptions = new List<Subscription>(m_subscriptions);
                    m_subscriptions.Clear();
                }

                foreach (var subscription in subscriptions)
                {
                    subscription.Dispose();
                }
                subscriptions.Clear();

                m_reconnectLock.Dispose();
            }

            base.Dispose(disposing);

            if (disposing)
            {
                // suppress spurious events
                m_KeepAlive = null;
                m_Publish = null;
                m_PublishError = null;
                m_PublishSequenceNumbersToAcknowledge = null;
                m_SubscriptionsChanged = null;
                m_SessionClosing = null;
                m_SessionConfigurationChanged = null;
            }
        }

        /// <summary>
        /// Raised when a keep alive arrives from the server or an error is detected.
        /// </summary>
        /// <remarks>
        /// Once a session is created a timer will periodically read the server state and current time.
        /// If this read operation succeeds this event will be raised each time the keep alive period elapses.
        /// If an error is detected (KeepAliveStopped == true) then this event will be raised as well.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event KeepAliveEventHandler KeepAlive
        {
            add
            {
                m_KeepAlive += value;
            }

            remove
            {
                m_KeepAlive -= value;
            }
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public event PublishErrorEventHandler PublishError
        {
            add
            {
                m_PublishError += value;
            }

            remove
            {
                m_PublishError -= value;
            }
        }

        /// <inheritdoc/>
        public event PublishSequenceNumbersToAcknowledgeEventHandler PublishSequenceNumbersToAcknowledge
        {
            add
            {
                m_PublishSequenceNumbersToAcknowledge += value;
            }

            remove
            {
                m_PublishSequenceNumbersToAcknowledge -= value;
            }
        }

        /// <inheritdoc/>
        public event EventHandler SessionConfigurationChanged
        {
            add
            {
                m_SessionConfigurationChanged += value;
            }

            remove
            {
                m_SessionConfigurationChanged -= value;
            }
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
        public ConfiguredEndpoint ConfiguredEndpoint => m_endpoint;

        /// <summary>
        /// Gets the name assigned to the session.
        /// </summary>
        public string SessionName => m_sessionName;

        /// <summary>
        /// Gets the period for wich the server will maintain the session if there is no communication from the client.
        /// </summary>
        public double SessionTimeout => m_sessionTimeout;

        /// <summary>
        /// Gets the local handle assigned to the session.
        /// </summary>
        public object Handle { get; set; }

        /// <summary>
        /// Gets the user identity currently used for the session.
        /// </summary>
        public IUserIdentity Identity => m_identity;

        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        public NamespaceTable NamespaceUris => m_namespaceUris;

        /// <summary>
        /// Gets the system context for use with the session.
        /// </summary>
        public ISystemContext SystemContext => m_systemContext;

        /// <summary>
        /// Gets the factory used to create encodeable objects that the server understands.
        /// </summary>
        public IEncodeableFactory Factory => m_factory;

        /// <summary>
        /// Gets the cache of nodes fetched from the server.
        /// </summary>
        public INodeCache NodeCache => m_nodeCache;

        /// <summary>
        /// Gets the locales that the server should use when returning localized text.
        /// </summary>
        public StringCollection PreferredLocales => m_preferredLocales;

        /// <summary>
        /// Gets the subscriptions owned by the session.
        /// </summary>
        public IEnumerable<Subscription> Subscriptions
        {
            get
            {
                lock (SyncRoot)
                {
                    return new ReadOnlyList<Subscription>(m_subscriptions);
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
                    return m_subscriptions.Count;
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
            get
            {
                return m_keepAliveInterval;
            }

            set
            {
                m_keepAliveInterval = value;
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
                var lastKeepAliveErrorStatusCode = m_lastKeepAliveErrorStatusCode;
                if (StatusCode.IsGood(lastKeepAliveErrorStatusCode) || lastKeepAliveErrorStatusCode == StatusCodes.BadNoCommunication)
                {
                    var delta = HiResClock.TickCount - m_lastKeepAliveTickCount;

                    // add a guard band to allow for network lag.
                    return (m_keepAliveInterval + kKeepAliveGuardBand) <= delta;
                }

                // another error was reported which caused keep alive to stop.
                return true;
            }
        }

        /// <summary>
        /// Gets the TickCount in ms of the last keep alive based on <see cref="HiResClock.TickCount"/>.
        /// Independent of system time changes.
        /// </summary>
        public int LastKeepAliveTickCount
        {
            get
            {
                return m_lastKeepAliveTickCount;
            }
        }

        /// <summary>
        /// Gets the number of outstanding publish or keep alive requests.
        /// </summary>
        public int OutstandingRequestCount
        {
            get
            {
                lock (m_outstandingRequests)
                {
                    return m_outstandingRequests.Count;
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
                lock (m_outstandingRequests)
                {
                    var count = 0;

                    for (var ii = m_outstandingRequests.First; ii != null; ii = ii.Next)
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
                lock (m_outstandingRequests)
                {
                    var count = 0;

                    for (var ii = m_outstandingRequests.First; ii != null; ii = ii.Next)
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
            get => m_minPublishRequestCount;
            set
            {
                lock (SyncRoot)
                {
                    if (value >= kDefaultPublishRequestCount && value <= kMinPublishRequestCountMax)
                    {
                        m_minPublishRequestCount = value;
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
            get => Math.Max(m_minPublishRequestCount, m_maxPublishRequestCount);
            set
            {
                lock (SyncRoot)
                {
                    if (value >= kDefaultPublishRequestCount && value <= kMaxPublishRequestCountMax)
                    {
                        m_maxPublishRequestCount = value;
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
        /// Creates a secure channel to the specified endpoint.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="connection">The client endpoint for the reverse connect.</param>
        /// <param name="endpoint">A configured endpoint to connect to.</param>
        /// <param name="updateBeforeConnect">Update configuration based on server prior connect.</param>
        /// <param name="checkDomain">Check that the certificate specifies a valid domain (computer) name.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<ITransportChannel> CreateChannelAsync(
            ApplicationConfiguration configuration,
            ITransportWaitingConnection connection,
            ConfiguredEndpoint endpoint,
            bool updateBeforeConnect,
            bool checkDomain,
            CancellationToken ct = default)
        {
            endpoint.UpdateBeforeConnect = updateBeforeConnect;

            var endpointDescription = endpoint.Description;

            // create the endpoint configuration (use the application configuration to provide default values).
            var endpointConfiguration = endpoint.Configuration;

            if (endpointConfiguration == null)
            {
                endpoint.Configuration = endpointConfiguration = EndpointConfiguration.Create(configuration);
            }

            // create message context.
            IServiceMessageContext messageContext = configuration.CreateMessageContext(true);

            // update endpoint description using the discovery endpoint.
            if (endpoint.UpdateBeforeConnect && connection == null)
            {
                await endpoint.UpdateFromServerAsync(ct).ConfigureAwait(false);
                endpointDescription = endpoint.Description;
                endpointConfiguration = endpoint.Configuration;
            }

            // checks the domains in the certificate.
            if (checkDomain &&
                endpoint.Description.ServerCertificate?.Length > 0)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                configuration.CertificateValidator?.ValidateDomains(
                    new X509Certificate2(endpoint.Description.ServerCertificate),
                    endpoint);
#pragma warning restore CA2000 // Dispose objects before losing scope
                checkDomain = false;
            }

            X509Certificate2 clientCertificate = null;
            X509Certificate2Collection clientCertificateChain = null;
            if (endpointDescription.SecurityPolicyUri != SecurityPolicies.None)
            {
                clientCertificate = await LoadCertificate(configuration).ConfigureAwait(false);
                clientCertificateChain = await LoadCertificateChain(configuration, clientCertificate).ConfigureAwait(false);
            }

            // initialize the channel which will be created with the server.
            if (connection != null)
            {
                return SessionChannel.CreateUaBinaryChannel(
                    configuration,
                    connection,
                    endpointDescription,
                    endpointConfiguration,
                    clientCertificate,
                    clientCertificateChain,
                    messageContext);
            }

            return SessionChannel.Create(
                 configuration,
                 endpointDescription,
                 endpointConfiguration,
                 clientCertificate,
                 clientCertificateChain,
                 messageContext);
        }

        /// <summary>
        /// Creates a new communication session with a server using a reverse connection.
        /// </summary>
        /// <param name="sessionInstantiator">The Session constructor to use to create the session.</param>
        /// <param name="configuration">The configuration for the client application.</param>
        /// <param name="connection">The client endpoint for the reverse connect.</param>
        /// <param name="endpoint">The endpoint for the server.</param>
        /// <param name="updateBeforeConnect">If set to <c>true</c> the discovery endpoint is used to update the endpoint description before connecting.</param>
        /// <param name="checkDomain">If set to <c>true</c> then the domain in the certificate must match the endpoint used.</param>
        /// <param name="sessionName">The name to assign to the session.</param>
        /// <param name="sessionTimeout">The timeout period for the session.</param>
        /// <param name="identity">The user identity to associate with the session.</param>
        /// <param name="preferredLocales">The preferred locales.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The new session object.</returns>
        public static async Task<Session> Create(
            ISessionInstantiator sessionInstantiator,
            ApplicationConfiguration configuration,
            ITransportWaitingConnection connection,
            ConfiguredEndpoint endpoint,
            bool updateBeforeConnect,
            bool checkDomain,
            string sessionName,
            uint sessionTimeout,
            IUserIdentity identity,
            IList<string> preferredLocales,
            CancellationToken ct = default)
        {
            // initialize the channel which will be created with the server.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var channel = await Session.CreateChannelAsync(configuration, connection, endpoint, updateBeforeConnect, checkDomain, ct).ConfigureAwait(false);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // create the session object.
            var session = sessionInstantiator.Create(channel, configuration, endpoint, null);

            // create the session.
            try
            {
                await session.OpenAsync(sessionName, sessionTimeout, identity, preferredLocales, checkDomain, ct).ConfigureAwait(false);
            }
            catch (Exception)
            {
                session.Dispose();
                throw;
            }

            return session;
        }

        /// <inheritdoc/>
        public event RenewUserIdentityEventHandler RenewUserIdentity
        {
            add { m_RenewUserIdentity += value; }
            remove { m_RenewUserIdentity -= value; }
        }

        private event RenewUserIdentityEventHandler m_RenewUserIdentity;
        /// <inheritdoc/>
        public async Task OpenAsync(
            string sessionName,
            uint sessionTimeout,
            IUserIdentity identity,
            IList<string> preferredLocales,
            bool checkDomain,
            CancellationToken ct)
        {
            OpenValidateIdentity(ref identity, out var identityToken, out var identityPolicy, out var securityPolicyUri, out var requireEncryption);

            // validate the server certificate /certificate chain.
            X509Certificate2 serverCertificate = null;
            var certificateData = m_endpoint.Description.ServerCertificate;

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
                        await m_configuration.CertificateValidator.ValidateAsync(serverCertificateChain, m_endpoint, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await m_configuration.CertificateValidator.ValidateAsync(serverCertificateChain, ct).ConfigureAwait(false);
                    }
                    // save for reconnect
                    m_checkDomain = checkDomain;
                }
            }

            // create a nonce.
            var length = (uint)m_configuration.SecurityConfiguration.NonceLength;
            var clientNonce = Utils.Nonce.CreateNonce(length);

            // send the application instance certificate for the client.
            BuildCertificateData(out var clientCertificateData, out var clientCertificateChainData);

            var clientDescription = new ApplicationDescription
            {
                ApplicationUri = m_configuration.ApplicationUri,
                ApplicationName = m_configuration.ApplicationName,
                ApplicationType = ApplicationType.Client,
                ProductUri = m_configuration.ProductUri
            };

            if (sessionTimeout == 0)
            {
                sessionTimeout = (uint)m_configuration.ClientConfiguration.DefaultSessionTimeout;
            }

            var successCreateSession = false;
            CreateSessionResponse response = null;

            //if security none, first try to connect without certificate
            if (m_endpoint.Description.SecurityPolicyUri == SecurityPolicies.None)
            {
                //first try to connect with client certificate NULL
                try
                {
                    response = await base.CreateSessionAsync(
                        null,
                        clientDescription,
                        m_endpoint.Description.Server.ApplicationUri,
                        m_endpoint.EndpointUrl.ToString(),
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
                    m_logger.LogInformation("Create session failed with client certificate NULL. {Error}", ex.Message);
                    successCreateSession = false;
                }
            }

            if (!successCreateSession)
            {
                response = await base.CreateSessionAsync(
                        null,
                        clientDescription,
                        m_endpoint.Description.Server.ApplicationUri,
                        m_endpoint.EndpointUrl.ToString(),
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

            m_sessionTimeout = response.RevisedSessionTimeout;
            m_maxRequestMessageSize = response.MaxRequestMessageSize;

            // save session id.
            lock (SyncRoot)
            {
                // save session id and cookie in base
                base.SessionCreated(sessionId, sessionCookie);
            }

            m_logger.LogInformation("Revised session timeout value: {Timeout}. ", m_sessionTimeout);
            m_logger.LogInformation("Max response message size value: {MaxResponseSize}. Max request message size: {MaxRequestSize} ",
                MessageContext.MaxMessageSize, m_maxRequestMessageSize);

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
                var clientSignature = SecurityPolicies.Sign(m_instanceCertificate, securityPolicyUri, dataToSign);

                // select the security policy for the user token.
                securityPolicyUri = identityPolicy.SecurityPolicyUri;

                if (string.IsNullOrEmpty(securityPolicyUri))
                {
                    securityPolicyUri = m_endpoint.Description.SecurityPolicyUri;
                }

                // save previous nonce
                var previousServerNonce = GetCurrentTokenServerNonce();

                // validate server nonce and security parameters for user identity.
                ValidateServerNonce(
                    identity,
                    serverNonce,
                    securityPolicyUri,
                    previousServerNonce,
                    m_endpoint.Description.SecurityMode);

                // sign data with user token.
                var userTokenSignature = identityToken.Sign(dataToSign, securityPolicyUri);

                // encrypt token.
                identityToken.Encrypt(serverCertificate, serverNonce, securityPolicyUri);

                // send the software certificates assigned to the client.
                var clientSoftwareCertificates = GetSoftwareCertificates();

                // copy the preferred locales if provided.
                if (preferredLocales?.Count > 0)
                {
                    m_preferredLocales = new StringCollection(preferredLocales);
                }

                // activate session.
                var activateResponse = await ActivateSessionAsync(
                    null,
                    clientSignature,
                    clientSoftwareCertificates,
                    m_preferredLocales,
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
                        m_logger.LogInformation("ActivateSession result[{Index}] = {Result}", i, certificateResults[i]);
                    }
                }

                if (clientSoftwareCertificates?.Count > 0 && (certificateResults == null || certificateResults.Count == 0))
                {
                    m_logger.LogInformation("Empty results were received for the ActivateSession call.");
                }

                // fetch namespaces.
                await FetchNamespaceTablesAsync(ct).ConfigureAwait(false);

                lock (SyncRoot)
                {
                    // save nonces.
                    m_sessionName = sessionName;
                    m_identity = identity;
                    m_previousServerNonce = previousServerNonce;
                    m_serverNonce = serverNonce;
                    m_serverCertificate = serverCertificate;

                    // update system context.
                    m_systemContext.PreferredLocales = m_preferredLocales;
                    m_systemContext.SessionId = SessionId;
                    m_systemContext.UserIdentity = identity;
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
                    m_logger.LogError("Cleanup: CloseSessionAsync() or CloseChannelAsync() raised exception {Error}.", e.Message);
                }
                finally
                {
                    SessionCreated(null, null);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveSubscriptionAsync(Subscription subscription, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(subscription);

            if (subscription.Created)
            {
                await subscription.DeleteAsync(false, ct).ConfigureAwait(false);
            }

            lock (SyncRoot)
            {
                if (!m_subscriptions.Remove(subscription))
                {
                    return false;
                }

                subscription.Session = null;
            }

            m_SubscriptionsChanged?.Invoke(this, null);

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveSubscriptionsAsync(IEnumerable<Subscription> subscriptions, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(subscriptions);

            var subscriptionsToDelete = new List<Subscription>();

            var removed = PrepareSubscriptionsToDelete(subscriptions, subscriptionsToDelete);

            foreach (var subscription in subscriptionsToDelete)
            {
                await subscription.DeleteAsync(true, ct).ConfigureAwait(false);
            }

            if (removed)
            {
                m_SubscriptionsChanged?.Invoke(this, null);
            }

            return removed;
        }

        /// <inheritdoc/>
        public async Task<bool> TransferSubscriptionsAsync(
            SubscriptionCollection subscriptions,
            bool sendInitialValues,
            CancellationToken ct)
        {
            var subscriptionIds = CreateSubscriptionIdsForTransfer(subscriptions);
            var failedSubscriptions = 0;

            if (subscriptionIds.Count > 0)
            {
                var reconnecting = false;
                await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    reconnecting = m_reconnecting;
                    m_reconnecting = true;

                    var response = await base.TransferSubscriptionsAsync(null, subscriptionIds, sendInitialValues, ct).ConfigureAwait(false);
                    var results = response.Results;
                    var diagnosticInfos = response.DiagnosticInfos;
                    var responseHeader = response.ResponseHeader;

                    if (!StatusCode.IsGood(responseHeader.ServiceResult))
                    {
                        m_logger.LogError("TransferSubscription failed: {Result}", responseHeader.ServiceResult);
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
                                lock (m_acknowledgementsToSendLock)
                                {
                                    // create ack for available sequence numbers
                                    foreach (var sequenceNumber in results[ii].AvailableSequenceNumbers)
                                    {
                                        AddAcknowledgementToSend(m_acknowledgementsToSend, subscriptionIds[ii], sequenceNumber);
                                    }
                                }
                            }
                        }
                        else if (results[ii].StatusCode == StatusCodes.BadNothingToDo)
                        {
                            m_logger.LogInformation("SubscriptionId {Id} is already member of the session.", subscriptionIds[ii]);
                            failedSubscriptions++;
                        }
                        else
                        {
                            m_logger.LogError("SubscriptionId {Id} failed to transfer, StatusCode={Status}", subscriptionIds[ii], results[ii].StatusCode);
                            failedSubscriptions++;
                        }
                    }

                    m_logger.LogInformation("Session TRANSFER ASYNC of {Count} subscriptions completed. {Failed} failed.", subscriptions.Count, failedSubscriptions);
                }
                finally
                {
                    m_reconnecting = reconnecting;
                    m_reconnectLock.Release();
                }

                StartPublishing(OperationTimeout, false);
            }
            else
            {
                m_logger.LogInformation("No subscriptions. TransferSubscription skipped.");
            }

            return failedSubscriptions == 0;
        }

        /// <inheritdoc/>
        public async Task FetchNamespaceTablesAsync(CancellationToken ct = default)
        {
            var nodesToRead = PrepareNamespaceTableNodesToRead();

            // read from server.
            var response = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                nodesToRead,
                ct).ConfigureAwait(false);

            var values = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;
            var responseHeader = response.ResponseHeader;

            ValidateResponse(values, nodesToRead);
            ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

            UpdateNamespaceTable(values, diagnosticInfos, responseHeader);
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

                var configOperationLimits = m_configuration?.ClientConfiguration?.OperationLimits ?? new OperationLimits();
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
                m_logger.LogError(ex, "Failed to read operation limits from server. Using configuration defaults.");
                var operationLimits = m_configuration?.ClientConfiguration?.OperationLimits;
                if (operationLimits != null)
                {
                    OperationLimits = operationLimits;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<(IList<Node>, IList<ServiceResult>)> ReadNodesAsync(
            IList<NodeId> nodeIds, CancellationToken ct = default)
        {
            if (nodeIds.Count == 0)
            {
                return (new List<Node>(), new List<ServiceResult>());
            }

            var nodeCollection = new NodeCollection(nodeIds.Count);
            var itemsToRead = new ReadValueIdCollection(nodeIds.Count);

            // first read only nodeclasses for nodes from server.
            itemsToRead = new ReadValueIdCollection(
                nodeIds.Select(nodeId =>
                    new ReadValueId
                    {
                        NodeId = nodeId,
                        AttributeId = Attributes.NodeClass
                    }));

            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                itemsToRead,
                ct).ConfigureAwait(false);

            var nodeClassValues = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(nodeClassValues, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            // second determine attributes to read per nodeclass
            var attributesPerNodeId = new List<IDictionary<uint, DataValue>>(nodeIds.Count);
            var serviceResults = new List<ServiceResult>(nodeIds.Count);
            var attributesToRead = new ReadValueIdCollection();

            CreateAttributesReadNodesRequest(
                readResponse.ResponseHeader,
                itemsToRead, nodeClassValues, diagnosticInfos,
                attributesToRead, attributesPerNodeId, nodeCollection, serviceResults);

            if (attributesToRead.Count > 0)
            {
                readResponse = await ReadAsync(
                    null,
                    0,
                    TimestampsToReturn.Neither,
                    attributesToRead, ct).ConfigureAwait(false);

                var values = readResponse.Results;
                diagnosticInfos = readResponse.DiagnosticInfos;

                ClientBase.ValidateResponse(values, attributesToRead);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, attributesToRead);

                ProcessAttributesReadNodesResponse(
                    readResponse.ResponseHeader,
                    attributesToRead, attributesPerNodeId,
                    values, diagnosticInfos,
                    nodeCollection, serviceResults);
            }

            return (nodeCollection, serviceResults);
        }

        /// <inheritdoc/>
        public async Task<Node> ReadNodeAsync(NodeId nodeId, CancellationToken ct = default)
        {
            // build list of attributes.
            IDictionary<uint, DataValue> attributes = CreateAttributes();

            // build list of values to read.
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
            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                itemsToRead, ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            return ProcessReadResponse(readResponse.ResponseHeader, attributes,
                itemsToRead, values, diagnosticInfos);
        }

        /// <inheritdoc/>
        public async Task<DataValue> ReadValueAsync(
            NodeId nodeId, CancellationToken ct = default)
        {
            var itemToRead = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };

            var itemsToRead = new ReadValueIdCollection {
                itemToRead
            };

            // read from server.
            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Both,
                itemsToRead,
                ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            if (StatusCode.IsBad(values[0].StatusCode))
            {
                var result = ClientBase.GetResult(values[0].StatusCode, 0, diagnosticInfos, readResponse.ResponseHeader);
                throw new ServiceResultException(result);
            }

            return values[0];
        }

        /// <inheritdoc/>
        public async Task<(DataValueCollection, IList<ServiceResult>)> ReadValuesAsync(
            IList<NodeId> nodeIds,
            CancellationToken ct = default)
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
            )> BrowseAsync(
            RequestHeader requestHeader,
            ViewDescription view,
            IList<NodeId> nodesToBrowse,
            uint maxResultsToReturn,
            BrowseDirection browseDirection,
            NodeId referenceTypeId,
            bool includeSubtypes,
            uint nodeClassMask,
            CancellationToken ct = default)
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

        /// <inheritdoc/>
        public async Task<(
            ResponseHeader responseHeader,
            ByteStringCollection revisedContinuationPoints,
            IList<ReferenceDescriptionCollection> referencesList,
            IList<ServiceResult> errors
            )> BrowseNextAsync(
            RequestHeader requestHeader,
            ByteStringCollection continuationPoints,
            bool releaseContinuationPoint,
            CancellationToken ct = default)
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
                    errors.Add(new ServiceResult(result.StatusCode, ii, diagnosticInfos, response.ResponseHeader.StringTable));
                }
                else
                {
                    errors.Add(ServiceResult.Good);
                }
                revisedContinuationPoints.Add(result.ContinuationPoint);
                referencesList.Add(result.References);
                ii++;
            }

            return (response.ResponseHeader, revisedContinuationPoints, referencesList, errors);
        }

        /// <inheritdoc/>
        public async Task<(
            IList<ReferenceDescriptionCollection>,
            IList<ServiceResult>
            )>
                ManagedBrowseAsync(
                    RequestHeader requestHeader,
                    ViewDescription view,
                    IList<NodeId> nodesToBrowse,
                    uint maxResultsToReturn,
                    BrowseDirection browseDirection,
                    NodeId referenceTypeId,
                    bool includeSubtypes,
                    uint nodeClassMask,
                    CancellationToken ct = default
            )
        {
            var count = nodesToBrowse.Count;
            var result = new List<ReferenceDescriptionCollection>(count);
            var errors = new List<ServiceResult>(count);

            // first attempt for implementation: create the references for the output in advance.
            // optimize later, when everything works fine.
            for (var i = 0; i < nodesToBrowse.Count; i++)
            {
                result.Add(new ReferenceDescriptionCollection());
                errors.Add(new ServiceResult(StatusCodes.Good));
            }

            try
            {
                // in the first pass, we browse all nodes from the input.
                // Some nodes may need to be browsed again, these are then fed into the next pass.
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

                    if (ContinuationPointPolicy == ContinuationPointPolicy.Balanced && ServerMaxContinuationPointsPerBrowse > 0)
                    {
                        maxNodesPerBrowse = ServerMaxContinuationPointsPerBrowse < maxNodesPerBrowse ? ServerMaxContinuationPointsPerBrowse : maxNodesPerBrowse;
                    }

                    // split input into batches
                    var batchOffset = 0;

                    var nodesToBrowseForNextPass = new List<NodeId>();
                    var referenceDescriptionsForNextPass = new List<ReferenceDescriptionCollection>();
                    var errorsForNextPass = new List<ServiceResult>();

                    // loop over the batches
                    foreach (var nodesToBrowseBatch in ((List<NodeId>)nodesToBrowseForPass).Batch<NodeId, List<NodeId>>(maxNodesPerBrowse))
                    {
                        var nodesToBrowseBatchCount = nodesToBrowseBatch.Count;

                        (
                            var resultForBatch,
                            var errorsForBatch
                        )
                        =
                        await BrowseWithBrowseNextAsync(requestHeader, view, nodesToBrowseBatch, maxResultsToReturn, browseDirection, referenceTypeId, includeSubtypes, nodeClassMask
, ct).ConfigureAwait(false);

                        var resultOffset = batchOffset;
                        for (var ii = 0; ii < nodesToBrowseBatchCount; ii++)
                        {
                            var statusCode = errorsForBatch[ii].StatusCode;
                            if (StatusCode.IsBad(statusCode))
                            {
                                var addToNextPass = false;
                                if (statusCode == StatusCodes.BadNoContinuationPoints)
                                {
                                    addToNextPass = true;
                                    badNoCPErrorsPerPass++;
                                }
                                else if (statusCode == StatusCodes.BadContinuationPointInvalid)
                                {
                                    addToNextPass = true;
                                    badCPInvalidErrorsPerPass++;
                                }
                                else
                                {
                                    otherErrorsPerPass++;
                                }

                                if (addToNextPass)
                                {
                                    nodesToBrowseForNextPass.Add(nodesToBrowseForPass[resultOffset]);
                                    referenceDescriptionsForNextPass.Add(resultForPass[resultOffset]);
                                    errorsForNextPass.Add(errorsForPass[resultOffset]);
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
                    referenceDescriptionsForNextPass = new List<ReferenceDescriptionCollection>();

                    errorsForPass = errorsForNextPass;
                    errorsForNextPass = new List<ServiceResult>();

                    nodesToBrowseForPass = nodesToBrowseForNextPass;
                    nodesToBrowseForNextPass = new List<NodeId>();

                    const string aggregatedErrorMessage = "ManagedBrowse: in pass {Count}, Errors={ErrorsInPass} occured with a status code {Status}.";

                    if (badCPInvalidErrorsPerPass > 0)
                    {
                        m_logger.LogDebug(aggregatedErrorMessage, passCount, badCPInvalidErrorsPerPass,
                            nameof(StatusCodes.BadContinuationPointInvalid));
                    }
                    if (badNoCPErrorsPerPass > 0)
                    {
                        m_logger.LogDebug(aggregatedErrorMessage, passCount, badNoCPErrorsPerPass,
                            nameof(StatusCodes.BadNoContinuationPoints));
                    }
                    if (otherErrorsPerPass > 0)
                    {
                        m_logger.LogDebug(aggregatedErrorMessage, passCount, otherErrorsPerPass,
                            $"different from {nameof(StatusCodes.BadNoContinuationPoints)} or {nameof(StatusCodes.BadContinuationPointInvalid)}");
                    }
                    if (otherErrorsPerPass == 0 && badCPInvalidErrorsPerPass == 0 && badNoCPErrorsPerPass == 0)
                    {
                        m_logger.LogTrace("ManagedBrowse completed with no errors.");
                    }

                    passCount++;
                } while (nodesToBrowseForPass.Count > 0);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "ManagedBrowse failed");
            }

            return (result, errors);
        }

        /// <summary>
        /// Used to pass on references to the Service results in the loop in ManagedBrowseAsync.
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
        private async Task<(
            IList<ReferenceDescriptionCollection>,
            IList<ServiceResult>
            )>
            BrowseWithBrowseNextAsync(
            RequestHeader requestHeader,
            ViewDescription view,
            List<NodeId> nodeIds,
            uint maxResultsToReturn,
            BrowseDirection browseDirection,
            NodeId referenceTypeId,
            bool includeSubtypes,
            uint nodeClassMask,
            CancellationToken ct = default
            )
        {
            if (requestHeader != null)
            {
                requestHeader.RequestHandle = 0;
            }

            var result = new List<ReferenceDescriptionCollection>(nodeIds.Count);

            (
                _,
                var continuationPoints,
                var referenceDescriptions,
                var errors
            ) =
            await BrowseAsync(
                requestHeader,
                view,
                nodeIds,
                maxResultsToReturn,
                browseDirection,
                referenceTypeId,
                includeSubtypes,
                nodeClassMask,
                ct).ConfigureAwait(false);

            result.AddRange(referenceDescriptions);

            // process any continuation point.
            var previousResults = result;
            var errorAnchors = new List<ReferenceWrapper<ServiceResult>>();
            var previousErrors = new List<ReferenceWrapper<ServiceResult>>();
            foreach (var error in errors)
            {
                previousErrors.Add(new ReferenceWrapper<ServiceResult> { reference = error });
                errorAnchors.Add(previousErrors[^1]);
            }

            var nextContinuationPoints = new ByteStringCollection();
            var nextResults = new List<ReferenceDescriptionCollection>();
            var nextErrors = new List<ReferenceWrapper<ServiceResult>>();

            for (var ii = 0; ii < nodeIds.Count; ii++)
            {
                if (continuationPoints[ii] != null && !StatusCode.IsBad(previousErrors[ii].reference.StatusCode))
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

                (
                    _,
                    var revisedContinuationPoints,
                    var browseNextResults,
                    var browseNextErrors
                ) = await BrowseNextAsync(
                    requestHeader,
                    nextContinuationPoints,
                    false,
                    ct
                    ).ConfigureAwait(false);

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
                    if (revisedContinuationPoints[ii] != null && !StatusCode.IsBad(browseNextErrors[ii].StatusCode))
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

            return (result, finalErrors);
        }

        /// <inheritdoc/>
        public async Task<IList<object>> CallAsync(NodeId objectId, NodeId methodId, CancellationToken ct = default, params object[] args)
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
        public async Task<ReferenceDescriptionCollection> FetchReferencesAsync(
            NodeId nodeId,
            CancellationToken ct = default)
        {
            (
                var descriptions,
                _
            ) =
                await ManagedBrowseAsync(
                    null,
                    null,
                    new NodeId[] { nodeId },
                    0,
                    BrowseDirection.Both,
                    null,
                    true,
                    0,
                    ct).ConfigureAwait(false);
            return descriptions[0];
        }

        /// <inheritdoc/>
        public Task<(IList<ReferenceDescriptionCollection>, IList<ServiceResult>)> FetchReferencesAsync(
            IList<NodeId> nodeIds,
            CancellationToken ct = default)
            => ManagedBrowseAsync(
                null,
                null,
                nodeIds,
                0,
                BrowseDirection.Both,
                null,
                true,
                0,
                ct
                );

        /// <summary>
        /// Recreates a session based on a specified template.
        /// </summary>
        /// <param name="sessionTemplate">The Session object to use as template</param>
        /// <param name="ct"></param>
        /// <returns>The new session object.</returns>
        public static async Task<Session> RecreateAsync(Session sessionTemplate, CancellationToken ct = default)
        {
            var messageContext = sessionTemplate.m_configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the channel object used to connect to the server.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var channel = SessionChannel.Create(
                sessionTemplate.m_configuration,
                sessionTemplate.ConfiguredEndpoint.Description,
                sessionTemplate.ConfiguredEndpoint.Configuration,
                sessionTemplate.m_instanceCertificate,
                sessionTemplate.m_configuration.SecurityConfiguration.SendCertificateChain ?
                    sessionTemplate.m_instanceCertificateChain : null,
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
                    sessionTemplate.m_checkDomain,
                    ct).ConfigureAwait(false);

                await session.RecreateSubscriptionsAsync(sessionTemplate.Subscriptions, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                session.Dispose();
                ThrowCouldNotRecreateSessionException(e, sessionTemplate.m_sessionName);
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
        public static async Task<Session> RecreateAsync(Session sessionTemplate, ITransportWaitingConnection connection, CancellationToken ct = default)
        {
            var messageContext = sessionTemplate.m_configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the channel object used to connect to the server.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var channel = SessionChannel.Create(
                sessionTemplate.m_configuration,
                connection,
                sessionTemplate.m_endpoint.Description,
                sessionTemplate.m_endpoint.Configuration,
                sessionTemplate.m_instanceCertificate,
                sessionTemplate.m_configuration.SecurityConfiguration.SendCertificateChain ?
                    sessionTemplate.m_instanceCertificateChain : null,
                messageContext);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // create the session object.
            var session = sessionTemplate.CloneSession(channel, true);

            try
            {
                // open the session.
                await session.OpenAsync(
                    sessionTemplate.m_sessionName,
                    (uint)sessionTemplate.m_sessionTimeout,
                    sessionTemplate.m_identity,
                    sessionTemplate.m_preferredLocales,
                    sessionTemplate.m_checkDomain,
                    ct).ConfigureAwait(false);

                await session.RecreateSubscriptionsAsync(sessionTemplate.Subscriptions, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                session.Dispose();
                ThrowCouldNotRecreateSessionException(e, sessionTemplate.m_sessionName);
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

            var messageContext = sessionTemplate.m_configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the session object.
            var session = sessionTemplate.CloneSession(transportChannel, true);

            try
            {
                // open the session.
                await session.OpenAsync(
                    sessionTemplate.m_sessionName,
                    (uint)sessionTemplate.m_sessionTimeout,
                    sessionTemplate.m_identity,
                    sessionTemplate.m_preferredLocales,
                    sessionTemplate.m_checkDomain,
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
                ThrowCouldNotRecreateSessionException(e, sessionTemplate.m_sessionName);
            }

            return session;
        }

        /// <inheritdoc/>
        public override Task<StatusCode> CloseAsync(CancellationToken ct = default)
        {
            return CloseAsync(m_keepAliveInterval, true, ct);
        }

        /// <inheritdoc/>
        public Task<StatusCode> CloseAsync(bool closeChannel, CancellationToken ct = default)
        {
            return CloseAsync(m_keepAliveInterval, closeChannel, ct);
        }

        /// <inheritdoc/>
        public virtual async Task<StatusCode> CloseAsync(int timeout, bool closeChannel, CancellationToken ct = default)
        {
            // check if already called.
            if (Disposed)
            {
                return StatusCodes.Good;
            }

            StatusCode result = StatusCodes.Good;

            // stop the keep alive timer.
            StopKeepAliveTimer();

            // check if correctly connected.
            var connected = Connected;

            // halt all background threads.
            if (connected && m_SessionClosing != null)
            {
                try
                {
                    m_SessionClosing(this, null);
                }
                catch (Exception e)
                {
                    m_logger.LogError(e, "Session: Unexpected error raising SessionClosing event.");
                }
            }

            // close the session with the server.
            if (connected && !KeepAliveStopped)
            {
                try
                {
                    // close the session and delete all subscriptions if specified.
                    var requestHeader = new RequestHeader()
                    {
                        TimeoutHint = timeout > 0 ? (uint)timeout : (uint)(OperationTimeout > 0 ? OperationTimeout : 0)
                    };
                    var response = await base.CloseSessionAsync(requestHeader, DeleteSubscriptionsOnClose, ct).ConfigureAwait(false);

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
        public Task ReconnectAsync(ITransportWaitingConnection connection, CancellationToken ct)
            => ReconnectAsync(connection, null, ct);

        /// <summary>
        /// Reconnects to the server after a network failure using a waiting connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transportChannel"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        private async Task ReconnectAsync(ITransportWaitingConnection connection, ITransportChannel transportChannel, CancellationToken ct)
        {
            var resetReconnect = false;
            await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var reconnecting = m_reconnecting;
                m_reconnecting = true;
                resetReconnect = true;
                m_reconnectLock.Release();

                // check if already connecting.
                if (reconnecting)
                {
                    m_logger.LogWarning("Session is already attempting to reconnect.");

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
                            m_logger.LogWarning("WARNING: {Error}", error.ToString());
                            operation.Fault(false, error);
                        }
                    }
                }
                else if (!result.AsyncWaitHandle.WaitOne(kReconnectTimeout / 2))
                {
                    m_logger.LogWarning("ACTIVATE SESSION ASYNC timed out. {Good}/{Outstanding}",
                        GoodPublishRequestCount, OutstandingRequestCount);
                }

                // reactivate session.
                byte[] serverNonce = null;
                StatusCodeCollection certificateResults = null;
                DiagnosticInfoCollection certificateDiagnosticInfos = null;

                EndActivateSession(
                    result,
                    out serverNonce,
                    out certificateResults,
                    out certificateDiagnosticInfos);

                m_logger.LogInformation("Session RECONNECT {Session} completed successfully.", SessionId);

                lock (SyncRoot)
                {
                    m_previousServerNonce = m_serverNonce;
                    m_serverNonce = serverNonce;
                }

                await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                m_reconnecting = false;
                resetReconnect = false;
                m_reconnectLock.Release();

                StartPublishing(OperationTimeout, true);

                StartKeepAliveTimer();

                IndicateSessionConfigurationChanged();
            }
            finally
            {
                if (resetReconnect)
                {
                    await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                    m_reconnecting = false;
                    m_reconnectLock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<(bool, ServiceResult)> RepublishAsync(uint subscriptionId, uint sequenceNumber, CancellationToken ct)
        {
            // send republish request.
            var requestHeader = new RequestHeader
            {
                TimeoutHint = (uint)OperationTimeout,
                ReturnDiagnostics = (uint)(int)ReturnDiagnostics,
                RequestHandle = Utils.IncrementIdentifier(ref m_publishCounter)
            };

            try
            {
                m_logger.LogInformation("Requesting RepublishAsync for {SubscriptionId}-{SeqNumber}", subscriptionId, sequenceNumber);

                // request republish.
                var response = await RepublishAsync(
                    requestHeader,
                    subscriptionId,
                    sequenceNumber,
                    ct).ConfigureAwait(false);
                var responseHeader = response.ResponseHeader;
                var notificationMessage = response.NotificationMessage;

                m_logger.LogInformation("Received RepublishAsync for {SubscriptionId}-{SeqNumber}-{Result}", subscriptionId, sequenceNumber, responseHeader.ServiceResult);

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
                        m_logger.LogWarning("Transfer subscription unsupported, TransferSubscriptionsOnReconnect set to false.");
                    }
                    else
                    {
                        m_logger.LogError(sre, "Transfer subscriptions failed.");
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Unexpected Transfer subscriptions error.");
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

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is Session session)
            {
                if (!m_endpoint.Equals(session.Endpoint))
                {
                    return false;
                }

                if (!m_sessionName.Equals(session.SessionName, StringComparison.Ordinal))
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
            return HashCode.Combine(m_endpoint, m_sessionName, SessionId);
        }

        /// <summary>
        /// An overrideable version of a session clone which is used
        /// internally to create new subclassed clones from a Session class.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="copyEventHandlers"></param>
        public virtual Session CloneSession(ITransportChannel channel, bool copyEventHandlers)
        {
            return new Session(channel, this, copyEventHandlers);
        }

        /// <inheritdoc/>
        public bool AddSubscription(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription);

            lock (SyncRoot)
            {
                if (m_subscriptions.Contains(subscription))
                {
                    return false;
                }

                subscription.Session = this;
                m_subscriptions.Add(subscription);
            }

            m_SubscriptionsChanged?.Invoke(this, null);

            return true;
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
                if (!m_subscriptions.Remove(subscription))
                {
                    return false;
                }

                subscription.Session = null;
            }

            m_SubscriptionsChanged?.Invoke(this, null);

            return true;
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
        protected virtual void OnSoftwareCertificateError(SignedSoftwareCertificate signedCertificate, ServiceResult result)
        {
            throw new ServiceResultException(result);
        }

        /// <summary>
        /// Inspects the software certificates provided by the server.
        /// </summary>
        /// <param name="softwareCertificates"></param>
        protected virtual void ValidateSoftwareCertificates(IList<SoftwareCertificate> softwareCertificates)
        {
            // always accept valid certificates.
        }

        /// <summary>
        /// Starts a timer to check that the connection to the server is still available.
        /// </summary>
        private void StartKeepAliveTimer()
        {
            var keepAliveInterval = m_keepAliveInterval;

            m_lastKeepAliveErrorStatusCode = StatusCodes.Good;
            Interlocked.Exchange(ref m_lastKeepAliveTime, DateTime.UtcNow.Ticks);
            m_lastKeepAliveTickCount = HiResClock.TickCount;

            m_serverState = ServerState.Unknown;

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
                m_keepAliveTimer = keepAliveTimer;
            }
#else
                // start timer
                m_keepAliveTimer = new Timer(OnKeepAlive, nodesToRead, keepAliveInterval, keepAliveInterval);
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
            m_keepAliveTimer?.Dispose();
            m_keepAliveTimer = null;
        }

        /// <summary>
        /// Removes a completed async request.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="requestId"></param>
        /// <param name="typeId"></param>
        private AsyncRequestState RemoveRequest(IAsyncResult result, uint requestId, uint typeId)
        {
            lock (m_outstandingRequests)
            {
                for (var ii = m_outstandingRequests.First; ii != null; ii = ii.Next)
                {
                    if (Object.ReferenceEquals(result, ii.Value.Result) || (requestId == ii.Value.RequestId && typeId == ii.Value.RequestTypeId))
                    {
                        var state = ii.Value;
                        m_outstandingRequests.Remove(ii);
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
        private void AsyncRequestStarted(IAsyncResult result, uint requestId, uint typeId)
        {
            lock (m_outstandingRequests)
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

                    m_outstandingRequests.AddLast(state);
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
            lock (m_outstandingRequests)
            {
                // remove the request.
                var state = RemoveRequest(result, requestId, typeId);

                if (state != null)
                {
                    // mark any old requests as default (i.e. the should have returned before this request).
                    const int maxAge = 1000;

                    for (var ii = m_outstandingRequests.First; ii != null; ii = ii.Next)
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

                    m_outstandingRequests.AddLast(state);
                }
            }
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

            m_logger.LogTrace("Session {Id}: KeepAlive PeriodicTimer exit.", SessionId);
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
                if (!Connected || m_keepAliveTimer == null)
                {
                    return;
                }

                // check if session has been closed.
                if (m_reconnecting)
                {
                    m_logger.LogWarning("Session {Id}: KeepAlive ignored while reconnecting.", SessionId);
                    return;
                }

                // raise error if keep alives are not coming back.
                if (KeepAliveStopped && !OnKeepAliveError(ServiceResult.Create(StatusCodes.BadNoCommunication, "Server not responding to keep alive requests.")))
                {
                    return;
                }

                var requestHeader = new RequestHeader
                {
                    RequestHandle = Utils.IncrementIdentifier(ref m_keepAliveCounter),
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
                m_logger.LogError("Could not send keep alive request: {ErrorType} {Message}", e.GetType().FullName, e.Message);
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
                m_logger.LogError("Unexpected keep alive error occurred: {Message}", e.Message);
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
                if (m_reconnecting)
                {
                    return;
                }

                m_lastKeepAliveErrorStatusCode = StatusCodes.Good;
                Interlocked.Exchange(ref m_lastKeepAliveTime, DateTime.UtcNow.Ticks);
                m_lastKeepAliveTickCount = HiResClock.TickCount;

                lock (m_outstandingRequests)
                {
                    for (var ii = m_outstandingRequests.First; ii != null; ii = ii.Next)
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
                m_lastKeepAliveErrorStatusCode = StatusCodes.Good;
                Interlocked.Exchange(ref m_lastKeepAliveTime, DateTime.UtcNow.Ticks);
                m_lastKeepAliveTickCount = HiResClock.TickCount;
            }

            // save server state.
            m_serverState = currentState;

            var callback = m_KeepAlive;

            if (callback != null)
            {
                try
                {
                    callback(this, new KeepAliveEventArgs(null, currentState, currentTime));
                }
                catch (Exception e)
                {
                    m_logger.LogError(e, "Session: Unexpected error invoking KeepAliveCallback.");
                }
            }
        }

        /// <summary>
        /// Called when a error occurs during a keep alive.
        /// </summary>
        /// <param name="result"></param>
        protected virtual bool OnKeepAliveError(ServiceResult result)
        {
            m_lastKeepAliveErrorStatusCode = result.StatusCode;
            if (result.StatusCode == StatusCodes.BadNoCommunication)
            {
                //keep alive read timed out
                var delta = HiResClock.TickCount - m_lastKeepAliveTickCount;
                m_logger.LogInformation(
                    "KEEP ALIVE LATE: {Late}ms, EndpointUrl={Url}, RequestCount={Good}/{Outstanding}",
                    delta,
                    Endpoint?.EndpointUrl,
                    GoodPublishRequestCount,
                    OutstandingRequestCount);
            }

            var callback = m_KeepAlive;

            if (callback != null)
            {
                try
                {
                    var args = new KeepAliveEventArgs(result, ServerState.Unknown, DateTime.UtcNow);
                    callback(this, args);
                    return !args.CancelKeepAlive;
                }
                catch (Exception e)
                {
                    m_logger.LogError(e, "Session: Unexpected error invoking KeepAliveCallback.");
                }
            }

            return true;
        }

        /// <summary>
        /// Prepare a list of subscriptions to delete.
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="subscriptionsToDelete"></param>
        private bool PrepareSubscriptionsToDelete(IEnumerable<Subscription> subscriptions, List<Subscription> subscriptionsToDelete)
        {
            var removed = false;
            lock (SyncRoot)
            {
                foreach (var subscription in subscriptions)
                {
                    if (m_subscriptions.Remove(subscription))
                    {
                        if (subscription.Created)
                        {
                            subscriptionsToDelete.Add(subscription);
                        }

                        removed = true;
                    }
                }
            }
            return removed;
        }

        /// <summary>
        /// Prepares the list of node ids to read to fetch the namespace table.
        /// </summary>
        private static ReadValueIdCollection PrepareNamespaceTableNodesToRead()
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

            return nodesToRead;
        }

        /// <summary>
        /// Updates the NamespaceTable with the result of the <see cref="PrepareNamespaceTableNodesToRead"/> read operation.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        private void UpdateNamespaceTable(DataValueCollection values, DiagnosticInfoCollection diagnosticInfos, ResponseHeader responseHeader)
        {
            // validate namespace array.
            var result = ValidateDataValue(values[0], typeof(string[]), 0, diagnosticInfos, responseHeader);

            if (ServiceResult.IsBad(result))
            {
                m_logger.LogError("FetchNamespaceTables: Cannot read NamespaceArray node: {Status}", result.StatusCode);
            }
            else
            {
                m_namespaceUris.Update((string[])values[0].Value);
            }

            // validate server array.
            result = ValidateDataValue(values[1], typeof(string[]), 1, diagnosticInfos, responseHeader);

            if (ServiceResult.IsBad(result))
            {
                m_logger.LogError("FetchNamespaceTables: Cannot read ServerArray node: {Status} ", result.StatusCode);
            }
            else
            {
                m_serverUris.Update((string[])values[1].Value);
            }
        }

        /// <summary>
        /// Creates a read request with attributes determined by the NodeClass.
        /// </summary>
        /// <param name="responseHeader"></param>
        /// <param name="itemsToRead"></param>
        /// <param name="nodeClassValues"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="attributesToRead"></param>
        /// <param name="attributesPerNodeId"></param>
        /// <param name="nodeCollection"></param>
        /// <param name="errors"></param>
        private static void CreateAttributesReadNodesRequest(
            ResponseHeader responseHeader,
            ReadValueIdCollection itemsToRead,
            DataValueCollection nodeClassValues,
            DiagnosticInfoCollection diagnosticInfos,
            ReadValueIdCollection attributesToRead,
            List<IDictionary<uint, DataValue>> attributesPerNodeId,
            Opc.Ua.NodeCollection nodeCollection,
            List<ServiceResult> errors)
        {
            int? nodeClass;
            for (var ii = 0; ii < itemsToRead.Count; ii++)
            {
                var node = new Node();
                node.NodeId = itemsToRead[ii].NodeId;
                if (!DataValue.IsGood(nodeClassValues[ii]))
                {
                    nodeCollection.Add(node);
                    errors.Add(new ServiceResult(nodeClassValues[ii].StatusCode, ii, diagnosticInfos, responseHeader.StringTable));
                    attributesPerNodeId.Add(null);
                    continue;
                }

                // check for valid node class.
                nodeClass = nodeClassValues[ii].Value as int?;

                if (nodeClass == null)
                {
                    nodeCollection.Add(node);
                    errors.Add(ServiceResult.Create(StatusCodes.BadUnexpectedError,
                        "Node does not have a valid value for NodeClass: {0}.", nodeClassValues[ii].Value));
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
                errors.Add(ServiceResult.Good);
                attributesPerNodeId.Add(attributes);
            }
        }

        /// <summary>
        /// Builds the node collection results based on the attribute values of the read response.
        /// </summary>
        /// <param name="responseHeader">The response header of the read request.</param>
        /// <param name="attributesToRead">The collection of all attributes to read passed in the read request.</param>
        /// <param name="attributesPerNodeId">The attributes requested per NodeId</param>
        /// <param name="values">The attribute values returned by the read request.</param>
        /// <param name="diagnosticInfos">The diagnostic info returned by the read request.</param>
        /// <param name="nodeCollection">The node collection which holds the results.</param>
        /// <param name="errors">The service results for each node.</param>
        private static void ProcessAttributesReadNodesResponse(
            ResponseHeader responseHeader,
            ReadValueIdCollection attributesToRead,
            IList<IDictionary<uint, DataValue>> attributesPerNodeId,
            DataValueCollection values,
            DiagnosticInfoCollection diagnosticInfos,
            IList<Node> nodeCollection,
            IList<ServiceResult> errors)
        {
            var readIndex = 0;
            for (var ii = 0; ii < nodeCollection.Count; ii++)
            {
                var attributes = attributesPerNodeId[ii];
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
                    nodeCollection[ii] = ProcessReadResponse(responseHeader, attributes,
                        subRangeAttributes, subRangeValues, subRangeDiagnostics);
                    errors[ii] = ServiceResult.Good;
                }
                catch (ServiceResultException sre)
                {
                    errors[ii] = sre.Result;
                }
                readIndex += readCount;
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
            if (m_reconnecting)
            {
                m_logger.LogWarning("Publish skipped due to reconnect");
                return null;
            }

            // get event handler to modify ack list
            var callback = m_PublishSequenceNumbersToAcknowledge;

            // collect the current set if acknowledgements.
            SubscriptionAcknowledgementCollection acknowledgementsToSend = null;
            lock (m_acknowledgementsToSendLock)
            {
                if (callback != null)
                {
                    try
                    {
                        var deferredAcknowledgementsToSend = new SubscriptionAcknowledgementCollection();
                        callback(this, new PublishSequenceNumbersToAcknowledgeEventArgs(m_acknowledgementsToSend, deferredAcknowledgementsToSend));
                        acknowledgementsToSend = m_acknowledgementsToSend;
                        m_acknowledgementsToSend = deferredAcknowledgementsToSend;
                    }
                    catch (Exception e2)
                    {
                        m_logger.LogError(e2, "Session: Unexpected error invoking PublishSequenceNumbersToAcknowledgeEventArgs.");
                    }
                }

                if (acknowledgementsToSend == null)
                {
                    // send all ack values, clear list
                    acknowledgementsToSend = m_acknowledgementsToSend;
                    m_acknowledgementsToSend = new SubscriptionAcknowledgementCollection();
                }
#if DEBUG_SEQUENTIALPUBLISHING
                foreach (var toSend in acknowledgementsToSend)
                {
                    m_latestAcknowledgementsSent[toSend.SubscriptionId] = toSend.SequenceNumber;
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
                RequestHandle = Utils.IncrementIdentifier(ref m_publishCounter)
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
                m_logger.LogError(e, "Unexpected error sending publish request.");
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
            bool moreNotifications;

            AsyncRequestCompleted(result, requestHeader.RequestHandle, DataTypes.PublishRequest);

            // CoreClientUtils.EventLog.PublishStop((int)requestHeader.RequestHandle);

            try
            {
                // gate entry if transfer/reactivate is busy
                m_reconnectLock.Wait();
                var reconnecting = m_reconnecting;
                m_reconnectLock.Release();

                // complete publish.
                UInt32Collection availableSequenceNumbers;
                NotificationMessage notificationMessage;
                StatusCodeCollection acknowledgeResults;
                DiagnosticInfoCollection acknowledgeDiagnosticInfos;

                var responseHeader = EndPublish(
                    result,
                    out subscriptionId,
                    out availableSequenceNumbers,
                    out moreNotifications,
                    out notificationMessage,
                    out acknowledgeResults,
                    out acknowledgeDiagnosticInfos);

                var logLevel = LogLevel.Warning;
                foreach (var code in acknowledgeResults)
                {
                    if (StatusCode.IsBad(code) && code != StatusCodes.BadSequenceNumberUnknown)
                    {
                        m_logger.Log(logLevel, "Publish Ack Response. ResultCode={ResultCode}; SubscriptionId={SubscriptionId}", code.ToString(), subscriptionId);
                        // only show the first error as warning
                        logLevel = LogLevel.Trace;
                    }
                }

                // nothing more to do if session changed.
                if (sessionId != SessionId)
                {
                    m_logger.LogWarning("Publish response discarded because session id changed: Old {Old} != New {New}", sessionId, SessionId);
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
                    m_logger.LogWarning("No new publish sent because of reconnect in progress.");
                    return;
                }
            }
            catch (Exception e)
            {
                if (m_subscriptions.Count == 0)
                {
                    // Publish responses with error should occur after deleting the last subscription.
                    m_logger.LogError("Publish #{Handle}, Subscription count = 0, Error: {Message}", requestHeader.RequestHandle, e.Message);
                }
                else
                {
                    m_logger.LogError("Publish #{Handle}, Reconnecting={Reconnecting}, Error: {Message}", requestHeader.RequestHandle, m_reconnecting, e.Message);
                }

                // raise an error event.
                var error = new ServiceResult(e);

                if (error.Code != StatusCodes.BadNoSubscription)
                {
                    var callback = m_PublishError;

                    if (callback != null)
                    {
                        try
                        {
                            callback(this, new PublishErrorEventArgs(error, subscriptionId, 0));
                        }
                        catch (Exception e2)
                        {
                            m_logger.LogError(e2, "Session: Unexpected error invoking PublishErrorCallback.");
                        }
                    }
                }

                // ignore errors if reconnecting.
                if (m_reconnecting)
                {
                    m_logger.LogWarning("Publish abandoned after error due to reconnect: {Message}", e.Message);
                    return;
                }

                // nothing more to do if session changed.
                if (sessionId != SessionId)
                {
                    m_logger.LogError("Publish abandoned after error because session id changed: Old {Old} != New {New}", sessionId, SessionId);
                    return;
                }

                // try to acknowledge the notifications again in the next publish.
                if (acknowledgementsToSend != null)
                {
                    lock (m_acknowledgementsToSendLock)
                    {
                        m_acknowledgementsToSend.AddRange(acknowledgementsToSend);
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
                            m_tooManyPublishRequests = tooManyPublishRequests;
                            m_logger.LogInformation("PUBLISH - Too many requests, set limit to GoodPublishRequestCount={NewGood}.", m_tooManyPublishRequests);
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
                        m_logger.LogError(e, "PUBLISH #{Handle} - Unhandled error {Status} during Publish.", requestHeader.RequestHandle, error.StatusCode);
                        goto case StatusCodes.BadServerTooBusy;

                }
            }

            QueueBeginPublish();
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
                m_logger.LogDebug("PUBLISH - Did not send another publish request. GoodPublishRequestCount={Good}, MinPublishRequestCount={Min}", requestCount, minPublishRequestCount);
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

            securityPolicyUri = m_endpoint.Description.SecurityPolicyUri;

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
            identityPolicy = m_endpoint.Description.FindUserTokenPolicy(identityToken.PolicyId);

            if (identityPolicy == null)
            {
                // try looking up by TokenType if the policy id was not found.
                identityPolicy = m_endpoint.Description.FindUserTokenPolicy(identity.TokenType, identity.IssuedTokenType);

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
            clientCertificateData = (m_instanceCertificate?.RawData);
            clientCertificateChainData = null;

            if (m_instanceCertificateChain?.Count > 0 &&
                m_configuration.SecurityConfiguration.SendCertificateChain)
            {
                var clientCertificateChain = new List<byte>();

                for (var i = 0; i < m_instanceCertificateChain.Count; i++)
                {
                    clientCertificateChain.AddRange(m_instanceCertificateChain[i].RawData);
                }

                clientCertificateChainData = clientCertificateChain.ToArray();
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
                m_endpoint.Description.ServerCertificate != null &&
                !Utils.IsEqual(serverCertificateData, m_endpoint.Description.ServerCertificate))
            {
                try
                {
                    // verify for certificate chain in endpoint.
                    var serverCertificateChain = Utils.ParseCertificateChainBlob(m_endpoint.Description.ServerCertificate);

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
                m_logger.LogInformation("Server signature is null or empty.");

                //throw ServiceResultException.Create(
                //    StatusCodes.BadSecurityChecksFailed,
                //    "Server signature is null or empty.");
            }

            // validate the server's signature.
            var dataToSign = Utils.Append(clientCertificateData, clientNonce);

            if (!SecurityPolicies.Verify(serverCertificate, m_endpoint.Description.SecurityPolicyUri, dataToSign, serverSignature))
            {
                // validate the signature with complete chain if the check with leaf certificate failed.
                if (clientCertificateChainData != null)
                {
                    dataToSign = Utils.Append(clientCertificateChainData, clientNonce);

                    if (!SecurityPolicies.Verify(serverCertificate, m_endpoint.Description.SecurityPolicyUri, dataToSign, serverSignature))
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
            if (m_discoveryServerEndpoints?.Count > 0)
            {
                // Compare EndpointDescriptions returned at GetEndpoints with values returned at CreateSession
                EndpointDescriptionCollection expectedServerEndpoints = null;

                if (serverEndpoints != null &&
                    m_discoveryProfileUris?.Count > 0)
                {
                    // Select EndpointDescriptions with a transportProfileUri that matches the
                    // profileUris specified in the original GetEndpoints() request.
                    expectedServerEndpoints = new EndpointDescriptionCollection();

                    foreach (var serverEndpoint in serverEndpoints)
                    {
                        if (m_discoveryProfileUris.Contains(serverEndpoint.TransportProfileUri))
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
                    m_discoveryServerEndpoints.Count != expectedServerEndpoints.Count)
                {
                    throw ServiceResultException.Create(
                        StatusCodes.BadSecurityChecksFailed,
                        "Server did not return a number of ServerEndpoints that matches the one from GetEndpoints.");
                }

                for (var ii = 0; ii < expectedServerEndpoints.Count; ii++)
                {
                    var serverEndpoint = expectedServerEndpoints[ii];
                    var expectedServerEndpoint = m_discoveryServerEndpoints[ii];

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

            var foundDescription = FindMatchingDescription(serverEndpoints, m_endpoint.Description, true);
            if (foundDescription != null)
            {
                found = true;
                // ensure endpoint has up to date information.
                UpdateDescription(m_endpoint.Description, foundDescription);
            }
            else
            {
                foundDescription = FindMatchingDescription(serverEndpoints, m_endpoint.Description, false);
                if (foundDescription != null)
                {
                    found = true;
                    // ensure endpoint has up to date information.
                    UpdateDescription(m_endpoint.Description, foundDescription);
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
                    serverEndpoint.SecurityPolicyUri == m_endpoint.Description.SecurityPolicyUri &&
                    serverEndpoint.SecurityMode == m_endpoint.Description.SecurityMode)
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
            m_logger.LogInformation("Session RECONNECT {Session} starting.", SessionId);

            // create the client signature.
            var dataToSign = Utils.Append(m_serverCertificate?.RawData, m_serverNonce);
            var endpoint = m_endpoint.Description;
            var clientSignature = SecurityPolicies.Sign(m_instanceCertificate, endpoint.SecurityPolicyUri, dataToSign);

            var identityPolicy = m_endpoint.Description.FindUserTokenPolicy(m_identity.PolicyId);

            if (identityPolicy == null)
            {
                m_logger.LogError("Reconnect: Endpoint does not support the user identity type provided.");

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
            if (m_RenewUserIdentity != null)
            {
                m_identity = m_RenewUserIdentity(this, m_identity);
            }

            // validate server nonce and security parameters for user identity.
            ValidateServerNonce(
                m_identity,
                m_serverNonce,
                securityPolicyUri,
                m_previousServerNonce,
                m_endpoint.Description.SecurityMode);

            // sign data with user token.
            var identityToken = m_identity.GetIdentityToken();
            identityToken.PolicyId = identityPolicy.PolicyId;
            var userTokenSignature = identityToken.Sign(dataToSign, securityPolicyUri);

            // encrypt token.
            identityToken.Encrypt(m_serverCertificate, m_serverNonce, securityPolicyUri);

            // send the software certificates assigned to the client.
            var clientSoftwareCertificates = GetSoftwareCertificates();

            m_logger.LogInformation("Session REPLACING channel for {Session}.", SessionId);

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
                        m_configuration,
                        connection,
                        m_endpoint.Description,
                        m_endpoint.Configuration,
                        m_instanceCertificate,
                        m_configuration.SecurityConfiguration.SendCertificateChain ? m_instanceCertificateChain : null,
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
                        m_configuration,
                        m_endpoint.Description,
                        m_endpoint.Configuration,
                        m_instanceCertificate,
                        m_configuration.SecurityConfiguration.SendCertificateChain ? m_instanceCertificateChain : null,
                        MessageContext);

                    // disposes the existing channel.
                    TransportChannel = channel;
                }
            }

            m_logger.LogInformation("Session RE-ACTIVATING {Session}.", SessionId);

            var header = new RequestHeader() { TimeoutHint = kReconnectTimeout };
            return BeginActivateSession(
                header,
                clientSignature,
                null,
                m_preferredLocales,
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
                    m_logger.LogWarning("Message {SubscriptionId}-{SeqNumber} no longer available.", subscriptionId, sequenceNumber);
                    break;

                // if encoding limits are exceeded, the issue is logged and
                // the published data is acknowledged to prevent the endless republish loop.
                case StatusCodes.BadEncodingLimitsExceeded:
                    m_logger.LogError(e, "Message {SubscriptionId}-{SeqNumber} exceeded size limits, ignored.", subscriptionId, sequenceNumber);
                    lock (m_acknowledgementsToSendLock)
                    {
                        AddAcknowledgementToSend(m_acknowledgementsToSend, subscriptionId, sequenceNumber);
                    }
                    break;

                default:
                    result = false;
                    m_logger.LogError(e, "Unexpected error sending republish request.");
                    break;
            }

            var callback = m_PublishError;

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
                    m_logger.LogError(e2, "Session: Unexpected error invoking PublishErrorCallback.");
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
            var validator = m_configuration.CertificateValidator;

            // validate software certificates.
            var softwareCertificates = new List<SoftwareCertificate>();

            foreach (var signedCertificate in serverSoftwareCertificates)
            {
                SoftwareCertificate softwareCertificate = null;

                var result = SoftwareCertificate.Validate(
                    validator,
                    signedCertificate.CertificateData,
                    out softwareCertificate);

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
            OnKeepAlive(m_serverState, responseHeader.Timestamp);

            // collect the current set of acknowledgements.
            lock (m_acknowledgementsToSendLock)
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
                for (var ii = 0; ii < m_acknowledgementsToSend.Count; ii++)
                {
                    var acknowledgement = m_acknowledgementsToSend[ii];

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
                        m_logger.LogWarning("SessionId {Id}, SubscriptionId {SubscriptionId}, Sequence number={SeqNumber} was not received in the available sequence numbers.", SessionId, subscriptionId, acknowledgement.SequenceNumber);
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
                            m_logger.LogWarning("SessionId {Id}, SubscriptionId {SubscriptionId}, Sequence number={SeqNumber} was outdated, acknowledged.", SessionId, subscriptionId, sequenceNumber);
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
                        if (m_latestAcknowledgementsSent.ContainsKey(subscriptionId))
                        {
                            lastSentSequenceNumber = m_latestAcknowledgementsSent[subscriptionId];
                            // If the last sent sequence number is uint.Max do not display the warning; the counter rolled over
                            // If the last sent sequence number is greater or equal to the available sequence number (returned by the publish),
                            // a warning must be logged.
                            if (((lastSentSequenceNumber >= availableSequenceNumber) && (lastSentSequenceNumber != uint.MaxValue)) ||
                                (lastSentSequenceNumber == availableSequenceNumber) && (lastSentSequenceNumber == uint.MaxValue))
                            {
                                m_logger.LogWarning("Received sequence number which was already acknowledged={SeqNumber}", availableSequenceNumber);
                            }
                        }
                    }
                }

                if (m_latestAcknowledgementsSent.ContainsKey(subscriptionId))
                {
                    lastSentSequenceNumber = m_latestAcknowledgementsSent[subscriptionId];

                    // If the last sent sequence number is uint.Max do not display the warning; the counter rolled over
                    // If the last sent sequence number is greater or equal to the notificationMessage's sequence number (returned by the publish),
                    // a warning must be logged.
                    if (((lastSentSequenceNumber >= notificationMessage.SequenceNumber) && (lastSentSequenceNumber != uint.MaxValue)) || (lastSentSequenceNumber == notificationMessage.SequenceNumber) && (lastSentSequenceNumber == uint.MaxValue))
                    {
                        m_logger.LogWarning("Received sequence number which was already acknowledged={SeqNumber}", notificationMessage.SequenceNumber);
                    }
                }
#endif

                m_acknowledgementsToSend = acknowledgementsToSend;

                if (notificationMessage.IsEmpty)
                {
                    m_logger.LogTrace("Empty notification message received for SessionId {Id} with PublishTime {PublishTime}", SessionId, notificationMessage.PublishTime.ToLocalTime());
                }
            }

            lock (SyncRoot)
            {
                // find the subscription.
                foreach (var current in m_subscriptions)
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
                    m_logger.LogTrace("PublishTime {PublishTime} in publish response is too old for SubscriptionId {SubscriptionId}.", notificationMessage.PublishTime.ToLocalTime(), subscription.Id);
                }

                // Validate publish time and reject old values.
                if (notificationMessage.PublishTime > DateTime.UtcNow.AddMilliseconds(subscription.CurrentPublishingInterval * subscription.CurrentLifetimeCount))
                {
                    m_logger.LogTrace("PublishTime {PublishTime} in publish response is newer than actual time for SubscriptionId {SubscriptionId}.", notificationMessage.PublishTime.ToLocalTime(), subscription.Id);
                }

                // update subscription cache.
                subscription.SaveMessageInCache(
                    availableSequenceNumbers,
                    notificationMessage,
                    responseHeader.StringTable);

                // raise the notification.
                var publishEventHandler = m_Publish;
                if (publishEventHandler != null)
                {
                    var args = new NotificationEventArgs(subscription, notificationMessage, responseHeader.StringTable);

                    Task.Run(() => OnRaisePublishNotification(publishEventHandler, args));
                }
            }
            else
            {
                if (DeleteSubscriptionsOnClose && !m_reconnecting)
                {
                    // Delete abandoned subscription from server.
                    m_logger.LogWarning("Received Publish Response for Unknown SubscriptionId={SubscriptionId}. Deleting abandoned subscription from server.", subscriptionId);

                    Task.Run(() => DeleteSubscriptionAsync(subscriptionId, default));
                }
                else
                {
                    // Do not delete publish requests of stale subscriptions
                    m_logger.LogWarning("Received Publish Response for Unknown SubscriptionId={SubscriptionId}. Ignored.", subscriptionId);
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
                m_logger.LogError(e, "Session: Unexpected error while raising Notification event.");
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
                m_logger.LogInformation("Deleting server subscription for SubscriptionId={SubscriptionId}", subscriptionId);

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
                m_logger.LogError(e, "Session: Unexpected error while deleting subscription for SubscriptionId={SubscriptionId}.", subscriptionId);
            }
        }

        /// <summary>
        /// Load certificate for connection.
        /// </summary>
        /// <param name="configuration"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static async Task<X509Certificate2> LoadCertificate(ApplicationConfiguration configuration)
        {
            X509Certificate2 clientCertificate;
            if (configuration.SecurityConfiguration.ApplicationCertificate == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadConfigurationError, "ApplicationCertificate must be specified.");
            }

            clientCertificate = await configuration.SecurityConfiguration.ApplicationCertificate.Find(true).ConfigureAwait(false);

            if (clientCertificate == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadConfigurationError, "ApplicationCertificate cannot be found.");
            }
            return clientCertificate;
        }

        /// <summary>
        /// Load certificate chain for connection.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="clientCertificate"></param>
        private static async Task<X509Certificate2Collection> LoadCertificateChain(ApplicationConfiguration configuration, X509Certificate2 clientCertificate)
        {
            X509Certificate2Collection clientCertificateChain = null;
            // load certificate chain.
            if (configuration.SecurityConfiguration.SendCertificateChain)
            {
                clientCertificateChain = new X509Certificate2Collection(clientCertificate);
                var issuers = new List<CertificateIdentifier>();
                await configuration.CertificateValidator.GetIssuers(clientCertificate, issuers).ConfigureAwait(false);

                for (var i = 0; i < issuers.Count; i++)
                {
                    clientCertificateChain.Add(issuers[i].Certificate);
                }
            }
            return clientCertificateChain;
        }

        private void AddAcknowledgementToSend(SubscriptionAcknowledgementCollection acknowledgementsToSend, uint subscriptionId, uint sequenceNumber)
        {
            ArgumentNullException.ThrowIfNull(acknowledgementsToSend);

            Debug.Assert(Monitor.IsEntered(m_acknowledgementsToSendLock));

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
            return (m_tooManyPublishRequests == 0) ||
                (requestCount < m_tooManyPublishRequests);
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
                if (m_subscriptions.Count == 0)
                {
                    return 0;
                }

                int publishCount;

                if (createdOnly)
                {
                    var count = 0;
                    foreach (var subscription in m_subscriptions)
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
                    publishCount = m_subscriptions.Count;
                }

                //
                // If a dynamic limit was set because of badTooManyPublishRequest error.
                // limit the number of publish requests to this value.
                //
                if (m_tooManyPublishRequests > 0 && publishCount > m_tooManyPublishRequests)
                {
                    publishCount = m_tooManyPublishRequests;
                }

                //
                // Limit resulting to a number between min and max request count.
                // If max is below min, we honor the min publish request count.
                // See return from MinPublishRequestCount property which the max of both.
                //
                if (publishCount > m_maxPublishRequestCount)
                {
                    publishCount = m_maxPublishRequestCount;
                }
                if (publishCount < m_minPublishRequestCount)
                {
                    publishCount = m_minPublishRequestCount;
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
                m_SessionConfigurationChanged?.Invoke(this, EventArgs.Empty);
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
        /// The period for which the server will maintain the session if there is no communication from the client.
        /// </summary>
        protected double m_sessionTimeout;

        /// <summary>
        /// The locales that the server should use when returning localized text.
        /// </summary>
        protected StringCollection m_preferredLocales;

        /// <summary>
        /// The Application Configuration.
        /// </summary>
        protected ApplicationConfiguration m_configuration;

        /// <summary>
        /// The endpoint used to connect to the server.
        /// </summary>
        protected ConfiguredEndpoint m_endpoint;

        /// <summary>
        /// The Instance Certificate.
        /// </summary>
        protected X509Certificate2 m_instanceCertificate;

        /// <summary>
        /// The Instance Certificate Chain.
        /// </summary>
        protected X509Certificate2Collection m_instanceCertificateChain;

        /// <summary>
        /// If set to<c>true</c> then the domain in the certificate must match the endpoint used.
        /// </summary>
        protected bool m_checkDomain;

        /// <summary>
        /// The name assigned to the session.
        /// </summary>
        protected string m_sessionName;

        /// <summary>
        /// The user identity currently used for the session.
        /// </summary>
        protected IUserIdentity m_identity;
        private SubscriptionAcknowledgementCollection m_acknowledgementsToSend;
        private object m_acknowledgementsToSendLock;
#if DEBUG_SEQUENTIALPUBLISHING
        private Dictionary<uint, uint> m_latestAcknowledgementsSent;
#endif
        private List<Subscription> m_subscriptions;
        private uint m_maxRequestMessageSize;
        private NamespaceTable m_namespaceUris;
        private StringTable m_serverUris;
        private IEncodeableFactory m_factory;
        private SystemContext m_systemContext;
        private NodeCache m_nodeCache;
        private byte[] m_serverNonce;
        private byte[] m_previousServerNonce;
        private X509Certificate2 m_serverCertificate;
        private long m_publishCounter;
        private int m_tooManyPublishRequests;
        private long m_lastKeepAliveTime;
        private int m_lastKeepAliveTickCount;
        private StatusCode m_lastKeepAliveErrorStatusCode;
        private ServerState m_serverState;
        private int m_keepAliveInterval;
#if PERIODIC_TIMER
        private PeriodicTimer m_keepAliveTimer;
#else
        private Timer m_keepAliveTimer;
#endif
        private long m_keepAliveCounter;
        private bool m_reconnecting;
        private SemaphoreSlim m_reconnectLock;
        private int m_minPublishRequestCount;
        private int m_maxPublishRequestCount;
        private LinkedList<AsyncRequestState> m_outstandingRequests;
        private readonly EndpointDescriptionCollection m_discoveryServerEndpoints;
        private readonly StringCollection m_discoveryProfileUris;
        private readonly ILogger m_logger;

        private class AsyncRequestState
        {
            public uint RequestTypeId;
            public uint RequestId;
            public int TickCount;
            public IAsyncResult Result;
            public bool Defunct;
        }

        private event KeepAliveEventHandler m_KeepAlive;
        private event NotificationEventHandler m_Publish;
        private event PublishErrorEventHandler m_PublishError;
        private event PublishSequenceNumbersToAcknowledgeEventHandler m_PublishSequenceNumbersToAcknowledge;
        private event EventHandler m_SubscriptionsChanged;
        private event EventHandler m_SessionClosing;
        private event EventHandler m_SessionConfigurationChanged;
    }
}
