// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A managed subscription inside a subscription manager. Can be
    /// extended to provide custom subscription implementations on
    /// top to route the received data appropriately per application.
    /// The subscription itself is based on top of the message processor
    /// implementation that routes messages to subscribers, but adds
    /// state management of the subscription on the server using the
    /// subscription and monitored item service set provided as context.
    /// </summary>
    public abstract class Subscription : MessageProcessor,
        IManagedSubscription, IMonitoredItemContext
    {
        /// <inheritdoc/>
        public byte CurrentPriority { get; private set; }

        /// <inheritdoc/>
        public TimeSpan CurrentPublishingInterval { get; private set; }

        /// <inheritdoc/>
        public uint CurrentKeepAliveCount { get; private set; }

        /// <inheritdoc/>
        public uint CurrentLifetimeCount { get; private set; }

        /// <inheritdoc/>
        public bool CurrentPublishingEnabled { get; private set; }

        /// <inheritdoc/>
        public uint CurrentMaxNotificationsPerPublish { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<MonitoredItem> MonitoredItems
        {
            get
            {
                lock (_monitoredItems)
                {
                    return new List<MonitoredItem>(_monitoredItems.Values);
                }
            }
        }

        /// <inheritdoc/>
        public uint MonitoredItemCount
        {
            get
            {
                lock (_monitoredItemsLock)
                {
                    return (uint)_monitoredItems.Count;
                }
            }
        }

        /// <inheritdoc/>
        public bool Created => Id != 0;

        /// <summary>
        /// Returns true if the subscription is not receiving publishes.
        /// </summary>
        internal bool PublishingStopped
        {
            get
            {
                var lastNotificationTimestamp = _lastNotificationTimestamp;
                if (lastNotificationTimestamp == 0)
                {
                    return false;
                }
                var timeSinceLastNotification = Observability.TimeProvider
                    .GetElapsedTime(lastNotificationTimestamp);
                return timeSinceLastNotification >
                    _keepAliveInterval + kKeepAliveTimerMargin;
            }
        }

        /// <summary>
        /// Current subscription options
        /// </summary>
        protected internal SubscriptionOptions Options { get; protected set; } = new();

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="session"></param>
        /// <param name="completion"></param>
        /// <param name="options"></param>
        /// <param name="observability"></param>
        protected Subscription(ISubscriptionContext session,
            IMessageAckQueue completion, IOptionsMonitor<SubscriptionOptions> options,
            IObservability observability) : base(session, completion, observability)
        {
            _publishTimer = Observability.TimeProvider.CreateTimer(OnKeepAlive,
                null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            OnOptionsChanged(options.CurrentValue);
            _changeTracking = options.OnChange((o, _) => OnOptionsChanged(o));
            _stateManagement = StateManagerAsync(_cts.Token);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{_session}:{Id}";
        }

        /// <inheritdoc/>
        public MonitoredItem AddMonitoredItem(IOptionsMonitor<MonitoredItemOptions> options)
        {
            var monitoredItem = CreateMonitoredItem(options);
            lock (_monitoredItemsLock)
            {
                _monitoredItems.Add(monitoredItem.ClientHandle, monitoredItem);
            }
            Update();
            return monitoredItem;
        }

        /// <inheritdoc/>
        public async ValueTask ConditionRefreshAsync(CancellationToken ct)
        {
            if (!Created)
            {
                throw ServiceResultException.Create(StatusCodes.BadSubscriptionIdInvalid,
                    "Subscription has not been created.");
            }
            var methodsToCall = new CallMethodRequestCollection
            {
                new CallMethodRequest()
                {
                    MethodId = MethodIds.ConditionType_ConditionRefresh,
                    InputArguments = new VariantCollection() { new Variant(Id) }
                }
            };
            await _session.CallAsync(null, methodsToCall, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask RecreateAsync(CancellationToken ct)
        {
            await _stateLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                _lastSequenceNumberProcessed = 0;
                _lastNotificationTimestamp = 0;

                Id = 0;
                CurrentPublishingInterval = TimeSpan.Zero;
                CurrentKeepAliveCount = 0;
                CurrentPublishingEnabled = false;
                CurrentPriority = 0;

                // Recreate subscription
                await CreateAsync(Options, ct).ConfigureAwait(false);

                if (TryGetMonitoredItemChanges(out var itemsToDelete, out var itemsToModify, true))
                {
                    await ApplyMonitoredItemChangesAsync(itemsToDelete, itemsToModify,
                        ct).ConfigureAwait(false);
                }
            }
            finally
            {
                _stateLock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask<bool> TryCompleteTransferAsync(
            IReadOnlyList<uint> availableSequenceNumbers, CancellationToken ct)
        {
            await _stateLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                StopKeepAliveTimer();
                if (!await TrySynchronizeServerAndClientHandlesAsync(ct).ConfigureAwait(false))
                {
                    return false;
                }

                // save available sequence numbers
                _availableInRetransmissionQueue = availableSequenceNumbers;

                if (TryGetMonitoredItemChanges(out var itemsToDelete, out var itemsToModify))
                {
                    await ApplyMonitoredItemChangesAsync(itemsToDelete, itemsToModify,
                        ct).ConfigureAwait(false);
                }
                StartKeepAliveTimer();
                return true;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        /// <inheritdoc/>
        public void RemoveItem(MonitoredItem monitoredItem)
        {
            Debug.Assert(monitoredItem != null);
            lock (_monitoredItemsLock)
            {
                if (!_monitoredItems.Remove(monitoredItem.ClientHandle))
                {
                    return;
                }
            }
            if (monitoredItem.Created)
            {
                _deletedItems.Add(monitoredItem.ServerId);
                _stateControl.Set();
            }
        }

        /// <inheritdoc/>
        public void Update()
        {
            _stateControl.Set();
        }

        /// <inheritdoc/>
        public virtual bool NotifyItemChangeResult(MonitoredItem monitoredItem,
            int retryCount, MonitoredItemOptions source, ServiceResult serviceResult,
            bool final, MonitoringFilterResult? filterResult)
        {
            return final || retryCount > 5; // TODO: Resiliency policy
        }

        /// <inheritdoc/>
        public override ValueTask OnPublishReceivedAsync(NotificationMessage message,
            IReadOnlyList<uint>? availableSequenceNumbers,
            IReadOnlyList<string> stringTable)
        {
            // Reset the keep alive timer
            _publishTimer.Change(_keepAliveInterval, _keepAliveInterval);

            // send notification that publishing received a keep alive
            // or has to republish.
            if (PublishingStopped)
            {
                OnPublishStateChanged(PublishState.Recovered);
            }
            return base.OnPublishReceivedAsync(message, availableSequenceNumbers,
                stringTable);
        }

        /// <inheritdoc/>
        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    await _cts.CancelAsync().ConfigureAwait(false);
                    await _stateManagement.ConfigureAwait(false);

                    foreach (var monitoredItem in MonitoredItems)
                    {
                        monitoredItem.Dispose();
                    }
                }
                finally
                {
                    _publishTimer.Dispose();
                    _changeTracking?.Dispose();
                    _cts.Dispose();
                    _stateLock.Dispose();
                }
            }
            await base.DisposeAsync(disposing).ConfigureAwait(false);
        }

        /// <summary>
        /// Create monitored item
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        protected abstract MonitoredItem CreateMonitoredItem(
            IOptionsMonitor<MonitoredItemOptions> options);

        /// <summary>
        /// Called when the options changed
        /// </summary>
        /// <param name="options"></param>
        ///
        protected virtual void OnOptionsChanged(SubscriptionOptions options)
        {
            var currentOptions = Options;
            if (currentOptions == options)
            {
                return;
            }
            Options = options;
            _stateControl.Set();
        }

        /// <summary>
        /// Called when the subscription state changed
        /// </summary>
        /// <param name="state"></param>
        protected virtual void OnSubscriptionStateChanged(SubscriptionState state)
        {
            _logger.LogInformation("{Subscription}: {State}.", this, state);
        }

        /// <summary>
        /// Returns the monitored item identified by the client handle.
        /// </summary>
        /// <param name="clientHandle"></param>
        protected internal MonitoredItem? FindItemByClientHandle(uint clientHandle)
        {
            lock (_monitoredItemsLock)
            {
                if (_monitoredItems.TryGetValue(clientHandle, out var monitoredItem))
                {
                    return monitoredItem;
                }
                return null;
            }
        }

        /// <summary>
        /// Controls the state changes of the subscriptions and the contained monitored
        /// items.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task StateManagerAsync(CancellationToken ct)
        {
            OnSubscriptionStateChanged(SubscriptionState.Opened);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await _stateControl.WaitAsync(ct).ConfigureAwait(false);
                    await _stateLock.WaitAsync(ct).ConfigureAwait(false);
                    var options = Options;
                    try
                    {
                        while (!ct.IsCancellationRequested)
                        {
                            if (options.Disabled)
                            {
                                await DeleteAsync(ct).ConfigureAwait(false);
                                break; // Wait for changes while disabled
                            }

                            if (!Created)
                            {
                                await CreateAsync(options, ct).ConfigureAwait(false);
                            }
                            else
                            {
                                await ModifyAsync(options, ct).ConfigureAwait(false);
                            }

                            var modified = false;
                            while (!ct.IsCancellationRequested &&
                                TryGetMonitoredItemChanges(out var itemsToDelete,
                                out var itemsToModify))
                            {
                                await ApplyMonitoredItemChangesAsync(itemsToDelete,
                                    itemsToModify, ct).ConfigureAwait(false);
                                // While there are changes pending to be applied apply them
                                modified = true;
                            }
                            if (modified)
                            {
                                OnSubscriptionStateChanged(SubscriptionState.Modified);
                            }
                            break;
                        }
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to apply subscription changes.");
                    }
                    finally
                    {
                        _stateLock.Release();
                    }
                }
            }
            catch (OperationCanceledException) { }

            // Delete subscription on server on dispose
            await DeleteAsync(default).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a subscription on the server, but keeps the subscription in the session.
        /// </summary>
        /// <param name="ct"></param>
        ///
        /// <exception cref="ServiceResultException"></exception>
        internal async ValueTask DeleteAsync(CancellationToken ct)
        {
            // nothing to do if not created.
            if (!Created)
            {
                return;
            }
            try
            {
                // delete the subscription.
                var subscriptionIds = new UInt32Collection { Id };
                var response = await _session.DeleteSubscriptionsAsync(null,
                    subscriptionIds, ct).ConfigureAwait(false);
                // validate response.
                ClientBase.ValidateResponse(response.Results, subscriptionIds);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

                if (StatusCode.IsBad(response.Results[0]))
                {
                    throw new ServiceResultException(ClientBase.GetResult(
                        response.Results[0], 0, response.DiagnosticInfos,
                        response.ResponseHeader));
                }
            }
            // suppress exception if silent flag is set.
            catch (Exception e)
            {
                _logger.LogInformation(e, "Deleting subscription on server failed.");
            }
            OnSubscriptionDeleteCompleted();
        }

        /// <summary>
        /// Creates a subscription on the server and adds all monitored items.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="ct"></param>
        internal async ValueTask CreateAsync(SubscriptionOptions options, CancellationToken ct)
        {
            // create the subscription.
            AdjustCounts(options, out var revisedMaxKeepAliveCount, out var revisedLifetimeCount);

            var response = await _session.CreateSubscriptionAsync(null,
                options.PublishingInterval.TotalMilliseconds, revisedLifetimeCount,
                revisedMaxKeepAliveCount, options.MaxNotificationsPerPublish,
                options.PublishingEnabled, options.Priority, ct).ConfigureAwait(false);

            OnSubscriptionUpdateComplete(true, response.SubscriptionId,
                TimeSpan.FromMilliseconds(response.RevisedPublishingInterval),
                response.RevisedMaxKeepAliveCount, response.RevisedLifetimeCount,
                options.Priority, options.MaxNotificationsPerPublish,
                options.PublishingEnabled);
        }

        /// <summary>
        /// Modifies a subscription on the server.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        internal async ValueTask ModifyAsync(SubscriptionOptions options, CancellationToken ct)
        {
            // modify the subscription.
            AdjustCounts(options, out var revisedMaxKeepAliveCount, out var revisedLifetimeCount);

            if (revisedMaxKeepAliveCount != CurrentKeepAliveCount ||
                revisedLifetimeCount != CurrentLifetimeCount ||
                options.Priority != CurrentPriority ||
                options.MaxNotificationsPerPublish != CurrentMaxNotificationsPerPublish ||
                options.PublishingInterval != CurrentPublishingInterval)
            {
                var response = await _session.ModifySubscriptionAsync(null, Id,
                    options.PublishingInterval.TotalMilliseconds, revisedLifetimeCount,
                    revisedMaxKeepAliveCount, options.MaxNotificationsPerPublish, options.Priority,
                    ct).ConfigureAwait(false);

                if (options.PublishingEnabled != CurrentPublishingEnabled)
                {
                    await SetPublishingModeAsync(options, ct).ConfigureAwait(false);
                }

                OnSubscriptionUpdateComplete(false, 0,
                    TimeSpan.FromMilliseconds(response.RevisedPublishingInterval),
                    response.RevisedMaxKeepAliveCount, response.RevisedLifetimeCount,
                    options.Priority, options.MaxNotificationsPerPublish,
                    options.PublishingEnabled);
            }

            else if (options.PublishingEnabled != CurrentPublishingEnabled)
            {
                await SetPublishingModeAsync(options, ct).ConfigureAwait(false);

                // update current state.
                CurrentPublishingEnabled = options.PublishingEnabled;
                OnSubscriptionStateChanged(SubscriptionState.Modified);
            }

            async Task SetPublishingModeAsync(SubscriptionOptions options, CancellationToken ct)
            {
                // modify the subscription.
                var subscriptionIds = new UInt32Collection { Id };
                var response = await _session.SetPublishingModeAsync(
                    null, options.PublishingEnabled, subscriptionIds, ct).ConfigureAwait(false);

                // validate response.
                ClientBase.ValidateResponse(response.Results, subscriptionIds);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, subscriptionIds);

                if (StatusCode.IsBad(response.Results[0]))
                {
                    throw new ServiceResultException(
                        ClientBase.GetResult(response.Results[0], 0,
                        response.DiagnosticInfos, response.ResponseHeader));
                }

                _logger.LogInformation(
                    "{Subscription}: Modified - Publishing is now {New}.",
                    this, options.PublishingEnabled ? "Enabled" : "Disabled");
            }
        }

        /// <summary>
        /// Update the subscription with the given revised settings.
        /// </summary>
        /// <param name="created"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="revisedPublishingInterval"></param>
        /// <param name="revisedKeepAliveCount"></param>
        /// <param name="revisedLifetimeCount"></param>
        /// <param name="priority"></param>
        /// <param name="maxNotificationsPerPublish"></param>
        /// <param name="publishingEnabled"></param>
        internal void OnSubscriptionUpdateComplete(bool created,
            uint subscriptionId, TimeSpan revisedPublishingInterval,
            uint revisedKeepAliveCount, uint revisedLifetimeCount,
            byte priority, uint maxNotificationsPerPublish,
            bool publishingEnabled)
        {
            if (CurrentPublishingEnabled != publishingEnabled)
            {
                _logger.LogInformation(
                    "{Subscription}: Created - Publishing is {New}.",
                    this, publishingEnabled ? "Enabled" : "Disabled");
                CurrentPublishingEnabled = publishingEnabled;
            }

            if (CurrentKeepAliveCount != revisedKeepAliveCount)
            {
                _logger.LogInformation(
                    "{Subscription}: Changed KeepAliveCount to {New}.",
                    this, revisedKeepAliveCount);

                CurrentKeepAliveCount = revisedKeepAliveCount;
            }

            if (CurrentPublishingInterval != revisedPublishingInterval)
            {
                _logger.LogInformation(
                    "{Subscription}: Changed PublishingInterval to {New}.",
                    this, revisedPublishingInterval);
                CurrentPublishingInterval = revisedPublishingInterval;
            }

            if (CurrentMaxNotificationsPerPublish != maxNotificationsPerPublish)
            {
                _logger.LogInformation(
                    "{Subscription}: Change MaxNotificationsPerPublish to {New}.",
                    this, maxNotificationsPerPublish);
                CurrentMaxNotificationsPerPublish = maxNotificationsPerPublish;
            }

            if (CurrentLifetimeCount != revisedLifetimeCount)
            {
                _logger.LogInformation(
                    "{Subscription}: Changed LifetimeCount to {New}.",
                    this, revisedLifetimeCount);
                CurrentLifetimeCount = revisedLifetimeCount;
            }

            if (CurrentPriority != priority)
            {
                _logger.LogInformation(
                    "{Subscription}: Changed Priority to {New}.",
                    this, priority);
                CurrentPriority = priority;
            }

            if (created)
            {
                Id = subscriptionId;
                StartKeepAliveTimer();
            }

            // Notify all monitored items of the changes
            var state = created ?
                SubscriptionState.Created : SubscriptionState.Modified;
            lock (_monitoredItemsLock)
            {
                foreach (var item in _monitoredItems.Values)
                {
                    item.OnSubscriptionStateChange(state,
                        CurrentPublishingInterval);
                }
            }
            OnSubscriptionStateChanged(state);
        }

        /// <summary>
        /// Delete the subscription.
        /// Ignore errors, always reset all parameter.
        /// </summary>
        internal void OnSubscriptionDeleteCompleted()
        {
            _lastSequenceNumberProcessed = 0;
            _lastNotificationTimestamp = 0;

            Id = 0;
            CurrentPublishingInterval = TimeSpan.Zero;
            CurrentKeepAliveCount = 0;
            CurrentPublishingEnabled = false;
            CurrentPriority = 0;

            _deletedItems.Clear();

            // Notify all monitored items of the changes
            lock (_monitoredItemsLock)
            {
                foreach (var item in _monitoredItems.Values)
                {
                    item.OnSubscriptionStateChange(SubscriptionState.Deleted,
                        CurrentPublishingInterval);
                }
            }
            OnSubscriptionStateChanged(SubscriptionState.Deleted);
        }

        /// <summary>
        /// Apply monitored item changes
        /// </summary>
        /// <param name="itemsToDelete"></param>
        /// <param name="itemsToModify"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ApplyMonitoredItemChangesAsync(List<uint> itemsToDelete,
            List<MonitoredItem.Change> itemsToModify, CancellationToken ct)
        {
            if (itemsToDelete.Count != 0)
            {
                try
                {
                    var monitoredItemIds = new UInt32Collection(itemsToDelete);
                    var response = await _session.DeleteMonitoredItemsAsync(null, Id,
                        monitoredItemIds, ct).ConfigureAwait(false);
                    ClientBase.ValidateResponse(response.Results, monitoredItemIds);
                    ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos,
                        monitoredItemIds);

                    for (var index = 0; index < response.Results.Count; index++)
                    {
                        if (StatusCode.IsGood(response.Results[index]) ||
                            response.Results[index] == StatusCodes.BadMonitoredItemIdInvalid)
                        {
                            continue;
                        }
                        _deletedItems.Add(itemsToDelete[index]); // Retry this
                        // TODO: Give up after a while
                    }
                }
                catch (Exception ex)
                {
                    _deletedItems.AddRange(itemsToDelete);
                    _logger.LogInformation(ex, "Failed to delete monitored items.");
                }
            }

            // To force recreate if item is created therefore, we need to delete the item first.
            var deletes = itemsToModify
                .Where(c => c.Item.Created && c.ForceRecreate)
                .ToList();
            if (deletes.Count > 0)
            {
                var monitoredItemIds = new UInt32Collection(deletes.Select(c => c.Item.ServerId));
                var response = await _session.DeleteMonitoredItemsAsync(null, Id,
                    monitoredItemIds, ct).ConfigureAwait(false);
                ClientBase.ValidateResponse(response.Results, deletes);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, deletes);
                for (var index = 0; index < response.Results.Count; index++)
                {
                    deletes[index].SetDeleteResult(
                        response.Results[index], index, response.DiagnosticInfos,
                        response.ResponseHeader);
                }
            }

            // Modify all items that need to be modified.
            var modifications = itemsToModify
                .Where(c => c.Item.Created)
                .ToList();
            foreach (var group in modifications
                .Where(c => c.Modify != null)
                .GroupBy(c => c.Timestamps))
            {
                var monitoredItems = group.ToList();
                var requests = new MonitoredItemModifyRequestCollection(group.Select(c => c.Modify));
                if (requests.Count > 0)
                {
                    var response = await _session.ModifyMonitoredItemsAsync(null, Id,
                        group.Key, requests, ct).ConfigureAwait(false);
                    ClientBase.ValidateResponse(response.Results, monitoredItems);
                    ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, monitoredItems);
                    // update results.
                    for (var index = 0; index < response.Results.Count; index++)
                    {
                        monitoredItems[index].SetModifyResult(requests[index],
                            response.Results[index], index, response.DiagnosticInfos,
                            response.ResponseHeader);
                    }
                }
            }

            // Finalize by updating the monitoring mode where needed
            foreach (var mode in modifications
                .Where(c => c.MonitoringModeChange != null)
                .GroupBy(c => c.MonitoringModeChange!.Value))
            {
                var monitoredItems = mode
                    .Where(c => c.Item.Created && c.Item.CurrentMonitoringMode != mode.Key)
                    .ToList();
                var requests = new UInt32Collection(monitoredItems.Select(c => c.Item.ServerId));
                if (requests.Count > 0)
                {
                    var response = await _session.SetMonitoringModeAsync(null, Id,
                        mode.Key, requests, ct).ConfigureAwait(false);
                    ClientBase.ValidateResponse(response.Results, monitoredItems);
                    ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, monitoredItems);
                    // update results.
                    for (var index = 0; index < response.Results.Count; index++)
                    {
                        monitoredItems[index].SetMonitoringModeResult(
                            mode.Key, response.Results[index], index, response.DiagnosticInfos,
                            response.ResponseHeader);
                    }
                }
            }

            // Create all items
            var creations = itemsToModify
                .Where(c => !c.Item.Created)
                .ToList();
            foreach (var group in creations
                .Where(c => c.Create != null)
                .GroupBy(c => c.Timestamps))
            {
                var monitoredItems = group.ToList();
                var requests = new MonitoredItemCreateRequestCollection(group.Select(c => c.Create));
                if (requests.Count > 0)
                {
                    var response = await _session.CreateMonitoredItemsAsync(null, Id,
                        group.Key, requests, ct).ConfigureAwait(false);
                    ClientBase.ValidateResponse(response.Results, monitoredItems);
                    ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, monitoredItems);
                    // update results.
                    for (var index = 0; index < response.Results.Count; index++)
                    {
                        monitoredItems[index].SetCreateResult(requests[index],
                            response.Results[index], index, response.DiagnosticInfos,
                            response.ResponseHeader);
                    }
                }
            }
        }

        /// <summary>
        /// Get monitored item changes
        /// </summary>
        /// <param name="itemsToDelete"></param>
        /// <param name="itemsToModify"></param>
        /// <param name="resetAll"></param>
        /// <returns></returns>
        private bool TryGetMonitoredItemChanges(out List<uint> itemsToDelete,
            out List<MonitoredItem.Change> itemsToModify, bool resetAll = false)
        {
            // modify the subscription.
            itemsToModify = new List<MonitoredItem.Change>();
            lock (_monitoredItemsLock)
            {
                itemsToDelete = _deletedItems.ToList();
                _deletedItems.Clear();

                foreach (var monitoredItem in _monitoredItems.Values)
                {
                    if (resetAll)
                    {
                        monitoredItem.Reset();
                    }
                    // build item request.
                    if (monitoredItem.TryGetPendingChange(out var change))
                    {
                        itemsToModify.Add(change);
                    }
                }
            }
            if (itemsToDelete.Count == 0 &&
                itemsToModify.Count == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// <para>
        /// If a Client called CreateMonitoredItems during the network interruption
        /// and the call succeeded in the Server but did not return to the Client,
        /// then the Client does not know if the call succeeded. The Client may
        /// receive data changes for these monitored items but is not able to remove
        /// them since it does not know the Server handle.
        /// </para>
        /// <para>
        /// There is also no way for the Client to detect if the create succeeded.
        /// To delete and recreate the Subscription is also not an option since
        /// there may be several monitored items operating normally that should
        /// not be interrupted.
        /// </para>
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<bool> TrySynchronizeServerAndClientHandlesAsync(
            CancellationToken ct)
        {
            var (success, serverHandleStateMap) = await GetMonitoredItemsAsync(
                ct).ConfigureAwait(false);

            lock (_monitoredItemsLock)
            {
                if (!success)
                {
                    // Reset all items
                    foreach (var monitoredItem in _monitoredItems.Values)
                    {
                        monitoredItem.Reset();
                    }
                    return false;
                }

                var monitoredItems = _monitoredItems.ToDictionary();
                _monitoredItems.Clear();

                //
                // Assign the server id to the item with the matching client handle. This
                // handles the case where the CreateMonitoredItems call succeeded on the
                // server side, but the response was not provided back.
                //
                var clientServerHandleMap = serverHandleStateMap
                    .ToDictionary(m => m.clientHandle, m => m.serverHandle);
                foreach (var monitoredItem in monitoredItems.ToList())
                {
                    //
                    // Adjust any items where the server handles does not map to the
                    // handle the server has assigned.
                    //
                    var clientHandle = monitoredItem.Value.ClientHandle;
                    if (clientServerHandleMap.Remove(clientHandle, out var serverHandle))
                    {
                        monitoredItem.Value.SetTransferResult(clientHandle, serverHandle);
                        monitoredItems.Remove(monitoredItem.Key);
                        _monitoredItems.Add(clientHandle, monitoredItem.Value);
                    }
                }

                //
                // Assign the server side client handle to the item with the same server id
                // This handles the case where we are recreating the subscription from a
                // previously stored state.
                //
                var serverClientHandleMap = clientServerHandleMap
                    .ToDictionary(m => m.Value, m => m.Key);
                foreach (var monitoredItem in monitoredItems.ToList())
                {
                    var serverHandle = monitoredItem.Value.ServerId;
                    if (serverClientHandleMap.Remove(serverHandle, out var clientHandle))
                    {
                        //
                        // There should not be any more item with the same client handle
                        // in this subscription, they were updated before. TODO: Assert
                        //
                        monitoredItem.Value.SetTransferResult(clientHandle, serverHandle);
                        monitoredItems.Remove(monitoredItem.Key);
                        _monitoredItems.Add(clientHandle, monitoredItem.Value);
                    }
                }

                // Ensure all items on the server that are not in the subscription are deleted
                _deletedItems.Clear();
                _deletedItems.AddRange(serverClientHandleMap.Keys);

                // Remaining items do not exist anymore on the server and need to be recreated
                foreach (var missingOnServer in monitoredItems.Values)
                {
                    if (missingOnServer.Created)
                    {
                        _logger.LogDebug("{Subscription}: Recreate client item {Item}.", this,
                            missingOnServer);
                        missingOnServer.Reset();
                    }
                    _monitoredItems.Add(missingOnServer.ClientHandle, missingOnServer);
                }
            }
            return true;
        }

        private record struct MonitoredItemsHandles(bool Success,
            IReadOnlyList<(uint serverHandle, uint clientHandle)> Handles);

        /// <summary>
        /// Call the GetMonitoredItems method on the server.
        /// </summary>
        /// <param name="ct"></param>
        private async ValueTask<MonitoredItemsHandles> GetMonitoredItemsAsync(
            CancellationToken ct)
        {
            try
            {
                var requests = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = ObjectIds.Server,
                        MethodId = MethodIds.Server_GetMonitoredItems,
                        InputArguments = new VariantCollection { new Variant(Id) }
                    }
                };

                var response = await _session.CallAsync(null, requests, ct).ConfigureAwait(false);
                var results = response.Results;
                var diagnosticInfos = response.DiagnosticInfos;
                ClientBase.ValidateResponse(results, requests);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, requests);
                if (StatusCode.IsBad(results[0].StatusCode))
                {
                    throw ServiceResultException.Create(results[0].StatusCode,
                        0, diagnosticInfos, response.ResponseHeader.StringTable);
                }

                var outputArguments = results[0].OutputArguments;
                if (outputArguments.Count != 2 ||
                    outputArguments[0].Value is not uint[] serverHandles ||
                    outputArguments[1].Value is not uint[] clientHandles ||
                    clientHandles.Length != serverHandles.Length)
                {
                    throw ServiceResultException.Create(StatusCodes.BadUnexpectedError,
                        "Output arguments incorrect");
                }
                return new MonitoredItemsHandles(true, serverHandles.Zip(clientHandles).ToList());
            }
            catch (ServiceResultException sre)
            {
                _logger.LogError(sre,
                    "{Subscription}: Failed to call GetMonitoredItems on server", this);
                return new MonitoredItemsHandles(false, Array.Empty<(uint, uint)>());
            }
        }

        /// <summary>
        /// Stop the keep alive timer for the subscription.
        /// </summary>
        private void StopKeepAliveTimer()
        {
            _publishTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Starts a timer to ensure publish requests are sent frequently
        /// enough to detect network interruptions.
        /// </summary>
        private void StartKeepAliveTimer()
        {
            var options = Options;
            _lastNotificationTimestamp = Observability.TimeProvider.GetTimestamp();
            _keepAliveInterval = CurrentPublishingInterval * (CurrentKeepAliveCount + 1);
            if (_keepAliveInterval < kMinKeepAliveTimerInterval)
            {
                _keepAliveInterval = options.PublishingInterval * (options.KeepAliveCount + 1);
            }
            if (_keepAliveInterval > Timeout.InfiniteTimeSpan)
            {
                _keepAliveInterval = Timeout.InfiniteTimeSpan;
            }
            if (_keepAliveInterval < kMinKeepAliveTimerInterval)
            {
                _keepAliveInterval = kMinKeepAliveTimerInterval;
            }
            _publishTimer.Change(_keepAliveInterval, _keepAliveInterval);
        }

        /// <summary>
        /// Checks if a notification has arrived in time.
        /// </summary>
        /// <param name="state"></param>
        private void OnKeepAlive(object? state)
        {
            // check if a publish has arrived.
            if (PublishingStopped)
            {
                Interlocked.Increment(ref _publishLateCount);
                OnPublishStateChanged(PublishState.Stopped);
            }
        }

        /// <summary>
        /// Ensures sensible values for the counts.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="keepAliveCount"></param>
        /// <param name="lifetimeCount"></param>
        private void AdjustCounts(SubscriptionOptions options, out uint keepAliveCount,
            out uint lifetimeCount)
        {
            const uint kDefaultKeepAlive = 10;
            const uint kDefaultLifeTime = 1000;

            keepAliveCount = options.KeepAliveCount;
            lifetimeCount = options.LifetimeCount;
            // keep alive count must be at least 1, 10 is a good default.
            if (keepAliveCount == 0)
            {
                _logger.LogInformation("{Subscription}: Adjusted KeepAliveCount " +
                    "from {Old} to {New}.", this, keepAliveCount, kDefaultKeepAlive);
                keepAliveCount = kDefaultKeepAlive;
            }

            // ensure the lifetime is sensible given the sampling interval.
            if (options.PublishingInterval > TimeSpan.Zero)
            {
                if (options.MinLifetimeInterval > TimeSpan.Zero &&
                    options.MinLifetimeInterval < _session.SessionTimeout)
                {
                    _logger.LogWarning(
                        "{Subscription}: A smaller minimum LifetimeInterval " +
                        "{Counter}ms than session timeout {Timeout}ms configured.",
                        this, options.MinLifetimeInterval, _session.SessionTimeout);
                }

                var minLifetimeInterval = (uint)options.MinLifetimeInterval.TotalMilliseconds;
                var publishingInterval = (uint)options.PublishingInterval.TotalMilliseconds;
                var minLifetimeCount = minLifetimeInterval / publishingInterval;
                if (lifetimeCount < minLifetimeCount)
                {
                    lifetimeCount = minLifetimeCount;

                    if (minLifetimeInterval % publishingInterval != 0)
                    {
                        lifetimeCount++;
                    }
                    _logger.LogInformation(
                        "{Subscription}: Adjusted LifetimeCount to value={New}.",
                        this, lifetimeCount);
                }

                if (lifetimeCount * options.PublishingInterval < _session.SessionTimeout)
                {
                    _logger.LogWarning(
                        "{Subscription}: Lifetime {LifeTime}ms configured is less " +
                        "than session timeout {Timeout}ms.", this,
                        lifetimeCount * options.PublishingInterval, _session.SessionTimeout);
                }
            }
            else if (lifetimeCount == 0)
            {
                // don't know what the sampling interval will be - use something large
                // enough to ensure the user does not experience unexpected drop outs.
                _logger.LogInformation(
                    "{Subscription}: Adjusted LifetimeCount from {Old} to {New}. ",
                    this, lifetimeCount, kDefaultLifeTime);
                lifetimeCount = kDefaultLifeTime;
            }

            // validate spec: lifetimecount shall be at least 3*keepAliveCount
            var minLifeTimeCount = 3 * keepAliveCount;
            if (lifetimeCount < minLifeTimeCount)
            {
                _logger.LogInformation(
                    "{Subscription}: Adjusted LifetimeCount from {Old} to {New}.",
                    this, lifetimeCount, minLifeTimeCount);
                lifetimeCount = minLifeTimeCount;
            }
        }

        private static readonly TimeSpan kMinKeepAliveTimerInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan kKeepAliveTimerMargin = TimeSpan.FromSeconds(1);
        private TimeSpan _keepAliveInterval;
        private int _publishLateCount;
        private readonly Nito.AsyncEx.AsyncAutoResetEvent _stateControl = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _stateManagement;
        private readonly SemaphoreSlim _stateLock = new(1, 1);
        private readonly List<uint> _deletedItems = new();
        private readonly ITimer _publishTimer;
        private readonly object _monitoredItemsLock = new();
        private readonly Dictionary<uint, MonitoredItem> _monitoredItems = new();
        private readonly IDisposable? _changeTracking;
    }
}
