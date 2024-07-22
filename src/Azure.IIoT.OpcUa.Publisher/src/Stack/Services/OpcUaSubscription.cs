// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Nito.AsyncEx;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Frozen;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription implementation
    /// </summary>
    [DataContract(Namespace = OpcUaClient.Namespace)]
    [KnownType(typeof(OpcUaMonitoredItem))]
    [KnownType(typeof(OpcUaMonitoredItem.DataChange))]
    [KnownType(typeof(OpcUaMonitoredItem.CyclicRead))]
    [KnownType(typeof(OpcUaMonitoredItem.Heartbeat))]
    [KnownType(typeof(OpcUaMonitoredItem.ModelChangeEventItem))]
    [KnownType(typeof(OpcUaMonitoredItem.Event))]
    [KnownType(typeof(OpcUaMonitoredItem.Condition))]
    [KnownType(typeof(OpcUaMonitoredItem.Field))]
    internal sealed class OpcUaSubscription : Subscription, ISubscriptionHandle,
        IOpcUaSubscription
    {
        /// <inheritdoc/>
        public string Name => _template.Id.Id;

        /// <inheritdoc/>
        public ushort LocalIndex { get; }

        /// <inheritdoc/>
        public IOpcUaClientDiagnostics State
            => (_client as IOpcUaClientDiagnostics) ?? OpcUaClient.Disconnected;

        /// <summary>
        /// Current metadata
        /// </summary>
        internal PublishedDataSetMetaDataModel? CurrentMetaData =>
            _metaDataLoader.IsValueCreated ? _metaDataLoader.Value.MetaData : null;

        /// <summary>
        /// Whether the subscription is online
        /// </summary>
        internal bool IsOnline
            => Handle != null && Session?.Connected == true && !_closed;

        /// <summary>
        /// Currently monitored but unordered
        /// </summary>
        private IEnumerable<OpcUaMonitoredItem> CurrentlyMonitored
            => _additionallyMonitored.Values
                .Concat(MonitoredItems
                .OfType<OpcUaMonitoredItem>());

        /// <summary>
        /// Subscription
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="callbacks"></param>
        /// <param name="template"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="metrics"></param>
        /// <param name="timeProvider"></param>
        internal OpcUaSubscription(IClientAccessor<ConnectionModel> clients,
            ISubscriptionCallbacks callbacks, SubscriptionModel template,
            IOptions<OpcUaClientOptions> options, ILoggerFactory loggerFactory,
            IMetricsContext metrics, TimeProvider? timeProvider = null)
        {
            _clients = clients ?? throw new ArgumentNullException(nameof(clients));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            _timeProvider = timeProvider ?? TimeProvider.System;
            _template = ValidateSubscriptionInfo(template);

            _logger = _loggerFactory.CreateLogger<OpcUaSubscription>();
            _additionallyMonitored = FrozenDictionary<uint, OpcUaMonitoredItem>.Empty;
            LocalIndex = Opc.Ua.SequenceNumber.Increment16(ref _lastIndex);

            Initialize();
            _metaDataLoader = new Lazy<MetaDataLoader>(() => new MetaDataLoader(this), true);
            _timer = _timeProvider.CreateTimer(OnSubscriptionManagementTriggered, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _keepAliveWatcher = _timeProvider.CreateTimer(OnKeepAliveMissing, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _monitoredItemWatcher = _timeProvider.CreateTimer(OnMonitoredItemWatchdog, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            InitializeMetrics();
            TriggerManageSubscription(true);
            ResetMonitoredItemWatchdogTimer(PublishingEnabled);
            Debug.Assert(_client != null);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="copyEventHandlers"></param>
        private OpcUaSubscription(OpcUaSubscription subscription, bool copyEventHandlers)
            : base(subscription, copyEventHandlers)
        {
            _clients = subscription._clients;
            _options = subscription._options;
            _loggerFactory = subscription._loggerFactory;
            _timeProvider = subscription._timeProvider;
            _metrics = subscription._metrics;
            _firstDataChangeReceived = subscription._firstDataChangeReceived;
            _template = ValidateSubscriptionInfo(subscription._template);
            _callbacks = subscription._callbacks;

            LocalIndex = subscription.LocalIndex;
            _client = subscription._client;
            _useDeferredAcknoledge = subscription._useDeferredAcknoledge;
            _logger = subscription._logger;
            _sequenceNumber = subscription._sequenceNumber;

            _goodMonitoredItems = subscription._goodMonitoredItems;
            _badMonitoredItems = subscription._badMonitoredItems;
            _lateMonitoredItems = subscription._lateMonitoredItems;
            _reportingItems = subscription._reportingItems;
            _disabledItems = subscription._disabledItems;
            _samplingItems = subscription._samplingItems;
            _notAppliedItems = subscription._notAppliedItems;

            _missingKeepAlives = subscription._missingKeepAlives;
            _unassignedNotifications = subscription._unassignedNotifications;

            _additionallyMonitored = subscription._additionallyMonitored;
            _currentSequenceNumber = subscription._currentSequenceNumber;
            _previousSequenceNumber = subscription._previousSequenceNumber;
            _continuouslyMissingKeepAlives = subscription._continuouslyMissingKeepAlives;
            _closed = subscription._closed;

            Initialize();
            _metaDataLoader = new Lazy<MetaDataLoader>(() => new MetaDataLoader(this), true);
            _timer = _timeProvider.CreateTimer(OnSubscriptionManagementTriggered, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _keepAliveWatcher = _timeProvider.CreateTimer(OnKeepAliveMissing, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _monitoredItemWatcher = _timeProvider.CreateTimer(OnMonitoredItemWatchdog, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            InitializeMetrics();

            if (!_closed)
            {
                TriggerManageSubscription(!_closed);
            }
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new OpcUaSubscription(this, true);
        }

        /// <inheritdoc/>
        public override Subscription CloneSubscription(bool copyEventHandlers)
        {
            return new OpcUaSubscription(this, copyEventHandlers);
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return $"{_template.Id.Id}:{Id}";
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not OpcUaSubscription subscription)
            {
                return false;
            }
            return subscription._template.Id.Equals(_template.Id);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _template.Id.GetHashCode();
        }

        /// <inheritdoc/>
        public bool TryGetCurrentPosition(out uint subscriptionId, out uint sequenceNumber)
        {
            subscriptionId = Id;
            sequenceNumber = _currentSequenceNumber;
            return _useDeferredAcknoledge;
        }

        /// <inheritdoc/>
        public void NotifySessionConnectionState(bool disconnected)
        {
            foreach (var item in CurrentlyMonitored)
            {
                item.NotifySessionConnectionState(disconnected);
            }
        }

        /// <inheritdoc/>
        public IOpcUaSubscriptionNotification? CreateKeepAlive()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    _logger.LogError("Subscription {Subscription} already DISPOSED!", this);
                    return null;
                }
                try
                {
                    var session = Session;
                    if (session == null)
                    {
                        return null;
                    }
                    return new Notification(this, Id, session.MessageContext)
                    {
                        ApplicationUri = session.Endpoint.Server.ApplicationUri,
                        EndpointUrl = session.Endpoint.EndpointUrl,
                        SubscriptionName = Name,
                        SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                        SubscriptionId = LocalIndex,
                        MessageType = MessageType.KeepAlive
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to create a subscription notification for subscription {Subscription}.",
                        this);
                    return null;
                }
            }
        }

        /// <inheritdoc/>
        public void Update(SubscriptionModel subscription)
        {
            Debug.Assert(!_closed);
            lock (_lock)
            {
                if (_disposed)
                {
                    _logger.LogError("Subscription {Subscription} already DISPOSED!", this);
                    return;
                }

                // Update subscription configuration
                var previousTemplateId = _template.Id;

                _template = ValidateSubscriptionInfo(subscription, previousTemplateId.Id);
                Debug.Assert(Name == previousTemplateId.Id, "The name must not change");

                // But connection information could have changed
                if (previousTemplateId != _template.Id)
                {
                    _logger.LogError("Upgrading subscription to different session.");

                    // Force closing of the subscription and ...
                    _forceRecreate = true;

                    // ... release client handle to cause closing of session if last reference.
                    _client?.Dispose();
                    _client = null;
                }

                TriggerManageSubscription(true);
            }
        }

        /// <inheritdoc/>
        public void Close()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    _logger.LogError("Subscription {Subscription} already DISPOSED!", this);
                    return;
                }

                Debug.Assert(!_closed);
                _closed = true;

                TriggerManageSubscription(true);
            }
        }

        /// <inheritdoc/>
        public async ValueTask CloseInSessionAsync(ISession? session, CancellationToken ct)
        {
            if (!_closed)
            {
                _logger.LogWarning("Hard close subscription {Subscription} in session {Session}.",
                    this, session);
                _closed = true;
            }

            // Finalize closing the subscription
            ResetKeepAliveTimer();
            ResetMonitoredItemWatchdogTimer(false);

            _callbacks.OnSubscriptionUpdated(null);

            // Does not throw
            await CloseCurrentSubscriptionAsync().ConfigureAwait(false);
            Debug.Assert(Session == null);

            lock (_lock)
            {
                _client?.Dispose();
                _client = null;
            }
        }

        /// <inheritdoc/>
        public async ValueTask SyncWithSessionAsync(ISession session, CancellationToken ct)
        {
            if (_disposed || _closed)
            {
                return;
            }
            try
            {
                await SyncWithSessionInternalAsync(session, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to apply state to Subscription {Subscription} in session {Session}...",
                    this, session);

                // Retry in 1 minute if not automatically retried
                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.SubscriptionErrorRetryDelay, kDefaultErrorRetryDelay);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    lock (_lock)
                    {
                        if (_disposed)
                        {
                            // Double dispose
                            Debug.Fail("Double dispose in subscription");
                            return;
                        }
                        try
                        {
                            ResetMonitoredItemWatchdogTimer(false);
                            _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                            FastDataChangeCallback = null;
                            FastEventCallback = null;
                            FastKeepAliveCallback = null;

                            PublishStatusChanged -= OnPublishStatusChange;
                            StateChanged -= OnStateChange;

                            // When the entire session is disposed and recreated we must still dispose
                            // all monitored items
                            var items = CurrentlyMonitored.ToList();
                            items.ForEach(item => item.Dispose());
                            RemoveItems(MonitoredItems);

                            _additionallyMonitored = FrozenDictionary<uint, OpcUaMonitoredItem>.Empty;
                            Debug.Assert(!CurrentlyMonitored.Any());

                            if (_closed)
                            {
                                _client?.Dispose();
                                _client = null;
                            }

                            _logger.LogInformation(
                                "Disposed of subscription {Subscription} with all {Count} items in it...",
                                this, items.Count);
                        }
                        finally
                        {
                            _keepAliveWatcher.Dispose();
                            _monitoredItemWatcher.Dispose();
                            _timer.Dispose();
                            _meter.Dispose();

                            _disposed = true;
                        }
                    }
                }

                Debug.Assert(!_disposed || FastDataChangeCallback == null);
                Debug.Assert(!_disposed || FastKeepAliveCallback == null);
                Debug.Assert(!_disposed || FastEventCallback == null);
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Send notification
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="notifications"></param>
        /// <param name="session"></param>
        /// <param name="dataSetName"></param>
        /// <param name="diagnosticsOnly"></param>
        internal void SendNotification(MessageType messageType,
            IEnumerable<MonitoredItemNotificationModel> notifications,
            ISession? session, string? dataSetName, bool diagnosticsOnly)
        {
            var curSession = session ?? Session;
            var messageContext = curSession?.MessageContext;

            if (messageContext == null)
            {
                if (session == null)
                {
                    // Can only send with context
                    _logger.LogDebug("Failed to send notification since no session exists " +
                        "to use as context. Notification was dropped.");
                    return;
                }
                _logger.LogWarning("A session was passed to send notification with but without " +
                    "message context. Using thread context.");
                messageContext = ServiceMessageContext.ThreadContext;
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var message = new Notification(this, Id, messageContext, notifications)
            {
                ApplicationUri = curSession?.Endpoint?.Server?.ApplicationUri,
                EndpointUrl = curSession?.Endpoint?.EndpointUrl,
                SubscriptionName = Name,
                DataSetName = dataSetName,
                SubscriptionId = LocalIndex,
                SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                MessageType = messageType
            };
#pragma warning restore CA2000 // Dispose objects before losing scope

            var count = message.GetDiagnosticCounters(out var modelChanges,
                out var heartbeats, out var overflows);
            if (messageType == MessageType.Event || messageType == MessageType.Condition)
            {
                if (!diagnosticsOnly)
                {
                    _callbacks.OnSubscriptionEventReceived(message);
                }
                if (count > 0)
                {
                    _callbacks.OnSubscriptionEventDiagnosticsChange(false,
                        count, overflows, modelChanges == 0 ? 0 : 1);
                }
            }
            else
            {
                if (!diagnosticsOnly)
                {
                    _callbacks.OnSubscriptionDataChangeReceived(message);
                }
                if (count > 0)
                {
                    _callbacks.OnSubscriptionDataDiagnosticsChange(false,
                        count, overflows, heartbeats);
                }
            }
        }

        /// <summary>
        /// Initialize state
        /// </summary>
        private void Initialize()
        {
            FastKeepAliveCallback = OnSubscriptionKeepAliveNotification;
            FastDataChangeCallback = OnSubscriptionDataChangeNotification;
            FastEventCallback = OnSubscriptionEventNotificationList;
            PublishStatusChanged += OnPublishStatusChange;
            StateChanged += OnStateChange;

            TimestampsToReturn = Opc.Ua.TimestampsToReturn.Both;
            DisableMonitoredItemCache = true;

            _callbacks.OnSubscriptionUpdated(_closed ? null : this);
        }

        /// <summary>
        /// Close subscription
        /// </summary>
        /// <returns></returns>
        private async Task CloseCurrentSubscriptionAsync()
        {
            ResetKeepAliveTimer();
            if (Handle == null)
            {
                // Already closed
                return;
            }

            Handle = null;
            try
            {
                _logger.LogDebug("Closing subscription '{Subscription}'...", this);

                // Dispose all monitored items
                var items = CurrentlyMonitored.ToList();

                _additionallyMonitored = FrozenDictionary<uint, OpcUaMonitoredItem>.Empty;
                _currentSequenceNumber = 0;
                _goodMonitoredItems = 0;
                _badMonitoredItems = 0;

                _reportingItems = 0;
                _disabledItems = 0;
                _samplingItems = 0;
                _notAppliedItems = 0;

                ResetMonitoredItemWatchdogTimer(false);

                await Try.Async(() => SetPublishingModeAsync(false)).ConfigureAwait(false);
                await Try.Async(() => DeleteItemsAsync(default)).ConfigureAwait(false);
                await Try.Async(() => ApplyChangesAsync()).ConfigureAwait(false);

                items.ForEach(item => item.Dispose());
                _logger.LogDebug("Deleted {Count} monitored items for '{Subscription}'.",
                    items.Count, this);

                await Try.Async(() => DeleteAsync(true)).ConfigureAwait(false);

                if (Session != null)
                {
                    await Session.RemoveSubscriptionAsync(this).ConfigureAwait(false);
                    Debug.Assert(Session == null);
                }

                Debug.Assert(!CurrentlyMonitored.Any());
                _logger.LogInformation("Subscription '{Subscription}' closed.", this);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to close subscription {Subscription}", this);
            }
        }

        /// <summary>
        /// Synchronize monitored items in subscription (no lock)
        /// </summary>
        /// <param name="monitoredItems"></param>
        /// <param name="ct"></param>
        private async Task<bool> SynchronizeMonitoredItemsAsync(
            IReadOnlyList<BaseMonitoredItemModel> monitoredItems, CancellationToken ct)
        {
            Debug.Assert(Session != null);
            if (Session is not OpcUaSession session)
            {
                return false;
            }

            TriggerSubscriptionManagementCallbackIn(Timeout.InfiniteTimeSpan);

            // Get limits to batch requests during resolve
            var operationLimits = await session.GetOperationLimitsAsync(
                ct).ConfigureAwait(false);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var desired = OpcUaMonitoredItem
                .Create(monitoredItems, _loggerFactory, _timeProvider, _client)
                .ToHashSet();
#pragma warning restore CA2000 // Dispose objects before losing scope

            var previouslyMonitored = CurrentlyMonitored.ToHashSet();
            var remove = previouslyMonitored.Except(desired).ToHashSet();
            var add = desired.Except(previouslyMonitored).ToHashSet();
            var same = previouslyMonitored.ToHashSet();
            same.IntersectWith(desired);

            //
            // Resolve the browse paths for all nodes first.
            //
            // We shortcut this only through the added items since the identity (hash)
            // of the monitored item is dependent on the node and browse path, so any
            // update of either results in a newly added monitored item and the old
            // one removed.
            //
            var allResolvers = add
                .Select(a => a.Resolve)
                .Where(a => a != null);
            foreach (var resolvers in allResolvers.Batch(
                operationLimits.GetMaxNodesPerTranslatePathsToNodeIds()))
            {
                var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                    new RequestHeader(), new BrowsePathCollection(resolvers
                        .Select(a => new BrowsePath
                        {
                            StartingNode = a!.Value.NodeId.ToNodeId(
                                session.MessageContext),
                            RelativePath = a.Value.Path.ToRelativePath(
                                session.MessageContext)
                        })), ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, s => s.StatusCode,
                    response.DiagnosticInfos, resolvers);
                if (results.ErrorInfo != null)
                {
                    // Could not do anything...
                    _logger.LogWarning(
                        "Failed to resolve browse path in {Subscription} due to {ErrorInfo}...",
                        this, results.ErrorInfo);
                    return false;
                }

                foreach (var result in results)
                {
                    var resolvedId = NodeId.Null;
                    if (result.ErrorInfo == null && result.Result.Targets.Count == 1)
                    {
                        resolvedId = result.Result.Targets[0].TargetId.ToNodeId(
                            session.MessageContext.NamespaceUris);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to resolve browse path for {NodeId} " +
                            "in {Subscription} due to '{ServiceResult}'",
                            result.Request!.Value.NodeId, this, result.ErrorInfo);
                    }
                    result.Request!.Value.Update(resolvedId, session.MessageContext);
                }
            }

            //
            // If retrieving paths for all the items from the root folder was configured do so
            // now. All items that fail here should be retried later.
            //
            if (_template.Configuration?.ResolveBrowsePathFromRoot == true)
            {
                var allGetPaths = add
                    .Select(a => a.GetPath)
                    .Where(a => a != null);
                var pathsRetrieved = 0;
                foreach (var getPathsBatch in allGetPaths.Batch(10000))
                {
                    var getPath = getPathsBatch.ToList();
                    var paths = await session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                        getPath.Select(n => n!.Value.NodeId.ToNodeId(session.MessageContext)),
                        ct).ConfigureAwait(false);
                    for (var index = 0; index < paths.Count; index++)
                    {
                        getPath[index]!.Value.Update(paths[index].Path, session.MessageContext);
                        if (paths[index].ErrorInfo != null)
                        {
                            _logger.LogWarning("Failed to get root path for {NodeId} " +
                                "in {Subscription} due to '{ServiceResult}'",
                                getPath[index]!.Value.NodeId, this, paths[index].ErrorInfo);
                        }
                        else
                        {
                            pathsRetrieved++;
                        }
                    }
                }
                if (pathsRetrieved > 0)
                {
                    _logger.LogInformation(
                        "Retrieved {Count} paths for items in subscription {Subscription}.",
                        pathsRetrieved, this);
                }
            }

            //
            // Register nodes for reading if needed. This is needed anytime the session
            // changes as the registration is only valid in the context of the session
            //
            // TODO: For now we do it every time for both added and merged item, but
            // this should be fixed to only be done when the session changed.
            //
            var allRegistrations = add.Concat(same)
                .Select(a => a.Register)
                .Where(a => a != null);
            foreach (var registrations in allRegistrations.Batch(
                operationLimits.GetMaxNodesPerRegisterNodes()))
            {
                var response = await session.Services.RegisterNodesAsync(
                    new RequestHeader(), new NodeIdCollection(registrations
                        .Select(a => a!.Value.NodeId.ToNodeId(session.MessageContext))),
                    ct).ConfigureAwait(false);
                foreach (var (First, Second) in response.RegisteredNodeIds.Zip(registrations))
                {
                    Debug.Assert(Second != null);
                    if (!NodeId.IsNull(First))
                    {
                        Second.Value.Update(First, session.MessageContext);
                    }
                }
            }

            var metadataChanged = false;
            var applyChanges = false;
            var updated = 0;
            var errors = 0;

            foreach (var toUpdate in same)
            {
                if (!desired.TryGetValue(toUpdate, out var theDesiredUpdate))
                {
                    errors++;
                    continue;
                }
                desired.Remove(theDesiredUpdate);
                Debug.Assert(toUpdate.GetType() == theDesiredUpdate.GetType());
                try
                {
                    if (toUpdate.MergeWith(theDesiredUpdate, session, out var metadata))
                    {
                        _logger.LogDebug(
                            "Trying to update monitored item '{Item}' in {Subscription}...",
                            toUpdate, this);
                        if (toUpdate.FinalizeMergeWith != null && metadata)
                        {
                            await toUpdate.FinalizeMergeWith(session, ct).ConfigureAwait(false);
                        }
                        updated++;
                        applyChanges = true;
                    }
                    if (metadata)
                    {
                        metadataChanged = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to update monitored item '{Item}' in {Subscription}...",
                        toUpdate, this);
                    errors++;
                }
                finally
                {
                    theDesiredUpdate.Dispose();
                }
            }

            var removed = 0;
            foreach (var toRemove in remove)
            {
                try
                {
                    if (toRemove.RemoveFrom(this, out var metadata))
                    {
                        _logger.LogDebug(
                            "Trying to remove monitored item '{Item}' from {Subscription}...",
                            toRemove, this);
                        removed++;
                        applyChanges = true;
                    }
                    if (metadata)
                    {
                        metadataChanged = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to remove monitored item '{Item}' from {Subscription}...",
                        toRemove, this);
                    errors++;
                }
            }

            var added = 0;
            foreach (var toAdd in add)
            {
                desired.Remove(toAdd);
                try
                {
                    if (toAdd.AddTo(this, session, out var metadata))
                    {
                        _logger.LogDebug(
                            "Adding monitored item '{Item}' to {Subscription}...",
                            toAdd, this);

                        if (toAdd.FinalizeAddTo != null && metadata)
                        {
                            await toAdd.FinalizeAddTo(session, ct).ConfigureAwait(false);
                        }
                        added++;
                        applyChanges = true;
                    }
                    if (metadata)
                    {
                        metadataChanged = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to add monitored item '{Item}' to {Subscription}...",
                        toAdd, this);
                    errors++;
                }
            }

            Debug.Assert(desired.Count == 0, "We should have processed all desired updates.");
            var noErrorFound = errors == 0;

            if (applyChanges)
            {
                await ApplyChangesAsync(ct).ConfigureAwait(false);
                if (MonitoredItemCount == 0 &&
                    _template.Configuration?.EnableImmediatePublishing != true)
                {
                    await SetPublishingModeAsync(false, ct).ConfigureAwait(false);

                    _logger.LogInformation(
                        "Disabled empty Subscription {Subscription} in session {Session}.",
                        this, session);

                    ResetMonitoredItemWatchdogTimer(false);
                }
            }

            // Perform second pass over all monitored items and complete.
            applyChanges = false;
            var invalidItems = 0;

            var desiredMonitoredItems = same;
            desiredMonitoredItems.UnionWith(add);

            //
            // Resolve display names for all nodes that still require a name
            // other than the node id string.
            //
            // Note that we use the desired set here and update the display
            // name after AddTo/MergeWith as it only effects the messages
            // and metadata emitted and not the item as it is set up in the
            // subscription (like what we do when resolving nodes). This
            // supports the scenario where the user sets a desired display
            // name of null to force reading the display name from the node
            // and updating the existing display name (previously set) and
            // at the same time is quite effective to only update what is
            // necessary.
            //
            var allDisplayNameUpdates = desiredMonitoredItems
                .Select(a => a.GetDisplayName)
                .Where(a => a != null)
                .ToList();
            if (allDisplayNameUpdates.Count > 0)
            {
                foreach (var displayNameUpdates in allDisplayNameUpdates.Batch(
                    operationLimits.GetMaxNodesPerRead()))
                {
                    var response = await session.Services.ReadAsync(new RequestHeader(),
                        0, Opc.Ua.TimestampsToReturn.Neither, new ReadValueIdCollection(
                        displayNameUpdates.Select(a => new ReadValueId
                        {
                            NodeId = a!.Value.NodeId.ToNodeId(session.MessageContext),
                            AttributeId = (uint)NodeAttribute.DisplayName
                        })), ct).ConfigureAwait(false);
                    var results = response.Validate(response.Results,
                        s => s.StatusCode, response.DiagnosticInfos, displayNameUpdates);

                    if (results.ErrorInfo != null)
                    {
                        _logger.LogWarning(
                            "Failed to resolve display name in {Subscription} due to {ErrorInfo}...",
                            this, results.ErrorInfo);

                        // We will retry later.
                        noErrorFound = false;
                    }
                    else
                    {
                        foreach (var result in results)
                        {
                            string? displayName = null;
                            if (result.Result.Value is not null)
                            {
                                displayName =
                                    (result.Result.Value as LocalizedText)?.ToString();
                                metadataChanged = true;
                            }
                            else
                            {
                                _logger.LogWarning("Failed to read display name for {NodeId} " +
                                    "in {Subscription} due to '{ServiceResult}'",
                                    result.Request!.Value.NodeId, this, result.ErrorInfo);
                            }
                            result.Request!.Value.Update(displayName ?? string.Empty);
                        }
                    }
                }
            }

            _logger.LogDebug(
                "Completing {Count} same/added and {Removed} removed items in subscription {Subscription}...",
                desiredMonitoredItems.Count, remove.Count, this);
            foreach (var monitoredItem in desiredMonitoredItems.Concat(remove))
            {
                if (!monitoredItem.TryCompleteChanges(this, ref applyChanges, SendNotification))
                {
                    // Apply more changes in future passes
                    invalidItems++;
                }
            }

            Debug.Assert(remove.All(m => !m.Valid), "All removed items should be invalid now");
            var set = desiredMonitoredItems.Where(m => m.Valid).ToList();
            _logger.LogDebug(
                "Completed {Count} valid and {Invalid} invalid items in subscription {Subscription}...",
                set.Count, desiredMonitoredItems.Count - set.Count, this);

            var finalize = set
                .Where(i => i.FinalizeCompleteChanges != null)
                .Select(i => i.FinalizeCompleteChanges!(ct))
                .ToArray();
            if (finalize.Length > 0)
            {
                await Task.WhenAll(finalize).ConfigureAwait(false);
            }

            if (applyChanges)
            {
                // Apply any additional changes
                await ApplyChangesAsync(ct).ConfigureAwait(false);
            }

            //
            // Ensure metadata is already available when we enable publishing if this
            // is a new subscription. This ensures that the meta data is part of the
            // first notification if we want. We store the metadata in a task variable
            // and materialize it first time we need it.
            //
            // TODO: We need a versioning scheme to align the metadata changes with the
            // notifications we receive. Right now if not initial change it is possible
            // that notifications arrive from previous state that already have the new
            // metadata. Then we need a way to retain the previous metadata until
            // switching over.
            //
            Debug.Assert(set.Select(m => m.ClientHandle).Distinct().Count() == set.Count,
                "Client handles are not distinct or one of the items is null");
            if (metadataChanged)
            {
                var threshold =
                    _template.Configuration?.AsyncMetaDataLoadThreshold
                        ?? 30; // Synchronous loading for 30 or less items
                var tcs = (set.Count <= threshold) ? new TaskCompletionSource() : null;
                var args = new MetaDataLoader.MetaDataLoaderArguments(tcs, session,
                    session.NamespaceUris, _timeProvider, set.OrderBy(m => m.ClientHandle));
                _metaDataLoader.Value.Reload(args);
                if (tcs != null)
                {
                    await tcs.Task.ConfigureAwait(false);
                }
                metadataChanged = false;
            }

            _logger.LogDebug(
                "Setting monitoring mode on {Count} items in subscription {Subscription}...",
                set.Count, this);

            //
            // Finally change the monitoring mode as required. Batch the requests
            // on the update of monitored item state from monitored items. On AddTo
            // the monitoring mode was already configured. This is for updates as
            // they are not applied through ApplyChanges
            //
            foreach (var change in set.GroupBy(i => i.GetMonitoringModeChange()))
            {
                if (change.Key == null)
                {
                    // Not a valid item
                    continue;
                }

                foreach (var itemsBatch in change.Batch(
                    operationLimits.GetMaxMonitoredItemsPerCall()))
                {
                    var itemsToChange = itemsBatch.Cast<MonitoredItem>().ToList();
                    _logger.LogInformation(
                        "Set monitoring to {Value} for {Count} items in subscription {Subscription}.",
                        change.Key.Value, itemsToChange.Count, this);

                    var results = await SetMonitoringModeAsync(change.Key.Value,
                        itemsToChange, ct).ConfigureAwait(false);
                    if (results != null)
                    {
                        var erroneousResultsCount = results
                            .Count(r => r != null && StatusCode.IsNotGood(r.StatusCode));

                        // Check the number of erroneous results and log.
                        if (erroneousResultsCount > 0)
                        {
                            _logger.LogWarning(
                                "Failed to set monitoring for {Count} items in subscription {Subscription}.",
                                erroneousResultsCount, this);
                            for (var i = 0; i < results.Count && i < itemsToChange.Count; ++i)
                            {
                                if (StatusCode.IsNotGood(results[i].StatusCode))
                                {
                                    _logger.LogWarning("Set monitoring for item '{Item}' in "
                                        + "subscription {Subscription} failed with '{Status}'.",
                                        itemsToChange[i].StartNodeId, this, results[i].StatusCode);
                                }
                            }
                            noErrorFound = false;
                        }
                    }
                }
            }

            finalize = set
                .Where(i => i.FinalizeMonitoringModeChange != null)
                .Select(i => i.FinalizeMonitoringModeChange!(ct))
                .ToArray();
            if (finalize.Length > 0)
            {
                await Task.WhenAll(finalize).ConfigureAwait(false);
            }

            // Cleanup all items that are not in the currently monitoring list
            var dispose = previouslyMonitored
                .Except(set)
                .ToList();
            dispose.ForEach(m => m.Dispose());

            // Update subscription state
            _additionallyMonitored = set
                .Where(m => !m.AttachedToSubscription)
                .ToFrozenDictionary(m => m.ClientHandle, m => m);

            _badMonitoredItems = invalidItems;
            _goodMonitoredItems = Math.Max(set.Count - invalidItems, 0);
            _reportingItems = set
                .Count(r => r.Status?.MonitoringMode == Opc.Ua.MonitoringMode.Reporting);
            _disabledItems = set
                .Count(r => r.Status?.MonitoringMode == Opc.Ua.MonitoringMode.Disabled);
            _samplingItems = set
                .Count(r => r.Status?.MonitoringMode == Opc.Ua.MonitoringMode.Sampling);
            _notAppliedItems = set
                .Count(r => r.Status?.MonitoringMode != r.MonitoringMode);

            _logger.LogInformation(@"{Subscription} - Now monitoring {Count} nodes:
# Good/Bad:     {Good}/{Bad}
# Reporting:    {Reporting}
# Sampling:     {Sampling}
# Disabled:     {Disabled}
# Not applied:  {NotApplied}
# Removed:      {Disposed}",
                this, set.Count,
                _goodMonitoredItems, _badMonitoredItems,
                _reportingItems,
                _samplingItems,
                _disabledItems,
                _notAppliedItems,
                dispose.Count);

            // Refresh condition
            if (set.OfType<OpcUaMonitoredItem.Condition>().Any())
            {
                _logger.LogInformation(
                    "Issuing ConditionRefresh on subscription {Subscription}", this);
                try
                {
                    await ConditionRefreshAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation("ConditionRefresh on subscription " +
                        "{Subscription} has completed.", this);
                }
                catch (Exception e)
                {
                    _logger.LogInformation("ConditionRefresh on subscription " +
                        "{Subscription} failed with an exception '{Message}'",
                        this, e.Message);
                    noErrorFound = false;
                }
                if (noErrorFound)
                {
                    _logger.LogInformation("ConditionRefresh on subscription " +
                        "{Subscription} has completed.", this);
                }
            }

            // Set up subscription management trigger
            if (invalidItems != 0)
            {
                // There were items that could not be added to subscription
                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.InvalidMonitoredItemRetryDelayDuration, TimeSpan.FromMinutes(5));
            }
            else if (desiredMonitoredItems.Count != set.Count)
            {
                // There were items !Valid but desired.
                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.BadMonitoredItemRetryDelayDuration, TimeSpan.FromMinutes(30));
            }
            else
            {
                // Nothing to do
                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.SubscriptionManagementIntervalDuration, Timeout.InfiniteTimeSpan);
            }

            return noErrorFound;
        }

        /// <summary>
        /// Resets the operation timeout on the session accrding to the
        /// publishing intervals on all subscriptions.
        /// </summary>
        private void ReapplySessionOperationTimeout()
        {
            Debug.Assert(Session != null);

            var currentOperationTimeout = _options.Value.Quotas.OperationTimeout;
            var localMaxOperationTimeout =
                PublishingInterval * (int)KeepAliveCount;
            if (currentOperationTimeout < localMaxOperationTimeout)
            {
                currentOperationTimeout = localMaxOperationTimeout;
            }

            foreach (var subscription in Session.Subscriptions)
            {
                localMaxOperationTimeout = (int)subscription.CurrentPublishingInterval
                    * (int)subscription.CurrentKeepAliveCount;
                if (currentOperationTimeout < localMaxOperationTimeout)
                {
                    currentOperationTimeout = localMaxOperationTimeout;
                }
            }
            if (Session.OperationTimeout != currentOperationTimeout)
            {
                Session.OperationTimeout = currentOperationTimeout;
            }
        }

        /// <summary>
        /// Apply state to session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask SyncWithSessionInternalAsync(ISession session,
            CancellationToken ct)
        {
            if (session?.Connected != true)
            {
                _logger.LogError(
                    "Session {Session} for {Subscription} not connected.",
                    session, this);
                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.CreateSessionTimeoutDuration, TimeSpan.FromSeconds(10));
                return;
            }

            if (_forceRecreate)
            {
                _forceRecreate = false;
                _logger.LogInformation(
                    "Closing subscription {Subscription} and then re-creating...", this);
                // Does not throw
                await CloseCurrentSubscriptionAsync().ConfigureAwait(false);
                Debug.Assert(Session == null);
            }

            // Synchronize subscription through the session.
            await SynchronizeSubscriptionAsync(session, ct).ConfigureAwait(false);
            Debug.Assert(Session != null);
            Debug.Assert(Session == session);

            if (_template.MonitoredItems != null)
            {
                // Resolves and sets the monitored items in the subscription
                await SynchronizeMonitoredItemsAsync(_template.MonitoredItems,
                    ct).ConfigureAwait(false);
            }

            if (ChangesPending)
            {
                await ApplyChangesAsync(ct).ConfigureAwait(false);
            }

            var shouldEnable = MonitoredItems
                .OfType<OpcUaMonitoredItem>()
                .Any(m => m.Valid && m.MonitoringMode != Opc.Ua.MonitoringMode.Disabled);
            if (PublishingEnabled ^ shouldEnable)
            {
                await SetPublishingModeAsync(shouldEnable, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "{State} Subscription {Subscription} in session {Session}.",
                    shouldEnable ? "Enabled" : "Disabled", this, session);

                ResetMonitoredItemWatchdogTimer(shouldEnable);
            }
        }

        /// <summary>
        /// Get a subscription with the supplied configuration (no lock)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private async ValueTask SynchronizeSubscriptionAsync(ISession session, CancellationToken ct)
        {
            Debug.Assert(session.DefaultSubscription != null, "No default subscription template.");

            GetSubscriptionConfiguration(session.DefaultSubscription,
                out var configuredPublishingInterval, out var configuredPriority,
                out var configuredKeepAliveCount, out var configuredLifetimeCount,
                out var configuredMaxNotificationsPerPublish);

            if (Handle == null)
            {
                var enablePublishing =
                    _template.Configuration?.EnableImmediatePublishing ?? false;
                var sequentialPublishing =
                    _template.Configuration?.EnableSequentialPublishing ?? false;
                var republishAfterTransfer =
                    _template.Configuration?.RepublishAfterTransfer ?? false;

                Handle = LocalIndex;
                DisplayName = Name;
                PublishingEnabled = enablePublishing;
                KeepAliveCount = configuredKeepAliveCount;
                PublishingInterval = configuredPublishingInterval;
                MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish;
                LifetimeCount = configuredLifetimeCount;
                Priority = configuredPriority;

                // TODO: use a channel and reorder task before calling OnMessage
                // to order or else republish is called too often
                RepublishAfterTransfer = republishAfterTransfer;
                SequentialPublishing = sequentialPublishing;

                var result = session.AddSubscription(this);
                Debug.Assert(result, "session should not already contain this subscription");
                Debug.Assert(Session == session);

                ReapplySessionOperationTimeout();

                _logger.LogInformation(
                    "Creating new {State} subscription {Subscription} in session {Session}.",
                    PublishingEnabled ? "enabled" : "disabled", this, session);

                Debug.Assert(enablePublishing == PublishingEnabled);
                Debug.Assert(Session != null);
                await CreateAsync(ct).ConfigureAwait(false);
                if (!Created)
                {
                    Handle = null;
                    await session.RemoveSubscriptionAsync(this, ct).ConfigureAwait(false);
                    Debug.Assert(Session == null);
                    throw new ServiceResultException(StatusCodes.BadSubscriptionIdInvalid,
                        $"Failed to create subscription {this} in session {session}");
                }

                ResetMonitoredItemWatchdogTimer(enablePublishing);
                LogRevisedValues(true);
                Debug.Assert(Id != 0);
                Debug.Assert(Created);

                _firstDataChangeReceived = false;
                _useDeferredAcknoledge = _template.Configuration?.UseDeferredAcknoledgements
                    ?? false;
            }
            else
            {
                // Apply new configuration on configuration on original subscription
                var modifySubscription = false;

                if (configuredKeepAliveCount != KeepAliveCount)
                {
                    _logger.LogInformation(
                        "Change KeepAliveCount to {New} in Subscription {Subscription}...",
                        configuredKeepAliveCount, this);

                    KeepAliveCount = configuredKeepAliveCount;
                    modifySubscription = true;
                }
                if (PublishingInterval != configuredPublishingInterval)
                {
                    _logger.LogInformation(
                        "Change publishing interval to {New} in Subscription {Subscription}...",
                        configuredPublishingInterval, this);
                    PublishingInterval = configuredPublishingInterval;
                    modifySubscription = true;
                }

                if (MaxNotificationsPerPublish != configuredMaxNotificationsPerPublish)
                {
                    _logger.LogInformation(
                        "Change MaxNotificationsPerPublish to {New} in Subscription {Subscription}",
                        configuredMaxNotificationsPerPublish, this);
                    MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish;
                    modifySubscription = true;
                }

                if (LifetimeCount != configuredLifetimeCount)
                {
                    _logger.LogInformation(
                        "Change LifetimeCount to {New} in Subscription {Subscription}...",
                        configuredLifetimeCount, this);
                    LifetimeCount = configuredLifetimeCount;
                    modifySubscription = true;
                }
                if (Priority != configuredPriority)
                {
                    _logger.LogInformation(
                        "Change Priority to {New} in Subscription {Subscription}...",
                        configuredPriority, this);
                    Priority = configuredPriority;
                    modifySubscription = true;
                }
                if (modifySubscription)
                {
                    await ModifyAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation(
                        "Subscription {Subscription} in session {Session} successfully modified.",
                        this, session);
                    LogRevisedValues(false);
                    ResetMonitoredItemWatchdogTimer(PublishingEnabled);
                }
            }
            ResetKeepAliveTimer();
        }

        /// <summary>
        /// Log revised values of the subscription
        /// </summary>
        /// <param name="created"></param>
        private void LogRevisedValues(bool created)
        {
            _logger.LogInformation(@"Successfully {Action} subscription {Subscription}'.
Actual (revised) state/desired state:
# PublishingEnabled {CurrentPublishingEnabled}/{PublishingEnabled}
# PublishingInterval {CurrentPublishingInterval}/{PublishingInterval}
# KeepAliveCount {CurrentKeepAliveCount}/{KeepAliveCount}
# LifetimeCount {CurrentLifetimeCount}/{LifetimeCount}", created ? "created" : "modified",
                this,
                CurrentPublishingEnabled, PublishingEnabled,
                CurrentPublishingInterval, PublishingInterval,
                CurrentKeepAliveCount, KeepAliveCount,
                CurrentLifetimeCount, LifetimeCount);
        }

        /// <summary>
        /// Get configuration
        /// </summary>
        /// <param name="defaultSubscription"></param>
        /// <param name="publishingInterval"></param>
        /// <param name="priority"></param>
        /// <param name="keepAliveCount"></param>
        /// <param name="lifetimeCount"></param>
        /// <param name="maxNotificationsPerPublish"></param>
        private void GetSubscriptionConfiguration(Subscription defaultSubscription,
            out int publishingInterval, out byte priority, out uint keepAliveCount,
            out uint lifetimeCount, out uint maxNotificationsPerPublish)
        {
            publishingInterval = (int)((_template.Configuration?.PublishingInterval) ??
                TimeSpan.FromSeconds(1)).TotalMilliseconds;
            keepAliveCount = (_template.Configuration?.KeepAliveCount) ??
                defaultSubscription.KeepAliveCount;
            maxNotificationsPerPublish = (_template.Configuration?.MaxNotificationsPerPublish) ??
                defaultSubscription.MaxNotificationsPerPublish;
            lifetimeCount = (_template.Configuration?.LifetimeCount) ??
                defaultSubscription.LifetimeCount;
            priority = (_template.Configuration?.Priority) ??
                defaultSubscription.Priority;
        }

        /// <summary>
        /// Trigger subscription management callback
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="defaultDelay"></param>
        private void TriggerSubscriptionManagementCallbackIn(TimeSpan? delay,
            TimeSpan defaultDelay = default)
        {
            if (delay == null)
            {
                delay = defaultDelay;
            }
            else if (delay == TimeSpan.Zero)
            {
                delay = Timeout.InfiniteTimeSpan;
            }
            if (delay != Timeout.InfiniteTimeSpan)
            {
                _logger.LogInformation(
                    "Setting up trigger to reapply state to {Subscription} in {Timeout}...",
                    this, delay);
            }
            _timer.Change(delay.Value, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// The subscription management timer expired. This timer is used to
        /// retry applying state to the subscription in the current session
        /// if previous application failed.
        /// </summary>
        /// <param name="state"></param>
        private void OnSubscriptionManagementTriggered(object? state)
        {
            lock (_lock)
            {
                TriggerManageSubscription(false);
            }
        }

        /// <summary>
        /// Trigger managing of this subscription, ensure client exists if it is null
        /// </summary>
        /// <param name="ensureClientExists"></param>
        private void TriggerManageSubscription(bool ensureClientExists)
        {
            Debug.Assert(!_disposed);
            //
            // Ensure a client and session exists for this subscription. This takes a
            // reference that must be released when the subscription is closed or the
            // underlying connection information changes.
            //

            if (_client == null)
            {
                if (!ensureClientExists)
                {
                    return;
                }
                _client = _clients.GetOrCreateClient(_template.Id.Connection);
            }

            // Execute creation/update on the session management thread inside the client
            Debug.Assert(_client != null);

            _logger.LogInformation("Trigger management of subscription {Subscription}...",
                this);

            _client.ManageSubscription(this, _closed);
        }

        /// <summary>
        /// Handle event notification. Depending on the sequential publishing setting
        /// this will be called in order and thread safe or from different threads.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <param name="stringTable"></param>
        private void OnSubscriptionEventNotificationList(Subscription subscription,
            EventNotificationList notification, IList<string>? stringTable)
        {
            Debug.Assert(ReferenceEquals(subscription, this));
            Debug.Assert(!_disposed);

            if (notification?.Events == null)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} has empty notification.", this);
                return;
            }

            if (notification.Events.Count == 0)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} has no events.", this);
                return;
            }

            var session = Session;
            if (session is not IOpcUaSession sessionContext)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} received without a session {Session}.",
                    this, session);
                return;
            }

            ResetKeepAliveTimer();

            var sw = Stopwatch.StartNew();
            try
            {
                var sequenceNumber = notification.SequenceNumber;
                var publishTime = notification.PublishTime;

                Debug.Assert(notification.Events != null);

                if (sequenceNumber == 1)
                {
                    // Do not log when the sequence number is 1 after reconnect
                    _previousSequenceNumber = 1;
                }
                else if (!Opc.Ua.SequenceNumber.Validate(sequenceNumber, ref _previousSequenceNumber,
                    out var missingSequenceNumbers, out var dropped))
                {
                    _logger.LogWarning("Event subscription notification for subscription " +
                        "{Subscription} has unexpected sequenceNumber {SequenceNumber} missing " +
                        "{ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                        this, sequenceNumber,
                        Opc.Ua.SequenceNumber.ToString(missingSequenceNumbers), dropped ?
                            "dropped" : "already received", publishTime);
                }

                var numOfEvents = 0;
                var overflows = 0;
                foreach (var eventFieldList in notification.Events)
                {
                    Debug.Assert(eventFieldList != null);
                    if (TryGetMonitoredItemForNotification(eventFieldList.ClientHandle, out var monitoredItem))
                    {
#pragma warning disable CA2000 // Dispose objects before losing scope
                        var message = new Notification(this, Id, session.MessageContext, sequenceNumber: sequenceNumber)
                        {
                            ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                            EndpointUrl = session.Endpoint?.EndpointUrl,
                            SubscriptionName = Name,
                            DataSetName = monitoredItem.DataSetName,
                            SubscriptionId = LocalIndex,
                            SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                            MessageType = MessageType.Event,
                            PublishTimestamp = publishTime
                        };
#pragma warning restore CA2000 // Dispose objects before losing scope

                        if (!monitoredItem.TryGetMonitoredItemNotifications(message.SequenceNumber,
                            publishTime, eventFieldList, message.Notifications))
                        {
                            _logger.LogDebug("Skipping the monitored item notification for Event " +
                                "received for subscription {Subscription}", this);
                        }

                        if (message.Notifications.Count > 0)
                        {
                            _callbacks.OnSubscriptionEventReceived(message);
                            numOfEvents++;
                            overflows += message.Notifications.Sum(n => n.Overflow);
                        }
                        else
                        {
                            _logger.LogDebug("No notifications added to the message.");
                        }
                    }
                }
                _callbacks.OnSubscriptionEventDiagnosticsChange(true, overflows, numOfEvents, 0);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
            finally
            {
                _logger.LogDebug("Event callback took {Elapsed}", sw.Elapsed);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Spent more than 1 second in fast event callback.");
                }
            }
        }

        /// <summary>
        /// Handle keep alive messages
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnSubscriptionKeepAliveNotification(Subscription subscription,
            NotificationData notification)
        {
            Debug.Assert(ReferenceEquals(subscription, this));
            Debug.Assert(!_disposed);

            ResetKeepAliveTimer();

            if (!PublishingEnabled)
            {
                _logger.LogDebug(
                    "Keep alive event received while publishing is not enabled - skip.");
                return;
            }

            var session = Session;
            if (session is not IOpcUaSession)
            {
                _logger.LogWarning(
                    "Keep alive event for subscription {Subscription} received without session {Session}.",
                    this, session);
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var sequenceNumber = notification.SequenceNumber;
                var publishTime = notification.PublishTime;

                // in case of a keepalive,the sequence number is not incremented by the servers
                _logger.LogDebug("Keep alive for subscription {Subscription} " +
                    "with sequenceNumber {SequenceNumber}, publishTime {PublishTime}.",
                    this, sequenceNumber, publishTime);

#pragma warning disable CA2000 // Dispose objects before losing scope
                var message = new Notification(this, Id, session.MessageContext)
                {
                    ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = session.Endpoint?.EndpointUrl,
                    SubscriptionName = Name,
                    PublishTimestamp = publishTime,
                    SubscriptionId = LocalIndex,
                    SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType = MessageType.KeepAlive
                };
#pragma warning restore CA2000 // Dispose objects before losing scope

                _callbacks.OnSubscriptionKeepAlive(message);
                Debug.Assert(message.Notifications != null);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing keep alive notification");
            }
            finally
            {
                _logger.LogDebug("Keep alive callback took {Elapsed}", sw.Elapsed);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Spent more than 1 second in fast keep alive callback.");
                }
            }
        }

        /// <summary>
        /// Handle cyclic read notifications created by the client
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="values"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        public void OnSubscriptionCylicReadNotification(Subscription subscription,
            List<SampledDataValueModel> values, uint sequenceNumber, DateTime publishTime)
        {
            Debug.Assert(ReferenceEquals(subscription, this));
            Debug.Assert(!_disposed);
            var session = Session;
            if (session is not IOpcUaSession sessionContext)
            {
                _logger.LogWarning(
                    "DataChange for subscription {Subscription} received without session {Session}.",
                    this, session);
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                var message = new Notification(this, Id, session.MessageContext, sequenceNumber: sequenceNumber)
                {
                    ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = session.Endpoint?.EndpointUrl,
                    SubscriptionName = Name,
                    SubscriptionId = LocalIndex,
                    PublishTimestamp = publishTime,
                    SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType = MessageType.DeltaFrame
                };
#pragma warning restore CA2000 // Dispose objects before losing scope

                foreach (var cyclicDataChange in values.OrderBy(m => m.Value?.SourceTimestamp))
                {
                    if (TryGetMonitoredItemForNotification(cyclicDataChange.ClientHandle, out var monitoredItem) &&
                        !monitoredItem.TryGetMonitoredItemNotifications(message.SequenceNumber,
                            publishTime, cyclicDataChange, message.Notifications))
                    {
                        _logger.LogDebug("Skipping the cyclic read data change received for subscription {Subscription}",
                            this);
                    }
                }

                _callbacks.OnSubscriptionCyclicReadCompleted(message);
                Debug.Assert(message.Notifications != null);
                var count = message.GetDiagnosticCounters(out var _, out _, out var overflows);
                if (count > 0)
                {
                    _callbacks.OnSubscriptionCyclicReadDiagnosticsChange(count, overflows);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing cyclic read notification");
            }
            finally
            {
                _logger.LogDebug("Cyclic read callback took {Elapsed}", sw.Elapsed);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Spent more than 1 second in cyclic read callback.");
                }
            }
        }

        /// <summary>
        /// Handle data change notification. Depending on the sequential publishing setting
        /// this will be called in order and thread safe or from different threads.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <param name="stringTable"></param>
        private void OnSubscriptionDataChangeNotification(Subscription subscription,
            DataChangeNotification notification, IList<string>? stringTable)
        {
            Debug.Assert(ReferenceEquals(subscription, this));
            Debug.Assert(!_disposed);

            var firstDataChangeReceived = _firstDataChangeReceived;
            _firstDataChangeReceived = true;
            var session = Session;
            if (session is not IOpcUaSession sessionContext)
            {
                _logger.LogWarning(
                    "DataChange for subscription {Subscription} received without session {Session}.",
                    this, session);
                return;
            }

            ResetKeepAliveTimer();

            var sw = Stopwatch.StartNew();
            try
            {
                var sequenceNumber = notification.SequenceNumber;
                var publishTime = notification.PublishTime;

#pragma warning disable CA2000 // Dispose objects before losing scope
                var message = new Notification(this, Id, session.MessageContext,
                    sequenceNumber: sequenceNumber)
                {
                    ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = session.Endpoint?.EndpointUrl,
                    SubscriptionName = Name,
                    SubscriptionId = LocalIndex,
                    PublishTimestamp = publishTime,
                    SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType =
                        firstDataChangeReceived ? MessageType.DeltaFrame : MessageType.KeyFrame
                };
#pragma warning restore CA2000 // Dispose objects before losing scope

                Debug.Assert(notification.MonitoredItems != null);

                // All notifications have the same message and thus sequence number
                if (sequenceNumber == 1)
                {
                    // Do not log when the sequence number is 1 after reconnect
                    _previousSequenceNumber = 1;
                }
                else if (!Opc.Ua.SequenceNumber.Validate(sequenceNumber, ref _previousSequenceNumber,
                    out var missingSequenceNumbers, out var dropped))
                {
                    _logger.LogWarning("DataChange notification for subscription " +
                        "{Subscription} has unexpected sequenceNumber {SequenceNumber} " +
                        "missing {ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                        this, sequenceNumber,
                        Opc.Ua.SequenceNumber.ToString(missingSequenceNumbers),
                        dropped ? "dropped" : "already received", publishTime);
                }

                foreach (var item in notification.MonitoredItems)
                {
                    Debug.Assert(item != null);
                    if (TryGetMonitoredItemForNotification(item.ClientHandle, out var monitoredItem) &&
                        !monitoredItem.TryGetMonitoredItemNotifications(message.SequenceNumber,
                            publishTime, item, message.Notifications))
                    {
                        _logger.LogDebug(
                            "Skipping the monitored item notification for DataChange " +
                            "received for subscription {Subscription}", this);
                    }
                }

                _callbacks.OnSubscriptionDataChangeReceived(message);
                Debug.Assert(message.Notifications != null);
                var count = message.GetDiagnosticCounters(out var _, out var heartbeats, out var overflows);
                if (count > 0)
                {
                    _callbacks.OnSubscriptionDataDiagnosticsChange(true, count, overflows, heartbeats);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
            finally
            {
                _logger.LogDebug("Data change callback took {Elapsed}", sw.Elapsed);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Spent more than 1 second in fast data change callback.");
                }
            }
        }

        /// <summary>
        /// Get monitored item using client handle
        /// </summary>
        /// <param name="clientHandle"></param>
        /// <param name="monitoredItem"></param>
        /// <returns></returns>
        private bool TryGetMonitoredItemForNotification(uint clientHandle,
            [NotNullWhen(true)] out OpcUaMonitoredItem? monitoredItem)
        {
            monitoredItem = FindItemByClientHandle(clientHandle) as OpcUaMonitoredItem;
            if (monitoredItem != null || _additionallyMonitored.TryGetValue(clientHandle, out monitoredItem))
            {
                return true;
            }

            _unassignedNotifications++;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Monitored item not found with client handle {ClientHandle} in subscription {Subscription}.",
                    clientHandle, this);
            }
            return false;
        }

        /// <summary>
        /// Get notifications
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="notifications"></param>
        /// <returns></returns>
        private bool TryGetNotifications(uint sequenceNumber,
            [NotNullWhen(true)] out IList<MonitoredItemNotificationModel>? notifications)
        {
            lock (_lock)
            {
                try
                {
                    if (Handle == null)
                    {
                        notifications = null;
                        return false;
                    }
                    notifications = new List<MonitoredItemNotificationModel>();

                    // Ensure we order by client handle exactly like the meta data is ordered
                    foreach (var item in CurrentlyMonitored.OrderBy(m => m.ClientHandle))
                    {
                        item.TryGetLastMonitoredItemNotifications(sequenceNumber, notifications);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    notifications = null;
                    _logger.LogError(ex, "Failed to get a notifications from monitored " +
                        "items in subscription {Subscription}.", this);
                    return false;
                }
            }
        }

        /// <summary>
        /// Advance the position
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="sequenceNumber"></param>
        private void AdvancePosition(uint subscriptionId, uint? sequenceNumber)
        {
            if (sequenceNumber.HasValue && Id == subscriptionId)
            {
                _logger.LogTrace("Advancing stream #{SubscriptionId} to #{Position}",
                    subscriptionId, sequenceNumber);
                _currentSequenceNumber = sequenceNumber.Value;
            }
        }

        /// <summary>
        /// Reset keep alive timer
        /// </summary>
        private void ResetKeepAliveTimer()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _continuouslyMissingKeepAlives = 0;

            if (!IsOnline)
            {
                _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }

            var keepAliveTimeout = TimeSpan.FromMilliseconds(
                (CurrentPublishingInterval * (CurrentKeepAliveCount + 1)) + 1000);
            try
            {
                _keepAliveWatcher.Change(keepAliveTimeout, keepAliveTimeout);
            }
            catch (ArgumentOutOfRangeException)
            {
                _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Reset the monitored item watchdog
        /// </summary>
        /// <param name="publishingEnabled"></param>
        private void ResetMonitoredItemWatchdogTimer(bool publishingEnabled)
        {
            var timeout = _template.Configuration?.MonitoredItemWatchdogTimeout;
            if (timeout == null || timeout.Value == TimeSpan.Zero)
            {
                if (_lastMonitoredItemCheck == null)
                {
                    return;
                }
                publishingEnabled = false;
            }
            if (!publishingEnabled)
            {
                if (_lastMonitoredItemCheck != null)
                {
                    _logger.LogInformation(
                        "{Subscription}: Stopping monitored item watchdog ({Timeout}).",
                        this, timeout);
                }
                _lastMonitoredItemCheck = null;
                _monitoredItemWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
            else
            {
                if (_lastMonitoredItemCheck == null)
                {
                    _logger.LogInformation(
                        "{Subscription}: Restarting monitored item watchdog ({Timeout}).",
                        this, timeout);
                }

                _lastMonitoredItemCheck = _timeProvider.GetUtcNow();
                Debug.Assert(timeout.HasValue);
                _monitoredItemWatcher.Change(timeout.Value, timeout.Value);
            }
        }

        /// <summary>
        /// Checks status of monitored items
        /// </summary>
        /// <param name="state"></param>
        private void OnMonitoredItemWatchdog(object? state)
        {
            var action = _template.Configuration?.WatchdogBehavior
                ?? SubscriptionWatchdogBehavior.Diagnostic;
            lock (_timers)
            {
                if (_disposed || _monitoredItemWatcher == null)
                {
                    Debug.Fail("Should not be called after dispose");
                    return;
                }

                if (!IsOnline || _lastMonitoredItemCheck == null)
                {
                    // Stop watchdog
                    _monitoredItemWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    return;
                }

                if (_goodMonitoredItems == 0)
                {
                    _lastMonitoredItemCheck = _timeProvider.GetUtcNow();
                    return;
                }

                var lastCount = _lateMonitoredItems;
                var itemsChecked = 0;
                foreach (var item in CurrentlyMonitored)
                {
                    itemsChecked++;
                    if (item.WasLastValueReceivedBefore(_lastMonitoredItemCheck.Value))
                    {
                        _logger.LogDebug(
                            "Monitored item {Item} in subscription {Subscription} is late.",
                            item, this);
                        _lateMonitoredItems++;
                    }
                }
                _lastMonitoredItemCheck = _timeProvider.GetUtcNow();
                var missing = _lateMonitoredItems - lastCount;
                if (missing == 0)
                {
                    _logger.LogDebug("All monitored items in {Subscription} are reporting.",
                        this);
                    return;
                }
                if (action == SubscriptionWatchdogBehavior.Diagnostic)
                {
                    return;
                }
                if (itemsChecked != missing && _template.Configuration?.WatchdogCondition
                    != MonitoredItemWatchdogCondition.WhenAllAreLate)
                {
                    _logger.LogDebug("Some monitored items in {Subscription} are late.",
                        this);
                    return;
                }
                _logger.LogInformation("{Count} of the {Total} monitored items in " +
                    "{Subscription} are now late - running {Action} behavior action.",
                    missing, itemsChecked, this, action);
            }

            var msg = $"Performed watchdog action {action} for subscription {this} " +
                $"because it has {_lateMonitoredItems} late monitored items.";
            switch (action)
            {
                case SubscriptionWatchdogBehavior.Reset:
                    ResetMonitoredItemWatchdogTimer(false);
                    _forceRecreate = true;
                    OnSubscriptionManagementTriggered(this);
                    break;
                case SubscriptionWatchdogBehavior.FailFast:
                    Publisher.Runtime.FailFast(msg, null);
                    break;
                case SubscriptionWatchdogBehavior.ExitProcess:
                    Console.WriteLine(msg);
                    Publisher.Runtime.Exit(-10);
                    break;
            }
        }

        /// <summary>
        /// Called when keep alive callback was missing
        /// </summary>
        /// <param name="state"></param>
        private void OnKeepAliveMissing(object? state)
        {
            lock (_timers)
            {
                if (_disposed)
                {
                    Debug.Fail("Should not be called after dispose");
                    return;
                }

                if (!IsOnline)
                {
                    // Stop watchdog
                    _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    return;
                }

                _missingKeepAlives++;
                _continuouslyMissingKeepAlives++;

                if (_continuouslyMissingKeepAlives == CurrentLifetimeCount + 1)
                {
                    _logger.LogCritical(
                        "#{Count}/{Lifetimecount}: Keep alive count exceeded. Resetting {Subscription}...",
                        _continuouslyMissingKeepAlives, CurrentLifetimeCount, this);

                    // TODO: option to fail fast here
                    _forceRecreate = true;
                    OnSubscriptionManagementTriggered(this);
                }
                else
                {
                    _logger.LogInformation(
                        "#{Count}/{Lifetimecount}: Subscription {Subscription} is missing keep alive.",
                        _continuouslyMissingKeepAlives, CurrentLifetimeCount, this);
                }
            }
        }

        /// <summary>
        /// Publish status changed
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="e"></param>
        private void OnPublishStatusChange(Subscription subscription, PublishStateChangedEventArgs e)
        {
            if (_disposed)
            {
                // Debug.Fail("Should not be called after dispose");
                // This currently happens because the stack caches the callbacks!
                return;
            }

            if (e.Status.HasFlag(PublishStateChangedMask.Stopped) && !_publishingStopped)
            {
                _logger.LogInformation("Subscription {Subscription} STOPPED!", this);
                _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                ResetMonitoredItemWatchdogTimer(false);
                _publishingStopped = true;
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Recovered) && _publishingStopped)
            {
                _logger.LogInformation("Subscription {Subscription} RECOVERED!", this);
                ResetKeepAliveTimer();
                ResetMonitoredItemWatchdogTimer(true);
                _publishingStopped = false;
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Transferred))
            {
                _logger.LogInformation("Subscription {Subscription} transferred.", this);
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Republish))
            {
                _logger.LogInformation("Subscription {Subscription} republishing...", this);
            }
            if (e.Status.HasFlag(PublishStateChangedMask.KeepAlive))
            {
                _logger.LogTrace("Subscription {Subscription} keep alive.", this);
                ResetKeepAliveTimer();
            }
            if (e.Status.HasFlag(PublishStateChangedMask.Timeout))
            {
                _logger.LogWarning("Subscription {Subscription} timed out! Re-creating...", this);

                //
                // Timed out on server - this means that the subscription is gone and
                // needs to be recreated.
                //
                _forceRecreate = true;
                _publishingStopped = true;
                OnSubscriptionManagementTriggered(this);
            }
        }

        /// <summary>
        /// Subscription status changed
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="e"></param>
        private void OnStateChange(Subscription subscription, SubscriptionStateChangedEventArgs e)
        {
            if (_disposed)
            {
                // Debug.Fail("Should not be called after dispose");
                // This currently happens because the stack caches the callbacks!
                return;
            }

            if (e.Status.HasFlag(SubscriptionChangeMask.Created))
            {
                _logger.LogDebug("Subscription {Subscription} created.", this);
                _publishingStopped = false;
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.Deleted))
            {
                _logger.LogDebug("Subscription {Subscription} deleted.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.Modified))
            {
                _logger.LogDebug("Subscription {Subscription} modified", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsAdded))
            {
                _logger.LogDebug("Subscription {Subscription} items added.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsRemoved))
            {
                _logger.LogDebug("Subscription {Subscription} items removed.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsCreated))
            {
                _logger.LogDebug("Subscription {Subscription} items created.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsDeleted))
            {
                _logger.LogDebug("Subscription {Subscription} items deleted.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.ItemsModified))
            {
                _logger.LogDebug("Subscription {Subscription} items modified.", this);
            }
            if (e.Status.HasFlag(SubscriptionChangeMask.Transferred))
            {
                _logger.LogDebug("Subscription {Subscription} transferred.", this);
            }
        }

        /// <summary>
        /// Helper to validate subscription template
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="subscriptionName"></param>
        /// <exception cref="ArgumentException"></exception>
        private static SubscriptionModel ValidateSubscriptionInfo(SubscriptionModel subscription,
            string? subscriptionName = null)
        {
            ArgumentNullException.ThrowIfNull(subscription);
            if (subscription.Configuration == null)
            {
                throw new ArgumentException("Missing configuration", nameof(subscription));
            }
            if (subscription.Id?.Connection == null)
            {
                throw new ArgumentException("Missing connection information", nameof(subscription));
            }
            return subscription with
            {
                Configuration = subscription.Configuration with
                {
                    MetaData = subscription.Configuration.MetaData.Clone()
                },
                Id = new SubscriptionIdentifier(subscription.Id.Connection,
                    subscriptionName ?? subscription.Id.Id),
                MonitoredItems = subscription.MonitoredItems?.ToList()
            };
        }

        /// <summary>
        /// Subscription notification container
        /// </summary>
        internal sealed record class Notification : IOpcUaSubscriptionNotification
        {
            /// <inheritdoc/>
            public object? Context { get; set; }

            /// <inheritdoc/>
            public PublishedDataSetMetaDataModel? MetaData { get; private set; }

            /// <inheritdoc/>
            public uint SequenceNumber { get; internal set; }

            /// <inheritdoc/>
            public MessageType MessageType { get; internal set; }

            /// <inheritdoc/>
            public string? SubscriptionName { get; internal set; }

            /// <inheritdoc/>
            public string? DataSetName { get; internal set; }

            /// <inheritdoc/>
            public ushort SubscriptionId { get; internal set; }

            /// <inheritdoc/>
            public string? EndpointUrl { get; internal set; }

            /// <inheritdoc/>
            public string? ApplicationUri { get; internal set; }

            /// <inheritdoc/>
            public DateTimeOffset? PublishTimestamp { get; internal set; }

            /// <inheritdoc/>
            public uint? PublishSequenceNumber { get; private set; }

            /// <inheritdoc/>
            public IServiceMessageContext ServiceMessageContext { get; private set; }

            /// <inheritdoc/>
            public IList<MonitoredItemNotificationModel> Notifications { get; private set; }

            /// <inheritdoc/>
            public DateTimeOffset CreatedTimestamp { get; }

            /// <summary>
            /// Create acknoledgeable notification
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="subscriptionId"></param>
            /// <param name="messageContext"></param>
            /// <param name="notifications"></param>
            /// <param name="sequenceNumber"></param>
            public Notification(OpcUaSubscription outer,
                uint subscriptionId, IServiceMessageContext messageContext,
                IEnumerable<MonitoredItemNotificationModel>? notifications = null,
                uint? sequenceNumber = null)
            {
                _outer = outer;
                PublishSequenceNumber = sequenceNumber;
                CreatedTimestamp = outer._timeProvider.GetUtcNow();
                ServiceMessageContext = messageContext;
                _subscriptionId = subscriptionId;

                MetaData = _outer.CurrentMetaData;
                Notifications = notifications?.ToList() ??
                    new List<MonitoredItemNotificationModel>();
            }

            /// <inheritdoc/>
            public IEnumerable<IOpcUaSubscriptionNotification> Split(
                Func<MonitoredItemNotificationModel, object?> selector)
            {
                if (Notifications.Count > 1)
                {
                    var original = PublishSequenceNumber;
                    PublishSequenceNumber = null;

                    var splitted = Notifications
                        .GroupBy(selector)
                        .Select(g => this with
                        {
                            Context = g.Key,
                            Notifications = g.ToList()
                        })
                        .ToList();

                    splitted[^1].PublishSequenceNumber = original;
#if DEBUG
                    MarkProcessed();
#endif
                    return splitted;
                }
                return this.YieldReturn();
            }

            /// <inheritdoc/>
            public bool TryUpgradeToKeyFrame()
            {
                if (!_outer.TryGetNotifications(SequenceNumber, out var allNotifications))
                {
                    return false;
                }
                MetaData = _outer.CurrentMetaData;
                MessageType = MessageType.KeyFrame;
                Notifications.Clear();
                Notifications.AddRange(allNotifications);
                return true;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _outer.AdvancePosition(_subscriptionId, PublishSequenceNumber);
            }
#if DEBUG
            /// <inheritdoc/>
            public void MarkProcessed()
            {
                _processed = true;
            }

            /// <inheritdoc/>
            public void DebugAssertProcessed()
            {
                Debug.Assert(_processed);
            }
            private bool _processed;
#endif

            /// <summary>
            /// Get diagnostics info from message
            /// </summary>
            /// <param name="modelChanges"></param>
            /// <param name="heartbeats"></param>
            /// <param name="overflow"></param>
            /// <returns></returns>
            internal int GetDiagnosticCounters(out int modelChanges, out int heartbeats,
                out int overflow)
            {
                modelChanges = 0;
                heartbeats = 0;
                overflow = 0;
                foreach (var n in Notifications)
                {
                    if (n.Flags.HasFlag(MonitoredItemSourceFlags.ModelChanges))
                    {
                        modelChanges++;
                    }
                    else if (n.Flags.HasFlag(MonitoredItemSourceFlags.Heartbeat))
                    {
                        heartbeats++;
                    }
                    overflow += n.Overflow;
                }
                return Notifications.Count;
            }

            private readonly OpcUaSubscription _outer;
            private readonly uint _subscriptionId;
        }

        /// <summary>
        /// Loader abstraction
        /// </summary>
        private sealed class MetaDataLoader : IAsyncDisposable
        {
            /// <summary>
            /// Current meta data
            /// </summary>
            public PublishedDataSetMetaDataModel? MetaData { get; private set; }

            /// <summary>
            /// Create loader
            /// </summary>
            /// <param name="subscription"></param>
            public MetaDataLoader(OpcUaSubscription subscription)
            {
                _subscription = subscription;
                _loader = StartAsync(_cts.Token);
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    await _cts.CancelAsync().ConfigureAwait(false);
                    await _loader.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _cts.Dispose();
                }
            }

            /// <summary>
            /// Load meta data
            /// </summary>
            /// <param name="arguments"></param>
            public void Reload(MetaDataLoaderArguments arguments)
            {
                Interlocked.Exchange(ref _arguments, arguments)?.tcs?.TrySetCanceled();
                _trigger.Set();
            }

            /// <summary>
            /// Meta data loader task
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task StartAsync(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    await _trigger.WaitAsync(ct).ConfigureAwait(false);

                    var args = Interlocked.Exchange(ref _arguments, null);
                    if (args == null)
                    {
                        continue;
                    }
                    try
                    {
                        await UpdateMetaDataAsync(args, ct).ConfigureAwait(false);
                        args.tcs?.TrySetResult();
                        Interlocked.Increment(ref _subscription._metadataLoadSuccess);
                    }
                    catch (OperationCanceledException)
                    {
                        args.tcs?.TrySetCanceled(ct);
                    }
                    catch (Exception ex)
                    {
                        _subscription._logger.LogError(
                            "Failed to get metadata for {Subscription} with error {Error}",
                            this, ex.Message);

                        args.tcs?.TrySetException(ex);
                        Interlocked.Increment(ref _subscription._metadataLoadFailures);
                    }
                }
            }

            /// <summary>
            /// Update metadata
            /// </summary>
            /// <param name="args"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            internal async Task UpdateMetaDataAsync(MetaDataLoaderArguments args,
                CancellationToken ct = default)
            {
                if (_subscription._template.Configuration?.MetaData == null)
                {
                    // Metadata disabled
                    MetaData = null;
                    return;
                }

                //
                // Use the date time to version across reboots. This could be done
                // more elegantly by saving the last version to persistent storage
                // such as twin, but this is ok for the sake of being able to have
                // an incremental version number defining metadata changes.
                //
                var minor = (uint)args.timeprovider.GetUtcNow().UtcDateTime.ToBinary();

                _subscription._logger.LogDebug(
                    "Loading Metadata {Major}.{Minor} for {Subscription}...",
                    _subscription._template.Configuration.MetaData.MajorVersion ?? 1,
                    minor, this);

                var sw = Stopwatch.StartNew();
                var typeSystem = await args.sessionHandle.GetComplexTypeSystemAsync(
                    ct).ConfigureAwait(false);
                var dataTypes = new NodeIdDictionary<object>();
                var fields = new List<PublishedFieldMetaDataModel>();
                foreach (var monitoredItem in args.monitoredItemsInDataSet)
                {
                    await monitoredItem.GetMetaDataAsync(args.sessionHandle, typeSystem,
                        fields, dataTypes, ct).ConfigureAwait(false);
                }

                _subscription._logger.LogInformation(
                    "Loading Metadata {Major}.{Minor} for {Subscription} took {Duration}.",
                    _subscription._template.Configuration.MetaData.MajorVersion ?? 1,
                    minor, this, sw.Elapsed);

                MetaData = new PublishedDataSetMetaDataModel
                {
                    DataSetMetaData =
                        _subscription._template.Configuration.MetaData.Clone(),
                    EnumDataTypes =
                        dataTypes.Values.OfType<EnumDescriptionModel>().ToList(),
                    StructureDataTypes =
                        dataTypes.Values.OfType<StructureDescriptionModel>().ToList(),
                    SimpleDataTypes =
                        dataTypes.Values.OfType<SimpleTypeDescriptionModel>().ToList(),
                    Fields =
                        fields,
                    MinorVersion =
                        minor
                };
            }

            internal record MetaDataLoaderArguments(TaskCompletionSource? tcs,
                IOpcUaSession sessionHandle, NamespaceTable namespaces, TimeProvider timeprovider,
                IEnumerable<OpcUaMonitoredItem> monitoredItemsInDataSet);
            private MetaDataLoaderArguments? _arguments;
            private readonly Task _loader;
            private readonly CancellationTokenSource _cts = new();
            private readonly AsyncAutoResetEvent _trigger = new();
            private readonly OpcUaSubscription _subscription;
        }

        private long TotalMonitoredItems => _additionallyMonitored.Count + MonitoredItemCount;

        /// <summary>
        /// Create observable metrics
        /// </summary>
        public void InitializeMetrics()
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_missing_keep_alives",
                () => new Measurement<long>(_missingKeepAlives, _metrics.TagList),
                description: "Number of missing keep alives in subscription.");
            _meter.CreateObservableCounter("iiot_edge_publisher_unassigned_notification_count",
                () => new Measurement<long>(_unassignedNotifications, _metrics.TagList),
                description: "Number of notifications that could not be assigned.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_monitored_items",
                () => new Measurement<long>(TotalMonitoredItems, _metrics.TagList),
                description: "Total monitored item count.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_good_nodes",
                () => new Measurement<long>(_goodMonitoredItems, _metrics.TagList),
                description: "Monitored items successfully created.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_bad_nodes",
                () => new Measurement<long>(_badMonitoredItems, _metrics.TagList),
                description: "Monitored items that were not successfully created.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_late_nodes",
                () => new Measurement<long>(_lateMonitoredItems, _metrics.TagList),
                description: "Monitored items that are late reporting.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_reporting_nodes",
                () => new Measurement<long>(_reportingItems, _metrics.TagList),
                description: "Monitored items reporting.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_sampling_nodes",
                () => new Measurement<long>(_samplingItems, _metrics.TagList),
                description: "Monitored items with sampling enabled.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_disabled_nodes",
                () => new Measurement<long>(_disabledItems, _metrics.TagList),
                description: "Monitored items with monitoring mode disabled.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_nodes_monitoring_mode_inconsistent",
                () => new Measurement<long>(_notAppliedItems, _metrics.TagList),
                description: "Monitored items with monitoring mode not applied.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_subscription_stopped_count",
                () => new Measurement<int>(_publishingStopped ? 1 : 0, _metrics.TagList),
                description: "Number of subscriptions that stopped publishing.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_good_metadata",
                () => new Measurement<long>(_metadataLoadSuccess, _metrics.TagList),
                description: "Number of successful metadata load operations.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_bad_metadata",
                () => new Measurement<long>(_metadataLoadFailures, _metrics.TagList),
                description: "Number of failed metadata load operations.");

            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.OutstandingRequestCount, State.SubscriptionCount),
                _metrics.TagList), description: "Good publish requests per subsciption.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_good_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.GoodPublishRequestCount, State.SubscriptionCount),
                _metrics.TagList), description: "Good publish requests per subsciption.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_bad_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.BadPublishRequestCount, State.SubscriptionCount),
                _metrics.TagList), description: "Bad publish requests per subsciption.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_min_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.MinPublishRequestCount, State.SubscriptionCount),
                _metrics.TagList), description: "Min publish requests queued per subsciption.");

            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_deferred_acks_enabled_count",
                () => new Measurement<int>(_useDeferredAcknoledge ? 1 : 0, _metrics.TagList),
                description: "Number of subscriptions with deferred acknoledgements enabled.");
            _meter.CreateObservableCounter("iiot_edge_publisher_deferred_acks_last_sequencenumber",
                () => new Measurement<long>(_sequenceNumber, _metrics.TagList),
                description: "Sequence number of the last notification received in subscription.");
            _meter.CreateObservableCounter("iiot_edge_publisher_deferred_acks_completed_sequencenumber",
                () => new Measurement<long>(_currentSequenceNumber, _metrics.TagList),
                description: "Sequence number of the next notification to acknoledge in subscription.");

            static double Ratio(int value, int count) => count == 0 ? 0.0 : (double)value / count;
        }

        private static readonly TimeSpan kDefaultErrorRetryDelay = TimeSpan.FromMinutes(1);
        private FrozenDictionary<uint, OpcUaMonitoredItem> _additionallyMonitored;
        private SubscriptionModel _template;
        private IOpcUaClient? _client;
        private uint _previousSequenceNumber;
        private uint _sequenceNumber;
        private uint _currentSequenceNumber;
        private bool _useDeferredAcknoledge;
        private bool _firstDataChangeReceived;
        private bool _closed;
        private bool _forceRecreate;
        private readonly ISubscriptionCallbacks _callbacks;
        private readonly Lazy<MetaDataLoader> _metaDataLoader;
        private readonly IClientAccessor<ConnectionModel> _clients;
        private readonly IOptions<OpcUaClientOptions> _options;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IMetricsContext _metrics;
        private readonly ITimer _timer;
        private readonly ITimer _keepAliveWatcher;
        private readonly ITimer _monitoredItemWatcher;
        private readonly TimeProvider _timeProvider;
        private DateTimeOffset? _lastMonitoredItemCheck;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private static uint _lastIndex;
        private int _metadataLoadSuccess;
        private int _metadataLoadFailures;
        private int _goodMonitoredItems;
        private int _reportingItems;
        private int _disabledItems;
        private int _samplingItems;
        private int _notAppliedItems;
        private int _badMonitoredItems;
        private int _missingKeepAlives;
        private int _continuouslyMissingKeepAlives;
        private long _unassignedNotifications;
        private bool _publishingStopped;
        private bool _disposed;
        private int _lateMonitoredItems;
        private readonly object _lock = new();
        private readonly object _timers = new();
    }
}
