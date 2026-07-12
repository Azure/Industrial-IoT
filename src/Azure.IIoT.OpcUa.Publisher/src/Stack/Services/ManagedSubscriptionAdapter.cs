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
        /// <param name="timeProvider">The time provider for notification creation.</param>
        public ManagedSubscriptionAdapter(ISubscriptionManager manager,
            SubscriptionModel template, OpcUaSubscriptionOptions options,
            IVariantEncoder codec, TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(codec);

            _options = options;
            _codec = codec;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _subscription = manager.Add(this, new StaticOptionsMonitor<ManagedSubscriptionOptions>(
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(template, options)));
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
        /// Adds one Publisher monitored item to the V2 logical subscription.
        /// </summary>
        /// <param name="owner">The Publisher subscriber that owns the item.</param>
        /// <param name="template">The Publisher item template.</param>
        /// <returns>
        /// <c>true</c> if V2 accepted the item; otherwise <c>false</c>.
        /// </returns>
        internal bool TryAdd(ISubscriber owner, BaseMonitoredItemModel template)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(template);

            ThrowIfDisposed();

            var binding = CreateBinding(CreateName(template), owner, template);
            if (!_subscription.MonitoredItems.TryAdd(binding.Name,
                new StaticOptionsMonitor<MonitoredItemOptions>(binding.Options),
                out var monitoredItem) || monitoredItem == null)
            {
                return false;
            }

            lock (_bindingsLock)
            {
                if (_bindingsByHandle.ContainsKey(monitoredItem.ClientHandle) ||
                    _bindingsByName.ContainsKey(binding.Name))
                {
                    // A V2 item is already present with this identity. Do not
                    // retain an ambiguous Publisher binding.
                    _subscription.MonitoredItems.TryRemove(monitoredItem.ClientHandle);
                    return false;
                }

                binding.ClientHandle = monitoredItem.ClientHandle;
                _bindingsByHandle.Add(monitoredItem.ClientHandle, binding);
                _bindingsByName.Add(binding.Name, binding);
            }
            owner.OnMonitoredItemUpdate(template, ToServiceResult(monitoredItem));
            return true;
        }

        /// <summary>
        /// Replaces the V2 collection's desired item state with Publisher
        /// bindings. V2 performs the server-side add, update and remove work.
        /// </summary>
        /// <param name="items">The desired Publisher items and owners.</param>
        internal void Update(IEnumerable<(ISubscriber Owner, BaseMonitoredItemModel Template)> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            ThrowIfDisposed();

            var desired = new List<ManagedSubscriptionItemBinding>();
            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (owner, template) in items)
            {
                ArgumentNullException.ThrowIfNull(owner);
                ArgumentNullException.ThrowIfNull(template);

                var baseName = GetNamePrefix(template);
                names.TryGetValue(baseName, out var ordinal);
                names[baseName] = ordinal + 1;
                desired.Add(CreateBinding($"{baseName}:{ordinal}", owner, template));
            }

            var state = desired
                .Select(binding => (binding.Name,
                    (IOptionsMonitor<MonitoredItemOptions>)new StaticOptionsMonitor<MonitoredItemOptions>(
                        binding.Options)))
                .ToList();
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
                    _bindingsByHandle.Add(monitoredItem.ClientHandle, binding);
                    _bindingsByName.Add(binding.Name, binding);
                }
            }

            foreach (var binding in GetBindings())
            {
                if (_subscription.MonitoredItems.TryGetMonitoredItemByClientHandle(
                    binding.ClientHandle, out var monitoredItem) && monitoredItem != null)
                {
                    binding.Owner.OnMonitoredItemUpdate(binding.Template,
                        ToServiceResult(monitoredItem));
                }
            }
        }

        /// <summary>
        /// Removes one bound item from V2 and the Publisher binding map.
        /// </summary>
        /// <param name="clientHandle">The V2 client handle.</param>
        /// <returns><c>true</c> if V2 removed the item.</returns>
        internal bool TryRemove(uint clientHandle)
        {
            ThrowIfDisposed();
            if (!_subscription.MonitoredItems.TryRemove(clientHandle))
            {
                return false;
            }

            lock (_bindingsLock)
            {
                if (_bindingsByHandle.Remove(clientHandle, out var binding))
                {
                    _bindingsByName.Remove(binding.Name);
                }
            }
            return true;
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
                var message = CreateNotification(notifications, sequenceNumber, publishTime,
                    MessageType.DeltaFrame);
                if (cyclic)
                {
                    owner.OnSubscriptionCyclicReadCompleted(message);
                    owner.OnSubscriptionCyclicReadDiagnosticsChange(notifications.Count,
                        notifications.Sum(item => item.Overflow));
                }
                else
                {
                    owner.OnSubscriptionDataChangeReceived(message);
                    owner.OnSubscriptionDataDiagnosticsChange(true, notifications.Count,
                        notifications.Sum(item => item.Overflow), 0);
                }
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask OnEventDataNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime,
            ReadOnlyMemory<EventNotification> notification, PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
        {
            var deliveries = new Dictionary<ISubscriber, List<MonitoredItemNotificationModel>>();
            var modelChanges = new Dictionary<ISubscriber, bool>();
            foreach (var item in notification.Span)
            {
                if (!TryGetBinding(item.MonitoredItem, out var binding))
                {
                    continue;
                }

                if (!deliveries.TryGetValue(binding.Owner, out var notifications))
                {
                    notifications = [];
                    deliveries.Add(binding.Owner, notifications);
                }

                var isModelChange = binding.Template is MonitoredAddressSpaceModel;
                modelChanges[binding.Owner] = isModelChange ||
                    modelChanges.GetValueOrDefault(binding.Owner);
                AddEventNotifications(notifications, binding, sequenceNumber, item.Fields,
                    isModelChange);
            }

            foreach (var (owner, notifications) in deliveries)
            {
                if (notifications.Count == 0)
                {
                    continue;
                }

                owner.OnSubscriptionEventReceived(CreateNotification(notifications,
                    sequenceNumber, publishTime, MessageType.Event));
                owner.OnSubscriptionEventDiagnosticsChange(true, notifications.Count,
                    notifications.Sum(item => item.Overflow),
                    modelChanges.GetValueOrDefault(owner) ? notifications.Count : 0);
            }
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask OnKeepAliveNotificationAsync(ISubscription subscription,
            uint sequenceNumber, DateTime publishTime, PublishState publishStateMask)
        {
            foreach (var owner in GetBindings().Select(binding => binding.Owner).Distinct())
            {
                owner.OnSubscriptionKeepAlive(CreateNotification([], sequenceNumber,
                    publishTime, MessageType.KeepAlive));
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
                if (_subscription.MonitoredItems.TryGetMonitoredItemByClientHandle(
                    binding.ClientHandle, out var monitoredItem) && monitoredItem != null)
                {
                    binding.Owner.OnMonitoredItemUpdate(binding.Template,
                        ToServiceResult(monitoredItem));
                }
            }

            var recovering = publishStateMask.HasFlag(PublishState.Recovered) ||
                publishStateMask.HasFlag(PublishState.Transferred);
            if (recovering || (state == SubscriptionState.Created && _created))
            {
                foreach (var owner in bindings.Select(binding => binding.Owner).Distinct())
                {
                    await owner.OnMonitoredItemSemanticsChangedAsync(ct).ConfigureAwait(false);
                }
            }
            _created |= state == SubscriptionState.Created;

            if (state is SubscriptionState.Created or SubscriptionState.Modified &&
                bindings.Any(binding => binding.Template is EventMonitoredItemModel
                    {
                        ConditionHandling: { SnapshotInterval: not null }
                    }))
            {
                try
                {
                    await subscription.ConditionRefreshAsync(ct).ConfigureAwait(false);
                }
                catch (ServiceResultException)
                {
                    // A condition refresh remains a Publisher concern. V2 has
                    // reported the subscription state and a later lifecycle
                    // transition retries it without retaining pooled payloads.
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

            lock (_bindingsLock)
            {
                _bindingsByHandle.Clear();
                _bindingsByName.Clear();
            }
            await _subscription.DisposeAsync().ConfigureAwait(false);
        }

        private ManagedSubscriptionItemBinding CreateBinding(string name,
            ISubscriber owner, BaseMonitoredItemModel template)
        {
            return new ManagedSubscriptionItemBinding(name, owner,
                template.SetDefaults(_options),
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(template, _options, _codec));
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
            ManagedSubscriptionItemBinding binding, uint sequenceNumber,
            ArrayOf<Variant> fields, bool isModelChange)
        {
            var names = binding.GetEventFieldNames();
            var count = Math.Min(names.Count, fields.Count);
            for (var i = 0; i < count; i++)
            {
                if (names[i] == null)
                {
                    continue;
                }

                notifications.Add(new MonitoredItemNotificationModel
                {
                    Id = binding.Template.Id ?? string.Empty,
                    DataSetName = binding.Template.DisplayName,
                    DataSetFieldName = names[i],
                    NodeId = binding.Template.StartNodeId,
                    Value = new DataValue(CoreUtils.Clone(fields[i])),
                    Flags = isModelChange ? MonitoredItemSourceFlags.ModelChanges : 0,
                    SequenceNumber = sequenceNumber
                });
            }
        }

        private OpcUaSubscriptionNotification CreateNotification(
            IList<MonitoredItemNotificationModel> notifications, uint sequenceNumber,
            DateTime publishTime, MessageType messageType)
        {
            return new OpcUaSubscriptionNotification(_timeProvider.GetUtcNow(),
                _codec.Context, notifications)
            {
                MessageType = messageType,
                PublishTimestamp = new DateTimeOffset(publishTime),
                SequenceNumber = sequenceNumber
            };
        }

        private bool TryGetBinding(IMonitoredItem? monitoredItem,
            out ManagedSubscriptionItemBinding binding)
        {
            binding = null!;
            return monitoredItem != null && TryGetBinding(monitoredItem.ClientHandle,
                out binding);
        }

        private bool TryGetBinding(uint clientHandle,
            out ManagedSubscriptionItemBinding binding)
        {
            lock (_bindingsLock)
            {
                return _bindingsByHandle.TryGetValue(clientHandle, out binding!);
            }
        }

        private ManagedSubscriptionItemBinding[] GetBindings()
        {
            lock (_bindingsLock)
            {
                return [.. _bindingsByHandle.Values];
            }
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

        private static ServiceResultModel? ToServiceResult(IMonitoredItem monitoredItem)
        {
            return ServiceResult.IsGood(monitoredItem.Error) ? null :
                monitoredItem.Error.ToServiceResultModel();
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
        }

        private sealed class ManagedSubscriptionItemBinding
        {
            public uint ClientHandle { get; set; }
            public string Name { get; }
            public ISubscriber Owner { get; }
            public BaseMonitoredItemModel Template { get; }
            public MonitoredItemOptions Options { get; }

            public ManagedSubscriptionItemBinding(string name, ISubscriber owner,
                BaseMonitoredItemModel template, MonitoredItemOptions options)
            {
                Name = name;
                Owner = owner;
                Template = template;
                Options = options;
            }

            public bool SkipFirstDataChange()
            {
                return Template is DataMonitoredItemModel { SkipFirst: true } &&
                    Interlocked.Exchange(ref _firstDataChange, 1) == 0;
            }

            public IReadOnlyList<string?> GetEventFieldNames()
            {
                return Template switch
                {
                    EventMonitoredItemModel eventItem => eventItem.EventFilter.SelectClauses?
                        .Select(clause => clause.DisplayName ??
                            (clause.BrowsePath is { Count: > 0 } ?
                                string.Join("/", clause.BrowsePath) : null))
                        .ToArray() ?? [],
                    MonitoredAddressSpaceModel => ["EventType", "Changes"],
                    _ => []
                };
            }

            private int _firstDataChange;
        }

        private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
        {
            public T CurrentValue { get; }

            public StaticOptionsMonitor(T value)
            {
                CurrentValue = value ?? throw new ArgumentNullException(nameof(value));
            }

            public T Get(string? name)
            {
                return CurrentValue;
            }

            public IDisposable OnChange(Action<T, string?> listener)
            {
                ArgumentNullException.ThrowIfNull(listener);
                return EmptyDisposable.Instance;
            }

            private sealed class EmptyDisposable : IDisposable
            {
                public static EmptyDisposable Instance { get; } = new();

                public void Dispose()
                {
                }
            }
        }

        private readonly Lock _bindingsLock = new();
        private readonly Dictionary<uint, ManagedSubscriptionItemBinding> _bindingsByHandle = [];
        private readonly Dictionary<string, ManagedSubscriptionItemBinding> _bindingsByName =
            new(StringComparer.Ordinal);
        private readonly IVariantEncoder _codec;
        private readonly OpcUaSubscriptionOptions _options;
        private readonly ISubscription _subscription;
        private readonly TimeProvider _timeProvider;
        private int _disposed;
        private int _nextItemName;
        private bool _created;
    }
}
