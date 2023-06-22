﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription implementation
    /// </summary>
    internal sealed class OpcUaSubscription : IOpcUaSubscription, ISubscriptionHandle
    {
        /// <inheritdoc/>
        public string? Name => _subscription?.Id.Id;

        /// <inheritdoc/>
        public ushort Id { get; }

        /// <inheritdoc/>
        public ConnectionModel? Connection => _subscription?.Id.Connection;

        /// <inheritdoc/>
        public event EventHandler<IOpcUaSubscriptionNotification>? OnSubscriptionKeepAlive;

        /// <inheritdoc/>
        public event EventHandler<IOpcUaSubscriptionNotification>? OnSubscriptionDataChange;

        /// <inheritdoc/>
        public event EventHandler<IOpcUaSubscriptionNotification>? OnSubscriptionEventChange;

        /// <inheritdoc/>
        public event EventHandler<int>? OnSubscriptionDataDiagnosticsChange;

        ///  <inheritdoc/>
        public event EventHandler<int>? OnSubscriptionEventDiagnosticsChange;

        /// <summary>
        /// Subscription
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="metrics"></param>
        private OpcUaSubscription(IClientAccessor<ConnectionModel> clients,
            IOptions<OpcUaClientOptions> options, ILoggerFactory loggerFactory,
            IMetricsContext? metrics)
        {
            _clients = clients ??
                throw new ArgumentNullException(nameof(clients));
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));
            _metrics = metrics ??
                throw new ArgumentNullException(nameof(metrics));

            _logger = loggerFactory.CreateLogger<OpcUaSubscription>();
            _lock = new SemaphoreSlim(1, 1);
            _currentlyMonitored = ImmutableHashSet<IOpcUaMonitoredItem>.Empty;
            Id = SequenceNumber.Increment16(ref _lastIndex);
            _timer = new Timer(OnSubscriptionManagementTriggered);
            InitializeMetrics();
        }

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="outer"></param>
        /// <param name="options"></param>
        /// <param name="subscription"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="metrics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async ValueTask<IOpcUaSubscription> CreateAsync(
            IClientAccessor<ConnectionModel> outer, IOptions<OpcUaClientOptions> options,
            SubscriptionModel subscription, ILoggerFactory loggerFactory,
            IMetricsContext? metrics, CancellationToken ct = default)
        {
            // Create object
            var newSubscription = new OpcUaSubscription(outer, options, loggerFactory, metrics);

            // Initialize
            await newSubscription.UpdateAsync(subscription, ct).ConfigureAwait(false);

            return newSubscription;
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return $"{_subscription?.Id?.ToString() ?? "<new>"}:{_currentSubscription?.Id ?? 0}";
        }

        /// <inheritdoc/>
        public void OnSubscriptionStateChanged(bool online, int connectionAttempts)
        {
            _connectionAttempts = connectionAttempts;
            _online = online;

            foreach (var monitoredItem in _currentlyMonitored)
            {
                monitoredItem.OnMonitoredItemStateChanged(online);
            }
        }

        /// <inheritdoc/>
        public bool TryGetCurrentPosition(out uint subscriptionId, out uint sequenceNumber)
        {
            subscriptionId = _currentSubscription?.Id ?? 0;
            sequenceNumber = _currentSequenceNumber;
            return _useDeferredAcknoledge;
        }

        /// <inheritdoc/>
        public IOpcUaSubscriptionNotification? CreateKeepAlive()
        {
            _lock.Wait();
            try
            {
                var subscription = _currentSubscription;
                if (subscription == null)
                {
                    return null;
                }
                return new Notification(this, subscription.Id)
                {
                    ServiceMessageContext = subscription.Session.MessageContext,
                    ApplicationUri = subscription.Session.Endpoint.Server.ApplicationUri,
                    EndpointUrl = subscription.Session.Endpoint.EndpointUrl,
                    SubscriptionName = Name,
                    SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                    SubscriptionId = Id,
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
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask UpdateAsync(SubscriptionModel subscription,
            CancellationToken ct)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }
            if (subscription.Configuration == null)
            {
                throw new ArgumentException("Missing configuration", nameof(subscription));
            }
            if (subscription.Id?.Connection == null)
            {
                throw new ArgumentException("Missing connection information", nameof(subscription));
            }
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var previousSubscription = _subscription?.Id;

                // Update subscription configuration
                _subscription = subscription.Clone();

                if (previousSubscription is not null && previousSubscription != _subscription.Id)
                {
                    //
                    // TODO: Do we need to remove any session  from session manager
                    // if the subscription was transfered to new connection model
                    // has changed?
                    //
                    _logger.LogError("Upgrading existing subscription to different session.");
                }

                // try to get a session using the provided configuration
                using var client = _clients.GetOrCreateClient(_subscription.Id.Connection);

                //
                // Now register with the session to ensure the state is re-applied when
                // session changes or connects if unconnected.
                // Registering takes a ref count on the client which will stay alive and
                // hopefully connected through the lifetime of the subscription.
                // Note that we can call Register many times and only a single ref count
                // is taken.
                //
                client.RegisterSubscription(this);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask SyncWithSessionAsync(IOpcUaSession handle, CancellationToken ct)
        {
            if (_closed)
            {
                return;
            }

            // If we get here we are online
            _online = true;

            // Lock access to the subscription state while we are applying the state.
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                try
                {
                    await SyncWithSessionInternalAsync(handle, ct).ConfigureAwait(false);
                    return;
                }
                catch (Exception e)
                {
                    _online = false; // Set true when retry comes back in

                    _logger.LogDebug(e,
                        "Failed to apply state to Subscription {Subscription} in session {Session}...",
                        this, handle);

                    // Retry in 2 seconds
                    TriggerSubscriptionManagementCallbackIn(
                        _options.Value.SubscriptionErrorRetryDelay, kDefaultErrorRetryDelay);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask CloseAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            IOpcUaClient? client = null;
            try
            {
                var connection = _subscription?.Id.Connection;
                if (_closed || connection == null)
                {
                    return;
                }
                _closed = true;

                client = _clients.GetClient(connection);
                if (client == null)
                {
                    _logger.LogWarning(
                        "Failed to unregister subscription '{Subscription}'. " +
                        "The client for the connection could not be found.", this);
                    return;
                }

                // Unregister subscription from session
                client.UnregisterSubscription(this);
            }
            catch (ObjectDisposedException) { } // client accessor already disposed
            finally
            {
                // Synchronize subscription first then close the synchronized subscription
                if (client is ISessionAccessor accessor && accessor.TryGetSession(out var session))
                {
                    _currentSubscription = await SynchronizeCurrentSubscriptionAsync(
                        session).ConfigureAwait(false);
                }
                // Does not throw
                await CloseCurrentSubscriptionAsync().ConfigureAwait(false);
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_closed)
            {
                Try.Async(() => CloseAsync().AsTask()).Wait();

                Debug.Assert(_closed);
            }

            _timer.Dispose();
            _meter.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        /// Send notification
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="notifications"></param>
        internal void SendNotification(MessageType messageType,
            IEnumerable<MonitoredItemNotificationModel> notifications)
        {
            var subscription = _currentSubscription;
            if (subscription == null || subscription.Id == 0)
            {
                return;
            }
            if (messageType == MessageType.Event || messageType == MessageType.Condition)
            {
                var onSubscriptionEventDiagnosticsChange = OnSubscriptionEventDiagnosticsChange;
                var onSubscriptionEventChange = OnSubscriptionEventChange;
                if (onSubscriptionEventChange == null)
                {
                    return;
                }
                onSubscriptionEventChange.Invoke(this,
                    CreateMessage(notifications, messageType, subscription));
                onSubscriptionEventDiagnosticsChange?.Invoke(this, 1);
            }
            else
            {
                var onSubscriptionDataDiagnosticsChange = OnSubscriptionDataDiagnosticsChange;
                var onSubscriptionDataChange = OnSubscriptionDataChange;
                if (onSubscriptionDataChange == null)
                {
                    return;
                }
                onSubscriptionDataChange.Invoke(this,
                    CreateMessage(notifications, messageType, subscription));
                onSubscriptionDataDiagnosticsChange?.Invoke(this, 1);
            }

            Notification CreateMessage(IEnumerable<MonitoredItemNotificationModel> notifications,
                MessageType messageType, Subscription subscription)
            {
                return new Notification(this, subscription.Id, notifications)
                {
                    ServiceMessageContext = subscription.Session?.MessageContext,
                    ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                    SubscriptionName = Name,
                    SubscriptionId = Id,
                    SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType = messageType,
                    PublishTimestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Close subscription
        /// </summary>
        /// <returns></returns>
        private async Task CloseCurrentSubscriptionAsync()
        {
            Debug.Assert(_lock.CurrentCount == 0); // Should be always under lock
            var subscription = _currentSubscription;
            if (subscription == null)
            {
                // Already closed
                return;
            }
            _currentSubscription = null;
            try
            {
                subscription.FastDataChangeCallback = null;
                subscription.FastEventCallback = null;
                subscription.Handle = null;

                _logger.LogDebug("Closing subscription '{Subscription}'...", this);
                _currentSequenceNumber = 0;

                await Try.Async(
                    () => subscription.SetPublishingModeAsync(false)).ConfigureAwait(false);
                await Try.Async(
                    () => subscription.DeleteItemsAsync(default)).ConfigureAwait(false);
                await Try.Async(
                    () => subscription.ApplyChangesAsync()).ConfigureAwait(false);
                _logger.LogDebug("Deleted monitored items for '{Subscription}'.", this);

                await Try.Async(
                    () => subscription.DeleteAsync(true)).ConfigureAwait(false);

                subscription.Session?.RemoveSubscription(subscription);
                _logger.LogInformation("Subscription '{Subscription}' closed.", this);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to close subscription {Subscription}", this);
            }
            finally
            {
                subscription.Dispose();
            }
        }

        /// <summary>
        /// Synchronize monitored items in subscription (no lock)
        /// </summary>
        /// <param name="rawSubscription"></param>
        /// <param name="sessionHandle"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="ct"></param>
        private async Task<bool> SynchronizeMonitoredItemsAsync(Subscription rawSubscription,
            IOpcUaSession sessionHandle, IEnumerable<BaseMonitoredItemModel> monitoredItems,
            CancellationToken ct)
        {
            Debug.Assert(_lock.CurrentCount == 0);

            TriggerSubscriptionManagementCallbackIn(Timeout.InfiniteTimeSpan);

            // Get limits to batch requests during resolve
            var operationLimits = await sessionHandle.GetOperationLimitsAsync(
                ct).ConfigureAwait(false);

            var desired = OpcUaMonitoredItem
                .Create(monitoredItems, _loggerFactory, _clients,
                    _subscription?.Id.Connection == null ? null :
                        new ConnectionIdentifier(_subscription.Id.Connection))
                .ToHashSet();
            var previouslyMonitored = _currentlyMonitored;

            var remove = previouslyMonitored.Except(desired);
            var add = desired.Except(previouslyMonitored).ToImmutableHashSet();
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
                .Where(a => a != null)
                .ToList();
            if (allResolvers.Count > 0)
            {
                foreach (var resolvers in allResolvers.Batch(
                    (int?)operationLimits.MaxNodesPerTranslatePathsToNodeIds ?? 1))
                {
                    var response = await sessionHandle.Services.TranslateBrowsePathsToNodeIdsAsync(
                        new RequestHeader(), new BrowsePathCollection(resolvers
                            .Select(a => new BrowsePath
                            {
                                StartingNode = a!.Value.NodeId.ToNodeId(
                                    sessionHandle.MessageContext),
                                RelativePath = a.Value.Path.ToRelativePath(
                                    sessionHandle.MessageContext)
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
                                sessionHandle.MessageContext.NamespaceUris);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to resolve browse path for {NodeId} " +
                                "in {Subscription} due to '{ServiceResult}'",
                                result.Request!.Value.NodeId, this, result.ErrorInfo);
                        }
                        result.Request!.Value.Update(resolvedId, sessionHandle.MessageContext);
                    }
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
                .Where(a => a != null)
                .ToList();
            if (allRegistrations.Count > 0)
            {
                foreach (var registrations in allRegistrations.Batch(
                    (int?)operationLimits.MaxNodesPerRegisterNodes ?? 1))
                {
                    var response = await sessionHandle.Services.RegisterNodesAsync(
                        new RequestHeader(), new NodeIdCollection(registrations
                            .Select(a => a!.Value.NodeId.ToNodeId(sessionHandle.MessageContext))),
                        ct).ConfigureAwait(false);
                    foreach (var result in response.RegisteredNodeIds.Zip(registrations))
                    {
                        Debug.Assert(result.Second != null);
                        if (!NodeId.IsNull(result.First))
                        {
                            result.Second.Value.Update(result.First, sessionHandle.MessageContext);
                        }
                    }
                }
            }

            var metadataChanged = false;
            var applyChanges = false;
            var session = rawSubscription.Session;
            var updated = 0;
            var errors = 0;

            foreach (var toUpdate in same)
            {
                if (!desired.TryGetValue(toUpdate, out var theUpdate))
                {
                    errors++;
                    continue;
                }
                desired.Remove(theUpdate);
                Debug.Assert(toUpdate.GetType() == theUpdate.GetType());
                try
                {
                    if (toUpdate.MergeWith(theUpdate, sessionHandle, out var metadata))
                    {
                        _logger.LogDebug(
                            "Trying to update monitored item '{Item}' in {Subscription}...",
                            toUpdate, this);
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
                    theUpdate.Dispose();
                }
            }

            var removed = 0;
            foreach (var toRemove in remove)
            {
                try
                {
                    if (toRemove.RemoveFrom(rawSubscription, out var metadata))
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
                    if (toAdd.AddTo(rawSubscription, sessionHandle, out var metadata))
                    {
                        _logger.LogDebug(
                            "Adding monitored item '{Item}' to {Subscription}...",
                            toAdd, this);
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
                await rawSubscription.ApplyChangesAsync(ct).ConfigureAwait(false);
                if (rawSubscription.MonitoredItemCount == 0)
                {
                    await rawSubscription.SetPublishingModeAsync(false, ct).ConfigureAwait(false);
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
            // Note that we use the desired set hereand  update the display
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
                .Select(a => a.DisplayName)
                .Where(a => a != null)
                .ToList();
            if (allDisplayNameUpdates.Count > 0)
            {
                foreach (var displayNameUpdates in allDisplayNameUpdates.Batch(
                    (int?)operationLimits.MaxNodesPerRead ?? 1))
                {
                    var response = await sessionHandle.Services.ReadAsync(new RequestHeader(),
                        0, Opc.Ua.TimestampsToReturn.Neither, new ReadValueIdCollection(
                        displayNameUpdates.Select(a => new ReadValueId
                        {
                            NodeId = a!.Value.NodeId.ToNodeId(sessionHandle.MessageContext),
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

            var successfullyCompletedItems = new List<IOpcUaMonitoredItem>();
            foreach (var monitoredItem in desiredMonitoredItems)
            {
                if (!monitoredItem.TryCompleteChanges(
                    rawSubscription, ref applyChanges, SendNotification))
                {
                    // Apply any changes from this second pass
                    invalidItems++;
                }
                else
                {
                    successfullyCompletedItems.Add(monitoredItem);
                }
            }

            if (applyChanges)
            {
                // Apply any additional changes
                await rawSubscription.ApplyChangesAsync(ct).ConfigureAwait(false);
            }

            //
            // Ensure metadata is already available when we enable publishing if this is a new
            // subscription. This ensures that the meta data is part of the first notification.
            //
            // TODO: We need a versioning scheme to align the metadata changes with the
            // notifications we receive. Right now if not initial change it is possible that
            // notifications arrive from previous state that already have the new metadata.
            //
            _currentlyMonitored = successfullyCompletedItems.ToImmutableHashSet();
            if (metadataChanged)
            {
                _currentMetaData = await BuildMetadataAsync(sessionHandle, session, _currentlyMonitored,
                    ct).ConfigureAwait(false);
                metadataChanged = false;
            }

            //
            // Finally change the monitoring mode as required. Batch the requests
            // on the update of monitored item state from monitored items. On AddTo
            // the monitoring mode was already configured. This is for updates as
            // they are not applied through ApplyChanges
            //
            foreach (var change in successfullyCompletedItems.GroupBy(i => i.MonitoringModeChange))
            {
                if (change.Key == null)
                {
                    // Not a valid item
                    continue;
                }

                foreach (var itemsBatch in change.Select(t => t.Item!).Batch(
                    (int?)operationLimits.MaxMonitoredItemsPerCall ?? 1))
                {
                    var itemsToChange = itemsBatch.ToList();
                    _logger.LogInformation(
                        "Set monitoring to {Value} for {Count} items in subscription {Subscription}.",
                        change.Key.Value, itemsToChange.Count, this);

                    var results = await rawSubscription.SetMonitoringModeAsync(
                        change.Key.Value, itemsToChange.ToList(), ct).ConfigureAwait(false);
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
                                    _logger.LogWarning("Set monitoring for item '{Item}' in subscription "
                                        + "{Subscription} failed with '{Status}'.", itemsToChange[i].StartNodeId,
                                        this, results[i].StatusCode);
                                }
                            }
                            noErrorFound = false;
                        }
                    }
                }
            }

            // Cleanup all items that are not in the currently monitoring list
            previouslyMonitored
                .Except(_currentlyMonitored)
                .ToList()
                .ForEach(m => m.Dispose());
            previouslyMonitored = ImmutableHashSet<IOpcUaMonitoredItem>.Empty;

            // Update subscription state
            NumberOfNotCreatedItems = invalidItems;
            NumberOfCreatedItems = _currentlyMonitored.Count - invalidItems;

            _logger.LogInformation(
                "Now monitoring {Count} nodes in subscription {Subscription}.",
                _currentlyMonitored.Count, this);

            // Refresh condition
            if (_currentlyMonitored.OfType<OpcUaMonitoredItem.Condition>().Any())
            {
                _logger.LogInformation(
                    "Issuing ConditionRefresh on subscription {Subscription}", this);
                try
                {
                    await rawSubscription.ConditionRefreshAsync(ct).ConfigureAwait(false);
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
                // Retry applying invalid items every 5 minutes
                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.InvalidMonitoredItemRetryDelay, TimeSpan.FromMinutes(5));
            }
            else if (desiredMonitoredItems.Count != _currentlyMonitored.Count)
            {
                // Try to periodically update the subscription
                // TODO: Trigger on address space model changes...

                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.BadMonitoredItemRetryDelay, TimeSpan.FromMinutes(30));
            }
            else
            {
                // Nothing to do
                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.SubscriptionManagementInterval, Timeout.InfiniteTimeSpan);
            }

            return noErrorFound;
        }

        /// <summary>
        /// Collect metadata for the monitored items in the data set
        /// </summary>
        /// <param name="sessionHandle"></param>
        /// <param name="session"></param>
        /// <param name="monitoredItemsInDataSet"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<DataSetMetaDataType?> BuildMetadataAsync(IOpcUaSession sessionHandle,
            ISession session, ImmutableHashSet<IOpcUaMonitoredItem> monitoredItemsInDataSet,
            CancellationToken ct)
        {
            if (_subscription?.Configuration?.MetaData == null)
            {
                // Metadata disabled
                return null;
            }

            //
            // Use the date time to version across reboots. This could be done more elegantly by
            // saving the last version to persistent storage such as twin, but this is ok for
            // the sake of being able to have an incremental version number defining metadata changes.
            //
            var metaDataVersion = DateTime.UtcNow.ToBinary();
            var major = (uint)(metaDataVersion >> 32);
            var minor = (uint)metaDataVersion;

            _logger.LogInformation(
                "Metadata changed to {Major}.{Minor} for subscription {Subscription}.",
                major, minor, this);

            var typeSystem = await sessionHandle.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
            var dataTypes = new NodeIdDictionary<DataTypeDescription>();
            var fields = new FieldMetaDataCollection();
            foreach (var monitoredItem in monitoredItemsInDataSet)
            {
                monitoredItem.GetMetaData(sessionHandle, typeSystem, fields, dataTypes);
            }

            return new DataSetMetaDataType
            {
                Name = _subscription.Configuration.MetaData.Name,
                DataSetClassId = (Uuid)_subscription.Configuration.MetaData.DataSetClassId,
                Namespaces = session.NamespaceUris.ToArray(),
                EnumDataTypes = dataTypes.Values.OfType<EnumDescription>().ToArray(),
                StructureDataTypes = dataTypes.Values.OfType<StructureDescription>().ToArray(),
                SimpleDataTypes = dataTypes.Values.OfType<SimpleTypeDescription>().ToArray(),
                Fields = fields,
                Description = _subscription.Configuration.MetaData.Description,
                ConfigurationVersion = new ConfigurationVersionDataType
                {
                    MajorVersion = major,
                    MinorVersion = minor
                }
            };
        }

        /// <summary>
        /// Resets the operation timeout on the session accrding to the
        /// publishing intervals on all subscriptions.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="newSubscription"></param>
        private void ReapplySessionOperationTimeout(ISession session, Subscription newSubscription)
        {
            if (session == null)
            {
                return;
            }

            var currentOperationTimeout = _options.Value.Quotas.OperationTimeout;
            var localMaxOperationTimeout =
                newSubscription.PublishingInterval * (int)newSubscription.KeepAliveCount;
            if (currentOperationTimeout < localMaxOperationTimeout)
            {
                currentOperationTimeout = localMaxOperationTimeout;
            }

            foreach (var subscription in session.Subscriptions)
            {
                localMaxOperationTimeout = (int)subscription.CurrentPublishingInterval
                    * (int)subscription.CurrentKeepAliveCount;
                if (currentOperationTimeout < localMaxOperationTimeout)
                {
                    currentOperationTimeout = localMaxOperationTimeout;
                }
            }
            if (session.OperationTimeout != currentOperationTimeout)
            {
                session.OperationTimeout = currentOperationTimeout;
            }
        }

        /// <summary>
        /// Apply state to session
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask SyncWithSessionInternalAsync(IOpcUaSession handle,
            CancellationToken ct)
        {
            Debug.Assert(_lock.CurrentCount == 0);

            // Get the raw session object from the session handle to do the heart surgery
            if (handle is not ISessionAccessor accessor ||
                !accessor.TryGetSession(out var session)) // Should never happen.
            {
                _logger.LogInformation(
                    "Failed to access session in {Session} to update subscription {Subscription}.",
                    handle, this);
                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.CreateSessionTimeout, TimeSpan.FromSeconds(10));
                return;
            }

            //
            // While we access the session it is valid and what is more connected.
            // We are called on the client connection manager thread and thus can
            // access the session however we want without anyone disconnecting it.
            //
            Debug.Assert(session.Connected);

            // Should not happen since we are called under lock.
            Debug.Assert(_subscription != null, "No subscription during apply");

            // Create or update the subscription inside the raw session object.
            var subscription = await AddOrUpdateSubscriptionInSessionAsync(handle, session,
                ct).ConfigureAwait(false);

            if (subscription == null)
            {
                _logger.LogWarning(
                    "Could not add or update a Subscription {Subscription} in {Session}.",
                    this, handle);

                TriggerSubscriptionManagementCallbackIn(
                    _options.Value.SubscriptionErrorRetryDelay, kDefaultErrorRetryDelay);
                return;
            }

            if (_subscription.MonitoredItems != null)
            {
                // Resolves and sets the monitored items in the subscription
                await SynchronizeMonitoredItemsAsync(subscription, handle,
                    _subscription.MonitoredItems, ct).ConfigureAwait(false);
            }

            if (!subscription.PublishingEnabled || subscription.ChangesPending)
            {
                _logger.LogDebug(
                    "Enabling subscription {Subscription} in session {Session}...",
                    this, handle);

                if (subscription.ChangesPending)
                {
                    await subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
                }

                if (!subscription.PublishingEnabled)
                {
                    await subscription.SetPublishingModeAsync(true, ct).ConfigureAwait(false);
                }

                _logger.LogInformation(
                    "Subscription {Subscription} in session {Session} enabled.",
                    this, handle);
            }
        }

        /// <summary>
        /// Get a subscription with the supplied configuration (no lock)
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private async ValueTask<Subscription?> AddOrUpdateSubscriptionInSessionAsync(
            IOpcUaSession handle, ISession session, CancellationToken ct)
        {
            Debug.Assert(session.DefaultSubscription != null, "No default subscription template.");
            Debug.Assert(_lock.CurrentCount == 0); // Under lock

            var subscription = await SynchronizeCurrentSubscriptionAsync(session).ConfigureAwait(false);

            GetSubscriptionConfiguration(subscription ?? session.DefaultSubscription,
                out var configuredPublishingInterval, out var configuredPriority,
                out var configuredKeepAliveCount, out var configuredLifetimeCount,
                out var configuredMaxNotificationsPerPublish);

            if (subscription == null)
            {
                Debug.Assert(_currentSubscription == null);

                subscription = new Subscription(session.DefaultSubscription)
                {
                    Handle = this,
                    DisplayName = Name,
                    PublishingEnabled = false, // always false on initialization
                    TimestampsToReturn = Opc.Ua.TimestampsToReturn.Both,
                    KeepAliveCount = configuredKeepAliveCount,
                    PublishingInterval = configuredPublishingInterval,
                    MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish,
                    LifetimeCount = configuredLifetimeCount,
                    Priority = configuredPriority,
                    // TODO: use a channel and reorder task before calling OnMessage
                    // to order or else republish is called too often
                    SequentialPublishing = true,
                    DisableMonitoredItemCache = true, // Not needed anymore
                    RepublishAfterTransfer = true,
                    FastKeepAliveCallback = OnSubscriptionKeepAliveNotification,
                    FastDataChangeCallback = OnSubscriptionDataChangeNotification,
                    FastEventCallback = OnSubscriptionEventNotificationList
                };

                ReapplySessionOperationTimeout(session, subscription);

                var result = session.AddSubscription(subscription);
                if (!result)
                {
                    _logger.LogError(
                        "Failed to add subscription '{Subscription}' to session {Session}",
                        this, handle);
                    return null;
                }

                _logger.LogInformation(
                    "Create new subscription '{Subscription}' in session {Session}",
                    this, handle);

                Debug.Assert(!subscription.PublishingEnabled);
                await subscription.CreateAsync(ct).ConfigureAwait(false);

                if (!subscription.Created)
                {
                    throw new ServiceResultException(StatusCodes.BadSubscriptionIdInvalid,
                        $"Failed to create subscription {this} in session {session}");
                }

                LogRevisedValues(subscription, true);
                Debug.Assert(subscription.Id != 0);
                _useDeferredAcknoledge = _subscription?.Configuration?.UseDeferredAcknoledgements
                    ?? false;
            }
            else
            {
                Debug.Assert(_currentSubscription != null);
                Debug.Assert(_currentSubscription.Id == subscription.Id);

                // Apply new configuration on configuration on original subscription
                var modifySubscription = false;

                if (configuredKeepAliveCount != subscription.KeepAliveCount)
                {
                    _logger.LogInformation(
                        "Change KeepAliveCount to {New} in Subscription {Subscription}...",
                        configuredKeepAliveCount, this);

                    subscription.KeepAliveCount = configuredKeepAliveCount;
                    modifySubscription = true;
                }
                if (subscription.PublishingInterval != configuredPublishingInterval)
                {
                    _logger.LogInformation(
                        "Change publishing interval to {New} in Subscription {Subscription}...",
                        configuredPublishingInterval, this);
                    subscription.PublishingInterval = configuredPublishingInterval;
                    modifySubscription = true;
                }

                if (subscription.MaxNotificationsPerPublish != configuredMaxNotificationsPerPublish)
                {
                    _logger.LogInformation(
                        "Change MaxNotificationsPerPublish to {New} in Subscription {Subscription}",
                        configuredMaxNotificationsPerPublish, this);
                    subscription.MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish;
                    modifySubscription = true;
                }

                if (subscription.LifetimeCount != configuredLifetimeCount)
                {
                    _logger.LogInformation(
                        "Change LifetimeCount to {New} in Subscription {Subscription}...",
                        configuredLifetimeCount, this);
                    subscription.LifetimeCount = configuredLifetimeCount;
                    modifySubscription = true;
                }
                if (subscription.Priority != configuredPriority)
                {
                    _logger.LogInformation(
                        "Change Priority to {New} in Subscription {Subscription}...",
                        configuredPriority, this);
                    subscription.Priority = configuredPriority;
                    modifySubscription = true;
                }
                if (modifySubscription)
                {
                    await subscription.ModifyAsync(ct).ConfigureAwait(false);
                    _logger.LogInformation(
                        "Subscription {Subscription} in session {Session} successfully modified.",
                        this, handle);
                    LogRevisedValues(subscription, false);
                }
            }
            _currentSubscription = subscription;
            return subscription;
        }

        /// <summary>
        /// <para>
        /// We must ensure that our subscription is in the session we were passed.
        /// If not, then we close the current subscription and re-create the subscription.
        /// </para>
        /// <para>
        /// When session is created there are no subscriptions in it. When session is
        /// reconnected, the reconnected session already contains our subscriptions
        /// (copy constructure of session invoked).
        /// </para>
        /// <para>
        /// When the session is recreated using Session.Recreate then the subscriptions
        /// are transferred to it. If they cannot be transferred then the subscriptions
        /// are recreated using Create and are then again correctly associated with us.
        /// If that all fails and the reconnect handler returns with error the reconnect
        /// handler is started again right away and the process restarts until the client
        /// is forcefully disconnected.
        /// </para>
        /// <para>
        /// This is just a precaution since reconnecthandler should work all the time and
        /// if we ever get here we are dealing with a bug in the stack.
        /// </para>
        /// <para>
        /// Note that we cannot use the id on the server as unique identity because the
        /// same id might refer to a different subscription after a restart of the server.
        /// We use the Handle to reference OpcUaSubscription instance and then validate
        /// correctness here.
        /// </para>
        /// </summary>
        /// <param name="session">
        /// The session containing the correct subscription associated with this object.
        /// </param>
        /// <returns>
        /// The actual subscription as the one that was valid in the session that was passed.
        /// </returns>
        private async Task<Subscription?> SynchronizeCurrentSubscriptionAsync(ISession session)
        {
            var subscription = _currentSubscription;
            if (subscription != null)
            {
                // Ensure the session contains the subscription
                subscription = session.Subscriptions.SingleOrDefault(s => s.Handle == this);

                // If it does not, close the current subscription
                if (subscription == null || _currentSubscription!.Id != subscription.Id)
                {
                    // Does not throw
                    await CloseCurrentSubscriptionAsync().ConfigureAwait(false);
                    subscription = null;
                }
            }
            return subscription;
        }

        /// <summary>
        /// Log revised values of the subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="created"></param>
        private void LogRevisedValues(Subscription subscription, bool created)
        {
            _logger.LogInformation(@"Successfully {Action} subscription {Subscription}'.
Actual (revised) state/desired state:
# PublishingEnabled {CurrentPublishingEnabled}/{PublishingEnabled}
# PublishingInterval {CurrentPublishingInterval}/{PublishingInterval}
# KeepAliveCount {CurrentKeepAliveCount}/{KeepAliveCount}
# LifetimeCount {CurrentLifetimeCount}/{LifetimeCount}", created ? "created" : "modified",
                this,
                subscription.CurrentPublishingEnabled, subscription.PublishingEnabled,
                subscription.CurrentPublishingInterval, subscription.PublishingInterval,
                subscription.CurrentKeepAliveCount, subscription.KeepAliveCount,
                subscription.CurrentLifetimeCount, subscription.LifetimeCount);
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
            publishingInterval = (int)((_subscription?.Configuration?.PublishingInterval) ??
                TimeSpan.FromSeconds(1)).TotalMilliseconds;
            keepAliveCount = (_subscription?.Configuration?.KeepAliveCount) ??
                defaultSubscription.KeepAliveCount;
            maxNotificationsPerPublish = (_subscription?.Configuration?.MaxNotificationsPerPublish) ??
                defaultSubscription.MaxNotificationsPerPublish;
            lifetimeCount = (_subscription?.Configuration?.LifetimeCount) ??
                defaultSubscription.LifetimeCount;
            priority = (_subscription?.Configuration?.Priority) ??
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
            if (delay == null || delay == TimeSpan.Zero)
            {
                delay = defaultDelay;
            }
            if (delay != Timeout.InfiniteTimeSpan)
            {
                _logger.LogDebug(
                    "Setting up trigger to reapply state to {Subscription} in {Timeout}",
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
            var connection = _subscription?.Id.Connection;
            if (connection != null)
            {
                // try to get a session using the provided configuration
                using var client = _clients.GetClient(connection);
                client?.ManageSubscription(this);
            }
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
            if (subscription == null || subscription.Id != _currentSubscription?.Id)
            {
                _logger.LogWarning(
                    "EventChange for wrong subscription {Id} received on {Subscription}.",
                    subscription?.Id, this);
                return;
            }
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

            var onSubscriptionEventChange = OnSubscriptionEventChange;
            if (onSubscriptionEventChange == null)
            {
                return;
            }

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
                else if (!SequenceNumber.Validate(sequenceNumber, ref _previousSequenceNumber,
                    out var missingSequenceNumbers, out var dropped))
                {
                    _logger.LogWarning("Event subscription notification for subscription " +
                        "{Subscription} has unexpected sequenceNumber {SequenceNumber} missing " +
                        "{ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                        this, sequenceNumber,
                        SequenceNumber.ToString(missingSequenceNumbers), dropped ?
                        "dropped" : "already received", publishTime);
                }

                var numOfEvents = 0;
                foreach (var eventFieldList in notification.Events)
                {
                    Debug.Assert(eventFieldList != null);
                    var monitoredItem = subscription.FindItemByClientHandle(
                        eventFieldList.ClientHandle);

                    if (monitoredItem?.Handle is IOpcUaMonitoredItem wrapper)
                    {
                        var message = new Notification(this, subscription.Id, sequenceNumber: sequenceNumber)
                        {
                            ServiceMessageContext = subscription.Session?.MessageContext,
                            ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                            EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                            SubscriptionName = Name,
                            SubscriptionId = Id,
                            SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                            MessageType = MessageType.Event,
                            PublishTimestamp = publishTime
                        };

                        wrapper.TryGetMonitoredItemNotifications(message.SequenceNumber,
                            message.PublishTimestamp, eventFieldList, message.Notifications);

                        if (message.Notifications.Count > 0)
                        {
                            onSubscriptionEventChange.Invoke(this, message);
                            numOfEvents++;
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Monitored item not found with client handle {ClientHandle} " +
                            "for Event received for subscription {Subscription}.",
                            eventFieldList.ClientHandle, this);
                    }
                }
                var onSubscriptionEventDiagnosticsChange = OnSubscriptionEventDiagnosticsChange;
                onSubscriptionEventDiagnosticsChange?.Invoke(this, numOfEvents);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
        }

        /// <summary>
        /// Handle keep alive messages
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnSubscriptionKeepAliveNotification(Subscription subscription, NotificationData notification)
        {
            if ((_currentSubscription?.Id ?? 0) == 0 || subscription == null)
            {
                // Nothing to do here
                _logger.LogDebug("Got Keep alive without subscription in {Subscription}.",
                    this);
                return;
            }

            Debug.Assert(_currentSubscription != null);
            if (subscription.Id != _currentSubscription.Id)
            {
                _logger.LogWarning(
                    "Keep alive for wrong subscription {Id} received on {Subscription}.",
                    subscription.Id, this);
                return;
            }

            var onSubscriptionKeepAlive = OnSubscriptionKeepAlive;
            if (onSubscriptionKeepAlive == null)
            {
                return;
            }

            var sequenceNumber = notification.SequenceNumber;
            var publishTime = notification.PublishTime;

            // in case of a keepalive,the sequence number is not incremented by the servers
            _logger.LogDebug("Keep alive for subscription {Subscription} " +
                "with sequenceNumber {SequenceNumber}, publishTime {PublishTime}.",
                this, sequenceNumber, publishTime);

            var message = new Notification(this, subscription.Id)
            {
                ServiceMessageContext = subscription.Session?.MessageContext,
                ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                SubscriptionName = Name,
                PublishTimestamp = publishTime,
                SubscriptionId = Id,
                SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                MessageType = MessageType.KeepAlive
            };

            onSubscriptionKeepAlive.Invoke(this, message);
            Debug.Assert(message.Notifications != null);
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
            if (subscription == null || subscription.Id != _currentSubscription?.Id)
            {
                _logger.LogWarning(
                    "DataChange for wrong subscription {Id} received on {Subscription}.",
                    subscription?.Id, this);
                return;
            }

            var onSubscriptionDataChange = OnSubscriptionDataChange;
            if (onSubscriptionDataChange == null)
            {
                return;
            }
            try
            {
                var sequenceNumber = notification.SequenceNumber;
                var publishTime = notification.PublishTime;

                var message = new Notification(this, subscription.Id, sequenceNumber: sequenceNumber)
                {
                    ServiceMessageContext = subscription.Session?.MessageContext,
                    ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                    SubscriptionName = Name,
                    SubscriptionId = Id,
                    PublishTimestamp = publishTime,
                    SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                    MessageType = MessageType.DeltaFrame
                };

                Debug.Assert(notification.MonitoredItems != null);

                // All notifications have the same message and thus sequence number
                if (sequenceNumber == 1)
                {
                    // Do not log when the sequence number is 1 after reconnect
                    _previousSequenceNumber = 1;
                }
                else if (!SequenceNumber.Validate(sequenceNumber, ref _previousSequenceNumber,
                    out var missingSequenceNumbers, out var dropped))
                {
                    _logger.LogWarning("DataChange notification for subscription " +
                        "{Subscription} has unexpected sequenceNumber {SequenceNumber} " +
                        "missing {ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                        this, sequenceNumber,
                        SequenceNumber.ToString(missingSequenceNumbers),
                        dropped ? "dropped" : "already received", publishTime);
                }

                foreach (var item in notification.MonitoredItems)
                {
                    Debug.Assert(item != null);
                    var monitoredItem = subscription.FindItemByClientHandle(item.ClientHandle);

                    if (monitoredItem?.Handle is IOpcUaMonitoredItem wrapper)
                    {
                        wrapper.TryGetMonitoredItemNotifications(message.SequenceNumber,
                            message.PublishTimestamp, item, message.Notifications);
                    }
                    else
                    {
                        _logger.LogWarning("Monitored item not found with client handle {ClientHandle} " +
                            "for DataChange received for subscription {Subscription}",
                            item.ClientHandle, this);
                    }
                }

                onSubscriptionDataChange.Invoke(this, message);
                Debug.Assert(message.Notifications != null);
                var onSubscriptionDataDiagnosticsChange = OnSubscriptionDataDiagnosticsChange;
                if (message.Notifications.Count > 0 && onSubscriptionDataDiagnosticsChange != null)
                {
                    onSubscriptionDataDiagnosticsChange.Invoke(this, message.Notifications.Count);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
            }
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
            _lock.Wait();
            try
            {
                var subscription = _currentSubscription;
                if (subscription == null)
                {
                    notifications = null;
                    return false;
                }
                notifications = new List<MonitoredItemNotificationModel>();
                foreach (var item in _currentlyMonitored)
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
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Advance the position
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="sequenceNumber"></param>
        private void AdvancePosition(uint subscriptionId, uint? sequenceNumber)
        {
            if (sequenceNumber.HasValue && _currentSubscription?.Id == subscriptionId)
            {
                _logger.LogDebug("Advancing stream #{SubscriptionId} to #{Position}",
                    subscriptionId, sequenceNumber);
                _currentSequenceNumber = sequenceNumber.Value;
            }
        }

        /// <summary>
        /// Subscription notification container
        /// </summary>
        internal sealed record class Notification : IOpcUaSubscriptionNotification
        {
            /// <inheritdoc/>
            public object? Context { get; set; }

            /// <inheritdoc/>
            public DataSetMetaDataType? MetaData { get; private set; }

            /// <inheritdoc/>
            public uint SequenceNumber { get; internal set; }

            /// <inheritdoc/>
            public MessageType MessageType { get; internal set; }

            /// <inheritdoc/>
            public string? SubscriptionName { get; internal set; }

            /// <inheritdoc/>
            public ushort SubscriptionId { get; internal set; }

            /// <inheritdoc/>
            public string? EndpointUrl { get; internal set; }

            /// <inheritdoc/>
            public string? ApplicationUri { get; internal set; }

            /// <inheritdoc/>
            public DateTime PublishTimestamp { get; internal set; }

            /// <inheritdoc/>
            public uint? PublishSequenceNumber { get; }

            /// <inheritdoc/>
            public IServiceMessageContext? ServiceMessageContext { get; internal set; }

            /// <inheritdoc/>
            public IList<MonitoredItemNotificationModel> Notifications { get; }

            /// <summary>
            /// Create acknoledgeable notification
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="subscriptionId"></param>
            /// <param name="notifications"></param>
            /// <param name="sequenceNumber"></param>
            public Notification(OpcUaSubscription outer, uint subscriptionId,
                IEnumerable<MonitoredItemNotificationModel>? notifications = null,
                uint? sequenceNumber = null)
            {
                _outer = outer;
                PublishSequenceNumber = sequenceNumber;
                _subscriptionId = subscriptionId;

                MetaData = _outer._currentMetaData;
                Notifications = notifications?.ToList() ??
                    new List<MonitoredItemNotificationModel>();
            }

            /// <inheritdoc/>
            public bool TryUpgradeToKeyFrame()
            {
                if (!_outer.TryGetNotifications(SequenceNumber, out var allNotifications))
                {
                    return false;
                }
                MetaData = _outer._currentMetaData;
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
            private readonly OpcUaSubscription _outer;
            private readonly uint _subscriptionId;
        }

        private int NumberOfCreatedItems { get; set; }
        private int NumberOfNotCreatedItems { get; set; }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        public void InitializeMetrics()
        {
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_good_nodes",
                () => new Measurement<long>(NumberOfCreatedItems, _metrics.TagList),
                "Monitored items", "Monitored items successfully created..");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_bad_nodes",
                () => new Measurement<long>(NumberOfNotCreatedItems, _metrics.TagList),
                "Monitored items", "Monitored items with errors.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_monitored_items",
                () => new Measurement<long>(_currentlyMonitored.Count, _metrics.TagList),
                "Monitored items", "Monitored item count.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_connection_retries",
                () => new Measurement<long>(_connectionAttempts, _metrics.TagList),
                "Connection attempts", "OPC UA connect retries.");
            _meter.CreateObservableGauge("iiot_edge_publisher_is_connection_ok",
                () => new Measurement<int>(_online && !_closed ? 1 : 0, _metrics.TagList),
                "", "OPC UA connection success flag.");
        }

        private static readonly TimeSpan kDefaultErrorRetryDelay = TimeSpan.FromSeconds(2);
        private ImmutableHashSet<IOpcUaMonitoredItem> _currentlyMonitored;
        private SubscriptionModel? _subscription;
        private DataSetMetaDataType? _currentMetaData;
        private Subscription? _currentSubscription;
        private bool _online;
        private long _connectionAttempts;
        private uint _previousSequenceNumber;
        private bool _useDeferredAcknoledge;
        private uint _sequenceNumber;
        private bool _closed;
        private readonly IClientAccessor<ConnectionModel> _clients;
        private readonly IOptions<OpcUaClientOptions> _options;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IMetricsContext _metrics;
        private readonly SemaphoreSlim _lock;
        private readonly Timer _timer;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private static uint _lastIndex;
        private uint _currentSequenceNumber;
    }
}
