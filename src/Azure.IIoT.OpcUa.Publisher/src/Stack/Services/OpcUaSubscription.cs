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
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Concrete subscription
    /// </summary>
    [DataContract(Namespace = OpcUaClient.Namespace)]
    [KnownType(typeof(OpcUaMonitoredItem))]
    [KnownType(typeof(OpcUaMonitoredItem.DataChange))]
    [KnownType(typeof(OpcUaMonitoredItem.CyclicRead))]
    [KnownType(typeof(OpcUaMonitoredItem.Heartbeat))]
    [KnownType(typeof(OpcUaMonitoredItem.ModelChangeEventItem))]
    [KnownType(typeof(OpcUaMonitoredItem.Event))]
    [KnownType(typeof(OpcUaMonitoredItem.Condition))]
    internal sealed class OpcUaSubscription : Subscription
    {
        /// <summary>
        /// The minimum lifetime for subscriptions
        /// </summary>
        public TimeSpan MinLifetimeInterval
        {
            get => Options.MinLifetimeInterval;
            set
            {
                if (Options.MinLifetimeInterval != value)
                {
                    OnOptionsChanged(Options with { MinLifetimeInterval = value });
                }
            }
        }

        /// <inheritdoc/>
        public TimeSpan PublishingInterval
        {
            get => Options.PublishingInterval;
            set
            {
                if (Options.PublishingInterval != value)
                {
                    OnOptionsChanged(Options with { PublishingInterval = value });
                }
            }
        }

        /// <inheritdoc/>
        public uint KeepAliveCount
        {
            get => Options.KeepAliveCount;
            set
            {
                if (Options.KeepAliveCount != value)
                {
                    OnOptionsChanged(Options with { KeepAliveCount = value });
                }
            }
        }

        /// <inheritdoc/>
        public uint LifetimeCount
        {
            get => Options.LifetimeCount;
            set
            {
                if (Options.LifetimeCount != value)
                {
                    OnOptionsChanged(Options with { LifetimeCount = value });
                }
            }
        }

        /// <inheritdoc/>
        public uint MaxNotificationsPerPublish
        {
            get => Options.MaxNotificationsPerPublish;
            set
            {
                if (Options.MaxNotificationsPerPublish != value)
                {
                    OnOptionsChanged(Options with { MaxNotificationsPerPublish = value });
                }
            }
        }

        /// <inheritdoc/>
        public bool PublishingEnabled
        {
            get => Options.PublishingEnabled;
            set
            {
                if (Options.PublishingEnabled != value)
                {
                    OnOptionsChanged(Options with { PublishingEnabled = value });
                }
            }
        }

        /// <inheritdoc/>
        public byte Priority
        {
            get => Options.Priority;
            set
            {
                if (Options.Priority != value)
                {
                    OnOptionsChanged(Options with { Priority = value });
                }
            }
        }

        /// <summary>
        /// Whether the subscription is online
        /// </summary>
        internal bool IsOnline
            => Session?.Connected == true && !IsClosed;

        /// <summary>
        /// Whether subscription is closed
        /// </summary>
        internal bool IsClosed
            => _disposed || Session == null;

        /// <summary>
        /// Currently monitored but unordered
        /// </summary>
        internal IEnumerable<OpcUaMonitoredItem> CurrentlyMonitored
            => MonitoredItems.OfType<OpcUaMonitoredItem>();

        /// <summary>
        /// Owning session
        /// </summary>
        internal OpcUaClient.OpcUaSession Session { get; }

        /// <summary>
        /// Subscription
        /// </summary>
        /// <param name="client"></param>
        /// <param name="session"></param>
        /// <param name="owner"></param>
        /// <param name="options"></param>
        /// <param name="observability"></param>
        /// <param name="metrics"></param>
        internal OpcUaSubscription(OpcUaClient client, OpcUaClient.OpcUaSession session,
            OpcUaClient.VirtualSubscription owner, IOptions<OpcUaSubscriptionOptions> options,
            IObservability observability, IMetricsContext metrics)
            : base(session, (IMessageAckQueue)session.Subscriptions,
                  new Opc.Ua.Client.OptionsMonitor<SubscriptionOptions>( // TODO
                      new OpcUaClient.VRef(owner)), observability)
        {
            _client = client;
            Session = session;
            _options = options;

            _metrics = metrics;
            _owner = owner;
            _meter = Observability.MeterFactory.Create(nameof(OpcUaSubscription));
            _logger = observability.LoggerFactory.CreateLogger<OpcUaSubscription>();

            _keepAliveWatcher = Observability.TimeProvider.CreateTimer(
                OnKeepAliveMissing, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _monitoredItemWatcher = Observability.TimeProvider.CreateTimer(
                OnMonitoredItemWatchdog, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            InitializeMetrics();
            ResetMonitoredItemWatchdogTimer(CurrentPublishingEnabled);
        }

        /// <inheritdoc/>
        protected override ValueTask DisposeAsync(bool disposing)
        {
            if (disposing && !_disposed)
            {
                try
                {
                    ResetMonitoredItemWatchdogTimer(false);
                    _keepAliveWatcher.Change(
                        Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    var items = CurrentlyMonitored.ToList();
                    if (items.Count != 0)
                    {
                        //
                        // When the entire session is disposed and recreated we must
                        // still dispose all monitored items that are remaining
                        //
                        items.ForEach(item => item.Dispose());
                        Debug.Assert(!CurrentlyMonitored.Any());

                        _logger.LogInformation(
                            "Disposed Subscription {Subscription} with {Count)} items.",
                            this, items.Count);
                    }
                    else
                    {
                        _logger.LogInformation("Disposed Subscription {Subscription}.",
                            this);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Disposing Subscription {Subscription} encountered error.", this);

                    // Eat the error
                }
                finally
                {
                    _disposed = true;
                    _keepAliveWatcher.Dispose();
                    _monitoredItemWatcher.Dispose();
                    _meter.Dispose();
                }
            }
            return base.DisposeAsync(disposing);
        }

        /// <inheritdoc/>
        protected override MonitoredItem CreateMonitoredItem(IObservability observability,
            IOptionsMonitor<MonitoredItemOptions> options)
        {
            if (options.CurrentValue is not Precreated pre || pre.Item is null)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadUnexpectedError, "Bad");
            }
            return pre.Item;
        }

        /// <summary>
        /// Notify session disconnected/reconnecting. This is called
        /// on all subscriptions in the session and takes child subscriptions
        /// into account
        /// </summary>
        /// <param name="disconnected"></param>
        /// <returns></returns>
        internal void NotifySessionConnectionState(bool disconnected)
        {
            foreach (var item in CurrentlyMonitored)
            {
                item.NotifySessionConnectionState(disconnected);
            }
        }

        /// <summary>
        /// Create a keep alive message
        /// </summary>
        /// <returns></returns>
        internal OpcUaSubscriptionNotification? CreateKeepAlive()
        {
            if (IsClosed)
            {
                _logger.LogError("Subscription {Subscription} closed!", this);
                return null;
            }
            try
            {
                var session = Session;
                if (session == null)
                {
                    return null;
                }
                return new OpcUaSubscriptionNotification(this, session.MessageContext,
                    Array.Empty<MonitoredItemNotificationModel>(), Observability.TimeProvider)
                {
                    ApplicationUri = session.Endpoint.Server.ApplicationUri,
                    EndpointUrl = session.Endpoint.EndpointUrl,
                    SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType = MessageType.KeepAlive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create keep alive for subscription {Subscription}.", this);
                return null;
            }
        }

        /// <summary>
        /// Applies all changes inside the monitored items if there are any
        /// The monitored items are updated and the update signals the subscription
        /// state management task to apply the changes.
        /// </summary>
        /// <param name="ct"></param>
        public ValueTask ApplyMonitoredItemChangesAsync(CancellationToken ct)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Finalize sync of all subscriptions in the chain if needed
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task FinalizeSyncAsync(CancellationToken ct)
        {
            await ApplyMonitoredItemChangesAsync(ct).ConfigureAwait(false);

            var shouldEnable = MonitoredItems
                .OfType<OpcUaMonitoredItem>()
                .Any(m => m.MonitoringMode != Opc.Ua.MonitoringMode.Disabled);
            if (PublishingEnabled ^ shouldEnable)
            {
                PublishingEnabled = shouldEnable;
                await ApplyChangesAsync(ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "{State} Subscription {Subscription} in session {Session}.",
                    shouldEnable ? "Enabled" : "Disabled", this, Session);

                ResetMonitoredItemWatchdogTimer(shouldEnable);
            }
        }

        /// <summary>
        /// Get a subscription with the supplied configuration (no lock)
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        internal async ValueTask SynchronizeSubscriptionAsync(CancellationToken ct)
        {
            if (!Created)
            {
                PublishingEnabled = _owner.EnableImmediatePublishing;
                KeepAliveCount = _owner.DesiredKeepAliveCount;
                PublishingInterval = _owner.DesiredPublishingInterval;
                MaxNotificationsPerPublish = _owner.DesiredMaxNotificationsPerPublish;
                LifetimeCount = _owner.DesiredLifetimeCount;
                Priority = _owner.DesiredPriority;

                _logger.LogInformation(
                    "Creating new {State} subscription {Subscription} in session {Session}.",
                    PublishingEnabled ? "enabled" : "disabled", this, Session);

                await ApplyChangesAsync(ct).ConfigureAwait(false);
                if (!Created)
                {
                    throw new ServiceResultException(StatusCodes.BadSubscriptionIdInvalid,
                        $"Failed to create subscription {this} in session {Session}");
                }

                ResetMonitoredItemWatchdogTimer(PublishingEnabled);
                Debug.Assert(Created);

                _firstDataChangeReceived = false;
            }
            else
            {
                //
                // Only needed when we reconfiguring a subscription with a single subscriber
                // This is not yet implemented.
                // TODO: Consider removing...
                //

                // Apply new configuration on configuration on original subscription
                var modifySubscription = false;

                if (_owner.DesiredKeepAliveCount != KeepAliveCount)
                {
                    _logger.LogInformation(
                        "Change KeepAliveCount to {New} in Subscription {Subscription}...",
                        _owner.DesiredKeepAliveCount, this);

                    KeepAliveCount = _owner.DesiredKeepAliveCount;
                    modifySubscription = true;
                }

                if (PublishingInterval != _owner.DesiredPublishingInterval)
                {
                    _logger.LogInformation(
                        "Change publishing interval to {New} in Subscription {Subscription}...",
                        _owner.DesiredPublishingInterval, this);
                    PublishingInterval = _owner.DesiredPublishingInterval;
                    modifySubscription = true;
                }

                if (MaxNotificationsPerPublish != _owner.DesiredMaxNotificationsPerPublish)
                {
                    _logger.LogInformation(
                        "Change MaxNotificationsPerPublish to {New} in Subscription {Subscription}",
                        _owner.DesiredMaxNotificationsPerPublish, this);
                    MaxNotificationsPerPublish = _owner.DesiredMaxNotificationsPerPublish;
                    modifySubscription = true;
                }

                if (LifetimeCount != _owner.DesiredLifetimeCount)
                {
                    _logger.LogInformation(
                        "Change LifetimeCount to {New} in Subscription {Subscription}...",
                        _owner.DesiredLifetimeCount, this);
                    LifetimeCount = _owner.DesiredLifetimeCount;
                    modifySubscription = true;
                }
                if (Priority != _owner.DesiredPriority)
                {
                    _logger.LogInformation(
                        "Change Priority to {New} in Subscription {Subscription}...",
                        _owner.DesiredPriority, this);
                    Priority = _owner.DesiredPriority;
                    modifySubscription = true;
                }
                if (modifySubscription)
                {
                    await ApplyChangesAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation(
                        "Subscription {Subscription} in session {Session} successfully modified.",
                        this, Session);
                    ResetMonitoredItemWatchdogTimer(CurrentPublishingEnabled);
                }
            }
            ResetKeepAliveTimer();
        }

        private Task ApplyChangesAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Synchronize partition of monitored items into this subscription
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="operationLimits"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        internal async ValueTask<TimeSpan> SynchronizeMonitoredItemsAsync(
            OpcUaClient.Partition partition, Opc.Ua.Client.Limits operationLimits,
            CancellationToken ct)
        {
            if (Session is not OpcUaClient.OpcUaSession session)
            {
                throw ServiceResultException.Create(StatusCodes.BadSessionIdInvalid,
                    "Session not connected.");
            }

            // Get the items assigned to this subscription.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var desired = OpcUaMonitoredItem
                .Create(_client, Session, this, partition.Items, Observability)
                .ToHashSet();
#pragma warning restore CA2000 // Dispose objects before losing scope

            var previouslyMonitored = CurrentlyMonitored.ToHashSet();
            var remove = previouslyMonitored.Except(desired).ToHashSet();
            var add = desired.Except(previouslyMonitored).ToHashSet();
            var same = previouslyMonitored.ToHashSet();
            var errorsDuringSync = 0;
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
                    throw ServiceResultException.Create(results.ErrorInfo.StatusCode,
                        results.ErrorInfo.ErrorMessage ?? "Failed to resolve browse paths");
                }

                foreach (var result in results)
                {
                    var resolvedId = NodeId.Null;
                    if (result.ErrorInfo == null && result.Result.Targets.Count == 1)
                    {
                        resolvedId = result.Result.Targets[0].TargetId.ToNodeId(
                            session.MessageContext.NamespaceUris);
                        result.Request!.Value.Update(resolvedId, session.MessageContext);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to resolve browse path for {NodeId} " +
                            "in {Subscription} due to '{ServiceResult}'",
                            result.Request!.Value.NodeId, this, result.ErrorInfo);
                        errorsDuringSync++;
                    }
                }
            }

            //
            // If retrieving paths for all the items from the root folder was configured do so
            // now. All items that fail here should be retried later.
            //
            if (_owner.ResolveBrowsePathFromRoot)
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

            var metadataChanged = new HashSet<ISubscriber>();
            var applyChanges = false;
            var updated = 0;

            foreach (var toUpdate in same)
            {
                if (!desired.TryGetValue(toUpdate, out var theDesiredUpdate))
                {
                    errorsDuringSync++;
                    continue;
                }
                desired.Remove(theDesiredUpdate);
                Debug.Assert(toUpdate.GetType() == theDesiredUpdate.GetType());
                try
                {
                    if (toUpdate.MergeWith(theDesiredUpdate, out var metadata))
                    {
                        _logger.LogDebug(
                            "Trying to update monitored item '{Item}' in {Subscription}...",
                            toUpdate, this);
                        if (toUpdate.FinalizeMergeWith != null && metadata)
                        {
                            await toUpdate.FinalizeMergeWith(ct).ConfigureAwait(false);
                        }
                        updated++;
                        applyChanges = true;
                    }
                    if (metadata)
                    {
                        _logger.LogDebug("Updated monitored item '{Item}' in {Subscription} " +
                            "so that metadata changed ...", toUpdate, this);
                        metadataChanged.Add(toUpdate.Owner);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to update monitored item '{Item}' in {Subscription}...",
                        toUpdate, this);
                    errorsDuringSync++;
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
                    toRemove.Dispose();
                    _logger.LogDebug(
                        "Trying to remove monitored item '{Item}' from {Subscription}...",
                        toRemove, this);
                    removed++;
                    applyChanges = true;
                    metadataChanged.Add(toRemove.Owner);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to remove monitored item '{Item}' from {Subscription}...",
                        toRemove, this);
                    errorsDuringSync++;
                }
            }

            var added = 0;
            foreach (var toAdd in add)
            {
                desired.Remove(toAdd);
                try
                {
                    if (toAdd.Initialize())
                    {
                        _logger.LogDebug(
                            "Adding monitored item '{Item}' to {Subscription}...",
                            toAdd, this);

                        if (toAdd.FinalizeInitialize != null)
                        {
                            await toAdd.FinalizeInitialize(ct).ConfigureAwait(false);
                        }
                        added++;
                        metadataChanged.Add(toAdd.Owner);
                        applyChanges = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to add monitored item '{Item}' to {Subscription}...",
                        toAdd, this);
                    errorsDuringSync++;
                }
            }

            Debug.Assert(desired.Count == 0, "We should have processed all desired updates.");

            if (applyChanges)
            {
                await ApplyMonitoredItemChangesAsync(ct).ConfigureAwait(false);
                if (MonitoredItemCount == 0 && !_owner.EnableImmediatePublishing)
                {
                    PublishingEnabled = false;

                    await ApplyChangesAsync(ct).ConfigureAwait(false);

                    _logger.LogInformation(
                        "Disabled empty Subscription {Subscription} in session {Session}.",
                        this, session);

                    ResetMonitoredItemWatchdogTimer(false);
                }
            }

            // Perform second pass over all monitored items and complete.
            applyChanges = false;
            var badMonitoredItems = 0;

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
                .Select(a => (a.Owner, a.GetDisplayName))
                .Where(a => a.GetDisplayName.HasValue)
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
                            NodeId = a.GetDisplayName!.Value.NodeId.ToNodeId(session.MessageContext),
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
                        errorsDuringSync++;
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
                                _logger.LogDebug(
                                    "Read display name for node '{NodeId}' in {Subscription}...",
                                    result.Request.GetDisplayName!.Value.NodeId, this);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to read display name for {NodeId} " +
                                    "in {Subscription} due to '{ServiceResult}'",
                                    result.Request.GetDisplayName!.Value.NodeId, this,
                                    result.ErrorInfo);
                            }
                            if (result.Request.GetDisplayName!.Value.Update(
                                displayName ?? string.Empty))
                            {
                                metadataChanged.Add(result.Request.Owner);
                            }
                        }
                    }
                }
            }

            _logger.LogDebug(
                "Completing {Count} same/added and {Removed} removed items in subscription {Subscription}...",
                desiredMonitoredItems.Count, remove.Count, this);
            foreach (var monitoredItem in desiredMonitoredItems.Concat(remove))
            {
                if (!monitoredItem.TryCompleteChanges(ref applyChanges))
                {
                    // Apply more changes in future passes
                    badMonitoredItems++;
                }
            }

            Debug.Assert(remove.All(m => m.Disposed), "All removed items should be invalid now");
            var set = desiredMonitoredItems.Where(m => !m.Disposed).ToList();
            _logger.LogDebug(
                "Completed {Count} valid and {Invalid} invalid items in subscription {Subscription}...",
                set.Count, desiredMonitoredItems.Count - set.Count, this);

            if (applyChanges)
            {
                // Apply any additional changes
                await ApplyMonitoredItemChangesAsync(ct).ConfigureAwait(false);
            }

            Debug.Assert(set.Select(m => m.ClientHandle).Distinct().Count() == set.Count,
                "Client handles are not distinct or one of the items is null");

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

                    await SetMonitoringModeAsync(change.Key.Value,
                        itemsToChange, ct).ConfigureAwait(false);
                }
            }

            // Cleanup all items that are not in the currently monitoring list
            var dispose = previouslyMonitored
                .Except(set)
                .ToList();
            dispose.ForEach(m => m.Dispose());

            // Notify semantic change now that we have update the monitored items
            foreach (var owner in metadataChanged)
            {
                _logger.LogDebug("Signalling metadata change for {Subscription}.", this);
                await owner.OnMonitoredItemSemanticsChangedAsync().ConfigureAwait(false);
            }

            // Refresh condition
            if (set.OfType<OpcUaMonitoredItem.Condition>().Any())
            {
                _logger.LogDebug(
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
                    errorsDuringSync++;
                }
            }

            var goodMonitoredItems =
                Math.Max(set.Count - badMonitoredItems, 0);
            var reportingItems = set
                .Count(r => r.CurrentMonitoringMode == Opc.Ua.MonitoringMode.Reporting);
            var disabledItems = set
                .Count(r => r.CurrentMonitoringMode == Opc.Ua.MonitoringMode.Disabled);
            var samplingItems = set
                .Count(r => r.CurrentMonitoringMode == Opc.Ua.MonitoringMode.Sampling);
            var notAppliedItems = set
                .Count(r => r.CurrentMonitoringMode != r.MonitoringMode);
            var heartbeatItems = set
                .Count(r => r is OpcUaMonitoredItem.Heartbeat);
            var conditionItems = set
                .Count(r => r is OpcUaMonitoredItem.Condition);
            var heartbeatsEnabled = set
                .Count(r => r is OpcUaMonitoredItem.Heartbeat h && h.TimerEnabled);
            var conditionsEnabled = set
                .Count(r => r is OpcUaMonitoredItem.Condition h && h.TimerEnabled);

            ReportMonitoredItemChanges(set.Count, goodMonitoredItems, badMonitoredItems,
                errorsDuringSync, notAppliedItems, reportingItems, disabledItems, heartbeatItems,
                heartbeatsEnabled, conditionItems, conditionsEnabled, samplingItems,
                dispose.Count);

            // Set up subscription management trigger
            if (badMonitoredItems != 0 || errorsDuringSync != 0)
            {
                // There were items that could not be added to subscription
                return Delay(_options.Value.InvalidMonitoredItemRetryDelayDuration,
                    TimeSpan.FromMinutes(5));
            }
            else if (desiredMonitoredItems.Count != set.Count)
            {
                // There were items !Valid but desired.
                return Delay(_options.Value.BadMonitoredItemRetryDelayDuration,
                    TimeSpan.FromMinutes(30));
            }
            else
            {
                // Nothing to do
                return Delay(_options.Value.SubscriptionManagementIntervalDuration,
                    Timeout.InfiniteTimeSpan);
            }
        }

        private Task SetMonitoringModeAsync(Opc.Ua.MonitoringMode value,
            List<MonitoredItem> itemsToChange, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get notifications
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="notifications"></param>
        /// <returns></returns>
        internal bool TryGetNotifications(ISubscriber owner,
            [NotNullWhen(true)] out IList<MonitoredItemNotificationModel>? notifications)
        {
            try
            {
                if (IsClosed)
                {
                    notifications = null;
                    return false;
                }

                var collector = new OpcUaMonitoredItem.MonitoredItemNotifications();
                // Ensure we order by client handle exactly like the meta data is ordered
                foreach (var item in CurrentlyMonitored
                    .Where(m => m.Owner == owner).OrderBy(m => m.ClientHandle))
                {
                    item.TryGetLastMonitoredItemNotifications(collector);
                }

                if (!collector.Notifications.TryGetValue(owner, out var actualNotifications))
                {
                    notifications = null;
                    return false;
                }
                notifications = actualNotifications;
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

        /// <summary>
        /// Send notification
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="messageType"></param>
        /// <param name="notifications"></param>
        /// <param name="eventTypeName"></param>
        /// <param name="diagnosticsOnly"></param>
        /// <param name="timestamp"></param>
        internal void SendNotification(ISubscriber callback, MessageType messageType,
            IList<MonitoredItemNotificationModel> notifications,
            string? eventTypeName, bool diagnosticsOnly, DateTimeOffset? timestamp)
        {
            var curSession = Session;
            var messageContext = curSession?.MessageContext;

            if (messageContext == null)
            {
                _logger.LogWarning("A session was passed to send notification with but without " +
                    "message context. Using thread context.");
                messageContext = ServiceMessageContext.ThreadContext;
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var message = new OpcUaSubscriptionNotification(this, messageContext, notifications,
                Observability.TimeProvider, createdTimestamp: timestamp)
            {
                ApplicationUri = curSession?.Endpoint?.Server?.ApplicationUri,
                EndpointUrl = curSession?.Endpoint?.EndpointUrl,
                EventTypeName = eventTypeName,
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
                    callback.OnSubscriptionEventReceivedAsync(message);
                }
                if (count > 0)
                {
                    callback.OnSubscriptionEventDiagnosticsChangeAsync(false,
                        count, overflows, modelChanges == 0 ? 0 : 1);
                }
            }
            else
            {
                if (!diagnosticsOnly)
                {
                    callback.OnSubscriptionDataChangeReceivedAsync(message);
                }
                if (count > 0)
                {
                    callback.OnSubscriptionDataDiagnosticsChangeAsync(false,
                        count, overflows, heartbeats);
                }
            }
        }

        /// <summary>
        /// Handle cyclic read notifications created by the client
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="values"></param>
        internal async ValueTask OnSubscriptionCylicReadNotificationAsync(uint sequenceNumber,
            DateTime publishTime, List<SampledDataValueModel> values)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
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
                var collector = new OpcUaMonitoredItem.MonitoredItemNotifications();
                foreach (var cyclicDataChange in values.OrderBy(m => m.Value?.SourceTimestamp))
                {
                    if (TryGetMonitoredItemForNotification(cyclicDataChange.ClientHandle, out var monitoredItem) &&
                        !monitoredItem.TryGetMonitoredItemNotifications(publishTime, cyclicDataChange, collector))
                    {
                        _logger.LogDebug(
                            "Skipping the cyclic read data change received for subscription {Subscription}", this);
                    }
                }

                foreach (var (callback, notifications) in collector.Notifications)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var message = new OpcUaSubscriptionNotification(this, session.MessageContext, notifications,
                        Observability.TimeProvider, null, sequenceNumber)
                    {
                        ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = session.Endpoint?.EndpointUrl,
                        PublishTimestamp = publishTime,
                        SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                        MessageType = MessageType.DeltaFrame
                    };
#pragma warning restore CA2000 // Dispose objects before losing scope

                    await callback.OnSubscriptionCyclicReadCompletedAsync(message).ConfigureAwait(false);
                    Debug.Assert(message.Notifications != null);
                    var count = message.GetDiagnosticCounters(out var _, out _, out var overflows);
                    if (count > 0)
                    {
                        await callback.OnSubscriptionCyclicReadDiagnosticsChangeAsync(count,
                            overflows).ConfigureAwait(false);
                    }
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

        /// <inheritdoc/>
        protected override async ValueTask OnEventDataNotificationAsync(uint sequenceNumber,
            DateTime publishTime, EventNotificationList notification,
            PublishState publishStateMask, IReadOnlyList<string> stringTable)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var session = Session;
            if (session is not IOpcUaSession sessionContext)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} received without a session {Session}.",
                    this, session);
                return;
            }

            ResetKeepAliveTimer();
            try
            {
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

                var overflows = 0;
                var events = new List<(string?, OpcUaMonitoredItem.MonitoredItemNotifications)>();
                foreach (var eventFieldList in notification.Events)
                {
                    Debug.Assert(eventFieldList != null);
                    if (TryGetMonitoredItemForNotification(eventFieldList.ClientHandle, out var monitoredItem))
                    {
                        var collector = new OpcUaMonitoredItem.MonitoredItemNotifications();
                        if (!monitoredItem.TryGetMonitoredItemNotifications(publishTime, eventFieldList, collector))
                        {
                            _logger.LogDebug("Skipping the monitored item notification for Event " +
                                "received for subscription {Subscription}", this);
                        }
                        events.Add((monitoredItem.EventTypeName, collector));
                    }
                }

                var total = events.Sum(e => e.Item2.Notifications.Count);
                foreach (var (name, evt) in events)
                {
                    foreach (var (callback, notifications) in evt.Notifications)
                    {
#pragma warning disable CA2000 // Dispose objects before losing scope
                        var message = new OpcUaSubscriptionNotification(this, session.MessageContext,
                            notifications, Observability.TimeProvider, null, sequenceNumber)
                        {
                            ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                            EndpointUrl = session.Endpoint?.EndpointUrl,
                            EventTypeName = name,
                            SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                            MessageType = MessageType.Event,
                            PublishTimestamp = publishTime
                        };
#pragma warning restore CA2000 // Dispose objects before losing scope

                        if (message.Notifications.Count > 0)
                        {
                            await callback.OnSubscriptionEventReceivedAsync(message).ConfigureAwait(false);
                            overflows += message.Notifications.Sum(n => n.Overflow);
                            await callback.OnSubscriptionEventDiagnosticsChangeAsync(true, overflows, 1,
                                0).ConfigureAwait(false);
                        }
                        else
                        {
                            _logger.LogDebug("No notifications added to the message.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
        }

        /// <inheritdoc/>
        protected override async ValueTask OnKeepAliveNotificationAsync(uint sequenceNumber,
            DateTime publishTime, PublishState publishStateMask)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

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

            try
            {
                // in case of a keepalive,the sequence number is not incremented by the servers
                _logger.LogDebug("Keep alive for subscription {Subscription} " +
                    "with sequenceNumber {SequenceNumber}, publishTime {PublishTime}.",
                    this, sequenceNumber, publishTime);

#pragma warning disable CA2000 // Dispose objects before losing scope
                var message = new OpcUaSubscriptionNotification(this, session.MessageContext,
                    Array.Empty<MonitoredItemNotificationModel>(), Observability.TimeProvider)
                {
                    ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = session.Endpoint?.EndpointUrl,
                    PublishTimestamp = publishTime,
                    SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType = MessageType.KeepAlive
                };
#pragma warning restore CA2000 // Dispose objects before losing scope
                foreach (var callback in CurrentlyMonitored
                    .Select(c => c.Owner)
                    .Distinct())
                {
                    await callback.OnSubscriptionKeepAliveAsync(message).ConfigureAwait(false);
                }

                Debug.Assert(message.Notifications != null);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing keep alive notification");
            }
        }

        protected override async ValueTask OnDataChangeNotificationAsync(uint sequenceNumber,
            DateTime publishTime, DataChangeNotification notification,
            PublishState publishStateMask, IReadOnlyList<string> stringTable)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var firstDataChangeReceived = _firstDataChangeReceived;
            _firstDataChangeReceived = true;
            var session = Session;
            if (session is not IOpcUaSession)
            {
                _logger.LogWarning(
                    "DataChange for subscription {Subscription} received without session {Session}.",
                    this, session);
                return;
            }

            ResetKeepAliveTimer();

            try
            {
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

                // Collect notifications
                var collector = new OpcUaMonitoredItem.MonitoredItemNotifications();
                foreach (var item in notification.MonitoredItems)
                {
                    Debug.Assert(item != null);
                    if (TryGetMonitoredItemForNotification(item.ClientHandle, out var monitoredItem) &&
                        !monitoredItem.TryGetMonitoredItemNotifications(publishTime, item, collector))
                    {
                        _logger.LogDebug(
                            "Skipping the monitored item notification for DataChange " +
                            "received for subscription {Subscription}", this);
                    }
                }

                // Send to listeners
                foreach (var (callback, notifications) in collector.Notifications)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var message = new OpcUaSubscriptionNotification(this, session.MessageContext,
                        notifications, Observability.TimeProvider, null, sequenceNumber)
                    {
                        ApplicationUri = session.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = session.Endpoint?.EndpointUrl,
                        PublishTimestamp = publishTime,
                        SequenceNumber = Opc.Ua.SequenceNumber.Increment32(ref _sequenceNumber),
                        MessageType =
                            firstDataChangeReceived ? MessageType.DeltaFrame : MessageType.KeyFrame
                    };
#pragma warning restore CA2000 // Dispose objects before losing scope
                    Debug.Assert(notification.MonitoredItems != null);

                    await callback.OnSubscriptionDataChangeReceivedAsync(message).ConfigureAwait(false);
                    Debug.Assert(message.Notifications != null);
                    var count = message.GetDiagnosticCounters(out var _, out var heartbeats, out var overflows);
                    if (count > 0)
                    {
                        await callback.OnSubscriptionDataDiagnosticsChangeAsync(true, count,
                            overflows, heartbeats).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
        }

        /// <inheritdoc/>
        protected override void OnPublishStateChanged(PublishState stateMask)
        {
            if (stateMask.HasFlag(PublishState.Completed))
            {
                _logger.LogInformation("Subscription {Subscription} CLOSED!", this);
                return;
            }

            ObjectDisposedException.ThrowIf(_disposed, this);
            if (stateMask.HasFlag(PublishState.Stopped) && !_publishingStopped)
            {
                _logger.LogInformation("Subscription {Subscription} STOPPED!", this);
                _keepAliveWatcher.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                ResetMonitoredItemWatchdogTimer(false);
                _publishingStopped = true;
            }
            if (stateMask.HasFlag(PublishState.Recovered) && _publishingStopped)
            {
                _logger.LogInformation("Subscription {Subscription} RECOVERED!", this);
                ResetKeepAliveTimer();
                ResetMonitoredItemWatchdogTimer(true);
                _publishingStopped = false;
            }
            if (stateMask.HasFlag(PublishState.Timeout))
            {
                var action = _owner.WatchdogBehavior ?? SubscriptionWatchdogBehavior.Reset;
                _logger.LogInformation("Subscription {Subscription} TIMEOUT! ---- " +
                    "Server closed subscription - performing recovery action {Action}...",
                    this, action);

                //
                // Timed out on server - this means that the subscription is gone and
                // needs to be recreated. This is the default watchdog behavior.
                //
                RunWatchdogAction(action, $"Subscription {this} timed out!");
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
            if (monitoredItem != null)
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

            var keepAliveTimeout =
                (CurrentPublishingInterval * (CurrentKeepAliveCount + 1)) + TimeSpan.FromSeconds(1);
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
            var timeout = _owner.MonitoredItemWatchdogTimeout;
            if (timeout == TimeSpan.Zero)
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

                _lastMonitoredItemCheck = Observability.TimeProvider.GetUtcNow();
                Debug.Assert(timeout != TimeSpan.Zero);
                _monitoredItemWatcher.Change(timeout, timeout);
            }
        }

        /// <summary>
        /// Checks status of monitored items
        /// </summary>
        /// <param name="state"></param>
        private void OnMonitoredItemWatchdog(object? state)
        {
            var action = _owner.WatchdogBehavior ?? SubscriptionWatchdogBehavior.Diagnostic;
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
                    _lastMonitoredItemCheck = Observability.TimeProvider.GetUtcNow();
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
                _lastMonitoredItemCheck = Observability.TimeProvider.GetUtcNow();
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
                if (itemsChecked != missing && _owner.WatchdogCondition
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
            RunWatchdogAction(action, msg);
        }

        /// <summary>
        /// Run watchdog action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="msg"></param>
        private void RunWatchdogAction(SubscriptionWatchdogBehavior action, string msg)
        {
            switch (action)
            {
                case SubscriptionWatchdogBehavior.Diagnostic:
                    _logger.LogCritical("{Message}", msg);
                    break;
                case SubscriptionWatchdogBehavior.Reset:
                    ResetMonitoredItemWatchdogTimer(false);
                    ForceRecreate = true;
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
                    var action = _owner.WatchdogBehavior ?? SubscriptionWatchdogBehavior.Reset;
                    _logger.LogCritical(
                        "#{Count}/{Lifetimecount}: Keep alive count exceeded. Perform {Action} for {Subscription}...",
                        _continuouslyMissingKeepAlives, CurrentLifetimeCount, action, this);

                    RunWatchdogAction(action, $"Subscription {this}: Keep alives exceeded " +
                        $"({_continuouslyMissingKeepAlives}/{CurrentLifetimeCount}).");
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
        /// Report monitored item changes
        /// </summary>
        /// <param name="count"></param>
        /// <param name="goodMonitoredItems"></param>
        /// <param name="badMonitoredItems"></param>
        /// <param name="errorsDuringSync"></param>
        /// <param name="notAppliedItems"></param>
        /// <param name="reportingItems"></param>
        /// <param name="disabledItems"></param>
        /// <param name="heartbeatItems"></param>
        /// <param name="heartbeatsEnabled"></param>
        /// <param name="conditionItems"></param>
        /// <param name="conditionsEnabled"></param>
        /// <param name="samplingItems"></param>
        /// <param name="disposed"></param>
        private void ReportMonitoredItemChanges(int count,
            int goodMonitoredItems, int badMonitoredItems,
            int errorsDuringSync, int notAppliedItems,
            int reportingItems, int disabledItems,
            int heartbeatItems, int heartbeatsEnabled,
            int conditionItems, int conditionsEnabled,
            int samplingItems, int disposed)
        {
            if (_badMonitoredItems != badMonitoredItems ||
                _errorsDuringSync != errorsDuringSync ||
                _goodMonitoredItems != goodMonitoredItems ||
                _reportingItems != reportingItems ||
                _disabledItems != disabledItems ||
                _samplingItems != samplingItems ||
                _notAppliedItems != notAppliedItems ||
                _heartbeatItems != heartbeatItems ||
                _conditionItems != conditionItems)
            {
                if (samplingItems == 0 && heartbeatItems == 0 && conditionItems == 0 &&
                    notAppliedItems == 0)
                {
                    if (errorsDuringSync == 0 && disabledItems == 0)
                    {
                        _logger.LogInformation(
@"{Subscription} - Removed {Removed} - now monitoring {Count} nodes:
# Good/Bad/Reporting:   {Good}/{Bad}/{Reporting}",
                            this, disposed, count,
                            goodMonitoredItems, badMonitoredItems, reportingItems);
                    }
                    else
                    {
                        _logger.LogWarning(
@"{Subscription} - Removed {Removed} - now monitoring {Count} nodes:
# Good/Bad/Reporting:   {Good}/{Bad}/{Reporting}
# Disabled/Errors:      {Disabled}/{Errors}",
                            this, disposed, count,
                            goodMonitoredItems, badMonitoredItems, reportingItems,
                            disabledItems, errorsDuringSync);
                    }
                }
                else
                {
                    _logger.LogInformation(
@"{Subscription} - Removed {Removed} - now monitoring {Count} nodes:
# Good/Bad/Reporting:   {Good}/{Bad}/{Reporting}
# Disabled/Errors:      {Disabled}/{Errors} (Not applied: {NotApplied})
# Sampling:             {Sampling}
# Heartbeat/ing:        {Heartbeat}/{EnabledHeartbeats}
# Condition/ing:        {Conditions}/{EnabledConditions}",
                            this, disposed, count,
                            goodMonitoredItems, badMonitoredItems, reportingItems,
                            disabledItems, errorsDuringSync, notAppliedItems,
                            samplingItems,
                            heartbeatItems, heartbeatsEnabled,
                            conditionItems, conditionsEnabled);
                }
            }
            else
            {
                _logger.LogDebug(
                    "{ Subscription} Applied changes to monitored items, but nothing changed.",
                    this);
            }

            _badMonitoredItems = badMonitoredItems;
            _errorsDuringSync = errorsDuringSync;
            _goodMonitoredItems = goodMonitoredItems;
            _reportingItems = reportingItems;
            _disabledItems = disabledItems;
            _samplingItems = samplingItems;
            _notAppliedItems = notAppliedItems;
            _heartbeatItems = heartbeatItems;
            _conditionItems = conditionItems;
        }

        /// <summary>
        /// Calculate delay
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="defaultDelay"></param>
        /// <returns></returns>
        private static TimeSpan Delay(TimeSpan? delay, TimeSpan defaultDelay)
        {
            if (delay == null)
            {
                delay = defaultDelay;
            }
            else if (delay == TimeSpan.Zero)
            {
                delay = Timeout.InfiniteTimeSpan;
            }
            return delay.Value;
        }

        /// <summary>
        /// For now use this wrapper to add the item to the subscription
        /// </summary>
        /// <param name="Item"></param>
        internal record class Precreated(MonitoredItem? Item = null) : MonitoredItemOptions;

        private int HeartbeatsEnabled
            => MonitoredItems.Count(r => r is OpcUaMonitoredItem.Heartbeat h && h.TimerEnabled);
        private int ConditionsEnabled
            => MonitoredItems.Count(r => r is OpcUaMonitoredItem.Condition h && h.TimerEnabled);
        private IOpcUaClientDiagnostics State
            => (_client as IOpcUaClientDiagnostics) ?? OpcUaClient.Disconnected;

        public bool ForceRecreate { get; set; }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        public void InitializeMetrics()
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_missing_keep_alives",
                () => new Measurement<long>(_missingKeepAlives, _metrics.TagList),
                description: "Number of missing keep alives in subscription.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_monitored_items",
                () => new Measurement<long>(MonitoredItemCount, _metrics.TagList),
                description: "Total monitored item count.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_disabled_nodes",
                () => new Measurement<long>(_disabledItems, _metrics.TagList),
                description: "Monitored items with monitoring mode disabled.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_nodes_monitoring_mode_inconsistent",
                () => new Measurement<long>(_notAppliedItems, _metrics.TagList),
                description: "Monitored items with monitoring mode not applied.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_reporting_nodes",
                () => new Measurement<long>(_reportingItems, _metrics.TagList),
                description: "Monitored items reporting.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_sampling_nodes",
                () => new Measurement<long>(_samplingItems, _metrics.TagList),
                description: "Monitored items with sampling enabled.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_heartbeat_nodes",
                () => new Measurement<long>(_heartbeatItems, _metrics.TagList),
                description: "Monitored items with heartbeats configured.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_heartbeat_enabled_nodes",
                () => new Measurement<long>(HeartbeatsEnabled, _metrics.TagList),
                description: "Monitored items with heartbeats enabled.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_condition_nodes",
                () => new Measurement<long>(_conditionItems, _metrics.TagList),
                description: "Monitored items with condition monitoring configured.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_condition_enabled_nodes",
                () => new Measurement<long>(ConditionsEnabled, _metrics.TagList),
                description: "Monitored items with condition monitoring enabled.");
            _meter.CreateObservableCounter("iiot_edge_publisher_unassigned_notification_count",
                () => new Measurement<long>(_unassignedNotifications, _metrics.TagList),
                description: "Number of notifications that could not be assigned.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_good_nodes",
                () => new Measurement<long>(_goodMonitoredItems, _metrics.TagList),
                description: "Monitored items successfully created.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_bad_nodes",
                () => new Measurement<long>(_badMonitoredItems, _metrics.TagList),
                description: "Monitored items that were not successfully created.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_late_nodes",
                () => new Measurement<long>(_lateMonitoredItems, _metrics.TagList),
                description: "Monitored items that are late reporting.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_subscription_stopped_count",
                () => new Measurement<int>(_publishingStopped ? 1 : 0, _metrics.TagList),
                description: "Number of subscriptions that stopped publishing.");
            _meter.CreateObservableCounter("iiot_edge_publisher_deferred_acks_last_sequencenumber",
                () => new Measurement<long>(_sequenceNumber, _metrics.TagList),
                description: "Sequence number of the last notification received in subscription.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_publish_requests_per_subscription",
                () => new Measurement<double>(Ratio(State.PublishWorkerCount, State.SubscriptionCount),
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

            static double Ratio(int value, int count) => count == 0 ? 0.0 : (double)value / count;
        }

        private uint _previousSequenceNumber;
        private uint _sequenceNumber;
        private bool _firstDataChangeReceived;
        private readonly OpcUaClient _client;
        private readonly IOptions<OpcUaSubscriptionOptions> _options;
        private readonly ILogger _logger;
        private readonly IMetricsContext _metrics;
        private readonly ITimer _keepAliveWatcher;
        private readonly ITimer _monitoredItemWatcher;
        private readonly OpcUaClient.VirtualSubscription _owner;
        private readonly Meter _meter;
        private DateTimeOffset? _lastMonitoredItemCheck;
        private int _goodMonitoredItems;
        private int _reportingItems;
        private int _disabledItems;
        private int _samplingItems;
        private int _notAppliedItems;
        private int _heartbeatItems;
        private int _conditionItems;
        private int _lateMonitoredItems;
        private int _badMonitoredItems;
        private int _errorsDuringSync;
        private int _missingKeepAlives;
        private int _continuouslyMissingKeepAlives;
        private long _unassignedNotifications;
        private bool _publishingStopped;
        private bool _disposed;
        private readonly Lock _timers = new();
    }
}
