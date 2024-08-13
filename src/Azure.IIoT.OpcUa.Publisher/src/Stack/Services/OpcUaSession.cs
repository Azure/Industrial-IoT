// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// OPC UA session extends the SDK session
    /// </summary>
    [DataContract(Namespace = OpcUaClient.Namespace)]
    [KnownType(typeof(OpcUaSubscription))]
    [KnownType(typeof(OpcUaMonitoredItem))]
    internal sealed class OpcUaSession : Session, IOpcUaSession, ISessionServices
    {
        /// <inheritdoc/>
        public IVariantEncoder Codec { get; }

        /// <inheritdoc/>
        public ISessionServices Services => this;

        /// <summary>
        /// Time the session was created
        /// </summary>
        internal DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Type system has loaded
        /// </summary>
        internal bool IsTypeSystemLoaded
            => _complexTypeSystem?.IsCompletedSuccessfully ?? false;

        /// <summary>
        /// Get list of subscription handles registered in the session
        /// </summary>
        internal List<IOpcUaSubscription> SubscriptionHandles
        {
            get
            {
                lock (SyncRoot)
                {
                    return Subscriptions.OfType<IOpcUaSubscription>().ToList();
                }
            }
        }

        // Remove when fixed in stack
#if !NO_UAFIX
#if !NO_DUMMY
        internal int MaxPublishRequestCount { get; set; }
#else
        internal int MaxPublishRequestCount
        {
            get
            {
                // TODO: Make accessible in base class
                var r = _maxPublishRequest?.GetValue(this);
                return r == null ? 0 : (int)r;
            }
            set
            {
                // TODO: Make accessible in base class
                _maxPublishRequest?.SetValue(this, value);
            }
        }
        private readonly FieldInfo? _maxPublishRequest = typeof(Session).GetField(
            "m_tooManyPublishRequests", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
#endif

        /// <summary>
        /// Enable or disable ChannelDiagnostics
        /// </summary>
        public bool DiagnosticsEnabled
        {
            get => _diagnosticsEnabled != false;
            set => _diagnosticsEnabled = value ? null : false;
        }

        /// <summary>
        /// Create session
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        /// <param name="channel"></param>
        /// <param name="configuration"></param>
        /// <param name="endpoint"></param>
        /// <param name="clientCertificate"></param>
        /// <param name="availableEndpoints"></param>
        /// <param name="discoveryProfileUris"></param>
        public OpcUaSession(OpcUaClient client, IJsonSerializer serializer,
            ILogger<OpcUaSession> logger, TimeProvider timeProvider,
            ITransportChannel channel, ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint, X509Certificate2? clientCertificate = null,
            EndpointDescriptionCollection? availableEndpoints = null,
            StringCollection? discoveryProfileUris = null)
            : base(channel, configuration, endpoint, clientCertificate,
                  availableEndpoints, discoveryProfileUris)
        {
            _logger = logger;
            _client = client;
            _serializer = serializer;
            _timeProvider = timeProvider;
            CreatedAt = _timeProvider.GetUtcNow();

            Initialize();
            Codec = new JsonVariantEncoder(MessageContext, serializer);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="session"></param>
        /// <param name="channel"></param>
        /// <param name="template"></param>
        /// <param name="copyEventHandlers"></param>
        private OpcUaSession(OpcUaSession session,
            ITransportChannel channel, Session template, bool copyEventHandlers)
            : base(channel, template, copyEventHandlers)
        {
            _logger = session._logger;
            _client = session._client;
            _serializer = session._serializer;
            _timeProvider = session._timeProvider;
            CreatedAt = _timeProvider.GetUtcNow();

            _complexTypeSystem = session._complexTypeSystem;
            _history = session._history;
            _limits = session._limits;
            _server = session._server;

            Initialize();
            Codec = new JsonVariantEncoder(MessageContext, _serializer);

            MaxPublishRequestCount = session.MaxPublishRequestCount;
            MinPublishRequestCount = session.MinPublishRequestCount;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && !_disposed)
            {
                var sessionName = SessionName;

                PublishError -=
                    _client.Session_HandlePublishError;
                PublishSequenceNumbersToAcknowledge -=
                    _client.Session_PublishSequenceNumbersToAcknowledge;
                KeepAlive -=
                    _client.Session_KeepAlive;
                SessionConfigurationChanged -=
                    Session_SessionConfigurationChanged;

                _disposed = true;
                CloseChannel(); // Ensure channel is closed

                try
                {
                    _cts.Cancel();
                    _logger.LogInformation("{Session}: Session disposed.",
                        sessionName);
                }
                finally
                {
                    _activitySource.Dispose();
                    _cts.Dispose();
                }
            }
            Debug.Assert(SubscriptionHandles.Count == 0);
        }

        /// <inheritdoc/>
        public override Session CloneSession(ITransportChannel channel,
            bool copyEventHandlers)
        {
            return new OpcUaSession(this, channel, this, copyEventHandlers);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return SessionName;
        }

        /// <inheritdoc/>
        public async ValueTask<SessionDiagnosticsModel> GetServerDiagnosticAsync(
            CancellationToken ct = default)
        {
            try
            {
                _lastDiagnostics = await FetchServerDiagnosticAsync(new RequestHeader(),
                    ct).ConfigureAwait(false);
            }
            catch (ServiceResultException sre)
            {
                _logger.LogDebug(sre, "Failed to fetch server diagnostics.");
            }
            return _lastDiagnostics ?? new SessionDiagnosticsModel();
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
            NamespaceFormat namespaceFormat, CancellationToken ct = default)
        {
            if (_server != null && namespaceFormat == NamespaceFormat.Uri)
            {
                return _server;
            }
            if (_limits == null)
            {
                _limits = await FetchOperationLimitsAsync(new RequestHeader(),
                    ct).ConfigureAwait(false);
            }
            var server = await FetchServerCapabilitiesAsync(new RequestHeader(),
                namespaceFormat, ct).ConfigureAwait(false);
            if (namespaceFormat == NamespaceFormat.Uri)
            {
                _server = server;
            }
            return server ?? new ServerCapabilitiesModel
            {
                OperationLimits = _limits ?? new OperationLimitsModel()
            };
        }

        /// <inheritdoc/>
        public async ValueTask<HistoryServerCapabilitiesModel> GetHistoryCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct = default)
        {
            if (_history != null && namespaceFormat == NamespaceFormat.Uri)
            {
                return _history;
            }
            var history = await FetchHistoryCapabilitiesAsync(new RequestHeader(),
                namespaceFormat, ct).ConfigureAwait(false);
            if (namespaceFormat == NamespaceFormat.Uri)
            {
                _history = history;
            }
            return history ?? new HistoryServerCapabilitiesModel();
        }

        /// <inheritdoc/>
        public async ValueTask<ComplexTypeSystem?> GetComplexTypeSystemAsync(CancellationToken ct)
        {
            if (_client.DisableComplexTypeLoading)
            {
                return null;
            }
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    Debug.Assert(_complexTypeSystem != null);
                    return await _complexTypeSystem.WaitAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Throw any cancellation token exception
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "{Session}: Attempt #{Attempt}. Failed to get complex type system.",
                        this, attempt);

                    // Try again. TODO: Throttle using a timer or so...
                    _complexTypeSystem = LoadComplexTypeSystemAsync();
                }
            }
            return null;
        }

        /// <inheritdoc/>
        async ValueTask<AddNodesResponse> ISessionServices.AddNodesAsync(RequestHeader requestHeader,
            AddNodesItemCollection nodesToAdd, CancellationToken ct)
        {
            using var activity = Begin<AddNodesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new AddNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToAdd = nodesToAdd
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<AddReferencesResponse> ISessionServices.AddReferencesAsync(
            RequestHeader requestHeader, AddReferencesItemCollection referencesToAdd,
            CancellationToken ct)
        {
            using var activity = Begin<AddReferencesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new AddReferencesRequest
            {
                RequestHeader = requestHeader,
                ReferencesToAdd = referencesToAdd
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<DeleteNodesResponse> ISessionServices.DeleteNodesAsync(
            RequestHeader requestHeader, DeleteNodesItemCollection nodesToDelete,
            CancellationToken ct)
        {
            using var activity = Begin<DeleteNodesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new DeleteNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToDelete = nodesToDelete
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<DeleteReferencesResponse> ISessionServices.DeleteReferencesAsync(
            RequestHeader requestHeader, DeleteReferencesItemCollection referencesToDelete,
            CancellationToken ct)
        {
            using var activity = Begin<DeleteReferencesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new DeleteReferencesRequest
            {
                RequestHeader = requestHeader,
                ReferencesToDelete = referencesToDelete
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<BrowseResponse> ISessionServices.BrowseAsync(
            RequestHeader requestHeader, ViewDescription? view,
            uint requestedMaxReferencesPerNode,
            BrowseDescriptionCollection nodesToBrowse, CancellationToken ct)
        {
            using var activity = Begin<BrowseResponse>(requestHeader);
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
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<BrowseNextResponse> ISessionServices.BrowseNextAsync(
            RequestHeader requestHeader, bool releaseContinuationPoints,
            ByteStringCollection continuationPoints, CancellationToken ct)
        {
            using var activity = Begin<BrowseNextResponse>(requestHeader);
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
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<TranslateBrowsePathsToNodeIdsResponse> ISessionServices.TranslateBrowsePathsToNodeIdsAsync(
            RequestHeader requestHeader, BrowsePathCollection browsePaths,
            CancellationToken ct)
        {
            using var activity = Begin<TranslateBrowsePathsToNodeIdsResponse>(
                requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new TranslateBrowsePathsToNodeIdsRequest
            {
                RequestHeader = requestHeader,
                BrowsePaths = browsePaths
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<RegisterNodesResponse> ISessionServices.RegisterNodesAsync(
            RequestHeader requestHeader, NodeIdCollection nodesToRegister,
            CancellationToken ct)
        {
            using var activity = Begin<RegisterNodesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new RegisterNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToRegister = nodesToRegister
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<UnregisterNodesResponse> ISessionServices.UnregisterNodesAsync(
            RequestHeader requestHeader, NodeIdCollection nodesToUnregister,
            CancellationToken ct)
        {
            using var activity = Begin<UnregisterNodesResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new UnregisterNodesRequest
            {
                RequestHeader = requestHeader,
                NodesToUnregister = nodesToUnregister
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<QueryFirstResponse> ISessionServices.QueryFirstAsync(
            RequestHeader requestHeader, ViewDescription view,
            NodeTypeDescriptionCollection nodeTypes, ContentFilter filter,
            uint maxDataSetsToReturn, uint maxReferencesToReturn,
            CancellationToken ct)
        {
            using var activity = Begin<QueryFirstResponse>(requestHeader);
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
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<QueryNextResponse> ISessionServices.QueryNextAsync(
            RequestHeader requestHeader, bool releaseContinuationPoint,
            byte[] continuationPoint, CancellationToken ct)
        {
            using var activity = Begin<QueryNextResponse>(requestHeader);
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
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<ReadResponse> ISessionServices.ReadAsync(RequestHeader requestHeader,
            double maxAge, Opc.Ua.TimestampsToReturn timestampsToReturn,
            ReadValueIdCollection nodesToRead, CancellationToken ct)
        {
            using var activity = Begin<ReadResponse>(requestHeader);
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
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<HistoryReadResponse> ISessionServices.HistoryReadAsync(
            RequestHeader requestHeader, ExtensionObject? historyReadDetails,
            Opc.Ua.TimestampsToReturn timestampsToReturn, bool releaseContinuationPoints,
            HistoryReadValueIdCollection nodesToRead, CancellationToken ct)
        {
            using var activity = Begin<HistoryReadResponse>(requestHeader);
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
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<WriteResponse> ISessionServices.WriteAsync(RequestHeader requestHeader,
            WriteValueCollection nodesToWrite, CancellationToken ct)
        {
            using var activity = Begin<WriteResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new WriteRequest
            {
                RequestHeader = requestHeader,
                NodesToWrite = nodesToWrite
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<HistoryUpdateResponse> ISessionServices.HistoryUpdateAsync(
            RequestHeader requestHeader, ExtensionObjectCollection historyUpdateDetails,
            CancellationToken ct)
        {
            using var activity = Begin<HistoryUpdateResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new HistoryUpdateRequest
            {
                RequestHeader = requestHeader,
                HistoryUpdateDetails = historyUpdateDetails
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <inheritdoc/>
        async ValueTask<CallResponse> ISessionServices.CallAsync(RequestHeader requestHeader,
            CallMethodRequestCollection methodsToCall, CancellationToken ct)
        {
            using var activity = Begin<CallResponse>(requestHeader);
            if (activity.Error != null)
            {
                return activity.Error;
            }
            var request = new CallRequest
            {
                RequestHeader = requestHeader,
                MethodsToCall = methodsToCall
            };
            var response = await TransportChannel.SendRequestAsync(
                request, ct).ConfigureAwait(false);
            return activity.ValidateResponse(response);
        }

        /// <summary>
        /// Called when session is created
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="sessionCookie"></param>
        public override void SessionCreated(NodeId sessionId, NodeId sessionCookie)
        {
            base.SessionCreated(sessionId, sessionCookie);
            if (NodeId.IsNull(sessionId))
            {
                // Also called when session closes
                return;
            }

            Debug.Assert(!NodeId.IsNull(sessionCookie));

            // Update operation limits with configuration provided overrides
            OperationLimits.Override(_client.LimitOverrides);

            PreloadComplexTypeSystem();
        }

        /// <summary>
        /// Called when session configuration changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Session_SessionConfigurationChanged(object? sender, EventArgs e)
        {
            PreloadComplexTypeSystem();
        }

        /// <summary>
        /// Preload type system
        /// </summary>
        private void PreloadComplexTypeSystem()
        {
            if (_complexTypeSystem == null &&
                Connected &&
                !_client.DisableComplexTypeLoading &&
                !_client.DisableComplexTypePreloading)
            {
                _complexTypeSystem = LoadComplexTypeSystemAsync();
            }
        }

        /// <summary>
        /// Initialize session settings from client configuration
        /// </summary>
        private void Initialize()
        {
            SessionFactory = _client;
            TransferSubscriptionsOnReconnect = !_client.DisableTransferSubscriptionOnReconnect;
            DeleteSubscriptionsOnClose = !TransferSubscriptionsOnReconnect;

            PublishError +=
                _client.Session_HandlePublishError;
            PublishSequenceNumbersToAcknowledge +=
                _client.Session_PublishSequenceNumbersToAcknowledge;
            KeepAlive +=
                _client.Session_KeepAlive;
            SessionConfigurationChanged +=
                Session_SessionConfigurationChanged;

            var keepAliveInterval =
                (int)(_client.KeepAliveInterval ?? kDefaultKeepAliveInterval).TotalMilliseconds;
            if (keepAliveInterval <= 0)
            {
                keepAliveInterval = kDefaultKeepAliveInterval.Milliseconds;
            }
            var operationTimeout =
                (int)(_client.OperationTimeout ?? kDefaultOperationTimeout).TotalMilliseconds;
            if (operationTimeout <= 0)
            {
                operationTimeout = kDefaultOperationTimeout.Milliseconds;
            }

            KeepAliveInterval = keepAliveInterval;
            OperationTimeout = operationTimeout;
            _defaultOperationTimeout = operationTimeout;
        }

        /// <summary>
        /// Fetch server diagnostics
        /// </summary>
        /// <param name="header"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<SessionDiagnosticsModel?> FetchServerDiagnosticAsync(RequestHeader header,
            CancellationToken ct)
        {
            if (_diagnosticsEnabled == false)
            {
                return null;
            }
            if (!_diagnosticsEnabled.HasValue)
            {
                // Check whether enabled and if not enabled enable it
                var diagnosticsEnabled = await ReadValueAsync(
                    VariableIds.Server_ServerDiagnostics_EnabledFlag, ct).ConfigureAwait(false);
                _diagnosticsEnabled = diagnosticsEnabled.Value as bool?;
                if (_diagnosticsEnabled == false)
                {
                    var enableResponse = await WriteAsync(header, new[]
                    {
                        new WriteValue
                        {
                            AttributeId = Attributes.Value,
                            NodeId = VariableIds.Server_ServerDiagnostics_EnabledFlag,
                            Value = new DataValue(true)
                        }
                    }, ct).ConfigureAwait(false);
                    if (ServiceResult.IsBad(enableResponse.Results[0]))
                    {
                        _logger.LogError("Session diagnostics disabled and failed to enable ({Error}).",
                            enableResponse.Results[0]);
                        return null;
                    }
                    _diagnosticsEnabled = true;
                }
            }
            var response = await ReadAsync(header, 0.0, Opc.Ua.TimestampsToReturn.Neither, new[]
            {
                new ReadValueId
                {
                    AttributeId = Attributes.Value,
                    NodeId =
        VariableIds.Server_ServerDiagnostics_SessionsDiagnosticsSummary_SessionDiagnosticsArray
                },
                new ReadValueId
                {
                    AttributeId = Attributes.Value,
                    NodeId = VariableIds.Server_ServerDiagnostics_SubscriptionDiagnosticsArray
                }
            }, ct).ConfigureAwait(false);
            if (ServiceResult.IsBad(response.Results[0].StatusCode))
            {
                _logger.LogInformation("Session diagnostics not retrievable ({Error1}/{Error2}).",
                    response.Results[0].StatusCode, response.Results[1].StatusCode);
                return null;
            }
            var sessionDiagnosticsArray = response.Results[0].Value as ExtensionObject[];
            var sessionDiagnostics = sessionDiagnosticsArray?
                .Select(o => o.Body)
                .OfType<SessionDiagnosticsDataType>()
                .FirstOrDefault(d => d.SessionId == SessionId);
            if (sessionDiagnostics == null)
            {
                _logger.LogError("Failed to find diagnostics for this session ({Error}).",
                    response.Results[0].StatusCode);
                return null;
            }

            List<SubscriptionDiagnosticsModel>? subscriptions = null;
            var subscriptionDiagnosticsArray = response.Results[1].Value as ExtensionObject[];
            if (!ServiceResult.IsBad(response.Results[1].StatusCode) &&
                subscriptionDiagnosticsArray != null)
            {
                subscriptions = subscriptionDiagnosticsArray
                    .Select(o => o.Body)
                    .OfType<SubscriptionDiagnosticsDataType>()
                    .Where(d => d.SessionId == SessionId)
                    .Select(diag => new SubscriptionDiagnosticsModel
                    {
                        SubscriptionId = diag.SubscriptionId,
                        Priority = diag.Priority,
                        PublishingInterval = diag.PublishingInterval,
                        MaxKeepAliveCount = diag.MaxKeepAliveCount,
                        MaxLifetimeCount = diag.MaxLifetimeCount,
                        MaxNotificationsPerPublish = diag.MaxNotificationsPerPublish,
                        PublishingEnabled = diag.PublishingEnabled,
                        ModifyCount = diag.ModifyCount,
                        EnableCount = diag.EnableCount,
                        DisableCount = diag.DisableCount,
                        RepublishRequestCount = diag.RepublishRequestCount,
                        RepublishMessageRequestCount = diag.RepublishMessageRequestCount,
                        RepublishMessageCount = diag.RepublishMessageCount,
                        TransferRequestCount = diag.TransferRequestCount,
                        TransferredToAltClientCount = diag.TransferredToAltClientCount,
                        TransferredToSameClientCount = diag.TransferredToSameClientCount,
                        PublishRequestCount = diag.PublishRequestCount,
                        DataChangeNotificationsCount = diag.DataChangeNotificationsCount,
                        EventNotificationsCount = diag.EventNotificationsCount,
                        NotificationsCount = diag.NotificationsCount,
                        LatePublishRequestCount = diag.LatePublishRequestCount,
                        CurrentKeepAliveCount = diag.CurrentKeepAliveCount,
                        CurrentLifetimeCount = diag.CurrentLifetimeCount,
                        UnacknowledgedMessageCount = diag.UnacknowledgedMessageCount,
                        DiscardedMessageCount = diag.DiscardedMessageCount,
                        MonitoredItemCount = diag.MonitoredItemCount,
                        DisabledMonitoredItemCount = diag.DisabledMonitoredItemCount,
                        MonitoringQueueOverflowCount = diag.MonitoringQueueOverflowCount,
                        NextSequenceNumber = diag.NextSequenceNumber,
                        EventQueueOverFlowCount = diag.EventQueueOverFlowCount
                    })
                    .ToList();
            }
            else
            {
                _logger.LogInformation("Subscription diagnostics not retrievable ({Error}).",
                    response.Results[1].StatusCode);
            }

            return new SessionDiagnosticsModel
            {
                SessionId =
                    sessionDiagnostics.SessionId.AsString(MessageContext, NamespaceFormat.Expanded),
                TranslateBrowsePathsToNodeIdsCount =
                    ToCounter(sessionDiagnostics.TranslateBrowsePathsToNodeIdsCount),
                AddNodesCount =
                    ToCounter(sessionDiagnostics.AddNodesCount),
                AddReferencesCount =
                    ToCounter(sessionDiagnostics.AddReferencesCount),
                BrowseCount =
                    ToCounter(sessionDiagnostics.BrowseCount),
                BrowseNextCount =
                    ToCounter(sessionDiagnostics.BrowseNextCount),
                CreateMonitoredItemsCount =
                    ToCounter(sessionDiagnostics.CreateMonitoredItemsCount),
                CreateSubscriptionCount =
                    ToCounter(sessionDiagnostics.CreateSubscriptionCount),
                DeleteMonitoredItemsCount =
                    ToCounter(sessionDiagnostics.DeleteMonitoredItemsCount),
                DeleteNodesCount =
                    ToCounter(sessionDiagnostics.DeleteNodesCount),
                DeleteReferencesCount =
                    ToCounter(sessionDiagnostics.DeleteReferencesCount),
                DeleteSubscriptionsCount =
                    ToCounter(sessionDiagnostics.DeleteSubscriptionsCount),
                CallCount =
                    ToCounter(sessionDiagnostics.CallCount),
                HistoryReadCount =
                    ToCounter(sessionDiagnostics.HistoryReadCount),
                HistoryUpdateCount =
                    ToCounter(sessionDiagnostics.HistoryUpdateCount),
                ModifyMonitoredItemsCount =
                    ToCounter(sessionDiagnostics.ModifyMonitoredItemsCount),
                ModifySubscriptionCount =
                    ToCounter(sessionDiagnostics.ModifySubscriptionCount),
                PublishCount =
                    ToCounter(sessionDiagnostics.PublishCount),
                RegisterNodesCount =
                    ToCounter(sessionDiagnostics.RegisterNodesCount),
                RepublishCount =
                    ToCounter(sessionDiagnostics.RepublishCount),
                SetMonitoringModeCount =
                    ToCounter(sessionDiagnostics.SetMonitoringModeCount),
                SetPublishingModeCount =
                    ToCounter(sessionDiagnostics.SetPublishingModeCount),
                UnregisterNodesCount =
                    ToCounter(sessionDiagnostics.UnregisterNodesCount),
                QueryFirstCount =
                    ToCounter(sessionDiagnostics.QueryFirstCount),
                QueryNextCount =
                    ToCounter(sessionDiagnostics.QueryNextCount),
                ReadCount =
                    ToCounter(sessionDiagnostics.ReadCount),
                WriteCount =
                    ToCounter(sessionDiagnostics.WriteCount),
                SetTriggeringCount =
                    ToCounter(sessionDiagnostics.SetTriggeringCount),
                TotalRequestCount =
                    ToCounter(sessionDiagnostics.TotalRequestCount),
                TransferSubscriptionsCount =
                    ToCounter(sessionDiagnostics.TransferSubscriptionsCount),
                ServerUri =
                    sessionDiagnostics.ServerUri,
                SessionName =
                    sessionDiagnostics.SessionName,
                ActualSessionTimeout =
                    sessionDiagnostics.ActualSessionTimeout,
                MaxResponseMessageSize =
                    sessionDiagnostics.MaxResponseMessageSize,
                UnauthorizedRequestCount =
                    sessionDiagnostics.UnauthorizedRequestCount,
                ConnectTime =
                    sessionDiagnostics.ClientConnectionTime,
                LastContactTime =
                    sessionDiagnostics.ClientLastContactTime,
                CurrentSubscriptionsCount =
                    sessionDiagnostics.CurrentSubscriptionsCount,
                CurrentMonitoredItemsCount =
                    sessionDiagnostics.CurrentMonitoredItemsCount,
                CurrentPublishRequestsInQueue =
                    sessionDiagnostics.CurrentPublishRequestsInQueue,
                Subscriptions =
                    subscriptions
            };

            static ServiceCounterModel? ToCounter(ServiceCounterDataType counter)
            {
                if (counter.TotalCount == 0 && counter.ErrorCount == 0)
                {
                    return null;
                }
                return new ServiceCounterModel
                {
                    TotalCount = counter.TotalCount,
                    ErrorCount = counter.ErrorCount
                };
            }
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
            // Fetch limits into the session using the new api
            var maxNodesPerRead = Validate32(OperationLimits.MaxNodesPerRead);

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
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerRead,
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
            foreach (var chunk in nodes.Batch(Math.Max(1, (int)maxNodesPerRead!)))
            {
                // Group the reads
                var requests = new ReadValueIdCollection(chunk
                    .Select(n => new ReadValueId
                    {
                        NodeId = n,
                        AttributeId = Attributes.Value
                    }));
                var response = await ReadAsync(header, 0,
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
                MaxNodesPerRead =
                    Validate32(value[9].GetValueOrDefault<uint?>(), OperationLimits.MaxNodesPerRead),
                MaxNodesPerHistoryUpdateData =
                    Validate32(value[10].GetValueOrDefault<uint?>()),
                MaxNodesPerHistoryUpdateEvents =
                    Validate32(value[11].GetValueOrDefault<uint?>()),
                MaxNodesPerMethodCall =
                    Validate32(value[12].GetValueOrDefault<uint?>()),
                MaxNodesPerBrowse =
                    Validate32(value[13].GetValueOrDefault<uint?>(), OperationLimits.MaxNodesPerBrowse),
                MaxNodesPerRegisterNodes =
                    Validate32(value[14].GetValueOrDefault<uint?>()),
                MaxNodesPerTranslatePathsToNodeIds =
                    Validate32(value[15].GetValueOrDefault<uint?>()),
                MaxNodesPerNodeManagement =
                    Validate32(value[16].GetValueOrDefault<uint?>()),
                MaxMonitoredItemsPerCall =
                    Validate32(value[17].GetValueOrDefault<uint?>())
            };

            static uint? Validate32(uint? v, uint max = 0) => v == null ? null :
                Math.Min(max == 0 ? int.MaxValue : max, v is > 0 and < int.MaxValue ? v.Value : int.MaxValue);
            static ushort? Validate16(ushort? v, ushort max = 0) => v == null ? null :
                Math.Min(max == 0 ? ushort.MaxValue : max, v > 0 ? v.Value : ushort.MaxValue);
        }

        /// <summary>
        /// Read the server capabilities if available
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<ServerCapabilitiesModel?> FetchServerCapabilitiesAsync(
            RequestHeader requestHeader, NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            // load the defaults for the historical capabilities object.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var config =
                new ServerCapabilitiesState(null);
#pragma warning restore CA2000 // Dispose objects before losing scope
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
            config.Create(SystemContext, null,
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
            config.AggregateFunctions.GetChildren(SystemContext, functions);
            var aggregateFunctions = functions.OfType<BaseObjectState>().ToDictionary(
                c => c.BrowseName.AsString(MessageContext, namespaceFormat),
                c => c.NodeId.AsString(MessageContext, namespaceFormat) ?? string.Empty);
            var rules = new List<BaseInstanceState>();
            config.ModellingRules.GetChildren(SystemContext, rules);
            var modellingRules = rules.OfType<BaseObjectState>().ToDictionary(
                c => c.BrowseName.AsString(MessageContext, namespaceFormat),
                c => c.NodeId.AsString(MessageContext, namespaceFormat) ?? string.Empty);
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
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<HistoryServerCapabilitiesModel?> FetchHistoryCapabilitiesAsync(
            RequestHeader requestHeader, NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            // load the defaults for the historical capabilities object.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var config =
                new HistoryServerCapabilitiesState(null);
#pragma warning restore CA2000 // Dispose objects before losing scope
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
            config.Create(SystemContext, null,
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
                config.AggregateFunctions.GetChildren(SystemContext, children);
                aggregateFunctions = children.OfType<BaseObjectState>().ToDictionary(
                    c => c.BrowseName.AsString(MessageContext, namespaceFormat),
                    c => c.NodeId.AsString(MessageContext, namespaceFormat) ?? string.Empty);
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
        /// Set appropriate operation timeouts
        /// </summary>
        /// <param name="closing"></param>
        internal void UpdateOperationTimeout(bool closing)
        {
            //
            // The OperationTimeout while publishing should be twice the
            // value for PublishingInterval * KeepAliveCount
            //
            if (closing)
            {
                OperationTimeout = 2000; // Update to 2 seconds for closing
                return;
            }
            var timeout = Subscriptions
                .Select(s => s.CurrentPublishingInterval * s.CurrentKeepAliveCount)
                .DefaultIfEmpty(0)
                .Max() * 2;
            if (timeout < _defaultOperationTimeout)
            {
                timeout = _defaultOperationTimeout;
            }
            if (timeout > kMaxOperationTimeout.TotalMilliseconds)
            {
                timeout = kMaxOperationTimeout.TotalMilliseconds;
            }
            if (OperationTimeout != timeout)
            {
                OperationTimeout = (int)timeout;
                _logger.LogInformation("Operation timeout updated to {Timeout}.",
                    TimeSpan.FromMilliseconds(timeout));
            }
        }

        /// <summary>
        /// Load complex type system
        /// </summary>
        /// <returns></returns>
        private Task<ComplexTypeSystem> LoadComplexTypeSystemAsync()
        {
            Debug.Assert(!_client.DisableComplexTypeLoading);
            return Task.Run(async () =>
            {
                var nodeCache = NodeCache;
                if (Connected)
                {
                    var complexTypeSystem = new ComplexTypeSystem(this);
                    await complexTypeSystem.Load().ConfigureAwait(false);

                    if (Connected)
                    {
                        _logger.LogInformation(
                            "{Session}: Complex type system loaded into client.", this);

                        // Clear cache to release memory.
                        // TODO: we should have a real node cache here
                        nodeCache.Clear();
                        return complexTypeSystem;
                    }
                }
                throw new ServiceResultException(StatusCodes.BadNotConnected);
            }, _cts.Token);
        }

        /// <summary>
        /// Begin request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <returns></returns>
        private SessionActivity<T> Begin<T>(RequestHeader header)
            where T : IServiceResponse, new()
        {
            var activity = new SessionActivity<T>(this, typeof(T).Name[0..^8]);
            if (!Connected)
            {
                var error = new T();
                error.ResponseHeader.ServiceResult = StatusCodes.BadNotConnected;
                error.ResponseHeader.Timestamp = _timeProvider.GetUtcNow().UtcDateTime;
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
                header.RequestHandle = NewRequestHandle();
                header.AuthenticationToken = AuthenticationToken;
                header.Timestamp = _timeProvider.GetUtcNow().UtcDateTime;
            }
            return activity;
        }

        /// <summary>
        /// Session activity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class SessionActivity<T> : IDisposable
            where T : IServiceResponse, new()
        {
            /// <summary>
            /// Any error to convey
            /// </summary>
            public T? Error { get; set; }

            /// <inheritdoc/>
            public SessionActivity(OpcUaSession outer, string activity)
            {
                _activity = outer._activitySource.StartActivity(activity);

                if (outer._logger.IsEnabled(LogLevel.Debug))
                {
                    _logScope = new LogScope(activity, Stopwatch.StartNew(),
                        outer._logger);
                    _logScope.logger.LogDebug("Session activity {Activity} started...",
                        _logScope.name);
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _activity?.Dispose();

                _logScope?.logger.LogDebug(
                    "Session activity {Activity} completed in {Elapsed} with {Status}.",
                    _logScope.name, _logScope.sw.Elapsed,
                        Error?.ResponseHeader?.ServiceResult == null ? "Good" :
                    StatusCodes.GetBrowseName(Error.ResponseHeader.ServiceResult.CodeBits));
            }

            /// <summary>
            /// Gets the response
            /// </summary>
            /// <param name="response"></param>
            /// <exception cref="ServiceResultException"></exception>
            /// <returns></returns>
            public T ValidateResponse(IServiceResponse response)
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

                if (StatusCode.IsBad(result.ResponseHeader.ServiceResult))
                {
                    Error = result;
                }
                return result;
            }

            private sealed record class LogScope(string name, Stopwatch sw, ILogger logger);
            private readonly Activity? _activity;
            private readonly LogScope? _logScope;
        }

        private ServerCapabilitiesModel? _server;
        private OperationLimitsModel? _limits;
        private SessionDiagnosticsModel? _lastDiagnostics;
        private HistoryServerCapabilitiesModel? _history;
        private Task<ComplexTypeSystem>? _complexTypeSystem;
        private bool _disposed;
        private bool? _diagnosticsEnabled;
        private int _defaultOperationTimeout;
        private readonly CancellationTokenSource _cts = new();
        private readonly ILogger _logger;
        private readonly OpcUaClient _client;
        private readonly IJsonSerializer _serializer;
        private readonly TimeProvider _timeProvider;
        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
        private static readonly TimeSpan kDefaultOperationTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan kDefaultKeepAliveInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan kMaxOperationTimeout = TimeSpan.FromMinutes(30);
    }
}
