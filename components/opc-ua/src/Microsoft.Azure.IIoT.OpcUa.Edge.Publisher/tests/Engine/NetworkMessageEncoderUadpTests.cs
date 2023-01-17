// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Moq;
    using Opc.Ua;
    using Serilog;
    using System;
    using System.Linq;
    using Xunit;

    public class NetworkMessageEncoderUadpTests {

        private static NetworkMessageEncoder GetEncoder() {
            var loggerMock = new Mock<ILogger>();
            var engineConfigMock = new Mock<IEngineConfiguration>();
            return new NetworkMessageEncoder(loggerMock.Object, engineConfigMock.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeChunkedUadpMessageTest1(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTestHelper.GenerateSampleSubscriptionNotifications(10, false, MessageEncoding.Uadp);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(33, networkMessages.Count());
            Assert.All(networkMessages, m => Assert.True(m.Body.Length <= maxMessageSize, m.Body.Length.ToString()));
            Assert.Equal((uint)10, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)33, encoder.MessagesProcessedCount);
            Assert.Equal(0.30, Math.Round(encoder.AvgNotificationsPerMessage, 2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeChunkedUadpMessageTest2(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTestHelper.GenerateSampleSubscriptionNotifications(100, false, MessageEncoding.Uadp);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(2025, networkMessages.Count());
            Assert.All(networkMessages, m => Assert.True(m.Body.Length <= maxMessageSize, m.Body.Length.ToString()));
            Assert.Equal((uint)100, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)2025, encoder.MessagesProcessedCount);
            Assert.Equal(0.05, Math.Round(encoder.AvgNotificationsPerMessage, 2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeUadpTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTestHelper.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.Uadp);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(1, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeEventsUadpTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTestHelper.GenerateSampleSubscriptionNotifications(20, true, MessageEncoding.Uadp);

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(1, networkMessages.Count());
            Assert.Equal((uint)20, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeMetadataUadpTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTestHelper.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.Uadp);
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
        public void EncodeMetadataUadpChunkTest(bool encodeBatchFlag) {
            var maxMessageSize = 100;
            var messages = NetworkMessageEncoderTestHelper.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.Uadp);
            messages[0].MessageType = Opc.Ua.PubSub.MessageType.Metadata; // Emit metadata
            messages[0].MetaData = new DataSetMetaDataType {
                Name = "test",
                Fields = Enumerable.Range(0, 10000).Select(r =>
                    new FieldMetaData {
                        Name = "testfield" + r,
                        BuiltInType = (byte)BuiltInType.UInt16
                    }).ToArray()
            };

            var encoder = GetEncoder();
            var networkMessages = encoder.Encode(messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(8194, networkMessages.Count());
            Assert.Equal((uint)0, encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)8194, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeNothingTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = NetworkMessageEncoderTestHelper.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.Uadp);
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
