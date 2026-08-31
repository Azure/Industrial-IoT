// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Encoders;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public sealed class OpcUaSubscriptionNotificationTests
    {
        [Fact]
        public void ProviderUpgradesDeltaToKeyFrame()
        {
            var owner = new Mock<ISubscriber>();
            IList<MonitoredItemNotificationModel> snapshot =
            [
                new MonitoredItemNotificationModel
                {
                    Id = "one"
                },
                new MonitoredItemNotificationModel
                {
                    Id = "two"
                }
            ];
            var provider = new FixedKeyFrameSnapshotProvider(snapshot);
            var notification = new OpcUaSubscriptionNotification(
                DateTimeOffset.UnixEpoch, notifications:
                    Array.Empty<MonitoredItemNotificationModel>(),
                keyFrameSnapshotProvider: provider)
            {
                MessageType = MessageType.DeltaFrame
            };

            var upgraded = notification.TryUpgradeToKeyFrame(owner.Object);

            Assert.True(upgraded);
            Assert.Equal(MessageType.KeyFrame, notification.MessageType);
            Assert.Equal(snapshot, notification.Notifications);
            Assert.Equal(1, provider.CallCount);
            Assert.Same(owner.Object, provider.Owner);
        }

        [Fact]
        public void MissingProviderLeavesDeltaUnchanged()
        {
            IList<MonitoredItemNotificationModel> original =
            [
                new MonitoredItemNotificationModel
                {
                    Id = "one"
                }
            ];
            var notification = new OpcUaSubscriptionNotification(
                DateTimeOffset.UnixEpoch, notifications: original)
            {
                MessageType = MessageType.DeltaFrame
            };

            var upgraded = notification.TryUpgradeToKeyFrame(
                new Mock<ISubscriber>().Object);

            Assert.False(upgraded);
            Assert.Equal(MessageType.DeltaFrame, notification.MessageType);
            Assert.Same(original, notification.Notifications);
        }

        [Fact]
        public void AlreadyKeyFrameReturnsTrueWithoutCallingProvider()
        {
            var provider = new FixedKeyFrameSnapshotProvider(
                [new MonitoredItemNotificationModel { Id = "x" }]);
            var notification = new OpcUaSubscriptionNotification(
                DateTimeOffset.UnixEpoch,
                keyFrameSnapshotProvider: provider)
            {
                MessageType = MessageType.KeyFrame
            };

            var upgraded = notification.TryUpgradeToKeyFrame(
                new Mock<ISubscriber>().Object);

            Assert.True(upgraded);
            Assert.Equal(0, provider.CallCount);
        }

        [Fact]
        public void ProviderUpgradesMutableNotificationsList()
        {
            var owner = new Mock<ISubscriber>();
            IList<MonitoredItemNotificationModel> snapshot =
            [
                new MonitoredItemNotificationModel { Id = "snap1" }
            ];
            var mutable = new List<MonitoredItemNotificationModel>
            {
                new MonitoredItemNotificationModel { Id = "old" }
            };
            var provider = new FixedKeyFrameSnapshotProvider(snapshot);
            var notification = new OpcUaSubscriptionNotification(
                DateTimeOffset.UnixEpoch, notifications: mutable,
                keyFrameSnapshotProvider: provider)
            {
                MessageType = MessageType.DeltaFrame
            };

            var upgraded = notification.TryUpgradeToKeyFrame(owner.Object);

            Assert.True(upgraded);
            Assert.Equal(MessageType.KeyFrame, notification.MessageType);
            // Mutable list is cleared and filled with snapshot items
            Assert.Equal(snapshot.Count, notification.Notifications.Count);
        }

        [Fact]
        public void GetDiagnosticCounters_AllZeroForEmptyNotifications()
        {
            var notification = new OpcUaSubscriptionNotification(DateTimeOffset.UnixEpoch);

            var total = notification.GetDiagnosticCounters(
                out var modelChanges, out var heartbeats, out var overflow);

            Assert.Equal(0, total);
            Assert.Equal(0, modelChanges);
            Assert.Equal(0, heartbeats);
            Assert.Equal(0, overflow);
        }

        [Fact]
        public void GetDiagnosticCounters_CountsHeartbeatsAndModelChangesAndOverflow()
        {
            var notifications = new List<MonitoredItemNotificationModel>
            {
                new() { Flags = MonitoredItemSourceFlags.Heartbeat, Overflow = 0 },
                new() { Flags = MonitoredItemSourceFlags.ModelChanges, Overflow = 2 },
                new() { Flags = MonitoredItemSourceFlags.Heartbeat, Overflow = 1 },
                new() { Flags = (MonitoredItemSourceFlags)0, Overflow = 0 }
            };
            var notification = new OpcUaSubscriptionNotification(
                DateTimeOffset.UnixEpoch, notifications: notifications);

            var total = notification.GetDiagnosticCounters(
                out var modelChanges, out var heartbeats, out var overflow);

            Assert.Equal(4, total);
            Assert.Equal(1, modelChanges);
            Assert.Equal(2, heartbeats);
            Assert.Equal(3, overflow);
        }

        [Fact]
        public void CopyConstructorPreservesAllProperties()
        {
            var ts = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var original = new OpcUaSubscriptionNotification(ts)
            {
                MessageType = MessageType.Condition,
                EndpointUrl = "opc.tcp://host:4840",
                ApplicationUri = "urn:app",
                EventTypeName = "MyEvent",
                SequenceNumber = 42
            };
            original.PublishTimestamp = ts;

            var copy = new OpcUaSubscriptionNotification(original,
                [new MonitoredItemNotificationModel { Id = "copied" }]);

            Assert.Equal(original.MessageType, copy.MessageType);
            Assert.Equal(original.EndpointUrl, copy.EndpointUrl);
            Assert.Equal(original.ApplicationUri, copy.ApplicationUri);
            Assert.Equal(original.EventTypeName, copy.EventTypeName);
            Assert.Equal(original.SequenceNumber, copy.SequenceNumber);
            Assert.Equal(original.PublishTimestamp, copy.PublishTimestamp);
            Assert.Equal(original.CreatedTimestamp, copy.CreatedTimestamp);
            Assert.Single(copy.Notifications);
        }

        [Fact]
        public void DisposeCallsAdvance()
        {
            var advance = new Mock<IDisposable>();
            var notification = new OpcUaSubscriptionNotification(
                null, Opc.Ua.ServiceMessageContext.GlobalContext,
                Array.Empty<MonitoredItemNotificationModel>(),
                TimeProvider.System, advance: advance.Object);

            notification.Dispose();

            advance.Verify(a => a.Dispose(), Times.Once());
        }

        [Fact]
        public void DisposeWithNullAdvanceDoesNotThrow()
        {
            var notification = new OpcUaSubscriptionNotification(DateTimeOffset.UnixEpoch);
            var ex = Record.Exception(() => notification.Dispose());
            Assert.Null(ex);
        }

        private sealed class FixedKeyFrameSnapshotProvider :
            IKeyFrameSnapshotProvider
        {
            public FixedKeyFrameSnapshotProvider(
                IList<MonitoredItemNotificationModel> notifications)
            {
                _notifications = notifications;
            }

            public int CallCount { get; private set; }
            public ISubscriber? Owner { get; private set; }

            public bool TryGetNotifications(ISubscriber owner,
                out IList<MonitoredItemNotificationModel>? notifications)
            {
                CallCount++;
                Owner = owner;
                notifications = _notifications;
                return true;
            }

            private readonly IList<MonitoredItemNotificationModel> _notifications;
        }
    }
}
