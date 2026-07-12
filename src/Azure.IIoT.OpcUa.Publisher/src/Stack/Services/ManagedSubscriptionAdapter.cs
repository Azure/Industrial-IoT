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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ManagedSubscriptionOptions = Opc.Ua.Client.Subscriptions.SubscriptionOptions;

    /// <summary>
    /// Converts a model-change notification to Publisher's browse/change-feed
    /// domain logic.
    /// </summary>
    internal interface IModelChangeRebrowseSink
    {
        /// <summary>
        /// Process the server's model-change payload.
        /// </summary>
        /// <param name="owner">The owning Publisher subscriber.</param>
        /// <param name="template">The address-space monitoring template.</param>
        /// <param name="changes">The deep-copied GeneralModelChangeEvent changes.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes after Publisher rebrowse processing.</returns>
        ValueTask ProcessAsync(ISubscriber owner, MonitoredAddressSpaceModel template,
            DataValue changes, CancellationToken ct);
    }

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
        IAsyncDisposable
    {
        /// <summary>
        /// Creates and registers a Publisher adapter with a V2 subscription
        /// manager.
        /// </summary>
        /// <param name="manager">The V2 subscription manager.</param>
        /// <param name="template">The Publisher subscription template.</param>
        /// <param name="options">The Publisher subscription options.</param>
        /// <param name="codec">The Publisher codec for node ids and filters.</param>
        /// <param name="modelChangeSink">Publisher's rebrowse/change-feed composition.</param>
        /// <param name="logger">Logger used to contain subscriber failures.</param>
        /// <param name="timeProvider">The time provider for notification creation.</param>
        /// <param name="periodicKeyFrameInterval">Optional Publisher key-frame period.</param>
        public ManagedSubscriptionAdapter(ISubscriptionManager manager,
            SubscriptionModel template, OpcUaSubscriptionOptions options,
            IVariantEncoder codec, IModelChangeRebrowseSink? modelChangeSink = null,
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
            _modelChangeSink = modelChangeSink;
            _options = options;
            _periodicKeyFrameInterval = periodicKeyFrameInterval;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _subscription = manager.Add(this, new MutableOptionsMonitor<ManagedSubscriptionOptions>(
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(template, options)));
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
            ThrowIfDisposed();
            if (template.TriggeredItems is { Count: > 0 })
            {
                throw new InvalidOperationException(
                    "Triggered items require TryAddAsync so V2 SetTriggeringAsync can complete.");
            }
            return TryAddBinding(CreateBinding(CreateName(template), owner, template, null, []),
                out _);
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
                completed = await AddTriggeredItemsAsync(root, rootItem!, root.Name, added, ct)
                    .ConfigureAwait(false);
                return completed;
            }
            finally
            {
                if (!completed)
                {
                    RemoveBindings(added);
                }
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
            ThrowIfDisposed();
            var desired = CreateDesiredBindings(items);
            if (desired.Any(binding => binding.Template.TriggeredItems is { Count: > 0 }))
            {
                throw new InvalidOperationException(
                    "Triggered item updates require UpdateAsync so V2 SetTriggeringAsync can complete.");
            }
            UpdateBindings(desired);
        }

        /// <summary>
        /// Replaces the V2 collection's desired item state including recursive
        /// triggered items and their V2 triggering relationships.
        /// </summary>
        /// <param name="items">The desired Publisher items and owners.</param>
        /// <param name="ct">Cancellation token.</param>
        internal async ValueTask UpdateAsync(
            IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(items);
            ThrowIfDisposed();
            var desired = CreateDesiredBindings(items, includeTriggeredItems: true);
            UpdateBindings(desired);

            foreach (var binding in GetBindings())
            {
                if (binding.ParentName == null ||
                    !TryGetBindingByName(binding.ParentName, out var parent) ||
                    !TryGetMonitoredItem(binding, out var childItem) ||
                    !TryGetMonitoredItem(parent, out var parentItem))
                {
                    continue;
                }
                await _subscription.SetTriggeringAsync(parentItem!, [childItem!], null, ct)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Removes one bound item and its recursive triggered-item descendants.
        /// </summary>
        /// <param name="clientHandle">The V2 client handle.</param>
        /// <returns><c>true</c> if V2 removed the root item.</returns>
        internal bool TryRemove(uint clientHandle)
        {
            ThrowIfDisposed();
            if (!TryGetBinding(clientHandle, out var binding))
            {
                return false;
            }
            var bindings = GetBindings()
                .Where(candidate => candidate.Name == binding.Name ||
                    candidate.RootName == binding.RootName && candidate.Name != binding.RootName)
                .ToArray();
            var removed = _subscription.MonitoredItems.TryRemove(clientHandle);
            RemoveBindings(bindings.Where(candidate => candidate.ClientHandle != clientHandle));
            RemoveBinding(binding);
            return removed;
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
                GetOwnerState(owner).KeyFrameRequired = true;
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
            notification = CreateKeyFrame(owner, NextSequenceNumber(), _timeProvider.GetUtcNow().UtcDateTime);
            return notification != null;
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
                var condition = binding.Condition!;
                var now = _timeProvider.GetUtcNow();
                if (!force && (condition.Refreshing ||
                    now < condition.LastSent + TimeSpan.FromSeconds(condition.SnapshotInterval)) &&
                    (!condition.Dirty ||
                        now < condition.LastSent + TimeSpan.FromSeconds(condition.UpdateInterval)))
                {
                    continue;
                }
                var notifications = condition.Active.Values
                    .SelectMany(value => value.Select(CloneNotification))
                    .ToList();
                condition.Dirty = false;
                condition.LastSent = now;
                if (notifications.Count != 0)
                {
                    var message = CreateNotification(notifications, NextSequenceNumber(),
                        now.UtcDateTime, MessageType.Condition);
                    Deliver(binding.Owner, message,
                        static (owner, notification) => owner.OnSubscriptionEventReceived(notification));
                    InvokeSubscriber(binding.Owner,
                        owner => owner.OnSubscriptionEventDiagnosticsChange(true,
                            notifications.Count, notifications.Sum(item => item.Overflow), 0));
                }
            }
        }

        /// <inheritdoc/>
        public ValueTask OnDataChangeNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime,
            ReadOnlyMemory<DataValueChange> notification, PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
        {
            var deliveries = new Dictionary<(ISubscriber Owner, bool Cyclic),
                List<MonitoredItemNotificationModel>>();
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
                if (!deliveries.TryGetValue(key, out var notifications))
                {
                    notifications = [];
                    deliveries.Add(key, notifications);
                }
                notifications.Add(CreateDataNotification(binding, sequenceNumber, change.Value));
            }

            foreach (var ((owner, cyclic), notifications) in deliveries)
            {
                var message = RequiresKeyFrame(owner)
                    ? CreateKeyFrame(owner, sequenceNumber, publishTime)
                    : CreateNotification(notifications, sequenceNumber, publishTime,
                        MessageType.DeltaFrame);
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
                    InvokeSubscriber(owner, subscriber =>
                        subscriber.OnSubscriptionCyclicReadDiagnosticsChange(message.Notifications.Count,
                            message.Notifications.Sum(item => item.Overflow)));
                }
                else
                {
                    Deliver(owner, message,
                        static (subscriber, notification) =>
                            subscriber.OnSubscriptionDataChangeReceived(notification));
                    InvokeSubscriber(owner, subscriber =>
                        subscriber.OnSubscriptionDataDiagnosticsChange(true,
                            message.Notifications.Count,
                            message.Notifications.Sum(item => item.Overflow), 0));
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
                if (binding.Template is MonitoredAddressSpaceModel addressSpace)
                {
                    await ProcessModelChangeAsync(binding, addressSpace, item.Fields, ct: default)
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
                AddEventNotifications(notifications, binding, sequenceNumber, item.Fields);
            }

            foreach (var (owner, notifications) in deliveries)
            {
                if (notifications.Count == 0)
                {
                    continue;
                }
                var message = CreateNotification(notifications, sequenceNumber, publishTime,
                    MessageType.Event);
                Deliver(owner, message,
                    static (subscriber, notification) =>
                        subscriber.OnSubscriptionEventReceived(notification));
                InvokeSubscriber(owner, subscriber => subscriber.OnSubscriptionEventDiagnosticsChange(
                    true, notifications.Count, notifications.Sum(item => item.Overflow), 0));
            }
        }

        /// <inheritdoc/>
        public ValueTask OnKeepAliveNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime, PublishState publishStateMask)
        {
            foreach (var owner in GetBindings().Select(binding => binding.Owner).Distinct())
            {
                if (RequiresKeyFrame(owner))
                {
                    var keyFrame = CreateKeyFrame(owner, sequenceNumber, publishTime);
                    if (keyFrame != null)
                    {
                        MarkKeyFrameDelivered(owner, true);
                        Deliver(owner, keyFrame,
                            static (subscriber, notification) =>
                                subscriber.OnSubscriptionDataChangeReceived(notification));
                    }
                }
                var keepAlive = CreateNotification([], sequenceNumber, publishTime,
                    MessageType.KeepAlive);
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
                    var keyFrame = CreateKeyFrame(owner, NextSequenceNumber(),
                        _timeProvider.GetUtcNow().UtcDateTime);
                    if (keyFrame != null)
                    {
                        MarkKeyFrameDelivered(owner, true);
                        Deliver(owner, keyFrame,
                            static (subscriber, notification) =>
                                subscriber.OnSubscriptionDataChangeReceived(notification));
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
            _conditionTimer.Dispose();
            _keyFrameTimer?.Dispose();
            lock (_bindingsLock)
            {
                _bindingsByHandle.Clear();
                _bindingsByName.Clear();
                _ownerStates.Clear();
            }
            await _subscription.DisposeAsync().ConfigureAwait(false);
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
                await _subscription.SetTriggeringAsync(parentItem, [childItem!], null, ct)
                    .ConfigureAwait(false);
                if (!await AddTriggeredItemsAsync(child, childItem!, rootName, added, ct)
                    .ConfigureAwait(false))
                {
                    return false;
                }
            }
            return true;
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

        private void UpdateBindings(List<ManagedSubscriptionItemBinding> desired)
        {
            var state = desired.Select(binding => (binding.Name,
                (IOptionsMonitor<MonitoredItemOptions>)binding.Monitor)).ToList();
            var monitoredItems = _subscription.MonitoredItems.Update(state);
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
        }

        private bool TryAddBinding(ManagedSubscriptionItemBinding binding,
            out IMonitoredItem? monitoredItem)
        {
            if (binding.Template is MonitoredAddressSpaceModel && _modelChangeSink == null)
            {
                throw new NotSupportedException(
                    "V2 ISubscription exposes model-change notifications but no public rebrowse/change-feed API. Supply IModelChangeRebrowseSink.");
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
            MonitoredAddressSpaceModel template, ArrayOf<Variant> fields, CancellationToken ct)
        {
            if (fields.Count < 2 ||
                !fields[0].TryGetValue(out NodeId eventType) ||
                eventType != ObjectTypeIds.BaseModelChangeEventType &&
                eventType != ObjectTypeIds.GeneralModelChangeEventType)
            {
                return;
            }
            var changes = new DataValue(CoreUtils.Clone(fields[1]));
            await InvokeModelChangeSinkAsync(binding.Owner, template, changes, ct)
                .ConfigureAwait(false);
        }

        private async ValueTask ProcessConditionAsync(ISubscription subscription,
            ManagedSubscriptionItemBinding binding, uint sequenceNumber, DateTime publishTime,
            ArrayOf<Variant> fields)
        {
            var condition = binding.Condition!;
            if (condition.EventTypeIndex >= 0 && condition.EventTypeIndex < fields.Count &&
                fields[condition.EventTypeIndex].TryGetValue(out NodeId eventType))
            {
                if (eventType == ObjectTypeIds.RefreshStartEventType)
                {
                    condition.Active.Clear();
                    condition.Refreshing = true;
                    return;
                }
                if (eventType == ObjectTypeIds.RefreshEndEventType)
                {
                    condition.Refreshing = false;
                    condition.Dirty = true;
                    FlushConditions(force: true);
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
            AddEventNotifications(notifications, binding, sequenceNumber, fields);
            if (notifications.Count == 0)
            {
                return;
            }
            condition.Active[id] = notifications.Select(CloneNotification).ToList();
            condition.Dirty = true;
            if (_timeProvider.GetUtcNow() >= condition.LastSent +
                TimeSpan.FromSeconds(condition.UpdateInterval))
            {
                FlushConditions(force: true);
            }
        }

        private static MonitoredItemNotificationModel CreateDataNotification(
            ManagedSubscriptionItemBinding binding, uint sequenceNumber, DataValue value)
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
                SequenceNumber = sequenceNumber
            };
        }

        private static void AddEventNotifications(List<MonitoredItemNotificationModel> notifications,
            ManagedSubscriptionItemBinding binding, uint sequenceNumber, ArrayOf<Variant> fields)
        {
            var names = binding.EventFieldNames;
            var count = Math.Min(names.Count, fields.Count);
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
                    Value = new DataValue(CoreUtils.Clone(fields[index])),
                    SequenceNumber = sequenceNumber
                });
            }
        }

        private OpcUaSubscriptionNotification? CreateKeyFrame(ISubscriber owner,
            uint sequenceNumber, DateTime publishTime)
        {
            var values = GetBindings()
                .Where(binding => binding.Owner.Equals(owner) &&
                    binding.Template is DataMonitoredItemModel)
                .Select(binding => CreateDataNotification(binding, sequenceNumber,
                    binding.LastDataValue ?? DataValue.FromStatusCode(StatusCodes.BadNoData)))
                .ToList();
            return values.Count == 0 ? null :
                CreateNotification(values, sequenceNumber, publishTime, MessageType.KeyFrame);
        }

        private OpcUaSubscriptionNotification CreateNotification(
            IList<MonitoredItemNotificationModel> notifications, uint sequenceNumber,
            DateTime publishTime, MessageType messageType)
        {
            return new OpcUaSubscriptionNotification(_timeProvider.GetUtcNow(),
                _codec.Context as ServiceMessageContext, notifications)
            {
                MessageType = messageType,
                PublishTimestamp = new DateTimeOffset(publishTime),
                SequenceNumber = sequenceNumber
            };
        }

        private bool RequiresKeyFrame(ISubscriber owner)
        {
            lock (_bindingsLock)
            {
                var state = GetOwnerState(owner);
                return state.KeyFrameRequired || _periodicKeyFrameInterval.HasValue &&
                    _timeProvider.GetUtcNow() >= state.LastKeyFrame +
                    _periodicKeyFrameInterval.Value;
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
                var state = GetOwnerState(owner);
                state.KeyFrameRequired = false;
                state.LastKeyFrame = _timeProvider.GetUtcNow();
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

        private static DataValue Clone(in DataValue value)
        {
            return new DataValue(CoreUtils.Clone(value.WrappedValue), value.StatusCode,
                value.SourceTimestamp, value.ServerTimestamp, value.SourcePicoseconds,
                value.ServerPicoseconds);
        }

        private static MonitoredItemNotificationModel CloneNotification(
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
                var notification = CreateKeyFrame(owner, NextSequenceNumber(),
                    _timeProvider.GetUtcNow().UtcDateTime);
                if (notification != null)
                {
                    MarkKeyFrameDelivered(owner, true);
                    Deliver(owner, notification,
                        static (subscriber, keyFrame) =>
                            subscriber.OnSubscriptionDataChangeReceived(keyFrame));
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

            private void Deliver(ISubscriber owner, OpcUaSubscriptionNotification notification,
                Action<ISubscriber, OpcUaSubscriptionNotification> callback)
            {
                using (notification)
                {
                    InvokeSubscriber(owner, subscriber => callback(subscriber, notification));
                }
            }
            catch (Exception ex)
            {
                _logger.SubscriberCallbackFailed(ex, owner.GetType().Name);
            }
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

        private async ValueTask InvokeModelChangeSinkAsync(ISubscriber owner,
            MonitoredAddressSpaceModel template, DataValue changes, CancellationToken ct)
        {
            try
            {
                await _modelChangeSink!.ProcessAsync(owner, template, changes, ct)
                    .ConfigureAwait(false);
                await InvokeSubscriberAsync(owner, subscriber =>
                    subscriber.OnMonitoredItemSemanticsChangedAsync(ct)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ModelChangeSinkFailed(ex, owner.GetType().Name);
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
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
                Owner = owner;
                Template = template;
                Monitor.Update(options);
                (EventFieldNames, Condition) = CreateEventLayout(template, options, Condition);
            }

            public bool SkipFirstDataChange()
            {
                return Template is DataMonitoredItemModel { SkipFirst: true } &&
                    Interlocked.Exchange(ref _firstDataChange, 1) == 0;
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
                    } ? existing ?? new ConditionState(eventTemplate) : null;
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
            public bool Refreshing { get; set; }
            public Dictionary<string, List<MonitoredItemNotificationModel>> Active { get; } = [];

            public ConditionState(EventMonitoredItemModel template)
            {
                SnapshotInterval = template.ConditionHandling!.SnapshotInterval!.Value;
                UpdateInterval = template.ConditionHandling.UpdateInterval ?? SnapshotInterval;
            }
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
        private readonly ILogger _logger;
        private readonly IModelChangeRebrowseSink? _modelChangeSink;
        private readonly OpcUaSubscriptionOptions _options;
        private readonly Dictionary<ISubscriber, OwnerState> _ownerStates = [];
        private readonly TimeSpan? _periodicKeyFrameInterval;
        private readonly ISubscription _subscription;
        private readonly TimeProvider _timeProvider;
        private readonly ITimer? _keyFrameTimer;
        private int _disposed;
        private int _nextItemName;
        private uint _sequenceNumber;
        private bool _created;
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
            Message = "The model-change sink failed for {Subscriber}.")]
        public static partial void ModelChangeSinkFailed(this ILogger logger,
            Exception exception, string subscriber);
    }
}
