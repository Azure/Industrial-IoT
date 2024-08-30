// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Xunit;

    public class NetworkMessageEncoderUadpTests
    {
        private static NetworkMessageEncoder GetEncoder()
        {
            var loggerMock = new Mock<ILogger<NetworkMessageEncoder>>();
            var options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            var metricsMock = new Mock<IMetricsContext>();
            metricsMock.SetupGet(m => m.TagList).Returns(new TagList());
            return new NetworkMessageEncoder(options, metricsMock.Object, loggerMock.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeChunkedUadpMessageTest1(bool encodeBatchFlag)
        {
            const int maxMessageSize = 100;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(10, false, MessageEncoding.Uadp);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(33, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.All(networkMessages, m => Assert.All(((NetworkMessage)m.Event).Buffers,
                m => Assert.True(m.Length <= maxMessageSize, m.Length.ToString(CultureInfo.InvariantCulture))));
            Assert.Equal(10, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(33, encoder.MessagesProcessedCount);
            Assert.Equal(0.30, Math.Round(encoder.AvgNotificationsPerMessage, 2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeChunkedUadpMessageTest2(bool encodeBatchFlag)
        {
            const int maxMessageSize = 100;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(100, false, MessageEncoding.Uadp);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(2025, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.All(networkMessages, m => Assert.All(((NetworkMessage)m.Event).Buffers,
                m => Assert.True(m.Length <= maxMessageSize, m.Length.ToString(CultureInfo.InvariantCulture))));
            Assert.Equal(100, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(2025, encoder.MessagesProcessedCount);
            Assert.Equal(0.05, Math.Round(encoder.AvgNotificationsPerMessage, 2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeUadpTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.Uadp);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(1, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeEventsUadpTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, true, MessageEncoding.Uadp);

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(1, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(20, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(1, encoder.MessagesProcessedCount);
            Assert.Equal(20, encoder.AvgNotificationsPerMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeMetadataUadpTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(20, false, MessageEncoding.Uadp);
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
        public void EncodeMetadataUadpChunkTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 100;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.Uadp);
            messages[0] = messages[0] with
            {
                // Emit metadata
                MessageType = Encoders.PubSub.MessageType.Metadata,
                Context = ((DataSetWriterContext)messages[0].Context) with
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
                            Fields = Enumerable.Range(0, 10000)
                            .Select(r => new PublishedFieldMetaDataModel
                            {
                                Name = "testfield" + r,
                                BuiltInType = (byte)BuiltInType.UInt16
                            })
                            .ToList()
                        }
                    }
                }
            };

            using var encoder = GetEncoder();
            var networkMessages = encoder.Encode(NetworkMessage.Create, messages, maxMessageSize, encodeBatchFlag);

            Assert.Equal(8194, networkMessages.Sum(m => ((NetworkMessage)m.Event).Buffers.Count));
            Assert.Equal(0, encoder.NotificationsProcessedCount);
            Assert.Equal(0, encoder.NotificationsDroppedCount);
            Assert.Equal(8194, encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeNothingTest(bool encodeBatchFlag)
        {
            const int maxMessageSize = 256 * 1024;
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.Uadp);
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
            var messages = NetworkMessage.GenerateSampleSubscriptionNotifications(1, false, MessageEncoding.Uadp);
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
