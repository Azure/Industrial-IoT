// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using Prometheus;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class SubscriptionServices {

        /// <summary>
        /// Subscription implementation
        /// </summary>
        internal sealed class SubscriptionWrapper : ISubscription {

            /// <inheritdoc/>
            public string Name => _subscription.Id;

            /// <inheritdoc/>
            public ushort Id { get; }

            /// <inheritdoc/>
            public bool Enabled { get; private set; }

            /// <inheritdoc/>
            public bool Active { get; private set; }

            /// <inheritdoc/>
            public int NumberOfConnectionRetries =>
                _outer._sessionManager.GetNumberOfConnectionRetries(_subscription.Connection);

            /// <inheritdoc/>
            public bool IsConnectionOk =>
                _outer._sessionManager.IsConnectionOk(_subscription.Connection);

            /// <inheritdoc/>
            public int NumberOfGoodNodes {
                get {
                    return IsConnectionOk ?
                        _currentlyMonitored.Values
                            .Count(x => x.Item.Status.MonitoringMode == Opc.Ua.MonitoringMode.Reporting) : 0;
                }
            }

            /// <inheritdoc/>
            public int NumberOfBadNodes {
                get {
                    return IsConnectionOk ?
                        _currentlyMonitored.Values
                            .Count(x => x.Item.Status.MonitoringMode != Opc.Ua.MonitoringMode.Reporting) : 0;
                }
            }

            /// <inheritdoc/>
            public ConnectionModel Connection => _subscription.Connection;

            /// <inheritdoc/>
            public event EventHandler<SubscriptionNotificationModel> OnSubscriptionDataChange;

            /// <inheritdoc/>
            public event EventHandler<SubscriptionNotificationModel> OnSubscriptionEventChange;

            /// <inheritdoc/>
            public event EventHandler<int> OnSubscriptionDataDiagnosticsChange;

            ///  <inheritdoc/>
            public event EventHandler<int> OnSubscriptionEventDiagnosticsChange;

            /// <summary>
            /// Subscription wrapper
            /// </summary>
            public SubscriptionWrapper(SubscriptionServices outer,
                SubscriptionModel subscription, ILogger logger) {
                _subscription = subscription.Clone() ??
                    throw new ArgumentNullException(nameof(subscription));
                _outer = outer ??
                    throw new ArgumentNullException(nameof(outer));
                _logger = logger?.ForContext<SubscriptionWrapper>() ??
                    throw new ArgumentNullException(nameof(logger));
                _lock = new SemaphoreSlim(1, 1);
                _currentlyMonitored = ImmutableDictionary<uint, MonitoredItemWrapper>.Empty;
                Id = SequenceNumber.Increment16(ref _lastIndex);
            }

            /// <inheritdoc/>
            public void OnSubscriptionStateChanged(bool online) {
                foreach (var monitoredItem in _currentlyMonitored.Values) {
                    monitoredItem.OnMonitoredItemStateChanged(online);
                }
            }

            /// <inheritdoc/>
            public async Task CloseAsync() {
                await _lock.WaitAsync().ConfigureAwait(false);
                try {
                    if (_closed) {
                        return;
                    }
                    _closed = true;
                    _outer._sessionManager.UnregisterSubscription(this);
                }
                finally {
                    _lock.Release();
                }
                try {
                    var session = _outer._sessionManager.GetOrCreateSession(_subscription.Connection, false);
                    if (session != null) {
                        var subscription = session.Subscriptions.SingleOrDefault(s => s.Handle == this);
                        if (subscription != null) {
                            _logger.Information("Closing subscription '{subscription}' in session '{sessionId}'...",
                                Name,
                                Connection.CreateConnectionId());
                            Try.Op(() => subscription.PublishingEnabled = false);
                            Try.Op(() => subscription.ApplyChanges());
                            Try.Op(() => subscription.DeleteItems());
                            _logger.Debug("Deleted monitored items for '{subscription}'/'{sessionId}'",
                                Name,
                                Connection.CreateConnectionId());
                            Try.Op(() => session.RemoveSubscription(subscription));
                            _logger.Information("Subscription '{subscription}' in session '{sessionId}' successfully closed (Remaining: {Remaining}).",
                                Name,
                                Connection.CreateConnectionId(), session.Subscriptions.Count());
                        }
                        else {
                            _logger.Warning("Failed to close subscription '{subscription}'. Subscription was not found in session '{sessionId}'.",
                                Name,
                                Connection.CreateConnectionId());
                        }
                    }
                    else {
                        _logger.Warning("Failed to close subscription '{subscription}'. The attached session '{sessionId}' could not be found.",
                                Name,
                                Connection.CreateConnectionId());
                    }
                }
                catch (Exception e) {
                    _logger.Error(e, "Failed to close subscription '{subscription}'/'{sessionId}'",
                        Name,
                        Connection.CreateConnectionId());
                }
            }

            /// <inheritdoc/>
            public void Dispose() {
                if (!_closed) {
                    Try.Async(CloseAsync).Wait();
                }
                _lock.Dispose();
            }

            /// <inheritdoc/>
            public bool TryUpgradeToKeyFrame(SubscriptionNotificationModel notification) {
                _lock.Wait();
                try {
                    var subscription = GetSubscription(null, null, false);
                    if (subscription == null) {
                        return false;
                    }
                    var allNotifications = subscription.MonitoredItems
                            .SelectMany(m => m.LastValue.ToMonitoredItemNotifications(m))
                            .ToList();
                    notification.MetaData = _currentMetaData;
                    notification.MessageType = Opc.Ua.PubSub.MessageType.KeyFrame;
                    notification.Notifications = allNotifications;
                    return true;
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to get a keyframe from subscription monitored item cache.");
                    return false;
                }
                finally {
                    _lock.Release();
                }
            }

            /// <inheritdoc/>
            public SubscriptionNotificationModel CreateKeepAlive() {
                _lock.Wait();
                try {
                    var subscription = GetSubscription(null, null, false);
                    if (subscription == null) {
                        return null;
                    }
                    var message = new SubscriptionNotificationModel {
                        ServiceMessageContext = subscription.Session.MessageContext,
                        ApplicationUri = subscription.Session.Endpoint.Server.ApplicationUri,
                        EndpointUrl = subscription.Session.Endpoint.EndpointUrl,
                        MetaData = _currentMetaData,
                        SubscriptionName = Name,
                        SubscriptionId = Id,
                        MessageType = Opc.Ua.PubSub.MessageType.KeepAlive,
                    };
                    return message;
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to create a subscription notification");
                    return null;
                }
                finally {
                    _lock.Release();
                }
            }

            /// <inheritdoc/>
            public async Task ApplyAsync(IEnumerable<BaseMonitoredItemModel> monitoredItems,
                SubscriptionConfigurationModel configuration) {
                await _lock.WaitAsync().ConfigureAwait(false);
                try {
                    // set the new set of monitored items
                    _subscription.MonitoredItems = monitoredItems?.Select(n => n.Clone()).ToList();

                    // try to get the subscription with the new configuration
                    var session = _outer._sessionManager.GetOrCreateSession(_subscription.Connection, true);
                    var rawSubscription = GetSubscription(session, configuration, Active);
                    if (session == null || rawSubscription == null) {
                        Enabled = false;
                        Active = false;
                    }
                    else {
                        ResolveDisplayNames(session);
                        Active = await SetMonitoredItemsAsync(rawSubscription, _subscription.MonitoredItems, Active)
                            .ConfigureAwait(false) && Active;
                    }
                }
                catch (Exception e) {
                    _logger.Error("Failed to apply monitored items due to {exception}", e.Message);
                    Enabled = false;
                    Active = false;
                }
                finally {
                    _lock.Release();
                    // just give the control to the session manager
                    _outer._sessionManager.RegisterSubscription(this);
                }
            }

            /// <inheritdoc/>
            public async Task EnableAsync(Session session) {
                try {
                    ResolveDisplayNames(session);
                    Active = await ReapplyAsync(session, false).ConfigureAwait(false);
                    Enabled = true;
                }
                catch (Exception e) {
                    _logger.Error(e, "Failed to enable subscription");
                    Enabled = false;
                    Active = false;
                }
            }

            /// <inheritdoc/>
            public async Task ActivateAsync(Session session) {
                try {
                    if (!Enabled) {
                        // force a reactivation
                        Active = false;
                    }
                    if (!Active) {
                        Active = await ReapplyAsync(session, true).ConfigureAwait(false);
                        Enabled = true;
                    }
                }
                catch (Exception e) {
                    _logger.Error(e, "Failed to activate subscription");
                    Enabled = false;
                    Active = false;
                }
            }

            /// <inheritdoc/>
            public async Task DeactivateAsync(Session session) {
                try {
                    Active = await ReapplyAsync(session, false).ConfigureAwait(false);
                }
                catch (Exception e) {
                    _logger.Error(e, "Failed to deactivate subscription");
                    Enabled = false;
                    Active = false;
                }
            }

            /// <summary>
            /// Send condition notification
            /// </summary>
            /// <param name="subscription"></param>
            /// <param name="notifications"></param>
            internal void SendConditionNotification(Subscription subscription,
                IEnumerable<MonitoredItemNotificationModel> notifications) {
                var message = new SubscriptionNotificationModel {
                    ServiceMessageContext = subscription?.Session?.MessageContext,
                    ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                    SubscriptionName = Name,
                    SubscriptionId = Id,
                    MessageType = Opc.Ua.PubSub.MessageType.Condition,
                    MetaData = _currentMetaData,
                    Timestamp = DateTime.UtcNow,
                    Notifications = notifications.ToList()
                };
                OnSubscriptionEventChange?.Invoke(this, message);
            }

            /// <summary>
            /// sanity check of the subscription
            /// </summary>
            /// <returns></returns>
            private async Task<bool> ReapplyAsync(Session session, bool activate) {
                await _lock.WaitAsync().ConfigureAwait(false);
                try {
                    var rawSubscription = GetSubscription(session, null, activate);
                    if (rawSubscription == null) {
                        return false;
                    }
                    return await SetMonitoredItemsAsync(rawSubscription, _subscription.MonitoredItems, activate)
                        .ConfigureAwait(false) && activate;
                }
                finally {
                    _lock.Release();
                }
            }

            /// <summary>
            /// Reads the display name of the nodes to be monitored
            /// </summary>
            private void ResolveDisplayNames(ISession session) {
                if (!(_subscription.Configuration?.ResolveDisplayName ?? false)) {
                    return;
                }

                if (session == null) {
                    return;
                }

                var unresolvedMonitoredItems = _subscription.MonitoredItems
                    .OfType<DataMonitoredItemModel>()
                    .Where(mi => string.IsNullOrEmpty(mi.DisplayName));
                if (!unresolvedMonitoredItems.Any()) {
                    return;
                }

                try {
                    var nodeIds = unresolvedMonitoredItems.
                        Select(n => {
                            try {
                                return n.StartNodeId.ToNodeId(session.MessageContext);
                            }
                            catch (ServiceResultException sre) {
                                _logger.Warning("Failed to resolve display name for '{monitoredItem}' due to '{message}'",
                                    n.StartNodeId, sre.Message);
                            }
                            catch (Exception e) {
                                _logger.Error(e, "Failed to resolve display name for '{monitoredItem}'",
                                    n.StartNodeId);
                                throw;
                            }
                            return null;
                        });
                    if (nodeIds.Any()) {
                        session.ReadDisplayName(nodeIds.ToList(), out var displayNames, out var errors);
                        var index = 0;
                        foreach (var monitoredItem in unresolvedMonitoredItems) {
                            if (StatusCode.IsGood(errors[index].StatusCode)) {
                                monitoredItem.DisplayName = displayNames[index];
                            }
                            else {
                                monitoredItem.DisplayName = null;
                                _logger.Warning("Failed to read display name for '{monitoredItem}' due to '{statusCode}'",
                                    monitoredItem.StartNodeId, errors[index].StatusCode);
                            }
                            index++;
                        }
                    }
                }
                catch (ServiceResultException sre) {
                    _logger.Warning("Failed to resolve display names for monitored items due to '{message}'",
                        sre.Message);
                }
                catch (Exception e) {
                    _logger.Error(e, "Failed to resolve display names for monitored items");
                    throw;
                }
            }

            /// <summary>
            /// Synchronize monitored items and triggering configuration in subscription
            /// </summary>
            private async Task<bool> SetMonitoredItemsAsync(Subscription rawSubscription,
                IEnumerable<BaseMonitoredItemModel> monitoredItems, bool activate) {

                var currentState = rawSubscription.MonitoredItems
                    .Select(m => m.Handle)
                    .OfType<MonitoredItemWrapper>()
                    .ToHashSetSafe();

                var noErrorFound = true;
                var count = 0;
                var codec = _outer._codec.Create(rawSubscription.Session.MessageContext);
                if (monitoredItems == null || !monitoredItems.Any()) {
                    // cleanup entire subscription
                    var toCleanupList = currentState.Select(t => t.Item);
                    if (toCleanupList.Any()) {
                        // Remove monitored items not in desired state
                        _logger.Verbose("Remove monitored items in subscription "
                            + "'{subscription}'/'{sessionId}'...",
                            Name,
                            Connection.CreateConnectionId());
                        foreach (var toRemove in toCleanupList) {
                            _logger.Verbose("Removing monitored item '{item}'...",
                                toRemove.StartNodeId);
                            ((MonitoredItemWrapper)toRemove.Handle).Destroy();
                            count++;
                        }
                        rawSubscription.RemoveItems(toCleanupList);
                        _logger.Information("Removed {count} monitored items in subscription "
                            + "'{subscription}'/'{sessionId}'",
                            count,
                            Name,
                            Connection.CreateConnectionId());
                    }
                    _currentlyMonitored = ImmutableDictionary<uint, MonitoredItemWrapper>.Empty;

                    // TODO: Set metadata to empty here
                    rawSubscription.ApplyChanges();
                    rawSubscription.SetPublishingMode(false);
                    if (rawSubscription.MonitoredItemCount != 0) {
                        _logger.Warning("Failed to remove {count} monitored items from subscription "
                            + "'{subscription}'/'{sessionId}'",
                            rawSubscription.MonitoredItemCount,
                            Name,
                            Connection.CreateConnectionId());
                    }
                    return noErrorFound;
                }

                // Synchronize the desired items with the state of the raw subscription
                var desiredState = monitoredItems
                    .Select(m => new MonitoredItemWrapper(m, _logger))
                    .ToHashSetSafe();

                var metadataChanged = false;
                var applyChanges = false;

                var toRemoveList = currentState.Except(desiredState).Select(t => t.Item);
                if (toRemoveList.Any()) {
                    count = 0;
                    // Remove monitored items not in desired state
                    foreach (var toRemove in toRemoveList) {
                        _logger.Verbose("Removing monitored item '{item}'...", toRemove.StartNodeId);
                        ((MonitoredItemWrapper)toRemove.Handle).Destroy();
                        count++;
                    }
                    rawSubscription.RemoveItems(toRemoveList);
                    applyChanges = true;
                    metadataChanged = true;

                    _logger.Information("Removed {count} monitored items from subscription "
                        + "'{subscription}'/'{sessionId}'",
                        count,
                        Name,
                        Connection.CreateConnectionId());
                }

                // todo re-associate detached handles!?
                var toRemoveDetached = rawSubscription.MonitoredItems.Where(m => m.Status == null);
                if (toRemoveDetached.Any()) {
                    rawSubscription.RemoveItems(toRemoveDetached);
                    _logger.Information("Removed {count} detached monitored items from subscription "
                        + "'{subscription}'/'{sessionId}'",
                        toRemoveDetached.Count(),
                        Name,
                        Connection.CreateConnectionId());
                }

                var nowMonitored = new List<MonitoredItemWrapper>();
                var toAddList = desiredState.Except(currentState);
                if (toAddList.Any()) {
                    count = 0;
                    // Add new monitored items not in current state
                    _logger.Verbose("Add monitored items to subscription '{subscription}'/'{sessionId}'...",
                        Name,
                        Connection.CreateConnectionId());
                    foreach (var toAdd in toAddList) {
                        // Create monitored item
                        if (!activate) {
                            toAdd.Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                        }
                        try {
                            toAdd.Create(rawSubscription.Session, codec, activate);
                            if (toAdd.EventTemplate != null) {
                                toAdd.Item.AttributeId = Attributes.EventNotifier;
                            }
                            nowMonitored.Add(toAdd);
                            count++;
                            _logger.Verbose("Adding new monitored item '{item}'...",
                                toAdd.Item.StartNodeId);
                        }
                        catch (ServiceResultException sre) {
                            _logger.Warning("Failed to add new monitored item '{item}' due to '{message}'",
                                toAdd.Template.StartNodeId, sre.Message);
                        }
                        catch (Exception e) {
                            _logger.Error(e, "Failed to add new monitored item '{item}'",
                                toAdd.Template.StartNodeId);
                            throw;
                        }
                    }
                    rawSubscription.AddItems(
                        toAddList.Where(t => t?.Item != null).Select(t => t.Item).ToList());
                    applyChanges = true;
                    metadataChanged = true;
                    _logger.Information("Added {count} monitored items to subscription "
                        + "'{subscription}'/'{sessionId}'",
                        count,
                        Name,
                        Connection.CreateConnectionId());
                }

                // Update monitored items that have changed
                var desiredUpdates = desiredState.Intersect(currentState)
                    .ToDictionary(k => k, v => v);
                count = 0;
                foreach (var toUpdate in currentState.Intersect(desiredState)) {
                    if (toUpdate.MergeWith(rawSubscription.Session?.MessageContext,
                            rawSubscription.Session?.NodeCache, rawSubscription.Session?.TypeTree,
                            codec, desiredUpdates[toUpdate], out var metadata)) {
                        _logger.Verbose("Updating monitored item '{item}'...", toUpdate);
                        count++;
                    }
                    if (metadata) {
                        metadataChanged = true;
                    }
                    nowMonitored.Add(toUpdate);
                }
                if (count > 0) {
                    applyChanges = true;
                    _logger.Information("Updated {count} monitored items in subscription "
                        + "'{subscription}'/'{sessionId}'",
                        count,
                        Name,
                        Connection.CreateConnectionId());
                }

                var currentlyMonitored = _currentlyMonitored;
                if (applyChanges) {

                    rawSubscription.ApplyChanges();
                    _currentlyMonitored = currentlyMonitored
                        = nowMonitored.ToImmutableDictionary(m => m.Item.ClientHandle, m => m);

                    var map = currentlyMonitored.Values.ToDictionary(k => k.Template.StartNodeId, v => v);
                    foreach (var item in currentlyMonitored.Values) {
                        if (item.Template.TriggerId != null &&
                            map.TryGetValue(item.Template.TriggerId, out var trigger)) {
                            trigger?.AddTriggerLink(item.ServerId.GetValueOrDefault());
                        }
                    }

                    // Set up any new trigger configuration if needed
                    foreach (var item in currentlyMonitored.Values) {
                        if (item.GetTriggeringLinks(out var added, out var removed)) {
                            var response = await rawSubscription.Session.SetTriggeringAsync(
                                null, rawSubscription.Id, item.ServerId.GetValueOrDefault(),
                                new UInt32Collection(added), new UInt32Collection(removed), CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                    }

                    // sanity check
                    foreach (var monitoredItem in currentlyMonitored.Values) {
                        if (monitoredItem.Item.Status.Error != null &&
                            StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode)) {
                            _logger.Warning("Error monitoring node {id} due to {code} in subscription "
                                + "'{subscription}'/'{sessionId}'", monitoredItem.Item.StartNodeId,
                                monitoredItem.Item.Status.Error.StatusCode,
                                Name,
                                Connection.CreateConnectionId());
                            monitoredItem.Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                            noErrorFound = false;
                        }
                    }

                    count = currentlyMonitored.Values.Count(m => m.Item.Status.Error == null);
                    kMonitoredItems.WithLabels(rawSubscription.Id.ToString()).Set(count);

                    _logger.Information("Now monitoring {count} nodes in subscription "
                        + "'{subscription}'/'{sessionId}'",
                        count,
                        Name,
                        Connection.CreateConnectionId());

                    if (currentlyMonitored.Count != rawSubscription.MonitoredItemCount) {
                        _logger.Error("Monitored items mismatch: wrappers: {wrappers} != items: {items} ",
                            currentlyMonitored.Count, rawSubscription.MonitoredItemCount);
                    }
                }
                else {
                    // do a sanity check
                    foreach (var monitoredItem in currentlyMonitored.Values) {
                        if (monitoredItem.Item.Status.MonitoringMode == Opc.Ua.MonitoringMode.Disabled ||
                            (monitoredItem.Item.Status.Error != null &&
                            StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode))) {

                            monitoredItem.Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                            noErrorFound = false;
                            applyChanges = true;
                        }
                    }
                    if (applyChanges) {
                        rawSubscription.ApplyChanges();
                    }
                }

                if (metadataChanged && _subscription.Configuration?.MetaData != null) {
                    //
                    // Use the date time to version across reboots. This could be done more elegantly by
                    // saving the last version to persistent storage such as twin, but this is ok for
                    // the sake of being able to have an incremental version number defining metadata changes.
                    //
                    var metaDataVersion = DateTime.UtcNow.ToBinary();
                    var major = (uint)(metaDataVersion >> 32);
                    var minor = (uint)metaDataVersion;

                    _logger.Information("Metadata changed to {major}.{minor} for subscription"
                        + "'{subscription}'/'{sessionId}'",
                        major, minor, Name, Connection.CreateConnectionId());
                    var dataTypes = new NodeIdDictionary<DataTypeDescription>();
                    var fields = new FieldMetaDataCollection();
                    foreach (var monitoredItem in currentlyMonitored.Values) {
                        monitoredItem.GetMetaData(
                            rawSubscription.Session?.MessageContext, rawSubscription.Session?.NodeCache,
                            rawSubscription.Session?.TypeTree,
                            _outer._sessionManager.GetComplexTypeSystem(rawSubscription.Session),
                            fields, dataTypes);
                    }

                    _currentMetaData = new DataSetMetaDataType {
                        Name = _subscription.Configuration.MetaData.Name,
                        DataSetClassId = (Uuid)_subscription.Configuration.MetaData.DataSetClassId,
                        Namespaces = rawSubscription.Session?.NamespaceUris.ToArray(),
                        EnumDataTypes = dataTypes.Values.OfType<EnumDescription>().ToArray(),
                        StructureDataTypes = dataTypes.Values.OfType<StructureDescription>().ToArray(),
                        SimpleDataTypes = dataTypes.Values.OfType<SimpleTypeDescription>().ToArray(),
                        Fields = fields,
                        Description = _subscription.Configuration.MetaData.Description,
                        ConfigurationVersion = new ConfigurationVersionDataType {
                            MajorVersion = major,
                            MinorVersion = minor
                        }
                    };
                }

                if (activate && currentlyMonitored.Count > 0) {
                    // Change monitoring mode of all valid items if needed
                    var validItems = currentlyMonitored.Values.Where(v => v.Item.Created);
                    foreach (var change in validItems.GroupBy(i => i.GetMonitoringModeChange())) {
                        if (change.Key == null) {
                            continue;
                        }
                        var changeList = change.ToList();
                        _logger.Information("Set monitoring to {value} for {count} items in subscription "
                            + "'{subscription}'/'{sessionId}'.",
                            change.Key.Value,
                            change.Count(),
                            Name,
                            Connection.CreateConnectionId());

                        var itemsToChange = changeList.Select(t => t.Item).ToList();
                        var results = rawSubscription.SetMonitoringMode(change.Key.Value, itemsToChange);
                        if (results != null) {
                            var erroneousResultsCount = results
                                .Count(r => (r == null) ? false : StatusCode.IsNotGood(r.StatusCode));

                            // Check the number of erroneous results and log.
                            if (erroneousResultsCount > 0) {
                                _logger.Warning("Failed to set monitoring for {count} items in subscription "
                                    + "'{subscription}'/'{sessionId}'.",
                                    erroneousResultsCount,
                                    Name,
                                    Connection.CreateConnectionId());

                                for (int i = 0; i < results.Count && i < itemsToChange.Count; ++i) {
                                    if (StatusCode.IsNotGood(results[i].StatusCode)) {
                                        _logger.Warning("Set monitoring for item '{item}' in subscription "
                                            + "'{subscription}'/'{sessionId}' failed with '{status}'.",
                                            itemsToChange[i].StartNodeId,
                                            Name,
                                            Connection.CreateConnectionId(),
                                            results[i].StatusCode);
                                        changeList[i].Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                                        changeList[i].Item.MonitoringMode = Opc.Ua.MonitoringMode.Disabled;
                                    }
                                }
                                noErrorFound = false;
                            }
                        }
                        if (change.Where(x => x.EventTemplate != null).Any()) {
                            _logger.Information("Now issuing ConditionRefresh for item {item} on subscription " +
                                "{subscription}", change.FirstOrDefault()?.Item?.DisplayName ?? "", rawSubscription.DisplayName);
                            try {
                                rawSubscription.ConditionRefresh();
                            }
                            catch (ServiceResultException e) {
                                _logger.Information("ConditionRefresh for item {item} on subscription " +
                                    "{subscription} failed with a ServiceResultException '{message}'",
                                    change.FirstOrDefault()?.Item?.DisplayName ?? "", rawSubscription.DisplayName, e.Message);
                                noErrorFound = false;
                            }
                            catch (Exception e) {
                                _logger.Information("ConditionRefresh for item {item} on subscription " +
                                    "{subscription} failed with an exception '{message}'",
                                    change.FirstOrDefault()?.Item?.DisplayName ?? "", rawSubscription.DisplayName, e.Message);
                                noErrorFound = false;

                            }
                            if (noErrorFound) {
                                _logger.Information("ConditionRefresh for item {item} on subscription " +
                                    "{subscription} has completed",
                                    change.FirstOrDefault()?.Item?.DisplayName ?? "", rawSubscription.DisplayName);
                            }
                        }
                    }
                    foreach (var item in validItems) {
                        if (item.Item.SamplingInterval != item.Item.Status.SamplingInterval ||
                            item.Item.QueueSize != item.Item.Status.QueueSize) {

                            var diagInfo = new StringBuilder();
                            diagInfo.Append("Revised monitored item '{item}' in '{subscription}" +
                                "'/'{sessionId}' has actual/desired states: ");
                            diagInfo.Append("SamplingInterval {currentSamplingInterval}/{samplingInterval}, ");
                            diagInfo.Append("QueueSize {currentQueueSize}/{queueSize}");

                            _logger.Warning(diagInfo.ToString(),
                                item.Item.StartNodeId,
                                _subscription.Id, _subscription.Connection.CreateConnectionId(),
                                item.Item.Status.SamplingInterval, item.Item.SamplingInterval,
                                item.Item.Status.QueueSize, item.Item.QueueSize);
                        }
                    }
                }
                return noErrorFound;
            }

            /// <summary>
            /// Helper to calculate greatest common divisor for the parameter of keep alive
            /// count used to allow the trigger of heart beats in a given interval.
            /// </summary>
            private static uint GreatCommonDivisor(uint a, uint b) {
                return b == 0 ? a : GreatCommonDivisor(b, a % b);
            }

            /// <summary>
            /// Resets the operation timeout on the session accrding to the publishing intervals on all subscriptions
            /// </summary>
            private void ReapplySessionOperationTimeout(ISession session, Subscription newSubscription) {
                if (session == null) {
                    return;
                }

                var currentOperationTimeout = _outer._clientConfig.OperationTimeout;
                var localMaxOperationTimeout =
                    newSubscription.PublishingInterval * (int)newSubscription.KeepAliveCount;
                if (currentOperationTimeout < localMaxOperationTimeout) {
                    currentOperationTimeout = localMaxOperationTimeout;
                }

                foreach (var subscription in session.Subscriptions) {
                    localMaxOperationTimeout =
                        (int)subscription.CurrentPublishingInterval * (int)subscription.CurrentKeepAliveCount;
                    if (currentOperationTimeout < localMaxOperationTimeout) {
                        currentOperationTimeout = localMaxOperationTimeout;
                    }
                }
                if (session.OperationTimeout != currentOperationTimeout) {
                    session.OperationTimeout = currentOperationTimeout;
                }
            }


            /// <summary>
            /// Retrieve a raw subscription with all settings applied (no lock)
            /// </summary>
            private Subscription GetSubscription(ISession session,
                SubscriptionConfigurationModel configuration, bool activate) {

                var hasNewConfig = false;
                var logRevisedValues = false;
                if (configuration != null) {
                    // Apply new configuration right here saving us from modifying later
                    _subscription.Configuration = configuration.Clone();
                    hasNewConfig = true;
                }

                if (session == null) {
                    session = _outer._sessionManager.GetOrCreateSession(_subscription.Connection, true);
                    if (session == null) {
                        return null;
                    }
                }

                // TODO propagate the default PublishingInterval currently only avaliable for standalone mode
                var configuredPublishingInterval = (int)(_subscription.Configuration?.PublishingInterval)
                    .GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
                var normedPublishingInterval = (uint)(configuredPublishingInterval > 0 ? configuredPublishingInterval : 1);

                // calculate the KeepAliveCount no matter what, perhaps monitored items were changed
                var revisedKeepAliveCount = (_subscription.Configuration?.KeepAliveCount)
                    .GetValueOrDefault(_outer._clientConfig.MaxKeepAliveCount);

                _subscription.MonitoredItems?.ForEach(m => {
                    if (m is DataMonitoredItemModel dataItem) {
                        var heartbeat = (uint)(dataItem?.HeartbeatInterval).GetValueOrDefault(TimeSpan.Zero).TotalMilliseconds;
                        if (heartbeat != 0) {
                            var itemKeepAliveCount = heartbeat / normedPublishingInterval;
                            revisedKeepAliveCount = GreatCommonDivisor(revisedKeepAliveCount, itemKeepAliveCount);
                        }
                    }
                });

                var configuredMaxNotificationsPerPublish = session.DefaultSubscription.MaxNotificationsPerPublish;

                var configuredLifetimeCount = (_subscription.Configuration?.LifetimeCount)
                    .GetValueOrDefault(session.DefaultSubscription.LifetimeCount);

                var configuredPriority = (_subscription.Configuration?.Priority)
                    .GetValueOrDefault(session.DefaultSubscription.Priority);

                var subscription = session.Subscriptions.SingleOrDefault(s => s.Handle == this);
                if (subscription == null) {
                    subscription = new Subscription(session.DefaultSubscription) {
                        Handle = this,
                        DisplayName = Name,
                        PublishingEnabled = activate, // false on initialization
                        KeepAliveCount = revisedKeepAliveCount,
                        PublishingInterval = configuredPublishingInterval,
                        MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish,
                        LifetimeCount = configuredLifetimeCount,
                        Priority = configuredPriority,
                        SequentialPublishing = true, // TODO: Make configurable
                        DisableMonitoredItemCache = false,
                        FastDataChangeCallback = OnSubscriptionDataChangeNotification,
                        FastEventCallback = OnSubscriptionEventNotificationList,
                    };

                    ReapplySessionOperationTimeout(session, subscription);

                    var result = session.AddSubscription(subscription);
                    if (!result) {
                        _logger.Error("Failed to add subscription '{subscription}' to session:'{sessionId}'",
                            _subscription.Id,
                            _subscription.Connection.CreateConnectionId());
                        return null;
                    }

                    subscription.Create();

                    if (subscription.Created == false) {
                        _logger.Error("Failed to create subscription '{subscription}'/'{sessionId}'",
                            _subscription.Id, _subscription.Connection.CreateConnectionId());
                        return null;
                    }
                    _logger.Debug("Subscription '{subscription}'/'{sessionId}' successfully created.",
                        _subscription.Id, _subscription.Connection.CreateConnectionId());

                    logRevisedValues = true;
                }
                else {
                    if (hasNewConfig) {
                        // Apply new configuration on configuration on original subscription
                        var modifySubscription = false;

                        if (revisedKeepAliveCount != subscription.KeepAliveCount) {
                            _logger.Debug(
                                "Subscription '{subscription}'/'{sessionId}' change KeepAliveCount to {new}",
                                _subscription.Id,
                                _subscription.Connection.CreateConnectionId(),
                                revisedKeepAliveCount);

                            subscription.KeepAliveCount = revisedKeepAliveCount;
                            modifySubscription = true;
                        }
                        if (subscription.PublishingInterval != configuredPublishingInterval) {
                            _logger.Debug(
                                "Subscription '{subscription}'/'{sessionId}' change publishing interval to {new}",
                                _subscription.Id,
                                _subscription.Connection.CreateConnectionId(),
                                configuredPublishingInterval);
                            subscription.PublishingInterval = configuredPublishingInterval;
                            modifySubscription = true;
                        }

                        if (subscription.MaxNotificationsPerPublish != configuredMaxNotificationsPerPublish) {
                            _logger.Debug(
                                "Subscription '{subscription}'/'{sessionId}' change MaxNotificationsPerPublish to {new}",
                                _subscription.Id,
                                _subscription.Connection.CreateConnectionId(),
                                configuredMaxNotificationsPerPublish);
                            subscription.MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish;
                            modifySubscription = true;
                        }

                        if (subscription.LifetimeCount != configuredLifetimeCount) {
                            _logger.Debug(
                                "Subscription '{subscription}'/'{sessionId}' change LifetimeCount to {new}",
                                _subscription.Id,
                                _subscription.Connection.CreateConnectionId(),
                                configuredLifetimeCount);
                            subscription.LifetimeCount = configuredLifetimeCount;
                            modifySubscription = true;
                        }
                        if (subscription.Priority != configuredPriority) {
                            _logger.Debug("Subscription '{subscription}'/'{sessionId}' change Priority to {new}",
                                _subscription.Id,
                                _subscription.Connection.CreateConnectionId(),
                                configuredPriority);
                            subscription.Priority = configuredPriority;
                            modifySubscription = true;
                        }
                        if (modifySubscription) {
                            subscription.Modify();
                            logRevisedValues = true;
                        }
                    }
                    if (subscription.CurrentPublishingEnabled != activate) {
                        // do not deactivate an already activated subscription
                        subscription.SetPublishingMode(activate);
                        logRevisedValues = true;
                    }
                }

                if (logRevisedValues && activate) {
                    var diagInfo = new StringBuilder();
                    diagInfo.Append("Subscription '{subscription}'/'{sessionId}' state actual(revised)/desired: ");
                    diagInfo.Append("PublishingEnabled {currentPublishingEnabled}/{publishingEnabled}, ");
                    diagInfo.Append("PublishingInterval {currentPublishingInterval}/{publishingInterval}, ");
                    diagInfo.Append("KeepAliveCount {currentKeepAliveCount}/{keepAliveCount}, ");
                    diagInfo.Append("LifetimeCount {currentLifetimeCount}/{lifetimeCount}");

                    _logger.Information(diagInfo.ToString(),
                        _subscription.Id, _subscription.Connection.CreateConnectionId(),
                        subscription.CurrentPublishingEnabled, subscription.PublishingEnabled,
                        subscription.CurrentPublishingInterval, subscription.PublishingInterval,
                        subscription.CurrentKeepAliveCount, subscription.KeepAliveCount,
                        subscription.CurrentLifetimeCount, subscription.LifetimeCount);
                }
                return subscription;
            }

            /// <summary>
            /// Handle event notification. Depending on the sequential publishing setting
            /// this will be called in order and thread safe or from different threads.
            /// </summary>
            /// <param name="subscription"></param>
            /// <param name="notification"></param>
            /// <param name="stringTable"></param>
            private void OnSubscriptionEventNotificationList(Subscription subscription,
                EventNotificationList notification, IList<string> stringTable) {
                if (OnSubscriptionEventChange == null) {
                    return;
                }

                if (subscription == null) {
                    _logger.Warning(
                        "EventChange for subscription: Subscription is null");
                    return;
                }

                if (notification?.Events == null) {
                    _logger.Warning(
                        "EventChange for subscription: {Subscription} having empty notification",
                        subscription.DisplayName);
                    return;
                }

                if (notification.Events.Count == 0) {
                    _logger.Warning(
                        "EventChange for subscription: {Subscription} having no events",
                        subscription.DisplayName);
                    return;
                }
                try {

                    // check if notification is a keep alive
                    var isKeepAlive = notification.Events[0].ClientHandle == 0
                        && notification.Events[0].Message?.NotificationData?.Count == 0;
                    if (isKeepAlive) {
                        var sequenceNumber = notification.Events[0].Message.SequenceNumber;
                        var publishTime = notification.Events[0].Message.PublishTime;

                        // in case of a keepalive,the sequence number is not incremented by the servers
                        _logger.Information("Keep alive for subscription '{subscription}'/'{sessionId}' " +
                            "with sequenceNumber {sequenceNumber}, publishTime {PublishTime} on Event callback.",
                            Name, Connection.CreateConnectionId(), sequenceNumber, publishTime);

                        var message = new SubscriptionNotificationModel {
                            ServiceMessageContext = subscription?.Session?.MessageContext,
                            ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                            EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                            SubscriptionName = Name,
                            Timestamp = publishTime,
                            SubscriptionId = Id,
                            MessageType = Opc.Ua.PubSub.MessageType.KeepAlive,
                            MetaData = _currentMetaData
                        };
                        OnSubscriptionEventChange.Invoke(this, message);
                    }
                    else {
                        var numOfEvents = 0;
                        var missingSequenceNumbers = Array.Empty<uint>();
                        for (var i = 0; i < notification.Events.Count; i++) {
                            var eventNotification = notification.Events[i];
                            Debug.Assert(eventNotification != null);

                            var monitoredItem = subscription.FindItemByClientHandle(eventNotification.ClientHandle);
                            var sequenceNumber = eventNotification.Message.SequenceNumber;

                            if (i == 0 && !SequenceNumber.Validate(sequenceNumber, ref _lastSequenceNumber,
                                out missingSequenceNumbers, out var dropped)) {
                                _logger.Warning("Event for monitored item {clientHandle} subscription " +
                                    "'{subscription}'/'{sessionId}' has unexpected sequenceNumber {sequenceNumber} " +
                                    "missing {expectedSequenceNumber} which were {dropped}, publishTime {PublishTime}",
                                    eventNotification.ClientHandle, Name, Connection.CreateConnectionId(), sequenceNumber,
                                    SequenceNumber.ToString(missingSequenceNumbers), dropped ? "dropped" : "already received",
                                    eventNotification.Message.PublishTime);
                            }

                            _logger.Verbose("Event for subscription: {Subscription}, sequence#: " +
                                "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                                subscription.DisplayName, sequenceNumber, isKeepAlive, eventNotification.Message.PublishTime);

                            if (monitoredItem?.Handle is MonitoredItemWrapper wrapper) {
                                var message = new SubscriptionNotificationModel {
                                    ServiceMessageContext = subscription?.Session?.MessageContext,
                                    ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                                    EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                                    SubscriptionName = Name,
                                    SubscriptionId = Id,
                                    MessageType = Opc.Ua.PubSub.MessageType.Event,
                                    MetaData = _currentMetaData,
                                    Timestamp = eventNotification.Message.PublishTime,
                                    Notifications = new List<MonitoredItemNotificationModel>(),
                                };

                                wrapper.ProcessEventNotification(message, eventNotification);

                                if (message.Notifications.Count > 0) {
                                    OnSubscriptionEventChange.Invoke(this, message);
                                    numOfEvents++;
                                }
                            }
                            else {
                                _logger.Warning("Monitored item not found with client handle {clientHandle} " +
                                    "for Event received for subscription '{subscription}'/'{sessionId}' + " +
                                    "{sequenceNumber}, publishTime {PublishTime}",
                                    eventNotification.ClientHandle, Name, Connection.CreateConnectionId(),
                                    sequenceNumber, eventNotification.Message.PublishTime);
                            }
                        }
                        OnSubscriptionEventDiagnosticsChange.Invoke(this, numOfEvents);
                    }
                }
                catch (Exception e) {
                    _logger.Warning(e, "Exception processing subscription notification");
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
                DataChangeNotification notification, IList<string> stringTable) {
                if (OnSubscriptionDataChange == null) {
                    return;
                }
                if (notification?.MonitoredItems == null || notification.MonitoredItems.Count == 0) {
                    _logger.Warning(
                        "DataChange for subscription '{subscription}'/'{sessionId}' has empty notification",
                        Name, Connection.CreateConnectionId());
                    return;
                }
                try {
                    // check if notification is a keep alive
                    var isKeepAlive = notification.MonitoredItems.Count == 1
                        && notification.MonitoredItems[0].ClientHandle == 0
                        && notification.MonitoredItems[0].Message?.NotificationData?.Count == 0;

                    SubscriptionNotificationModel message;
                    if (isKeepAlive) {
                        var sequenceNumber = notification.MonitoredItems[0].Message.SequenceNumber;
                        var publishTime = notification.MonitoredItems[0].Message.PublishTime;

                        // in case of a keepalive,the sequence number is not incremented by the servers
                        _logger.Information("Keep alive for subscription '{subscription}'/'{sessionId}' " +
                            "with sequenceNumber {sequenceNumber}, publishTime {PublishTime} on data change.",
                            Name, Connection.CreateConnectionId(), sequenceNumber, publishTime);

                        message = new SubscriptionNotificationModel {
                            ServiceMessageContext = subscription?.Session?.MessageContext,
                            ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                            EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                            SubscriptionName = Name,
                            Timestamp = publishTime,
                            SubscriptionId = Id,
                            MessageType = Opc.Ua.PubSub.MessageType.KeepAlive,
                            MetaData = _currentMetaData,
                            Notifications = new List<MonitoredItemNotificationModel>(),
                        };
                    }
                    else {
                        message = new SubscriptionNotificationModel {
                            ServiceMessageContext = subscription?.Session?.MessageContext,
                            ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                            EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                            SubscriptionName = Name,
                            SubscriptionId = Id,
                            MessageType = Opc.Ua.PubSub.MessageType.DeltaFrame,
                            MetaData = _currentMetaData,
                            Notifications = new List<MonitoredItemNotificationModel>(),
                        };
                        var missingSequenceNumbers = Array.Empty<uint>();
                        for (var i = 0; i < notification.MonitoredItems.Count; i++) {
                            Debug.Assert(notification?.MonitoredItems != null);
                            var item = notification.MonitoredItems[i];
                            Debug.Assert(item != null);

                            var monitoredItem = subscription.FindItemByClientHandle(item.ClientHandle);
                            var sequenceNumber = item.Message.SequenceNumber;
                            message.Timestamp = item.Message.PublishTime;

                            // All notifications have the same message and thus sequence number
                            if (i == 0 && !SequenceNumber.Validate(sequenceNumber, ref _lastSequenceNumber,
                                out missingSequenceNumbers, out var dropped)) {
                                _logger.Warning("DataChange for monitored item {clientHandle} subscription " +
                                    "'{subscription}'/'{sessionId}' has unexpected sequenceNumber {sequenceNumber} " +
                                    "missing {expectedSequenceNumber} which were {ropped}, publishTime {PublishTime}",
                                    item.ClientHandle, Name, Connection.CreateConnectionId(), sequenceNumber,
                                    SequenceNumber.ToString(missingSequenceNumbers),
                                    dropped ? "dropped" : "already received", item.Message.PublishTime);
                            }

                            _logger.Verbose("Data change for subscription: {Subscription}, sequence#: " +
                                "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                                subscription.DisplayName, sequenceNumber, isKeepAlive, message.Timestamp);

                            if (monitoredItem?.Handle is MonitoredItemWrapper wrapper) {
                                wrapper.ProcessMonitoredItemNotification(message, item);
                            }
                            else {
                                _logger.Warning("Monitored item not found with client handle {clientHandle} " +
                                    "for DataChange received for subscription '{subscription}'/'{sessionId}' + " +
                                    "{sequenceNumber}, publishTime {PublishTime}",
                                    item.ClientHandle, Name, Connection.CreateConnectionId(),
                                    sequenceNumber, message.Timestamp);
                            }
                        }
                    }

                    // add a heartbeat for monitored items that did not receive a datachange notification
                    var currentlyMonitored = _currentlyMonitored.ToBuilder();
                    currentlyMonitored.RemoveRange(notification.MonitoredItems.Select(m => m.ClientHandle));
                    foreach (var wrapper in currentlyMonitored.Values) {
                        wrapper.ProcessMonitoredItemNotification(message, null);
                    }

                    if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug)) {
                        var erroneousNotifications = message.Notifications?
                            .Where(n => n.Value.Value == null
                                || StatusCode.IsNotGood(n.Value.StatusCode))
                            .ToList();

                        if (erroneousNotifications.Count > 0) {
                            _logger.Debug("Found {count} notifications with null value or not good status "
                                + "code for '{subscription}'/'{sessionId}' subscription.",
                                erroneousNotifications.Count,
                                Name,
                                Connection.CreateConnectionId());
                        }
                    }

                    OnSubscriptionDataChange.Invoke(this, message);
                    Debug.Assert(message.Notifications != null);
                    if (message.Notifications.Count > 0) {
                        OnSubscriptionDataDiagnosticsChange.Invoke(this, message.Notifications.Count);
                    }
                }
                catch (Exception e) {
                    _logger.Warning(e, "Exception processing subscription notification");
                }
            }

            private readonly SubscriptionModel _subscription;
            private readonly SubscriptionServices _outer;
            private readonly ILogger _logger;
            private readonly SemaphoreSlim _lock;
            private ImmutableDictionary<uint, MonitoredItemWrapper> _currentlyMonitored;
            private DataSetMetaDataType _currentMetaData;
            private uint _lastSequenceNumber;
            private bool _closed;

            private static uint _lastIndex;
            private static readonly Gauge kMonitoredItems = Metrics.CreateGauge(
                "iiot_edge_publisher_monitored_items", "monitored items count",
                new GaugeConfiguration {
                    LabelNames = new[] { "subscription" }
                });
        }
    }
}
