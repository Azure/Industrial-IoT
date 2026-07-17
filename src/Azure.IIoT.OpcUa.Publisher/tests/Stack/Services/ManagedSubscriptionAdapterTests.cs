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
    using Microsoft.Extensions.Logging.Abstractions;
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
        public async Task PublishingStartsAfterInitialTriggerSynchronizationAndStopsWhenEmpty()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems =
                [
                    CreateDataItem("ns=2;s=child")
                ]
            };
            var gate = manager.Subscription!.BlockTriggering();

            var update = adapter.UpdateAsync([(owner, root)]).AsTask();
            await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.False(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);
            gate.Release.TrySetResult();
            await update;
            Assert.True(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);

            await adapter.UpdateAsync([]);
            Assert.False(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
        }

        [Fact]
        public async Task PublishingWaitsForInitialMonitoredItemsToBeApplied()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            manager.Subscription!.Collection.NewItemsCreated = false;

            var update = adapter.UpdateAsync(
                [(owner, CreateDataItem("ns=2;s=value"))]).AsTask();
            var item = Assert.IsType<FakeMonitoredItem>(
                Assert.Single(manager.Subscription.Collection.Items));

            Assert.False(update.IsCompleted);
            Assert.False(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);

            item.Created = true;
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);
            await update;

            Assert.True(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
        }

        [Fact]
        public async Task FailedInitialTriggerSynchronizationRollsBackWithoutPublishing()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var publishingStates = new List<bool>();
            using var registration = manager.CapturedOptionsMonitor!.OnChange(
                (options, _) => publishingStates.Add(options.PublishingEnabled));
            manager.Subscription!.TriggerAddStatus = StatusCodes.BadMonitoredItemIdInvalid;
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };

            await Assert.ThrowsAsync<ServiceResultException>(() =>
                adapter.UpdateAsync([(owner, root)]).AsTask());

            Assert.Equal(0, adapter.BindingCount);
            Assert.Equal(0u, manager.Subscription.Collection.Count);
            Assert.False(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
            Assert.DoesNotContain(true, publishingStates);
        }

        [Fact]
        public async Task ImmediatePublishingDisablesAfterSubscriptionBecomesEmpty()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions
            {
                EnableImmediatePublishing = true
            });
            var owner = new FakeSubscriber();

            Assert.True(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);
            await adapter.UpdateAsync([]);

            Assert.False(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
        }

        [Fact]
        public async Task ImmediatePublishingStaysEnabledWhileInitialItemIsApplied()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions
            {
                EnableImmediatePublishing = true
            });
            var owner = new FakeSubscriber();
            manager.Subscription!.Collection.NewItemsCreated = false;

            var update = adapter.UpdateAsync(
                [(owner, CreateDataItem("ns=2;s=value"))]).AsTask();
            var item = Assert.IsType<FakeMonitoredItem>(
                Assert.Single(manager.Subscription.Collection.Items));

            Assert.False(update.IsCompleted);
            Assert.True(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);

            item.Created = true;
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);
            await update;

            Assert.True(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
        }

        [Fact]
        public async Task EmitsManagedHeartbeatWithLastValueAndDiagnostics()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10)
            }));
            var item = Assert.Single(manager.Subscription!.Collection.Items);

            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();
            owner.DataDiagnostics.Clear();

            adapter.FlushHeartbeats();
            Assert.Empty(owner.DataChanges);
            adapter.FlushHeartbeats(force: true);

            var heartbeat = Assert.Single(owner.DataChanges);
            var notification = Assert.Single(heartbeat.Notifications);
            Assert.Equal(MessageType.DeltaFrame, heartbeat.MessageType);
            Assert.Null(heartbeat.PublishTimestamp);
            Assert.True(notification.Flags.HasFlag(MonitoredItemSourceFlags.Heartbeat));
            Assert.Equal(Variant.From(42),
                Assert.IsType<DataValue>(notification.Value).WrappedValue);
            Assert.Equal(1u, notification.SequenceNumber);
            Assert.Equal(0, notification.Overflow);
            var diagnostics = Assert.Single(owner.DataDiagnostics);
            Assert.False(diagnostics.LiveData);
            Assert.Equal(1, diagnostics.ValueChanges);
            Assert.Equal(1, diagnostics.Heartbeats);
            Assert.Equal(1, adapter.GetHeartbeatsEnabled(owner));
        }

        [Fact]
        public async Task ManagedHeartbeatUsesSubscriptionDefaults()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions
            {
                DefaultHeartbeatInterval = TimeSpan.FromMinutes(10),
                DefaultHeartbeatBehavior = HeartbeatBehavior.WatchdogLKVDiagnosticsOnly
            });
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);
            var item = Assert.Single(manager.Subscription!.Collection.Items);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();
            owner.DataDiagnostics.Clear();

            adapter.FlushHeartbeats(force: true);

            Assert.Equal(1, adapter.GetHeartbeatsEnabled(owner));
            Assert.Empty(owner.DataChanges);
            Assert.Equal(1, Assert.Single(owner.DataDiagnostics).Heartbeats);
        }

        [Fact]
        public async Task DiagnosticWatchdogMarksLateItemsWithoutAction()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Diagnostic
                }, watchdogAction: (behavior, _) => actions.Add(behavior));
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);

            adapter.FlushWatchdog();

            Assert.Equal(1, adapter.GetLateMonitoredItems(owner));
            Assert.Empty(actions);
        }

        [Fact]
        public async Task AnyLateWatchdogRunsResetOnce()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset,
                    WatchdogCondition = MonitoredItemWatchdogCondition.WhenAnyIsLate
                }, watchdogAction: (behavior, _) => actions.Add(behavior));
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync(
            [
                (owner, CreateDataItem("ns=2;s=first")),
                (owner, CreateDataItem("ns=2;s=second"))
            ]);
            var first = manager.Subscription!.Collection.Items.First();
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(first, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);

            adapter.FlushWatchdog();
            adapter.FlushWatchdog();

            Assert.Equal([SubscriptionWatchdogBehavior.Reset], actions);
            Assert.Equal(1, adapter.GetLateMonitoredItems(owner));
        }

        [Fact]
        public async Task FailedWatchdogResetRearmsWatchdog()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset
                }, watchdogAction: (behavior, _) => actions.Add(behavior));
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);

            adapter.FlushWatchdog();
            adapter.CompleteWatchdogReset(succeeded: false);
            adapter.FlushWatchdog();

            Assert.Equal(
                [
                    SubscriptionWatchdogBehavior.Reset,
                    SubscriptionWatchdogBehavior.Reset
                ], actions);
        }

        [Fact]
        public async Task WatchdogRunsWhenTimeProviderTimestampStartsAtZero()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset
                }, watchdogAction: (behavior, _) => actions.Add(behavior),
                timeProvider: new ZeroTimestampTimeProvider());
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);

            adapter.FlushWatchdog();

            Assert.Equal([SubscriptionWatchdogBehavior.Reset], actions);
        }

        [Fact]
        public async Task ZeroTimestampActivityIsNotMarkedLate()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset
                }, watchdogAction: (behavior, _) => actions.Add(behavior),
                timeProvider: new ZeroTimestampTimeProvider());
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);
            var item = Assert.Single(manager.Subscription!.Collection.Items);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);

            adapter.FlushWatchdog();

            Assert.Empty(actions);
        }

        [Fact]
        public async Task AllLateWatchdogWaitsUntilEveryItemIsLate()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset,
                    WatchdogCondition = MonitoredItemWatchdogCondition.WhenAllAreLate
                }, watchdogAction: (behavior, _) => actions.Add(behavior));
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync(
            [
                (owner, CreateDataItem("ns=2;s=first")),
                (owner, CreateDataItem("ns=2;s=second"))
            ]);
            var first = manager.Subscription!.Collection.Items.First();
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(first, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);

            adapter.FlushWatchdog();
            Assert.Empty(actions);

            adapter.FlushWatchdog();
            Assert.Equal([SubscriptionWatchdogBehavior.Reset], actions);
            Assert.Equal(2, adapter.GetLateMonitoredItems(owner));
        }

        [Fact]
        public async Task WatchdogStopsAndRecoversWithPublishingState()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset
                }, watchdogAction: (behavior, _) => actions.Add(behavior));
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);

            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription!,
                SubscriptionState.Modified, PublishState.Stopped);
            adapter.FlushWatchdog();
            Assert.Empty(actions);

            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, PublishState.Recovered);
            adapter.FlushWatchdog();
            Assert.Equal([SubscriptionWatchdogBehavior.Reset], actions);
        }

        [Fact]
        public async Task SecondaryPartitionDeletionDoesNotStopLogicalWatchdog()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset
                }, watchdogAction: (behavior, _) => actions.Add(behavior));
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);
            manager.Subscription!.Created = false;
            manager.Subscription.CurrentPublishingEnabled = true;

            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Deleted, default);
            adapter.FlushWatchdog();

            Assert.Equal([SubscriptionWatchdogBehavior.Reset], actions);
        }

        [Fact]
        public async Task RemainingStoppedPartitionKeepsWatchdogDisabledOnDeletion()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset
                }, watchdogAction: (behavior, _) => actions.Add(behavior));
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);

            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription!,
                SubscriptionState.Deleted,
                PublishState.Stopped | PublishState.Completed);
            adapter.FlushWatchdog();

            Assert.Empty(actions);
        }

        [Fact]
        public async Task SubscriptionTimeoutDefaultsToResetAction()
        {
            var manager = new FakeSubscriptionManager();
            var actions = new List<SubscriptionWatchdogBehavior>();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), watchdogAction:
                    (behavior, _) => actions.Add(behavior));

            await manager.Handler!.OnSubscriptionStateChangedAsync(manager.Subscription!,
                default, PublishState.Timeout);
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                default, PublishState.Timeout);

            Assert.Equal([SubscriptionWatchdogBehavior.Reset], actions);
        }

        [Fact]
        public async Task DisablingLateItemClearsWatchdogDiagnostics()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10)
                });
            var owner = new FakeSubscriber();
            var item = CreateDataItem("ns=2;s=value") with
            {
                DataSetFieldId = "stable"
            };
            await adapter.UpdateAsync([(owner, item)]);
            adapter.FlushWatchdog();
            Assert.Equal(1, adapter.GetLateMonitoredItems(owner));

            await adapter.UpdateAsync([(owner, item with
            {
                MonitoringMode = PublisherMonitoringMode.Disabled
            })]);

            Assert.Equal(0, adapter.GetLateMonitoredItems(owner));
        }

        [Fact]
        public async Task SubscriptionDeletedCallbackDuringDisposeDoesNotUseDisposedTimer()
        {
            var manager = new FakeSubscriptionManager();
            var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10)
                });
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);
            manager.Subscription!.OnDisposeAsync = () =>
                manager.Handler!.OnSubscriptionStateChangedAsync(
                    manager.Subscription, SubscriptionState.Deleted, default);

            await adapter.DisposeAsync();

            Assert.Equal(1, manager.Subscription.DisposeCount);
        }

        [Fact]
        public async Task FailedWatchdogResetCompletionAfterDisposeIsIgnored()
        {
            var manager = new FakeSubscriptionManager();
            var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions(), template: new SubscriptionModel
                {
                    MonitoredItemWatchdogTimeout = TimeSpan.FromMinutes(10),
                    WatchdogBehavior = SubscriptionWatchdogBehavior.Reset
                }, watchdogAction: (_, _) => { });
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=value"))]);
            adapter.FlushWatchdog();

            await adapter.DisposeAsync();
            adapter.CompleteWatchdogReset(succeeded: false);

            Assert.Equal(1, manager.Subscription!.DisposeCount);
        }

        [Fact]
        public async Task PeriodicDropValueSuppressesDataButEmitsHeartbeat()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10),
                HeartbeatBehavior = HeartbeatBehavior.PeriodicLKVDropValue
            }));
            var item = Assert.Single(manager.Subscription!.Collection.Items);

            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            Assert.Empty(owner.DataChanges);
            Assert.False(adapter.TryCreateKeyFrame(owner, out _));

            adapter.FlushHeartbeats(force: true);

            Assert.Single(owner.DataChanges);
            Assert.True(Assert.Single(owner.DataChanges[0].Notifications).Flags
                .HasFlag(MonitoredItemSourceFlags.Heartbeat));
        }

        [Fact]
        public async Task HeartbeatMarksDisconnectAndRestoresConnectedStatus()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10)
            }));
            var item = Assert.Single(manager.Subscription!.Collection.Items);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();

            adapter.NotifyConnectionState(disconnected: true);
            adapter.FlushHeartbeats(force: true);

            var disconnected = Assert.IsType<DataValue>(
                Assert.Single(Assert.Single(owner.DataChanges).Notifications).Value);
            Assert.Equal(StatusCodes.UncertainNoCommunicationLastUsableValue,
                disconnected.StatusCode);

            owner.DataChanges.Clear();
            adapter.NotifyConnectionState(disconnected: false);
            adapter.FlushHeartbeats(force: true);
            var connected = Assert.IsType<DataValue>(
                Assert.Single(Assert.Single(owner.DataChanges).Notifications).Value);
            Assert.Equal(StatusCodes.Good, connected.StatusCode);
        }

        [Fact]
        public async Task ReconnectDoesNotOverwriteNewerBadValue()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10)
            }));
            var item = Assert.Single(manager.Subscription!.Collection.Items);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            adapter.NotifyConnectionState(disconnected: true);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 2,
                DateTime.UtcNow,
                new[] { new DataValueChange(item,
                    DataValue.FromStatusCode(StatusCodes.BadNodeIdUnknown), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();

            adapter.NotifyConnectionState(disconnected: false);
            adapter.FlushHeartbeats(force: true);

            var value = Assert.IsType<DataValue>(
                Assert.Single(Assert.Single(owner.DataChanges).Notifications).Value);
            Assert.Equal(StatusCodes.BadNodeIdUnknown, value.StatusCode);
        }

        [Fact]
        public async Task StatusOnlyHeartbeatReportsCommunicationLoss()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10)
            }));
            var item = Assert.Single(manager.Subscription!.Collection.Items);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item,
                    DataValue.FromStatusCode(StatusCodes.BadNodeIdUnknown), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();

            adapter.NotifyConnectionState(disconnected: true);
            adapter.FlushHeartbeats(force: true);
            var disconnected = Assert.IsType<DataValue>(
                Assert.Single(Assert.Single(owner.DataChanges).Notifications).Value);
            Assert.Equal(StatusCodes.BadNoCommunication, disconnected.StatusCode);

            owner.DataChanges.Clear();
            adapter.NotifyConnectionState(disconnected: false);
            adapter.FlushHeartbeats(force: true);
            var connected = Assert.IsType<DataValue>(
                Assert.Single(Assert.Single(owner.DataChanges).Notifications).Value);
            Assert.Equal(StatusCodes.BadNodeIdUnknown, connected.StatusCode);
        }

        [Fact]
        public async Task EnablingHeartbeatSeedsExistingCachedValue()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var template = CreateDataItem("ns=2;s=value");
            adapter.Update([(owner, template)]);
            var item = Assert.Single(manager.Subscription!.Collection.Items);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();

            adapter.Update([(owner, template with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10)
            })]);
            Assert.Equal(1, adapter.GetHeartbeatsEnabled(owner));
            adapter.FlushHeartbeats(force: true);

            Assert.Equal(Variant.From(42), Assert.IsType<DataValue>(
                Assert.Single(Assert.Single(owner.DataChanges).Notifications).Value)
                .WrappedValue);
        }

        [Fact]
        public async Task EnablingHeartbeatWhileDisconnectedMarksCachedValue()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var template = CreateDataItem("ns=2;s=value");
            adapter.Update([(owner, template)]);
            var item = Assert.Single(manager.Subscription!.Collection.Items);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();
            adapter.NotifyConnectionState(disconnected: true);

            adapter.Update([(owner, template with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10)
            })]);
            adapter.FlushHeartbeats(force: true);

            var value = Assert.IsType<DataValue>(
                Assert.Single(Assert.Single(owner.DataChanges).Notifications).Value);
            Assert.Equal(StatusCodes.UncertainNoCommunicationLastUsableValue,
                value.StatusCode);
        }

        [Fact]
        public async Task HeartbeatTogglePreservesStatusRestorationAcrossReconnect()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var template = CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10)
            };
            adapter.Update([(owner, template)]);
            var item = Assert.Single(manager.Subscription!.Collection.Items);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();

            adapter.NotifyConnectionState(disconnected: true);
            adapter.Update([(owner, template with { HeartbeatInterval = null })]);
            adapter.NotifyConnectionState(disconnected: false);
            adapter.Update([(owner, template)]);
            adapter.FlushHeartbeats(force: true);

            var value = Assert.IsType<DataValue>(
                Assert.Single(Assert.Single(owner.DataChanges).Notifications).Value);
            Assert.Equal(StatusCodes.Good, value.StatusCode);
        }

        [Fact]
        public async Task FailedLastKnownGoodItemDisablesHeartbeatUntilRecovery()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10),
                HeartbeatBehavior = HeartbeatBehavior.WatchdogLKG
            }));
            var item = Assert.IsType<FakeMonitoredItem>(
                Assert.Single(manager.Subscription!.Collection.Items));
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();

            item.Created = false;
            item.Error = new ServiceResult(StatusCodes.BadNodeIdUnknown);
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);
            adapter.FlushHeartbeats(force: true);
            Assert.Empty(owner.DataChanges);
            Assert.Equal(0, adapter.GetHeartbeatsEnabled(owner));

            item.Created = true;
            item.Error = ServiceResult.Good;
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);
            adapter.FlushHeartbeats(force: true);
            Assert.Single(owner.DataChanges);
            Assert.Equal(1, adapter.GetHeartbeatsEnabled(owner));
        }

        [Fact]
        public async Task LastKnownGoodHeartbeatSkipsBadValue()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10),
                HeartbeatBehavior = HeartbeatBehavior.WatchdogLKG
            }));
            var item = Assert.Single(manager.Subscription!.Collection.Items);

            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item,
                    DataValue.FromStatusCode(StatusCodes.BadNodeIdUnknown), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();

            adapter.FlushHeartbeats(force: true);

            Assert.Empty(owner.DataChanges);
        }

        [Fact]
        public async Task DiagnosticsOnlyHeartbeatDoesNotDeliverData()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=value") with
            {
                HeartbeatInterval = TimeSpan.FromMinutes(10),
                HeartbeatBehavior = HeartbeatBehavior.WatchdogLKVDiagnosticsOnly
            }));
            var item = Assert.Single(manager.Subscription!.Collection.Items);

            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(42)), null) },
                PublishState.None, []);
            owner.DataChanges.Clear();
            owner.DataDiagnostics.Clear();

            adapter.FlushHeartbeats(force: true);

            Assert.Empty(owner.DataChanges);
            Assert.Equal(1, Assert.Single(owner.DataDiagnostics).Heartbeats);
        }

        [Fact]
        public async Task ReenablingAfterEmptyWaitsForMonitoredItemApplication()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=first"))]);
            await adapter.UpdateAsync([]);
            manager.Subscription!.Collection.NewItemsCreated = false;

            var update = adapter.UpdateAsync(
                [(owner, CreateDataItem("ns=2;s=second"))]).AsTask();
            var item = Assert.IsType<FakeMonitoredItem>(
                Assert.Single(manager.Subscription.Collection.Items));

            Assert.False(update.IsCompleted);
            Assert.False(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);

            item.Created = true;
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);
            await update;

            Assert.True(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
        }

        [Fact]
        public async Task EnablingExistingDisabledItemWaitsForAppliedMonitoringMode()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var item = CreateDataItem("ns=2;s=value") with
            {
                MonitoringMode = PublisherMonitoringMode.Disabled
            };
            await adapter.UpdateAsync([(owner, item)]);
            var monitoredItem = Assert.IsType<FakeMonitoredItem>(
                Assert.Single(manager.Subscription!.Collection.Items));
            monitoredItem.ApplyOptionsImmediately = false;

            var update = adapter.UpdateAsync([(owner, item with
            {
                MonitoringMode = PublisherMonitoringMode.Reporting
            })]).AsTask();

            Assert.False(update.IsCompleted);
            Assert.False(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);

            monitoredItem.CurrentMonitoringMode = Opc.Ua.MonitoringMode.Reporting;
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);
            await update;

            Assert.True(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
        }

        [Fact]
        public async Task FailedItemDoesNotEnablePublishingUntilRetryApplies()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            manager.Subscription!.Collection.NewItemsCreated = false;
            var update = adapter.UpdateAsync(
                [(owner, CreateDataItem("ns=2;s=value"))]).AsTask();
            var monitoredItem = Assert.IsType<FakeMonitoredItem>(
                Assert.Single(manager.Subscription.Collection.Items));

            monitoredItem.Error = new ServiceResult(StatusCodes.BadNodeIdInvalid);
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);
            await update;

            Assert.False(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);

            monitoredItem.Error = ServiceResult.Good;
            monitoredItem.Created = true;
            monitoredItem.CurrentMonitoringMode = Opc.Ua.MonitoringMode.Reporting;
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);

            Assert.True(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
        }

        [Fact]
        public async Task DisposalCancelsPendingInitialMonitoredItemApplication()
        {
            var manager = new FakeSubscriptionManager();
            var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            manager.Subscription!.Collection.NewItemsCreated = false;
            var update = adapter.UpdateAsync(
                [(owner, CreateDataItem("ns=2;s=value"))]).AsTask();
            Assert.Single(manager.Subscription.Collection.Items);

            var dispose = adapter.DisposeAsync().AsTask();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => update);
            await dispose;
            Assert.Equal(1, manager.Subscription.DisposeCount);
        }

        [Fact]
        public async Task RejectsSynchronousMutationDuringAsyncSynchronization()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };
            var gate = manager.Subscription!.BlockTriggering();
            var update = adapter.UpdateAsync([(owner, root)]).AsTask();
            await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Throws<InvalidOperationException>(() =>
                adapter.TryAdd(owner, CreateDataItem("ns=2;s=other")));

            gate.Release.TrySetResult();
            await update;
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
        public async Task RejectsPartialV2UpdateAndRestoresPreviousBindings()
        {
            var manager = new FakeSubscriptionManager(maxItems: 1);
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                adapter.UpdateAsync(
                [
                    (owner, CreateDataItem("ns=2;s=one")),
                    (owner, CreateDataItem("ns=2;s=two"))
                ]).AsTask());

            Assert.Equal(0, adapter.BindingCount);
            Assert.Equal(0u, manager.Subscription!.Collection.Count);
            Assert.False(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);
        }

        [Fact]
        public void RejectsZeroAsAnExplicitPartitionCap()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    new SubscriptionModel(), new OpcUaSubscriptionOptions
                    {
                        MaxSubscriptionPartitions = 0
                    }));

            Assert.Equal("options", exception.ParamName);
            Assert.Contains("partition cap", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(null, 0u)]
        [InlineData(1u, 1u)]
        [InlineData(17u, 17u)]
        public void MapsPartitionCapToManagedSentinelOrPositiveValue(
            uint? partitionCap, uint expected)
        {
            var translated = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new SubscriptionModel(), new OpcUaSubscriptionOptions
                {
                    MaxSubscriptionPartitions = partitionCap
                });

            Assert.Equal(expected, translated.MaxPartitionCount);
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
        public async Task SubscriptionDefaultsMatchClassicOptionResolutionAsync()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultPublishingInterval = TimeSpan.FromSeconds(2),
                DefaultKeepAliveCount = 7,
                DefaultLifeTimeCount = 21,
                EnableImmediatePublishing = true,
                MaxMonitoredItemPerSubscription = 37,
                MaxSubscriptionPartitions = 5
            };
            var template = new SubscriptionModel();
            var client = CreateClassicClient(options);
            try
            {
                await using var classic = new OpcUaSubscription(client, template,
                    Options.Create(options), NullLoggerFactory.Instance, IMetricsContext.Empty);

                var managed = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    template, options);

                Assert.Equal(TimeSpan.FromSeconds(2), managed.PublishingInterval);
                Assert.Equal(7u, managed.KeepAliveCount);
                Assert.Equal(21u, managed.LifetimeCount);
                Assert.Equal((byte)0, managed.Priority);
                Assert.Equal(0u, managed.MaxNotificationsPerPublish);
                Assert.True(managed.PublishingEnabled);
                Assert.Equal(classic.DesiredPublishingInterval, managed.PublishingInterval);
                Assert.Equal(classic.DesiredKeepAliveCount, managed.KeepAliveCount);
                Assert.Equal(classic.DesiredLifetimeCount, managed.LifetimeCount);
                Assert.Equal(classic.DesiredPriority, managed.Priority);
                Assert.Equal(classic.DesiredMaxNotificationsPerPublish,
                    managed.MaxNotificationsPerPublish);
                Assert.True(classic.EnableImmediatePublishing);
                Assert.True(managed.PublishingEnabled);
                Assert.Equal(37u, managed.MaxMonitoredItemsPerPartition);
                Assert.Equal(5u, managed.MaxPartitionCount);
            }
            finally
            {
                await client.CloseAsync(shutdown: true);
            }
        }

        [Fact]
        public async Task PerSubscriptionOverridesMatchClassicPrecedenceAsync()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultPublishingInterval = TimeSpan.FromSeconds(20),
                DefaultKeepAliveCount = 10,
                DefaultLifeTimeCount = 30,
                EnableImmediatePublishing = true
            };
            var template = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromMilliseconds(250),
                KeepAliveCount = 3,
                LifetimeCount = 9,
                Priority = 17,
                MaxNotificationsPerPublish = 41,
                EnableImmediatePublishing = false
            };
            var client = CreateClassicClient(options);
            try
            {
                await using var classic = new OpcUaSubscription(client, template,
                    Options.Create(options), NullLoggerFactory.Instance, IMetricsContext.Empty);

                var managed = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    template, options);

                Assert.Equal(TimeSpan.FromMilliseconds(250), managed.PublishingInterval);
                Assert.Equal(3u, managed.KeepAliveCount);
                Assert.Equal(9u, managed.LifetimeCount);
                Assert.Equal((byte)17, managed.Priority);
                Assert.Equal(41u, managed.MaxNotificationsPerPublish);
                Assert.False(managed.PublishingEnabled);
                Assert.Equal(classic.DesiredPublishingInterval, managed.PublishingInterval);
                Assert.Equal(classic.DesiredKeepAliveCount, managed.KeepAliveCount);
                Assert.Equal(classic.DesiredLifetimeCount, managed.LifetimeCount);
                Assert.Equal(classic.DesiredPriority, managed.Priority);
                Assert.Equal(classic.DesiredMaxNotificationsPerPublish,
                    managed.MaxNotificationsPerPublish);
                Assert.False(classic.EnableImmediatePublishing);
                Assert.False(managed.PublishingEnabled);
            }
            finally
            {
                await client.CloseAsync(shutdown: true);
            }
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
        public async Task RejectsUnrelatedModelChangesAndUsesPublisherBrowser()
        {
            var manager = new FakeSubscriptionManager();
            var browser = new FakeModelChangeBrowser();
            TimeSpan? capturedRebrowsePeriod = null;
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions
            {
                DefaultRebrowsePeriod = TimeSpan.FromMinutes(3)
            }, (period, _) =>
            {
                capturedRebrowsePeriod = period;
                return browser;
            });
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, new MonitoredAddressSpaceModel
            {
                StartNodeId = "ns=2;s=model"
            })]);
            Assert.Equal(TimeSpan.FromMinutes(3), capturedRebrowsePeriod);
            Assert.Equal(1, browser.StartCount);
            var item = Assert.Single(manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());

            await manager.Handler!.OnEventDataNotificationAsync(manager.Subscription, 11,
                DateTime.UtcNow, new EventNotification[] { new(item, ArrayOf.Wrapped(
                    Variant.From(ObjectTypeIds.BaseEventType), Variant.From("ignored"))) },
                PublishState.None, []);
            Assert.Equal(0, browser.RebrowseCount);

            await manager.Handler.OnEventDataNotificationAsync(manager.Subscription, 12,
                DateTime.UtcNow, new EventNotification[] { new(item, ArrayOf.Wrapped(
                    Variant.From(ObjectTypeIds.GeneralModelChangeEventType),
                    Variant.From("changes"))) }, PublishState.None, []);
            Assert.Equal(1, browser.RebrowseCount);
            Assert.Equal(1, owner.SemanticsChanges);

            browser.RaiseReference(new Change<ReferenceDescription>(
                new NodeId(1u, 2), new RelativePath(), null,
                new ReferenceDescription
                {
                    NodeId = new ExpandedNodeId(2u, 2),
                    BrowseName = new QualifiedName("Changed", 2)
                }, 7, DateTimeOffset.UtcNow));
            var changeEvent = Assert.Single(owner.Events);
            Assert.Equal(MessageType.Event, changeEvent.MessageType);
            Assert.Equal(5, changeEvent.Notifications.Count);
            Assert.All(changeEvent.Notifications, notification =>
                Assert.True(notification.Flags.HasFlag(
                    MonitoredItemSourceFlags.ModelChanges)));
            Assert.Equal(7u, changeEvent.Notifications[0].SequenceNumber);

            Assert.Throws<InvalidOperationException>(() => adapter.Update([]));
            await adapter.UpdateAsync([]);
            Assert.Equal(1, browser.CloseCount);
        }

        [Fact]
        public async Task ModelChangeBrowserStartsOnlyAfterTriggeringCommits()
        {
            var manager = new FakeSubscriptionManager();
            var browser = new FakeModelChangeBrowser();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions(),
                (_, _) => browser);
            var owner = new FakeSubscriber();
            var root = new MonitoredAddressSpaceModel
            {
                StartNodeId = "ns=2;s=model",
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };
            var gate = manager.Subscription!.BlockTriggering();

            var update = adapter.UpdateAsync([(owner, root)]).AsTask();
            await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, browser.StartCount);

            gate.Release.TrySetResult();
            await update;
            Assert.Equal(1, browser.StartCount);
        }

        [Fact]
        public async Task BrowserCloseFailureDoesNotSkipSubscriptionDisposal()
        {
            var manager = new FakeSubscriptionManager();
            var browser = new FakeModelChangeBrowser
            {
                ThrowOnClose = true
            };
            var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions(),
                (_, _) => browser);
            var owner = new FakeSubscriber();
            await adapter.UpdateAsync([(owner, new MonitoredAddressSpaceModel
            {
                StartNodeId = "ns=2;s=model"
            })]);

            await Assert.ThrowsAsync<AggregateException>(
                () => adapter.DisposeAsync().AsTask());

            Assert.Equal(1, manager.Subscription!.DisposeCount);
        }

        [Fact]
        public async Task KeepsMultipleEventsFromOnePublishDistinctAndOrdered()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateEventItem("ns=2;s=events")));
            var item = Assert.Single(
                manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());

            await manager.Handler!.OnEventDataNotificationAsync(manager.Subscription, 250,
                DateTime.UnixEpoch,
                new[]
                {
                    new EventNotification(item, ArrayOf.Wrapped(Variant.From("first"))),
                    new EventNotification(item, ArrayOf.Wrapped(Variant.From("second")))
                },
                PublishState.None, []);

            var message = Assert.Single(owner.Events);
            Assert.Equal(1u, message.SequenceNumber);
            Assert.Equal(250u, message.PublishSequenceNumber);
            Assert.Equal([1u, 2u],
                message.Notifications.Select(notification => notification.SequenceNumber));
            Assert.Equal([Variant.From("first"), Variant.From("second")],
                message.Notifications.Select(notification =>
                    Assert.IsType<DataValue>(notification.Value).WrappedValue));
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
        public async Task RestoresPreviousBindingsWhenTriggeredUpdateFails()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };
            Assert.True(await adapter.TryAddAsync(owner, root));

            manager.Subscription!.EnqueueTriggeringResult(
                addStatus: StatusCodes.BadMonitoredItemIdInvalid);
            var exception = await Assert.ThrowsAsync<ServiceResultException>(() =>
                adapter.UpdateAsync([(owner, root)]).AsTask());

            Assert.Equal(StatusCodes.BadMonitoredItemIdInvalid, exception.Result.StatusCode);
            Assert.Equal(2u, manager.Subscription.Collection.Count);
            Assert.Equal(3, manager.Subscription.TriggeringCalls.Count);
        }

        [Fact]
        public async Task TriggerRollbackPreservesExactDuplicatePrefixBindingNames()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var roots = Enumerable.Range(0, 12)
                .Select(index => CreateDataItem($"ns=2;s=item-{index}"))
                .ToArray();
            roots[^1] = roots[^1] with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };
            await adapter.UpdateAsync(roots.Select(root =>
                ((ISubscriber)owner, (BaseMonitoredItemModel)root)));
            var before = manager.Subscription!.Collection.Items
                .Cast<FakeMonitoredItem>()
                .Where(item => !item.Name.Contains("/triggered/", StringComparison.Ordinal))
                .ToDictionary(item => item.Name,
                    item => item.Options.StartNodeId.ToString(), StringComparer.Ordinal);
            manager.Subscription.EnqueueTriggeringResult(
                addStatus: StatusCodes.BadMonitoredItemIdInvalid);

            await Assert.ThrowsAsync<ServiceResultException>(() =>
                adapter.UpdateAsync(roots.Select(root =>
                    ((ISubscriber)owner, (BaseMonitoredItemModel)root))).AsTask());

            var after = manager.Subscription.Collection.Items
                .Cast<FakeMonitoredItem>()
                .Where(item => !item.Name.Contains("/triggered/", StringComparison.Ordinal))
                .ToDictionary(item => item.Name,
                    item => item.Options.StartNodeId.ToString(), StringComparer.Ordinal);
            Assert.Equal(before.Count, after.Count);
            Assert.All(before, pair => Assert.Equal(pair.Value, after[pair.Key]));
        }

        [Fact]
        public async Task DeferredStateChangeRecomputesPublishingAfterRollback()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var root = CreateDataItem("ns=2;s=root") with
            {
                TriggeredItems = [CreateDataItem("ns=2;s=child")]
            };
            await adapter.UpdateAsync([(owner, root)]);
            var monitoredItems = manager.Subscription!.Collection.Items
                .Cast<FakeMonitoredItem>().ToArray();
            Assert.All(monitoredItems, item => item.ApplyOptionsImmediately = false);
            var gate = manager.Subscription.BlockTriggering();
            manager.Subscription.EnqueueTriggeringResult(
                addStatus: StatusCodes.BadMonitoredItemIdInvalid);
            var update = adapter.UpdateAsync([(owner, root)]).AsTask();
            await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

            foreach (var item in monitoredItems)
            {
                item.CurrentMonitoringMode = Opc.Ua.MonitoringMode.Disabled;
            }
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Modified, default);
            Assert.True(manager.CapturedOptionsMonitor!.CurrentValue.PublishingEnabled);

            gate.Release.TrySetResult();
            await Assert.ThrowsAsync<ServiceResultException>(() => update);

            Assert.False(manager.CapturedOptionsMonitor.CurrentValue.PublishingEnabled);
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
            manager.Subscription!.EnqueueTriggeringResult(resultCount: 0);

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
            manager.Subscription!.EnqueueTriggeringResult(
                removeStatus: StatusCodes.BadMonitoredItemIdInvalid);

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
        public async Task DoesNotClearNewConditionRefreshRequestAfterOlderRequestCompletes()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            var original = CreateConditionItem("condition-stable", 5, 2);
            adapter.Update([(owner, original)]);
            var handle = Assert.Single(manager.Subscription!.Collection.Items).ClientHandle;

            var firstGate = manager.Subscription.EnqueueConditionRefresh();
            var firstUpdate = adapter.UpdateAsync([(owner, original with
            {
                EventFilter = CreateEventFilter("first")
            })]).AsTask();
            await firstGate.Started.Task;

            var secondGate = manager.Subscription.EnqueueConditionRefresh();
            var secondUpdate = adapter.UpdateAsync([(owner, original with
            {
                EventFilter = CreateEventFilter("second")
            })]).AsTask();
            Assert.False(secondGate.Started.Task.IsCompleted);

            firstGate.Gate.SetResult();
            await firstUpdate;

            await secondGate.Started.Task;
            Assert.True(adapter.IsConditionRefreshRequested(handle));

            secondGate.Gate.SetResult();
            await secondUpdate;

            Assert.False(adapter.IsConditionRefreshRequested(handle));
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
        public async Task SkipsStaleOwnerInBatchedDataDelivery()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var first = new FakeSubscriber();
            var second = new FakeSubscriber();
            Assert.True(adapter.TryAdd(first, CreateDataItem("ns=2;s=first")));
            Assert.True(adapter.TryAdd(second, CreateDataItem("ns=2;s=second")));
            var items = manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>().ToArray();
            var secondHandle = items.Single(item => item.Name.Contains("second", StringComparison.Ordinal))
                .ClientHandle;
            first.OnDataChangeAction = () => adapter.TryRemove(secondHandle);

            await manager.Handler!.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new DataValueChange[]
                {
                    new DataValueChange(items[0], new DataValue(Variant.From(1)), null),
                    new DataValueChange(items[1], new DataValue(Variant.From(2)), null)
                }, PublishState.None, []);

            Assert.Single(first.DataChanges);
            Assert.Empty(second.DataChanges);
        }

        [Fact]
        public async Task SkipsStaleOwnerInBatchedEventDelivery()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var first = new FakeSubscriber();
            var second = new FakeSubscriber();
            Assert.True(adapter.TryAdd(first, CreateEventItem("ns=2;s=first")));
            Assert.True(adapter.TryAdd(second, CreateEventItem("ns=2;s=second")));
            var items = manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>().ToArray();
            var secondHandle = items.Single(item => item.Name.Contains("second", StringComparison.Ordinal))
                .ClientHandle;
            first.OnEventReceivedAction = () => adapter.TryRemove(secondHandle);

            await manager.Handler!.OnEventDataNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new EventNotification[]
                {
                    new EventNotification(items[0], ArrayOf.Wrapped(Variant.From(1))),
                    new EventNotification(items[1], ArrayOf.Wrapped(Variant.From(2)))
                }, PublishState.None, []);

            Assert.Single(first.Events);
            Assert.Empty(second.Events);
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
        public async Task DeliveredDeltaCanUpgradeFromManagedSnapshotProvider()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager,
                new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=one")));
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=two")));
            var item = manager.Subscription!.Collection.Items
                .Cast<FakeMonitoredItem>().First();

            await manager.Handler!.OnDataChangeNotificationAsync(manager.Subscription, 1,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(1)), null) },
                PublishState.None, []);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 2,
                DateTime.UtcNow,
                new[] { new DataValueChange(item, new DataValue(Variant.From(2)), null) },
                PublishState.None, []);
            var delta = owner.DataChanges.Last();
            Assert.Equal(MessageType.DeltaFrame, delta.MessageType);
            Assert.Single(delta.Notifications);

            Assert.True(delta.TryUpgradeToKeyFrame(owner));
            Assert.Equal(MessageType.KeyFrame, delta.MessageType);
            Assert.Equal(2, delta.Notifications.Count);
        }

        [Fact]
        public async Task PreservesDataDeliveryOrderAcrossSimulatedRecreateBoundary()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateDataItem("ns=2;s=sequence")));
            var item = Assert.Single(
                manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());
            await manager.Handler!.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Created, PublishState.None);

            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 100,
                DateTime.UnixEpoch.AddSeconds(1),
                new[] { new DataValueChange(item, new DataValue(Variant.From(10)), null) },
                PublishState.None, []);
            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 101,
                DateTime.UnixEpoch.AddSeconds(2),
                new[] { new DataValueChange(item, new DataValue(Variant.From(11)), null) },
                PublishState.None, []);

            // This is an adapter-only recreation signal. It verifies callback ordering
            // and the cached recovery frame, not server-side transfer continuity.
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Created,
                PublishState.Recovered | PublishState.Transferred);

            await manager.Handler.OnDataChangeNotificationAsync(manager.Subscription, 102,
                DateTime.UnixEpoch.AddSeconds(3),
                new[] { new DataValueChange(item, new DataValue(Variant.From(12)), null) },
                PublishState.None, []);

            Assert.Equal(
                [
                    MessageType.KeyFrame,
                    MessageType.DeltaFrame,
                    MessageType.KeyFrame,
                    MessageType.DeltaFrame
                ],
                owner.DataChanges.Select(notification => notification.MessageType));
            Assert.Equal([1u, 2u, 3u, 4u],
                owner.DataChanges.Select(notification => notification.SequenceNumber));
            Assert.Equal([100u, 101u, null, 102u],
                owner.DataChanges.Select(notification => notification.PublishSequenceNumber));
            Assert.Equal(
                [Variant.From(10), Variant.From(11), Variant.From(11), Variant.From(12)],
                owner.DataChanges.Select(GetSingleValue));

            var sourceDeliveries = new[]
            {
                owner.DataChanges[0],
                owner.DataChanges[1],
                owner.DataChanges[3]
            };
            Assert.Equal([1u, 2u, 4u],
                sourceDeliveries.Select(notification =>
                    Assert.Single(notification.Notifications).SequenceNumber));
            Assert.Equal([Variant.From(10), Variant.From(11), Variant.From(12)],
                sourceDeliveries.Select(GetSingleValue));
            Assert.Equal(1, owner.SemanticsChanges);
        }

        [Fact]
        public async Task PreservesEventDeliveryOrderAcrossSimulatedRecreateBoundary()
        {
            var manager = new FakeSubscriptionManager();
            await using var adapter = CreateAdapter(manager, new OpcUaSubscriptionOptions());
            var owner = new FakeSubscriber();
            Assert.True(adapter.TryAdd(owner, CreateEventItem("ns=2;s=events")));
            var item = Assert.Single(
                manager.Subscription!.Collection.Items.Cast<FakeMonitoredItem>());
            await manager.Handler!.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Created, PublishState.None);

            await manager.Handler.OnEventDataNotificationAsync(manager.Subscription, 200,
                DateTime.UnixEpoch.AddSeconds(1),
                new[] { new EventNotification(
                    item, ArrayOf.Wrapped(Variant.From("before-1"))) },
                PublishState.None, []);
            await manager.Handler.OnEventDataNotificationAsync(manager.Subscription, 201,
                DateTime.UnixEpoch.AddSeconds(2),
                new[] { new EventNotification(
                    item, ArrayOf.Wrapped(Variant.From("before-2"))) },
                PublishState.None, []);

            // No real server is involved; this only characterizes adapter dispatch at
            // the same lifecycle boundary raised by a recreate/transfer.
            await manager.Handler.OnSubscriptionStateChangedAsync(manager.Subscription,
                SubscriptionState.Created,
                PublishState.Recovered | PublishState.Transferred);

            await manager.Handler.OnEventDataNotificationAsync(manager.Subscription, 202,
                DateTime.UnixEpoch.AddSeconds(3),
                new[] { new EventNotification(
                    item, ArrayOf.Wrapped(Variant.From("after"))) },
                PublishState.None, []);

            Assert.Equal(3, owner.Events.Count);
            Assert.All(owner.Events,
                notification => Assert.Equal(MessageType.Event, notification.MessageType));
            Assert.Equal([1u, 2u, 3u],
                owner.Events.Select(notification => notification.SequenceNumber));
            Assert.Equal([200u, 201u, 202u],
                owner.Events.Select(notification => notification.PublishSequenceNumber));
            Assert.Equal([1u, 2u, 3u],
                owner.Events.Select(notification =>
                    Assert.Single(notification.Notifications).SequenceNumber));
            Assert.Equal(
                [
                    Variant.From("before-1"),
                    Variant.From("before-2"),
                    Variant.From("after")
                ],
                owner.Events.Select(GetSingleValue));
            Assert.Empty(owner.DataChanges);
            Assert.Equal(1, owner.SemanticsChanges);
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

            await adapter.UpdateAsync([(owner, CreateDataItem("ns=2;s=one"))]);
            Assert.Equal(1, adapter.BindingCount);
            var handle = Assert.Single(manager.Subscription!.Collection.Items).ClientHandle;
            Assert.True(adapter.TryRemove(handle));
            Assert.Equal(0, adapter.BindingCount);

            await adapter.UpdateAsync([]);
            Assert.Equal(0u, manager.Subscription.Collection.Count);
        }

        private static OpcUaClient CreateClassicClient(
            OpcUaSubscriptionOptions subscriptionOptions)
        {
            var configuration = new ApplicationConfiguration
            {
                ApplicationName = "managed-subscription-parity-tests",
                ApplicationUri = "urn:managed-subscription-parity-tests",
                ApplicationType = Opc.Ua.ApplicationType.Client,
                ClientConfiguration = new Opc.Ua.ClientConfiguration()
            };
            var connection = new ConnectionIdentifier(new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = "opc.tcp://localhost:4840"
                }
            });
            return new OpcUaClient(configuration, connection,
                NullLoggerFactory.Instance, TimeProvider.System, IMetricsContext.Empty,
                () => Task.CompletedTask, notifier: null, reverseConnectManager: null,
                diagnosticsCallback: _ => { },
                Options.Create(new OpcUaClientOptions()),
                Options.Create(subscriptionOptions));
        }

        private static Variant GetSingleValue(
            OpcUaSubscriptionNotification notification)
        {
            var item = Assert.Single(notification.Notifications);
            var value = Assert.IsType<DataValue>(item.Value);
            return value.WrappedValue;
        }

        private static ManagedSubscriptionAdapter CreateAdapter(FakeSubscriptionManager manager,
            OpcUaSubscriptionOptions options,
            Func<TimeSpan, string, IOpcUaBrowser> modelChangeBrowserFactory = null,
            SubscriptionModel template = null,
            Action<SubscriptionWatchdogBehavior, string> watchdogAction = null,
            TimeProvider timeProvider = null)
        {
            return new ManagedSubscriptionAdapter(manager, template ?? new SubscriptionModel(), options,
                new JsonVariantEncoder(new ServiceMessageContext()),
                modelChangeBrowserFactory,
                timeProvider: timeProvider,
                watchdogAction: watchdogAction == null ? null :
                    (_, behavior, message) => watchdogAction(behavior, message));
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

        private static EventMonitoredItemModel CreateEventItem(string nodeId)
        {
            return new EventMonitoredItemModel
            {
                StartNodeId = nodeId,
                EventFilter = CreateEventFilter("Value")
            };
        }

        private static EventFilterModel CreateEventFilter(string displayName)
        {
            return new EventFilterModel
            {
                SelectClauses = [new SimpleAttributeOperandModel
                {
                    DisplayName = displayName
                }]
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
            public IOptionsMonitor<SubscriptionOptions> CapturedOptionsMonitor { get; private set; }

            public FakeSubscriptionManager(uint maxItems = 0)
            {
                _maxItems = maxItems;
            }

            public ISubscription Add(ISubscriptionNotificationHandler handler,
                IOptionsMonitor<SubscriptionOptions> options)
            {
                Handler = handler;
                CapturedOptionsMonitor = options;
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

        private sealed class ZeroTimestampTimeProvider : TimeProvider
        {
            public override long GetTimestamp()
            {
                return 0;
            }
        }

        private sealed class FakeSubscription : IPartitionedSubscription
        {
            public FakeCollection Collection { get; }
            public int DisposeCount { get; private set; }
            public int ConditionRefreshCount { get; private set; }
            public StatusCode TriggerServiceStatus { get; set; } = StatusCodes.Good;
            public StatusCode TriggerAddStatus { get; set; } = StatusCodes.Good;
            public StatusCode TriggerRemoveStatus { get; set; } = StatusCodes.Good;
            public int TriggerResultCount { get; set; } = -1;
            public Queue<ConditionRefreshGate> ConditionRefreshGates { get; } = [];
            public TriggeringGate? TriggeringGate { get; private set; }
            public List<(IMonitoredItem Trigger, IReadOnlyCollection<IMonitoredItem> Children)>
                TriggeringCalls { get; } = [];

            public FakeSubscription(uint maxItems)
            {
                Collection = new FakeCollection(maxItems);
            }

            public bool Created { get; set; } = true;
            public TimeSpan CurrentPublishingInterval => TimeSpan.FromSeconds(1);
            public byte CurrentPriority => 0;
            public uint CurrentLifetimeCount => 0;
            public uint CurrentKeepAliveCount => 0;
            public bool CurrentPublishingEnabled { get; set; } = true;
            public uint CurrentMaxNotificationsPerPublish => 0;
            public IMonitoredItemCollection MonitoredItems => Collection;
            public long MissingMessageCount => 0;
            public long RepublishMessageCount => 0;
            public int PartitionCount { get; set; } = 1;
            public IReadOnlyList<uint> PartitionIds =>
                Enumerable.Range(1, PartitionCount).Select(index => (uint)index).ToArray();
            public Func<ValueTask>? OnDisposeAsync { get; set; }

            public ValueTask ConditionRefreshAsync(CancellationToken ct = default)
            {
                ConditionRefreshCount++;
                if (ConditionRefreshGates.TryDequeue(out var gate))
                {
                    gate.Started.SetResult();
                    return new ValueTask(gate.Gate.Task.WaitAsync(ct));
                }
                return ValueTask.CompletedTask;
            }

            public ValueTask RecreateAsync(CancellationToken ct = default)
            {
                return ValueTask.CompletedTask;
            }

            public ConditionRefreshGate EnqueueConditionRefresh()
            {
                var gate = new ConditionRefreshGate();
                ConditionRefreshGates.Enqueue(gate);
                return gate;
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
                if (TriggeringGate is { } gate)
                {
                    gate.Started.TrySetResult();
                    return new ValueTask<SetTriggeringResult>(
                        CompleteTriggeringAsync(gate, triggeringItem, linksToAdd,
                            linksToRemove, ct));
                }
                return ValueTask.FromResult(CreateTriggeringResult(triggeringItem,
                    linksToAdd, linksToRemove));
            }

            public TriggeringGate BlockTriggering()
            {
                TriggeringGate = new TriggeringGate();
                return TriggeringGate;
            }

            public void EnqueueTriggeringResult(
                StatusCode? serviceStatus = null,
                StatusCode? addStatus = null,
                StatusCode? removeStatus = null,
                int? resultCount = null)
            {
                _triggeringResults.Enqueue(new TriggeringResultOptions(
                    serviceStatus ?? StatusCodes.Good,
                    addStatus ?? StatusCodes.Good,
                    removeStatus ?? StatusCodes.Good,
                    resultCount ?? -1));
            }

            private async Task<SetTriggeringResult> CompleteTriggeringAsync(
                TriggeringGate gate, IMonitoredItem triggeringItem,
                IReadOnlyCollection<IMonitoredItem>? linksToAdd,
                IReadOnlyCollection<IMonitoredItem>? linksToRemove,
                CancellationToken ct)
            {
                await gate.Release.Task.WaitAsync(ct);
                TriggeringGate = null;
                return CreateTriggeringResult(triggeringItem, linksToAdd, linksToRemove);
            }

            private SetTriggeringResult CreateTriggeringResult(IMonitoredItem triggeringItem,
                IReadOnlyCollection<IMonitoredItem>? linksToAdd,
                IReadOnlyCollection<IMonitoredItem>? linksToRemove)
            {
                var options = _triggeringResults.TryDequeue(out var queued) ? queued :
                    new TriggeringResultOptions(TriggerServiceStatus, TriggerAddStatus,
                        TriggerRemoveStatus, TriggerResultCount);
                var addResults = (linksToAdd ?? [])
                    .Select(item => (item, options.AddStatus))
                    .ToList();
                if (options.ResultCount >= 0)
                {
                    addResults = [.. addResults.Take(options.ResultCount)];
                }
                return new SetTriggeringResult(triggeringItem,
                    addResults,
                    StatusCode.IsGood(options.RemoveStatus) ? [] :
                        [(triggeringItem, options.RemoveStatus)],
                    options.ServiceStatus);
            }

            public ValueTask DisposeAsync()
            {
                DisposeCount++;
                return OnDisposeAsync?.Invoke() ?? ValueTask.CompletedTask;
            }

            private sealed record class TriggeringResultOptions(
                StatusCode ServiceStatus,
                StatusCode AddStatus,
                StatusCode RemoveStatus,
                int ResultCount);

            private readonly Queue<TriggeringResultOptions> _triggeringResults = [];
        }

        private sealed class TriggeringGate
        {
            public TaskCompletionSource Started { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource Release { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private sealed class ConditionRefreshGate
        {
            public TaskCompletionSource Started { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource Gate { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private sealed class FakeModelChangeBrowser : IStartableOpcUaBrowser
        {
            public event EventHandler<Change<Node>> OnNodeChange;
            public event EventHandler<Change<ReferenceDescription>> OnReferenceChange;
            public int RebrowseCount { get; private set; }
            public int CloseCount { get; private set; }
            public int StartCount { get; private set; }
            public int ConnectedCount { get; private set; }
            public bool ThrowOnClose { get; set; }

            public void Rebrowse()
            {
                RebrowseCount++;
            }

            public ValueTask CloseAsync()
            {
                CloseCount++;
                if (ThrowOnClose)
                {
                    return ValueTask.FromException(
                        new InvalidOperationException("Injected browser close failure."));
                }
                return ValueTask.CompletedTask;
            }

            public void Start()
            {
                StartCount++;
            }

            public void OnConnected()
            {
                ConnectedCount++;
            }

            public void RaiseReference(Change<ReferenceDescription> change)
            {
                OnReferenceChange?.Invoke(this, change);
            }

            public void RaiseNode(Change<Node> change)
            {
                OnNodeChange?.Invoke(this, change);
            }
        }

        private sealed class FakeCollection : IMonitoredItemCollection
        {
            public uint Count => (uint)_items.Count;
            public IEnumerable<IMonitoredItem> Items => _items.Values;
            public bool NewItemsCreated { get; set; } = true;

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

                var item = new FakeMonitoredItem(_nextHandle++, name, options)
                {
                    Created = NewItemsCreated
                };
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
            public bool ApplyOptionsImmediately { get; set; } = true;

            public FakeMonitoredItem(uint clientHandle, string name,
                IOptionsMonitor<MonitoredItemOptions> options)
            {
                ClientHandle = clientHandle;
                Name = name;
                Options = options.CurrentValue;
                CurrentMonitoringMode = Options.MonitoringMode;
                _registration = options.OnChange((updated, _) =>
                {
                    Options = updated;
                    if (ApplyOptionsImmediately)
                    {
                        CurrentMonitoringMode = updated.MonitoringMode;
                    }
                });
            }

            public uint Order => Options.Order;
            public uint ServerId => Created ? ClientHandle : 0;
            public bool Created { get; set; } = true;
            public MonitoringFilterResult FilterResult => null;
            public Opc.Ua.MonitoringMode CurrentMonitoringMode { get; set; }
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
            public Action? OnDataChangeAction { get; set; }
            public Action? OnEventReceivedAction { get; set; }
            public bool ThrowOnData { get; set; }
            public List<OpcUaSubscriptionNotification> DataChanges { get; } = [];
            public List<OpcUaSubscriptionNotification> Events { get; } = [];
            public List<(bool LiveData, int ValueChanges, int Overflow, int Heartbeats)>
                DataDiagnostics { get; } = [];
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
                OnDataChangeAction?.Invoke();
            }

            public void OnSubscriptionCyclicReadCompleted(OpcUaSubscriptionNotification notification)
            {
                DataChanges.Add(notification);
            }

            public void OnSubscriptionEventReceived(OpcUaSubscriptionNotification notification)
            {
                Events.Add(notification);
                OnEventReceivedAction?.Invoke();
            }

            public void OnSubscriptionDataDiagnosticsChange(bool liveData, int valueChanges,
                int overflow, int heartbeats)
            {
                DataDiagnostics.Add((liveData, valueChanges, overflow, heartbeats));
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
