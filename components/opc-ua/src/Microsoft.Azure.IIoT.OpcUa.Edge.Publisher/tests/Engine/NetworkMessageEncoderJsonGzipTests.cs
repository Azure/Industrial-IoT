﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime;
    using Moq;
    using Opc.Ua;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class NetworkMessageEncoderJsonGzipTests {

        /// <summary>
        /// Create compliant encoder
        /// </summary>
        /// <returns></returns>
        private static NetworkMessageEncoder GetEncoder() {
            var loggerMock = new Mock<ILogger>();
            return new NetworkMessageEncoder(loggerMock.Object, new WriterGroupJobConfig {
                UseStandardsCompliantEncoding = true,
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyMessagesTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new List<SubscriptionNotificationModel>();

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyDataSetMessageModelTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new[] { new SubscriptionNotificationModel() };

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeTooBigJsonMessageTest(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTests.GenerateSampleSubscriptionNotifications(3, false, MessageEncoding.JsonGzip);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)3, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.JsonGzip);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            // Batch or no batch, the difference is that we cram all notifications
            // into a message or write all messages as array in batch mode. If
            // single message is desired, single message mode should be set (see next test).

            Assert.Equal(1, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeJsonChunkTest(bool encodeBatchFlag) {
            var maxMessageSize = 8 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleSubscriptionNotifications(500, false, MessageEncoding.JsonGzip);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            var count = networkMessages.Count();
            var total = networkMessages.Sum(m => m.Body.Length);
            Assert.All(networkMessages, m => Assert.True(m.Body.Length <= maxMessageSize, m.Body.Length.ToString()));
            Assert.InRange(count, 165, 190);
            Assert.Equal((uint)500, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)count, encoder.MessagesProcessedCount);
            Assert.Equal(3, Math.Round(encoder.AvgNotificationsPerMessage));
        }

        [Fact]
        public void EncodeJsonSingleMessageTest() {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.JsonGzip,
                NetworkMessageContentMask.SingleDataSetMessage);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, false);

            Assert.Equal(20, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)20, encoder.MessagesProcessedCount);
            Assert.Equal(1, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeEventsJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleSubscriptionNotifications(20, true, MessageEncoding.JsonGzip);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(1, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Fact]
        public void EncodeEventsSingleMessageJsonTest() {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleSubscriptionNotifications(20, true, MessageEncoding.JsonGzip,
                NetworkMessageContentMask.SingleDataSetMessage);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, false);

            // Single message, no array envelope due to batching resulting in 210 events from 20 notifications.
            Assert.Equal(210, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)210, encoder.MessagesProcessedCount);
            Assert.Equal(0.10, Math.Round(encoder.AvgNotificationsPerMessage, 2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeMetadataJsonTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.JsonGzip);
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
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(3, networkMessages.Count());
            Assert.Equal((uint)19, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)3, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeNothingTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTests.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.JsonGzip);
            messages[0].MessageType = Opc.Ua.PubSub.MessageType.KeepAlive;
            messages[0].Notifications.Clear();

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, encoder.MessagesProcessedCount);
            Assert.Equal(0, encoder.AvgNotificationsPerMessage);
        }
    }
}
