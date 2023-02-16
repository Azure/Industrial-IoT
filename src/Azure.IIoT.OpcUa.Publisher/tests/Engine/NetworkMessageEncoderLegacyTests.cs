// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Engine {
    using Azure.IIoT.OpcUa.Publisher.Engine;
    using Azure.IIoT.OpcUa.Publisher.Runtime;
    using Azure.IIoT.OpcUa.Api.Models;
    using Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Moq;
    using Opc.Ua;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Xunit;

    public class NetworkMessageEncoderLegacyTests {

        /// <summary>
        /// Create legacy encoder
        /// </summary>
        /// <returns></returns>
        private static NetworkMessageEncoder GetEncoder() {
            var loggerMock = new Mock<ILogger>();
            var metricsMock = new Mock<IMetricsContext>();
            metricsMock.SetupGet(m => m.TagList).Returns(new TagList());
            return new NetworkMessageEncoder(new WriterGroupJobConfig {
                UseStandardsCompliantEncoding = false
            }, metricsMock.Object, loggerMock.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyMessagesTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new List<SubscriptionNotificationModel>();

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyDataSetMessageModelTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new[] { new SubscriptionNotificationModel() };

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeTooBigJsonMessageTest(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(3, false, MessageEncoding.Json);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(3, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.Json);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            // Batch or no batch, the difference is that we cram all notifications
            // into a message or write all messages as array in batch mode. If
            // single message is desired, single message mode should be set (see next test).

            Assert.Equal(1, networkMessages.Sum(m => m.Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeChunkTest(bool encodeBatchFlag) {
            var maxMessageSize = 8 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(500, false, MessageEncoding.Json);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            var count = networkMessages.Sum(m => m.Buffers.Count(b => b != null));
            Assert.InRange(count, 66, 68);
            Assert.Equal(96, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)500 - 96, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)count, encoder.MessagesProcessedCount);
            Assert.Equal(1, Math.Round(encoder.AvgNotificationsPerMessage));
        }

        [Fact]
        public void EncodeJsonSingleMessageTest() {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.Json,
                NetworkMessageContentMask.SingleDataSetMessage);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, false);

            Assert.Equal(20, networkMessages.Sum(m => m.Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(20, encoder.MessagesProcessedCount);
            Assert.Equal(1, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeEventsJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, true, MessageEncoding.Json);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(1, networkMessages.Sum(m => m.Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Fact]
        public void EncodeEventsSingleMessageJsonTest() {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, true, MessageEncoding.Json,
                NetworkMessageContentMask.SingleDataSetMessage);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, false);

            // Single message, no array envelope due to batching resulting in 210 events from 20 notifications.
            Assert.Equal(210, networkMessages.Sum(m => m.Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(210, encoder.MessagesProcessedCount);
            Assert.Equal(0.10, Math.Round(encoder.AvgNotificationsPerMessage, 2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeMetadataJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.Json);
            messages[10].MessageType = Opc.Ua.PubSub.MessageType.Metadata; // Emit metadata
            messages[10].MetaData = new DataSetMetaDataType {
                Name = "test",
                Fields = new FieldMetaDataCollection {
                    new FieldMetaData {
                        Name = "test",
                        BuiltInType = (byte)BuiltInType.UInt16
                    }
                }
            };

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(3, networkMessages.Sum(m => m.Buffers.Count));
            Assert.Equal(19, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(3, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeNothingTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.Json);
            messages[0].MessageType = Opc.Ua.PubSub.MessageType.KeepAlive;
            messages[0].Notifications.Clear();

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
            Assert.Equal(0, encoder.AvgNotificationsPerMessage);
        }
    }
}
