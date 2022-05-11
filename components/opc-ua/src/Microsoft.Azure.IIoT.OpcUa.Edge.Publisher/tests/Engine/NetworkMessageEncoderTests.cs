// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class NetworkMessageEncoderTests {

        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IEngineConfiguration> _engineConfigMock;
        private readonly NetworkMessageEncoder _encoder;

        public NetworkMessageEncoderTests() {
            _loggerMock = new Mock<ILogger>();
            _engineConfigMock = new Mock<IEngineConfiguration>();
            _encoder = new NetworkMessageEncoder(_loggerMock.Object, _engineConfigMock.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EmptyMessagesTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new List<DataSetMessageModel>();

            var networkMessages = await (encodeBatchFlag
                ?_encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, _encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, _encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EmptyDataSetMessageModelTest(bool encodeBatchFlag) {
            var maxMessageSize = 256 * 1024;
            var messages = new[] { new DataSetMessageModel() };

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, _encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, _encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false, MessageEncoding.Json)]
        [InlineData(true, MessageEncoding.Json)]
        [InlineData(false, MessageEncoding.Uadp)]
        [InlineData(true, MessageEncoding.Uadp)]
        public async Task EncodeTooBigMessageTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 100;
            var messages = GenerateSampleMessages(3, encoding);

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, _encoder.NotificationsProcessedCount);
            Assert.Equal((uint)6, _encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, _encoder.MessagesProcessedCount);
        }

        [Theory]
        [InlineData(false, MessageEncoding.Json)]
        [InlineData(true, MessageEncoding.Json)]
        [InlineData(false, MessageEncoding.Uadp)]
        [InlineData(true, MessageEncoding.Uadp)]
        public async Task EncodeTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 256 * 1024;
            var messages = GenerateSampleMessages(20, encoding);

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            );

            if (encodeBatchFlag) {
                Assert.Equal(1, networkMessages.Count());
                Assert.Equal((uint)210, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)1, _encoder.MessagesProcessedCount);
                Assert.Equal(210, _encoder.AvgNotificationsPerMessage);
            }
            else {
                Assert.Equal(20, networkMessages.Count());
                Assert.Equal((uint)210, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)20, _encoder.MessagesProcessedCount);
                Assert.Equal(10.5, _encoder.AvgNotificationsPerMessage);
            }
        }

        public static IList<DataSetMessageModel> GenerateSampleMessages(
            uint numOfMessages,
            MessageEncoding encoding = MessageEncoding.Json
        ) {
            var messages = new List<DataSetMessageModel>();

            for (uint i = 0; i < numOfMessages; i++) {
                var suffix = $"-{i}";

                var notifications = new List<MonitoredItemNotificationModel>();

                for (uint k = 0; k < i + 1; k++) {
                    var notificationSuffix = suffix + $"-{k}";

                    var monitoredItemNotification = new MonitoredItemNotification {
                        ClientHandle = k,
                        Value = new DataValue(new Variant(k), new StatusCode(0), DateTime.UtcNow),
                        Message = new NotificationMessage()
                    };

                    var monitoredItem = new MonitoredItem {
                        DisplayName = "DisplayName" + notificationSuffix,
                        StartNodeId = new NodeId("NodeId" + notificationSuffix),
                        AttributeId = k
                    };

                    var notification = monitoredItemNotification.ToMonitoredItemNotification(monitoredItem);
                    notifications.Add(notification);
                }

                var message = new DataSetMessageModel {
                    SequenceNumber = i,
                    PublisherId = "PublisherId" + suffix,
                    Writer = new DataSetWriterModel {
                        DataSet = new PublishedDataSetModel {
                            DataSetSource = new PublishedDataSetSourceModel {
                                PublishedVariables = new PublishedDataItemsModel {
                                    PublishedData = new List<PublishedDataSetVariableModel>()
                                }
                            }
                        }
                    },
                    WriterGroup = new WriterGroupModel {
                        MessageSettings = new WriterGroupMessageSettingsModel {
                            NetworkMessageContentMask = (NetworkMessageContentMask) 0xffff
                        },
                        MessageType = encoding
                    },
                    TimeStamp = DateTime.UtcNow,
                    ServiceMessageContext = new ServiceMessageContext { },
                    Notifications = notifications,
                    SubscriptionId = "SubscriptionId" + suffix,
                    EndpointUrl = "EndpointUrl" + suffix,
                    ApplicationUri = "ApplicationUri" + suffix
                };

                messages.Add(message);
            }

            return messages;
        }
    }
}
