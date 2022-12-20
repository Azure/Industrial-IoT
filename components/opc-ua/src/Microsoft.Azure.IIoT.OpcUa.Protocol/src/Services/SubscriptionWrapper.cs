﻿// ------------------------------------------------------------
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
            public string Id => _subscription.Id;

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
                    return (IsConnectionOk && _currentlyMonitored != null) ?
                        _currentlyMonitored
                            .Where(x => x.Item.Status.MonitoringMode == Opc.Ua.MonitoringMode.Reporting)
                            .Count() : 0;
                }
            }

            /// <inheritdoc/>
            public int NumberOfBadNodes {
                get {
                    return (IsConnectionOk && _currentlyMonitored != null) ?
                        _currentlyMonitored
                            .Where(x => x.Item.Status.MonitoringMode != Opc.Ua.MonitoringMode.Reporting)
                            .Count() : 0;
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
            }

            /// <inheritdoc/>
            public void OnSubscriptionStateChanged(bool online) {
                if (_currentlyMonitored == null) {
                    return;
                }
                foreach (var monitoredItem in _currentlyMonitored) {
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
                                Id,
                                Connection.CreateConnectionId());
                            Try.Op(() => subscription.PublishingEnabled = false);
                            Try.Op(() => subscription.ApplyChanges());
                            Try.Op(() => subscription.DeleteItems());
                            _logger.Debug("Deleted monitored items for '{subscription}'/'{sessionId}'",
                                Id,
                                Connection.CreateConnectionId());
                            Try.Op(() => session.RemoveSubscription(subscription));
                            _logger.Information("Subscription '{subscription}' in session '{sessionId}' successfully closed (Remaining: {Remaining}).",
                                Id,
                                Connection.CreateConnectionId(), session.Subscriptions.Count());
                        }
                        else {
                            _logger.Warning("Failed to close subscription '{subscription}'. Subscription was not found in session '{sessionId}'.",
                                Id,
                                Connection.CreateConnectionId());
                        }
                    }
                    else {
                        _logger.Warning("Failed to close subscription '{subscription}'. The attached session '{sessionId}' could not be found.",
                                Id,
                                Connection.CreateConnectionId());
                    }
                }
                catch (Exception e) {
                    _logger.Error(e, "Failed to close subscription '{subscription}'/'{sessionId}'",
                        Id,
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
            public async Task<SubscriptionNotificationModel> GetSubscriptionNotificationAsync(bool withNotifications) {
                await _lock.WaitAsync().ConfigureAwait(false);
                try {
                    var subscription = GetSubscription(null, null, false);
                    if (subscription == null) {
                        return null;
                    }
                    return new SubscriptionNotificationModel {
                        ServiceMessageContext = subscription.Session.MessageContext,
                        ApplicationUri = subscription.Session.Endpoint.Server.ApplicationUri,
                        EndpointUrl = subscription.Session.Endpoint.EndpointUrl,
                        MetaData = _currentMetaData,
                        SubscriptionId = Id,
                        Notifications = !withNotifications ? null : subscription.MonitoredItems
                            .Select(m => m.LastValue.ToMonitoredItemNotification(m))
                            .Where(m => m != null)
                            .ToList()
                    };
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
            private void ResolveDisplayNames(Session session) {
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
                            Id,
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
                            Id,
                            Connection.CreateConnectionId());
                    }
                    _currentlyMonitored = null;

                    // TODO: Set metadata to empty here
                    rawSubscription.ApplyChanges();
                    rawSubscription.SetPublishingMode(false);
                    if (rawSubscription.MonitoredItemCount != 0) {
                        _logger.Warning("Failed to remove {count} monitored items from subscription "
                            + "'{subscription}'/'{sessionId}'",
                            rawSubscription.MonitoredItemCount,
                            Id,
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
                        Id,
                        Connection.CreateConnectionId());
                }

                // todo re-associate detached handles!?
                var toRemoveDetached = rawSubscription.MonitoredItems.Where(m => m.Status == null);
                if (toRemoveDetached.Any()) {
                    rawSubscription.RemoveItems(toRemoveDetached);
                    _logger.Information("Removed {count} detached monitored items from subscription "
                        + "'{subscription}'/'{sessionId}'",
                        toRemoveDetached.Count(),
                        Id,
                        Connection.CreateConnectionId());
                }

                var nowMonitored = new List<MonitoredItemWrapper>();
                var toAddList = desiredState.Except(currentState);
                if (toAddList.Any()) {
                    count = 0;
                    // Add new monitored items not in current state
                    _logger.Verbose("Add monitored items to subscription '{subscription}'/'{sessionId}'...",
                        Id,
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
                        Id,
                        Connection.CreateConnectionId());
                }

                // Update monitored items that have changed
                var desiredUpdates = desiredState.Intersect(currentState)
                    .ToDictionary(k => k, v => v);
                count = 0;
                foreach (var toUpdate in currentState.Intersect(desiredState)) {
                    if (toUpdate.MergeWith(rawSubscription.Session?.MessageContext,
                            rawSubscription.Session?.NodeCache, codec, desiredUpdates[toUpdate], out var metadata)) {
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
                        Id,
                        Connection.CreateConnectionId());
                }

                if (applyChanges) {

                    rawSubscription.ApplyChanges();
                    _currentlyMonitored = nowMonitored;

                    var map = _currentlyMonitored.ToDictionary(k => k.Template.StartNodeId, v => v);
                    foreach (var item in _currentlyMonitored.ToList()) {
                        if (item.Template.TriggerId != null &&
                            map.TryGetValue(item.Template.TriggerId, out var trigger)) {
                            trigger?.AddTriggerLink(item.ServerId.GetValueOrDefault());
                        }
                    }

                    // Set up any new trigger configuration if needed
                    foreach (var item in _currentlyMonitored.ToList()) {
                        if (item.GetTriggeringLinks(out var added, out var removed)) {
                            var response = await rawSubscription.Session.SetTriggeringAsync(
                                null, rawSubscription.Id, item.ServerId.GetValueOrDefault(),
                                new UInt32Collection(added), new UInt32Collection(removed), CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                    }

                    // sanity check
                    foreach (var monitoredItem in _currentlyMonitored) {
                        if (monitoredItem.Item.Status.Error != null &&
                            StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode)) {
                            _logger.Warning("Error monitoring node {id} due to {code} in subscription "
                                + "'{subscription}'/'{sessionId}'", monitoredItem.Item.StartNodeId,
                                monitoredItem.Item.Status.Error.StatusCode,
                                Id,
                                Connection.CreateConnectionId());
                            monitoredItem.Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                            noErrorFound = false;
                        }
                    }

                    count = _currentlyMonitored.Count(m => m.Item.Status.Error == null);
                    kMonitoredItems.WithLabels(rawSubscription.Id.ToString()).Set(count);

                    _logger.Information("Now monitoring {count} nodes in subscription "
                        + "'{subscription}'/'{sessionId}'",
                        count,
                        Id,
                        Connection.CreateConnectionId());

                    if (_currentlyMonitored.Count != rawSubscription.MonitoredItemCount) {
                        _logger.Error("Monitored items mismatch: wrappers: {wrappers} != items: {items} ",
                            _currentlyMonitored.Count,
                            _currentlyMonitored.Count);
                    }
                }
                else {
                    if (_currentlyMonitored != null) {
                        // do a sanity check
                        foreach (var monitoredItem in _currentlyMonitored) {
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
                }

                if (metadataChanged) {
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
                        major, minor, Id, Connection.CreateConnectionId());
                    var dataTypes = new NodeIdDictionary<DataTypeDescription>();
                    var fields = new FieldMetaDataCollection();
                    _currentlyMonitored?.ForEach(monitoredItem => monitoredItem.GetMetaData(
                        rawSubscription.Session?.MessageContext, rawSubscription.Session?.NodeCache,
                        fields, dataTypes));

                    _currentMetaData = new DataSetMetaDataType {
                        Namespaces = rawSubscription.Session?.NamespaceUris.ToArray(),
                        EnumDataTypes = dataTypes.Values.OfType<EnumDescription>().ToArray(),
                        StructureDataTypes = dataTypes.Values.OfType<StructureDescription>().ToArray(),
                        SimpleDataTypes = dataTypes.Values.OfType<SimpleTypeDescription>().ToArray(),
                        Fields = fields,
                        ConfigurationVersion = new ConfigurationVersionDataType {
                            MajorVersion = major,
                            MinorVersion = minor
                        }
                    };
                }

                if (activate && _currentlyMonitored != null) {
                    // Change monitoring mode of all valid items if needed
                    var validItems = _currentlyMonitored.Where(v => v.Item.Created);
                    foreach (var change in validItems.GroupBy(i => i.GetMonitoringModeChange())) {
                        if (change.Key == null) {
                            continue;
                        }
                        var changeList = change.ToList();
                        _logger.Information("Set monitoring to {value} for {count} items in subscription "
                            + "'{subscription}'/'{sessionId}'.",
                            change.Key.Value,
                            change.Count(),
                            Id,
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
                                    Id,
                                    Connection.CreateConnectionId());

                                for (int i = 0; i < results.Count && i < itemsToChange.Count; ++i) {
                                    if (StatusCode.IsNotGood(results[i].StatusCode)) {
                                        _logger.Warning("Set monitoring for item '{item}' in subscription "
                                            + "'{subscription}'/'{sessionId}' failed with '{status}'.",
                                            itemsToChange[i].StartNodeId,
                                            Id,
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
            private void ReapplySessionOperationTimeout(Session session, Subscription newSubscription) {
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
            private Subscription GetSubscription(Session session,
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
                        DisplayName = Id,
                        PublishingEnabled = activate, // false on initialization
                        KeepAliveCount = revisedKeepAliveCount,
                        PublishingInterval = configuredPublishingInterval,
                        MaxNotificationsPerPublish = configuredMaxNotificationsPerPublish,
                        LifetimeCount = configuredLifetimeCount,
                        Priority = configuredPriority,
                        FastDataChangeCallback = OnSubscriptionDataChanged,
                        FastEventCallback = OnSubscriptionEventChanged,
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

            private void OnSubscriptionEventChanged(Subscription subscription, EventNotificationList notification, IList<string> stringTable) {
                try {
                    if (OnSubscriptionEventChange == null) {
                        return;
                    }

                    if (subscription == null) {
                        _logger.Warning(
                            "EventChange for subscription: Subscription is null");
                        return;
                    }

                    if (notification == null) {
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

                    if (_currentlyMonitored == null) {
                        _logger.Information(
                            "EventChange for subscription: {Subscription} having no monitored items yet",
                            subscription.DisplayName);
                        return;
                    }

                    // check if notification is a keep alive
                    var isKeepAlive = notification.Events.First().ClientHandle == 0 &&
                                      notification.Events.First().Message?.NotificationData?.Count == 0;
                    var sequenceNumber = notification.Events.First().Message?.SequenceNumber;
                    var publishTime = (notification.Events.First().Message?.PublishTime).
                        GetValueOrDefault(DateTime.UtcNow);
                    var numOfEvents = 0;

                    _logger.Debug("Event for subscription: {Subscription}, sequence#: " +
                        "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                        subscription.DisplayName, sequenceNumber, isKeepAlive, publishTime);

                    if (notification?.Events != null) {
                        for (var i = 0; i < notification.Events.Count; i++) {
                            var monitoredItem = subscription.MonitoredItems.SingleOrDefault(
                                    m => m.ClientHandle == notification.Events[i].ClientHandle);
                            if (monitoredItem == null) {
                                continue;
                            }

                            var message = new SubscriptionNotificationModel {
                                ServiceMessageContext = subscription.Session?.MessageContext,
                                ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                                EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                                SubscriptionId = Id,
                                Timestamp = publishTime,
                                MetaData = _currentMetaData,
                                Notifications = new List<MonitoredItemNotificationModel>()
                            };

                            if (monitoredItem.Handle is MonitoredItemWrapper itemWrapper) {
                                itemWrapper.ProcessMonitoredItemNotification(message, notification.Events[i]);
                            }

                            if (message.Notifications.Any()) {
                                OnSubscriptionEventChange.Invoke(this, message);
                                numOfEvents++;
                            }
                        }
                    }

                    OnSubscriptionEventDiagnosticsChange.Invoke(this, numOfEvents);
                }
                catch (Exception e) {
                    _logger.Warning(e, "Exception processing subscription notification");
                }
            }

            /// <summary>
            /// Subscription data changed
            /// </summary>
            private void OnSubscriptionDataChanged(Subscription subscription,
                DataChangeNotification notification, IList<string> stringTable) {
                try {
                    if (OnSubscriptionDataChange == null) {
                        return;
                    }
                    if (notification == null) {
                        _logger.Warning(
                            "DataChange for subscription '{subscription}'/'{sessionId}' has empty notification",
                            Id,
                            Connection.CreateConnectionId());
                        return;
                    }

                    if (_currentlyMonitored == null) {
                        _logger.Information(
                            "DataChange for subscription '{subscription}'/'{sessionId}' has no monitored items yet",
                            Id,
                            Connection.CreateConnectionId());
                        return;
                    }

                    // check if notification is a keep alive
                    var isKeepAlive = notification?.MonitoredItems?.Count == 1 &&
                                      notification?.MonitoredItems?.First().ClientHandle == 0 &&
                                      notification?.MonitoredItems?.First().Message?.NotificationData?.Count == 0;
                    var sequenceNumber = (notification?.MonitoredItems?.First()?.Message?.SequenceNumber)
                                         .GetValueOrDefault(0);
                    var publishTime = (notification?.MonitoredItems?.First().Message?.PublishTime).
                        GetValueOrDefault(DateTime.UtcNow);

                    if (isKeepAlive) {
                        // in case of a keepalive,the sequence number is not incremented by the servers
                        _logger.Information("Keep alive for subscription '{subscription}'/'{sessionId}' with sequenceNumber "
                            + "{sequenceNumber}, publishTime {PublishTime}",
                            Id, Connection.CreateConnectionId(), sequenceNumber, publishTime);
                    }
                    else {
                        if (_expectedSequenceNumber != sequenceNumber) {
                            _logger.Warning("DataChange for subscription '{subscription}'/'{sessionId}' has unexpected sequenceNumber "
                                + "{sequenceNumber} vs expected {expectedSequenceNumber}, publishTime {PublishTime}",
                                Id, Connection.CreateConnectionId(),
                                sequenceNumber, _expectedSequenceNumber, publishTime);
                        }
                        _expectedSequenceNumber = sequenceNumber + 1;
                    }

                    var message = new SubscriptionNotificationModel {
                        ServiceMessageContext = subscription?.Session?.MessageContext,
                        ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                        SubscriptionId = Id,
                        Timestamp = publishTime,
                        MetaData = _currentMetaData,
                        Notifications = notification.ToMonitoredItemNotifications(
                                subscription?.MonitoredItems)?.ToList()
                                ?? new List<MonitoredItemNotificationModel>(),
                    };

                    // add the heartbeat for monitored items that did not receive a datachange notification
                    // Try access lock if we cannot continue...
                    List<MonitoredItemWrapper> currentlyMonitored = null;
                    if (_lock?.Wait(0) ?? true) {
                        try {
                            currentlyMonitored = _currentlyMonitored;
                        }
                        finally {
                            _lock?.Release();
                        }
                    }

                    if (currentlyMonitored != null) {
                        // add the heartbeat for monitored items that did not receive a
                        // a datachange notification
                        foreach (var item in currentlyMonitored) {
                            if (!notification.MonitoredItems.
                                Exists(m => m.ClientHandle == item.Item.ClientHandle)
                                && item.ValidateHeartbeat(publishTime)) {

                                MonitoredItemNotificationModel GetDefaultNotification() {
                                    return new MonitoredItemNotificationModel {
                                        DataSetFieldName = item?.Template?.DataSetFieldName,
                                        Id = item?.Template?.Id,
                                        DisplayName = item?.Item?.DisplayName,
                                        NodeId = item?.Template?.StartNodeId,
                                        AttributeId = item.Item.AttributeId,
                                        Value = new DataValue(Variant.Null,
                                            item?.Item?.Status?.Error?.StatusCode ??
                                            StatusCodes.BadMonitoredItemIdInvalid),
                                    };
                                }

                                var heartbeatValue = item.Item?.LastValue.
                                    ToMonitoredItemNotification(item.Item, GetDefaultNotification);
                                if (heartbeatValue != null) {
                                    heartbeatValue.SequenceNumber = sequenceNumber;
                                    heartbeatValue.IsHeartbeat = true;
                                    if (message.Notifications == null) {
                                        message.Notifications =
                                            new List<MonitoredItemNotificationModel>();
                                    }
                                    message.Notifications.Add(heartbeatValue);
                                }
                                continue;
                            }
                            item.ValidateHeartbeat(publishTime);
                        }
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
                                Id,
                                Connection.CreateConnectionId());
                        }
                    }

                    if (message.Notifications?.Any() == true) {
                        OnSubscriptionDataChange.Invoke(this, message);
                        OnSubscriptionDataDiagnosticsChange.Invoke(this, message.Notifications.Count);
                    }
                }
                catch (Exception e) {
                    _logger.Warning(e, "Exception processing subscription notification");
                }
            }

            public void SendEventNotification(Subscription subscription, MonitoredItemNotificationModel notification) {
                var message = new SubscriptionNotificationModel {
                    ServiceMessageContext = subscription?.Session?.MessageContext,
                    ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                    SubscriptionId = Id,
                    MetaData = _currentMetaData,
                    Timestamp = DateTime.UtcNow,
                    Notifications = new List<MonitoredItemNotificationModel> { notification }
                };
                OnSubscriptionEventChange.Invoke(this, message);
            }

            private readonly SubscriptionModel _subscription;
            private readonly SubscriptionServices _outer;
            private readonly ILogger _logger;
            private readonly SemaphoreSlim _lock;
            private List<MonitoredItemWrapper> _currentlyMonitored;
            private DataSetMetaDataType _currentMetaData;
            private uint _expectedSequenceNumber = 1;
            private bool _closed;
            private static readonly Gauge kMonitoredItems = Metrics.CreateGauge(
                "iiot_edge_publisher_monitored_items", "monitored items count",
                new GaugeConfiguration {
                    LabelNames = new[] { "subscription" }
                });
        }
    }
}
