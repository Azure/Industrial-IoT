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
            return (monitoredItem.Handle as MonitoredItemWrapper)?.GetFieldDisplayName(index);
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

                    _logger.Debug("Event for subscription: {Subscription}, sequence#: " +
                        "{Sequence} isKeepAlive: {KeepAlive}, publishTime: {PublishTime}",
                        subscription.DisplayName, sequenceNumber, isKeepAlive, publishTime);

                    var message = new SubscriptionNotificationModel {
                        ServiceMessageContext = subscription.Session?.MessageContext,
                        ApplicationUri = subscription.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription.Session?.Endpoint?.EndpointUrl,
                        SubscriptionId = Id,
                        Timestamp = publishTime,
                        Notifications = notification.ToMonitoredItemNotifications(
                                subscription.MonitoredItems)?.ToList()
                    };

                    if (message.Notifications?.Any() == true) {
                        OnSubscriptionEventChange.Invoke(this, message);
                    }
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
        private class MonitoredItemWrapper {

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
            /// <param name="session"></param>
            /// <param name="codec"></param>
            /// <param name="activate"></param>
            /// <returns></returns>
            internal void Create(Session session, IVariantEncoder codec, bool activate) {
                Item = new MonitoredItem {
                    Handle = this,
                    DisplayName = Template.DisplayName ?? Template.Id,
                    AttributeId = (uint)Template.AttributeId.GetValueOrDefault((NodeAttribute)Attributes.Value),
                    IndexRange = Template.IndexRange,
                    RelativePath = Template.RelativePath?
                                .ToRelativePath(session.MessageContext)?
                                .Format(session.NodeCache.TypeTree),
                    MonitoringMode = activate
                        ? Template.MonitoringMode.ToStackType().
                            GetValueOrDefault(Opc.Ua.MonitoringMode.Reporting)
                        : Opc.Ua.MonitoringMode.Disabled,
                    StartNodeId = Template.StartNodeId.ToNodeId(session.MessageContext),
                    QueueSize = Template.QueueSize.GetValueOrDefault(1),
                    SamplingInterval = (int)Template.SamplingInterval.
                        GetValueOrDefault(TimeSpan.FromSeconds(1)).TotalMilliseconds,
                    DiscardOldest = !Template.DiscardNew.GetValueOrDefault(false),
                };

                if (DataTemplate != null) {
                    Item.Filter = DataTemplate.DataChangeFilter.ToStackModel() ??
                        ((MonitoringFilter)DataTemplate.AggregateFilter.ToStackModel(session.MessageContext));
                }
                else if (EventTemplate != null) {
                    var eventFilter = !string.IsNullOrEmpty(EventTemplate.EventFilter.EventTypeDefinitionId) ?
                        BuildBasicEventFilter(session) :
                        codec.Decode(EventTemplate.EventFilter, true);
                    if (EventTemplate.PendingAlarms != null && EventTemplate.PendingAlarms.IsEnabled) {
                        if (!eventFilter.SelectClauses
                            .Where(x => x.TypeDefinitionId == ObjectTypeIds.ConditionType && x.AttributeId == Attributes.NodeId)
                            .Any()) {
                            eventFilter.SelectClauses.Add(new SimpleAttributeOperand() {
                                BrowsePath = new QualifiedNameCollection(),
                                TypeDefinitionId = ObjectTypeIds.ConditionType,
                                AttributeId = Attributes.NodeId
                            });
                        }
                    }
                    Item.Filter = eventFilter;
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

            internal string GetFieldDisplayName(int index) {
                var fieldName = EventTemplate?.EventFilter?.SelectClauses?[index]?.DisplayName;
                if (fieldName == null) {
                    fieldName = Item.GetFieldName(index);
                    if (!string.IsNullOrEmpty(fieldName) && fieldName[0] == '/') {
                        fieldName = fieldName[1..];
                    }
                }

                return fieldName;
            }

            internal EventFilter BuildBasicEventFilter(Session session) {
                var simpleEventTypeNodeId = NodeId.Parse(EventTemplate.EventFilter.EventTypeDefinitionId);
                var fields = ClientUtils.CollectInstanceDeclarationsForType(session, simpleEventTypeNodeId);

                var eventFilter = new EventFilter();
                foreach (var field in fields) {
                    eventFilter.SelectClauses.Add(new SimpleAttributeOperand() {
                        TypeDefinitionId = ObjectTypeIds.BaseEventType,
                        BrowsePath = field.BrowsePath,
                        AttributeId = (field.NodeClass == Opc.Ua.NodeClass.Variable) ? Attributes.Value : Attributes.NodeId
                    });
                }
                eventFilter.WhereClause = new ContentFilter();
                eventFilter.WhereClause.Push(FilterOperator.OfType, simpleEventTypeNodeId);

                return eventFilter;
            }

            private HashSet<uint> _newTriggers = new HashSet<uint>();
            private HashSet<uint> _triggers = new HashSet<uint>();
            private Publisher.Models.MonitoringMode? _modeChange;
            private readonly ILogger _logger;
        }

        internal static class ClientUtils {
            #region Browse
            /// <summary>
            /// Browses the address space and returns the references found.
            /// </summary>
            /// <param name="session">The session.</param>
            /// <param name="nodesToBrowse">The set of browse operations to perform.</param>
            /// <param name="throwOnError">if set to <c>true</c> a exception will be thrown on an error.</param>
            /// <returns>
            /// The references found. Null if an error occurred.
            /// </returns>
            public static ReferenceDescriptionCollection Browse(Session session, BrowseDescriptionCollection nodesToBrowse, bool throwOnError) {
                return Browse(session, null, nodesToBrowse, throwOnError);
            }

            /// <summary>
            /// Browses the address space and returns the references found.
            /// </summary>
            public static ReferenceDescriptionCollection Browse(Session session, ViewDescription view, BrowseDescriptionCollection nodesToBrowse, bool throwOnError) {
                try {
                    ReferenceDescriptionCollection references = new ReferenceDescriptionCollection();
                    BrowseDescriptionCollection unprocessedOperations = new BrowseDescriptionCollection();

                    while (nodesToBrowse.Count > 0) {
                        // start the browse operation.
                        BrowseResultCollection results = null;
                        DiagnosticInfoCollection diagnosticInfos = null;

                        session.Browse(
                            null,
                            view,
                            0,
                            nodesToBrowse,
                            out results,
                            out diagnosticInfos);

                        ClientBase.ValidateResponse(results, nodesToBrowse);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);

                        ByteStringCollection continuationPoints = new ByteStringCollection();

                        for (int ii = 0; ii < nodesToBrowse.Count; ii++) {
                            // check for error.
                            if (StatusCode.IsBad(results[ii].StatusCode)) {
                                // this error indicates that the server does not have enough simultaneously active 
                                // continuation points. This request will need to be resent after the other operations
                                // have been completed and their continuation points released.
                                if (results[ii].StatusCode == StatusCodes.BadNoContinuationPoints) {
                                    unprocessedOperations.Add(nodesToBrowse[ii]);
                                }

                                continue;
                            }

                            // check if all references have been fetched.
                            if (results[ii].References.Count == 0) {
                                continue;
                            }

                            // save results.
                            references.AddRange(results[ii].References);

                            // check for continuation point.
                            if (results[ii].ContinuationPoint != null) {
                                continuationPoints.Add(results[ii].ContinuationPoint);
                            }
                        }

                        // process continuation points.
                        ByteStringCollection revisedContiuationPoints = new ByteStringCollection();

                        while (continuationPoints.Count > 0) {
                            // continue browse operation.
                            session.BrowseNext(
                                null,
                                false,
                                continuationPoints,
                                out results,
                                out diagnosticInfos);

                            ClientBase.ValidateResponse(results, continuationPoints);
                            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);

                            for (int ii = 0; ii < continuationPoints.Count; ii++) {
                                // check for error.
                                if (StatusCode.IsBad(results[ii].StatusCode)) {
                                    continue;
                                }

                                // check if all references have been fetched.
                                if (results[ii].References.Count == 0) {
                                    continue;
                                }

                                // save results.
                                references.AddRange(results[ii].References);

                                // check for continuation point.
                                if (results[ii].ContinuationPoint != null) {
                                    revisedContiuationPoints.Add(results[ii].ContinuationPoint);
                                }
                            }

                            // check if browsing must continue;
                            revisedContiuationPoints = continuationPoints;
                        }

                        // check if unprocessed results exist.
                        nodesToBrowse = unprocessedOperations;
                    }

                    // return complete list.
                    return references;
                }
                catch (Exception exception) {
                    if (throwOnError) {
                        throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
                    }

                    return null;
                }
            }

            /// <summary>
            /// Browses the address space and returns the references found.
            /// </summary>
            /// <param name="session">The session.</param>
            /// <param name="nodeToBrowse">The NodeId for the starting node.</param>
            /// <param name="throwOnError">if set to <c>true</c> a exception will be thrown on an error.</param>
            /// <returns>
            /// The references found. Null if an error occurred.
            /// </returns>
            public static ReferenceDescriptionCollection Browse(Session session, BrowseDescription nodeToBrowse, bool throwOnError) {
                return Browse(session, null, nodeToBrowse, throwOnError);
            }

            /// <summary>
            /// Browses the address space and returns the references found.
            /// </summary>
            public static ReferenceDescriptionCollection Browse(Session session, ViewDescription view, BrowseDescription nodeToBrowse, bool throwOnError) {
                try {
                    ReferenceDescriptionCollection references = new ReferenceDescriptionCollection();

                    // construct browse request.
                    BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection();
                    nodesToBrowse.Add(nodeToBrowse);

                    // start the browse operation.
                    BrowseResultCollection results = null;
                    DiagnosticInfoCollection diagnosticInfos = null;

                    session.Browse(
                        null,
                        view,
                        0,
                        nodesToBrowse,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, nodesToBrowse);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);

                    do {
                        // check for error.
                        if (StatusCode.IsBad(results[0].StatusCode)) {
                            throw new ServiceResultException(results[0].StatusCode);
                        }

                        // process results.
                        for (int ii = 0; ii < results[0].References.Count; ii++) {
                            references.Add(results[0].References[ii]);
                        }

                        // check if all references have been fetched.
                        if (results[0].References.Count == 0 || results[0].ContinuationPoint == null) {
                            break;
                        }

                        // continue browse operation.
                        ByteStringCollection continuationPoints = new ByteStringCollection();
                        continuationPoints.Add(results[0].ContinuationPoint);

                        session.BrowseNext(
                            null,
                            false,
                            continuationPoints,
                            out results,
                            out diagnosticInfos);

                        ClientBase.ValidateResponse(results, continuationPoints);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);
                    }
                    while (true);

                    //return complete list.
                    return references;
                }
                catch (Exception exception) {
                    if (throwOnError) {
                        throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
                    }

                    return null;
                }
            }

            /// <summary>
            /// Browses the address space and returns all of the supertypes of the specified type node.
            /// </summary>
            /// <param name="session">The session.</param>
            /// <param name="typeId">The NodeId for a type node in the address space.</param>
            /// <param name="throwOnError">if set to <c>true</c> a exception will be thrown on an error.</param>
            /// <returns>
            /// The references found. Null if an error occurred.
            /// </returns>
            public static ReferenceDescriptionCollection BrowseSuperTypes(Session session, NodeId typeId, bool throwOnError) {
                ReferenceDescriptionCollection supertypes = new ReferenceDescriptionCollection();

                try {
                    // find all of the children of the field.
                    BrowseDescription nodeToBrowse = new BrowseDescription();

                    nodeToBrowse.NodeId = typeId;
                    nodeToBrowse.BrowseDirection = Opc.Ua.BrowseDirection.Inverse;
                    nodeToBrowse.ReferenceTypeId = ReferenceTypeIds.HasSubtype;
                    nodeToBrowse.IncludeSubtypes = false; // more efficient to use IncludeSubtypes=False when possible.
                    nodeToBrowse.NodeClassMask = 0; // the HasSubtype reference already restricts the targets to Types. 
                    nodeToBrowse.ResultMask = (uint)BrowseResultMask.All;

                    ReferenceDescriptionCollection references = Browse(session, nodeToBrowse, throwOnError);

                    while (references != null && references.Count > 0) {
                        // should never be more than one supertype.
                        supertypes.Add(references[0]);

                        // only follow references within this server.
                        if (references[0].NodeId.IsAbsolute) {
                            break;
                        }

                        // get the references for the next level up.
                        nodeToBrowse.NodeId = (NodeId)references[0].NodeId;
                        references = Browse(session, nodeToBrowse, throwOnError);
                    }

                    // return complete list.
                    return supertypes;
                }
                catch (Exception exception) {
                    if (throwOnError) {
                        throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
                    }

                    return null;
                }
            }

            /// <summary>
            /// Returns the node ids for a set of relative paths.
            /// </summary>
            /// <param name="session">An open session with the server to use.</param>
            /// <param name="startNodeId">The starting node for the relative paths.</param>
            /// <param name="namespacesUris">The namespace URIs referenced by the relative paths.</param>
            /// <param name="relativePaths">The relative paths.</param>
            /// <returns>A collection of local nodes.</returns>
            public static List<NodeId> TranslateBrowsePaths(
                Session session,
                NodeId startNodeId,
                NamespaceTable namespacesUris,
                params string[] relativePaths) {
                // build the list of browse paths to follow by parsing the relative paths.
                BrowsePathCollection browsePaths = new BrowsePathCollection();

                if (relativePaths != null) {
                    for (int ii = 0; ii < relativePaths.Length; ii++) {
                        BrowsePath browsePath = new BrowsePath();

                        // The relative paths used indexes in the namespacesUris table. These must be 
                        // converted to indexes used by the server. An error occurs if the relative path
                        // refers to a namespaceUri that the server does not recognize.

                        // The relative paths may refer to ReferenceType by their BrowseName. The TypeTree object
                        // allows the parser to look up the server's NodeId for the ReferenceType.

                        browsePath.RelativePath = RelativePath.Parse(
                            relativePaths[ii],
                            session.TypeTree,
                            namespacesUris,
                            session.NamespaceUris);

                        browsePath.StartingNode = startNodeId;

                        browsePaths.Add(browsePath);
                    }
                }

                // make the call to the server.
                BrowsePathResultCollection results;
                DiagnosticInfoCollection diagnosticInfos;

                ResponseHeader responseHeader = session.TranslateBrowsePathsToNodeIds(
                    null,
                    browsePaths,
                    out results,
                    out diagnosticInfos);

                // ensure that the server returned valid results.
                Session.ValidateResponse(results, browsePaths);
                Session.ValidateDiagnosticInfos(diagnosticInfos, browsePaths);

                // collect the list of node ids found.
                List<NodeId> nodes = new List<NodeId>();

                for (int ii = 0; ii < results.Count; ii++) {
                    // check if the start node actually exists.
                    if (StatusCode.IsBad(results[ii].StatusCode)) {
                        nodes.Add(null);
                        continue;
                    }

                    // an empty list is returned if no node was found.
                    if (results[ii].Targets.Count == 0) {
                        nodes.Add(null);
                        continue;
                    }

                    // Multiple matches are possible, however, the node that matches the type model is the
                    // one we are interested in here. The rest can be ignored.
                    BrowsePathTarget target = results[ii].Targets[0];

                    if (target.RemainingPathIndex != UInt32.MaxValue) {
                        nodes.Add(null);
                        continue;
                    }

                    // The targetId is an ExpandedNodeId because it could be node in another server. 
                    // The ToNodeId function is used to convert a local NodeId stored in a ExpandedNodeId to a NodeId.
                    nodes.Add(ExpandedNodeId.ToNodeId(target.TargetId, session.NamespaceUris));
                }

                // return whatever was found.
                return nodes;
            }
            #endregion

            #region Type Model Browsing

            /// <summary>
            /// Stores an instance declaration fetched from the server.
            /// </summary>
            public class InstanceDeclaration {
                /// <summary>
                /// The browse path to the instance declaration.
                /// </summary>
                public QualifiedNameCollection BrowsePath;

                /// <summary>
                /// The browse path to the instance declaration.
                /// </summary>
                public string BrowsePathDisplayText;

                /// <summary>
                /// The node id for the instance declaration.
                /// </summary>
                public NodeId NodeId;

                /// <summary>
                /// The node class of the instance declaration.
                /// </summary>
                public Opc.Ua.NodeClass NodeClass;

                /// <summary>
                /// The modelling rule for the instance declaration (i.e. Mandatory or Optional).
                /// </summary>
                public NodeId ModellingRule;

                /// <summary>
                /// The data type for the instance declaration.
                /// </summary>
                public NodeId DataType;

                /// <summary>
                /// The value rank for the instance declaration.
                /// </summary>
                public int ValueRank;
            }


            /// <summary>
            /// Collects the instance declarations for a type.
            /// </summary>
            public static List<InstanceDeclaration> CollectInstanceDeclarationsForType(Session session, NodeId typeId) {
                return CollectInstanceDeclarationsForType(session, typeId, true);
            }

            /// <summary>
            /// Collects the instance declarations for a type.
            /// </summary>
            public static List<InstanceDeclaration> CollectInstanceDeclarationsForType(Session session, NodeId typeId, bool includeSupertypes) {
                // process the types starting from the top of the tree.
                List<InstanceDeclaration> instances = new List<InstanceDeclaration>();
                Dictionary<string, InstanceDeclaration> map = new Dictionary<string, InstanceDeclaration>();

                // get the supertypes.
                if (includeSupertypes) {
                    ReferenceDescriptionCollection supertypes = BrowseSuperTypes(session, typeId, false);

                    if (supertypes != null) {
                        for (int ii = supertypes.Count - 1; ii >= 0; ii--) {
                            CollectInstanceDeclarations(session, (NodeId)supertypes[ii].NodeId, null, instances, map);
                        }
                    }
                }

                // collect the fields for the selected type.
                CollectInstanceDeclarations(session, typeId, null, instances, map);

                // return the complete list.
                return instances;
            }

            /// <summary>
            /// Collects the fields for the instance node.
            /// </summary>
            private static void CollectInstanceDeclarations(
                Session session,
                NodeId typeId,
                InstanceDeclaration parent,
                List<InstanceDeclaration> instances,
                IDictionary<string, InstanceDeclaration> map) {
                // find the children.
                BrowseDescription nodeToBrowse = new BrowseDescription();

                if (parent == null) {
                    nodeToBrowse.NodeId = typeId;
                }
                else {
                    nodeToBrowse.NodeId = parent.NodeId;
                }

                nodeToBrowse.BrowseDirection = Opc.Ua.BrowseDirection.Forward;
                nodeToBrowse.ReferenceTypeId = ReferenceTypeIds.HasChild;
                nodeToBrowse.IncludeSubtypes = true;
                nodeToBrowse.NodeClassMask = (uint)Opc.Ua.NodeClass.Variable;
                nodeToBrowse.ResultMask = (uint)BrowseResultMask.All;

                // ignore any browsing errors.
                ReferenceDescriptionCollection references = Browse(session, nodeToBrowse, false);

                if (references == null) {
                    return;
                }

                // process the children.
                List<NodeId> nodeIds = new List<NodeId>();
                List<InstanceDeclaration> children = new List<InstanceDeclaration>();

                for (int ii = 0; ii < references.Count; ii++) {
                    ReferenceDescription reference = references[ii];

                    if (reference.NodeId.IsAbsolute) {
                        continue;
                    }

                    // create a new declaration.
                    InstanceDeclaration child = new InstanceDeclaration();

                    child.NodeId = (NodeId)reference.NodeId;
                    child.NodeClass = reference.NodeClass;

                    if (parent != null) {
                        child.BrowsePath = new QualifiedNameCollection(parent.BrowsePath);
                        child.BrowsePathDisplayText = Utils.Format("{0}/{1}", parent.BrowsePathDisplayText, reference.BrowseName);
                    }
                    else {
                        child.BrowsePath = new QualifiedNameCollection();
                        child.BrowsePathDisplayText = Utils.Format("{0}", reference.BrowseName);
                    }

                    child.BrowsePath.Add(reference.BrowseName);

                    map[child.BrowsePathDisplayText] = child;

                    // add to list.
                    children.Add(child);
                    nodeIds.Add(child.NodeId);
                }

                // check if nothing more to do.
                if (children.Count == 0) {
                    return;
                }

                // find the modelling rules.
                List<NodeId> modellingRules = FindTargetOfReference(session, nodeIds, Opc.Ua.ReferenceTypeIds.HasModellingRule, false);

                if (modellingRules != null) {
                    for (int ii = 0; ii < nodeIds.Count; ii++) {
                        children[ii].ModellingRule = modellingRules[ii];

                        // if the modelling rule is null then the instance is not part of the type declaration.
                        if (NodeId.IsNull(modellingRules[ii])) {
                            map.Remove(children[ii].BrowsePathDisplayText);
                        }
                    }
                }

                // update the descriptions.
                UpdateInstanceDescriptions(session, children, false);

                // recusively collect instance declarations for the tree below.
                for (int ii = 0; ii < children.Count; ii++) {
                    if (!NodeId.IsNull(children[ii].ModellingRule)) {
                        instances.Add(children[ii]);
                        CollectInstanceDeclarations(session, typeId, children[ii], instances, map);
                    }
                }
            }

            /// <summary>
            /// Finds the targets for the specified reference.
            /// </summary>
            private static List<NodeId> FindTargetOfReference(Session session, List<NodeId> nodeIds, NodeId referenceTypeId, bool throwOnError) {
                try {
                    // construct browse request.
                    BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection();

                    for (int ii = 0; ii < nodeIds.Count; ii++) {
                        BrowseDescription nodeToBrowse = new BrowseDescription();
                        nodeToBrowse.NodeId = nodeIds[ii];
                        nodeToBrowse.BrowseDirection = Opc.Ua.BrowseDirection.Forward;
                        nodeToBrowse.ReferenceTypeId = referenceTypeId;
                        nodeToBrowse.IncludeSubtypes = false;
                        nodeToBrowse.NodeClassMask = 0;
                        nodeToBrowse.ResultMask = (uint)BrowseResultMask.None;
                        nodesToBrowse.Add(nodeToBrowse);
                    }

                    // start the browse operation.
                    BrowseResultCollection results = null;
                    DiagnosticInfoCollection diagnosticInfos = null;

                    session.Browse(
                        null,
                        null,
                        1,
                        nodesToBrowse,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, nodesToBrowse);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);

                    List<NodeId> targetIds = new List<NodeId>();
                    ByteStringCollection continuationPoints = new ByteStringCollection();

                    for (int ii = 0; ii < nodeIds.Count; ii++) {
                        targetIds.Add(null);

                        // check for error.
                        if (StatusCode.IsBad(results[ii].StatusCode)) {
                            continue;
                        }

                        // check for continuation point.
                        if (results[ii].ContinuationPoint != null && results[ii].ContinuationPoint.Length > 0) {
                            continuationPoints.Add(results[ii].ContinuationPoint);
                        }

                        // get the node id.
                        if (results[ii].References.Count > 0) {
                            if (NodeId.IsNull(results[ii].References[0].NodeId) || results[ii].References[0].NodeId.IsAbsolute) {
                                continue;
                            }

                            targetIds[ii] = (NodeId)results[ii].References[0].NodeId;
                        }
                    }

                    // release continuation points.
                    if (continuationPoints.Count > 0) {
                        session.BrowseNext(
                            null,
                            true,
                            continuationPoints,
                            out results,
                            out diagnosticInfos);

                        ClientBase.ValidateResponse(results, nodesToBrowse);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);
                    }

                    //return complete list.
                    return targetIds;
                }
                catch (Exception exception) {
                    if (throwOnError) {
                        throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
                    }

                    return null;
                }
            }

            /// <summary>
            /// Finds the targets for the specified reference.
            /// </summary>
            private static void UpdateInstanceDescriptions(Session session, List<InstanceDeclaration> instances, bool throwOnError) {
                try {
                    ReadValueIdCollection nodesToRead = new ReadValueIdCollection();

                    for (int ii = 0; ii < instances.Count; ii++) {
                        ReadValueId nodeToRead = new ReadValueId();
                        nodeToRead.NodeId = instances[ii].NodeId;
                        nodeToRead.AttributeId = Attributes.Description;
                        nodesToRead.Add(nodeToRead);

                        nodeToRead = new ReadValueId();
                        nodeToRead.NodeId = instances[ii].NodeId;
                        nodeToRead.AttributeId = Attributes.DataType;
                        nodesToRead.Add(nodeToRead);

                        nodeToRead = new ReadValueId();
                        nodeToRead.NodeId = instances[ii].NodeId;
                        nodeToRead.AttributeId = Attributes.ValueRank;
                        nodesToRead.Add(nodeToRead);
                    }

                    // start the browse operation.
                    DataValueCollection results = null;
                    DiagnosticInfoCollection diagnosticInfos = null;

                    session.Read(
                        null,
                        0,
                        TimestampsToReturn.Neither,
                        nodesToRead,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, nodesToRead);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

                    // update the instances.
                    for (int ii = 0; ii < nodesToRead.Count; ii += 3) {
                        InstanceDeclaration instance = instances[ii / 3];

                        instance.DataType = results[ii + 1].GetValue<NodeId>(NodeId.Null);
                        instance.ValueRank = results[ii + 2].GetValue<int>(ValueRanks.Any);
                    }
                }
                catch (Exception exception) {
                    if (throwOnError) {
                        throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
                    }
                }
            }
            #endregion
        }

        private readonly ILogger _logger;
        // TODO - check if we still need this list here
        private readonly ConcurrentDictionary<string, SubscriptionWrapper> _subscriptions =
            new ConcurrentDictionary<string, SubscriptionWrapper>();
        private readonly ISessionManager _sessionManager;
        private readonly IVariantEncoderFactory _codec;
    }
}