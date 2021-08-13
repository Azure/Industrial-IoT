// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Prometheus;
    using Opc.Ua.Encoders;
    using Timer = System.Timers.Timer;
    using System.Text;

    /// <summary>
    /// Subscription services implementation
    /// </summary>
    public class SubscriptionServices : ISubscriptionManager, IDisposable {

        /// <inheritdoc/>
        public int TotalSubscriptionCount => _subscriptions.Count;

        /// <summary>
        /// Create subscription manager
        /// </summary>
        /// <param name="sessionManager"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public SubscriptionServices(ISessionManager sessionManager, IVariantEncoderFactory codec,
            ILogger logger) {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task<ISubscription> GetOrCreateSubscriptionAsync(SubscriptionModel subscriptionModel) {
            if (string.IsNullOrEmpty(subscriptionModel?.Id)) {
                throw new ArgumentNullException(nameof(subscriptionModel));
            }
            var sub = _subscriptions.GetOrAdd(subscriptionModel.Id,
                key => new SubscriptionWrapper(this, subscriptionModel, _logger));
            _sessionManager.RegisterSubscription(sub);
            return Task.FromResult<ISubscription>(sub);
        }

        /// <inheritdoc/>
        public void Dispose() {
            // Cleanup remaining subscriptions
            var subscriptions = _subscriptions.Values.ToList();
            _subscriptions.Clear();
            subscriptions.ForEach(s => Try.Op(() => _sessionManager.UnregisterSubscription(s)));
            subscriptions.ForEach(s => Try.Op(() => s.Dispose()));
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
            /// <param name="outer"></param>
            /// <param name="subscription"></param>
            /// <param name="logger"></param>
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
                    _logger.Information("Closing subscription {subscription}", Id);
                    _outer._sessionManager.UnregisterSubscription(this);
                    _outer._subscriptions.TryRemove(Id, out _);
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
                            Try.Op(() => subscription.PublishingEnabled = false);
                            Try.Op(() => subscription.ApplyChanges());
                            Try.Op(() => subscription.DeleteItems());
                            _logger.Debug("Deleted monitored items for {subscription}", Id);
                            Try.Op(() => session?.RemoveSubscription(subscription));
                            _logger.Debug("Subscription successfully removed {subscription}", Id);
                        }
                    }
                }
                catch (Exception e) {
                    _logger.Error(e, "Failed to close subscription {subscription}", Id);
                }
            }

            /// <inheritdoc/>
            public void Dispose() {
                Try.Async(CloseAsync).Wait();
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
            /// reads the display name of the nodes to be monitored
            /// </summary>
            /// <param name="session"></param>
            /// <returns></returns>
            private void ResolveDisplayNames(Session session) {
                if (!(_subscription?.Configuration?.ResolveDisplayName ?? false)) {
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
                                    monitoredItem.StartNodeId, errors[index]);
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
            /// <param name="rawSubscription"></param>
            /// <param name="monitoredItems"></param>
            /// <param name="activate"></param>
            /// <returns></returns>
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
                        foreach (var toRemove in toCleanupList) {
                            _logger.Verbose("Removing monitored item '{item}'...", toRemove.StartNodeId);
                            toRemove.Notification -= OnMonitoredItemChanged;
                            count++;
                        }
                        rawSubscription.RemoveItems(toCleanupList);
                        _logger.Information("Removed {count} monitored items in subscription "
                            + "{subscription}", count, rawSubscription.DisplayName);
                    }
                    _currentlyMonitored = null;
                    rawSubscription.ApplyChanges();
                    rawSubscription.SetPublishingMode(false);
                    if (rawSubscription.MonitoredItemCount != 0) {
                        _logger.Warning("Failed to remove {count} monitored items from subscription "
                            + "{subscription}", rawSubscription.MonitoredItemCount, rawSubscription.DisplayName);
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
                        count++;
                    }
                    rawSubscription.RemoveItems(toRemoveList);
                    applyChanges = true;
                    _logger.Information("Removed {count} monitored items from subscription "
                        + "{subscription}", count, rawSubscription.DisplayName);
                }

                // todo re-associate detached handles!?
                var toRemoveDetached = rawSubscription.MonitoredItems.Where(m => m.Status == null);
                if (toRemoveDetached.Any()) {
                    _logger.Information("Removed {count} detached monitored items from subscription "
                        + "{subscription}", toRemoveDetached.Count(), rawSubscription.DisplayName);
                    rawSubscription.RemoveItems(toRemoveDetached);
                }

                var nowMonitored = new List<MonitoredItemWrapper>();
                var toAddList = desiredState.Except(currentState);
                if (toAddList.Any()) {
                    count = 0;
                    // Add new monitored items not in current state
                    foreach (var toAdd in toAddList) {
                        // Create monitored item
                        if (!activate) {
                            toAdd.Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                        }
                        try {
                            toAdd.Create(rawSubscription.Session.MessageContext, rawSubscription.Session.NodeCache, codec, activate);
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
                        + "{subscription}", count, rawSubscription.DisplayName);
                }

                // Update monitored items that have changed
                var desiredUpdates = desiredState.Intersect(currentState)
                    .ToDictionary(k => k, v => v);
                count = 0;
                foreach (var toUpdate in currentState.Intersect(desiredState)) {
                    if (toUpdate.MergeWith(desiredUpdates[toUpdate])) {
                        _logger.Verbose("Updating monitored item '{item}'...", toUpdate);
                        count++;
                    }
                    nowMonitored.Add(toUpdate);
                }
                if (count > 0) {
                    applyChanges = true;
                    _logger.Information("Updated {count} monitored items in subscription "
                        + "{subscription}", count, rawSubscription.DisplayName);
                }

                if (applyChanges) {
                    rawSubscription.ApplyChanges();
                    _currentlyMonitored = nowMonitored;
                    if (!activate) {
                        var map = _currentlyMonitored.ToDictionary(
                            k => k.Template.Id ?? k.Template.StartNodeId, v => v);
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
                                    new UInt32Collection(added), new UInt32Collection(removed))
                                    .ConfigureAwait(false);
                            }
                        }

                        // sanity check
                        foreach (var monitoredItem in _currentlyMonitored) {
                            if (monitoredItem.Item.Status.Error != null &&
                                StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode)) {
                                _logger.Warning("Error monitoring node {id} due to {code} in subscription " +
                                    "{subscription}", monitoredItem.Item.StartNodeId,
                                    monitoredItem.Item.Status.Error.StatusCode, rawSubscription.DisplayName);
                                monitoredItem.Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                                noErrorFound = false;
                            }
                        }

                        count = _currentlyMonitored.Count(m => m.Item.Status.Error == null);
                        kMonitoredItems.WithLabels(rawSubscription.Id.ToString()).Set(count);

                        _logger.Information("Now monitoring {count} nodes in subscription " +
                            "{subscription}", count, rawSubscription.DisplayName);

                        if (_currentlyMonitored.Count != rawSubscription.MonitoredItemCount) {
                            _logger.Error("Monitored items mismatch: wrappers{wrappers} != items:{items} ",
                                _currentlyMonitored.Count, _currentlyMonitored.Count);
                        }
                    }
                }
                else {
                    // do a sanity check
                    foreach (var monitoredItem in _currentlyMonitored) {
                        if (monitoredItem.Item.Status.Error != null &&
                            StatusCode.IsNotGood(monitoredItem.Item.Status.Error.StatusCode)) {
                            monitoredItem.Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                            noErrorFound = false;
                            applyChanges = true;
                        }
                    }
                    if (applyChanges) {
                        rawSubscription.ApplyChanges();
                    }
                }

                if (activate) {
                    // Change monitoring mode of all valid items if necessary
                    var validItems = _currentlyMonitored.Where(v => v.Item.Status.Error == null);
                    foreach (var change in validItems.GroupBy(i => i.GetMonitoringModeChange())) {
                        if (change.Key == null) {
                            continue;
                        }
                        _logger.Information("Set Monitoring to {value} for {count} nodes in subscription " +
                            "{subscription}", change.Key.Value, change.Count(),
                            rawSubscription.DisplayName);
                        var results = rawSubscription.SetMonitoringMode(change.Key.Value,
                            change.Select(t => t.Item).ToList());
                        if (results != null) {
                            _logger.Information("Failed to set monitoring for {count} nodes in subscription " +
                                "{subscription}",
                                results.Count(r => r != null && StatusCode.IsNotGood(r.StatusCode)),
                                rawSubscription.DisplayName);
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
                }
                return noErrorFound;
            }

            private static uint GreatCommonDivisor(uint a, uint b) {
                return b == 0 ? a : GreatCommonDivisor(b, a % b);
            }

            /// <summary>
            /// Retrieve a raw subscription with all settings applied (no lock)
            /// </summary>
            /// <param name="session"></param>
            /// <param name="configuration"></param>
            /// <param name="activate"></param>
            /// <returns></returns>
            private Subscription GetSubscription(Session session,
                SubscriptionConfigurationModel configuration, bool activate) {

                if (configuration != null) {
                    // Apply new configuration right here saving us from modifying later
                    _subscription.Configuration = configuration.Clone();
                }

                if (session == null) {
                    session = _outer._sessionManager.GetOrCreateSession(_subscription.Connection, true);
                    if (session == null) {
                        return null;
                    }
                }

                // calculate the KeepAliveCount no matter what, perhaps monitored items were changed
                var revisedKeepAliveCount = _subscription.Configuration.KeepAliveCount
                    .GetValueOrDefault(session.DefaultSubscription.KeepAliveCount);
                _subscription.MonitoredItems?.ForEach(m => {
                    if (m is DataMonitoredItemModel dataModel && dataModel.HeartbeatInterval != null && dataModel.HeartbeatInterval != TimeSpan.Zero) {
                        var itemKeepAliveCount = (uint)dataModel.HeartbeatInterval.Value.TotalMilliseconds /
                            (uint)_subscription.Configuration.PublishingInterval.Value.TotalMilliseconds;
                        revisedKeepAliveCount = GreatCommonDivisor(revisedKeepAliveCount, itemKeepAliveCount);
                    }
                });

                var subscription = session.Subscriptions.SingleOrDefault(s => s.Handle == this);
                if (subscription == null) {
                    subscription = new Subscription(session.DefaultSubscription) {
                        Handle = this,
                        DisplayName = Id,
                        PublishingEnabled = activate, // false on initialization
                        KeepAliveCount = revisedKeepAliveCount,
                        FastDataChangeCallback = OnSubscriptionDataChanged,
                        FastEventCallback = OnSubscriptionEventChanged,
                        PublishingInterval = (int)_subscription.Configuration.PublishingInterval
                            .GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds,
                        MaxNotificationsPerPublish = _subscription.Configuration.MaxNotificationsPerPublish
                            .GetValueOrDefault(0),
                        LifetimeCount = _subscription.Configuration.LifetimeCount
                            .GetValueOrDefault(session.DefaultSubscription.LifetimeCount),
                        Priority = _subscription.Configuration.Priority
                            .GetValueOrDefault(session.DefaultSubscription.Priority)
                    };
                    var result = session.AddSubscription(subscription);
                    if (!result) {
                        _logger.Error("Failed to add subscription '{name}' to session '{session}'",
                             Id, session.SessionName);
                        subscription = null;
                    }
                    else {
                        subscription.Create();
                        //TODO - add logs for the revised values
                        _logger.Debug("Added subscription '{name}' to session '{session}'",
                             Id, session.SessionName);
                    }
                }
                else {
                    // Apply new configuration on configuration on original subscription
                    var modifySubscription = false;

                    if (revisedKeepAliveCount != subscription.KeepAliveCount) {
                        _logger.Debug(
                            "{subscription} Changing KeepAlive Count from {old} to {new}",
                            _subscription.Id, _subscription.Configuration?.KeepAliveCount ?? 0,
                            revisedKeepAliveCount);

                        subscription.KeepAliveCount = revisedKeepAliveCount;
                        modifySubscription = true;
                    }
                    if (subscription.PublishingInterval != (int)_subscription.Configuration.PublishingInterval
                            .GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds) {
                        _logger.Debug(
                            "{subscription} Changing publishing interval from {old} to {new}",
                            _subscription.Id,
                            configuration?.PublishingInterval ?? TimeSpan.Zero);
                        subscription.PublishingInterval = (int)_subscription.Configuration.PublishingInterval
                            .GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds;

                        modifySubscription = true;
                    }
                    if (subscription.MaxNotificationsPerPublish !=
                            _subscription.Configuration.MaxNotificationsPerPublish.GetValueOrDefault(0)) {
                        _logger.Debug(
                            "{subscription} Changing Max NotificationsPerPublish from {old} to {new}",
                            _subscription.Id, _subscription.Configuration?.MaxNotificationsPerPublish ?? 0,
                            configuration?.MaxNotificationsPerPublish ?? 0);
                        subscription.MaxNotificationsPerPublish =
                            _subscription.Configuration.MaxNotificationsPerPublish.GetValueOrDefault(0);
                        modifySubscription = true;
                    }
                    if (subscription.LifetimeCount != _subscription.Configuration.LifetimeCount
                            .GetValueOrDefault(session.DefaultSubscription.LifetimeCount)) {
                        _logger.Debug(
                            "{subscription} Changing Lifetime Count from {old} to {new}",
                            _subscription.Id, _subscription.Configuration?.LifetimeCount ?? 0,
                            configuration?.LifetimeCount ?? 0);
                        subscription.LifetimeCount = _subscription.Configuration.LifetimeCount
                            .GetValueOrDefault(session.DefaultSubscription.LifetimeCount);
                        modifySubscription = true;
                    }
                    if (subscription.Priority != _subscription.Configuration.Priority
                            .GetValueOrDefault(session.DefaultSubscription.Priority)) {
                        _logger.Debug("{subscription} Changing Priority from {old} to {new}",
                            _subscription.Id, _subscription.Configuration?.Priority ?? 0,
                            configuration?.Priority ?? 0);
                        subscription.Priority = _subscription.Configuration.Priority
                            .GetValueOrDefault(session.DefaultSubscription.Priority);
                        modifySubscription = true;
                    }
                    if (modifySubscription) {
                        subscription.Modify();
                        //Todo - add logs for the revised values
                    }
                    if (subscription.CurrentPublishingEnabled != activate) {
                        // do not deactivate an already activated subscription
                        subscription.SetPublishingMode(activate);
                    }
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
                                CompressedPayload = false,
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
            /// <param name="subscription"></param>
            /// <param name="notification"></param>
            /// <param name="stringTable"></param>
            private void OnSubscriptionDataChanged(Subscription subscription,
                DataChangeNotification notification, IList<string> stringTable) {
                try {
                    if (OnSubscriptionDataChange == null) {
                        return;
                    }
                    if (notification == null) {
                        _logger.Warning(
                            "DataChange for subscription: {Subscription} having empty notification",
                            subscription.DisplayName);
                        return;
                    }

                    if (_currentlyMonitored == null) {
                        _logger.Information(
                            "DataChange for subscription: {Subscription} having no monitored items yet",
                            subscription.DisplayName);
                        return;
                    }

                    // check if notification is a keep alive
                    var isKeepAlive = notification?.MonitoredItems?.Count == 1 &&
                                      notification?.MonitoredItems?.First().ClientHandle == 0 &&
                                      notification?.MonitoredItems?.First().Message?.NotificationData?.Count == 0;
                    var sequenceNumber = notification?.MonitoredItems?.First().Message?.SequenceNumber;
                    var publishTime = (notification?.MonitoredItems?.First().Message?.PublishTime).
                        GetValueOrDefault(DateTime.UtcNow);

                    _logger.Debug("DataChange for subscription: {Subscription}, sequence#: " +
                        "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                        subscription.DisplayName, sequenceNumber, isKeepAlive, publishTime);

                    var message = new SubscriptionNotificationModel {
                        ServiceMessageContext = subscription?.Session?.MessageContext,
                        ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                        SubscriptionId = Id,
                        Timestamp = publishTime,
                        CompressedPayload = false,
                        Notifications = notification.ToMonitoredItemNotifications(
                                subscription?.MonitoredItems)?.ToList()
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
                                Exists(m => m.ClientHandle == item.Item.ClientHandle)) {
                                if (item.ValidateHeartbeat(publishTime)) {
                                    var defaultNotification =
                                        new MonitoredItemNotificationModel {
                                            Id = item.Item.DisplayName,
                                            DisplayName = item.Item.DisplayName,
                                            NodeId = item.Item.StartNodeId,
                                            AttributeId = item.Item.AttributeId,
                                            ClientHandle = item.Item.ClientHandle,
                                            Value = new DataValue(Variant.Null,
                                                item.Item?.Status?.Error?.StatusCode ??
                                                StatusCodes.BadMonitoredItemIdInvalid),
                                            Overflow = false,
                                            NotificationData = null,
                                            StringTable = null,
                                            DiagnosticInfo = null,
                                        };

                                    var heartbeatValue = item.Item?.LastValue.
                                        ToMonitoredItemNotification(item.Item, () => defaultNotification);
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
                            }
                            item.ValidateHeartbeat(publishTime);
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
            /// <param name="monitoredItem"></param>
            /// <param name="eventArgs"></param>
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
            public Dictionary<string, MonitoredItemNotificationModel> PendingAlarmEvents { get; } = new Dictionary<string, MonitoredItemNotificationModel>();

            /// <summary>
            /// Property setter that gets indication if item is online or not.
            /// </summary>
            public void OnMonitoredItemStateChanged(bool online) {
                if (EventTemplate?.PendingAlarms?.IsEnabled == true && online) {
                    _pendingAlarmsTimer.Start();
                }
                else {
                    _pendingAlarmsTimer.Stop();
                    lock (_lock) {
                        PendingAlarmEvents.Clear();
                    }
                }
            }

            private readonly Object _lock = new object();

            /// <summary>
            /// Destructor for this class
            /// </summary>
            ~MonitoredItemWrapper() {
                _pendingAlarmsTimer.Stop();
                _pendingAlarmsTimer.Dispose();
            }

            /// <summary>
            /// validates if a heartbeat is required.
            /// A heartbeat will be forced for the very first time
            /// </summary>
            /// <returns></returns>
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
            /// <param name="template"></param>
            /// <param name="logger"></param>
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
                return $"Item {Template.Id ?? "<unknown>"}{ServerId}: '{Template.StartNodeId}'" +
                    $" - {(Item?.Status?.Created == true ? "" : "not ")}created";
            }

            /// <summary>
            /// Create new stack monitored item
            /// </summary>
            /// <param name="messageContext"></param>
            /// <param name="nodeCache"></param>
            /// <param name="codec"></param>
            /// <param name="activate"></param>
            /// <returns></returns>
            public void Create(ServiceMessageContext messageContext, INodeCache nodeCache, IVariantEncoder codec, bool activate) {
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
                    QueueSize = Template.QueueSize.GetValueOrDefault(1),
                    SamplingInterval = (int)Template.SamplingInterval.
                        GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds,
                    DiscardOldest = !Template.DiscardNew.GetValueOrDefault(false),
                };

                if (DataTemplate != null) {
                    Item.Filter = DataTemplate.DataChangeFilter.ToStackModel() ??
                        ((MonitoringFilter)DataTemplate.AggregateFilter.ToStackModel(messageContext));
                }
                else if (EventTemplate != null) {
                    var eventFilter = new EventFilter();
                    if (EventTemplate.EventFilter != null) {
                        if (!string.IsNullOrEmpty(EventTemplate.EventFilter.TypeDefinitionId)) {
                            eventFilter = GetSimpleEventFilter(nodeCache, messageContext);
                        }
                        else {
                            eventFilter = codec.Decode(EventTemplate.EventFilter, true);
                        }
                    }

                    Task.Run(() => {
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
                    });

                    // let's keep track of the internal fields we add so that they don't show up in the output
                    var internalSelectClauses = new List<SimpleAttributeOperand>();

                    // Add SourceTimestamp and ServerTimestamp select clauses.
                    if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType && x.BrowsePath?.FirstOrDefault() == BrowseNames.Time)) {
                        var selectClause = new SimpleAttributeOperand(ObjectTypeIds.BaseEventType, BrowseNames.Time);
                        eventFilter.SelectClauses.Add(selectClause);
                        internalSelectClauses.Add(selectClause);
                    }
                    if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType && x.BrowsePath?.FirstOrDefault() == BrowseNames.ReceiveTime)) {
                        var selectClause = new SimpleAttributeOperand(ObjectTypeIds.BaseEventType, BrowseNames.ReceiveTime);
                        eventFilter.SelectClauses.Add(selectClause);
                        internalSelectClauses.Add(selectClause);
                    }
                    if (!eventFilter.SelectClauses.Any(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType)) {
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
                            EventTemplate.PendingAlarms.ConditionIdIndex = eventFilter.SelectClauses.Count();
                            var selectClause = new SimpleAttributeOperand() {
                                BrowsePath = new QualifiedNameCollection(),
                                TypeDefinitionId = ObjectTypeIds.ConditionType,
                                AttributeId = Attributes.NodeId
                            };
                            eventFilter.SelectClauses.Add(selectClause);
                            internalSelectClauses.Add(selectClause);
                        }

                        var retainClause = eventFilter.SelectClauses
                            .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType && x.BrowsePath?.FirstOrDefault() == BrowseNames.Retain);
                        if (retainClause != null) {
                            EventTemplate.PendingAlarms.RetainIndex = eventFilter.SelectClauses.IndexOf(retainClause);
                        }
                        else {
                            EventTemplate.PendingAlarms.RetainIndex = eventFilter.SelectClauses.Count();
                            var selectClause = new SimpleAttributeOperand(ObjectTypeIds.ConditionType, BrowseNames.Retain);
                            eventFilter.SelectClauses.Add(selectClause);
                            internalSelectClauses.Add(selectClause);
                        }

                        // set up the timer
                        _pendingAlarmsTimer.Interval = 1000;
                        _pendingAlarmsTimer.Elapsed += OnPendingAlarmsTimerElapsed;
                        _pendingAlarmsTimer.AutoReset = false;
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
                                            sb.Append("#");
                                        }
                                        else {
                                            sb.Append($"{selectClause.BrowsePath[i].NamespaceIndex}:");
                                        }
                                    }
                                }
                                else {
                                    sb.Append("/");
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
                            FieldNames.Add("");
                        }
                    }

                    Item.Filter = eventFilter;
                }
            }

            private void OnPendingAlarmsTimerElapsed(object sender, System.Timers.ElapsedEventArgs e) {
                var now = DateTime.UtcNow;
                var pendingAlarmsOptions = EventTemplate.PendingAlarms;
                if (pendingAlarmsOptions?.IsEnabled == true) {
                    try {
                        // is it time to send anything?
                        if (Item.Created && pendingAlarmsOptions.IsEnabled == true &&
                            (((now > (_lastSentPendingAlarms + (pendingAlarmsOptions.SnapshotIntervalTimespan ?? TimeSpan.MaxValue))) ||
                                ((now > (_lastSentPendingAlarms + (pendingAlarmsOptions.UpdateIntervalTimespan ?? TimeSpan.MaxValue))) &&
                                pendingAlarmsOptions.Dirty)))) {
                            SendPendingAlarms();
                            _lastSentPendingAlarms = now;
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error("SendPendingAlarms failed with exception {message}.", ex.Message);
                    }
                    finally {
                        _pendingAlarmsTimer.Start();
                    }
                }
            }

            /// <summary>
            /// Add the monitored item identifier of the triggering item.
            /// </summary>
            /// <param name="id"></param>
            internal void AddTriggerLink(uint? id) {
                if (id != null) {
                    _newTriggers.Add(id.Value);
                }
            }

            /// <summary>
            /// Merge with desired state
            /// </summary>
            /// <param name="model"></param>
            internal bool MergeWith(MonitoredItemWrapper model) {

                if (model == null || Item == null) {
                    return false;
                }

                var changes = false;
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
                    changes = true;
                }
                if (Template.DiscardNew.GetValueOrDefault(false) !=
                        model.Template.DiscardNew.GetValueOrDefault()) {
                    _logger.Debug("{item}: Changing discard new mode from {old} to {new}",
                        this, Template.DiscardNew.GetValueOrDefault(false),
                        model.Template.DiscardNew.GetValueOrDefault(false));
                    Template.DiscardNew = model.Template.DiscardNew;
                    Item.DiscardOldest = !Template.DiscardNew.GetValueOrDefault(false);
                    changes = true;
                }
                if (Template.QueueSize.GetValueOrDefault(1) !=
                    model.Template.QueueSize.GetValueOrDefault(1)) {
                    _logger.Debug("{item}: Changing queue size from {old} to {new}",
                        this, Template.QueueSize.GetValueOrDefault(1),
                        model.Template.QueueSize.GetValueOrDefault(1));
                    Template.QueueSize = model.Template.QueueSize;
                    Item.QueueSize = Template.QueueSize.GetValueOrDefault(1);
                    changes = true;
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
                    changes = true;
                }

                // TODO
                // monitoredItem.Filter = monitoredItemInfo.Filter?.ToStackType();
                return changes;
            }

            /// <summary>
            /// Get triggering configuration changes for this item
            /// </summary>
            /// <param name="addLinks"></param>
            /// <param name="removeLinks"></param>
            /// <returns></returns>
            internal bool GetTriggeringLinks(out IEnumerable<uint> addLinks,
                out IEnumerable<uint> removeLinks) {
                var remove = _triggers.Except(_newTriggers).ToList();
                var add = _newTriggers.Except(_triggers).ToList();
                _triggers = _newTriggers;
                _newTriggers = new HashSet<uint>();
                addLinks = add;
                removeLinks = remove;
                if (add.Count > 0 || remove.Count > 0) {
                    _logger.Debug("{item}: Adding {add} links and removing {remove} links",
                        this, add.Count, remove.Count);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Get any changes in the monitoring mode
            /// </summary>
            /// <returns></returns>
            internal Opc.Ua.MonitoringMode? GetMonitoringModeChange() {
                var change = _modeChange.ToStackType();
                _modeChange = null;
                return Item.MonitoringMode == change ? null : change;
            }

            /// <summary>
            /// Builds select clause and where clause by using OPC UA reflection
            /// </summary>
            /// <param name="nodeCache"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            public EventFilter GetSimpleEventFilter(INodeCache nodeCache, ServiceMessageContext context) {
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
                        .FirstOrDefault(x => x.TypeDefinitionId == ObjectTypeIds.BaseEventType && x.BrowsePath?.FirstOrDefault() == BrowseNames.EventType));

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
                        }
                        return;
                    }
                    else if (eventType == ObjectTypeIds.RefreshEndEventType) {
                        if (pendingAlarmsOptions?.IsEnabled == true) {
                            // restart the timers once condition refresh is done.
                            _pendingAlarmsTimer.Start();
                        }
                        return;
                    }
                    else if (eventType == ObjectTypeIds.RefreshRequiredEventType) {
                        var noErrorFound = true;

                        // issue a condition refresh to make sure we are in a correct state
                        _logger.Information("Now issuing ConditionRefresh for item {item} on subscription " +
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

                var monitoredItemNotification = notification
                .ToMonitoredItemNotification(Item);
                if (message == null) {
                    return;
                }
                if (pendingAlarmsOptions?.IsEnabled == true && monitoredItemNotification.Value.GetValue(typeof(EncodeableDictionary)) is EncodeableDictionary values) {
                    if (pendingAlarmsOptions.ConditionIdIndex.HasValue && pendingAlarmsOptions.RetainIndex.HasValue) {
                        var conditionId = values[pendingAlarmsOptions.ConditionIdIndex.Value].Value.ToString();
                        var retain = values[pendingAlarmsOptions.RetainIndex.Value].Value.GetValue<bool>(false);
                        lock (_lock) {
                            if (PendingAlarmEvents.ContainsKey(conditionId) && !retain) {
                                PendingAlarmEvents.Remove(conditionId, out var monitoredItemNotificationModel);
                                pendingAlarmsOptions.Dirty = true;
                            }
                            else if (retain) {
                                pendingAlarmsOptions.Dirty = true;
                                PendingAlarmEvents[conditionId] = monitoredItemNotification;
                            }
                        }
                    }
                }
                else {
                    message.Notifications?.Add(monitoredItemNotification);
                }
            }

            private void SendPendingAlarms() {
                List<MonitoredItemNotificationModel> notifications = null;
                lock (_lock) {
                    notifications = new List<MonitoredItemNotificationModel>(PendingAlarmEvents.Values);
                    EventTemplate.PendingAlarms.Dirty = false;
                }

                var firstNotification = notifications.FirstOrDefault();
                var pendingAlarmsNotification = new MonitoredItemNotificationModel() {
                    AttributeId = Item.AttributeId,
                    ClientHandle = firstNotification?.ClientHandle ?? 0,
                    DiagnosticInfo = null,
                    DisplayName = Item.DisplayName,
                    Id = Item.DisplayName,
                    IsHeartbeat = false,
                    SequenceNumber = null,
                    NodeId = Item.StartNodeId,
                    StringTable = null,
                    Value = new DataValue(notifications.Select(x => x.Value.Value).OfType<ExtensionObject>().ToArray())
                };

                var message = new SubscriptionNotificationModel {
                    ServiceMessageContext = Item.Subscription?.Session?.MessageContext,
                    ApplicationUri = Item.Subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                    EndpointUrl = Item.Subscription?.Session?.Endpoint?.EndpointUrl,
                    SubscriptionId = (Item.Subscription?.Handle as SubscriptionWrapper)?.Id,
                    Timestamp = DateTime.UtcNow,
                    CompressedPayload = EventTemplate.PendingAlarms.CompressedPayload,
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

            private readonly Timer _pendingAlarmsTimer = new Timer();
            private DateTime _lastSentPendingAlarms = DateTime.UtcNow;
            private HashSet<uint> _newTriggers = new HashSet<uint>();
            private HashSet<uint> _triggers = new HashSet<uint>();
            private Publisher.Models.MonitoringMode? _modeChange;
            private readonly ILogger _logger;
        }

        private readonly ILogger _logger;
        // TODO - check if we still need this list here
        private readonly ConcurrentDictionary<string, SubscriptionWrapper> _subscriptions =
            new ConcurrentDictionary<string, SubscriptionWrapper>();
        private readonly ISessionManager _sessionManager;
        private readonly IVariantEncoderFactory _codec;
    }
}