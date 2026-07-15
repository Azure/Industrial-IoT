// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Moq;
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
