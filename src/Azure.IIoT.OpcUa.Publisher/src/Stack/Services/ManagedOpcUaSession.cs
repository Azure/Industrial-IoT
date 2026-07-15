// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Client.Subscriptions;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher compatibility facade over a public <see cref="ManagedSession"/>.
    /// </summary>
    /// <remarks>
    /// This type owns its managed inner session. Notification pooling is disabled because
    /// Publisher callers can retain received values after a notification callback returns.
    /// It is an unused composition seam; the classic session remains the production default.
    /// </remarks>
    internal sealed class ManagedOpcUaSession : IOpcUaSession, ISessionServices,
        IAsyncDisposable
    {
        /// <summary>
        /// Create a facade over a public managed session.
        /// </summary>
        public ManagedOpcUaSession(IManagedSessionConnection connection,
            ITelemetryContext telemetry, TimeProvider? timeProvider = null,
            TimeSpan? nodeCacheTimeout = null, int nodeCacheCapacity = 4096,
            bool disableComplexTypeLoading = false)
        {
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
            _telemetry = telemetry ??
                throw new ArgumentNullException(nameof(telemetry));
            _timeProvider = timeProvider ??
                TimeProvider.System;
            _disableComplexTypeLoading = disableComplexTypeLoading;
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nodeCacheCapacity);

            var session = _connection.Session;
            LruNodeCache = new LruNodeCache(new NodeCacheContext(session), _telemetry,
                nodeCacheTimeout ?? TimeSpan.FromMinutes(1), nodeCacheCapacity, true);
            Codec = new JsonVariantEncoder(session.MessageContext);
            CreatedAt = _timeProvider.GetUtcNow();
            ConnectivityState = session.Connected ?
                EndpointConnectivityState.Ready :
                EndpointConnectivityState.Disconnected;
            _connection.ConnectionStateChanged += OnConnectionStateChanged;

            // Do not allow pooled notification instances to outlive dispatch.
            if (session.TryGetSubscriptionManager(out ISubscriptionManager? subscriptions))
            {
                subscriptions.PoolNotifications = false;
            }
        }

        /// <inheritdoc/>
        public ISessionServices Services => this;

        /// <inheritdoc/>
        public ISystemContext SystemContext => _connection.Session.SystemContext;

        /// <inheritdoc/>
        public ILruNodeCache LruNodeCache { get; }

        /// <inheritdoc/>
        public IServiceMessageContext MessageContext => _connection.Session.MessageContext;

        /// <inheritdoc/>
        public IVariantEncoder Codec { get; }

        /// <summary>
        /// The identity used by the managed inner session.
        /// </summary>
        internal IUserIdentity Identity => _connection.Session.Identity;

        /// <summary>
        /// The configured endpoint selected for the managed inner session.
        /// </summary>
        internal ConfiguredEndpoint Endpoint => _connection.Session.ConfiguredEndpoint;

        /// <summary>
        /// The managed inner session identifier.
        /// </summary>
        internal NodeId SessionId => _connection.Session.SessionId;

        /// <summary>
        /// Time the facade was created.
        /// </summary>
        internal DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// The current managed-session connectivity state mapped to Publisher state.
        /// </summary>
        internal EndpointConnectivityState ConnectivityState { get; private set; }
            = EndpointConnectivityState.Disconnected;

        /// <summary>
        /// Raised when managed-session connectivity changes.
        /// </summary>
        internal event EventHandler<EndpointConnectivityStateEventArgs>? OnConnectionStateChange
        {
            add
            {
                _connectionStateChange += value;
                value?.Invoke(this, new EndpointConnectivityStateEventArgs(ConnectivityState));
            }
            remove => _connectionStateChange -= value;
        }

        /// <summary>
        /// Gets the public managed subscription manager associated with this session.
        /// </summary>
        internal bool TryGetSubscriptionManager(out ISubscriptionManager? manager)
        {
            return _connection.Session.TryGetSubscriptionManager(out manager);
        }

        /// <summary>
        /// Reconnects the public managed session used by this facade.
        /// </summary>
        internal Task ReconnectAsync(CancellationToken ct)
        {
            return _connection.ReconnectAsync(ct);
        }

        /// <inheritdoc/>
        public async ValueTask<ComplexTypeSystem?> GetComplexTypeSystemAsync(
            CancellationToken ct = default)
        {
            if (_disableComplexTypeLoading || !_connection.Session.Connected)
            {
                return null;
            }

            await _complexTypeGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_complexTypeSystem != null)
                {
                    return _complexTypeSystem;
                }

                var typeSystem = new ComplexTypeSystem(new NodeCacheResolver(
                    _connection.Session, LruNodeCache.Inner, _telemetry, _timeProvider),
                    _telemetry);
                await typeSystem.LoadAsync(throwOnError: false, ct: ct)
                    .ConfigureAwait(false);
                _complexTypeSystem = typeSystem;
                return typeSystem;
            }
            finally
            {
                _complexTypeGate.Release();
            }
        }

        /// <inheritdoc/>
        public ValueTask<OperationLimitsModel> GetOperationLimitsAsync(
            CancellationToken ct = default)
        {
            return new ValueTask<OperationLimitsModel>(GetOperationLimitsCoreAsync(ct));
        }

        private async Task<OperationLimitsModel> GetOperationLimitsCoreAsync(
            CancellationToken ct)
        {
            if (_operationLimits != null)
            {
                return _operationLimits;
            }
            _operationLimits = await FetchOperationLimitsAsync(ct).ConfigureAwait(false);
            return _operationLimits;
        }

        /// <inheritdoc/>
        public ValueTask<SessionDiagnosticsModel> GetServerDiagnosticAsync(
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var session = _connection.Session;
            return ValueTask.FromResult(new SessionDiagnosticsModel
            {
                SessionId = session.SessionId.AsString(MessageContext, NamespaceFormat.Expanded),
                SessionName = session.SessionName,
                ServerUri = session.Endpoint.Server?.ApplicationUri,
                ActualSessionTimeout = session.SessionTimeout,
                LastContactTime = session.LastKeepAliveTime,
                CurrentSubscriptionsCount = (uint)session.SubscriptionCount,
                CurrentPublishRequestsInQueue = (uint)session.OutstandingRequestCount
            });
        }

        /// <inheritdoc/>
        public async ValueTask<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct = default)
        {
            if (_serverCapabilities != null && namespaceFormat == NamespaceFormat.Uri)
            {
                return _serverCapabilities;
            }

            var capabilities = await FetchServerCapabilitiesAsync(namespaceFormat, ct)
                .ConfigureAwait(false);
            var result = capabilities ?? new ServerCapabilitiesModel
            {
                OperationLimits = await GetOperationLimitsAsync(ct).ConfigureAwait(false)
            };
            if (namespaceFormat == NamespaceFormat.Uri)
            {
                _serverCapabilities = result;
            }
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask<HistoryServerCapabilitiesModel> GetHistoryCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct = default)
        {
            if (_historyCapabilities != null && namespaceFormat == NamespaceFormat.Uri)
            {
                return _historyCapabilities;
            }

            var result = await FetchHistoryCapabilitiesAsync(namespaceFormat, ct)
                .ConfigureAwait(false) ?? new HistoryServerCapabilitiesModel();
            if (namespaceFormat == NamespaceFormat.Uri)
            {
                _historyCapabilities = result;
            }
            return result;
        }

        /// <inheritdoc/>
        public ValueTask<AddNodesResponse> AddNodesAsync(RequestHeader requestHeader,
            AddNodesItemCollection nodesToAdd, CancellationToken ct)
        {
            return _connection.Session.AddNodesAsync(requestHeader,
                new ArrayOf<AddNodesItem>(nodesToAdd.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<AddReferencesResponse> AddReferencesAsync(RequestHeader requestHeader,
            AddReferencesItemCollection referencesToAdd, CancellationToken ct)
        {
            return _connection.Session.AddReferencesAsync(requestHeader,
                new ArrayOf<AddReferencesItem>(referencesToAdd.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<BrowseResponse> BrowseAsync(RequestHeader requestHeader,
            ViewDescription? view, uint requestedMaxReferencesPerNode,
            BrowseDescriptionCollection nodesToBrowse, CancellationToken ct)
        {
            return _connection.Session.BrowseAsync(requestHeader, view,
                requestedMaxReferencesPerNode,
                new ArrayOf<BrowseDescription>(nodesToBrowse.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<BrowseNextResponse> BrowseNextAsync(RequestHeader requestHeader,
            bool releaseContinuationPoints, ByteStringCollection continuationPoints,
            CancellationToken ct)
        {
            return _connection.Session.BrowseNextAsync(requestHeader,
                releaseContinuationPoints,
                new ArrayOf<ByteString>(continuationPoints.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<CallResponse> CallAsync(RequestHeader requestHeader,
            CallMethodRequestCollection methodsToCall, CancellationToken ct)
        {
            return _connection.Session.CallAsync(requestHeader,
                new ArrayOf<CallMethodRequest>(methodsToCall.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<DeleteNodesResponse> DeleteNodesAsync(RequestHeader requestHeader,
            DeleteNodesItemCollection nodesToDelete, CancellationToken ct)
        {
            return _connection.Session.DeleteNodesAsync(requestHeader,
                new ArrayOf<DeleteNodesItem>(nodesToDelete.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<DeleteReferencesResponse> DeleteReferencesAsync(RequestHeader requestHeader,
            DeleteReferencesItemCollection referencesToDelete, CancellationToken ct)
        {
            return _connection.Session.DeleteReferencesAsync(requestHeader,
                new ArrayOf<DeleteReferencesItem>(referencesToDelete.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<HistoryReadResponse> HistoryReadAsync(RequestHeader requestHeader,
            ExtensionObject? historyReadDetails, Opc.Ua.TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints, HistoryReadValueIdCollection nodesToRead,
            CancellationToken ct)
        {
            return _connection.Session.HistoryReadAsync(requestHeader,
                historyReadDetails ?? ExtensionObject.Null, timestampsToReturn,
                releaseContinuationPoints,
                new ArrayOf<HistoryReadValueId>(nodesToRead.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<HistoryUpdateResponse> HistoryUpdateAsync(RequestHeader requestHeader,
            ExtensionObjectCollection historyUpdateDetails, CancellationToken ct)
        {
            return _connection.Session.HistoryUpdateAsync(requestHeader,
                new ArrayOf<ExtensionObject>(historyUpdateDetails.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<QueryFirstResponse> QueryFirstAsync(RequestHeader requestHeader,
            ViewDescription view, NodeTypeDescriptionCollection nodeTypes, ContentFilter filter,
            uint maxDataSetsToReturn, uint maxReferencesToReturn, CancellationToken ct)
        {
            return _connection.Session.QueryFirstAsync(requestHeader, view,
                new ArrayOf<NodeTypeDescription>(nodeTypes.ToArray()), filter,
                maxDataSetsToReturn, maxReferencesToReturn, ct);
        }

        /// <inheritdoc/>
        public ValueTask<QueryNextResponse> QueryNextAsync(RequestHeader requestHeader,
            bool releaseContinuationPoint, byte[] continuationPoint, CancellationToken ct)
        {
            return _connection.Session.QueryNextAsync(requestHeader,
                releaseContinuationPoint, (ByteString)continuationPoint, ct);
        }

        /// <inheritdoc/>
        public ValueTask<ReadResponse> ReadAsync(RequestHeader requestHeader, double maxAge,
            Opc.Ua.TimestampsToReturn timestampsToReturn, ReadValueIdCollection nodesToRead,
            CancellationToken ct)
        {
            return _connection.Session.ReadAsync(requestHeader, maxAge,
                timestampsToReturn,
                new ArrayOf<ReadValueId>(nodesToRead.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<RegisterNodesResponse> RegisterNodesAsync(RequestHeader requestHeader,
            NodeIdCollection nodesToRegister, CancellationToken ct)
        {
            return _connection.Session.RegisterNodesAsync(requestHeader,
                new ArrayOf<NodeId>(nodesToRegister.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<UnregisterNodesResponse> UnregisterNodesAsync(RequestHeader requestHeader,
            NodeIdCollection nodesToUnregister, CancellationToken ct)
        {
            return _connection.Session.UnregisterNodesAsync(requestHeader,
                new ArrayOf<NodeId>(nodesToUnregister.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<TranslateBrowsePathsToNodeIdsResponse>
            TranslateBrowsePathsToNodeIdsAsync(RequestHeader requestHeader,
                BrowsePathCollection browsePaths, CancellationToken ct)
        {
            return _connection.Session.TranslateBrowsePathsToNodeIdsAsync(
                requestHeader, new ArrayOf<BrowsePath>(browsePaths.ToArray()), ct);
        }

        /// <inheritdoc/>
        public ValueTask<WriteResponse> WriteAsync(RequestHeader requestHeader,
            WriteValueCollection nodesToWrite, CancellationToken ct)
        {
            return _connection.Session.WriteAsync(requestHeader,
                new ArrayOf<WriteValue>(nodesToWrite.ToArray()), ct);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _connection.ConnectionStateChanged -= OnConnectionStateChanged;
            LruNodeCache.Clear();
            _complexTypeGate.Dispose();
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        private void OnConnectionStateChanged(object? sender,
            ConnectionStateChangedEventArgs e)
        {
            _ = sender;
            var state = e.NewState switch
            {
                ConnectionState.Connected => EndpointConnectivityState.Ready,
                ConnectionState.Disconnected or ConnectionState.Closing or
                    ConnectionState.Closed => EndpointConnectivityState.Disconnected,
                _ => EndpointConnectivityState.Connecting
            };
            ConnectivityState = state;
            _connectionStateChange?.Invoke(this,
                new EndpointConnectivityStateEventArgs(state));
        }

        private async Task<OperationLimitsModel> FetchOperationLimitsAsync(
            CancellationToken ct)
        {
            var stackLimits = _connection.Session.OperationLimits;
            var nodeIds = new[]
            {
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
            var values = new List<DataValue>(nodeIds.Length);
            var maxNodesPerRead = Math.Max(1, (int)stackLimits.MaxNodesPerRead);
            foreach (var batch in nodeIds.Batch(maxNodesPerRead))
            {
                var requests = new ReadValueIdCollection(batch.Select(nodeId => new ReadValueId
                {
                    NodeId = new NodeId(nodeId),
                    AttributeId = Attributes.Value
                }));
                var response = await Services.ReadAsync(new RequestHeader(), 0,
                    Opc.Ua.TimestampsToReturn.Both, requests, ct).ConfigureAwait(false);
                if (response.Results.Count != requests.Count)
                {
                    throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                        "The server returned an incomplete operation limits response.");
                }
                foreach (var value in response.Results)
                {
                    if (StatusCode.IsBad(value.StatusCode))
                    {
                        throw new ServiceResultException(value.StatusCode);
                    }
                    values.Add(value);
                }
            }

            return new OperationLimitsModel
            {
                MaxArrayLength = Validate32(values[0].GetValueOrDefaultEx<uint?>()),
                MaxBrowseContinuationPoints =
                    Validate16(values[1].GetValueOrDefaultEx<ushort?>()),
                MaxByteStringLength = Validate32(values[2].GetValueOrDefaultEx<uint?>()),
                MaxHistoryContinuationPoints =
                    Validate16(values[3].GetValueOrDefaultEx<ushort?>()),
                MaxQueryContinuationPoints =
                    Validate16(values[4].GetValueOrDefaultEx<ushort?>()),
                MaxStringLength = Validate32(values[5].GetValueOrDefaultEx<uint?>()),
                MinSupportedSampleRate =
                    Validate64(values[6].GetValueOrDefaultEx<double?>()),
                MaxNodesPerHistoryReadData =
                    Validate32(values[7].GetValueOrDefaultEx<uint?>()),
                MaxNodesPerHistoryReadEvents =
                    Validate32(values[8].GetValueOrDefaultEx<uint?>()),
                MaxNodesPerWrite =
                    Validate32(values[9].GetValueOrDefaultEx<uint?>(), stackLimits.MaxNodesPerWrite),
                MaxNodesPerRead =
                    Validate32(values[10].GetValueOrDefaultEx<uint?>(), stackLimits.MaxNodesPerRead),
                MaxNodesPerHistoryUpdateData =
                    Validate32(values[11].GetValueOrDefaultEx<uint?>()),
                MaxNodesPerHistoryUpdateEvents =
                    Validate32(values[12].GetValueOrDefaultEx<uint?>()),
                MaxNodesPerMethodCall =
                    Validate32(values[13].GetValueOrDefaultEx<uint?>()),
                MaxNodesPerBrowse =
                    Validate32(values[14].GetValueOrDefaultEx<uint?>(), stackLimits.MaxNodesPerBrowse),
                MaxNodesPerRegisterNodes =
                    Validate32(values[15].GetValueOrDefaultEx<uint?>()),
                MaxNodesPerTranslatePathsToNodeIds =
                    Validate32(values[16].GetValueOrDefaultEx<uint?>()),
                MaxNodesPerNodeManagement =
                    Validate32(values[17].GetValueOrDefaultEx<uint?>()),
                MaxMonitoredItemsPerCall =
                    Validate32(values[18].GetValueOrDefaultEx<uint?>())
            };

            static uint? Validate32(uint? value, uint maximum = 0)
            {
                if (value is null or 0)
                {
                    return null;
                }
                var limit = maximum == 0 ? int.MaxValue : maximum;
                return Math.Min(limit, value is > 0 and < int.MaxValue ?
                    value.Value :
                    int.MaxValue);
            }

            static ushort? Validate16(ushort? value, ushort maximum = 0)
            {
                if (value is null or 0)
                {
                    return null;
                }
                return Math.Min(maximum == 0 ? ushort.MaxValue : maximum,
                    value > 0 ? value.Value : ushort.MaxValue);
            }

            static double? Validate64(double? value, double maximum = 0)
            {
                if (value is null or 0)
                {
                    return null;
                }
                return Math.Min(maximum == 0 ? double.MaxValue : maximum,
                    value > 0 ? value.Value : double.MaxValue);
            }
        }

        private async Task<ServerCapabilitiesModel?> FetchServerCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            var config = new ServerCapabilitiesState(null);
            config.ServerProfileArray =
                PropertyState<ArrayOf<string>>.With<VariantBuilder>(config);
            config.LocaleIdArray =
                PropertyState<ArrayOf<string>>.With<VariantBuilder>(config);
            config.ModellingRules =
                new FolderState(config);
            config.AggregateFunctions =
                new FolderState(config);
            config.Create(SystemContext, NodeId.Null,
                new QualifiedName(BrowseNames.ServerCapabilities), LocalizedText.Null, false);

            var relativePath = new RelativePath
            {
                Elements =
                [
                    new RelativePathElement
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasComponent,
                        IsInverse = false,
                        IncludeSubtypes = false,
                        TargetName = new QualifiedName(BrowseNames.ServerCapabilities)
                    }
                ]
            };
            var errorInfo = await this.ReadNodeStateAsync(new RequestHeader(), config,
                new NodeId(Objects.Server), relativePath, ct).ConfigureAwait(false);
            if (errorInfo != null)
            {
                return null;
            }

            var aggregateFunctionStates = new List<BaseInstanceState>();
            config.AggregateFunctions.GetChildren(SystemContext, aggregateFunctionStates);
            var aggregateFunctions = aggregateFunctionStates
                .OfType<BaseObjectState>()
                .ToDictionary(
                    state => state.BrowseName.AsString(MessageContext, namespaceFormat),
                    state => state.NodeId.AsString(MessageContext, namespaceFormat) ?? string.Empty);
            var modellingRuleStates = new List<BaseInstanceState>();
            config.ModellingRules.GetChildren(SystemContext, modellingRuleStates);
            var modellingRules = modellingRuleStates
                .OfType<BaseObjectState>()
                .ToDictionary(
                    state => state.BrowseName.AsString(MessageContext, namespaceFormat),
                    state => state.NodeId.AsString(MessageContext, namespaceFormat) ?? string.Empty);
            var conformanceUnits = config.ConformanceUnits.GetValueOrDefaultEx(
                values => values is { Count: > 0 } items ?
                    items.ToArray()!.Select(
                        item => item.AsString(MessageContext, namespaceFormat)).ToList() :
                    null);
            return new ServerCapabilitiesModel
            {
                OperationLimits = await GetOperationLimitsAsync(ct).ConfigureAwait(false),
                ModellingRules = modellingRules.Count == 0 ? null : modellingRules,
                SupportedLocales = config.LocaleIdArray.GetValueOrDefaultEx(
                    values => values is { Count: > 0 } items ? items.ToArray() : null),
                ServerProfiles = config.ServerProfileArray.GetValueOrDefaultEx(
                    values => values is { Count: > 0 } items ? items.ToArray() : null),
                AggregateFunctions = aggregateFunctions.Count == 0 ? null : aggregateFunctions,
                MaxSessions = config.MaxSessions.GetValueOrDefaultEx(),
                MaxSubscriptions = config.MaxSubscriptions.GetValueOrDefaultEx(),
                MaxMonitoredItems = config.MaxMonitoredItems.GetValueOrDefaultEx(),
                MaxMonitoredItemsPerSubscription =
                    config.MaxMonitoredItemsPerSubscription.GetValueOrDefaultEx(),
                MaxMonitoredItemsQueueSize =
                    config.MaxMonitoredItemsQueueSize.GetValueOrDefaultEx(),
                MaxSubscriptionsPerSession =
                    config.MaxSubscriptionsPerSession.GetValueOrDefaultEx(),
                MaxWhereClauseParameters =
                    config.MaxWhereClauseParameters.GetValueOrDefaultEx(),
                MaxSelectClauseParameters =
                    config.MaxSelectClauseParameters.GetValueOrDefaultEx(),
                ConformanceUnits = conformanceUnits
            };
        }

        private async Task<HistoryServerCapabilitiesModel?> FetchHistoryCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct)
        {
            var config = new HistoryServerCapabilitiesState(null);
            config.AccessHistoryDataCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.AccessHistoryEventsCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.MaxReturnDataValues =
                PropertyState<uint>.With<VariantBuilder>(config);
            config.MaxReturnEventValues =
                PropertyState<uint>.With<VariantBuilder>(config);
            config.InsertDataCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.ReplaceDataCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.UpdateDataCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.DeleteRawCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.DeleteAtTimeCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.InsertEventCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.ReplaceEventCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.UpdateEventCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.DeleteEventCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.InsertAnnotationCapability =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.ServerTimestampSupported =
                PropertyState<bool>.With<VariantBuilder>(config);
            config.AggregateFunctions =
                new FolderState(config);
            config.Create(SystemContext, NodeId.Null,
                new QualifiedName(BrowseNames.HistoryServerCapabilities),
                LocalizedText.Null, false);

            var relativePath = new RelativePath
            {
                Elements =
                [
                    new RelativePathElement
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasComponent,
                        IsInverse = false,
                        IncludeSubtypes = false,
                        TargetName = new QualifiedName(BrowseNames.HistoryServerCapabilities)
                    }
                ]
            };
            var errorInfo = await this.ReadNodeStateAsync(new RequestHeader(), config,
                new NodeId(Objects.Server_ServerCapabilities), relativePath, ct)
                .ConfigureAwait(false);
            if (errorInfo != null)
            {
                return null;
            }

            var supportsValues =
                config.AccessHistoryDataCapability.GetValueOrDefaultEx() ?? false;
            var supportsEvents =
                config.AccessHistoryEventsCapability.GetValueOrDefaultEx() ?? false;
            Dictionary<string, string>? aggregateFunctions = null;
            if (supportsEvents || supportsValues)
            {
                var aggregateFunctionStates = new List<BaseInstanceState>();
                config.AggregateFunctions.GetChildren(SystemContext, aggregateFunctionStates);
                aggregateFunctions = aggregateFunctionStates
                    .OfType<BaseObjectState>()
                    .ToDictionary(
                        state => state.BrowseName.AsString(MessageContext, namespaceFormat),
                        state => state.NodeId.AsString(MessageContext, namespaceFormat) ??
                            string.Empty);
            }
            return new HistoryServerCapabilitiesModel
            {
                AccessHistoryDataCapability = supportsValues,
                AccessHistoryEventsCapability = supportsEvents,
                MaxReturnDataValues = config.MaxReturnDataValues.GetValueOrDefaultEx(
                    value => !supportsValues ? null : value == 0 ? uint.MaxValue : value),
                MaxReturnEventValues = config.MaxReturnEventValues.GetValueOrDefaultEx(
                    value => !supportsEvents ? null : value == 0 ? uint.MaxValue : value),
                InsertDataCapability = config.InsertDataCapability.GetValueOrDefaultEx(),
                ReplaceDataCapability = config.ReplaceDataCapability.GetValueOrDefaultEx(),
                UpdateDataCapability = config.UpdateDataCapability.GetValueOrDefaultEx(),
                DeleteRawCapability = config.DeleteRawCapability.GetValueOrDefaultEx(),
                DeleteAtTimeCapability = config.DeleteAtTimeCapability.GetValueOrDefaultEx(),
                InsertEventCapability = config.InsertEventCapability.GetValueOrDefaultEx(),
                ReplaceEventCapability = config.ReplaceEventCapability.GetValueOrDefaultEx(),
                UpdateEventCapability = config.UpdateEventCapability.GetValueOrDefaultEx(),
                DeleteEventCapability = config.DeleteEventCapability.GetValueOrDefaultEx(),
                InsertAnnotationCapability =
                    config.InsertAnnotationCapability.GetValueOrDefaultEx(),
                ServerTimestampSupported =
                    config.ServerTimestampSupported.GetValueOrDefaultEx(),
                AggregateFunctions = aggregateFunctions is not { Count: > 0 } ?
                    null :
                    aggregateFunctions
            };
        }

        private ComplexTypeSystem? _complexTypeSystem;
        private OperationLimitsModel? _operationLimits;
        private ServerCapabilitiesModel? _serverCapabilities;
        private HistoryServerCapabilitiesModel? _historyCapabilities;
        private EventHandler<EndpointConnectivityStateEventArgs>? _connectionStateChange;
        private int _disposed;
        private readonly IManagedSessionConnection _connection;
        private readonly bool _disableComplexTypeLoading;
        private readonly ITelemetryContext _telemetry;
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _complexTypeGate = new(1, 1);
    }
}
