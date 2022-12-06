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
    using Opc.Ua.Encoders;
    using Opc.Ua.Extensions;
    using Prometheus;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Subscription services implementation
    /// </summary>
    public class SubscriptionServices : ISubscriptionManager, IDisposable {

        /// <summary>
        /// Create subscription manager
        /// </summary>
        public SubscriptionServices(ISessionManager sessionManager,
            IVariantEncoderFactory codec,
            IClientServicesConfig clientConfig,
            ILogger logger) {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(codec));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task<ISubscription> GetOrCreateSubscriptionAsync(SubscriptionModel subscriptionModel) {
            if (string.IsNullOrEmpty(subscriptionModel?.Id)) {
                throw new ArgumentNullException(nameof(subscriptionModel));
            }
            var sub = new SubscriptionWrapper(this, subscriptionModel, _logger);
            _sessionManager.RegisterSubscription(sub);
            return Task.FromResult<ISubscription>(sub);
        }

        /// <inheritdoc/>
        public void Dispose() {
        }

        /// <summary>
        /// Function to retrieve display name for a field in the select clause
        /// </summary>
        /// <param name="monitoredItem"></param>
        /// <param name="index"></param>
        /// <returns>The display name, if defined</returns>
        public static string GetFieldDisplayName(MonitoredItem monitoredItem, int index) {
            return (monitoredItem.Handle as MonitoredItemWrapper)?.FieldNames[index];
        }

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

            /// <inheritdoc/>
            public event EventHandler<SubscriptionNotificationModel> OnMonitoredItemChange;

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
                        var subscription = session.Subscriptions.
                            SingleOrDefault(s => s.Handle == this);
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
            public async Task<SubscriptionNotificationModel> GetSnapshotAsync() {
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
                        SubscriptionId = Id,
                        Notifications = subscription.MonitoredItems
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

                var applyChanges = false;
                var noErrorFound = true;
                var count = 0;
                var codec = _outer._codec.Create(rawSubscription.Session.MessageContext);
                if (monitoredItems == null || !monitoredItems.Any()) {
                    // cleanup
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
                            toRemove.Notification -= OnMonitoredItemChanged;
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

                var toRemoveList = currentState.Except(desiredState).Select(t => t.Item);
                if (toRemoveList.Any()) {
                    count = 0;
                    // Remove monitored items not in desired state
                    foreach (var toRemove in toRemoveList) {
                        _logger.Verbose("Removing monitored item '{item}'...", toRemove.StartNodeId);
                        toRemove.Notification -= OnMonitoredItemChanged;
                        ((MonitoredItemWrapper)toRemove.Handle).Destroy();
                        count++;
                    }
                    rawSubscription.RemoveItems(toRemoveList);
                    applyChanges = true;
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
                            toAdd.Item.Notification += OnMonitoredItemChanged;
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
                            rawSubscription.Session?.NodeCache, codec, desiredUpdates[toUpdate])) {
                        _logger.Verbose("Updating monitored item '{item}'...", toUpdate);
                        count++;
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

                // TODO propagate the default values currently only available for standalone mode
                var configuredMaxNotificationsPerPublish = (_subscription.Configuration?.MaxNotificationsPerPublish)
                    .GetValueOrDefault(session.DefaultSubscription.MaxNotificationsPerPublish);

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
                                        Id = item?.Template?.Id,
                                        DisplayName = item?.Item?.DisplayName,
                                        NodeId = item?.Template?.StartNodeId,
                                        AttributeId = item.Item.AttributeId,
                                        Value = new DataValue(Variant.Null,
                                            item?.Item?.Status?.Error?.StatusCode ??
                                            StatusCodes.BadMonitoredItemIdInvalid),
                                        Overflow = false,
                                        NotificationData = null,
                                        StringTable = null,
                                        DiagnosticInfo = null,
                                    };
                                }

                                var heartbeatValue = item.Item?.LastValue.
                                    ToMonitoredItemNotification(item.Item, GetDefaultNotification);
                                if (heartbeatValue != null) {
                                    heartbeatValue.SequenceNumber = sequenceNumber;
                                    heartbeatValue.IsHeartbeat = true;
                                    heartbeatValue.PublishTime = publishTime;
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

            /// <summary>
            /// Monitored item notification handler
            /// </summary>
            private void OnMonitoredItemChanged(MonitoredItem monitoredItem,
                MonitoredItemNotificationEventArgs eventArgs) {
                try {
                    if (OnMonitoredItemChange == null) {
                        return;
                    }
                    if (eventArgs?.NotificationValue == null || monitoredItem?.Subscription?.Session == null) {
                        return;
                    }
                    if (!(eventArgs.NotificationValue is MonitoredItemNotification notification)) {
                        return;
                    }
                    if (!(notification.Value is DataValue value)) {
                        return;
                    }

                    var message = new SubscriptionNotificationModel {
                        ServiceMessageContext = monitoredItem.Subscription.Session.MessageContext,
                        ApplicationUri = monitoredItem.Subscription.Session.Endpoint.Server.ApplicationUri,
                        EndpointUrl = monitoredItem.Subscription.Session.Endpoint.EndpointUrl,
                        SubscriptionId = Id,
                        Notifications = new List<MonitoredItemNotificationModel> {
                            notification.ToMonitoredItemNotification(monitoredItem)
                        }
                    };
                    OnMonitoredItemChange(this, message);
                }
                catch (Exception e) {
                    _logger.Warning(e, "Exception processing monitored item notification");
                }
            }

            public void SendMessage(SubscriptionNotificationModel message) {
                OnSubscriptionEventChange.Invoke(this, message);
            }

            private readonly SubscriptionModel _subscription;
            private readonly SubscriptionServices _outer;
            private readonly ILogger _logger;
            private readonly SemaphoreSlim _lock;
            private List<MonitoredItemWrapper> _currentlyMonitored;
            private uint _expectedSequenceNumber = 1;
            private bool _closed;
            private static readonly Gauge kMonitoredItems = Metrics.CreateGauge(
                "iiot_edge_publisher_monitored_items", "monitored items count",
                new GaugeConfiguration {
                    LabelNames = new[] { "subscription" }
                });
        }

        /// <summary>
        /// Monitored item
        /// </summary>
        public class MonitoredItemWrapper {

            /// <summary>
            /// Assigned monitored item id on server
            /// </summary>
            public uint? ServerId => Item?.Status.Id;

            /// <summary>
            /// Monitored item
            /// </summary>
            public BaseMonitoredItemModel Template { get; }

            /// <summary>
            /// Monitored item as data
            /// </summary>
            public DataMonitoredItemModel DataTemplate { get { return Template as DataMonitoredItemModel; } }

            /// <summary>
            /// Monitored item as event
            /// </summary>
            public EventMonitoredItemModel EventTemplate { get { return Template as EventMonitoredItemModel; } }

            /// <summary>
            /// Monitored item created from template
            /// </summary>
            public MonitoredItem Item { get; private set; }

            /// <summary>
            /// Last published time
            /// </summary>
            public DateTime NextHeartbeat { get; private set; }

            /// <summary>
            /// List of field names. Only used for events
            /// </summary>
            public List<string> FieldNames { get; } = new List<string>();

            /// <summary>
            /// Cache of the latest events for the pending alarms optionally monitored
            /// </summary>
            public Dictionary<string, MonitoredItemNotificationModel> PendingAlarmEvents { get; }
                = new Dictionary<string, MonitoredItemNotificationModel>();

            /// <summary>
            /// Property setter that gets indication if item is online or not.
            /// </summary>
            public void OnMonitoredItemStateChanged(bool online) {
                if (_pendingAlarmsTimer != null) {
                    var enabled = _pendingAlarmsTimer.Enabled;
                    if (EventTemplate.PendingAlarms?.IsEnabled == true && online && !enabled) {
                        _pendingAlarmsTimer.Start();
                        _logger.Debug("Restarted pending alarm handling after item went online.");
                    }
                    else if (enabled) {
                        _pendingAlarmsTimer.Stop();
                        lock (_lock) {
                            PendingAlarmEvents.Clear();
                        }
                        if (!online) {
                            _logger.Debug("Stopped pending alarm handling while item is offline.");
                        }
                    }
                }
            }

            private readonly object _lock = new object();

            /// <summary>
            /// validates if a heartbeat is required.
            /// A heartbeat will be forced for the very first time
            /// </summary>
            public bool ValidateHeartbeat(DateTime currentPublish) {
                if (DataTemplate == null) {
                    return false;
                }
                if (NextHeartbeat == DateTime.MaxValue) {
                    return false;
                }
                if (NextHeartbeat > currentPublish + TimeSpan.FromMilliseconds(50)) {
                    return false;
                }
                NextHeartbeat = TimeSpan.Zero < DataTemplate.HeartbeatInterval.GetValueOrDefault(TimeSpan.Zero) ?
                    currentPublish + DataTemplate.HeartbeatInterval.Value : DateTime.MaxValue;
                return true;
            }

            /// <summary>
            /// Create wrapper
            /// </summary>
            public MonitoredItemWrapper(BaseMonitoredItemModel template, ILogger logger) {
                _logger = logger?.ForContext<MonitoredItemWrapper>() ??
                    throw new ArgumentNullException(nameof(logger));
                Template = template?.Clone() ??
                    throw new ArgumentNullException(nameof(template));
            }

            /// <inheritdoc/>
            public override bool Equals(object obj) {
                if (!(obj is MonitoredItemWrapper item)) {
                    return false;
                }
                if (Template.GetType() != item.Template.GetType()) {
                    // Event item is incompatible with a data item
                    return false;
                }
                if (Template.Id != item.Template.Id) {
                    return false;
                }
                if (!Template.RelativePath.SequenceEqualsSafe(item.Template.RelativePath)) {
                    return false;
                }
                if (Template.StartNodeId != item.Template.StartNodeId) {
                    return false;
                }
                if (Template.IndexRange != item.Template.IndexRange) {
                    return false;
                }
                if (Template.AttributeId != item.Template.AttributeId) {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode() {
                var hashCode = 1301977042;
                // Event item is incompatible with a data item
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<Type>.Default.GetHashCode(Template.GetType());
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(Template.Id);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string[]>.Default.GetHashCode(Template.RelativePath);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(Template.StartNodeId);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(Template.IndexRange);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<NodeAttribute?>.Default.GetHashCode(Template.AttributeId);
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString() {
                return $"Item '{Template.StartNodeId}' with server id {ServerId} - " +
                    $"{(Item?.Status?.Created == true ? "" : "not ")}created";
            }

            /// <summary>
            /// Create new stack monitored item
            /// </summary>

            public void Create(Session session, IVariantEncoder codec, bool activate) {
                Create(session.MessageContext as ServiceMessageContext, session.NodeCache, codec, activate);
            }

            /// <summary>
            /// Destructor for this class
            /// </summary>
            public void Destroy() {
                if (_pendingAlarmsTimer != null) {
                    _pendingAlarmsTimer.Stop();
                    _pendingAlarmsTimer.Dispose();
                }
            }

            /// <summary>
            /// Create new stack monitored item
            /// </summary>
            public void Create(ServiceMessageContext messageContext, INodeCache nodeCache,
                IVariantEncoder codec, bool activate) {

                Item = new MonitoredItem {
                    Handle = this,
                    DisplayName = Template.DisplayName ?? Template.Id,
                    AttributeId = (uint)Template.AttributeId.GetValueOrDefault((NodeAttribute)Attributes.Value),
                    IndexRange = Template.IndexRange,
                    RelativePath = Template.RelativePath?
                                .ToRelativePath(messageContext)?
                                .Format(nodeCache.TypeTree),
                    MonitoringMode = activate
                        ? Template.MonitoringMode.ToStackType().
                            GetValueOrDefault(Opc.Ua.MonitoringMode.Reporting)
                        : Opc.Ua.MonitoringMode.Disabled,
                    StartNodeId = Template.StartNodeId.ToNodeId(messageContext),
                    QueueSize = Template.QueueSize,
                    SamplingInterval = (int)Template.SamplingInterval.
                        GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds,
                    DiscardOldest = !Template.DiscardNew.GetValueOrDefault(false),
                };

                // Set event filter
                if (DataTemplate != null) {
                    Item.Filter = DataTemplate.DataChangeFilter.ToStackModel() ??
                        ((MonitoringFilter)DataTemplate.AggregateFilter.ToStackModel(messageContext));
                }
                else if (EventTemplate != null) {
                    var eventFilter = GetEventFilter(messageContext, nodeCache, codec);
                    Item.Filter = eventFilter;
                }
                else {
                    Debug.Fail($"Unexpected: Unknown type {Template.GetType()}");
                }
            }

            /// <summary>
            /// Add the monitored item identifier of the triggering item.
            /// </summary>
            internal void AddTriggerLink(uint? id) {
                if (id != null) {
                    _newTriggers.Add(id.Value);
                }
            }

            /// <summary>
            /// Merge with desired state
            /// </summary>
            /// <param name="messageContext"></param>
            /// <param name="nodeCache"></param>
            /// <param name="codec"></param>
            /// <param name="model"></param>
            /// <returns>Whether apply changes should be called on the subscription</returns>
            internal bool MergeWith(IServiceMessageContext messageContext, INodeCache nodeCache,
                IVariantEncoder codec, MonitoredItemWrapper model) {

                if (model == null || Item == null) {
                    return false;
                }

                var itemChange = false;
                if (Template.SamplingInterval.GetValueOrDefault(TimeSpan.FromSeconds(1)) !=
                    model.Template.SamplingInterval.GetValueOrDefault(TimeSpan.FromSeconds(1))) {
                    _logger.Debug("{item}: Changing sampling interval from {old} to {new}",
                        this, Template.SamplingInterval.GetValueOrDefault(
                            TimeSpan.FromSeconds(1)).TotalMilliseconds,
                        model.Template.SamplingInterval.GetValueOrDefault(
                            TimeSpan.FromSeconds(1)).TotalMilliseconds);
                    Template.SamplingInterval = model.Template.SamplingInterval;
                    Item.SamplingInterval =
                        (int)Template.SamplingInterval.GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;
                    itemChange = true;
                }
                if (Template.DiscardNew.GetValueOrDefault(false) !=
                        model.Template.DiscardNew.GetValueOrDefault()) {
                    _logger.Debug("{item}: Changing discard new mode from {old} to {new}",
                        this, Template.DiscardNew.GetValueOrDefault(false),
                        model.Template.DiscardNew.GetValueOrDefault(false));
                    Template.DiscardNew = model.Template.DiscardNew;
                    Item.DiscardOldest = !Template.DiscardNew.GetValueOrDefault(false);
                    itemChange = true;
                }
                if (Template.QueueSize != model.Template.QueueSize) {
                    _logger.Debug("{item}: Changing queue size from {old} to {new}",
                        this, Template.QueueSize,
                        model.Template.QueueSize);
                    Template.QueueSize = model.Template.QueueSize;
                    Item.QueueSize = Template.QueueSize;
                    itemChange = true;
                }
                if (Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting) !=
                    model.Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting)) {
                    _logger.Debug("{item}: Changing monitoring mode from {old} to {new}",
                        this, Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting),
                        model.Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting));
                    Template.MonitoringMode = model.Template.MonitoringMode;
                    _modeChange = Template.MonitoringMode.GetValueOrDefault(Publisher.Models.MonitoringMode.Reporting);
                }

                if (Template.DisplayName != model.Template.DisplayName) {
                    Template.DisplayName = model.Template.DisplayName;
                    Item.DisplayName = Template.DisplayName;
                    itemChange = true;
                }

                // Should never merge items with different template types
                Debug.Assert(model.Template.GetType() == Template.GetType());

                if (model.DataTemplate != null) {

                    // Update change filter
                    if (!model.DataTemplate.DataChangeFilter.IsSameAs(DataTemplate.DataChangeFilter)) {
                        DataTemplate.DataChangeFilter = model.DataTemplate.DataChangeFilter;
                        _logger.Debug("{item}: Changing data change filter.");
                        Item.Filter = DataTemplate.DataChangeFilter.ToStackModel();
                        itemChange = true;
                    }

                    // Update AggregateFilter
                    else if (!model.DataTemplate.AggregateFilter.IsSameAs(DataTemplate.AggregateFilter)) {
                        DataTemplate.AggregateFilter = model.DataTemplate.AggregateFilter;
                        _logger.Debug("{item}: Changing aggregate change filter.");
                        Item.Filter = DataTemplate.AggregateFilter.ToStackModel(messageContext);
                        itemChange = true;
                    }

                    if (model.DataTemplate.HeartbeatInterval != DataTemplate.HeartbeatInterval) {
                        _logger.Debug("{item}: Changing heartbeat from {old} to {new}",
                            this, DataTemplate.HeartbeatInterval, model.DataTemplate.HeartbeatInterval);
                        DataTemplate.HeartbeatInterval = model.DataTemplate.HeartbeatInterval;

                        itemChange = true; // TODO: Not really a change in the item
                    }
                }
                else if (model.EventTemplate != null) {

                    // Update event filter
                    if (!model.EventTemplate.EventFilter.IsSameAs(EventTemplate.EventFilter) ||
                        !model.EventTemplate.PendingAlarms.IsSameAs(EventTemplate.PendingAlarms)) {

                        EventTemplate.PendingAlarms = model.EventTemplate.PendingAlarms;
                        EventTemplate.EventFilter = model.EventTemplate.EventFilter;
                        _logger.Debug("{item}: Changing event filter.");

                        Item.Filter = GetEventFilter(messageContext, nodeCache, codec);
                        itemChange = true;
                    }
                }
                else {
                    Debug.Fail($"Unexpected: Unknown type {model.Template.GetType()}");
                }
                return itemChange;
            }

            /// <summary>
            /// Get triggering configuration changes for this item
            /// </summary>
            internal bool GetTriggeringLinks(out IEnumerable<uint> addLinks,
                out IEnumerable<uint> removeLinks) {
                var remove = _triggers.Except(_newTriggers).ToList();
                var add = _newTriggers.Except(_triggers).ToList();
                _triggers = _newTriggers;
                _newTriggers = new HashSet<uint>();
                addLinks = add;
                removeLinks = remove;
                if (add.Count > 0 || remove.Count > 0) {
                    _logger.Debug("{item}: Adding {add} triggering links and removing {remove} triggering links",
                        this,
                        add.Count,
                        remove.Count);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Get any changes in the monitoring mode
            /// </summary>
            internal Opc.Ua.MonitoringMode? GetMonitoringModeChange() {
                var change = _modeChange.ToStackType();
                _modeChange = null;
                return Item.MonitoringMode == change ? null : change;
            }

            /// <summary>
            /// Get event filter
            /// </summary>
            /// <param name="messageContext"></param>
            /// <param name="nodeCache"></param>
            /// <param name="codec"></param>
            /// <returns></returns>
            private EventFilter GetEventFilter(IServiceMessageContext messageContext, INodeCache nodeCache,
                IVariantEncoder codec) {

                // set up the timer even if event is not a pending alarms event.
                var created = false;
                if (_pendingAlarmsTimer == null) {
                    _pendingAlarmsTimer = new Timer(1000);
                    _pendingAlarmsTimer.AutoReset = false;
                    _pendingAlarmsTimer.Elapsed += OnPendingAlarmsTimerElapsed;
                    created = true;
                }

                if (!created && EventTemplate.PendingAlarms?.IsEnabled != true) {
                    // Always stop in case we are asked to disable pending alarms
                    _pendingAlarmsTimer.Stop();
                    lock (_lock) {
                        PendingAlarmEvents.Clear();
                    }
                    _logger.Information("Disabled pending alarm handling.");
                }

                var eventFilter = new EventFilter();
                if (EventTemplate.EventFilter != null) {
                    if (!string.IsNullOrEmpty(EventTemplate.EventFilter.TypeDefinitionId)) {
                        eventFilter = GetSimpleEventFilter(nodeCache, messageContext);
                    }
                    else {
                        eventFilter = codec.Decode(EventTemplate.EventFilter, true);
                    }
                }

                TestWhereClause(messageContext, nodeCache, eventFilter);

                // let's keep track of the internal fields we add so that they don't show up in the output
                var internalSelectClauses = new List<SimpleAttributeOperand>();
                if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                    && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType)) {
                    var selectClause = new SimpleAttributeOperand(ObjectTypeIds.BaseEventType, BrowseNames.EventType);
                    eventFilter.SelectClauses.Add(selectClause);
                    internalSelectClauses.Add(selectClause);
                }

                if (EventTemplate.PendingAlarms?.IsEnabled == true) {
                    var conditionIdClause = eventFilter.SelectClauses
                        .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType && x.AttributeId == Attributes.NodeId);
                    if (conditionIdClause != null) {
                        EventTemplate.PendingAlarms.ConditionIdIndex = eventFilter.SelectClauses.IndexOf(conditionIdClause);
                    }
                    else {
                        EventTemplate.PendingAlarms.ConditionIdIndex = eventFilter.SelectClauses.Count;
                        var selectClause = new SimpleAttributeOperand() {
                            BrowsePath = new QualifiedNameCollection(),
                            TypeDefinitionId = ObjectTypeIds.ConditionType,
                            AttributeId = Attributes.NodeId
                        };
                        eventFilter.SelectClauses.Add(selectClause);
                        internalSelectClauses.Add(selectClause);
                    }

                    var retainClause = eventFilter.SelectClauses
                        .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            x.BrowsePath?.FirstOrDefault() == BrowseNames.Retain);
                    if (retainClause != null) {
                        EventTemplate.PendingAlarms.RetainIndex = eventFilter.SelectClauses.IndexOf(retainClause);
                    }
                    else {
                        EventTemplate.PendingAlarms.RetainIndex = eventFilter.SelectClauses.Count;
                        var selectClause = new SimpleAttributeOperand(ObjectTypeIds.ConditionType, BrowseNames.Retain);
                        eventFilter.SelectClauses.Add(selectClause);
                        internalSelectClauses.Add(selectClause);
                    }
                    _pendingAlarmsTimer.Start();
                    _logger.Information("{Action} pending alarm handling.", created ? "Enabled" : "Re-enabled");
                }

                var sb = new StringBuilder();

                // let's loop thru the select clause and setup the field names
                foreach (var selectClause in eventFilter.SelectClauses) {
                    if (!internalSelectClauses.Any(x => x == selectClause)) {
                        sb.Clear();
                        for (var i = 0; i < selectClause.BrowsePath?.Count; i++) {
                            if (i == 0) {
                                if (selectClause.BrowsePath[i].NamespaceIndex != 0) {
                                    if (selectClause.BrowsePath[i].NamespaceIndex < nodeCache.NamespaceUris.Count) {
                                        sb.Append(nodeCache.NamespaceUris.GetString(selectClause.BrowsePath[i].NamespaceIndex));
                                        sb.Append('#');
                                    }
                                    else {
                                        sb.Append($"{selectClause.BrowsePath[i].NamespaceIndex}:");
                                    }
                                }
                            }
                            else {
                                sb.Append('/');
                            }
                            sb.Append(selectClause.BrowsePath[i].Name);
                        }

                        if (sb.Length == 0) {
                            if (selectClause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                                selectClause.AttributeId == Attributes.NodeId) {
                                sb.Append("ConditionId");
                            }
                        }
                        FieldNames.Add(sb.ToString());
                    }
                    else {
                        // if a field's nameis empty, it's not written to the output
                        FieldNames.Add(null);
                    }
                }

                return eventFilter;
            }

            /// <summary>
            /// Builds select clause and where clause by using OPC UA reflection
            /// </summary>
            /// <param name="nodeCache"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            public EventFilter GetSimpleEventFilter(INodeCache nodeCache, IServiceMessageContext context) {
                var typeDefinitionId = EventTemplate.EventFilter.TypeDefinitionId.ToNodeId(context);
                var nodes = new List<Node>();
                ExpandedNodeId superType = null;
                nodes.Insert(0, nodeCache.FetchNode(typeDefinitionId));
                do {
                    superType = nodes[0].GetSuperType(nodeCache.TypeTree);
                    if (superType != null) {
                        nodes.Insert(0, nodeCache.FetchNode(superType));
                    }
                }
                while (superType != null);

                var fieldNames = new List<QualifiedName>();
                foreach (var node in nodes) {
                    ParseFields(nodeCache, fieldNames, node);
                }
                fieldNames = fieldNames
                    .Distinct()
                    .OrderBy(x => x.Name).ToList();

                var eventFilter = new EventFilter();
                // Let's add ConditionId manually first if event is derived from ConditionType
                if (nodes.Any(x => x.NodeId == ObjectTypeIds.ConditionType)) {
                    eventFilter.SelectClauses.Add(new SimpleAttributeOperand() {
                        BrowsePath = new QualifiedNameCollection(),
                        TypeDefinitionId = ObjectTypeIds.ConditionType,
                        AttributeId = Attributes.NodeId
                    });
                }

                foreach (var fieldName in fieldNames) {
                    var selectClause = new SimpleAttributeOperand() {
                        TypeDefinitionId = ObjectTypeIds.BaseEventType,
                        AttributeId = Attributes.Value,
                        BrowsePath = fieldName.Name
                            .Split('|')
                            .Select(x => new QualifiedName(x, fieldName.NamespaceIndex))
                            .ToArray()
                    };
                    eventFilter.SelectClauses.Add(selectClause);
                }
                eventFilter.WhereClause = new ContentFilter();
                eventFilter.WhereClause.Push(FilterOperator.OfType, typeDefinitionId);

                return eventFilter;
            }

            /// <summary>
            /// Processing the monitored item notification
            /// </summary>
            /// <param name="message"></param>
            /// <param name="notification"></param>
            public void ProcessMonitoredItemNotification(SubscriptionNotificationModel message, EventFieldList notification) {
                var pendingAlarmsOptions = EventTemplate?.PendingAlarms;
                var evFilter = Item.Filter as EventFilter;
                var eventTypeIndex = evFilter?.SelectClauses.IndexOf(
                    evFilter?.SelectClauses
                        .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType
                            && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType));

                // now, is this a regular event or RefreshStartEventType/RefreshEndEventType?
                if (eventTypeIndex.HasValue && eventTypeIndex.Value != -1) {
                    var eventType = notification.EventFields[eventTypeIndex.Value].Value as NodeId;
                    if (eventType == ObjectTypeIds.RefreshStartEventType) {
                        // stop the timers during condition refresh
                        if (pendingAlarmsOptions?.IsEnabled == true) {
                            _pendingAlarmsTimer.Stop();
                            lock (_lock) {
                                PendingAlarmEvents.Clear();
                            }
                            _logger.Debug("Stopped pending alarm handling during condition refresh.");
                        }
                        return;
                    }
                    else if (eventType == ObjectTypeIds.RefreshEndEventType) {
                        if (pendingAlarmsOptions?.IsEnabled == true) {
                            // restart the timers once condition refresh is done.
                            _pendingAlarmsTimer.Start();
                            _logger.Debug("Restarted pending alarm handling after condition refresh.");
                        }
                        return;
                    }
                    else if (eventType == ObjectTypeIds.RefreshRequiredEventType) {
                        var noErrorFound = true;

                        // issue a condition refresh to make sure we are in a correct state
                        _logger.Information("Issuing ConditionRefresh for item {item} on subscription " +
                            "{subscription} due to receiving a RefreshRequired event",
                            Item.DisplayName ?? "", Item.Subscription.DisplayName);
                        try {
                            Item.Subscription.ConditionRefresh();
                        }
                        catch (ServiceResultException e) {
                            _logger.Information("ConditionRefresh for item {item} on subscription " +
                                "{subscription} failed with a ServiceResultException '{message}'",
                                Item.DisplayName ?? "", Item.Subscription.DisplayName, e.Message);
                            noErrorFound = false;
                        }
                        catch (Exception e) {
                            _logger.Information("ConditionRefresh for item {item} on subscription " +
                                "{subscription} failed with an exception '{message}'",
                                Item.DisplayName ?? "", Item.Subscription.DisplayName, e.Message);
                            noErrorFound = false;
                        }
                        if (noErrorFound) {
                            _logger.Information("ConditionRefresh for item {item} on subscription " +
                                "{subscription} has completed",
                                Item.DisplayName ?? "", Item.Subscription.DisplayName);
                        }
                        return;
                    }
                }

                var monitoredItemNotification = notification.ToMonitoredItemNotification(Item);
                if (message == null) {
                    return;
                }
                var values = monitoredItemNotification.Value.GetValue(typeof(EncodeableDictionary)) as EncodeableDictionary;
                if (pendingAlarmsOptions?.IsEnabled == true && values != null) {
                    if (pendingAlarmsOptions.ConditionIdIndex.HasValue && pendingAlarmsOptions.RetainIndex.HasValue) {
                        var conditionId = values[pendingAlarmsOptions.ConditionIdIndex.Value].Value.ToString();
                        var retain = values[pendingAlarmsOptions.RetainIndex.Value].Value.GetValue(false);
                        lock (_lock) {
                            if (PendingAlarmEvents.ContainsKey(conditionId) && !retain) {
                                PendingAlarmEvents.Remove(conditionId, out var monitoredItemNotificationModel);
                                pendingAlarmsOptions.Dirty = true;
                            }
                            else if (retain) {
                                pendingAlarmsOptions.Dirty = true;
                                PendingAlarmEvents.AddOrUpdate(conditionId, monitoredItemNotification);
                            }
                        }
                    }
                }
                else {
                    message.Notifications?.Add(monitoredItemNotification);
                }
            }

            private void TestWhereClause(IServiceMessageContext messageContext,
                INodeCache nodeCache, EventFilter eventFilter) {
                foreach (var element in eventFilter.WhereClause.Elements) {
                    if (element.FilterOperator == FilterOperator.OfType) {
                        foreach (var filterOperand in element.FilterOperands) {
                            var nodeId = default(NodeId);
                            try {
                                nodeId = (filterOperand.Body as LiteralOperand).Value.ToString().ToNodeId(messageContext);
                                nodeCache.FetchNode(nodeId.ToExpandedNodeId(messageContext.NamespaceUris)); // it will throw an exception if it doesn't work
                            }
                            catch (Exception ex) {
                                _logger.Warning($"Where clause is doing OfType({nodeId}) and we got this message {ex.Message} while looking it up");
                            }
                        }
                    }
                }
            }

            private void OnPendingAlarmsTimerElapsed(object sender, System.Timers.ElapsedEventArgs e) {
                var now = DateTime.UtcNow;
                var pendingAlarmsOptions = EventTemplate.PendingAlarms;
                if (pendingAlarmsOptions?.IsEnabled == true) {
                    try {
                        // is it time to send anything?
                        if (Item.Created &&
                            (now > (_lastSentPendingAlarms + (pendingAlarmsOptions.SnapshotIntervalTimespan ?? TimeSpan.MaxValue))) ||
                                ((now > (_lastSentPendingAlarms + (pendingAlarmsOptions.UpdateIntervalTimespan ?? TimeSpan.MaxValue))) && pendingAlarmsOptions.Dirty)) {
                            SendPendingAlarms();
                            _lastSentPendingAlarms = now;
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "SendPendingAlarms failed.");
                    }
                    finally {
                        _pendingAlarmsTimer.Start();
                    }
                }
            }

            /// <summary>
            /// Send pending alarms
            /// </summary>
            private void SendPendingAlarms() {
                ExtensionObject[] notifications = null;
                uint sequenceNumber;
                lock (_lock) {
                    notifications = PendingAlarmEvents.Values
                        .Select(x => x.Value.Value)
                        .OfType<ExtensionObject>()
                        .ToArray();
                    sequenceNumber = ++_pendingAlarmsSequenceNumber;
                    EventTemplate.PendingAlarms.Dirty = false;
                }

                var pendingAlarmsNotification = new MonitoredItemNotificationModel {
                    AttributeId = Item.AttributeId,
                    DiagnosticInfo = null,
                    DisplayName = Item.DisplayName,
                    Id = Item.DisplayName,
                    IsHeartbeat = false,
                    SequenceNumber = sequenceNumber,
                    NodeId = Item.StartNodeId.ToString(),
                    StringTable = null,
                    Value = new DataValue(notifications)
                };

                var message = new SubscriptionNotificationModel {
                    ServiceMessageContext = Item.Subscription?.Session?.MessageContext,
                    ApplicationUri = Item.Subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = Item.Subscription?.Session?.Endpoint?.EndpointUrl,
                    SubscriptionId = (Item.Subscription?.Handle as SubscriptionWrapper)?.Id,
                    Timestamp = DateTime.UtcNow,
                    Notifications = new List<MonitoredItemNotificationModel>()
                };
                message.Notifications.Add(pendingAlarmsNotification);
                (Item.Subscription?.Handle as SubscriptionWrapper)?.SendMessage(message);
            }

            private void ParseFields(INodeCache nodeCache, List<QualifiedName> fieldNames, Node node, string browsePathPrefix = "") {
                foreach (var reference in node.ReferenceTable) {
                    if (reference.ReferenceTypeId == ReferenceTypeIds.HasComponent && !reference.IsInverse) {
                        var componentNode = nodeCache.FetchNode(reference.TargetId);
                        if (componentNode.NodeClass == Opc.Ua.NodeClass.Variable) {
                            var fieldName = $"{browsePathPrefix}{componentNode.BrowseName.Name}";
                            fieldNames.Add(new QualifiedName(fieldName, componentNode.BrowseName.NamespaceIndex));
                            ParseFields(nodeCache, fieldNames, componentNode, $"{fieldName}|");
                        }
                    }
                    else if (reference.ReferenceTypeId == ReferenceTypeIds.HasProperty) {
                        var propertyNode = nodeCache.FetchNode(reference.TargetId);
                        var fieldName = $"{browsePathPrefix}{propertyNode.BrowseName.Name}";
                        fieldNames.Add(new QualifiedName(fieldName, propertyNode.BrowseName.NamespaceIndex));
                    }
                }
            }

            private Timer _pendingAlarmsTimer;
            private DateTime _lastSentPendingAlarms = DateTime.UtcNow;
            private uint _pendingAlarmsSequenceNumber;
            private HashSet<uint> _newTriggers = new HashSet<uint>();
            private HashSet<uint> _triggers = new HashSet<uint>();
            private Publisher.Models.MonitoringMode? _modeChange;
            private readonly ILogger _logger;
        }

        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IVariantEncoderFactory _codec;
        private readonly IClientServicesConfig _clientConfig;
    }
}
