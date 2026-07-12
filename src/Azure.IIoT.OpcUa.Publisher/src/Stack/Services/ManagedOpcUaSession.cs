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
    using System;
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
            TimeSpan? nodeCacheTimeout = null, int nodeCacheCapacity = 4096)
        {
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
            _telemetry = telemetry ??
                throw new ArgumentNullException(nameof(telemetry));
            _timeProvider = timeProvider ??
                TimeProvider.System;
            if (nodeCacheCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeCacheCapacity));
            }

            var session = _connection.Session;
            LruNodeCache = new LruNodeCache(new NodeCacheContext(session), _telemetry,
                nodeCacheTimeout ?? TimeSpan.FromMinutes(1), nodeCacheCapacity, true);
            Codec = new JsonVariantEncoder(session.MessageContext);
            CreatedAt = _timeProvider.GetUtcNow();
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
        internal event EventHandler<EndpointConnectivityStateEventArgs>? OnConnectionStateChange;

        /// <inheritdoc/>
        public async ValueTask<ComplexTypeSystem?> GetComplexTypeSystemAsync(
            CancellationToken ct = default)
        {
            if (!_connection.Session.Connected)
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
                    _connection.Session, LruNodeCache.Inner, _telemetry, _timeProvider));
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
            ct.ThrowIfCancellationRequested();
            var limits = _connection.Session.OperationLimits;
            return ValueTask.FromResult(new OperationLimitsModel
            {
                MaxNodesPerRead = ToNullable(limits.MaxNodesPerRead),
                MaxNodesPerHistoryReadData = ToNullable(limits.MaxNodesPerHistoryReadData),
                MaxNodesPerHistoryReadEvents = ToNullable(limits.MaxNodesPerHistoryReadEvents),
                MaxNodesPerWrite = ToNullable(limits.MaxNodesPerWrite),
                MaxNodesPerHistoryUpdateData = ToNullable(limits.MaxNodesPerHistoryUpdateData),
                MaxNodesPerHistoryUpdateEvents = ToNullable(limits.MaxNodesPerHistoryUpdateEvents),
                MaxNodesPerMethodCall = ToNullable(limits.MaxNodesPerMethodCall),
                MaxNodesPerBrowse = ToNullable(limits.MaxNodesPerBrowse),
                MaxNodesPerRegisterNodes = ToNullable(limits.MaxNodesPerRegisterNodes),
                MaxNodesPerTranslatePathsToNodeIds =
                    ToNullable(limits.MaxNodesPerTranslateBrowsePathsToNodeIds),
                MaxNodesPerNodeManagement = ToNullable(limits.MaxNodesPerNodeManagement),
                MaxMonitoredItemsPerCall = ToNullable(limits.MaxMonitoredItemsPerCall)
            });
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
            _ = namespaceFormat;
            return new ServerCapabilitiesModel
            {
                OperationLimits = await GetOperationLimitsAsync(ct).ConfigureAwait(false)
            };
        }

        /// <inheritdoc/>
        public ValueTask<HistoryServerCapabilitiesModel> GetHistoryCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct = default)
        {
            _ = namespaceFormat;
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new HistoryServerCapabilitiesModel());
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
            ExtensionObject? historyReadDetails, TimestampsToReturn timestampsToReturn,
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
            TimestampsToReturn timestampsToReturn, ReadValueIdCollection nodesToRead,
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
            OnConnectionStateChange?.Invoke(this,
                new EndpointConnectivityStateEventArgs(state));
        }

        private static uint? ToNullable(uint value)
        {
            return value == 0 ? null : value;
        }

        private ComplexTypeSystem? _complexTypeSystem;
        private int _disposed;
        private readonly IManagedSessionConnection _connection;
        private readonly ITelemetryContext _telemetry;
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _complexTypeGate = new(1, 1);
    }
}
