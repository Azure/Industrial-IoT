// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Exceptions;
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
            _timer = new Timer(OnCheckAsync, null, kIdleCheckTimespan, Timeout.InfiniteTimeSpan);
            _errorSignaled = false; 
        }

        /// <summary>
        /// notify the owner about exceptions in subscriptions
        /// </summary>
        public void SignalSubscriptionError() {
            if (_errorSignaled != true) {
                _errorSignaled = true;
                Try.Op(() => _timer.Change(kFastRetryTimespan, Timeout.InfiniteTimeSpan));
            }
        }

        /// <summary>
        /// Check connectivity
        /// </summary>
        private async void OnCheckAsync(object sender) {
            var success = true;
            try {
                Try.Op(() => _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan));
                foreach (var subscriptionsGroup in _subscriptions.Values.GroupBy(s => new ConnectionIdentifier(s.Connection))){
                    try {
                        var errorSignaled = subscriptionsGroup.ToList().Any(s => s.ErrorSignaled == true);
                        var session = await _sessionManager.GetOrCreateSessionAsync(subscriptionsGroup.Key.Connection, true,
                            errorSignaled ? StatusCodes.BadNotConnected : StatusCodes.Good);
                        if (session == null) {
                            subscriptionsGroup.ToList().ForEach(s => s.ErrorSignaled = true);
                            throw new ResourceNotFoundException("Session not available");
                        }
                        if (errorSignaled) {
                            // just go through the elements and try to recreate the subscrption state
                            foreach (var subscription in subscriptionsGroup.ToList()) {
                                await subscription.ReapplyAsync(false);
                            }
                        }
                        // go through the elements and activate the subscriptions
                        foreach (var subscription in subscriptionsGroup.ToList()) {
                            await subscription.ReapplyAsync(true);
                        }
                    }
                    catch (Exception e){
                        success = false;
                        _logger.Warning("Failed ensure connection for monitored items due to {exception}.", e.Message);
                    }
                }
            }
            catch (Exception e) {
                _logger.Error(e, "CheckAsync failed.");
                success = false;
            }
            finally{
                _logger.Debug("CheckAsync succeeded.");
                _errorSignaled = !success;
                if (success) {
                    Try.Op(() => _timer.Change(kIdleCheckTimespan, Timeout.InfiniteTimeSpan));
                }
                else {
                    Try.Op(() => _timer.Change(kFastRetryTimespan, Timeout.InfiniteTimeSpan));
                }
            }
        }

        /// <inheritdoc/>
        public Task<ISubscription> GetOrCreateSubscriptionAsync(SubscriptionModel subscriptionModel) {
            if (string.IsNullOrEmpty(subscriptionModel?.Id)) {
                throw new ArgumentNullException(nameof(subscriptionModel));
            }
            var sub = _subscriptions.GetOrAdd(subscriptionModel.Id,
                key => new SubscriptionWrapper(this, subscriptionModel, _logger));
            return Task.FromResult<ISubscription>(sub);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            // Cleanup remaining subscriptions
            var subscriptions = _subscriptions.Values.ToList();
            _subscriptions.Clear();
            subscriptions.ForEach(s => Try.Op(() => s.Dispose()));
            _timer.Dispose();
        }

        /// <summary>
        /// Subscription implementation
        /// </summary>
        internal sealed class SubscriptionWrapper : ISubscription {

            /// <inheritdoc/>
            public string Id => _subscription.Id;

            /// <inheritdoc/>
            public long NumberOfConnectionRetries { get; private set; }

            /// <inheritdoc/>
            public bool ErrorSignaled { get; set; }

            /// <inheritdoc/>
            public ConnectionModel Connection => _subscription.Connection;

            /// <inheritdoc/>
            public event EventHandler<SubscriptionNotificationModel> OnSubscriptionChange;

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

                _outer._subscriptions.TryRemove(Id, out _);

                await _lock.WaitAsync();
                try {
                    var session = await _outer._sessionManager.GetOrCreateSessionAsync(
                        Connection, false);
                    if (session != null) {
                        var subscription = session.Subscriptions
                            .SingleOrDefault(s => s.DisplayName == Id);
                        if (subscription != null) {
                            Try.Op(() => subscription.RemoveItems(subscription.MonitoredItems));
                            Try.Op(() => subscription.DeleteItems());
                            Try.Op(() => session.RemoveSubscription(subscription));
                        }
                        // Cleanup session if empty
                        await _outer._sessionManager.RemoveSessionAsync(Connection);
                    }
                }
                finally {
                    _lock.Release();
                }
            }

            /// <inheritdoc/>
            public void Dispose() {
                Try.Async(CloseAsync).Wait();
                _lock.Dispose();
            }

            /// <inheritdoc/>
            public async Task<SubscriptionNotificationModel> GetSnapshotAsync() {
                await _lock.WaitAsync();
                try {
                    var subscription = await GetSubscriptionAsync(null, false);
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
            public async Task ApplyAsync(IEnumerable<MonitoredItemModel> monitoredItems,
                SubscriptionConfigurationModel configuration, bool activate) {
                await _lock.WaitAsync();
                try {
                    ErrorSignaled = false;
                    if (!activate) {
                        _subscription.MonitoredItems = monitoredItems?.Select(n => n.Clone()).ToList();
                        if (configuration?.ResolveDisplayName ?? false) {
                            await ResolveDisplayNameAsync(_subscription.MonitoredItems);
                        }
                    }

                    var rawSubscription = await GetSubscriptionAsync(configuration, activate);
                    if (rawSubscription == null) {
                        throw new ResourceNotFoundException("Session/Subscription not available");
                    }

                    await SetMonitoredItemsAsync(rawSubscription, _subscription.MonitoredItems, activate);
                }
                catch(Exception e){
                    _logger.Warning(e, "Failed to apply monitored items");
                    ErrorSignaled = true;
                    throw;
                }
                finally {
                    _lock.Release();
                    if (ErrorSignaled) {
                        NumberOfConnectionRetries++;
                        _outer.SignalSubscriptionError();
                    }
                }
            }

            /// <summary>
            /// sanity check of the subscription
            /// </summary>
            /// <returns></returns>
            public async Task ReapplyAsync(bool activate) {
                await _lock.WaitAsync();
                try {
                    ErrorSignaled = false;
                    var rawSubscription = await GetSubscriptionAsync(null, activate);
                    if (rawSubscription == null) {
                        throw new ResourceNotFoundException("Session/Subscription not available");
                    }
                    await SetMonitoredItemsAsync(rawSubscription, _subscription.MonitoredItems, activate) ;
                }
                catch (Exception e) {
                    _logger.Warning(e, "Failed to apply monitored items");
                    ErrorSignaled = true;
                    throw;
                }
                finally {
                    _lock.Release();
                    if (ErrorSignaled) {
                        NumberOfConnectionRetries++;
                        _outer.SignalSubscriptionError();
                    }
                }
            }

            /// <summary>
            /// reads the display name of the nodes to be monitored
            /// </summary>
            /// <param name="monitoredItems"></param>
            /// <returns></returns>
            private async Task ResolveDisplayNameAsync(IEnumerable<MonitoredItemModel> monitoredItems) {

                if (monitoredItems == null) {
                    return;
                }

                var session = await _outer._sessionManager.GetOrCreateSessionAsync(Connection, true);
                if (session == null) {
                    return;
                }

                var unresolvedMonitoredItems = monitoredItems.Where(mi => string.IsNullOrEmpty(mi.DisplayName));
                if (!unresolvedMonitoredItems.Any()) {
                    return;
                }

                try {
                    var nodeIds = unresolvedMonitoredItems.
                        Select(n => n.StartNodeId.ToNodeId(session.MessageContext));
                    if (nodeIds.Any()) {
                        session.ReadDisplayName(nodeIds.ToList(), out var displayNames, out var errors);
                        var index = 0;
                        foreach (var monitoredItem in unresolvedMonitoredItems) {
                            if (StatusCode.IsGood(errors[index].StatusCode)) {
                                monitoredItem.DisplayName = displayNames[index];
                            }
                            else {
                                _logger.Warning("Failed resolve display name for {monitoredItem}",
                                    monitoredItem.StartNodeId);
                            }
                            index++;
                        }
                    }
                }
                catch (ServiceResultException sre) {
                    _logger.Error(sre, "Failed resolve display names monitored items.");
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
            private async Task SetMonitoredItemsAsync(Subscription rawSubscription,
                IEnumerable<MonitoredItemModel> monitoredItems, bool activate) {

                var currentState = rawSubscription.MonitoredItems
                    .Select(m => m.Handle)
                    .OfType<MonitoredItemWrapper>()
                    .ToHashSetSafe();

                var applyChanges = false;
                var count = 0;
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
                        _logger.Information("Removed {count} monitored item ...", count);
                    }
                    _currentlyMonitored = null;
                    rawSubscription.ApplyChanges();
                    rawSubscription.SetPublishingMode(false);
                    if (rawSubscription.MonitoredItemCount != 0) {
                        _logger.Warning("Subscription still has {count} monitored items.",
                            rawSubscription.MonitoredItemCount);
                    }
                    return;
                }

                // Synchronize the desired items with the state of the raw subscription
                var desiredState = monitoredItems
                    .Select(m => new MonitoredItemWrapper(m, _logger))
                    .ToHashSetSafe();

                var toRemoveList = currentState.Except(desiredState).Select(t=>t.Item);
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
                    _logger.Information("Removed {count} monitored item ...", count);
                }
                // todo re-associate detached handles
                var toRemoveDetached = rawSubscription.MonitoredItems.Where(m => m.Status == null);
                if (toRemoveDetached.Any()) {
                    _logger.Information("Removed {count} detached monitored items ...", toRemoveDetached.Count());
                    rawSubscription.RemoveItems(toRemoveDetached);
                }

                var nowMonitored = new List<MonitoredItemWrapper>();
                var toAddList = desiredState.Except(currentState);
                if (toAddList.Any()) {
                    count = 0;
                    var codec = _outer._codec.Create(rawSubscription.Session.MessageContext);
                    // Add new monitored items not in current state
                    foreach (var toAdd in toAddList) {
                        // Create monitored item
                        if (!activate) {
                            toAdd.Template.MonitoringMode = Publisher.Models.MonitoringMode.Disabled;
                        }
                        toAdd.Create(rawSubscription.Session, codec, activate);
                        toAdd.Item.Notification += OnMonitoredItemChanged;
                        nowMonitored.Add(toAdd);
                        count++;
                        _logger.Verbose("Adding new monitored item '{item}'...", toAdd.Item.StartNodeId);
                    }

                    rawSubscription.AddItems(toAddList.Select(t=>t.Item).ToList());
                    applyChanges = true;
                    _logger.Information("Added {count} monitored item ...", count);
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
                    _logger.Information("Updated {count} monitored item ...", count);
                }

                if (applyChanges) {
                    rawSubscription.ApplyChanges();

                    foreach (var monitoredItem in nowMonitored) {
                        if (monitoredItem.Item.Status.Error != null &&
                            StatusCode.IsBad(monitoredItem.Item.Status.Error.StatusCode)) {
                            _logger.Error("Error monitoring node {id} in subscription " +
                                "{subscriptionId}, status code: {code}",
                                monitoredItem.Item.StartNodeId, monitoredItem.Item.Subscription.Id,
                                monitoredItem.Item.Status.Error.StatusCode);
                        }
                    }

                    count = rawSubscription.MonitoredItems.Count(m => m.Status.Error == null);
                    kMonitoredItems.WithLabels(rawSubscription.Id.ToString()).Set(count);
                    _logger.Information("Now monitoring {count} nodes in subscription " +
                        "{subscriptionId} (Session: {sessionId}).", count, rawSubscription.Id,
                        rawSubscription.Session.SessionName);

                    var map = nowMonitored.ToDictionary(
                        k => k.Template.Id ?? k.Template.StartNodeId, v => v);
                    foreach (var item in nowMonitored.ToList()) {
                        if (item.Template.TriggerId != null &&
                            map.TryGetValue(item.Template.TriggerId, out var trigger)) {
                            trigger?.AddTriggerLink(item.ServerId.GetValueOrDefault());
                        }
                    }

                    // Set up any new trigger configuration if needed
                    foreach (var item in nowMonitored.ToList()) {
                        if (item.GetTriggeringLinks(out var added, out var removed)) {
                            var response = await rawSubscription.Session.SetTriggeringAsync(
                                null, rawSubscription.Id, item.ServerId.GetValueOrDefault(),
                                new UInt32Collection(added), new UInt32Collection(removed));
                        }
                    }

                    _currentlyMonitored = nowMonitored;
                    if (_currentlyMonitored.Count != rawSubscription.MonitoredItemCount) {
                        _logger.Warning("Monitored items mismatch: wrappers{wrappers} != items:{items} ",
                            _currentlyMonitored.Count, _currentlyMonitored.Count);
                    }
                }
                if (activate) {
                    var validItems = _currentlyMonitored;//.Where(m => m.Item.Status.Error == null);
                    // Change monitoring mode of all items if necessary
                    foreach (var change in validItems.GroupBy(i => i.GetMonitoringModeChange())) {
                        if (change.Key == null) {
                            continue;
                        }
                        _logger.Information("Monitoring {value} {count} nodes in subscription " +
                            "{subscriptionId} (Session: {sessionId}).", change.Key.Value, change.Count(),
                            rawSubscription.Id, rawSubscription.Session.SessionName);
                        var results = rawSubscription.SetMonitoringMode(change.Key.Value,
                            change.Select(t => t.Item).ToList());
                        if (results != null) {
                            _logger.Warning("Failed to set monitoring for {count} nodes in subscription " +
                                "{subscriptionId} (Session: {sessionId}).", results.Count,
                                rawSubscription.Id, rawSubscription.Session.SessionName);
                        }
                    }
                }
            }

            private static uint GreatCommonDivisor(uint a, uint b) {
                return b == 0 ? a : GreatCommonDivisor(b, a % b);
            }

            /// <summary>
            /// Retrieve a raw subscription with all settings applied (no lock)
            /// </summary>
            /// <param name="configuration"></param>
            /// <param name="activate"></param>
            /// <returns></returns>
            private async Task<Subscription> GetSubscriptionAsync(
                SubscriptionConfigurationModel configuration, bool activate) {
                var session = await _outer._sessionManager.GetOrCreateSessionAsync(Connection, true);
                if (session == null) {
                    return null;
                }

                if (configuration != null) {
                    // Apply new configuration right here saving us from modifying later
                    _subscription.Configuration = configuration.Clone();
                }

                // calculate the KeepAliveCount no matter what, perhaps monitored items were changed
                var revisedKeepAliveCount = _subscription.Configuration.KeepAliveCount
                    .GetValueOrDefault(session.DefaultSubscription.KeepAliveCount);
                _subscription.MonitoredItems?.ForEach(m => {
                    if (m.HeartbeatInterval != null && m.HeartbeatInterval != TimeSpan.Zero) {
                        var itemKeepAliveCount = (uint)m.HeartbeatInterval.Value.TotalMilliseconds /
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
                        _logger.Error("Adding subscription '{name}' to session '{session}' failed.",
                             Id, session.SessionName);
                        subscription = null;
                    }
                    else {
                        subscription.Create();
                        _logger.Debug("Added subscription '{name}' to session '{session}'.",
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
                    if (subscription.MaxNotificationsPerPublish != _subscription.Configuration.MaxNotificationsPerPublish
                            .GetValueOrDefault(0)) {
                        _logger.Debug(
                            "{subscription} Changing Max NotificationsPerPublish from {old} to {new}",
                            _subscription.Id, _subscription.Configuration?.MaxNotificationsPerPublish ?? 0,
                            configuration?.MaxNotificationsPerPublish ?? 0);
                        subscription.MaxNotificationsPerPublish = _subscription.Configuration.MaxNotificationsPerPublish
                            .GetValueOrDefault(0);
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
                    }
                    if (subscription.CurrentPublishingEnabled != activate) {
                        // do not deactivate an already activated subscription
                        subscription.SetPublishingMode(activate);
                    }
                }
                return subscription;
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
                    if (OnSubscriptionChange == null) {
                        return;
                    }
                    if (notification == null) {
                        _logger.Warning("DataChange for subscription: {Subscription} having empty notification",
                            subscription.DisplayName);
                        return;
                    }

                    if (_currentlyMonitored == null) {
                        _logger.Information("DataChange for subscription: {Subscription} having no monitored items yet",
                            subscription.DisplayName);
                        return;
                    }

                    // check if notification is a keep alive
                    var isKeepAlive = notification?.MonitoredItems?.Count == 1 &&
                                      notification?.MonitoredItems?.First().ClientHandle == 0 &&
                                      notification?.MonitoredItems?.First().Message?.NotificationData?.Count == 0;
                    var sequenceNumber = notification?.MonitoredItems?.First().Message?.SequenceNumber;
                    var publishTime = (notification?.MonitoredItems?.First().Message?.PublishTime).
                        GetValueOrDefault(DateTime.MinValue);

                    _logger.Debug("DataChange for subscription: {Subscription}, sequence#: " +
                        "{Sequence} isKeepAlive{KeepAlive}, publishTime: {PublishTime}",
                        subscription.DisplayName, sequenceNumber, isKeepAlive, publishTime);

                    // TODO crash
                    var message = new SubscriptionNotificationModel {
                        ServiceMessageContext = subscription?.Session?.MessageContext,
                        ApplicationUri = subscription?.Session?.Endpoint?.Server?.ApplicationUri,
                        EndpointUrl = subscription?.Session?.Endpoint?.EndpointUrl,
                        SubscriptionId = Id,
                        Notifications = (!isKeepAlive)
                            ? notification.ToMonitoredItemNotifications(
                                subscription?.MonitoredItems)?.ToList()
                            : new List<MonitoredItemNotificationModel>()
                    };
                    message.IsKeyMessage = true;

                    // add the heartbeat for monitored items that did not receive a datachange notification
                    // Try access lock if we cannot continue...
                    List<MonitoredItemWrapper> currentlyMonitored = null;
                    if (_lock.Wait(0)) {
                        try {
                            currentlyMonitored = _currentlyMonitored;
                        }
                        finally {
                            _lock.Release();
                        }
                    }
                    // TODO crash?
                    if (currentlyMonitored != null) {
                        // add the heartbeat for monitored items that did not receive a
                        // a datachange notification
                        foreach (var item in currentlyMonitored) {
                            if (isKeepAlive ||
                                !notification.MonitoredItems.Exists(m => m.ClientHandle == item.Item.ClientHandle)) {
                                if (item.TriggerHeartbeat(publishTime)) {
                                    var heartbeatValue = item.Item.LastValue.ToMonitoredItemNotification(item.Item);
                                    if (heartbeatValue != null) {
                                        heartbeatValue.SequenceNumber = sequenceNumber;
                                        heartbeatValue.IsHeartbeat = true;
                                        heartbeatValue.PublishTime = publishTime;
                                        message.Notifications.Add(heartbeatValue);
                                    }
                                }
                                else {
                                    // just reset the heartbeat for the items already processed
                                    item.TriggerHeartbeat(publishTime);
                                }
                            }
                        }
                    }
                    OnSubscriptionChange?.Invoke(this, message);
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Exception processing subscription notification");
                }
            }

            /// <summary>
            /// Monitored item notification handler
            /// </summary>
            /// <param name="monitoredItem"></param>
            /// <param name="e"></param>
            private void OnMonitoredItemChanged(MonitoredItem monitoredItem,
                MonitoredItemNotificationEventArgs e) {
                try {
                    if (OnMonitoredItemChange == null) {
                        return;
                    }
                    if (e?.NotificationValue == null || monitoredItem?.Subscription?.Session == null) {
                        return;
                    }
                    if (!(e.NotificationValue is MonitoredItemNotification notification)) {
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
                catch (Exception ex) {
                    _logger.Debug(ex, "Exception processing monitored item notification");
                }
            }

            private readonly SubscriptionModel _subscription;
            private readonly SubscriptionServices _outer;
            private readonly ILogger _logger;
            private readonly SemaphoreSlim _lock;
            private List<MonitoredItemWrapper> _currentlyMonitored;
            private static readonly Gauge kMonitoredItems = Metrics.CreateGauge("iiot_edge_publisher_monitored_items", "monitored items count",
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
            public MonitoredItemModel Template { get; }

            /// <summary>
            /// Monitored item created from template
            /// </summary>
            public MonitoredItem Item { get; private set; }

            /// <summary>
            /// Last published time
            /// </summary>
            public DateTime NextHeartbeat {get; private set; }

            /// <summary>
            /// validates if a heartbeat is required
            /// </summary>
            /// <returns></returns>
            public bool TriggerHeartbeat(DateTime currentPublish) {
                if (TimeSpan.Zero ==
                    Template?.HeartbeatInterval.GetValueOrDefault(TimeSpan.Zero)) {
                    return false;
                }
                if (NextHeartbeat > currentPublish + TimeSpan.FromMilliseconds(50)) {
                    return false;
                }
                NextHeartbeat = currentPublish + Template.HeartbeatInterval.GetValueOrDefault();
                return true;
            }

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public MonitoredItemWrapper(MonitoredItemModel template, ILogger logger) {
                _logger = logger?.ForContext<MonitoredItemWrapper>() ??
                    throw new ArgumentNullException(nameof(logger));
                Template = template.Clone() ??
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
                    DisplayName = Template.DisplayName,
                    AttributeId = ((uint?)Template.AttributeId) ?? Attributes.Value,
                    IndexRange = Template.IndexRange,
                    RelativePath = Template.RelativePath?
                                .ToRelativePath(session.MessageContext)?
                                .Format(session.NodeCache.TypeTree),
                    MonitoringMode = activate 
                        ? Template.MonitoringMode.ToStackType() ?? Opc.Ua.MonitoringMode.Reporting
                        : Opc.Ua.MonitoringMode.Disabled,
                    StartNodeId = Template.StartNodeId.ToNodeId(session.MessageContext),
                    QueueSize = Template.QueueSize ?? 2,
                    SamplingInterval =
                        (int?)Template.SamplingInterval?.TotalMilliseconds ?? -1,
                    DiscardOldest = !(Template.DiscardNew ?? false),
                    Filter =
                        Template.DataChangeFilter.ToStackModel() ??
                        codec.Decode(Template.EventFilter, true) ??
                        ((MonitoringFilter)Template.AggregateFilter
                            .ToStackModel(session.MessageContext))
                };
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

                if (((int?)Template.SamplingInterval?.TotalMilliseconds ?? -1) !=
                        ((int?)model.Template.SamplingInterval?.TotalMilliseconds ?? -1)) {
                    _logger.Debug("{item}: Changing sampling interval from {old} to {new}",
                        this, (int?)Template.SamplingInterval?.TotalMilliseconds ?? -1,
                        (int?)model.Template.SamplingInterval?.TotalMilliseconds ?? -1);
                    Template.SamplingInterval = model.Template.SamplingInterval;
                    Item.SamplingInterval =
                        (int?)Template.SamplingInterval?.TotalMilliseconds ?? -1;
                    changes = true;
                }

                if ((Template.DiscardNew ?? false) !=
                        (model.Template.DiscardNew ?? false)) {
                    _logger.Debug("{item}: Changing discard new mode from {old} to {new}",
                        this, Template.DiscardNew ?? false, model.Template.DiscardNew ?? false);
                    Template.DiscardNew = model.Template.DiscardNew;
                    Item.DiscardOldest = !(Template.DiscardNew ?? false);
                    changes = true;
                }

                if ((Template.QueueSize ?? 0) != (model.Template.QueueSize ?? 0)) {
                    _logger.Debug("{item}: Changing queue size from {old} to {new}",
                        this, Template.QueueSize ?? 0, model.Template.QueueSize ?? 0);
                    Template.QueueSize = model.Template.QueueSize;
                    Item.QueueSize = Template.QueueSize ?? 0;
                    changes = true;
                }

                if ((Template.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting) !=
                    (model.Template.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting)) {
                    _logger.Debug("{item}: Changing monitoring mode from {old} to {new}",
                        this,
                        Template.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting,
                        model.Template.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting);
                    Template.MonitoringMode = model.Template.MonitoringMode;
                    _modeChange = Template.MonitoringMode ??
                        Publisher.Models.MonitoringMode.Reporting;
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
                    _logger.Debug("{item}: Adding {add} links and removing {remove} links.",
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

            private HashSet<uint> _newTriggers = new HashSet<uint>();
            private HashSet<uint> _triggers = new HashSet<uint>();
            private Publisher.Models.MonitoringMode? _modeChange;
            private readonly ILogger _logger;
        }

        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, SubscriptionWrapper> _subscriptions =
            new ConcurrentDictionary<string, SubscriptionWrapper>();
        private readonly ISessionManager _sessionManager;
        private readonly IVariantEncoderFactory _codec;
        private readonly Timer _timer;
        private bool _errorSignaled;
        private readonly TimeSpan kFastRetryTimespan = TimeSpan.FromSeconds(5);
        private readonly TimeSpan kIdleCheckTimespan = TimeSpan.FromSeconds(15);

    }
}