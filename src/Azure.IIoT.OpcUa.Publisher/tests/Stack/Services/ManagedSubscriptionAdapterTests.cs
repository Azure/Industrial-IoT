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
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using PublisherMonitoringMode = Azure.IIoT.OpcUa.Publisher.Models.MonitoringMode;

    public sealed class ManagedSubscriptionAdapterTests
    {
        [Fact]
        public void DocumentsEveryPublisherSubscriptionOption()
        {
            var optionNames = typeof(OpcUaSubscriptionOptions).GetProperties()
                .Select(property => property.Name);

            Assert.All(optionNames, name =>
                Assert.True(ManagedSubscriptionOptionsAdapter.OptionBehaviors.ContainsKey(name),
                    $"Missing V2 adapter behavior for {name}."));
        }

        [Fact]
        public async Task MapsNullPartitionCapToV2UnboundedSentinelAndAllowsMoreThan32Items()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();

            for (var i = 0; i < 33; i++)
            {
                Assert.True(adapter.TryAdd(owner, CreateDataItem($"ns=2;s=item-{i}")));
            }

            Assert.Equal(0u, manager.CapturedOptions!.MaxPartitionCount);
            Assert.Equal(33, adapter.BindingCount);
            Assert.Equal(33u, manager.Subscription!.Collection.Count);
        }

        [Fact]
        public async Task RejectsExplicitCapAndCleansUpBindingAndSubscription()
        {
            var manager = new FakeSubscriptionManager(maxItems: 2);
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions
            {
                MaxSubscriptionPartitions = 2
            });
            var owner = new FakeSubscriber();

            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=one")));
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=two")));
            Assert.False(adapter.TryAdd(owner, CreateDataItem("ns=2;s=three")));

            Assert.Equal(2u, manager.CapturedOptions!.MaxPartitionCount);
            Assert.Equal(2, adapter.BindingCount);
            Assert.Equal(2u, manager.Subscription!.Collection.Count);

            await adapter.DisposeAsync();

            Assert.Equal(1, manager.Subscription.DisposeCount);
            Assert.Equal(0, adapter.BindingCount);
        }

        [Fact]
        public void RejectsZeroAsAnExplicitPartitionCap()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateAdapter(new FakeSubscriptionManager(), new OpcUaSubscriptionOptions
                {
                    MaxSubscriptionPartitions = 0
                }));

            Assert.Contains("partition cap", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TranslatesDataItemQueuesModesAndFilters()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultPublishingInterval = TimeSpan.FromSeconds(2)
            };
            var translated = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new DataMonitoredItemModel
                {
                    StartNodeId = "ns=2;s=value",
                    SamplingInterval = TimeSpan.FromMilliseconds(250),
                    QueueSize = 5,
                    DiscardNew = true,
                    MonitoringMode = PublisherMonitoringMode.Sampling,
                    DataChangeFilter = new DataChangeFilterModel
                    {
                        DeadbandValue = 1.5
                    }
                }, options, new JsonVariantEncoder(new ServiceMessageContext()));

            Assert.Equal(TimeSpan.FromMilliseconds(250), translated.SamplingInterval);
            Assert.Equal(5u, translated.QueueSize);
            Assert.False(translated.DiscardOldest);
            Assert.Equal(Opc.Ua.MonitoringMode.Sampling, translated.MonitoringMode);
            Assert.IsType<DataChangeFilter>(translated.Filter);
        }

        [Fact]
        public async Task UsesUniqueHandlesSkipsFirstValueAndDeepCopiesPooledPayloads()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions
            {
                DefaultSkipFirst = true
            });
            var owner = new FakeSubscriber();

            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=one")));
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=two")));
            var items = manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>().ToArray();
            Assert.Equal(2, items.Select(item => item.ClientHandle).Distinct().Count());

            var source = new[] { 1, 2 };
            await manager.Handler!.OnDataChangeNotificationAsync(manager.Subscription, 7,
                DateTime.UtcNow, new[]
                {
                    new DataValueChange(items[0], new DataValue(new Variant(source)), null)
                }, PublishState.None, []);
            Assert.Empty(owner.DataChanges);

            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 8,
                DateTime.UtcNow, new[]
                {
                    new DataValueChange(items[0], new DataValue(new Variant(source)), null)
                }, PublishState.None, []);
            source[0] = 99;

            var notification = Assert.Single(owner.DataChanges);
            var monitoredItem = Assert.Single(notification.Notifications);
            var value = Assert.IsType<DataValue>(monitoredItem.Value);
            Assert.True(value.WrappedValue.TryGetValue(out int[] copied));
            Assert.Equal([1, 2], copied);
            Assert.Equal(MessageType.DeltaFrame, notification.MessageType);
        }

        [Fact]
        public async Task ConvertsEventAndModelChangeNotifications()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, new EventMonitoredItemModel
            {
                StartNodeId = "ns=2;s=events",
                EventFilter = new EventFilterModel
                {
                    SelectClauses = [new SimpleAttributeOperandModel
                    {
                        DisplayName = "Severity"
                    }]
                }
            }));
            Assert.True(adapter.TryAdd(owner, new MonitoredAddressSpaceModel
            {
                StartNodeId = "ns=2;s=model"
            }));
            Assert.True(adapter.TryAdd(owner, new EventMonitoredItemModel
            {
                StartNodeId = "ns=2;s=conditions",
                ConditionHandling = new ConditionHandlingOptionsModel
                {
                    SnapshotInterval = 1
                },
                EventFilter = new EventFilterModel
                {
                    SelectClauses = [new SimpleAttributeOperandModel
                    {
                        DisplayName = "ConditionId"
                    }]
                }
            }));
            var items = manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>().ToArray();

            await manager.Handler!.OnEventDataNotificationAsync(manager.Subscription, 11,
                DateTime.UtcNow,
                [
                    new EventNotification(items[0], ArrayOf.Wrapped(Variant.From(500))),
                    new EventNotification(items[1], ArrayOf.Wrapped(
                        Variant.From(ObjectTypeIds.BaseModelChangeEventType),
                        Variant.From("changed"))),
                    new EventNotification(items[2], ArrayOf.Wrapped(Variant.From("condition")))
                ], PublishState.None, []);
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Created, PublishState.None);

            var notification = Assert.Single(owner.Events);
            Assert.Equal(MessageType.Event, notification.MessageType);
            Assert.Equal(4, notification.Notifications.Count);
            Assert.Equal("Severity", notification.Notifications[0].DataSetFieldName);
            Assert.Equal(MonitoredItemSourceFlags.ModelChanges,
                notification.Notifications[1].Flags);
            Assert.Equal(1, manager.Subscription.ConditionRefreshCount);
        }

        [Fact]
        public async Task ReportsReactiveLimitRecoveryAndReconnectSemantics()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=one")));
            var item = Assert.Single(manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());
            item.Error = new ServiceResult(StatusCodes.BadTooManyMonitoredItems);

            await manager.Handler!.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, PublishState.None);

            var update = owner.Updates.Last();
            Assert.NotNull(update);
            Assert.Equal(StatusCodes.BadTooManyMonitoredItems.Code, update!.StatusCode);

            item.Error = ServiceResult.Good;
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Created, PublishState.Recovered | PublishState.Transferred);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 12,
                DateTime.UtcNow,
                [new DataValueChange(item, new DataValue(Variant.From(42)), null)],
                PublishState.Recovered, []);

            Assert.Equal(1, owner.SemanticsChanges);
            Assert.Single(owner.DataChanges);
        }

        [Fact]
        public async Task UpdateAndRemoveKeepTheBindingMapInSync()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();

            adapter.Update([(owner, CreateDataItem("ns=2;s=one"))]);
            Assert.Equal(1, adapter.BindingCount);
            var handle = Assert.Single(manager.Subscription!.Collection.Items).ClientHandle;
            Assert.True(adapter.TryRemove(handle));
            Assert.Equal(0, adapter.BindingCount);

            adapter.Update([]);
            Assert.Equal(0u, manager.Subscription.Collection.Count);
        }

        private static ManagedSubscriptionAdapter CreateAdapter(FakeSubscriptionManager manager,
            OpcUaSubscriptionOptions options)
        {
            return new ManagedSubscriptionAdapter(manager, new SubscriptionModel(), options,
                new JsonVariantEncoder(new ServiceMessageContext()));
        }

        private static DataMonitoredItemModel CreateDataItem(string nodeId)
        {
            return new DataMonitoredItemModel
            {
                StartNodeId = nodeId
            };
        }

        private sealed class FakeSubscriptionManager : ISubscriptionManager
        {
            public FakeSubscription? Subscription { get; private set; }
            public ISubscriptionNotificationHandler? Handler { get; private set; }
            public SubscriptionOptions? CapturedOptions { get; private set; }

            public FakeSubscriptionManager(uint maxItems = 0)
            {
                _maxItems = maxItems;
            }

            public ISubscription Add(ISubscriptionNotificationHandler handler,
                IOptionsMonitor<SubscriptionOptions> options)
            {
                Handler = handler;
                CapturedOptions = options.CurrentValue;
                Subscription = new FakeSubscription(_maxItems);
                return Subscription;
            }

            public DiagnosticsMasks ReturnDiagnostics { get; set; }
            public int MaxPublishWorkerCount { get; set; }
            public int MinPublishWorkerCount { get; set; }
            public int PublishWorkerCount => 0;
            public int GoodPublishRequestCount => 0;
            public int BadPublishRequestCount => 0;
            public long MissingMessageCount => 0;
            public long RepublishMessageCount => 0;
            public int Count => Subscription == null ? 0 : 1;
            public bool PoolNotifications { get; set; }
            public IEnumerable<ISubscription> Items => Subscription == null ? [] : [Subscription];

            public ValueTask SaveAsync(Stream stream, IServiceMessageContext messageContext,
                IEnumerable<ISubscription>? subscriptions = null, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public ValueTask<IReadOnlyList<ISubscription>> LoadAsync(Stream stream,
                IServiceMessageContext messageContext,
                Func<string, ISubscriptionNotificationHandler> handlerFactory,
                bool transferSubscriptions = false, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            private readonly uint _maxItems;
        }

        private sealed class FakeSubscription : ISubscription
        {
            public FakeCollection Collection { get; }
            public int DisposeCount { get; private set; }
            public int ConditionRefreshCount { get; private set; }

            public FakeSubscription(uint maxItems)
            {
                Collection = new FakeCollection(maxItems);
            }

            public bool Created => true;
            public TimeSpan CurrentPublishingInterval => TimeSpan.FromSeconds(1);
            public byte CurrentPriority => 0;
            public uint CurrentLifetimeCount => 0;
            public uint CurrentKeepAliveCount => 0;
            public bool CurrentPublishingEnabled => true;
            public uint CurrentMaxNotificationsPerPublish => 0;
            public IMonitoredItemCollection MonitoredItems => Collection;
            public long MissingMessageCount => 0;
            public long RepublishMessageCount => 0;

            public ValueTask ConditionRefreshAsync(CancellationToken ct = default)
            {
                ConditionRefreshCount++;
                return ValueTask.CompletedTask;
            }

            public ValueTask<TimeSpan> SetAsDurableAsync(TimeSpan lifetime,
                CancellationToken ct = default)
            {
                return ValueTask.FromResult(lifetime);
            }

            public ValueTask<SetTriggeringResult> SetTriggeringAsync(
                IMonitoredItem triggeringItem,
                IReadOnlyCollection<IMonitoredItem>? linksToAdd = null,
                IReadOnlyCollection<IMonitoredItem>? linksToRemove = null,
                CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public ValueTask DisposeAsync()
            {
                DisposeCount++;
                return ValueTask.CompletedTask;
            }
        }

        private sealed class FakeCollection : IMonitoredItemCollection
        {
            public uint Count => (uint)_items.Count;
            public IEnumerable<IMonitoredItem> Items => _items.Values;

            public FakeCollection(uint maxItems)
            {
                _maxItems = maxItems;
            }

            public bool TryGetMonitoredItemByClientHandle(uint clientHandle,
                out IMonitoredItem? monitoredItem)
            {
                if (_items.TryGetValue(clientHandle, out var item))
                {
                    monitoredItem = item;
                    return true;
                }
                monitoredItem = null;
                return false;
            }

            public bool TryGetMonitoredItemByName(string name,
                out IMonitoredItem? monitoredItem)
            {
                monitoredItem = _items.Values.FirstOrDefault(item =>
                    string.Equals(item.Name, name, StringComparison.Ordinal));
                return monitoredItem != null;
            }

            public bool TryAdd(string name, IOptionsMonitor<MonitoredItemOptions> options,
                out IMonitoredItem? monitoredItem)
            {
                if (_maxItems != 0 && _items.Count >= _maxItems)
                {
                    monitoredItem = null;
                    return false;
                }

                var item = new FakeMonitoredItem(_nextHandle++, name, options.CurrentValue);
                _items.Add(item.ClientHandle, item);
                monitoredItem = item;
                return true;
            }

            public bool TryRemove(uint clientHandle)
            {
                return _items.Remove(clientHandle);
            }

            public IReadOnlyList<IMonitoredItem> Update(
                IReadOnlyList<(string Name, IOptionsMonitor<MonitoredItemOptions> Options)> state)
            {
                var currentNames = _items.Values.Select(item => item.Name).ToHashSet(
                    StringComparer.Ordinal);
                foreach (var (name, options) in state)
                {
                    if (!currentNames.Contains(name))
                    {
                        _ = TryAdd(name, options, out _);
                    }
                }

                var desiredNames = state.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
                foreach (var handle in _items.Values
                    .Where(item => !desiredNames.Contains(item.Name))
                    .Select(item => item.ClientHandle).ToArray())
                {
                    _items.Remove(handle);
                }
                return _items.Values.Cast<IMonitoredItem>().ToArray();
            }

            private readonly Dictionary<uint, FakeMonitoredItem> _items = [];
            private readonly uint _maxItems;
            private uint _nextHandle = 1;
        }

        private sealed class FakeMonitoredItem : IMonitoredItem
        {
            public uint ClientHandle { get; }
            public string Name { get; }
            public ServiceResult Error { get; set; } = ServiceResult.Good;

            public FakeMonitoredItem(uint clientHandle, string name,
                MonitoredItemOptions options)
            {
                ClientHandle = clientHandle;
                Name = name;
                _options = options;
            }

            public uint Order => _options.Order;
            public uint ServerId => 0;
            public bool Created => true;
            public MonitoringFilterResult? FilterResult => null;
            public Opc.Ua.MonitoringMode CurrentMonitoringMode => _options.MonitoringMode;
            public TimeSpan CurrentSamplingInterval => _options.SamplingInterval;
            public uint CurrentQueueSize => _options.QueueSize;
            public IEnumerable<IMonitoredItem> TriggeringItems => [];
            public IEnumerable<IMonitoredItem> TriggeredItems => [];

            public ValueTask ConditionRefreshAsync(CancellationToken ct = default)
            {
                return ValueTask.CompletedTask;
            }

            private readonly MonitoredItemOptions _options;
        }

        private sealed class FakeSubscriber : ISubscriber
        {
            public int SemanticsChanges { get; private set; }
            public List<OpcUaSubscriptionNotification> DataChanges { get; } = [];
            public List<OpcUaSubscriptionNotification> Events { get; } = [];
            public List<ServiceResultModel?> Updates { get; } = [];
            public IEnumerable<BaseMonitoredItemModel> MonitoredItems => [];

            public Task OnMonitoredItemSemanticsChangedAsync(CancellationToken ct = default)
            {
                SemanticsChanges++;
                return Task.CompletedTask;
            }

            public void OnSubscriptionKeepAlive(OpcUaSubscriptionNotification notification)
            {
            }

            public void OnSubscriptionDataChangeReceived(OpcUaSubscriptionNotification notification)
            {
                DataChanges.Add(notification);
            }

            public void OnSubscriptionCyclicReadCompleted(OpcUaSubscriptionNotification notification)
            {
                DataChanges.Add(notification);
            }

            public void OnSubscriptionEventReceived(OpcUaSubscriptionNotification notification)
            {
                Events.Add(notification);
            }

            public void OnSubscriptionDataDiagnosticsChange(bool liveData, int valueChanges,
                int overflow, int heartbeats)
            {
            }

            public void OnSubscriptionCyclicReadDiagnosticsChange(int valuesSampled, int overflow)
            {
            }

            public void OnSubscriptionEventDiagnosticsChange(bool liveData, int events,
                int overflow, int modelChanges)
            {
            }

            public void OnMonitoredItemUpdate(BaseMonitoredItemModel monitoredItem,
                ServiceResultModel? serviceResult)
            {
                Updates.Add(serviceResult);
            }
        }
    }
}
