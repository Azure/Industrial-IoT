// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using MonitoringMode = Publisher.Models.MonitoringMode;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
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
        public event EventHandler<SubscriptionNotificationModel>? OnSubscriptionDataChange;

        /// <inheritdoc/>
        public event EventHandler<SubscriptionNotificationModel>? OnSubscriptionEventChange;

        /// <inheritdoc/>
        public event EventHandler<int>? OnSubscriptionDataDiagnosticsChange;

        ///  <inheritdoc/>
        public event EventHandler<int>? OnSubscriptionEventDiagnosticsChange;

        /// <summary>
        /// Get subscription from session (no lock)
        /// </summary>
        /// <returns></returns>
        private Subscription? Subscription
        {
            get
            {
                var connection = _subscription?.Id.Connection;
                if (connection == null)
                {
                    return null;
                }

                using var client = _clients.GetClient(connection);
                if (client is not ISessionAccessor accessor ||
                    !accessor.TryGetSession(out var session))
                {
                    return null;
                }
                return session.Subscriptions.SingleOrDefault(s => s.Handle == this);
            }
        }

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
            _currentlyMonitored = ImmutableDictionary<uint, OpcUaMonitoredItem>.Empty;
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
            return _subscription?.Id?.ToString() ?? "<new>";
        }

        /// <inheritdoc/>
        public void OnSubscriptionStateChanged(bool online, int connectionAttempts)
        {
            _connectionAttempts = connectionAttempts;
            _online = online;

            foreach (var monitoredItem in _currentlyMonitored.Values)
            {
                monitoredItem.OnMonitoredItemStateChanged(online);
            }
        }

        /// <inheritdoc/>
        public bool TryUpgradeToKeyFrame(SubscriptionNotificationModel notification)
        {
            _lock.Wait();
            try
            {
                var subscription = Subscription;
                if (subscription == null)
                {
                    return false;
                }
                var allNotifications = subscription.MonitoredItems
                        .SelectMany(m => m.LastValue.ToMonitoredItemNotifications(m))
                        .ToList();
                notification.MetaData = _currentMetaData;
                notification.MessageType = Encoders.PubSub.MessageType.KeyFrame;
                notification.Notifications = allNotifications;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to get a keyframe from monitored item cache in subscription {Subscription}.",
                    this);
                return false;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public SubscriptionNotificationModel? CreateKeepAlive()
        {
            _lock.Wait();
            try
            {
                var subscription = Subscription;
                if (subscription == null)
                {
                    return null;
                }
                return new SubscriptionNotificationModel
                {
                    ServiceMessageContext = subscription.Session.MessageContext,
                    ApplicationUri = subscription.Session.Endpoint.Server.ApplicationUri,
                    EndpointUrl = subscription.Session.Endpoint.EndpointUrl,
                    MetaData = _currentMetaData,
                    SubscriptionName = Name,
                    SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                    SubscriptionId = Id,
                    MessageType = Encoders.PubSub.MessageType.KeepAlive
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
        public async ValueTask ReapplyToSessionAsync(IOpcUaSession handle, CancellationToken ct)
        {
            if (_closed)
            {
                return;
            }

            // Lock access to the subscription state while we are applying the state.
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                try
                {
                    await ReapplyToSessionInternalAsync(handle, ct).ConfigureAwait(false);
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
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
                        "Failed to close subscription '{Subscription}'. " +
                        "The client for the connection could not be found.", this);
                    return;
                }

                // Unregister subscription from session
                client.UnregisterSubscription(this);
            }
            catch (ObjectDisposedException) { } // client accessor already disposed
            finally
            {
                _lock.Release();
            }

            if (client == null)
            {
                // Session already closed.
                return;
            }
            try
            {
                // Get raw subscription from underlying session and close that one too
                using var handle = await client.GetSessionHandleAsync().ConfigureAwait(false);
                if (handle.Handle is ISessionAccessor accessor &&
                    accessor.TryGetSession(out var session))
                {
                    var subscription = session.Subscriptions.SingleOrDefault(s => s.Handle == this);
                    if (subscription != null)
                    {
                        // This does not throw
                        await CloseSubscriptionAsync(subscription).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                client.Dispose();
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
        /// Send condition notification
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notifications"></param>
        internal void SendConditionNotification(Subscription subscription,
            IEnumerable<MonitoredItemNotificationModel> notifications)
        {
            var onSubscriptionEventChange = OnSubscriptionEventChange;
            if (onSubscriptionEventChange == null || !(subscription?.Session?.Connected ?? false))
            {
                return;
            }
            var message = new SubscriptionNotificationModel
            {
                ServiceMessageContext = subscription?.Session?.MessageContext,
                ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                SubscriptionName = Name,
                SubscriptionId = Id,
                SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                MessageType = Encoders.PubSub.MessageType.Condition,
                MetaData = _currentMetaData,
                Timestamp = DateTime.UtcNow,
                Notifications = notifications.ToList()
            };
            onSubscriptionEventChange.Invoke(this, message);
            var onSubscriptionEventDiagnosticsChange = OnSubscriptionEventDiagnosticsChange;
            onSubscriptionEventDiagnosticsChange?.Invoke(this, 1);
        }

        /// <summary>
        /// Destroy subscription
        /// </summary>
        /// <param name="subscription"></param>
        private async Task CloseSubscriptionAsync(Subscription subscription)
        {
            try
            {
                _logger.LogInformation("Closing subscription '{Subscription}'...", this);

                Try.Op(() => subscription.SetPublishingMode(false));
                await Try.Async(() => subscription.ApplyChangesAsync()).ConfigureAwait(false);
                await Try.Async(() => subscription.DeleteItemsAsync(default)).ConfigureAwait(false);
                await Try.Async(() => subscription.ApplyChangesAsync()).ConfigureAwait(false);

                _logger.LogDebug("Deleted monitored items for {Subscription}", this);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to close subscription {Subscription}", this);
            }
        }

        /// <summary>
        /// Reads the display name of the nodes to be monitored
        /// </summary>
        /// <param name="session"></param>
        private void ResolveDisplayNames(ISession session)
        {
            if (_subscription?.MonitoredItems == null ||
                !(_subscription.Configuration?.ResolveDisplayName ?? false))
            {
                return;
            }

            var unresolvedMonitoredItems = _subscription.MonitoredItems
                .OfType<DataMonitoredItemModel>()
                .Where(mi => string.IsNullOrEmpty(mi.DisplayName));
            if (!unresolvedMonitoredItems.Any())
            {
                return;
            }
            try
            {
                var nodeIds = unresolvedMonitoredItems.
                    Select(n =>
                    {
                        try
                        {
                            return n.StartNodeId.ToNodeId(session.MessageContext);
                        }
                        catch (ServiceResultException sre)
                        {
                            _logger.LogWarning(
                                "Failed to resolve display name for '{MonitoredItem}' " +
                                "in {Subscription} due to '{Message}'",
                                n.StartNodeId, this, sre.Message);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Failed to resolve display name for " +
                                "'{MonitoredItem}' in {Subscription} ", n.StartNodeId, this);
                            throw;
                        }
                        return null;
                    });
                if (nodeIds.Any())
                {
                    session.ReadDisplayName(nodeIds.ToList(), out var displayNames, out var errors);
                    var index = 0;
                    foreach (var monitoredItem in unresolvedMonitoredItems)
                    {
                        if (StatusCode.IsGood(errors[index].StatusCode))
                        {
                            monitoredItem.DisplayName = displayNames[index];
                        }
                        else
                        {
                            monitoredItem.DisplayName = null;
                            _logger.LogWarning(
                                "Failed to read display name for '{MonitoredItem}' " +
                                "in {Subscription} due to '{StatusCode}'",
                                monitoredItem.StartNodeId, this, errors[index].StatusCode);
                        }
                        index++;
                    }
                }
            }
            catch (ServiceResultException sre)
            {
                _logger.LogWarning(
                    "Failed to resolve display names for monitored items in {Subscription} due to '{Message}'.",
                    this, sre.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to resolve display names for monitored items in {Subscription}.", this);
                throw;
            }
        }

        /// <summary>
        /// Synchronize monitored items and triggering configuration in subscription (no lock)
        /// </summary>
        /// <param name="sessionHandle"></param>
        /// <param name="rawSubscription"></param>
        /// <param name="monitoredItems"></param>
        /// <param name="ct"></param>
        private async Task<bool> SetMonitoredItemsAsync(IOpcUaSession sessionHandle,
            Subscription rawSubscription, IEnumerable<BaseMonitoredItemModel> monitoredItems,
            CancellationToken ct)
        {
            TriggerSubscriptionManagementCallbackIn(Timeout.InfiniteTimeSpan);

            var currentState = rawSubscription.MonitoredItems
                .Select(m => m.Handle)
                .OfType<OpcUaMonitoredItem>()
                .ToHashSet();

            var noErrorFound = true;
            var count = 0;
            var session = rawSubscription.Session;
            if (monitoredItems?.Any() != true)
            {
                // cleanup entire subscription
                var toCleanupList = currentState
                    .Where(t => t.Item != null)
                    .Select(t => t.Item!)
                    .ToList();
                if (toCleanupList.Count > 0)
                {
                    // Remove monitored items not in desired state
                    foreach (var toRemove in toCleanupList)
                    {
                        _logger.LogDebug(
                            "Removing monitored item '{Item}' from subscription {Subscription}....",
                            toRemove.StartNodeId, this);
                        ((OpcUaMonitoredItem)toRemove.Handle).Dispose();
                        count++;
                    }
                    rawSubscription.RemoveItems(toCleanupList);
                    _logger.LogInformation(
                        "Removed {Count} monitored items from subscription {Subscription}.",
                        count, this);
                }
                _currentlyMonitored = ImmutableDictionary<uint, OpcUaMonitoredItem>.Empty;
                UpdateState(0);

                // TODO: Set metadata to empty here
                await rawSubscription.ApplyChangesAsync(ct).ConfigureAwait(false);
                await rawSubscription.SetPublishingModeAsync(false, ct).ConfigureAwait(false);
                if (rawSubscription.MonitoredItemCount != 0)
                {
                    _logger.LogWarning(
                        "Failed to remove {Count} monitored items from subscription {Subscription}",
                        rawSubscription.MonitoredItemCount, this);
                }
                return noErrorFound;
            }

            // Synchronize the desired items with the state of the raw subscription
            var desiredState = monitoredItems
                .Select(m => new OpcUaMonitoredItem(m, _loggerFactory.CreateLogger<OpcUaMonitoredItem>()))
                .ToHashSet();

            var metadataChanged = false;
            var applyChanges = false;

            var toRemoveList = currentState.Except(desiredState)
                .Where(t => t.Item != null)
                .Select(t => t.Item!)
                .ToList();
            if (toRemoveList.Count > 0)
            {
                count = 0;
                // Remove monitored items not in desired state
                foreach (var toRemove in toRemoveList)
                {
                    _logger.LogDebug(
                        "Removing monitored item '{Item}' from subscription {Subscription}....",
                        toRemove.StartNodeId, this);
                    ((OpcUaMonitoredItem)toRemove.Handle).Dispose();
                    count++;
                }
                rawSubscription.RemoveItems(toRemoveList);
                applyChanges = true;
                metadataChanged = true;

                _logger.LogInformation(
                    "Removed {Count} monitored items from subscription {Subscription}",
                    count, this);
            }

            // todo re-associate detached handles!?
            var toRemoveDetached = rawSubscription.MonitoredItems.Where(m => m.Status == null).ToList();
            if (toRemoveDetached.Count > 0)
            {
                rawSubscription.RemoveItems(toRemoveDetached);
                _logger.LogInformation(
                    "Removed {Count} detached monitored items from subscription {Subscription}",
                    toRemoveDetached.Count, this);
            }

            var nowMonitored = new List<OpcUaMonitoredItem>();
            var toAddList = desiredState.Except(currentState).ToList();
            var invalidItems = 0;
            count = 0;
            if (toAddList.Count > 0)
            {
                // Add new monitored items not in current state
                foreach (var toAdd in toAddList)
                {
                    // Create monitored item
                    try
                    {
                        toAdd.Create(session, sessionHandle.Codec);
                        if (toAdd.Item == null)
                        {
                            _logger.LogError("Failed to create new monitored item '{Item}' to " +
                                "add to subscription {Subscription}.", toAdd.Template.StartNodeId);
                            continue;
                        }
                        if (toAdd.EventTemplate != null)
                        {
                            toAdd.Item.AttributeId = Attributes.EventNotifier;
                        }
                        nowMonitored.Add(toAdd);
                        count++;
                        _logger.LogDebug(
                            "Adding new monitored item '{Item}' to subscription {Subscription}...",
                            toAdd.Item.StartNodeId, this);
                    }
                    catch (Exception sre)
                    {
                        _logger.LogWarning("Failed to process new monitored item '{Item}' to be " +
                            "added to subscription {Subscription} due to '{Message}'.",
                            toAdd.Template.StartNodeId, this, sre.Message);
                    }
                }

                var validItems = toAddList.Where(t => t?.Item != null).Select(t => t.Item).ToList();
                invalidItems = toAddList.Count - validItems.Count;
                rawSubscription.AddItems(validItems);

                applyChanges = true;
                metadataChanged = true;

                _logger.LogInformation(
                    "Trying to add {Count} of total {Total} monitored items to subscription {Subscription}...",
                    validItems.Count, toAddList.Count, this);
            }

            // Update monitored items that have changed
            var desiredUpdates = desiredState.Intersect(currentState)
                .ToDictionary(k => k, v => v);
            foreach (var toUpdate in currentState.Intersect(desiredState))
            {
                if (toUpdate.MergeWith(session.MessageContext, session.NodeCache, session.TypeTree,
                    sessionHandle.Codec, desiredUpdates[toUpdate], out var metadata))
                {
                    _logger.LogDebug(
                        "Trying to update monitored item '{Item}' in subscription {Subscription}...",
                        toUpdate, this);
                    count++;
                }
                if (metadata)
                {
                    metadataChanged = true;
                }
                nowMonitored.Add(toUpdate);
            }
            if (count > 0)
            {
                applyChanges = true;
                _logger.LogInformation(
                    "Trying to add or update {Count} monitored items in subscription {Subscription}...",
                    count, this);
            }

            var currentlyMonitored = _currentlyMonitored;
            if (applyChanges)
            {
                await rawSubscription.ApplyChangesAsync(ct).ConfigureAwait(false);

                _currentlyMonitored = currentlyMonitored = nowMonitored
                    .Where(m => m.Item != null)
                    .ToImmutableDictionary(m => m.Item!.ClientHandle, m => m);

                // sanity check
                foreach (var monitoredItem in currentlyMonitored.Values)
                {
                    if (monitoredItem.Item?.Status.Error != null &&
                        StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode))
                    {
                        _logger.LogWarning("Error monitoring node {Id} due to '{Status}' ({Code:X8}) " +
                            "in subscription {Subscription}.", monitoredItem.Item.StartNodeId,
                            monitoredItem.Item.Status.Error.StatusCode,
                            monitoredItem.Item.Status.Error.StatusCode.Code, this);
                        monitoredItem.Template = monitoredItem.Template with
                        {
                            MonitoringMode = MonitoringMode.Disabled
                        };
                        noErrorFound = false;
                    }
                }

                count = currentlyMonitored.Values.Count(m => m.Item!.Status.Error == null);
                _logger.LogInformation("Now monitoring {Count} nodes in subscription {Subscription}.",
                    count, this);
                if (currentlyMonitored.Count != rawSubscription.MonitoredItemCount)
                {
                    _logger.LogError("Monitored items mismatch: Monitoring: {Existing} != " +
                        "In Subscription {Subscription}: {Items}.",
                        currentlyMonitored.Count, this, rawSubscription.MonitoredItemCount);
                }
            }
            else
            {
                // do a sanity check
                foreach (var monitoredItem in currentlyMonitored.Values)
                {
                    if (monitoredItem.Item?.Status.MonitoringMode == Opc.Ua.MonitoringMode.Disabled ||
                        (monitoredItem.Item?.Status.Error != null &&
                        StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode)))
                    {
                        monitoredItem.Template = monitoredItem.Template with
                        {
                            MonitoringMode = MonitoringMode.Disabled
                        };
                        noErrorFound = false;
                        applyChanges = true;
                    }
                }
                if (applyChanges)
                {
                    await rawSubscription.ApplyChangesAsync(ct).ConfigureAwait(false);
                }
            }

            UpdateState(invalidItems);

            if (metadataChanged && _subscription?.Configuration?.MetaData != null)
            {
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

                var typeSystem = await sessionHandle.GetComplexTypeSystemAsync().ConfigureAwait(false);
                var dataTypes = new NodeIdDictionary<DataTypeDescription>();
                var fields = new FieldMetaDataCollection();
                foreach (var monitoredItem in currentlyMonitored.Values)
                {
                    monitoredItem.GetMetaData(session.MessageContext, session.NodeCache,
                        session.TypeTree, typeSystem, fields, dataTypes);
                }

                _currentMetaData = new DataSetMetaDataType
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

            if (currentlyMonitored.Count > 0)
            {
                // Change monitoring mode of all valid items if needed
                var validItems = currentlyMonitored.Values.Where(v => v.Item?.Created ?? false);
                foreach (var change in validItems.GroupBy(i => i.GetMonitoringModeChange()))
                {
                    if (change.Key == null)
                    {
                        continue;
                    }
                    var changeList = change.ToList();
                    _logger.LogInformation(
                        "Set monitoring to {Value} for {Count} items in subscription {Subscription}.",
                        change.Key.Value, changeList.Count, this);

                    var itemsToChange = changeList.ConvertAll(t => t.Item!);
                    var results = rawSubscription.SetMonitoringMode(change.Key.Value, itemsToChange);
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
                                    changeList[i].Template = changeList[i].Template with
                                    {
                                        MonitoringMode = MonitoringMode.Disabled
                                    };
                                    changeList[i].Item!.MonitoringMode = Opc.Ua.MonitoringMode.Disabled;
                                }
                            }
                            noErrorFound = false;
                        }
                    }
                    if (change.Any(x => x.EventTemplate != null))
                    {
                        _logger.LogInformation(
                            "Now issuing ConditionRefresh for item {Item} on subscription {Subscription}",
                            change.FirstOrDefault()?.Item?.DisplayName ?? "", this);
                        try
                        {
                            rawSubscription.ConditionRefresh();
                        }
                        catch (ServiceResultException e)
                        {
                            _logger.LogInformation("ConditionRefresh for item {Item} on subscription " +
                                "{Subscription} failed with a ServiceResultException '{Message}'",
                                change.FirstOrDefault()?.Item?.DisplayName ?? "", this, e.Message);
                            noErrorFound = false;
                        }
                        catch (Exception e)
                        {
                            _logger.LogInformation("ConditionRefresh for item {Item} on subscription " +
                                "{Subscription} failed with an exception '{Message}'",
                                change.FirstOrDefault()?.Item?.DisplayName ?? "", this, e.Message);
                            noErrorFound = false;
                        }
                        if (noErrorFound)
                        {
                            _logger.LogInformation(
                                "ConditionRefresh for item {Item} on subscription {Subscription} has completed",
                                change.FirstOrDefault()?.Item?.DisplayName ?? "", this);
                        }
                    }
                }
                foreach (var item in validItems)
                {
                    Debug.Assert(item.Item != null);
                    if (item.Item.SamplingInterval != item.Item.Status.SamplingInterval ||
                        item.Item.QueueSize != item.Item.Status.QueueSize)
                    {
                        _logger.LogInformation(@"Server has revised '{Item}' in subscription {Subscription}
The item's actual/desired states:
SamplingInterval {CurrentSamplingInterval}/{SamplingInterval},
QueueSize {CurrentQueueSize}/{QueueSize}",
                            item.Item.StartNodeId, this,
                            item.Item.Status.SamplingInterval,
                            item.Item.SamplingInterval, item.Item.Status.QueueSize, item.Item.QueueSize);
                    }
                }
            }
            return noErrorFound;

            void UpdateState(int invalidItems)
            {
                var monitored = _currentlyMonitored.Count;
                var created = _currentlyMonitored.Values.Count(x => StatusCode.IsGood(x.Status));

                NumberOfNotCreatedItems = invalidItems + (monitored - created);
                NumberOfCreatedItems = created;

                // Set up subscription management trigger
                if (monitored == 0 && invalidItems == 0)
                {
                    // Nothing monitored, turn off timer
                    TriggerSubscriptionManagementCallbackIn(Timeout.InfiniteTimeSpan);
                }
                else if (invalidItems != 0)
                {
                    // Retry applying invalid items every 5 minutes
                    TriggerSubscriptionManagementCallbackIn(
                        _options.Value.InvalidMonitoredItemRetryDelay, TimeSpan.FromMinutes(5));
                }
                else if (monitored != created)
                {
                    // Try to periodically update the subscription
                    // TODO: Trigger on address space model changes...

                    TriggerSubscriptionManagementCallbackIn(
                        _options.Value.BadMonitoredItemRetryDelay, TimeSpan.FromMinutes(30));
                }
                else
                {
                    TriggerSubscriptionManagementCallbackIn(
                        _options.Value.SubscriptionManagementInterval, Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// Helper to calculate greatest common divisor for the parameter of keep alive
        /// count used to allow the trigger of heart beats in a given interval.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private static uint GreatCommonDivisor(uint a, uint b)
        {
            return b == 0 ? a : GreatCommonDivisor(b, a % b);
        }

        /// <summary>
        /// Resets the operation timeout on the session accrding to the publishing intervals on all subscriptions
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
                localMaxOperationTimeout =
                    (int)subscription.CurrentPublishingInterval * (int)subscription.CurrentKeepAliveCount;
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
        private async ValueTask ReapplyToSessionInternalAsync(IOpcUaSession handle,
            CancellationToken ct)
        {
            // Should not happen since we are called under lock.
            Debug.Assert(_subscription != null, "No subscription during apply");

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

            if (!subscription.PublishingEnabled)
            {
                // Initialized subscription, resolve display names first
                ResolveDisplayNames(session);
            }

            if (_subscription.MonitoredItems != null)
            {
                // Set the monitored items in the subscription
                await SetMonitoredItemsAsync(handle, subscription,
                    _subscription.MonitoredItems, ct).ConfigureAwait(false);
            }

            if (!subscription.PublishingEnabled || subscription.ChangesPending)
            {
                _logger.LogDebug(
                    "Enabling subscription {Subscription} in session {Session}...",
                    this, handle);

                subscription.SetPublishingMode(true);
                await subscription.ApplyChangesAsync(ct).ConfigureAwait(false);

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

            var configuredPublishingInterval = (int)(_subscription?.Configuration?.PublishingInterval)
                .GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
            var normedPublishingInterval =
                (uint)(configuredPublishingInterval > 0 ? configuredPublishingInterval : 1);

            // calculate the KeepAliveCount no matter what, perhaps monitored items were changed
            var revisedKeepAliveCount = (_subscription?.Configuration?.KeepAliveCount) ?? 10;

            _subscription?.MonitoredItems?.ForEach(m =>
            {
                if (m is DataMonitoredItemModel dataItem)
                {
                    var heartbeat = (uint)(dataItem?.HeartbeatInterval)
                        .GetValueOrDefault(TimeSpan.Zero).TotalMilliseconds;
                    if (heartbeat != 0)
                    {
                        var itemKeepAliveCount = heartbeat / normedPublishingInterval;
                        revisedKeepAliveCount = GreatCommonDivisor(
                            revisedKeepAliveCount, itemKeepAliveCount);
                    }
                }
            });

            var configuredMaxNotificationsPerPublish =
                session.DefaultSubscription.MaxNotificationsPerPublish;
            var configuredLifetimeCount = (_subscription?.Configuration?.LifetimeCount)
                ?? session.DefaultSubscription.LifetimeCount;

            var configuredPriority = (_subscription?.Configuration?.Priority)
                ?? session.DefaultSubscription.Priority;

            var subscription = session.Subscriptions.SingleOrDefault(s => s.Handle == this);
            if (subscription == null)
            {
                subscription = new Subscription(session.DefaultSubscription)
                {
                    Handle = this,
                    DisplayName = Name,
                    PublishingEnabled = false, // false on initialization
                    KeepAliveCount = revisedKeepAliveCount,
                    PublishingInterval = configuredPublishingInterval,
                    MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish,
                    LifetimeCount = configuredLifetimeCount,
                    Priority = configuredPriority,
                    SequentialPublishing = true, // TODO: Make configurable
                    DisableMonitoredItemCache = false,
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

                await subscription.CreateAsync(ct).ConfigureAwait(false);

                if (!subscription.Created)
                {
                    throw new ServiceResultException(StatusCodes.BadSubscriptionIdInvalid,
                        $"Failed to create subscription {this} in session {session}");
                }

                LogRevisedValues(subscription, true);
                Debug.Assert(subscription.Id != 0);
            }
            else
            {
                // Apply new configuration on configuration on original subscription
                var modifySubscription = false;

                if (revisedKeepAliveCount != subscription.KeepAliveCount)
                {
                    _logger.LogInformation(
                        "Change KeepAliveCount to {New} in Subscription {Subscription}...",
                        revisedKeepAliveCount, this);

                    subscription.KeepAliveCount = revisedKeepAliveCount;
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
            var onSubscriptionEventChange = OnSubscriptionEventChange;
            if (onSubscriptionEventChange == null)
            {
                return;
            }

            if (subscription == null)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} is null", this);
                return;
            }

            if (notification?.Events == null)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} has empty notification.",
                    this);
                return;
            }

            if (notification.Events.Count == 0)
            {
                _logger.LogWarning(
                    "EventChange for subscription {Subscription} has no events.",
                    this);
                return;
            }
            try
            {
                // check if notification is a keep alive
                var isKeepAlive = notification.Events[0].ClientHandle == 0
                    && notification.Events[0].Message?.NotificationData?.Count == 0;
                if (isKeepAlive)
                {
                    var sequenceNumber = notification.Events[0].Message.SequenceNumber;
                    var publishTime = notification.Events[0].Message.PublishTime;

                    // in case of a keepalive,the sequence number is not incremented by the servers
                    _logger.LogInformation("Keep alive for subscription {Subscription} " +
                        "with sequenceNumber {SequenceNumber}, publishTime {PublishTime} on Event callback.",
                        this, sequenceNumber, publishTime);

                    var message = new SubscriptionNotificationModel
                    {
                        ServiceMessageContext = subscription.Session?.MessageContext,
                        ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                        SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                        SubscriptionName = Name,
                        Timestamp = publishTime,
                        SubscriptionId = Id,
                        MessageType = Encoders.PubSub.MessageType.KeepAlive,
                        MetaData = _currentMetaData
                    };
                    onSubscriptionEventChange.Invoke(this, message);
                }
                else
                {
                    var numOfEvents = 0;
                    var missingSequenceNumbers = Array.Empty<uint>();
                    for (var i = 0; i < notification.Events.Count; i++)
                    {
                        var eventNotification = notification.Events[i];
                        Debug.Assert(eventNotification != null);

                        var monitoredItem = subscription.FindItemByClientHandle(eventNotification.ClientHandle);
                        var sequenceNumber = eventNotification.Message.SequenceNumber;

                        if (sequenceNumber == 1)
                        {
                            // Do not log when the sequence number is 1 after reconnect
                            _lastSequenceNumber = 1;
                        }
                        else if (i == 0 && !SequenceNumber.Validate(sequenceNumber, ref _lastSequenceNumber,
                            out missingSequenceNumbers, out var dropped))
                        {
                            _logger.LogWarning("Event for monitored item {ClientHandle} subscription " +
                                "{Subscription} has unexpected sequenceNumber {SequenceNumber} missing " +
                                "{ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                                eventNotification.ClientHandle, this, sequenceNumber,
                                SequenceNumber.ToString(missingSequenceNumbers), dropped ?
                                "dropped" : "already received", eventNotification.Message.PublishTime);
                        }

                        _logger.LogTrace("Event for subscription: {Subscription}, sequence#: " +
                            "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                            subscription.DisplayName, sequenceNumber, isKeepAlive,
                            eventNotification.Message.PublishTime);

                        if (monitoredItem?.Handle is OpcUaMonitoredItem wrapper)
                        {
                            var message = new SubscriptionNotificationModel
                            {
                                ServiceMessageContext = subscription.Session?.MessageContext,
                                ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                                EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                                SubscriptionName = Name,
                                SubscriptionId = Id,
                                SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                                MessageType = Encoders.PubSub.MessageType.Event,
                                MetaData = _currentMetaData,
                                Timestamp = eventNotification.Message.PublishTime,
                                Notifications = new List<MonitoredItemNotificationModel>()
                            };

                            wrapper.ProcessEventNotification(message, eventNotification);

                            if (message.Notifications.Count > 0)
                            {
                                onSubscriptionEventChange.Invoke(this, message);
                                numOfEvents++;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Monitored item not found with client handle {ClientHandle} " +
                                "for Event received for subscription {Subscription} + " +
                                "{SequenceNumber}, publishTime {PublishTime}",
                                eventNotification.ClientHandle, this,
                                sequenceNumber, eventNotification.Message.PublishTime);
                        }
                    }
                    var onSubscriptionEventDiagnosticsChange = OnSubscriptionEventDiagnosticsChange;
                    onSubscriptionEventDiagnosticsChange?.Invoke(this, numOfEvents);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception processing subscription notification");
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
            var onSubscriptionDataChange = OnSubscriptionDataChange;
            if (onSubscriptionDataChange == null)
            {
                return;
            }

            if (subscription == null)
            {
                _logger.LogWarning(
                    "DataChange for subscription {Subscription} is null", this);
                return;
            }

            if (notification?.MonitoredItems == null || notification.MonitoredItems.Count == 0)
            {
                _logger.LogWarning(
                    "DataChange for subscription {Subscription} has empty notification", this);
                return;
            }
            try
            {
                // check if notification is a keep alive
                var isKeepAlive = notification.MonitoredItems.Count == 1
                    && notification.MonitoredItems[0].ClientHandle == 0
                    && notification.MonitoredItems[0].Message?.NotificationData?.Count == 0;

                SubscriptionNotificationModel message;
                if (isKeepAlive)
                {
                    var sequenceNumber = notification.MonitoredItems[0].Message.SequenceNumber;
                    var publishTime = notification.MonitoredItems[0].Message.PublishTime;

                    // in case of a keepalive,the sequence number is not incremented by the servers
                    _logger.LogInformation("Keep alive for subscription {Subscription} " +
                        "with sequenceNumber {SequenceNumber}, publishTime {PublishTime} on data change.",
                        this, sequenceNumber, publishTime);

                    message = new SubscriptionNotificationModel
                    {
                        ServiceMessageContext = subscription.Session?.MessageContext,
                        ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                        SubscriptionName = Name,
                        Timestamp = publishTime,
                        SubscriptionId = Id,
                        SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                        MessageType = Encoders.PubSub.MessageType.KeepAlive,
                        MetaData = _currentMetaData,
                        Notifications = new List<MonitoredItemNotificationModel>()
                    };
                }
                else
                {
                    message = new SubscriptionNotificationModel
                    {
                        ServiceMessageContext = subscription.Session?.MessageContext,
                        ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                        SubscriptionName = Name,
                        SubscriptionId = Id,
                        MessageType = Encoders.PubSub.MessageType.DeltaFrame,
                        MetaData = _currentMetaData,
                        SequenceNumber = SequenceNumber.Increment32(ref _sequenceNumber),
                        Notifications = new List<MonitoredItemNotificationModel>()
                    };
                    var missingSequenceNumbers = Array.Empty<uint>();
                    for (var i = 0; i < notification.MonitoredItems.Count; i++)
                    {
                        Debug.Assert(notification?.MonitoredItems != null);
                        var item = notification.MonitoredItems[i];
                        Debug.Assert(item != null);

                        var monitoredItem = subscription.FindItemByClientHandle(item.ClientHandle);
                        var sequenceNumber = item.Message.SequenceNumber;
                        message.Timestamp = item.Message.PublishTime;

                        // All notifications have the same message and thus sequence number
                        if (sequenceNumber == 1)
                        {
                            // Do not log when the sequence number is 1 after reconnect
                            _lastSequenceNumber = 1;
                        }
                        else if (i == 0 && !SequenceNumber.Validate(sequenceNumber, ref _lastSequenceNumber,
                            out missingSequenceNumbers, out var dropped))
                        {
                            _logger.LogWarning("DataChange for monitored item {ClientHandle} subscription " +
                                "{Subscription} has unexpected sequenceNumber {SequenceNumber} " +
                                "missing {ExpectedSequenceNumber} which were {Dropped}, publishTime {PublishTime}",
                                item.ClientHandle, this, sequenceNumber,
                                SequenceNumber.ToString(missingSequenceNumbers),
                                dropped ? "dropped" : "already received", item.Message.PublishTime);
                        }

                        _logger.LogTrace("Data change for subscription: {Subscription}, sequence#: " +
                            "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                            subscription.DisplayName, sequenceNumber, isKeepAlive, message.Timestamp);

                        if (monitoredItem?.Handle is OpcUaMonitoredItem wrapper)
                        {
                            wrapper.ProcessMonitoredItemNotification(message, item);
                        }
                        else
                        {
                            _logger.LogWarning("Monitored item not found with client handle {ClientHandle} " +
                                "for DataChange received for subscription {Subscription} + " +
                                "{SequenceNumber}, publishTime {PublishTime}",
                                item.ClientHandle, this,
                                sequenceNumber, message.Timestamp);
                        }
                    }
                }

                // add a heartbeat for monitored items that did not receive a datachange notification
                var currentlyMonitored = _currentlyMonitored.ToBuilder();
                currentlyMonitored.RemoveRange(notification.MonitoredItems.Select(m => m.ClientHandle));
                foreach (var wrapper in currentlyMonitored.Values)
                {
                    wrapper.ProcessMonitoredItemNotification(message, null);
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var erroneousNotifications = message.Notifications
                        .Where(n => n.Value?.Value == null
                            || StatusCode.IsNotGood(n.Value.StatusCode))
                        .ToList();

                    if (erroneousNotifications.Count > 0)
                    {
                        _logger.LogDebug(
                            "Found {Count} notifications with null value or not good status " +
                            "code for subscription {Subscription}.",
                            erroneousNotifications.Count, this);
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
                () => new Measurement<int>(_online ? 1 : 0, _metrics.TagList),
                "", "OPC UA connection success flag.");
        }

        private static readonly TimeSpan kDefaultErrorRetryDelay = TimeSpan.FromSeconds(2);
        private ImmutableDictionary<uint, OpcUaMonitoredItem> _currentlyMonitored;
        private SubscriptionModel? _subscription;
        private DataSetMetaDataType? _currentMetaData;
        private bool _online;
        private long _connectionAttempts;
        private uint _lastSequenceNumber;
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
    }
}
