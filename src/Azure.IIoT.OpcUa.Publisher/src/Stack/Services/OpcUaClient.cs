// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Exceptions;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Logging;
    using Nito.AsyncEx;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// OPC UA Client based on official ua client reference sample.
    /// </summary>
    public sealed class OpcUaClient : IAsyncDisposable, ISessionHandle,
        IMetricsContext, ISessionServices
    {
        /// <inheritdoc/>
        public event EventHandler<EndpointConnectivityState>? OnConnectionStateChange;

        /// <inheritdoc/>
        public IVariantEncoder Codec { get; private set; }

        /// <inheritdoc/>
        public ISessionServices Services => this;

        /// <inheritdoc/>
        public ITypeTable TypeTree
            => _session?.TypeTree ?? kTypeTree;

        /// <inheritdoc/>
        public IServiceMessageContext MessageContext
            => _session?.MessageContext ?? kMessageContext;

        /// <inheritdoc/>
        public ISystemContext SystemContext
            => _session?.SystemContext ?? kSystemContext;

        /// <inheritdoc/>
        public TagList TagList { get; }

        /// <summary>
        /// The session keepalive interval to be used in ms.
        /// </summary>
        public int KeepAliveInterval { get; set; } = 5000;

        /// <summary>
        /// The reconnect period to be used in ms.
        /// </summary>
        public int ReconnectPeriod { get; set; } = 1000;

        /// <summary>
        /// The session lifetime.
        /// </summary>
        public uint SessionLifeTime { get; set; } = 30 * 1000;

        /// <summary>
        /// The file to use for log output.
        /// </summary>
        public int NumberOfConnectRetries { get; internal set; }

        /// <summary>
        /// Is reconnecting
        /// </summary>
        public bool IsReconnecting => _reconnectHandler != null;

        /// <summary>
        /// Is reconnecting
        /// </summary>
        internal bool IsConnected => _lastState == EndpointConnectivityState.Ready;

        /// <summary>
        /// Whether the connect operation is in progress
        /// </summary>
        internal bool HasSubscriptions => !_subscriptions.IsEmpty;

        /// <summary>
        /// Check if session is active
        /// </summary>
        public bool IsActive => HasSubscriptions ||
            _lastActivity + TimeSpan.FromSeconds(SessionLifeTime) > DateTime.UtcNow;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="connection"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        /// <param name="sessionName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OpcUaClient(ApplicationConfiguration configuration,
            ConnectionIdentifier connection, IJsonSerializer serializer,
            ILogger logger, IMetricsContext metrics, string? sessionName = null)
        {
            if (connection?.Connection?.Endpoint == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            _authenticationToken = NodeId.Null;
            _connection = connection.Connection;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            _needsConnecting = true;
            _lastState = EndpointConnectivityState.Connecting;
            _sessionName = sessionName ?? connection.ToString();
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            Codec = CreateCodec();
            TagList = new TagList(metrics.TagList.ToArray().AsSpan()) {
                { "EndpointUrl", _connection.Endpoint.Url },
                { "SecurityMode", _connection.Endpoint.SecurityMode }
            };
            InitializeMetrics();
        }

        /// <inheritdoc/>
        public IDisposable GetSession(out ISession? session)
        {
            var activity = new SessionActivity<IServiceResponse>(_lock.ReaderLock(),
                "Raw Session access.", _session!);
            session = activity.Session;
            return activity;
        }

        /// <inheritdoc/>
        public void RegisterSubscription(ISubscription subscription)
        {
            var id = new ConnectionIdentifier(subscription.Connection);
            try
            {
                _subscriptions.AddOrUpdate(subscription.Name, subscription, (_, _) => subscription);
                _logger.LogInformation(
                    "Subscription {Subscription} registered/updated in session {Session}.",
                    subscription.Name, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register subscription");
            }
        }

        /// <inheritdoc/>
        public void UnregisterSubscription(ISubscription subscription)
        {
            if (_subscriptions.TryRemove(subscription.Name, out _))
            {
                _logger.LogInformation(
                    "Subscription {Subscription} unregistered from session {Session}.",
                    subscription.Name, _sessionName);
            }
        }

        /// <summary>
        /// Connect the client. Returns true if connection was established.
        /// </summary>
        /// <param name="reapplySubscriptionState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask<bool> ConnectAsync(bool reapplySubscriptionState = false,
            CancellationToken ct = default)
        {
            if (!_needsConnecting)
            {
                return true;
            }

            _needsConnecting = false;
            bool connected;
            using (var writerlock = await _lock.WriterLockAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    connected = await ConnectInternalAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Log Error
                    _logger.LogError(ex, "Error creating session {Name}.", _sessionName);
                    _session?.Dispose();
                    _session = null;
                    Codec = CreateCodec();
                    connected = false;
                    _needsConnecting = true;
                }
            }

            if (connected && reapplySubscriptionState)
            {
                //
                // Apply subscription settings for existing subscriptions
                // This will take the subscription lock, since the connect
                // can be called under it the default should be false.
                // Only if the manager task calls connect we should do this.
                //
                foreach (var subscription in _subscriptions.Values)
                {
                    await subscription.ReapplyToSessionAsync(this).ConfigureAwait(false);
                }
                _logger.LogInformation("Reapplied all subscriptions to session {Name}.",
                    _sessionName);
            }

            NotifySubscriptionStateChange(connected);
            return connected;
        }

        /// <inheritdoc/>
        public async ValueTask<OperationLimitsModel> GetOperationLimitsAsync(
            CancellationToken ct = default)
        {
            if (_limits != null)
            {
                return _limits;
            }
            _limits = await FetchOperationLimitsAsync(new RequestHeader(),
                ct).ConfigureAwait(false);
            return _limits ?? new OperationLimitsModel();
        }

        /// <inheritdoc/>
        public async ValueTask<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            CancellationToken ct = default)
        {
            if (_server != null)
            {
                return _server;
            }
            if (_limits == null)
            {
                _limits = await FetchOperationLimitsAsync(new RequestHeader(),
                    ct).ConfigureAwait(false);
            }
            _server = await FetchServerCapabilitiesAsync(new RequestHeader(),
                ct).ConfigureAwait(false);
            return _server ?? new ServerCapabilitiesModel
            {
                OperationLimits = _limits ?? new OperationLimitsModel()
            };
        }

        /// <inheritdoc/>
        public async ValueTask<HistoryServerCapabilitiesModel> GetHistoryCapabilitiesAsync(
            CancellationToken ct = default)
        {
            if (_history != null)
            {
                return _history;
            }
            _history = await FetchHistoryCapabilitiesAsync(new RequestHeader(),
                ct).ConfigureAwait(false);
            return _history ?? new HistoryServerCapabilitiesModel();
        }

        /// <inheritdoc/>
        public async Task<T> RunAsync<T>(Func<ISessionHandle, Task<T>> service,
            CancellationToken ct)
        {
            while (true)
            {
                await _connected.WaitAsync(ct).ConfigureAwait(false);
                if (_disposed)
                {
                    throw new ConnectionException(
                        $"Session {_sessionName} was closed.");
                }
                if (_session?.Connected != true)
                {
                    _logger.LogInformation(
                        "Connected signaled but not connected, retry in 5 seconds...");
                    // Delay and try again
                    await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                    continue;
                }
                try
                {
                    return await service(this).ConfigureAwait(false);
                }
                catch (Exception ex) when (!_session.Connected)
                {
                    _logger.LogInformation("Session disconnected during service call " +
                        "with message {Message}, retrying.", ex.Message);
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(_sessionName);
            }
            _disposed = true;

            Session? session;
            List<ISubscription>? subscriptions;
            using (var writerlock = await _lock.WriterLockAsync().ConfigureAwait(false))
            {
                NumberOfConnectRetries = 0;
                _lastState = EndpointConnectivityState.Disconnected;
                _reconnectHandler?.Dispose();
                subscriptions = _subscriptions.Values.ToList();
                _subscriptions.Clear();
                session = _session;
                UnsetSession(true);

                _connected.Set(); // Release any waiting tasks with exception
            }

            if (session == null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Closing session {Name}...", _sessionName);

                if (subscriptions.Count > 0)
                {
                    //
                    // Close all subscriptions. Since this might call back into
                    // the session manager and we are under the lock, queue this
                    // to the thread pool to execute after
                    //
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        foreach (var subscription in _subscriptions.Values)
                        {
                            Try.Op(() => subscription.Dispose());
                        }
                    });
                }

                await session.CloseAsync().ConfigureAwait(false);

                // Log Session Disconnected event
                _logger.LogDebug("Session {Name} closed.", _sessionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during closing of session {Name}.", _sessionName);
            }
            finally
            {
                session.Dispose();
            }
        }

        /// <summary>
        /// Connect client (no lock)
        /// </summary>
        /// <returns></returns>
        private async ValueTask<bool> ConnectInternalAsync()
        {
            if (IsReconnecting)
            {
                // Cannot connect while reconnecting.
                _logger.LogInformation("Session {Name} is reconnecting. Not connecting.",
                    _sessionName);
                return false;
            }

            if (IsConnected)
            {
                // Nothing to do but try ensure complex type system is loaded
                if (_complexTypeSystem?.IsCompleted == false)
                {
                    await _complexTypeSystem.ConfigureAwait(false);
                }
                return true;
            }

            UnsetSession(); // Ensure any previous session is disposed here.
            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);
            _logger.LogDebug("--- SESSION {Name} CONNECTING... ---", _sessionName);

            var endpointUrlCandidates = _connection.Endpoint!.Url.YieldReturn();
            if (_connection.Endpoint.AlternativeUrls != null)
            {
                endpointUrlCandidates = endpointUrlCandidates.Concat(
                    _connection.Endpoint.AlternativeUrls);
            }
            var attempt = 0;
            foreach (var endpointUrl in endpointUrlCandidates)
            {
                try
                {
                    //
                    // Get the endpoint by connecting to server's discovery endpoint.
                    // Try to find the first endpoint with security.
                    //
                    var endpointDescription = CoreClientUtils.SelectEndpoint(
                        _configuration, endpointUrl,
                        _connection.Endpoint.SecurityMode != SecurityMode.None);
                    var endpointConfiguration = EndpointConfiguration.Create(
                        _configuration);
                    var endpoint = new ConfiguredEndpoint(null, endpointDescription,
                        endpointConfiguration);

                    if (_connection.Endpoint.SecurityMode.HasValue &&
                        _connection.Endpoint.SecurityMode != SecurityMode.None &&
                        endpointDescription.SecurityMode == MessageSecurityMode.None)
                    {
                        _logger.LogWarning("Although the use of security was configured, " +
                            "there was no security-enabled endpoint available at url " +
                            "{EndpointUrl}. An endpoint with no security will be used.",
                            endpointUrl);
                    }

                    _logger.LogInformation(
                        "#{Attempt}: Creating session {Name} for endpoint {EndpointUrl}...",
                        ++attempt, _sessionName, endpointUrl);
                    var userIdentity = _connection.User.ToStackModel()
                        ?? new UserIdentity(new AnonymousIdentityToken());

                    // Create the session with english as default and current language
                    // locale as backup
                    var preferredLocales = new HashSet<string>
                    {
                        "en-US",
                        CultureInfo.CurrentCulture.Name
                    }.ToList();
                    var session = await Session.Create(_configuration, endpoint,
                        false, false, _sessionName, SessionLifeTime, userIdentity,
                        preferredLocales).ConfigureAwait(false);

                    // Assign the created session
                    SetSession(session);
                    _logger.LogInformation(
                        "New Session {Name} created with endpoint {EndpointUrl} ({Original}).",
                        _sessionName, endpointUrl, _connection.Endpoint.Url);

                    _limits = await FetchOperationLimitsAsync(new RequestHeader(), default).ConfigureAwait(false);

                    // Try and load type system - does not throw but logs error
                    Debug.Assert(_complexTypeSystem != null);
                    await _complexTypeSystem.ConfigureAwait(false);
                    NumberOfConnectRetries++;

                    _logger.LogInformation("--- SESSION {Name} CONNECTED ---", _sessionName);
                    return true;
                }
                catch (Exception ex)
                {
                    NotifyConnectivityStateChange(ToConnectivityState(ex));
                    NumberOfConnectRetries++;
                    _logger.LogInformation(
                        "#{Attempt}: Failed to create session {Name} to {EndpointUrl}: {Message}...",
                        ++attempt, _sessionName, endpointUrl, ex.Message);
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public async ValueTask<ComplexTypeSystem?> GetComplexTypeSystemAsync()
        {
            using var readerlock = await _lock.ReaderLockAsync().ConfigureAwait(false);
            try
            {
                _complexTypeSystem ??= LoadComplexTypeSystemAsync();
                return await _complexTypeSystem.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get complex type system for session {Name}.",
                    _sessionName);
                return null;
            }
        }

        /// <summary>
        /// Invokes the AddNodes service using async Task based request.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToAdd"></param>
        /// <param name="ct"></param>
        public async Task<AddNodesResponse> AddNodesAsync(RequestHeader requestHeader,
            AddNodesItemCollection nodesToAdd, CancellationToken ct)
        {
            using var activity = await BeginAsync<AddNodesResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new AddNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToAdd = nodesToAdd
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<AddNodesResponse>(response);
        }

        /// <summary>
        /// Invokes the AddReferences service using async Task based request.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="referencesToAdd"></param>
        /// <param name="ct"></param>
        public async Task<AddReferencesResponse> AddReferencesAsync(
            RequestHeader requestHeader, AddReferencesItemCollection referencesToAdd,
            CancellationToken ct)
        {
            using var activity = await BeginAsync<AddReferencesResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new AddReferencesRequest
            {
                RequestHeader = requestHeader,
                ReferencesToAdd = referencesToAdd
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<AddReferencesResponse>(response);
        }

        /// <summary>
        /// Invokes the DeleteNodes service using async Task based request.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToDelete"></param>
        /// <param name="ct"></param>
        public async Task<DeleteNodesResponse> DeleteNodesAsync(
            RequestHeader requestHeader, DeleteNodesItemCollection nodesToDelete,
            CancellationToken ct)
        {
            using var activity = await BeginAsync<DeleteNodesResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new DeleteNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToDelete = nodesToDelete
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<DeleteNodesResponse>(response);
        }

        /// <summary>
        /// Invokes the DeleteReferences service using async Task based request.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="referencesToDelete"></param>
        /// <param name="ct"></param>
        public async Task<DeleteReferencesResponse> DeleteReferencesAsync(
            RequestHeader requestHeader, DeleteReferencesItemCollection referencesToDelete,
            CancellationToken ct)
        {
            using var activity = await BeginAsync<DeleteReferencesResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new DeleteReferencesRequest
            {
                RequestHeader = requestHeader,
                ReferencesToDelete = referencesToDelete
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<DeleteReferencesResponse>(response);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="requestedMaxReferencesPerNode"></param>
        /// <param name="nodesToBrowse"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<BrowseResponse> BrowseAsync(
            RequestHeader requestHeader, ViewDescription? view,
            uint requestedMaxReferencesPerNode,
            BrowseDescriptionCollection nodesToBrowse, CancellationToken ct)
        {
            using var activity = await BeginAsync<BrowseResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new BrowseRequest
            {
                RequestHeader = requestHeader,
                View = view,
                RequestedMaxReferencesPerNode = requestedMaxReferencesPerNode,
                NodesToBrowse = nodesToBrowse
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<BrowseResponse>(response);
        }

        /// <summary>
        /// Invokes the BrowseNext service using async Task based request.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="continuationPoints"></param>
        /// <param name="ct"></param>
        public async Task<BrowseNextResponse> BrowseNextAsync(
            RequestHeader requestHeader, bool releaseContinuationPoints,
            ByteStringCollection continuationPoints, CancellationToken ct)
        {
            using var activity = await BeginAsync<BrowseNextResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new BrowseNextRequest
            {
                RequestHeader = requestHeader,
                ReleaseContinuationPoints = releaseContinuationPoints,
                ContinuationPoints = continuationPoints
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<BrowseNextResponse>(response);
        }

        /// <summary>
        /// Translate browse paths
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="browsePaths"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<TranslateBrowsePathsToNodeIdsResponse> TranslateBrowsePathsToNodeIdsAsync(
            RequestHeader requestHeader, BrowsePathCollection browsePaths,
            CancellationToken ct)
        {
            using var activity = await BeginAsync<TranslateBrowsePathsToNodeIdsResponse>(
                requestHeader, ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new TranslateBrowsePathsToNodeIdsRequest
            {
                RequestHeader = requestHeader,
                BrowsePaths = browsePaths
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<TranslateBrowsePathsToNodeIdsResponse>(response);
        }

        /// <summary>
        /// Register nodes
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToRegister"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<RegisterNodesResponse> RegisterNodesAsync(
            RequestHeader requestHeader, NodeIdCollection nodesToRegister,
            CancellationToken ct)
        {
            using var activity = await BeginAsync<RegisterNodesResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new RegisterNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToRegister = nodesToRegister
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<RegisterNodesResponse>(response);
        }

        /// <summary>
        /// Invokes the UnregisterNodes service using async Task based request.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToUnregister"></param>
        /// <param name="ct"></param>
        public async Task<UnregisterNodesResponse> UnregisterNodesAsync(
            RequestHeader requestHeader, NodeIdCollection nodesToUnregister,
            CancellationToken ct)
        {
            using var activity = await BeginAsync<UnregisterNodesResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new UnregisterNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToUnregister = nodesToUnregister
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<UnregisterNodesResponse>(response);
        }

        /// <summary>
        /// Invokes the QueryFirst service using async Task based request.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodeTypes"></param>
        /// <param name="filter"></param>
        /// <param name="maxDataSetsToReturn"></param>
        /// <param name="maxReferencesToReturn"></param>
        /// <param name="ct"></param>
        public async Task<QueryFirstResponse> QueryFirstAsync(
            RequestHeader requestHeader, ViewDescription view,
            NodeTypeDescriptionCollection nodeTypes, ContentFilter filter,
            uint maxDataSetsToReturn, uint maxReferencesToReturn,
            CancellationToken ct)
        {
            using var activity = await BeginAsync<QueryFirstResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new QueryFirstRequest
            {
                RequestHeader = requestHeader,
                View = view,
                NodeTypes = nodeTypes,
                Filter = filter,
                MaxDataSetsToReturn = maxDataSetsToReturn,
                MaxReferencesToReturn = maxReferencesToReturn
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<QueryFirstResponse>(response);
        }

        /// <summary>
        /// Invokes the QueryNext service using async Task based request.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="releaseContinuationPoint"></param>
        /// <param name="continuationPoint"></param>
        /// <param name="ct"></param>
        public async Task<QueryNextResponse> QueryNextAsync(
            RequestHeader requestHeader, bool releaseContinuationPoint,
            byte[] continuationPoint, CancellationToken ct)
        {
            using var activity = await BeginAsync<QueryNextResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new QueryNextRequest
            {
                RequestHeader = requestHeader,
                ReleaseContinuationPoint = releaseContinuationPoint,
                ContinuationPoint = continuationPoint
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<QueryNextResponse>(response);
        }

        /// <summary>
        /// Read
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="maxAge"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ReadResponse> ReadAsync(RequestHeader requestHeader,
            double maxAge, Opc.Ua.TimestampsToReturn timestampsToReturn,
            ReadValueIdCollection nodesToRead, CancellationToken ct)
        {
            using var activity = await BeginAsync<ReadResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new ReadRequest
            {
                RequestHeader = requestHeader,
                MaxAge = maxAge,
                TimestampsToReturn = timestampsToReturn,
                NodesToRead = nodesToRead
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<ReadResponse>(response);
        }

        /// <summary>
        /// History read
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="historyReadDetails"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponse> HistoryReadAsync(
            RequestHeader requestHeader, ExtensionObject? historyReadDetails,
            Opc.Ua.TimestampsToReturn timestampsToReturn, bool releaseContinuationPoints,
            HistoryReadValueIdCollection nodesToRead, CancellationToken ct)
        {
            using var activity = await BeginAsync<HistoryReadResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new HistoryReadRequest
            {
                RequestHeader = requestHeader,
                HistoryReadDetails = historyReadDetails,
                TimestampsToReturn = timestampsToReturn,
                ReleaseContinuationPoints = releaseContinuationPoints,
                NodesToRead = nodesToRead
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<HistoryReadResponse>(response);
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToWrite"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<WriteResponse> WriteAsync(RequestHeader requestHeader,
            WriteValueCollection nodesToWrite, CancellationToken ct)
        {
            using var activity = await BeginAsync<WriteResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new WriteRequest
            {
                RequestHeader = requestHeader,
                NodesToWrite = nodesToWrite
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<WriteResponse>(response);
        }

        /// <summary>
        /// History update
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="historyUpdateDetails"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponse> HistoryUpdateAsync(
            RequestHeader requestHeader, ExtensionObjectCollection historyUpdateDetails,
            CancellationToken ct)
        {
            using var activity = await BeginAsync<HistoryUpdateResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new HistoryUpdateRequest
            {
                RequestHeader = requestHeader,
                HistoryUpdateDetails = historyUpdateDetails
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<HistoryUpdateResponse>(response);
        }

        /// <summary>
        /// Call
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="methodsToCall"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<CallResponse> CallAsync(RequestHeader requestHeader,
            CallMethodRequestCollection methodsToCall, CancellationToken ct)
        {
            using var activity = await BeginAsync<CallResponse>(requestHeader,
                ct).ConfigureAwait(false);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new CallRequest
            {
                RequestHeader = requestHeader,
                MethodsToCall = methodsToCall
            };
            var response = await activity.Session.TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return ValidateResponse<CallResponse>(response);
        }

        /// <summary>
        /// Read operation limits
        /// </summary>
        /// <param name="header"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<OperationLimitsModel?> FetchOperationLimitsAsync(RequestHeader header,
            CancellationToken ct)
        {
            var session = _session;
            if (session == null)
            {
                return null;
            }

            // Fetch limits into the session using the new api
            session.FetchOperationLimits();
            var maxNodesPerRead = Validate32(session.OperationLimits.MaxNodesPerRead);

            // Read once more to ensure we have all we need and also correctly show what is not provided.
            var nodes = new[] {
                Variables.Server_ServerCapabilities_MaxArrayLength,
                Variables.Server_ServerCapabilities_MaxBrowseContinuationPoints,
                Variables.Server_ServerCapabilities_MaxByteStringLength,
                Variables.Server_ServerCapabilities_MaxHistoryContinuationPoints,
                Variables.Server_ServerCapabilities_MaxQueryContinuationPoints,
                Variables.Server_ServerCapabilities_MaxStringLength,
                Variables.Server_ServerCapabilities_MinSupportedSampleRate,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryReadData,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryReadEvents,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerWrite,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryUpdateData,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryUpdateEvents,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerMethodCall,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerBrowse,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerRegisterNodes,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerTranslateBrowsePathsToNodeIds,
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerNodeManagement,
                Variables.Server_ServerCapabilities_OperationLimits_MaxMonitoredItemsPerCall
            };

            var values = Enumerable.Empty<DataValue>();
            foreach (var chunk in nodes.Batch(Math.Max(1, (int)(maxNodesPerRead ?? 0))))
            {
                // Group the reads
                var requests = new ReadValueIdCollection(chunk
                    .Select(n => new ReadValueId
                    {
                        NodeId = n,
                        AttributeId = Attributes.Value
                    }));
                var response = await session.ReadAsync(header, 0,
                    Opc.Ua.TimestampsToReturn.Both, requests, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, d => d.StatusCode,
                    response.DiagnosticInfos, requests);
                results.ThrowIfError();
                values = values.Concat(results.Select(r => r.Result));
            }

            var value = values.ToList();
            return new OperationLimitsModel
            {
                MaxArrayLength =
                    Validate32(value[0].GetValueOrDefault<uint?>()),
                MaxBrowseContinuationPoints =
                    Validate16(value[1].GetValueOrDefault<ushort?>()),
                MaxByteStringLength =
                    Validate32(value[2].GetValueOrDefault<uint?>()),
                MaxHistoryContinuationPoints =
                    Validate16(value[3].GetValueOrDefault<ushort?>()),
                MaxQueryContinuationPoints =
                    Validate16(value[4].GetValueOrDefault<ushort?>()),
                MaxStringLength =
                    Validate32(value[5].GetValueOrDefault<uint?>()),
                MinSupportedSampleRate =
                    value[6].GetValueOrDefault<double?>(),
                MaxNodesPerHistoryReadData =
                    Validate32(value[7].GetValueOrDefault<uint?>()),
                MaxNodesPerHistoryReadEvents =
                    Validate32(value[8].GetValueOrDefault<uint?>()),
                MaxNodesPerWrite =
                    Validate32(value[9].GetValueOrDefault<uint?>()),
                MaxNodesPerHistoryUpdateData =
                    Validate32(value[10].GetValueOrDefault<uint?>()),
                MaxNodesPerHistoryUpdateEvents =
                    Validate32(value[11].GetValueOrDefault<uint?>()),
                MaxNodesPerMethodCall =
                    Validate32(value[12].GetValueOrDefault<uint?>()),
                MaxNodesPerBrowse =
                    Validate32(value[13].GetValueOrDefault<uint?>()),
                MaxNodesPerRegisterNodes =
                    Validate32(value[14].GetValueOrDefault<uint?>()),
                MaxNodesPerTranslatePathsToNodeIds =
                    Validate32(value[15].GetValueOrDefault<uint?>()),
                MaxNodesPerNodeManagement =
                    Validate32(value[16].GetValueOrDefault<uint?>()),
                MaxMonitoredItemsPerCall =
                    Validate32(value[17].GetValueOrDefault<uint?>()),
                MaxNodesPerRead = maxNodesPerRead
            };

            static uint? Validate32(uint? v) => v == null ? null :
                v is > 0 and < int.MaxValue ? v : int.MaxValue;
            static ushort? Validate16(ushort? v) => v == null ? null :
                v > 0 ? v : ushort.MaxValue;
        }

        /// <summary>
        /// Read the server capabilities if available
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<ServerCapabilitiesModel?> FetchServerCapabilitiesAsync(
            RequestHeader requestHeader, CancellationToken ct)
        {
            var session = _session;
            if (session == null)
            {
                return null;
            }
            // load the defaults for the historical capabilities object.
            var config =
                new ServerCapabilitiesState(null);
            config.ServerProfileArray =
                new PropertyState<string[]>(config);
            config.LocaleIdArray =
                new PropertyState<string[]>(config);
            config.SoftwareCertificates =
                new PropertyState<SignedSoftwareCertificate[]>(config);
            config.ModellingRules =
                new FolderState(config);
            config.AggregateFunctions =
                new FolderState(config);
            config.Create(session.SystemContext, null,
                BrowseNames.ServerCapabilities, null, false);

            var relativePath = new RelativePath();
            relativePath.Elements.Add(new RelativePathElement
            {
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                IsInverse = false,
                IncludeSubtypes = false,
                TargetName = BrowseNames.ServerCapabilities
            });
            var errorInfo = await this.ReadNodeStateAsync(requestHeader, config,
                Objects.Server, relativePath, ct).ConfigureAwait(false);
            if (errorInfo != null)
            {
                return null;
            }

            var functions = new List<BaseInstanceState>();
            config.AggregateFunctions.GetChildren(session.SystemContext, functions);
            var aggregateFunctions = functions.OfType<BaseObjectState>().ToDictionary(
                c => c.BrowseName.AsString(session.MessageContext),
                c => c.NodeId.AsString(session.MessageContext) ?? string.Empty);
            var rules = new List<BaseInstanceState>();
            config.ModellingRules.GetChildren(session.SystemContext, rules);
            var modellingRules = rules.OfType<BaseObjectState>().ToDictionary(
                c => c.BrowseName.AsString(session.MessageContext),
                c => c.NodeId.AsString(session.MessageContext) ?? string.Empty);
            return new ServerCapabilitiesModel
            {
                OperationLimits = _limits ?? new OperationLimitsModel(),
                ModellingRules =
                    modellingRules.Count == 0 ? null : modellingRules,
                SupportedLocales =
                    config.LocaleIdArray.GetValueOrDefault(
                        v => v == null || v.Length == 0 ? null : v),
                ServerProfiles =
                    config.ServerProfileArray.GetValueOrDefault(
                        v => v == null || v.Length == 0 ? null : v),
                AggregateFunctions =
                    aggregateFunctions.Count == 0 ? null : aggregateFunctions
            };
        }

        /// <summary>
        /// Read the history server capabilities if available
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<HistoryServerCapabilitiesModel?> FetchHistoryCapabilitiesAsync(
            RequestHeader requestHeader, CancellationToken ct)
        {
            var session = _session;
            if (session == null)
            {
                return null;
            }
            // load the defaults for the historical capabilities object.
            var config =
                new HistoryServerCapabilitiesState(null);
            config.AccessHistoryDataCapability =
                new PropertyState<bool>(config);
            config.AccessHistoryEventsCapability =
                new PropertyState<bool>(config);
            config.MaxReturnDataValues =
                new PropertyState<uint>(config);
            config.MaxReturnEventValues =
                new PropertyState<uint>(config);
            config.InsertDataCapability =
                new PropertyState<bool>(config);
            config.ReplaceDataCapability =
                new PropertyState<bool>(config);
            config.UpdateDataCapability =
                new PropertyState<bool>(config);
            config.DeleteRawCapability =
                new PropertyState<bool>(config);
            config.DeleteAtTimeCapability =
                new PropertyState<bool>(config);
            config.InsertEventCapability =
                new PropertyState<bool>(config);
            config.ReplaceEventCapability =
                new PropertyState<bool>(config);
            config.UpdateEventCapability =
                new PropertyState<bool>(config);
            config.DeleteEventCapability =
                new PropertyState<bool>(config);
            config.InsertAnnotationCapability =
                new PropertyState<bool>(config);
            config.ServerTimestampSupported =
                new PropertyState<bool>(config);
            config.AggregateFunctions =
                new FolderState(config);
            config.Create(session.SystemContext, null,
                BrowseNames.HistoryServerCapabilities, null, false);

            var relativePath = new RelativePath();
            relativePath.Elements.Add(new RelativePathElement
            {
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                IsInverse = false,
                IncludeSubtypes = false,
                TargetName = BrowseNames.HistoryServerCapabilities
            });
            var errorInfo = await this.ReadNodeStateAsync(requestHeader, config,
                Objects.Server_ServerCapabilities, relativePath, ct).ConfigureAwait(false);
            if (errorInfo != null)
            {
                return null;
            }
            var supportsValues =
              config.AccessHistoryDataCapability.GetValueOrDefault() ?? false;
            var supportsEvents =
                config.AccessHistoryEventsCapability.GetValueOrDefault() ?? false;
            Dictionary<string, string>? aggregateFunctions = null;
            if (supportsEvents || supportsValues)
            {
                var children = new List<BaseInstanceState>();
                config.AggregateFunctions.GetChildren(session.SystemContext, children);
                aggregateFunctions = children.OfType<BaseObjectState>().ToDictionary(
                    c => c.BrowseName.AsString(session.MessageContext),
                    c => c.NodeId.AsString(session.MessageContext) ?? string.Empty);
            }
            return new HistoryServerCapabilitiesModel
            {
                AccessHistoryDataCapability =
                    supportsValues,
                AccessHistoryEventsCapability =
                    supportsEvents,
                MaxReturnDataValues =
                    config.MaxReturnDataValues.GetValueOrDefault(
                        v => !supportsValues ? null : v == 0 ? uint.MaxValue : v),
                MaxReturnEventValues =
                    config.MaxReturnEventValues.GetValueOrDefault(
                        v => !supportsEvents ? null : v == 0 ? uint.MaxValue : v),
                InsertDataCapability =
                    config.InsertDataCapability.GetValueOrDefault(),
                ReplaceDataCapability =
                    config.ReplaceDataCapability.GetValueOrDefault(),
                UpdateDataCapability =
                    config.UpdateDataCapability.GetValueOrDefault(),
                DeleteRawCapability =
                    config.DeleteRawCapability.GetValueOrDefault(),
                DeleteAtTimeCapability =
                    config.DeleteAtTimeCapability.GetValueOrDefault(),
                InsertEventCapability =
                    config.InsertEventCapability.GetValueOrDefault(),
                ReplaceEventCapability =
                    config.ReplaceEventCapability.GetValueOrDefault(),
                UpdateEventCapability =
                    config.UpdateEventCapability.GetValueOrDefault(),
                DeleteEventCapability =
                    config.DeleteEventCapability.GetValueOrDefault(),
                InsertAnnotationCapability =
                    config.InsertAnnotationCapability.GetValueOrDefault(),
                ServerTimestampSupported =
                    config.ServerTimestampSupported.GetValueOrDefault(),
                AggregateFunctions = aggregateFunctions == null ||
                    aggregateFunctions.Count == 0 ? null : aggregateFunctions
            };
        }

        /// <summary>
        /// Handles a keep alive event from a session and triggers a reconnect if necessary.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            try
            {
                // check for events from discarded sessions.
                if (!ReferenceEquals(session, _session))
                {
                    return;
                }

                // start reconnect sequence on communication error.
                if (ServiceResult.IsBad(e.Status))
                {
                    if (ReconnectPeriod <= 0)
                    {
                        _logger.LogWarning(
                            "KeepAlive status {Status} for session {Name}, but reconnect is disabled.",
                            e.Status, _sessionName);
                        return;
                    }

                    using (var writerLock = _lock.WriterLock())
                    {
                        if (_reconnectHandler == null)
                        {
                            // Unset session under lock
                            UnsetSession();

                            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);

                            _logger.LogInformation(
                                "KeepAlive status {Status} for session {Name}, reconnecting in {Period}ms.",
                                e.Status, _sessionName, ReconnectPeriod);
                            _reconnectHandler = new SessionReconnectHandler(true);
                            _reconnectHandler.BeginReconnect(_session,
                                ReconnectPeriod, Client_ReconnectComplete);
                        }
                        else
                        {
                            _logger.LogDebug(
                                "KeepAlive status {Status} for session {Name}, reconnect in progress.",
                                e.Status, _sessionName);
                        }
                    }

                    // Go offline
                    NotifySubscriptionStateChange(false);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex, "Error in OnKeepAlive for session {Name}.", _sessionName);
            }
        }

        /// <summary>
        /// Called when the reconnect attempt was successful.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_ReconnectComplete(object? sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!ReferenceEquals(sender, _reconnectHandler))
            {
                return;
            }

            using (var writerLock = _lock.WriterLock())
            {
                // if session recovered, Session property is not null
                var newSession = _reconnectHandler?.Session as Session;
                _reconnectHandler?.Dispose();
                _reconnectHandler = null;

                if (newSession?.Connected != true)
                {
                    // Failed to reconnect.
                    _logger.LogInformation("--- SESSION {Name} failed reconnecting. ---",
                        _sessionName);
                    _needsConnecting = true;
                    return;
                }

                SetSession(newSession);

                _logger.LogInformation("--- SESSION {Name} RECONNECTED ---", _sessionName);
                NumberOfConnectRetries++;

                NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
            }

            // Go back online
            NotifySubscriptionStateChange(true);
        }

        /// <summary>
        /// Update session state
        /// </summary>
        /// <param name="session"></param>
        private void SetSession(Session session)
        {
            if (session?.Connected != true)
            {
                _logger.LogInformation("Session not connected.");
                return;
            }

            if (_session == session)
            {
                Debug.Assert(session.Handle == this);
                return;
            }

            UnsetSession();

            // override keep alive interval
            Codec = CreateCodec(session.MessageContext);
            _session = session;
            _session.KeepAliveInterval = KeepAliveInterval;
            _complexTypeSystem = LoadComplexTypeSystemAsync();

            // set up keep alive callback.
            _session.KeepAlive += Session_KeepAlive;
            _session.Handle = this;

            // Need to work around accessibility here.
            _authenticationToken = (NodeId?)typeof(ClientBase).GetProperty(
                "AuthenticationToken",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)?.GetValue(session) ?? NodeId.Null;

            NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
        }

        /// <summary>
        /// Unset and dispose existing session
        /// </summary>
        /// <param name="noDispose"></param>
        private void UnsetSession(bool noDispose = false)
        {
            var session = _session;
            _session = null;
            _complexTypeSystem = Task.FromResult<ComplexTypeSystem?>(null);
            _authenticationToken = NodeId.Null;
            if (session == null)
            {
                return;
            }
            session.KeepAlive -= Session_KeepAlive;
            session.Handle = null;
            if (noDispose)
            {
                return;
            }
            Codec = CreateCodec();
            session.Dispose();
            _logger.LogDebug("Session {Name} disposed.", _sessionName);
        }

        /// <summary>
        /// Load complex type system
        /// </summary>
        /// <returns></returns>
        private async Task<ComplexTypeSystem?> LoadComplexTypeSystemAsync()
        {
            try
            {
                if (_session?.Connected == true)
                {
                    var complexTypeSystem = new ComplexTypeSystem(_session);
                    await complexTypeSystem.Load().ConfigureAwait(false);
                    _logger.LogInformation("Session {Name} complex type system loaded",
                        _sessionName);
                    return complexTypeSystem;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load complex type system.");
            }
            return null;
        }

        /// <summary>
        /// Queue a notification state change
        /// </summary>
        /// <param name="online"></param>
        private void NotifySubscriptionStateChange(bool online)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach (var subscription in _subscriptions.Values)
                {
                    subscription.OnSubscriptionStateChanged(online);
                }
            });
        }

        /// <summary>
        /// Notify about new connectivity state using any status callback registered.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private void NotifyConnectivityStateChange(EndpointConnectivityState state)
        {
            var previous = _lastState;
            if (previous == state)
            {
                return;
            }
            if (previous != EndpointConnectivityState.Connecting &&
                previous != EndpointConnectivityState.Ready &&
                state == EndpointConnectivityState.Error)
            {
                // Do not change state to generic error once we have
                // a specific error state already set...
                _logger.LogDebug(
                    "Error, connection to {Endpoint} - leaving state at {Previous}.",
                    _connection.Endpoint!.Url, previous);
                return;
            }

            _lastState = state;
            if (_lastState == EndpointConnectivityState.Ready)
            {
                _connected.Set();
            }
            else
            {
                _connected.Reset();
            }
            _logger.LogInformation(
                "Session {Name} with {Endpoint} changed from {Previous} to {State}",
                _sessionName, _connection.Endpoint!.Url, previous, state);
            try
            {
                OnConnectionStateChange?.Invoke(this, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during state callback");
            }
        }

        /// <summary>
        /// Convert exception to connectivity state
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="reconnecting"></param>
        /// <returns></returns>
        public EndpointConnectivityState ToConnectivityState(Exception ex, bool reconnecting = true)
        {
            var state = EndpointConnectivityState.Error;
            switch (ex)
            {
                case ServiceResultException sre:
                    switch (sre.StatusCode)
                    {
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
                    _logger.LogDebug("{Result} => {State}", sre.Result, state);
                    break;
                default:
                    state = EndpointConnectivityState.Error;
                    _logger.LogDebug("{Message} => {State}", ex.Message, state);
                    break;
            }
            return state;
        }

        /// <summary>
        /// Begin request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<SessionActivity<T>> BeginAsync<T>(RequestHeader header,
            CancellationToken ct)
            where T : IServiceResponse, new()
        {
            var readerLock = await _lock.ReaderLockAsync(ct).ConfigureAwait(false);
            try
            {
                var session = _session;
                var activity = new SessionActivity<T>(readerLock, typeof(T).Name[0..^8],
                    session!);
                if (session == null)
                {
                    var error = new T();
                    error.ResponseHeader.ServiceResult = StatusCodes.BadNotConnected;
                    error.ResponseHeader.Timestamp = DateTime.UtcNow;
                    var text = error.ResponseHeader.StringTable.Count;
                    error.ResponseHeader.StringTable.Add("Session not connected.");
                    var locale = error.ResponseHeader.StringTable.Count;
                    error.ResponseHeader.StringTable.Add("en-US");
                    var symbol = error.ResponseHeader.StringTable.Count;
                    error.ResponseHeader.StringTable.Add("BadNotConnected");
                    error.ResponseHeader.ServiceDiagnostics = new DiagnosticInfo
                    {
                        SymbolicId = symbol,
                        Locale = locale,
                        LocalizedText = text
                    };
                    activity.Error = error;
                }
                else
                {
                    header.RequestHandle = session.NewRequestHandle();
                    header.AuthenticationToken = _authenticationToken;
                    header.Timestamp = DateTime.UtcNow;
                }
                return activity;
            }
            catch
            {
                readerLock.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Gets the response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private static T ValidateResponse<T>(IServiceResponse response)
            where T : IServiceResponse, new()
        {
            if (response?.ResponseHeader == null)
            {
                // Throw - this is likely an issue in the transport.
                throw new ServiceResultException(StatusCodes.BadUnknownResponse);
            }
            if (response is not T result)
            {
                // Received a response, but not the type we expected.
                // Promote to expected Type.
                result = new T();

                result.ResponseHeader.ServiceResult =
                    response.ResponseHeader.ServiceResult;
                result.ResponseHeader.StringTable =
                    response.ResponseHeader.StringTable;
                result.ResponseHeader.AdditionalHeader =
                    response.ResponseHeader.AdditionalHeader;
                result.ResponseHeader.RequestHandle =
                    response.ResponseHeader.RequestHandle;
                result.ResponseHeader.ServiceDiagnostics =
                    response.ResponseHeader.ServiceDiagnostics;
                result.ResponseHeader.Timestamp =
                    response.ResponseHeader.Timestamp;
            }
            return result;
        }

        /// <summary>
        /// Create codec
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private IVariantEncoder CreateCodec(IServiceMessageContext? context = null)
        {
            return new JsonVariantEncoder(context ?? new ServiceMessageContext(), _serializer);
        }

        /// <summary>
        /// Session activity
        /// </summary>
        private sealed class SessionActivity<T> : IDisposable where T : IServiceResponse
        {
            /// <summary>
            /// Any error to convey
            /// </summary>
            public ISession Session { get; set; }

            /// <summary>
            /// Any error to convey
            /// </summary>
            public T? Error { get; set; }

            /// <inheritdoc/>
            public SessionActivity(IDisposable readerLock, string activity, ISession session)
            {
                Session = session;
                _lock = readerLock;
                _activity = kActivity.StartActivity(activity);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _activity?.Dispose();
                _lock.Dispose();
            }

            private readonly Activity? _activity;
            private readonly IDisposable _lock;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_connection_retries",
                () => new Measurement<long>(NumberOfConnectRetries, TagList), "Connection attempts",
                "OPC UA connect retries.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_publisher_is_connection_ok",
                () => new Measurement<int>(IsConnected ? 1 : 0, TagList), "",
                "OPC UA connection success flag.");
        }

        private SessionReconnectHandler? _reconnectHandler;
        private ServerCapabilitiesModel? _server;
        private OperationLimitsModel? _limits;
        private HistoryServerCapabilitiesModel? _history;
        private Session? _session;
        private Task<ComplexTypeSystem?>? _complexTypeSystem;
        private EndpointConnectivityState _lastState;
        private bool _disposed;
        private NodeId _authenticationToken;
        private bool _needsConnecting;
        private readonly AsyncReaderWriterLock _lock = new();
        private readonly AsyncManualResetEvent _connected = new();
        private readonly ApplicationConfiguration _configuration;
        private readonly IJsonSerializer _serializer;
        private readonly string _sessionName;
        private readonly ConnectionModel _connection;
        private readonly ILogger _logger;
        private readonly DateTime _lastActivity = DateTime.UtcNow;
        private readonly ConcurrentDictionary<string, ISubscription> _subscriptions = new();
        private static readonly TypeTable kTypeTree = new(new NamespaceTable());
        private static readonly SystemContext kSystemContext = new();
        private static readonly ServiceMessageContext kMessageContext = new();
        private static readonly ActivitySource kActivity = new(typeof(OpcUaClient).FullName!);
    }
}
