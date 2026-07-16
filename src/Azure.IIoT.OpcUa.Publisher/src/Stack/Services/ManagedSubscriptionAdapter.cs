// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Client.Subscriptions;
    using Opc.Ua.Client.Subscriptions.MonitoredItems;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ManagedSubscriptionOptions = Opc.Ua.Client.Subscriptions.SubscriptionOptions;

    /// <summary>
    /// Publisher-owned adapter for the public V2 subscription APIs.
    /// </summary>
    /// <remarks>
    /// This seam deliberately has no registration in the production client
    /// path. A later cutover supplies it with the V2 manager exposed by the
    /// managed session facade. It uses only the public V2 contracts so it
    /// remains independent from the V2 implementation's internal types.
    /// </remarks>
    internal sealed class ManagedSubscriptionAdapter : ISubscriptionNotificationHandler,
        IAsyncDisposable, IKeyFrameSnapshotProvider
    {
        /// <summary>
        /// Creates and registers a Publisher adapter with a V2 subscription
        /// manager.
        /// </summary>
        /// <param name="manager">The V2 subscription manager.</param>
        /// <param name="template">The Publisher subscription template.</param>
        /// <param name="options">The Publisher subscription options.</param>
        /// <param name="codec">The Publisher codec for node ids and filters.</param>
        /// <param name="modelChangeBrowserFactory">Creates Publisher address-space browsers.</param>
        /// <param name="logger">Logger used to contain subscriber failures.</param>
        /// <param name="timeProvider">The time provider for notification creation.</param>
        /// <param name="periodicKeyFrameInterval">Optional Publisher key-frame period.</param>
        public ManagedSubscriptionAdapter(ISubscriptionManager manager,
            SubscriptionModel template, OpcUaSubscriptionOptions options,
            IVariantEncoder codec,
            Func<TimeSpan, string, IOpcUaBrowser>? modelChangeBrowserFactory = null,
            ILogger<ManagedSubscriptionAdapter>? logger = null, TimeProvider? timeProvider = null,
            TimeSpan? periodicKeyFrameInterval = null)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(codec);
            if (periodicKeyFrameInterval.HasValue &&
                periodicKeyFrameInterval.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(periodicKeyFrameInterval));
            }

            _codec = codec;
            _logger = logger ?? NullLogger<ManagedSubscriptionAdapter>.Instance;
            _modelChangeBrowserFactory = modelChangeBrowserFactory;
            _options = options;
            _periodicKeyFrameInterval = periodicKeyFrameInterval;
            _timeProvider = timeProvider ?? TimeProvider.System;
            var subscriptionOptions =
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(template, options);
            _subscriptionOptions =
                new MutableOptionsMonitor<ManagedSubscriptionOptions>(subscriptionOptions);
            _subscription = manager.Add(this, _subscriptionOptions);
            if (periodicKeyFrameInterval.HasValue)
            {
                _keyFrameTimer = _timeProvider.CreateTimer(OnKeyFrameTimer, null,
                    periodicKeyFrameInterval.Value, periodicKeyFrameInterval.Value);
            }
            _conditionTimer = _timeProvider.CreateTimer(OnConditionTimer, null,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// The V2 logical subscription owned by this adapter.
        /// </summary>
        internal ISubscription Subscription => _subscription;

        /// <summary>
        /// The number of Publisher bindings currently registered.
        /// </summary>
        internal int BindingCount
        {
            get
            {
                lock (_bindingsLock)
                {
                    return _bindingsByHandle.Count;
                }
            }
        }

        /// <summary>
        /// The number of owners that retain Publisher key-frame state.
        /// </summary>
        internal int OwnerStateCount
        {
            get
            {
                lock (_bindingsLock)
                {
                    return _ownerStates.Count;
                }
            }
        }

        /// <summary>
        /// Gets the effective condition timing state for an item binding.
        /// </summary>
        internal bool TryGetConditionIntervals(uint clientHandle,
            out int snapshotInterval, out int updateInterval)
        {
            snapshotInterval = 0;
            updateInterval = 0;
            if (!TryGetBinding(clientHandle, out var binding) || binding.Condition == null)
            {
                return false;
            }
            lock (binding.Condition._lock)
            {
                snapshotInterval = binding.Condition.SnapshotInterval;
                updateInterval = binding.Condition.UpdateInterval;
                return true;
            }
        }

        /// <summary>
        /// Gets whether the binding has a condition refresh request in flight.
        /// </summary>
        internal bool IsConditionRefreshRequested(uint clientHandle)
        {
            return TryGetBinding(clientHandle, out var binding) &&
                binding.Condition is { RefreshRequested: true };
        }

        /// <summary>
        /// Adds a non-triggered Publisher monitored item to the V2 logical
        /// subscription.
        /// </summary>
        /// <param name="owner">The Publisher subscriber that owns the item.</param>
        /// <param name="template">The Publisher item template.</param>
        /// <returns><c>true</c> if V2 accepted the item.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an asynchronous triggered-item tree is supplied.
        /// </exception>
        internal bool TryAdd(ISubscriber owner, BaseMonitoredItemModel template)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(template);
            EnterSynchronousMutation();
            try
            {
                if (template.TriggeredItems is { Count: > 0 })
                {
                    throw new InvalidOperationException(
                        "Triggered items require TryAddAsync so V2 SetTriggeringAsync can complete.");
                }
                if (template is MonitoredAddressSpaceModel)
                {
                    throw new InvalidOperationException(
                        "Address-space monitoring requires TryAddAsync for browser lifecycle synchronization.");
                }
                var added = TryAddBinding(
                    CreateBinding(CreateName(template), owner, template, null, []), out _);
                if (added)
                {
                    ApplyPublishingState();
                }
                return added;
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Adds a Publisher monitored-item tree and applies V2 triggering
        /// links after every item is registered.
        /// </summary>
        /// <param name="owner">The Publisher subscriber that owns the tree.</param>
        /// <param name="template">The root Publisher item template.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><c>true</c> if the complete tree was registered.</returns>
        internal async ValueTask<bool> TryAddAsync(ISubscriber owner,
            BaseMonitoredItemModel template, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(template);
            ThrowIfDisposed();
            if (ContainsAddressSpaceTemplate(template))
            {
                throw new InvalidOperationException(
                    "Address-space monitoring requires UpdateAsync for browser lifecycle synchronization.");
            }

            using var operation = CancellationTokenSource.CreateLinkedTokenSource(
                ct, _disposeCts.Token);
            await _updateGate.WaitAsync(operation.Token).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                var added = new List<ManagedSubscriptionItemBinding>();
                var completed = false;
                try
                {
                    var root = CreateBinding(CreateName(template), owner, template, null, []);
                    if (!TryAddBinding(root, out var rootItem))
                    {
                        return false;
                    }
                    added.Add(root);
                    var itemsAdded = await AddTriggeredItemsAsync(root, rootItem!, root.Name, added,
                        operation.Token).ConfigureAwait(false);
                    if (itemsAdded)
                    {
                        completed = true;
                        ApplyPublishingState();
                    }
                    return completed;
                }
                finally
                {
                    if (!completed)
                    {
                        RemoveBindings(added);
                        ApplyPublishingState();
                    }
                }
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Replaces the V2 collection's desired item state. Existing names keep
        /// their mutable V2 options monitor, so V2 receives an OnChange update
        /// without losing the server item or its client handle.
        /// </summary>
        /// <param name="items">The desired Publisher items and owners.</param>
        internal void Update(IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            EnterSynchronousMutation();
            try
            {
                if (GetBindings().Any(binding =>
                    binding.Template is MonitoredAddressSpaceModel))
                {
                    throw new InvalidOperationException(
                        "Address-space monitoring updates require UpdateAsync for browser lifecycle synchronization.");
                }
                var desired = CreateDesiredBindings(items);
                if (desired.Any(binding => binding.Template.TriggeredItems is { Count: > 0 }))
                {
                    throw new InvalidOperationException(
                        "Triggered item updates require UpdateAsync so V2 SetTriggeringAsync can complete.");
                }
                if (desired.Any(binding => binding.Template is MonitoredAddressSpaceModel))
                {
                    throw new InvalidOperationException(
                        "Address-space monitoring updates require UpdateAsync for browser lifecycle synchronization.");
                }
                UpdateBindings(desired, requestConditionRefresh: true);
                ApplyPublishingState();
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Replaces the V2 collection's desired item state including recursive
        /// triggered items and their V2 triggering relationships.
        /// </summary>
        /// <param name="items">The desired Publisher items and owners.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="lifetimeCt">Runtime lifetime cancellation token.</param>
        internal async ValueTask UpdateAsync(
            IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items,
            CancellationToken ct = default, CancellationToken lifetimeCt = default)
        {
            ArgumentNullException.ThrowIfNull(items);
            ThrowIfDisposed();
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(
                ct, lifetimeCt, _disposeCts.Token);
            await _updateGate.WaitAsync(operation.Token).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                var previousPublishingEnabled = _subscriptionOptions.CurrentValue.PublishingEnabled;
                var previousItems = SnapshotDesiredItems();
                PendingInitialApply? pendingApply = null;
                try
                {
                    var desired = CreateDesiredBindings(items, includeTriggeredItems: true);
                    if (desired.Any(binding =>
                        binding.Monitor.CurrentValue.MonitoringMode !=
                            Opc.Ua.MonitoringMode.Disabled))
                    {
                        pendingApply = BeginPendingInitialApply();
                    }
                    var monitoredItems = UpdateBindings(desired, requestConditionRefresh: false);
                    if (pendingApply != null)
                    {
                        await WaitForInitialApplyAsync(pendingApply, monitoredItems,
                            operation.Token)
                            .ConfigureAwait(false);
                    }
                    await RequestPendingConditionRefreshAsync(operation.Token)
                        .ConfigureAwait(false);
                    await ApplyTriggeringAsync(operation.Token).ConfigureAwait(false);
                    await SynchronizeModelChangeBrowsersAsync(operation.Token)
                        .ConfigureAwait(false);
                    ApplyPublishingState();
                }
                catch (Exception updateException)
                {
                    ClearPendingInitialApply(pendingApply);
                    try
                    {
                        RestoreBindings(previousItems);
                        if (!lifetimeCt.IsCancellationRequested &&
                            !_disposeCts.IsCancellationRequested)
                        {
                            using var rollback =
                                CancellationTokenSource.CreateLinkedTokenSource(
                                    lifetimeCt, _disposeCts.Token);
                            await SynchronizeModelChangeBrowsersAsync(rollback.Token)
                                .ConfigureAwait(false);
                            await ApplyTriggeringAsync(rollback.Token).ConfigureAwait(false);
                        }
                        SetPublishingEnabled(previousPublishingEnabled);
                    }
                    catch (Exception rollbackException)
                    {
                        throw new AggregateException(
                            "Managed subscription synchronization and rollback failed.",
                            updateException, rollbackException);
                    }
                    throw;
                }
                finally
                {
                    ClearPendingInitialApply(pendingApply);
                }
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Removes one bound item and its recursive triggered-item descendants.
        /// </summary>
        /// <param name="clientHandle">The V2 client handle.</param>
        /// <returns><c>true</c> if V2 removed the root item.</returns>
        internal bool TryRemove(uint clientHandle)
        {
            EnterSynchronousMutation();
            try
            {
                if (!TryGetBinding(clientHandle, out var binding))
                {
                    return false;
                }
                var bindings = GetDescendants(binding).ToArray();
                if (binding.Template is MonitoredAddressSpaceModel ||
                    bindings.Any(candidate =>
                        candidate.Template is MonitoredAddressSpaceModel))
                {
                    throw new InvalidOperationException(
                        "Address-space monitoring removal requires UpdateAsync for browser lifecycle synchronization.");
                }
                var removed = _subscription.MonitoredItems.TryRemove(clientHandle);
                RemoveBinding(binding);
                RemoveBindings(bindings.Where(candidate => candidate.ClientHandle != clientHandle));
                ApplyPublishingState();
                return removed;
            }
            finally
            {
                ExitMutation();
            }
        }

        /// <summary>
        /// Marks an owner for a full Publisher key frame on its next delivery.
        /// </summary>
        /// <param name="owner">The Publisher subscriber.</param>
        internal void RequestKeyFrame(ISubscriber owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            lock (_bindingsLock)
            {
                if (_bindingsByHandle.Values.Any(binding => binding.Owner.Equals(owner)) &&
                    _ownerStates.TryGetValue(owner, out var state))
                {
                    state.KeyFrameRequired = true;
                }
            }
        }

        /// <summary>
        /// Produces a full Publisher key frame from the owner value cache.
        /// </summary>
        /// <param name="owner">The Publisher subscriber.</param>
        /// <param name="notification">The resulting full frame.</param>
        /// <returns><c>true</c> when a frame was created.</returns>
        internal bool TryCreateKeyFrame(ISubscriber owner,
            out OpcUaSubscriptionNotification? notification)
        {
            ArgumentNullException.ThrowIfNull(owner);
            notification = CreateKeyFrame(owner, _timeProvider.GetUtcNow().UtcDateTime);
            return notification != null;
        }

        bool IKeyFrameSnapshotProvider.TryGetNotifications(ISubscriber owner,
            [NotNullWhen(true)] out IList<MonitoredItemNotificationModel>? notifications)
        {
            return TryGetKeyFrameNotifications(owner, out notifications);
        }

        /// <summary>
        /// Creates a Publisher keep-alive for one currently registered owner.
        /// </summary>
        internal OpcUaSubscriptionNotification? CreateKeepAlive(ISubscriber owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            return IsLiveOwner(owner) ? CreateNotification([],
                _timeProvider.GetUtcNow().UtcDateTime, MessageType.KeepAlive) : null;
        }

        /// <summary>
        /// Emits any pending condition snapshots. This is the timer's testable
        /// Publisher-owned seam.
        /// </summary>
        /// <param name="force">Whether to bypass configured intervals.</param>
        internal void FlushConditions(bool force = false)
        {
            foreach (var binding in GetBindings().Where(binding => binding.Condition != null))
            {
                FlushCondition(binding, force, endRefresh: false);
            }
        }

        /// <inheritdoc/>
        public ValueTask OnDataChangeNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime,
            ReadOnlyMemory<DataValueChange> notification, PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
        {
            var deliveries = new Dictionary<(ISubscriber Owner, bool Cyclic),
                List<(ManagedSubscriptionItemBinding Binding, DataValue Value)>>();
            foreach (var change in notification.Span)
            {
                if (!TryGetBinding(change.MonitoredItem, out var binding) ||
                    binding.SkipFirstDataChange())
                {
                    continue;
                }
                binding.LastDataValue = Clone(change.Value);
                var key = (binding.Owner, binding.Template is DataMonitoredItemModel data &&
                    data.SamplingUsingCyclicRead == true);
                if (!deliveries.TryGetValue(key, out var changes))
                {
                    changes = [];
                    deliveries.Add(key, changes);
                }
                changes.Add((binding, change.Value));
            }

            foreach (var ((owner, cyclic), changes) in deliveries)
            {
                if (!IsLiveOwner(owner))
                {
                    continue;
                }
                var keyFrameRequired = RequiresKeyFrame(owner);
                var notifications = keyFrameRequired ? null : changes
                    .Select(change => CreateDataNotification(change.Binding, change.Value))
                    .ToList();
                using var message = keyFrameRequired
                    ? CreateKeyFrame(owner, publishTime, sequenceNumber)
                    : CreateNotification(notifications!, publishTime,
                        MessageType.DeltaFrame, sequenceNumber);
                if (message == null)
                {
                    continue;
                }
                MarkKeyFrameDelivered(owner, message.MessageType == MessageType.KeyFrame);
                if (cyclic)
                {
                    Deliver(owner, message,
                        static (subscriber, notification) =>
                            subscriber.OnSubscriptionCyclicReadCompleted(notification));
                    if (IsLiveOwner(owner))
                    {
                        InvokeSubscriber(owner, subscriber =>
                            subscriber.OnSubscriptionCyclicReadDiagnosticsChange(message.Notifications.Count,
                                message.Notifications.Sum(item => item.Overflow)));
                    }
                }
                else
                {
                    Deliver(owner, message,
                        static (subscriber, notification) =>
                            subscriber.OnSubscriptionDataChangeReceived(notification));
                    if (IsLiveOwner(owner))
                    {
                        InvokeSubscriber(owner, subscriber =>
                            subscriber.OnSubscriptionDataDiagnosticsChange(true,
                                message.Notifications.Count,
                                message.Notifications.Sum(item => item.Overflow), 0));
                    }
                }
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask OnEventDataNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime,
            ReadOnlyMemory<EventNotification> notification, PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
        {
            var deliveries = new Dictionary<ISubscriber, List<MonitoredItemNotificationModel>>();
            var eventNotifications = notification.ToArray();
            foreach (var item in eventNotifications)
            {
                if (!TryGetBinding(item.MonitoredItem, out var binding))
                {
                    continue;
                }
                if (binding.Template is MonitoredAddressSpaceModel)
                {
                    await ProcessModelChangeAsync(binding, item.Fields, ct: default)
                        .ConfigureAwait(false);
                    continue;
                }
                if (binding.Condition != null)
                {
                    await ProcessConditionAsync(subscription, binding, sequenceNumber,
                        publishTime, item.Fields).ConfigureAwait(false);
                    continue;
                }
                if (!deliveries.TryGetValue(binding.Owner, out var notifications))
                {
                    notifications = [];
                    deliveries.Add(binding.Owner, notifications);
                }
                AddEventNotifications(notifications, binding, item.Fields);
            }

            foreach (var (owner, notifications) in deliveries)
            {
                if (notifications.Count == 0 || !IsLiveOwner(owner))
                {
                    continue;
                }
                using var message = CreateNotification(notifications, publishTime,
                    MessageType.Event, sequenceNumber);
                Deliver(owner, message,
                    static (subscriber, notification) =>
                        subscriber.OnSubscriptionEventReceived(notification));
                if (IsLiveOwner(owner))
                {
                    InvokeSubscriber(owner, subscriber => subscriber.OnSubscriptionEventDiagnosticsChange(
                        true, notifications.Count, notifications.Sum(item => item.Overflow), 0));
                }
            }
        }

        /// <inheritdoc/>
        public ValueTask OnKeepAliveNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime, PublishState publishStateMask)
        {
            foreach (var owner in GetBindings().Select(binding => binding.Owner).Distinct())
            {
                if (!IsLiveOwner(owner))
                {
                    continue;
                }
                if (RequiresKeyFrame(owner))
                {
                    var keyFrame = CreateKeyFrame(owner, publishTime, sequenceNumber);
                    if (keyFrame != null && IsLiveOwner(owner))
                    {
                        MarkKeyFrameDelivered(owner, true);
                        using (keyFrame)
                        {
                            Deliver(owner, keyFrame,
                                static (subscriber, notification) =>
                                    subscriber.OnSubscriptionDataChangeReceived(notification));
                        }
                    }
                }
                if (!IsLiveOwner(owner))
                {
                    continue;
                }
                using var keepAlive = CreateNotification([], publishTime,
                    MessageType.KeepAlive, sequenceNumber);
                Deliver(owner, keepAlive,
                    static (subscriber, notification) =>
                        subscriber.OnSubscriptionKeepAlive(notification));
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask OnSubscriptionStateChangedAsync(ISubscription subscription,
            SubscriptionState state, PublishState publishStateMask,
            CancellationToken ct = default)
        {
            if (state is SubscriptionState.Created or SubscriptionState.Modified)
            {
                TryCompletePendingInitialApply();
                QueuePublishingStateEvaluation();
            }
            var bindings = GetBindings();
            foreach (var binding in bindings)
            {
                if (TryGetMonitoredItem(binding, out var monitoredItem))
                {
                    InvokeSubscriber(binding.Owner, subscriber =>
                        subscriber.OnMonitoredItemUpdate(binding.Template,
                            ToServiceResult(monitoredItem!)));
                }
            }

            var recovering = publishStateMask.HasFlag(PublishState.Recovered) ||
                publishStateMask.HasFlag(PublishState.Transferred) ||
                state == SubscriptionState.Created && _created;
            if (recovering)
            {
                foreach (var owner in bindings.Select(binding => binding.Owner).Distinct())
                {
                    RequestKeyFrame(owner);
                    await InvokeSubscriberAsync(owner, subscriber =>
                        subscriber.OnMonitoredItemSemanticsChangedAsync(ct)).ConfigureAwait(false);
                    var keyFrame = CreateKeyFrame(owner,
                        _timeProvider.GetUtcNow().UtcDateTime);
                    if (keyFrame != null)
                    {
                        MarkKeyFrameDelivered(owner, true);
                        using (keyFrame)
                        {
                            Deliver(owner, keyFrame,
                                static (subscriber, notification) =>
                                    subscriber.OnSubscriptionDataChangeReceived(notification));
                        }
                    }
                }
            }
            _created |= state == SubscriptionState.Created;

            if (state is SubscriptionState.Created or SubscriptionState.Modified &&
                bindings.Any(binding => binding.Condition != null))
            {
                try
                {
                    await subscription.ConditionRefreshAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.ConditionRefreshFailed(ex);
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            CancelPendingInitialApply();
            await _disposeCts.CancelAsync().ConfigureAwait(false);
            await _updateGate.WaitAsync().ConfigureAwait(false);
            List<Exception>? exceptions = null;
            try
            {
                try
                {
                    _conditionTimer.Dispose();
                    _keyFrameTimer?.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
                try
                {
                    await DisposeModelChangeBrowsersAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
                lock (_bindingsLock)
                {
                    _bindingsByHandle.Clear();
                    _bindingsByName.Clear();
                    _ownerStates.Clear();
                }
                try
                {
                    await _subscription.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
            finally
            {
                _updateGate.Release();
                _disposeCts.Dispose();
            }
            if (exceptions != null)
            {
                throw new AggregateException(
                    "Managed subscription disposal failed.", exceptions);
            }
        }

        private async ValueTask<bool> AddTriggeredItemsAsync(
            ManagedSubscriptionItemBinding parent, IMonitoredItem parentItem, string rootName,
            List<ManagedSubscriptionItemBinding> added, CancellationToken ct)
        {
            if (parent.Template.TriggeredItems == null)
            {
                return true;
            }
            for (var index = 0; index < parent.Template.TriggeredItems.Count; index++)
            {
                var template = parent.Template.TriggeredItems[index];
                var child = CreateBinding($"{parent.Name}/triggered/{index}:{GetNamePrefix(template)}",
                    parent.Owner, template, rootName, [parent.Name]);
                if (!TryAddBinding(child, out var childItem))
                {
                    return false;
                }
                added.Add(child);
                var result = await _subscription.SetTriggeringAsync(parentItem, [childItem!], null, ct)
                    .ConfigureAwait(false);
                if (!TryApplyTriggeringResult(parent, [child], result,
                    out _))
                {
                    return false;
                }
                if (!await AddTriggeredItemsAsync(child, childItem!, rootName, added, ct)
                    .ConfigureAwait(false))
                {
                    return false;
                }
            }
            return true;
        }

        private bool TryApplyTriggeringResult(ManagedSubscriptionItemBinding parent,
            IReadOnlyList<ManagedSubscriptionItemBinding> children, SetTriggeringResult result,
            out StatusCode failureStatus)
        {
            if (!StatusCode.IsGood(result.ServiceResult))
            {
                ReportTriggeringFailure(parent, result.ServiceResult);
                failureStatus = result.ServiceResult;
                return false;
            }
            if (result.AddResults.Count != children.Count)
            {
                ReportTriggeringFailure(parent, StatusCodes.BadUnexpectedError);
                failureStatus = StatusCodes.BadUnexpectedError;
                return false;
            }
            for (var index = 0; index < children.Count; index++)
            {
                var status = result.AddResults[index].Status;
                if (!StatusCode.IsGood(status))
                {
                    ReportTriggeringFailure(children[index], status);
                    failureStatus = status;
                    return false;
                }
            }
            foreach (var (_, status) in result.RemoveResults)
            {
                if (!StatusCode.IsGood(status))
                {
                    ReportTriggeringFailure(parent, status);
                    failureStatus = status;
                    return false;
                }
            }
            if (result.RemoveResults.Count != 0)
            {
                ReportTriggeringFailure(parent, StatusCodes.BadUnexpectedError);
                failureStatus = StatusCodes.BadUnexpectedError;
                return false;
            }
            failureStatus = StatusCodes.Good;
            return true;
        }

        private async ValueTask ApplyTriggeringAsync(CancellationToken ct)
        {
            foreach (var binding in GetBindings())
            {
                if (binding.ParentName == null ||
                    !TryGetBindingByName(binding.ParentName, out var parent) ||
                    !TryGetMonitoredItem(binding, out var childItem) ||
                    !TryGetMonitoredItem(parent, out var parentItem))
                {
                    continue;
                }
                var result = await _subscription.SetTriggeringAsync(parentItem!,
                    [childItem!], null, ct).ConfigureAwait(false);
                if (!TryApplyTriggeringResult(parent, [binding], result,
                    out var failureStatus))
                {
                    throw new ServiceResultException(failureStatus);
                }
            }
        }

        private void ReportTriggeringFailure(ManagedSubscriptionItemBinding binding,
            StatusCode statusCode)
        {
            InvokeSubscriber(binding.Owner, subscriber =>
                subscriber.OnMonitoredItemUpdate(binding.Template,
                    new ServiceResult(statusCode).ToServiceResultModel()));
        }

        private List<ManagedSubscriptionItemBinding> CreateDesiredBindings(
            IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items,
            bool includeTriggeredItems = false)
        {
            var desired = new List<ManagedSubscriptionItemBinding>();
            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (owner, template) in items)
            {
                ArgumentNullException.ThrowIfNull(owner);
                ArgumentNullException.ThrowIfNull(template);
                var prefix = GetNamePrefix(template);
                names.TryGetValue(prefix, out var ordinal);
                names[prefix] = ordinal + 1;
                var rootName = $"{prefix}:{ordinal}";
                AddDesiredBinding(owner, template, rootName, null, [], desired, includeTriggeredItems);
            }
            return desired;
        }

        private void AddDesiredBinding(ISubscriber owner, BaseMonitoredItemModel template,
            string name, string? rootName, IReadOnlyList<string> triggeredByNames,
            List<ManagedSubscriptionItemBinding> desired, bool includeTriggeredItems)
        {
            var binding = TryGetBindingByName(name, out var current) ? current :
                CreateBinding(name, owner, template, rootName, triggeredByNames);
            binding.Update(owner, template, ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, _options, _codec, rootName ?? name, triggeredByNames));
            desired.Add(binding);
            if (!includeTriggeredItems || template.TriggeredItems == null)
            {
                return;
            }
            for (var index = 0; index < template.TriggeredItems.Count; index++)
            {
                var child = template.TriggeredItems[index];
                AddDesiredBinding(owner, child, $"{name}/triggered/{index}:{GetNamePrefix(child)}",
                    rootName ?? name, [name], desired, true);
            }
        }

        private IReadOnlyList<IMonitoredItem> UpdateBindings(
            List<ManagedSubscriptionItemBinding> desired,
            bool requestConditionRefresh)
        {
            var state = desired.Select(binding => (binding.Name,
                (IOptionsMonitor<MonitoredItemOptions>)binding.Monitor)).ToList();
            var monitoredItems = _subscription.MonitoredItems.Update(state);
            var desiredNames = desired
                .Select(binding => binding.Name)
                .ToHashSet(StringComparer.Ordinal);
            var monitoredItemNames = monitoredItems
                .Select(monitoredItem => monitoredItem.Name)
                .ToHashSet(StringComparer.Ordinal);
            if (monitoredItemNames.Count != monitoredItems.Count ||
                !desiredNames.SetEquals(monitoredItemNames))
            {
                var missing = desiredNames.Except(monitoredItemNames).Take(5);
                var unexpected = monitoredItemNames.Except(desiredNames).Take(5);
                throw new InvalidOperationException(
                    $"V2 monitored-item update was incomplete. Missing: " +
                    $"[{string.Join(", ", missing)}]; unexpected: " +
                    $"[{string.Join(", ", unexpected)}].");
            }
            lock (_bindingsLock)
            {
                _bindingsByHandle.Clear();
                _bindingsByName.Clear();
                foreach (var monitoredItem in monitoredItems)
                {
                    var binding = desired.FirstOrDefault(candidate =>
                        string.Equals(candidate.Name, monitoredItem.Name, StringComparison.Ordinal));
                    if (binding == null || _bindingsByHandle.ContainsKey(monitoredItem.ClientHandle))
                    {
                        continue;
                    }
                    binding.ClientHandle = monitoredItem.ClientHandle;
                    binding.Registered = true;
                    _bindingsByHandle.Add(binding.ClientHandle, binding);
                    _bindingsByName.Add(binding.Name, binding);
                    GetOwnerState(binding.Owner).KeyFrameRequired = true;
                }
                PruneOwnerStates();
            }
            foreach (var binding in GetBindings())
            {
                if (TryGetMonitoredItem(binding, out var monitoredItem))
                {
                    InvokeSubscriber(binding.Owner, subscriber =>
                        subscriber.OnMonitoredItemUpdate(binding.Template,
                            ToServiceResult(monitoredItem!)));
                }
            }
            if (requestConditionRefresh)
            {
                _ = RequestPendingConditionRefreshAsync(default).AsTask();
            }
            return monitoredItems;
        }

        private async ValueTask RequestPendingConditionRefreshAsync(CancellationToken ct)
        {
            var pending = GetBindings()
                .Select(TryCapturePendingConditionRefresh)
                .Where(pending => pending != null)
                .Cast<PendingConditionRefresh>()
                .ToArray();
            if (pending.Length == 0)
            {
                return;
            }
            try
            {
                await _subscription.ConditionRefreshAsync(ct).ConfigureAwait(false);
                foreach (var pendingCondition in pending)
                {
                    lock (pendingCondition.Condition!._lock)
                    {
                        if (ReferenceEquals(pendingCondition.Binding.Condition,
                            pendingCondition.Condition) &&
                            pendingCondition.Condition.Generation ==
                            pendingCondition.Generation)
                        {
                            pendingCondition.Condition.RefreshRequested = false;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ConditionRefreshFailed(ex);
            }
        }

        private static PendingConditionRefresh? TryCapturePendingConditionRefresh(
            ManagedSubscriptionItemBinding binding)
        {
            var condition = binding.Condition;
            if (condition == null)
            {
                return null;
            }
            lock (condition._lock)
            {
                return condition.RefreshRequested ?
                    new PendingConditionRefresh(binding, condition) : null;
            }
        }

        private bool TryAddBinding(ManagedSubscriptionItemBinding binding,
            out IMonitoredItem? monitoredItem)
        {
            if (binding.Template is MonitoredAddressSpaceModel &&
                _modelChangeBrowserFactory == null)
            {
                throw new NotSupportedException(
                    "Address-space monitoring requires a managed browser factory.");
            }
            if (!_subscription.MonitoredItems.TryAdd(binding.Name, binding.Monitor,
                out monitoredItem) || monitoredItem == null)
            {
                return false;
            }
            lock (_bindingsLock)
            {
                if (_bindingsByHandle.ContainsKey(monitoredItem.ClientHandle) ||
                    _bindingsByName.ContainsKey(binding.Name))
                {
                    _subscription.MonitoredItems.TryRemove(monitoredItem.ClientHandle);
                    monitoredItem = null;
                    return false;
                }
                binding.ClientHandle = monitoredItem.ClientHandle;
                binding.Registered = true;
                _bindingsByHandle.Add(binding.ClientHandle, binding);
                _bindingsByName.Add(binding.Name, binding);
                GetOwnerState(binding.Owner).KeyFrameRequired = true;
            }
            var createdMonitoredItem = monitoredItem;
            InvokeSubscriber(binding.Owner, subscriber =>
                subscriber.OnMonitoredItemUpdate(binding.Template,
                    ToServiceResult(createdMonitoredItem!)));
            return true;
        }

        private void RemoveBindings(IEnumerable<ManagedSubscriptionItemBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                _subscription.MonitoredItems.TryRemove(binding.ClientHandle);
                RemoveBinding(binding);
            }
        }

        private void RemoveBinding(ManagedSubscriptionItemBinding binding)
        {
            lock (_bindingsLock)
            {
                _bindingsByHandle.Remove(binding.ClientHandle);
                _bindingsByName.Remove(binding.Name);
                PruneOwnerStates();
            }
        }

        private IEnumerable<ManagedSubscriptionItemBinding> GetDescendants(
            ManagedSubscriptionItemBinding root)
        {
            var bindings = GetBindings();
            var byName = bindings.ToDictionary(binding => binding.Name, StringComparer.Ordinal);
            foreach (var candidate in bindings)
            {
                var parent = candidate.ParentName;
                while (parent != null)
                {
                    if (string.Equals(parent, root.Name, StringComparison.Ordinal))
                    {
                        yield return candidate;
                        break;
                    }
                    parent = byName.TryGetValue(parent, out var parentBinding) ?
                        parentBinding.ParentName : null;
                }
            }
        }

        private void ApplyPublishingState()
        {
            var hasActiveItems = GetBindings().Any(binding =>
                TryGetMonitoredItem(binding, out var monitoredItem) &&
                monitoredItem!.Created &&
                monitoredItem.CurrentMonitoringMode != Opc.Ua.MonitoringMode.Disabled);
            SetPublishingEnabled(hasActiveItems);
        }

        private void QueuePublishingStateEvaluation()
        {
            Interlocked.Exchange(ref _publishingStateDirty, 1);
            ApplyPendingPublishingState();
        }

        private void ApplyPendingPublishingState()
        {
            while (Volatile.Read(ref _disposed) == 0)
            {
                if (!_updateGate.Wait(0))
                {
                    return;
                }
                try
                {
                    do
                    {
                        Interlocked.Exchange(ref _publishingStateDirty, 0);
                        if (Volatile.Read(ref _disposed) == 0 && BindingCount != 0)
                        {
                            ApplyPublishingState();
                        }
                    }
                    while (Volatile.Read(ref _publishingStateDirty) != 0);
                }
                finally
                {
                    _updateGate.Release();
                }
                if (Volatile.Read(ref _publishingStateDirty) == 0)
                {
                    return;
                }
            }
        }

        private void ExitMutation()
        {
            _updateGate.Release();
            if (Volatile.Read(ref _publishingStateDirty) != 0)
            {
                ApplyPendingPublishingState();
            }
        }

        private void SetPublishingEnabled(bool enabled)
        {
            var current = _subscriptionOptions.CurrentValue;
            if (current.PublishingEnabled != enabled)
            {
                _subscriptionOptions.Update(current with
                {
                    PublishingEnabled = enabled
                });
            }
        }

        private DesiredItemSnapshot[] SnapshotDesiredItems()
        {
            return GetBindings()
                .Where(binding => binding.ParentName == null)
                .Select(binding => new DesiredItemSnapshot(
                    binding.Name, binding.Owner, binding.Template))
                .ToArray();
        }

        private void RestoreBindings(IEnumerable<DesiredItemSnapshot> items)
        {
            var desired = new List<ManagedSubscriptionItemBinding>();
            foreach (var item in items)
            {
                AddDesiredBinding(item.Owner, item.Template, item.Name, null, [],
                    desired, includeTriggeredItems: true);
            }
            UpdateBindings(desired, requestConditionRefresh: false);
        }

        private async ValueTask SynchronizeModelChangeBrowsersAsync(CancellationToken ct)
        {
            var desired = GetBindings()
                .Where(binding => binding.Template is MonitoredAddressSpaceModel)
                .ToDictionary(binding => binding.Name, StringComparer.Ordinal);
            if (desired.Count != 0 && _modelChangeBrowserFactory == null)
            {
                throw new NotSupportedException(
                    "Address-space monitoring requires a managed browser factory.");
            }

            Dictionary<string, ModelChangeBrowserRegistration> current;
            lock (_modelChangeBrowsersLock)
            {
                current = new Dictionary<string, ModelChangeBrowserRegistration>(
                    _modelChangeBrowsers, StringComparer.Ordinal);
            }

            List<ModelChangeBrowserRegistration> staged = [];
            try
            {
                foreach (var binding in desired.Values)
                {
                    ct.ThrowIfCancellationRequested();
                    var rebrowsePeriod = GetRebrowsePeriod(binding);
                    if (current.TryGetValue(binding.Name, out var existingRegistration) &&
                        existingRegistration.RebrowsePeriod == rebrowsePeriod)
                    {
                        continue;
                    }
                    var browser = _modelChangeBrowserFactory!(rebrowsePeriod,
                        _modelChangeSubscriptionName);
                    ModelChangeBrowserRegistration? registration = null;
                    registration = new ModelChangeBrowserRegistration(binding.Name,
                        rebrowsePeriod, browser,
                        (_, change) => PublishModelChange(registration!,
                            kNodeChangeType, change),
                        (_, change) => PublishModelChange(registration!,
                            kReferenceChangeType, change));
                    registration.Attach();
                    staged.Add(registration);
                }
            }
            catch (Exception creationException)
            {
                foreach (var registration in staged)
                {
                    registration.Detach();
                }
                try
                {
                    await CloseModelChangeBrowsersAsync(staged).ConfigureAwait(false);
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException(
                        "Managed model-change browser creation and cleanup failed.",
                        creationException, cleanupException);
                }
                throw;
            }

            List<ModelChangeBrowserRegistration> removed = [];
            lock (_modelChangeBrowsersLock)
            {
                foreach (var registration in _modelChangeBrowsers.Values.ToArray())
                {
                    if (!desired.TryGetValue(registration.Name, out var binding) ||
                        registration.RebrowsePeriod != GetRebrowsePeriod(binding))
                    {
                        _modelChangeBrowsers.Remove(registration.Name);
                        registration.Detach();
                        removed.Add(registration);
                    }
                }
                foreach (var registration in staged)
                {
                    _modelChangeBrowsers.Add(registration.Name, registration);
                }
            }
            await CloseModelChangeBrowsersAsync(removed).ConfigureAwait(false);
            foreach (var registration in staged)
            {
                registration.Start();
            }
        }

        private async ValueTask DisposeModelChangeBrowsersAsync()
        {
            ModelChangeBrowserRegistration[] registrations;
            lock (_modelChangeBrowsersLock)
            {
                registrations = [.. _modelChangeBrowsers.Values];
                _modelChangeBrowsers.Clear();
                foreach (var registration in registrations)
                {
                    registration.Detach();
                }
            }
            await CloseModelChangeBrowsersAsync(registrations).ConfigureAwait(false);
        }

        private static async ValueTask CloseModelChangeBrowsersAsync(
            IEnumerable<ModelChangeBrowserRegistration> registrations)
        {
            List<Exception>? exceptions = null;
            foreach (var registration in registrations)
            {
                try
                {
                    await registration.Browser.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
            if (exceptions != null)
            {
                throw new AggregateException(
                    "One or more managed model-change browsers failed to close.",
                    exceptions);
            }
        }

        private void PublishModelChange<T>(ModelChangeBrowserRegistration registration,
            ExpandedNodeId eventType,
            Change<T> change) where T : class, IEncodeable
        {
            lock (_modelChangeBrowsersLock)
            {
                if (!_modelChangeBrowsers.TryGetValue(registration.Name, out var current) ||
                    !ReferenceEquals(current, registration))
                {
                    return;
                }
            }
            if (Volatile.Read(ref _disposed) != 0 ||
                !TryGetBindingByName(registration.Name, out var binding) ||
                binding.Template is not MonitoredAddressSpaceModel template ||
                !IsLiveOwner(binding.Owner))
            {
                return;
            }

            var notifications = CreateModelChangeNotifications(template, eventType, change)
                .ToList();
            using var message = CreateNotification(notifications,
                change.Timestamp.UtcDateTime, MessageType.Event);
            Deliver(binding.Owner, message,
                static (subscriber, notification) =>
                    subscriber.OnSubscriptionEventReceived(notification));
            if (IsLiveOwner(binding.Owner))
            {
                InvokeSubscriber(binding.Owner, subscriber =>
                    subscriber.OnSubscriptionEventDiagnosticsChange(false,
                        notifications.Count,
                        notifications.Sum(notification => notification.Overflow), 1));
            }
        }

        private static IEnumerable<MonitoredItemNotificationModel>
            CreateModelChangeNotifications<T>(MonitoredAddressSpaceModel template,
                ExpandedNodeId eventType, Change<T> change) where T : class, IEncodeable
        {
            for (var index = 0; index < kModelChangeFields.Length; index++)
            {
                Variant value = index switch
                {
                    0 => new Variant((Uuid)Guid.NewGuid()),
                    1 => new Variant(eventType),
                    2 => new Variant(change.Source),
                    3 => new Variant(change.Timestamp.UtcDateTime),
                    4 => change.ChangedItem == null ?
                        Variant.Null : Variant.FromStructure(change.ChangedItem),
                    _ => Variant.Null
                };
                yield return new MonitoredItemNotificationModel
                {
                    Id = template.Id ?? string.Empty,
                    DataSetName = template.DisplayName,
                    DataSetFieldName = kModelChangeFields[index],
                    PathFromRoot = change.PathFromRoot,
                    NodeId = template.StartNodeId,
                    Value = new DataValue(value),
                    Flags = MonitoredItemSourceFlags.ModelChanges,
                    SequenceNumber = change.SequenceNumber
                };
            }
        }

        private TimeSpan GetRebrowsePeriod(
            ManagedSubscriptionItemBinding binding)
        {
            return ((MonitoredAddressSpaceModel)binding.Template).RebrowsePeriod ??
                _options.DefaultRebrowsePeriod ??
                TimeSpan.FromHours(12);
        }

        private static bool ContainsAddressSpaceTemplate(
            BaseMonitoredItemModel template)
        {
            return template is MonitoredAddressSpaceModel ||
                template.TriggeredItems?.Any(ContainsAddressSpaceTemplate) == true;
        }

        private PendingInitialApply BeginPendingInitialApply()
        {
            var pending = new PendingInitialApply();
            lock (_initialApplyLock)
            {
                if (_pendingInitialApply != null)
                {
                    throw new InvalidOperationException(
                        "An initial monitored-item synchronization is already pending.");
                }
                _pendingInitialApply = pending;
            }
            return pending;
        }

        private async ValueTask WaitForInitialApplyAsync(PendingInitialApply pending,
            IReadOnlyList<IMonitoredItem> monitoredItems, CancellationToken ct)
        {
            lock (_initialApplyLock)
            {
                if (!ReferenceEquals(_pendingInitialApply, pending))
                {
                    ThrowIfDisposed();
                    throw new InvalidOperationException(
                        "The initial monitored-item synchronization is no longer active.");
                }
                pending.MonitoredItems = monitoredItems.Select(monitoredItem =>
                {
                    if (!TryGetBindingByName(monitoredItem.Name, out var binding))
                    {
                        throw new InvalidOperationException(
                            $"Missing Publisher binding for V2 item '{monitoredItem.Name}'.");
                    }
                    return new PendingMonitoredItem(monitoredItem,
                        binding.Monitor.CurrentValue.MonitoringMode);
                }).ToArray();
            }
            TryCompletePendingInitialApply();
            await pending.Completion.Task.WaitAsync(ct).ConfigureAwait(false);
        }

        private void TryCompletePendingInitialApply()
        {
            PendingInitialApply? pending;
            IReadOnlyList<PendingMonitoredItem>? monitoredItems;
            lock (_initialApplyLock)
            {
                pending = _pendingInitialApply;
                monitoredItems = pending?.MonitoredItems;
            }
            if (pending == null || monitoredItems == null)
            {
                return;
            }
            if (monitoredItems.All(item =>
                !ServiceResult.IsGood(item.Item.Error) ||
                item.Item.Created &&
                item.Item.CurrentMonitoringMode == item.DesiredMonitoringMode))
            {
                pending.Completion.TrySetResult();
            }
        }

        private void ClearPendingInitialApply(PendingInitialApply? pending)
        {
            if (pending == null)
            {
                return;
            }
            lock (_initialApplyLock)
            {
                if (ReferenceEquals(_pendingInitialApply, pending))
                {
                    _pendingInitialApply = null;
                }
            }
        }

        private void CancelPendingInitialApply()
        {
            PendingInitialApply? pending;
            lock (_initialApplyLock)
            {
                pending = _pendingInitialApply;
            }
            pending?.Completion.TrySetException(new ObjectDisposedException(
                nameof(ManagedSubscriptionAdapter)));
        }

        private void EnterSynchronousMutation()
        {
            ThrowIfDisposed();
            if (!_updateGate.Wait(0))
            {
                throw new InvalidOperationException(
                    "A managed subscription mutation is already in progress.");
            }
            try
            {
                ThrowIfDisposed();
            }
            catch
            {
                _updateGate.Release();
                throw;
            }
        }

        private void PruneOwnerStates()
        {
            var activeOwners = _bindingsByHandle.Values
                .Select(binding => binding.Owner)
                .ToHashSet();
            foreach (var owner in _ownerStates.Keys
                .Where(owner => !activeOwners.Contains(owner))
                .ToArray())
            {
                _ownerStates.Remove(owner);
            }
        }

        private ManagedSubscriptionItemBinding CreateBinding(string name,
            ISubscriber owner, BaseMonitoredItemModel template, string? rootName,
            IReadOnlyList<string> triggeredByNames)
        {
            var effective = template.SetDefaults(_options);
            return new ManagedSubscriptionItemBinding(name, owner, effective,
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(effective, _options, _codec,
                    rootName ?? name, triggeredByNames),
                rootName ?? name, triggeredByNames);
        }

        private async ValueTask ProcessModelChangeAsync(ManagedSubscriptionItemBinding binding,
            ArrayOf<Variant> fields, CancellationToken ct)
        {
            if (fields.Count < 2 ||
                !fields[0].TryGetValue(out NodeId eventType) ||
                eventType != ObjectTypeIds.BaseModelChangeEventType &&
                eventType != ObjectTypeIds.GeneralModelChangeEventType)
            {
                return;
            }
            await InvokeModelChangeBrowserAsync(binding, ct)
                .ConfigureAwait(false);
        }

        private async ValueTask ProcessConditionAsync(ISubscription subscription,
            ManagedSubscriptionItemBinding binding, uint sequenceNumber, DateTime publishTime,
            ArrayOf<Variant> fields)
        {
            var condition = binding.Condition!;
            lock (condition._lock)
            {
                if (condition.Superseded)
                {
                    return;
                }
            }
            if (condition.EventTypeIndex >= 0 && condition.EventTypeIndex < fields.Count &&
                fields[condition.EventTypeIndex].TryGetValue(out NodeId eventType))
            {
                if (eventType == ObjectTypeIds.RefreshStartEventType)
                {
                    lock (condition._lock)
                    {
                        condition.Active.Clear();
                        condition.Refreshing = true;
                    }
                    return;
                }
                if (eventType == ObjectTypeIds.RefreshEndEventType)
                {
                    FlushCondition(binding, force: true, endRefresh: true);
                    return;
                }
                if (eventType == ObjectTypeIds.RefreshRequiredEventType)
                {
                    try
                    {
                        await subscription.ConditionRefreshAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.ConditionRefreshFailed(ex);
                    }
                    return;
                }
            }
            lock (condition._lock)
            {
                if (condition.ConditionIdIndex < 0 || condition.RetainIndex < 0 ||
                    condition.ConditionIdIndex >= fields.Count || condition.RetainIndex >= fields.Count)
                {
                    return;
                }
                var id = fields[condition.ConditionIdIndex].ToString();
                if (string.IsNullOrEmpty(id))
                {
                    return;
                }
                var retain = fields[condition.RetainIndex].TryGetValue(out bool value) && value;
                if (!retain)
                {
                    condition.Active.Remove(id);
                    condition.Dirty = true;
                    return;
                }
                var notifications = new List<MonitoredItemNotificationModel>();
                AddEventNotifications(notifications, binding, fields);
                if (notifications.Count == 0)
                {
                    return;
                }
                condition.Active[id] = notifications.Select(CloneNotification).ToList();
                condition.Dirty = true;
            }
        }

        private void FlushCondition(ManagedSubscriptionItemBinding binding,
            bool force, bool endRefresh)
        {
            var condition = binding.Condition!;
            var now = _timeProvider.GetUtcNow();
            List<MonitoredItemNotificationModel>? notifications = null;
            lock (condition._lock)
            {
                if (endRefresh)
                {
                    condition.Refreshing = false;
                    condition.Dirty = true;
                }
                if (condition.Superseded || condition.Refreshing || condition.Publishing ||
                    !force && now < condition.LastSent +
                    TimeSpan.FromSeconds(condition.SnapshotInterval) &&
                    (!condition.Dirty || now < condition.LastSent +
                        TimeSpan.FromSeconds(condition.UpdateInterval)))
                {
                    return;
                }
                condition.Publishing = true;
                notifications = condition.Active.Values
                    .SelectMany(value => value.Select(CloneNotification))
                    .ToList();
                condition.Dirty = false;
                condition.LastSent = now;
            }
            try
            {
                if (notifications.Count != 0 &&
                    ReferenceEquals(binding.Condition, condition) &&
                    IsLiveOwner(binding.Owner))
                {
                    using var message = CreateNotification(notifications,
                        now.UtcDateTime, MessageType.Condition);
                    Deliver(binding.Owner, message,
                        static (owner, notification) => owner.OnSubscriptionEventReceived(notification));
                    InvokeSubscriber(binding.Owner,
                        owner => owner.OnSubscriptionEventDiagnosticsChange(true,
                            notifications.Count, notifications.Sum(item => item.Overflow), 0));
                }
            }
            finally
            {
                lock (condition._lock)
                {
                    condition.Publishing = false;
                }
            }
        }

        private MonitoredItemNotificationModel CreateDataNotification(
            ManagedSubscriptionItemBinding binding, DataValue value)
        {
            return new MonitoredItemNotificationModel
            {
                Id = binding.Template.DataSetFieldId ?? string.Empty,
                DataSetFieldName = binding.Template.DisplayName,
                DataSetName = binding.Template.DisplayName,
                NodeId = binding.Template.StartNodeId,
                Value = Clone(value),
                Flags = 0,
                Overflow = value.StatusCode.Overflow ? 1 : 0,
                SequenceNumber = binding.NextSequenceNumber()
            };
        }

        private void AddEventNotifications(List<MonitoredItemNotificationModel> notifications,
            ManagedSubscriptionItemBinding binding, ArrayOf<Variant> fields)
        {
            var names = binding.EventFieldNames;
            var count = Math.Min(names.Count, fields.Count);
            var itemSequenceNumber = binding.NextSequenceNumber();
            for (var index = 0; index < count; index++)
            {
                if (names[index] == null)
                {
                    continue;
                }
                notifications.Add(new MonitoredItemNotificationModel
                {
                    Id = binding.Template.Id ?? string.Empty,
                    DataSetName = binding.Template.DisplayName,
                    DataSetFieldName = names[index],
                    NodeId = binding.Template.StartNodeId,
                    Value = Clone(new DataValue(fields[index])),
                    SequenceNumber = itemSequenceNumber
                });
            }
        }

        private OpcUaSubscriptionNotification? CreateKeyFrame(ISubscriber owner,
            DateTime publishTime, uint? publishSequenceNumber = null)
        {
            if (!TryGetKeyFrameNotifications(owner, out var values))
            {
                return null;
            }
            return CreateNotification(values, publishTime, MessageType.KeyFrame,
                publishSequenceNumber);
        }

        private bool TryGetKeyFrameNotifications(ISubscriber owner,
            [NotNullWhen(true)] out IList<MonitoredItemNotificationModel>? notifications)
        {
            notifications = null;
            if (!IsLiveOwner(owner))
            {
                return false;
            }
            var values = GetBindings()
                .Where(binding => binding.Owner.Equals(owner) &&
                    binding.Template is DataMonitoredItemModel)
                .Select(binding => CreateDataNotification(binding,
                    binding.LastDataValue ?? DataValue.FromStatusCode(StatusCodes.BadNoData)))
                .ToList();
            if (values.Count == 0)
            {
                return false;
            }
            notifications = values;
            return true;
        }

        private OpcUaSubscriptionNotification CreateNotification(
            IList<MonitoredItemNotificationModel> notifications,
            DateTime publishTime, MessageType messageType,
            uint? publishSequenceNumber = null)
        {
            return new OpcUaSubscriptionNotification(_timeProvider.GetUtcNow(),
                _codec.Context as ServiceMessageContext, notifications,
                publishSequenceNumber, keyFrameSnapshotProvider: this)
            {
                MessageType = messageType,
                PublishTimestamp = new DateTimeOffset(publishTime),
                SequenceNumber = NextSequenceNumber()
            };
        }

        private bool RequiresKeyFrame(ISubscriber owner)
        {
            lock (_bindingsLock)
            {
                if (!_bindingsByHandle.Values.Any(binding => binding.Owner.Equals(owner)) ||
                    !_ownerStates.TryGetValue(owner, out var state))
                {
                    return false;
                }

                return state.KeyFrameRequired || _periodicKeyFrameInterval.HasValue &&
                    _timeProvider.GetUtcNow() >= state.LastKeyFrame +
                    _periodicKeyFrameInterval.Value;
            }
        }

        private bool IsLiveOwner(ISubscriber owner)
        {
            lock (_bindingsLock)
            {
                return _bindingsByHandle.Values.Any(binding => binding.Owner.Equals(owner));
            }
        }

        private void MarkKeyFrameDelivered(ISubscriber owner, bool delivered)
        {
            if (!delivered)
            {
                return;
            }
            lock (_bindingsLock)
            {
                if (_bindingsByHandle.Values.Any(binding => binding.Owner.Equals(owner)) &&
                    _ownerStates.TryGetValue(owner, out var state))
                {
                    state.KeyFrameRequired = false;
                    state.LastKeyFrame = _timeProvider.GetUtcNow();
                }
            }
        }

        private bool TryGetBinding(IMonitoredItem? monitoredItem,
            out ManagedSubscriptionItemBinding binding)
        {
            binding = null!;
            return monitoredItem != null && TryGetBinding(monitoredItem.ClientHandle, out binding);
        }

        private bool TryGetBinding(uint clientHandle, out ManagedSubscriptionItemBinding binding)
        {
            lock (_bindingsLock)
            {
                return _bindingsByHandle.TryGetValue(clientHandle, out binding!);
            }
        }

        private bool TryGetBindingByName(string name, out ManagedSubscriptionItemBinding binding)
        {
            lock (_bindingsLock)
            {
                return _bindingsByName.TryGetValue(name, out binding!);
            }
        }

        private bool TryGetMonitoredItem(ManagedSubscriptionItemBinding binding,
            out IMonitoredItem? monitoredItem)
        {
            return _subscription.MonitoredItems.TryGetMonitoredItemByClientHandle(
                binding.ClientHandle, out monitoredItem);
        }

        private ManagedSubscriptionItemBinding[] GetBindings()
        {
            lock (_bindingsLock)
            {
                return [.. _bindingsByHandle.Values];
            }
        }

        private OwnerState GetOwnerState(ISubscriber owner)
        {
            if (!_ownerStates.TryGetValue(owner, out var state))
            {
                state = new OwnerState(_timeProvider.GetUtcNow());
                _ownerStates.Add(owner, state);
            }
            return state;
        }

        private string CreateName(BaseMonitoredItemModel template)
        {
            return $"{GetNamePrefix(template)}:{Interlocked.Increment(ref _nextItemName)}";
        }

        private static string GetNamePrefix(BaseMonitoredItemModel template)
        {
            return string.IsNullOrWhiteSpace(template.Id) ? "monitored-item" : template.Id;
        }

        private DataValue Clone(in DataValue value)
        {
            using var stream = new MemoryStream();
            using (var encoder = new BinaryEncoder(stream, _codec.Context, leaveOpen: true))
            {
                encoder.WriteDataValue(null, value);
            }
            stream.Position = 0;
            using var decoder = new BinaryDecoder(stream, _codec.Context, leaveOpen: true);
            return decoder.ReadDataValue(null);
        }

        private MonitoredItemNotificationModel CloneNotification(
            MonitoredItemNotificationModel notification)
        {
            return notification with
            {
                Value = notification.Value.HasValue ? Clone(notification.Value.Value) : null
            };
        }

        private static ServiceResultModel? ToServiceResult(IMonitoredItem monitoredItem)
        {
            return ServiceResult.IsGood(monitoredItem.Error) ? null :
                monitoredItem.Error.ToServiceResultModel();
        }

        private void OnKeyFrameTimer(object? state)
        {
            foreach (var owner in GetBindings().Select(binding => binding.Owner).Distinct())
            {
                RequestKeyFrame(owner);
                if (!RequiresKeyFrame(owner))
                {
                    continue;
                }
                var notification = CreateKeyFrame(owner,
                    _timeProvider.GetUtcNow().UtcDateTime);
                if (notification != null && IsLiveOwner(owner))
                {
                    MarkKeyFrameDelivered(owner, true);
                    using (notification)
                    {
                        Deliver(owner, notification,
                            static (subscriber, keyFrame) =>
                                subscriber.OnSubscriptionDataChangeReceived(keyFrame));
                    }
                }
            }
        }

        private void OnConditionTimer(object? state)
        {
            FlushConditions();
        }

        private uint NextSequenceNumber()
        {
            return SequenceNumber.Increment32(ref _sequenceNumber);
        }

        private void InvokeSubscriber(ISubscriber owner, Action<ISubscriber> callback)
        {
            try
            {
                callback(owner);
            }
            catch (Exception ex)
            {
                _logger.SubscriberCallbackFailed(ex, owner.GetType().Name);
            }
        }

        private void Deliver(ISubscriber owner, OpcUaSubscriptionNotification notification,
            Action<ISubscriber, OpcUaSubscriptionNotification> callback)
        {
            InvokeSubscriber(owner, subscriber => callback(subscriber, notification));
        }

        private async ValueTask InvokeSubscriberAsync(ISubscriber owner,
            Func<ISubscriber, Task> callback)
        {
            try
            {
                await callback(owner).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.SubscriberCallbackFailed(ex, owner.GetType().Name);
            }
        }

        private async ValueTask InvokeModelChangeBrowserAsync(
            ManagedSubscriptionItemBinding binding, CancellationToken ct)
        {
            try
            {
                ModelChangeBrowserRegistration? registration;
                lock (_modelChangeBrowsersLock)
                {
                    _modelChangeBrowsers.TryGetValue(binding.Name, out registration);
                }
                registration?.Browser.Rebrowse();
                await InvokeSubscriberAsync(binding.Owner, subscriber =>
                    subscriber.OnMonitoredItemSemanticsChangedAsync(ct)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ModelChangeBrowserFailed(ex, binding.Owner.GetType().Name);
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        }

        private sealed class ManagedSubscriptionItemBinding
        {
            public uint ClientHandle { get; set; }
            public string Name { get; }
            public string RootName { get; }
            public string? ParentName { get; }
            public ISubscriber Owner { get; private set; }
            public BaseMonitoredItemModel Template { get; private set; }
            public MutableOptionsMonitor<MonitoredItemOptions> Monitor { get; }
            public IReadOnlyList<string?> EventFieldNames { get; private set; }
            public ConditionState? Condition { get; private set; }
            public DataValue? LastDataValue { get; set; }
            public bool Registered { get; set; }

            public ManagedSubscriptionItemBinding(string name, ISubscriber owner,
                BaseMonitoredItemModel template, MonitoredItemOptions options,
                string rootName, IReadOnlyList<string> triggeredByNames)
            {
                Name = name;
                Owner = owner;
                Template = template;
                RootName = rootName;
                ParentName = triggeredByNames.Count == 0 ? null : triggeredByNames[0];
                Monitor = new MutableOptionsMonitor<MonitoredItemOptions>(options);
                (EventFieldNames, Condition) = CreateEventLayout(template, options);
            }

            public void Update(ISubscriber owner, BaseMonitoredItemModel template,
                MonitoredItemOptions options)
            {
                var ownerChanged = !ReferenceEquals(Owner, owner);
                var itemIdentityChanged = ownerChanged ||
                    Template.GetType() != template.GetType() ||
                    !string.Equals(Template.StartNodeId, template.StartNodeId,
                        StringComparison.Ordinal) ||
                    Template.AttributeId != template.AttributeId;
                var conditionChanged = Registered && Template is EventMonitoredItemModel &&
                    template is EventMonitoredItemModel &&
                    (!Equals(Template, template) ||
                        !ReferenceEquals(Monitor.CurrentValue.Filter, options.Filter));
                if (itemIdentityChanged)
                {
                    LastDataValue = null;
                    Interlocked.Exchange(ref _firstDataChange, 0);
                }
                Owner = owner;
                Template = template;
                Monitor.Update(options);
                if (conditionChanged && Condition != null)
                {
                    lock (Condition._lock)
                    {
                        Condition.Superseded = true;
                        Condition.Refreshing = true;
                    }
                }
                (EventFieldNames, Condition) = CreateEventLayout(template, options,
                    conditionChanged ? null : Condition);
                if (conditionChanged && Condition != null)
                {
                    Condition.Refreshing = true;
                    Condition.RefreshRequested = true;
                }
            }

            public bool SkipFirstDataChange()
            {
                return Template is DataMonitoredItemModel { SkipFirst: true } &&
                    Interlocked.Exchange(ref _firstDataChange, 1) == 0;
            }

            public uint NextSequenceNumber()
            {
                return SequenceNumber.Increment32(ref _sequenceNumber);
            }

            private static (IReadOnlyList<string?> Fields, ConditionState? Condition) CreateEventLayout(
                BaseMonitoredItemModel template, MonitoredItemOptions options,
                ConditionState? existing = null)
            {
                if (options.Filter is not EventFilter filter)
                {
                    return ([], null);
                }
                var fields = new List<string?>();
                var eventTemplate = template as EventMonitoredItemModel;
                var condition = eventTemplate is
                    {
                        ConditionHandling: { SnapshotInterval: not null }
                    } ? existing is { } current && current.Matches(eventTemplate) ? current :
                        new ConditionState(eventTemplate) : null;
                for (var index = 0; index < filter.SelectClauses.Count; index++)
                {
                    var clause = filter.SelectClauses[index];
                    if (condition != null)
                    {
                        if (clause.TypeDefinitionId == ObjectTypeIds.BaseEventType &&
                            clause.BrowsePath.Count != 0 &&
                            clause.BrowsePath[0] == BrowseNames.EventType)
                        {
                            condition.EventTypeIndex = index;
                            fields.Add(null);
                            continue;
                        }
                        if (clause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            clause.AttributeId == Attributes.NodeId)
                        {
                            condition.ConditionIdIndex = index;
                            fields.Add(null);
                            continue;
                        }
                        if (clause.TypeDefinitionId == ObjectTypeIds.ConditionType &&
                            clause.BrowsePath.Count != 0 &&
                            clause.BrowsePath[0] == BrowseNames.Retain)
                        {
                            condition.RetainIndex = index;
                            fields.Add(null);
                            continue;
                        }
                    }
                    fields.Add(GetFieldName(eventTemplate, clause, index));
                }
                return (fields, condition);

                static string? GetFieldName(EventMonitoredItemModel? template,
                    SimpleAttributeOperand clause, int index)
                {
                    SimpleAttributeOperandModel? configured = null;
                    if (template?.EventFilter.SelectClauses is { } configuredClauses &&
                        index < configuredClauses.Count)
                    {
                        configured = configuredClauses[index];
                    }
                    if (configured != null)
                    {
                        return configured.DisplayName ??
                            (configured.BrowsePath is { Count: > 0 } ?
                                string.Join("/", configured.BrowsePath) : null);
                    }
                    if (clause.BrowsePath.Count == 0)
                    {
                        return null;
                    }
                    var names = new string[clause.BrowsePath.Count];
                    for (var pathIndex = 0; pathIndex < clause.BrowsePath.Count; pathIndex++)
                    {
                        names[pathIndex] = clause.BrowsePath[pathIndex].Name ?? string.Empty;
                    }
                    return string.Join("/", names);
                }
            }

            private int _firstDataChange;
            private uint _sequenceNumber;
        }

        private sealed class ConditionState
        {
            public int EventTypeIndex { get; set; } = -1;
            public int ConditionIdIndex { get; set; } = -1;
            public int RetainIndex { get; set; } = -1;
            public int SnapshotInterval { get; }
            public int UpdateInterval { get; }
            public DateTimeOffset LastSent { get; set; }
            public bool Dirty { get; set; }
            public bool Publishing { get; set; }
            public bool RefreshRequested { get; set; }
            public bool Refreshing { get; set; }
            public bool Superseded { get; set; }
            public long Generation { get; }
            public Dictionary<string, List<MonitoredItemNotificationModel>> Active { get; } = [];

            public ConditionState(EventMonitoredItemModel template)
            {
                Generation = Interlocked.Increment(ref s_generation);
                SnapshotInterval = template.ConditionHandling!.SnapshotInterval!.Value;
                UpdateInterval = template.ConditionHandling.UpdateInterval ?? SnapshotInterval;
            }

            public bool Matches(EventMonitoredItemModel template)
            {
                return SnapshotInterval == template.ConditionHandling!.SnapshotInterval!.Value &&
                    UpdateInterval == (template.ConditionHandling.UpdateInterval ?? SnapshotInterval);
            }

            internal readonly Lock _lock = new();
            private static long s_generation;
        }

        private sealed record class PendingConditionRefresh(
            ManagedSubscriptionItemBinding Binding, ConditionState? Condition)
        {
            public long Generation => Condition?.Generation ?? 0;
        }

        private sealed record class DesiredItemSnapshot(
            string Name,
            ISubscriber Owner,
            BaseMonitoredItemModel Template);

        private sealed record class PendingMonitoredItem(
            IMonitoredItem Item,
            Opc.Ua.MonitoringMode DesiredMonitoringMode);

        private sealed class ModelChangeBrowserRegistration
        {
            public string Name { get; }
            public TimeSpan RebrowsePeriod { get; }
            public IOpcUaBrowser Browser { get; }

            public ModelChangeBrowserRegistration(string name, TimeSpan rebrowsePeriod,
                IOpcUaBrowser browser, EventHandler<Change<Node>> nodeChanged,
                EventHandler<Change<ReferenceDescription>> referenceChanged)
            {
                Name = name;
                RebrowsePeriod = rebrowsePeriod;
                Browser = browser;
                _nodeChanged = nodeChanged;
                _referenceChanged = referenceChanged;
            }

            public void Attach()
            {
                Browser.OnNodeChange += _nodeChanged;
                Browser.OnReferenceChange += _referenceChanged;
            }

            public void Detach()
            {
                Browser.OnNodeChange -= _nodeChanged;
                Browser.OnReferenceChange -= _referenceChanged;
            }

            public void Start()
            {
                if (Browser is IStartableOpcUaBrowser startable)
                {
                    startable.Start();
                }
            }

            private readonly EventHandler<Change<Node>> _nodeChanged;
            private readonly EventHandler<Change<ReferenceDescription>> _referenceChanged;
        }

        private sealed class OwnerState
        {
            public bool KeyFrameRequired { get; set; } = true;
            public DateTimeOffset LastKeyFrame { get; set; }

            public OwnerState(DateTimeOffset now)
            {
                LastKeyFrame = now;
            }
        }

        private sealed class PendingInitialApply
        {
            public TaskCompletionSource Completion { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public IReadOnlyList<PendingMonitoredItem>? MonitoredItems { get; set; }
        }

        private sealed class MutableOptionsMonitor<T> : IOptionsMonitor<T> where T : class
        {
            public T CurrentValue
            {
                get
                {
                    lock (_lock)
                    {
                        return _value;
                    }
                }
            }

            public MutableOptionsMonitor(T value)
            {
                _value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public T Get(string? name)
            {
                return CurrentValue;
            }

            public IDisposable OnChange(Action<T, string?> listener)
            {
                ArgumentNullException.ThrowIfNull(listener);
                lock (_lock)
                {
                    _listeners.Add(listener);
                }
                return new Registration(this, listener);
            }

            public void Update(T value)
            {
                ArgumentNullException.ThrowIfNull(value);
                Action<T, string?>[] listeners;
                lock (_lock)
                {
                    _value = value;
                    listeners = [.. _listeners];
                }
                foreach (var listener in listeners)
                {
                    listener(value, Options.DefaultName);
                }
            }

            private sealed class Registration : IDisposable
            {
                public Registration(MutableOptionsMonitor<T> owner, Action<T, string?> listener)
                {
                    _owner = owner;
                    _listener = listener;
                }

                public void Dispose()
                {
                    lock (_owner._lock)
                    {
                        _owner._listeners.Remove(_listener);
                    }
                }

                private readonly Action<T, string?> _listener;
                private readonly MutableOptionsMonitor<T> _owner;
            }

            private readonly Lock _lock = new();
            private readonly List<Action<T, string?>> _listeners = [];
            private T _value;
        }

        private readonly Lock _bindingsLock = new();
        private readonly Dictionary<uint, ManagedSubscriptionItemBinding> _bindingsByHandle = [];
        private readonly Dictionary<string, ManagedSubscriptionItemBinding> _bindingsByName =
            new(StringComparer.Ordinal);
        private readonly IVariantEncoder _codec;
        private readonly ITimer _conditionTimer;
        private readonly CancellationTokenSource _disposeCts = new();
        private readonly Lock _initialApplyLock = new();
        private readonly ILogger _logger;
        private readonly Func<TimeSpan, string, IOpcUaBrowser>? _modelChangeBrowserFactory;
        private readonly Lock _modelChangeBrowsersLock = new();
        private readonly Dictionary<string, ModelChangeBrowserRegistration>
            _modelChangeBrowsers = new(StringComparer.Ordinal);
        private readonly string _modelChangeSubscriptionName = Guid.NewGuid().ToString("N");
        private readonly OpcUaSubscriptionOptions _options;
        private readonly Dictionary<ISubscriber, OwnerState> _ownerStates = [];
        private readonly TimeSpan? _periodicKeyFrameInterval;
        private readonly ISubscription _subscription;
        private readonly MutableOptionsMonitor<ManagedSubscriptionOptions> _subscriptionOptions;
        private readonly TimeProvider _timeProvider;
        private readonly ITimer? _keyFrameTimer;
#pragma warning disable CA2213 // Retained so pre-disposal waiters can release safely.
        private readonly SemaphoreSlim _updateGate = new(1, 1);
#pragma warning restore CA2213 // Disposable fields should be disposed
        private PendingInitialApply? _pendingInitialApply;
        private int _disposed;
        private int _nextItemName;
        private int _publishingStateDirty;
        private uint _sequenceNumber;
        private bool _created;
        private static readonly ExpandedNodeId kReferenceChangeType
            = new("ReferenceChange", "http://www.microsoft.com/opc-publisher");
        private static readonly ExpandedNodeId kNodeChangeType
            = new("NodeChange", "http://www.microsoft.com/opc-publisher");
        private static readonly string[] kModelChangeFields =
        [
            BrowseNames.EventId,
            BrowseNames.EventType,
            BrowseNames.SourceNode,
            BrowseNames.Time,
            "Change"
        ];
    }

    /// <summary>
    /// Source-generated logging for the managed subscription adapter.
    /// </summary>
    internal static partial class ManagedSubscriptionAdapterLogging
    {
        [LoggerMessage(EventId = 1120, Level = LogLevel.Error,
            Message = "A subscriber callback failed for {Subscriber}.")]
        public static partial void SubscriberCallbackFailed(this ILogger logger,
            Exception exception, string subscriber);

        [LoggerMessage(EventId = 1121, Level = LogLevel.Warning,
            Message = "Condition refresh failed.")]
        public static partial void ConditionRefreshFailed(this ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1122, Level = LogLevel.Error,
            Message = "The model-change browser failed for {Subscriber}.")]
        public static partial void ModelChangeBrowserFailed(this ILogger logger,
            Exception exception, string subscriber);
    }
}
