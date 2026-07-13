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
            Assert.Equal(MessageType.KeyFrame, notification.MessageType);
            var monitoredItem = notification.Notifications.Single(item =>
                item.NodeId == "ns=2;s=one");
            var value = Assert.IsType<DataValue>(monitoredItem.Value);
            Assert.Equal(new Variant(new[] { 1, 2 }), value.WrappedValue);
        }

        [Fact]
        public async Task CachesConditionsAndEmitsRefreshSnapshots()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
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
                new EventNotification[]
                {
                    new EventNotification(items[0], ArrayOf.Wrapped(
                        Variant.From("condition"),
                        Variant.From(ObjectTypeIds.BaseEventType),
                        Variant.From(new NodeId(1234u, 2)),
                        Variant.From(true)))
                }, PublishState.None, []);
            adapter.FlushConditions(force: true);

            var notification = Assert.Single(owner.Events);
            Assert.Equal(MessageType.Condition, notification.MessageType);
            Assert.Single(notification.Notifications);

            await manager.Handler.OnEventDataNotificationAsync(manager.Subscription, 12,
                DateTime.UtcNow,
                new EventNotification[]
                {
                    new EventNotification(items[0], ArrayOf.Wrapped(
                        Variant.From("ignored"),
                        Variant.From(ObjectTypeIds.RefreshStartEventType),
                        Variant.From(new NodeId(1234u, 2)),
                        Variant.From(true))),
                    new EventNotification(items[0], ArrayOf.Wrapped(
                        Variant.From("refreshed"),
                        Variant.From(ObjectTypeIds.BaseEventType),
                        Variant.From(new NodeId(1234u, 2)),
                        Variant.From(true))),
                    new EventNotification(items[0], ArrayOf.Wrapped(
                        Variant.From("ignored"),
                        Variant.From(ObjectTypeIds.RefreshEndEventType),
                        Variant.From(new NodeId(1234u, 2)),
                        Variant.From(true)))
                }, PublishState.None, []);

            Assert.Equal(2, owner.Events.Count);
            Assert.Equal(MessageType.Condition, owner.Events[1].MessageType);
        }

        [Fact]
        public async Task DoesNotFlushConditionsWhileRefreshIsActive()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateConditionItem("stable", 5, 2)));
            var item = Assert.Single(manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());

            await manager.Handler!.OnEventDataNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow, new EventNotification[]
                {
                    new(item, ArrayOf.Wrapped(
                        Variant.From("ignored"),
                        Variant.From(ObjectTypeIds.RefreshStartEventType),
                        Variant.From(new NodeId(1u, 2)),
                        Variant.From(true)))
                }, PublishState.None, []);
            adapter.FlushConditions(force: true);

            Assert.Empty(owner.Events);
        }

        [Fact]
        public async Task RejectsUnrelatedModelChangesAndUsesPublisherChangeFeedSink()
        {
            var manager = new FakeSubscriptionManager();
            var sink = new FakeModelChangeSink();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions(), sink);
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, new MonitoredAddressSpaceModel
            {
                StartNodeId = "ns=2;s=model"
            }));
            var item = Assert.Single(manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());

            await manager.Handler!.OnEventDataNotificationAsync(manager.Subscription, 11,
                DateTime.UtcNow, new EventNotification[] { new(item, ArrayOf.Wrapped(
                    Variant.From(ObjectTypeIds.BaseEventType), Variant.From("ignored"))) },
                PublishState.None, []);
            Assert.Equal(0, sink.CallCount);

            await manager.Handler.OnEventDataNotificationAsync(manager.Subscription, 12,
                DateTime.UtcNow, new EventNotification[] { new(item, ArrayOf.Wrapped(
                    Variant.From(ObjectTypeIds.GeneralModelChangeEventType),
                    Variant.From("changes"))) }, PublishState.None, []);
            Assert.Equal(1, sink.CallCount);
            Assert.Equal(1, owner.SemanticsChanges);
        }

        [Fact]
        public async Task AddsTriggeredItemTreeWithStableNamesAndV2Links()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };

            Assert.True(await adapter.TryAddAsync(owner, root));

            var items = manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>().ToArray();
            Assert.Equal(2, items.Length);
            Assert.Single(manager.Subscription.TriggeringCalls);
            var child = items.Single(item => item.Name.Contains("/triggered/", StringComparison.Ordinal));
            Assert.Equal(items[0].Options.Affinity, child.Options.Affinity);
            Assert.Equal([items[0].Name], child.Options.TriggeredByNames);

            Assert.True(adapter.TryRemove(items[0].ClientHandle));
            Assert.Equal(0u, manager.Subscription.Collection.Count);
        }

        [Fact]
        public async Task RollsBackTriggeredTreeWhenV2RejectsTriggering()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };

            manager.Subscription!.TriggerServiceStatus = StatusCodes.BadUnexpectedError;
            Assert.False(await adapter.TryAddAsync(owner, root));

            Assert.Equal(0, adapter.BindingCount);
            Assert.Equal(0u, manager.Subscription.Collection.Count);
            Assert.Contains(owner.Updates, update =>
                update?.StatusCode == StatusCodes.BadUnexpectedError.Code);
        }

        [Fact]
        public async Task RollsBackOnlyFailingTriggeredUpdateBranch()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };
            Assert.True(await adapter.TryAddAsync(owner, root));

            manager.Subscription!.TriggerAddStatus = StatusCodes.BadMonitoredItemIdInvalid;
            var exception = await Assert.ThrowsAsync<ServiceResultException>(() =>
                adapter.UpdateAsync([(owner, root)]).AsTask());

            Assert.Equal(StatusCodes.BadMonitoredItemIdInvalid, exception.Result.StatusCode);
            Assert.Single(manager.Subscription.Collection.Items);
        }

        [Fact]
        public async Task SurfacesMalformedTriggeringResultAsUnexpectedError()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };
            Assert.True(await adapter.TryAddAsync(owner, root));
            manager.Subscription!.TriggerResultCount = 0;

            var exception = await Assert.ThrowsAsync<ServiceResultException>(() =>
                adapter.UpdateAsync([(owner, root)]).AsTask());

            Assert.Equal(StatusCodes.BadUnexpectedError, exception.Result.StatusCode);
        }

        [Fact]
        public async Task SurfacesBadRemoveLinkStatusFromTriggeringResult()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };
            Assert.True(await adapter.TryAddAsync(owner, root));
            manager.Subscription!.TriggerRemoveStatus = StatusCodes.BadMonitoredItemIdInvalid;

            var exception = await Assert.ThrowsAsync<ServiceResultException>(() =>
                adapter.UpdateAsync([(owner, root)]).AsTask());

            Assert.Equal(StatusCodes.BadMonitoredItemIdInvalid, exception.Result.StatusCode);
        }

        [Fact]
        public async Task RemovesOnlySelectedTriggeredDescendants()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems =
                [
                    CreateDataItem("ns=2;s=left") with
                    {
                        TriggeredItems = [CreateDataItem("ns=2;s=left-grandchild")]
                    },
                    CreateDataItem("ns=2;s=right")
                ]
            };
            Assert.True(await adapter.TryAddAsync(owner, root));
            var left = manager.Subscription!.Collection.Items
                .Cast<FakeMonitoredItem>()
                .Single(item => item.Name.Contains("left", StringComparison.Ordinal) &&
                    !item.Name.Contains("grandchild", StringComparison.Ordinal));

            Assert.True(adapter.TryRemove(left.ClientHandle));

            var remaining = manager.Subscription.Collection.Items
                .Cast<FakeMonitoredItem>().Select(item => item.Name).ToArray();
            Assert.Equal(2, remaining.Length);
            Assert.Contains(remaining, name => name.Contains("root", StringComparison.Ordinal));
            Assert.Contains(remaining, name => name.Contains("right", StringComparison.Ordinal));
        }

        [Fact]
        public async Task RetainedMonitorUpdatesSamplingFilterQueueAndMode()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var first = CreateDataItem("ns=2;s=one") with
            {
                SamplingInterval = TimeSpan.FromSeconds(1),
                QueueSize = 1
            };
            adapter.Update([(owner, first)]);
            var item = Assert.Single(manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());
            var handle = item.ClientHandle;

            adapter.Update([(owner, first with
            {
                SamplingInterval = TimeSpan.FromMilliseconds(250),
                QueueSize = 4,
                MonitoringMode = PublisherMonitoringMode.Sampling,
                DataChangeFilter = new DataChangeFilterModel { DeadbandValue = 2.0 }
            })]);

            Assert.Equal(handle, Assert.Single(manager.Subscription.Collection.Items).ClientHandle);
            Assert.Equal(TimeSpan.FromMilliseconds(250), item.Options.SamplingInterval);
            Assert.Equal(4u, item.Options.QueueSize);
            Assert.Equal(Opc.Ua.MonitoringMode.Sampling, item.Options.MonitoringMode);
            Assert.IsType<DataChangeFilter>(item.Options.Filter);
        }

        [Fact]
        public async Task RetargetingStableBindingClearsCachedDataAndReplacesConditionSettings()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var first = CreateDataItem("ns=2;s=old") with { DataSetFieldId = "stable" };
            adapter.Update([(owner, first)]);
            var item = Assert.Single(manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());
            await manager.Handler!.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow, new DataValueChange[]
                {
                    new(item, new DataValue(Variant.From(42)), null)
                }, PublishState.None, []);

            adapter.Update([(owner, first with { StartNodeId = "ns=2;s=new" })]);
            adapter.RequestKeyFrame(owner);
            await manager.Handler.OnKeepAliveNotificationAsync(manager.Subscription, 2,
                DateTime.UtcNow, PublishState.KeepAlive);

            var keyFrame = owner.DataChanges.Last();
            var retargeted = Assert.Single(keyFrame.Notifications);
            var value = Assert.IsType<DataValue>(retargeted.Value);
            Assert.Equal(StatusCodes.BadNoData, value.StatusCode);

            var condition = CreateConditionItem("condition-stable", 5, 2);
            adapter.Update([(owner, condition)]);
            var conditionItem = Assert.Single(manager.Subscription.Collection.Items);
            Assert.True(adapter.TryGetConditionIntervals(conditionItem.ClientHandle,
                out var snapshot, out var update));
            Assert.Equal(5, snapshot);
            Assert.Equal(2, update);

            adapter.Update([(owner, CreateConditionItem("condition-stable", 10, 4))]);
            Assert.True(adapter.TryGetConditionIntervals(conditionItem.ClientHandle,
                out snapshot, out update));
            Assert.Equal(10, snapshot);
            Assert.Equal(4, update);
        }

        [Fact]
        public async Task PrunesOwnerStateWhenFinalBindingIsRemoved()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var firstOwner = new FakeSubscriber();
            var secondOwner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(firstOwner, CreateDataItem("ns=2;s=one")));
            Assert.True(adapter.TryAdd(secondOwner, CreateDataItem("ns=2;s=two")));
            var items = manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>().ToArray();
            Assert.Equal(2, adapter.OwnerStateCount);

            Assert.True(adapter.TryRemove(items[0].ClientHandle));
            Assert.Equal(1, adapter.OwnerStateCount);
            Assert.True(adapter.TryRemove(items[1].ClientHandle));
            Assert.Equal(0, adapter.OwnerStateCount);
        }

        [Fact]
        public async Task RecreatesConditionStateBeforeDeliveringToNewOwner()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var originalOwner = new FakeSubscriber();
            var replacementOwner = new FakeSubscriber();
            var original = CreateConditionItem("condition-stable", 5, 2);
            adapter.Update([(originalOwner, original)]);
            var item = Assert.Single(manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());
            await manager.Handler!.OnEventDataNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow, CreateConditionNotification(item, "cached"), PublishState.None, []);
            adapter.FlushConditions(force: true);
            Assert.Single(originalOwner.Events);

            var updated = original with
            {
                EventFilter = new EventFilterModel
                {
                    SelectClauses = [new SimpleAttributeOperandModel
                    {
                        DisplayName = "ChangedConditionId"
                    }]
                }
            };
            await adapter.UpdateAsync([(replacementOwner, updated)]);
            adapter.FlushConditions(force: true);

            Assert.Empty(replacementOwner.Events);
            Assert.Equal(1, manager.Subscription.ConditionRefreshCount);
            Assert.Single(originalOwner.Events);
        }

        [Fact]
        public async Task DoesNotRecreateOwnerStateForStaleKeepAliveOwner()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var first = new FakeSubscriber();
            var second = new FakeSubscriber();
            Assert.True(adapter.TryAdd(first, CreateDataItem("ns=2;s=first")));
            Assert.True(adapter.TryAdd(second, CreateDataItem("ns=2;s=second")));
            var secondHandle = manager.Subscription!.Collection.Items
                .Cast<FakeMonitoredItem>()
                .Single(item => item.Name.Contains("second", StringComparison.Ordinal))
                .ClientHandle;
            first.OnKeepAliveAction = () => adapter.TryRemove(secondHandle);

            await manager.Handler!.OnKeepAliveNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow, PublishState.KeepAlive);

            Assert.Equal(1, adapter.OwnerStateCount);
            Assert.Equal(1u, manager.Subscription.Collection.Count);
        }

        [Fact]
        public async Task ReleasesOwnerAfterItsFinalBindingIsRemoved()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var weakOwner = AddAndRemoveTransientOwner(adapter, manager);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.Equal(0, adapter.OwnerStateCount);
            Assert.False(weakOwner.IsAlive);
        }

        [Fact]
        public async Task EmitsInitialDeltaRecoveryAndExplicitKeyFrames()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=one")));
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=two")));
            var item = manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>().First();

            await manager.Handler!.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow, new DataValueChange[]
                {
                    new(item, new DataValue(Variant.From(1)), null)
                },
                PublishState.None, []);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 2,
                DateTime.UtcNow, new DataValueChange[]
                {
                    new(item, new DataValue(Variant.From(2)), null)
                },
                PublishState.None, []);
            adapter.RequestKeyFrame(owner);
            await manager.Handler.OnKeepAliveNotificationAsync(manager.Subscription, 3,
                DateTime.UtcNow, PublishState.KeepAlive);
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, PublishState.Recovered);

            Assert.Equal(MessageType.KeyFrame, owner.DataChanges[0].MessageType);
            Assert.Equal(2, owner.DataChanges[0].Notifications.Count);
            Assert.Equal(MessageType.DeltaFrame, owner.DataChanges[1].MessageType);
            Assert.Equal(MessageType.KeyFrame, owner.DataChanges[2].MessageType);
            Assert.Equal(MessageType.KeyFrame, owner.DataChanges[3].MessageType);
        }

        [Fact]
        public async Task ContainsThrowingSubscriberAndContinuesOtherDeliveries()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var throwing = new FakeSubscriber { ThrowOnData = true };
            var receiving = new FakeSubscriber();
            Assert.True(adapter.TryAdd(throwing, CreateDataItem("ns=2;s=throw")));
            Assert.True(adapter.TryAdd(receiving, CreateDataItem("ns=2;s=receive")));
            var items = manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>().ToArray();

            await manager.Handler!.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new DataValueChange[]
                {
                    new DataValueChange(items[0], new DataValue(Variant.From(1)), null),
                    new DataValueChange(items[1], new DataValue(Variant.From(2)), null)
                }, PublishState.None, []);

            Assert.Single(receiving.DataChanges);
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
                new DataValueChange[]
                {
                    new(item, new DataValue(Variant.From(42)), null)
                },
                PublishState.Recovered, []);

            Assert.Equal(1, owner.SemanticsChanges);
            Assert.Equal(2, owner.DataChanges.Count);
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
            OpcUaSubscriptionOptions options, IModelChangeRebrowseSink modelChangeSink = null)
        {
            return new ManagedSubscriptionAdapter(manager, new SubscriptionModel(), options,
                new JsonVariantEncoder(new ServiceMessageContext()), modelChangeSink);
        }

        private static DataMonitoredItemModel CreateDataItem(string nodeId)
        {
            return new DataMonitoredItemModel
            {
                StartNodeId = nodeId
            };
        }

        private static EventMonitoredItemModel CreateConditionItem(string stableId,
            int snapshotInterval, int updateInterval)
        {
            return new EventMonitoredItemModel
            {
                DataSetFieldId = stableId,
                StartNodeId = "ns=2;s=conditions",
                ConditionHandling = new ConditionHandlingOptionsModel
                {
                    SnapshotInterval = snapshotInterval,
                    UpdateInterval = updateInterval
                },
                EventFilter = new EventFilterModel
                {
                    SelectClauses = [new SimpleAttributeOperandModel
                    {
                        DisplayName = "ConditionId"
                    }]
                }
            };
        }

        private static EventNotification[] CreateConditionNotification(FakeMonitoredItem item,
            string value)
        {
            return
            [
                new EventNotification(item, ArrayOf.Wrapped(
                    Variant.From(value),
                    Variant.From(ObjectTypeIds.BaseEventType),
                    Variant.From(new NodeId(1u, 2)),
                    Variant.From(true)))
            ];
        }

        private static WeakReference AddAndRemoveTransientOwner(
            ManagedSubscriptionAdapter adapter, FakeSubscriptionManager manager)
        {
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=transient")));
            var handle = Assert.Single(manager.Subscription!.Collection.Items).ClientHandle;
            Assert.True(adapter.TryRemove(handle));
            return new WeakReference(owner);
        }

        private sealed class FakeSubscriptionManager : ISubscriptionManager
        {
            public FakeSubscription Subscription { get; private set; }
            public ISubscriptionNotificationHandler Handler { get; private set; }
            public SubscriptionOptions CapturedOptions { get; private set; }

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
                IEnumerable<ISubscription> subscriptions = null, CancellationToken ct = default)
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
            public StatusCode TriggerServiceStatus { get; set; } = StatusCodes.Good;
            public StatusCode TriggerAddStatus { get; set; } = StatusCodes.Good;
            public StatusCode TriggerRemoveStatus { get; set; } = StatusCodes.Good;
            public int TriggerResultCount { get; set; } = -1;
            public List<(IMonitoredItem Trigger, IReadOnlyCollection<IMonitoredItem> Children)>
                TriggeringCalls { get; } = [];

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
                IReadOnlyCollection<IMonitoredItem> linksToAdd = null,
                IReadOnlyCollection<IMonitoredItem> linksToRemove = null,
                CancellationToken ct = default)
            {
                TriggeringCalls.Add((triggeringItem, linksToAdd ?? []));
                var addResults = (linksToAdd ?? [])
                    .Select(item => (item, TriggerAddStatus))
                    .ToList();
                if (TriggerResultCount >= 0)
                {
                    addResults = [.. addResults.Take(TriggerResultCount)];
                }
                return ValueTask.FromResult(new SetTriggeringResult(triggeringItem,
                    addResults,
                    StatusCode.IsGood(TriggerRemoveStatus) ? [] :
                        [(triggeringItem, TriggerRemoveStatus)],
                    TriggerServiceStatus));
            }

            public ValueTask DisposeAsync()
            {
                DisposeCount++;
                return ValueTask.CompletedTask;
            }
        }

        private sealed class FakeModelChangeSink : IModelChangeRebrowseSink
        {
            public int CallCount { get; private set; }

            public ValueTask ProcessAsync(ISubscriber owner,
                MonitoredAddressSpaceModel template, DataValue changes, CancellationToken ct)
            {
                CallCount++;
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
                out IMonitoredItem monitoredItem)
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
                out IMonitoredItem monitoredItem)
            {
                monitoredItem = _items.Values.FirstOrDefault(item =>
                    string.Equals(item.Name, name, StringComparison.Ordinal));
                return monitoredItem != null;
            }

            public bool TryAdd(string name, IOptionsMonitor<MonitoredItemOptions> options,
                out IMonitoredItem monitoredItem)
            {
                if (_maxItems != 0 && _items.Count >= _maxItems)
                {
                    monitoredItem = null;
                    return false;
                }

                var item = new FakeMonitoredItem(_nextHandle++, name, options);
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
            public MonitoredItemOptions Options { get; private set; }

            public FakeMonitoredItem(uint clientHandle, string name,
                IOptionsMonitor<MonitoredItemOptions> options)
            {
                ClientHandle = clientHandle;
                Name = name;
                Options = options.CurrentValue;
                _registration = options.OnChange((updated, _) => Options = updated);
            }

            public uint Order => Options.Order;
            public uint ServerId => 0;
            public bool Created => true;
            public MonitoringFilterResult FilterResult => null;
            public Opc.Ua.MonitoringMode CurrentMonitoringMode => Options.MonitoringMode;
            public TimeSpan CurrentSamplingInterval => Options.SamplingInterval;
            public uint CurrentQueueSize => Options.QueueSize;
            public IEnumerable<IMonitoredItem> TriggeringItems => [];
            public IEnumerable<IMonitoredItem> TriggeredItems => [];

            public ValueTask ConditionRefreshAsync(CancellationToken ct = default)
            {
                return ValueTask.CompletedTask;
            }

            private readonly IDisposable _registration;
        }

        private sealed class FakeSubscriber : ISubscriber
        {
            public int SemanticsChanges { get; private set; }
            public Action? OnKeepAliveAction { get; set; }
            public bool ThrowOnData { get; set; }
            public List<OpcUaSubscriptionNotification> DataChanges { get; } = [];
            public List<OpcUaSubscriptionNotification> Events { get; } = [];
            public List<ServiceResultModel> Updates { get; } = [];
            public IEnumerable<BaseMonitoredItemModel> MonitoredItems => [];

            public Task OnMonitoredItemSemanticsChangedAsync(CancellationToken ct = default)
            {
                SemanticsChanges++;
                return Task.CompletedTask;
            }

            public void OnSubscriptionKeepAlive(OpcUaSubscriptionNotification notification)
            {
                OnKeepAliveAction?.Invoke();
            }

            public void OnSubscriptionDataChangeReceived(OpcUaSubscriptionNotification notification)
            {
                if (ThrowOnData)
                {
                    throw new InvalidOperationException("expected test callback failure");
                }
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
                ServiceResultModel serviceResult)
            {
                Updates.Add(serviceResult);
            }
        }
    }
}
