// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Xunit;

    public class MonitoredItemMessageEncoderJsonGzipTests
    {
        /// <summary>
        /// Create compliant encoder
        /// </summary>
        /// <returns></returns>
        private static NetworkMessageEncoder GetEncoder()
        {
            var loggerMock = new Mock<ILogger<NetworkMessageEncoder>>();
            var metricsMock = new Mock<IMetricsContext>();
            metricsMock.SetupGet(m => m.TagList).Returns(new TagList());
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            return new NetworkMessageEncoder(options, metricsMock.Object, loggerMock.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyMessagesTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = new List<OpcUaSubscriptionNotification>();

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyDataSetMessageModelTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = new[] { new OpcUaSubscriptionNotification(DateTimeOffset.UtcNow) };

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeTooBigMessageTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 100;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(3, false,
                encoding: MessageEncoding.JsonGzip, isSampleMode: true);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(6, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeDataTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false,
                encoding: MessageEncoding.JsonGzip, isSampleMode: true);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            if (encodeBatchFlag)
            {
                Assert.Equal(1, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
                Assert.Equal(20, encoder.NotificationsProcessedCount);
                Assert.Equal(0, encoder.NotificationsDroppedCount);
                Assert.Equal(1, encoder.MessagesProcessedCount);
                Assert.Equal(20, encoder.AvgNotificationsPerMessage);
            }
            else
            {
                Assert.Equal(210, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
                Assert.Equal(20, encoder.NotificationsProcessedCount);
                Assert.Equal(0, encoder.NotificationsDroppedCount);
                Assert.Equal(210, encoder.MessagesProcessedCount);
                Assert.Equal(0.10, Math.Round(encoder.AvgNotificationsPerMessage, 2));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeDataWithMultipleNotificationsTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false,
                encoding: MessageEncoding.JsonGzip, isSampleMode: true);
            messages = new List<OpcUaSubscriptionNotification>
            {
                new OpcUaSubscriptionNotification(messages[0], messages.SelectMany(n => n.Notifications).ToList())
            };

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            if (encodeBatchFlag)
            {
                Assert.Equal(1, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
                Assert.Equal(1, encoder.NotificationsProcessedCount);
                Assert.Equal(0, encoder.NotificationsDroppedCount);
                Assert.Equal(1, encoder.MessagesProcessedCount);
                Assert.Equal(1, encoder.AvgNotificationsPerMessage);
            }
            else
            {
                Assert.Equal(210, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
                Assert.Equal(1, encoder.NotificationsProcessedCount);
                Assert.Equal(0, encoder.NotificationsDroppedCount);
                Assert.Equal(210, encoder.MessagesProcessedCount);
                Assert.Equal(0.005, Math.Round(encoder.AvgNotificationsPerMessage, 3));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeChunkTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 8 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(50, false,
                MessageEncoding.JsonGzip, isSampleMode: true);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            var count = networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count);
            Assert.All(networkMessages, m => Assert.All(((NetworkMessage)m.Event).Buffers,
                m => Assert.True(m.Length <= maxMessageSize, m.Length.ToString(CultureInfo.InvariantCulture))));
            if (encodeBatchFlag)
            {
                Assert.InRange(count, 2, 3);
                Assert.Equal(50, encoder.NotificationsProcessedCount);
                Assert.Equal(0, encoder.NotificationsDroppedCount);
                Assert.Equal((uint)count, encoder.MessagesProcessedCount);
                Assert.Equal(25, Math.Round(encoder.AvgNotificationsPerMessage));
            }
            else
            {
                Assert.InRange(count, 1270, 1280);
                Assert.Equal(50, encoder.NotificationsProcessedCount);
                Assert.Equal(0, encoder.NotificationsDroppedCount);
                Assert.Equal((uint)count, encoder.MessagesProcessedCount);
                Assert.Equal(0.04, Math.Round(encoder.AvgNotificationsPerMessage, 2));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeEventTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(1, true,
                encoding: MessageEncoding.JsonGzip, isSampleMode: true);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(1, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(1u, encoder.NotificationsProcessedCount);
            Assert.Equal(0u, encoder.NotificationsDroppedCount);
            Assert.Equal(1u, encoder.MessagesProcessedCount);
            Assert.Equal(1, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeEventsTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, true,
                encoding: MessageEncoding.JsonGzip, isSampleMode: true);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            if (encodeBatchFlag)
            {
                Assert.Equal(1, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
                Assert.Equal(20, encoder.NotificationsProcessedCount);
                Assert.Equal(0, encoder.NotificationsDroppedCount);
                Assert.Equal(1, encoder.MessagesProcessedCount);
                Assert.Equal(20, encoder.AvgNotificationsPerMessage);
            }
            else
            {
                Assert.Equal(210, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
                Assert.Equal(20, encoder.NotificationsProcessedCount);
                Assert.Equal(0, encoder.NotificationsDroppedCount);
                Assert.Equal(210, encoder.MessagesProcessedCount);
                Assert.Equal(0.10, Math.Round(encoder.AvgNotificationsPerMessage, 2));
            }
        }
    }
}
