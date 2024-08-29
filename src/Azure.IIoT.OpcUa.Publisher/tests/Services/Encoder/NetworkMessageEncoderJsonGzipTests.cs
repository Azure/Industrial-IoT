// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
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

    public class NetworkMessageEncoderJsonGzipTests
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
            options.Value.UseStandardsCompliantEncoding = true;
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
            var messages = new[]
            {
                new OpcUaSubscriptionNotification(DateTimeOffset.UtcNow)
            };

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
        public void EncodeTooBigJsonMessageTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 100;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(3, false, MessageEncoding.JsonGzip);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(3, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeJsonTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.JsonGzip);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            // Batch or no batch, the difference is that we cram all notifications
            // into a message or write all messages as array in batch mode. If
            // single message is desired, single message mode should be set (see next test).

            Assert.Equal(1, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeChunkTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 8 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(500, false, MessageEncoding.JsonGzip);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            var count = networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count);
            Assert.All(networkMessages, m => Assert.All(((NetworkMessage)m.Event).Buffers,
                m => Assert.True(m.Length <= maxMessageSize, m.Length.ToString(CultureInfo.InvariantCulture))));
            Assert.InRange(count, 150, 210);
            Assert.Equal(500, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal((uint)count, encoder.MessagesProcessedCount);
            Assert.InRange(Math.Round(encoder.AvgNotificationsPerMessage), 2, 3);
        }

        [Fact]
        public void EncodeJsonSingleMessageTest()
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.JsonGzip,
                NetworkMessageContentFlags.SingleDataSetMessage);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, false);

            Assert.Equal(20, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(20, encoder.MessagesProcessedCount);
            Assert.Equal(1, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeEventsJsonTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, true, MessageEncoding.JsonGzip);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(1, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Fact]
        public void EncodeEventsSingleMessageJsonTest()
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, true, MessageEncoding.JsonGzip,
                NetworkMessageContentFlags.SingleDataSetMessage);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, false);

            // Single message, no array envelope due to batching resulting in 210 events from 20 notifications.
            Assert.Equal(210, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(210, encoder.MessagesProcessedCount);
            Assert.Equal(0.10, Math.Round(encoder.AvgNotificationsPerMessage, 2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeMetadataJsonTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.JsonGzip);
            messages[10] = messages[10] with
            {
                // Emit metadata
                MessageType = Encoders.PubSub.MessageType.Metadata,
                Context = ((DataSetWriterContext)messages[10].Context) with
                {
                    MetaData = new PublishedDataSetMessageSchemaModel
                    {
                        DataSetFieldContentFlags = null,
                        DataSetMessageContentFlags = null,
                        MetaData = new PublishedDataSetMetaDataModel
                        {
                            DataSetMetaData = new DataSetMetaDataModel
                            {
                                Name = "test"
                            },
                            Fields = new[]
                            {
                                new PublishedFieldMetaDataModel
                                {
                                    Name = "test",
                                    BuiltInType = (byte)BuiltInType.UInt16
                                }
                            }
                        }
                    }
                }
            };
            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(3, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(19, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(3, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeNothingTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.JsonGzip);
            messages[0].MessageType = Encoders.PubSub.MessageType.KeyFrame;
            messages[0].Notifications.Clear();

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Empty(networkMessages);
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(0, encoder.MessagesProcessedCount);
            Assert.Equal(0, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeKeepAliveTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.JsonGzip);
            messages[0].MessageType = Encoders.PubSub.MessageType.KeepAlive;
            messages[0].Notifications.Clear();

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Single(networkMessages);
            Assert.Equal(1, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(1, encoder.MessagesProcessedCount);
            Assert.Equal(1, encoder.AvgNotificationsPerMessage);
        }
    }
}
