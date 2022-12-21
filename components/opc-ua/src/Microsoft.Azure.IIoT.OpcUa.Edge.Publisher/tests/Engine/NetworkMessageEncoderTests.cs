// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
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
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
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
            var messages = GenerateSampleMessages(3, false, encoding);

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
            var messages = GenerateSampleMessages(20, false, encoding);

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

        [Theory]
        [InlineData(false, MessageEncoding.Json)]
        [InlineData(true, MessageEncoding.Json)]
        [InlineData(false, MessageEncoding.Uadp)]
        [InlineData(true, MessageEncoding.Uadp)]
        public async Task EncodeEventsTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 256 * 1024;
            var messages = GenerateSampleMessages(20, true, encoding);

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            );

            if (encodeBatchFlag) {
                Assert.Equal(1, networkMessages.Count());
                Assert.Equal((uint)1260, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)1, _encoder.MessagesProcessedCount);
                Assert.Equal(1260, _encoder.AvgNotificationsPerMessage);
            }
            else {
                Assert.Equal(20, networkMessages.Count());
                Assert.Equal((uint)1260, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)20, _encoder.MessagesProcessedCount);
                Assert.Equal(63, _encoder.AvgNotificationsPerMessage);
            }
        }

        [Theory]
        [InlineData(false, MessageEncoding.Json)]
        [InlineData(true, MessageEncoding.Json)]
        [InlineData(false, MessageEncoding.Uadp)]
        [InlineData(true, MessageEncoding.Uadp)]
        public async Task EncodeMetadataTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 256 * 1024;
            var messages = GenerateSampleMessages(20, false, encoding);
            messages[10].Notifications = null; // Emit metadata
            messages[10].MetaData = new DataSetMetaDataType {
                Name = "test",
                Fields = new FieldMetaDataCollection {
                    new FieldMetaData {
                        Name = "test",
                        BuiltInType = (byte)BuiltInType.UInt16
                    }
                }
            };

            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            );

            if (encodeBatchFlag) {
                Assert.Equal(1, networkMessages.Count());
                Assert.Equal((uint)199, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)1, _encoder.MessagesProcessedCount);
                Assert.Equal(199, _encoder.AvgNotificationsPerMessage);
            }
            else {
                Assert.Equal(20, networkMessages.Count());
                Assert.Equal((uint)199, _encoder.NotificationsProcessedCount);
                Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
                Assert.Equal((uint)20, _encoder.MessagesProcessedCount);
            }
        }

        [Theory]
        [InlineData(false, MessageEncoding.Json)]
        [InlineData(true, MessageEncoding.Json)]
        [InlineData(false, MessageEncoding.Uadp)]
        [InlineData(true, MessageEncoding.Uadp)]
        public async Task EncodeNothingTest(bool encodeBatchFlag, MessageEncoding encoding) {
            var maxMessageSize = 256 * 1024;
            var messages = GenerateSampleMessages(1, false, encoding);
            messages[0].Notifications = null;
            messages[0].MetaData = null;
            var networkMessages = await (encodeBatchFlag
                ? _encoder.EncodeBatchAsync(messages, maxMessageSize)
                : _encoder.EncodeAsync(messages, maxMessageSize)
            );

            Assert.Empty(networkMessages);
            Assert.Equal((uint)0, _encoder.NotificationsProcessedCount);
            Assert.Equal((uint)0, _encoder.NotificationsDroppedCount);
            Assert.Equal((uint)0, _encoder.MessagesProcessedCount);
            Assert.Equal(0, _encoder.AvgNotificationsPerMessage);
        }

        public static IList<DataSetMessageModel> GenerateSampleMessages(
            uint numOfMessages, bool eventList = false,
            MessageEncoding encoding = MessageEncoding.Json
        ) {
            var messages = new List<DataSetMessageModel>();

            for (uint i = 0; i < numOfMessages; i++) {
                var suffix = $"-{i}";

                var notifications = new List<MonitoredItemNotificationModel>();

                for (uint k = 0; k < i + 1; k++) {
                    var notificationSuffix = suffix + $"-{k}";

                    var monitoredItem = new MonitoredItem {
                        DisplayName = "DisplayName" + notificationSuffix,
                        StartNodeId = new NodeId("NodeId" + notificationSuffix),
                        AttributeId = k
                    };
                    if (eventList) {
                        var handle = new MonitoredItemWrapper(new EventMonitoredItemModel(), ConsoleLogger.Create());
                        handle.Fields.Add(("1", default));
                        handle.Fields.Add(("2", default));
                        handle.Fields.Add(("3", default));
                        handle.Fields.Add(("4", default));
                        handle.Fields.Add(("5", default));
                        handle.Fields.Add(("6", default));
                        monitoredItem.Handle = handle;
                        var eventFieldList = new EventFieldList {
                            ClientHandle = k,
                            EventFields = new Variant[] { 1, 2, 3, 4, 5, 6 },
                            Message = new NotificationMessage()
                        };
                        var notification = eventFieldList.ToMonitoredItemNotifications(monitoredItem);
                        notifications.AddRange(notification);
                    }
                    else {
                        var monitoredItemNotification = new MonitoredItemNotification {
                            ClientHandle = k,
                            Value = new DataValue(new Variant(k), new StatusCode(0), DateTime.UtcNow),
                            Message = new NotificationMessage()
                        };
                        var notification = monitoredItemNotification.ToMonitoredItemNotifications(monitoredItem);
                        notifications.AddRange(notification);
                    }
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
                            },
                            DataSetMetaData = new DataSetMetaDataModel {
                                Name = "test",
                                DataSetClassId = Guid.NewGuid()
                            }
                        }
                    },
                    WriterGroup = new WriterGroupModel {
                        MessageSettings = new WriterGroupMessageSettingsModel {
                            NetworkMessageContentMask = (NetworkMessageContentMask)0xffff
                        },
                        MessageEncoding = encoding
                    },
                    TimeStamp = DateTime.UtcNow,
                    ServiceMessageContext = new ServiceMessageContext { },
                    Notifications = notifications,
                    SubscriptionName = "SubscriptionId" + suffix,
                    EndpointUrl = "EndpointUrl" + suffix,
                    ApplicationUri = "ApplicationUri" + suffix
                };

                messages.Add(message);
            }

            return messages;
        }
    }
}
