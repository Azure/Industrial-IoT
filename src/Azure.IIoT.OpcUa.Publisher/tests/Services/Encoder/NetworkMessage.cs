// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using Furly.Extensions.Messaging;

    public sealed class NetworkMessage : IEvent
    {
        public DateTime Timestamp { get; private set; }

        public IEvent SetTimestamp(DateTime value)
        {
            Timestamp = value;
            return this;
        }

        public string ContentType { get; private set; }

        public IEvent SetContentType(string value)
        {
            ContentType = value;
            return this;
        }

        public string ContentEncoding { get; private set; }

        public IEvent SetContentEncoding(string value)
        {
            ContentEncoding = value;
            return this;
        }

        public string MessageSchema { get; private set; }

        public IEvent SetMessageSchema(string value)
        {
            MessageSchema = value;
            return this;
        }

        public string RoutingInfo { get; private set; }

        public IEvent SetRoutingInfo(string value)
        {
            RoutingInfo = value;
            return this;
        }

        public string Topic { get; private set; }

        public IEvent SetTopic(string value)
        {
            Topic = value;
            return this;
        }

        public bool Retain { get; private set; }

        public IEvent SetRetain(bool value)
        {
            Retain = value;
            return this;
        }

        public TimeSpan Ttl { get; private set; }

        public IEvent SetTtl(TimeSpan value)
        {
            Ttl = value;
            return this;
        }

        public List<ReadOnlyMemory<byte>> Buffers { get; } = new();

        public IEvent AddBuffers(IEnumerable<ReadOnlyMemory<byte>> value)
        {
            Buffers.AddRange(value);
            return this;
        }

        public Dictionary<string, string> Properties { get; } = new();

        public IEvent AddProperty(string name, string value)
        {
            Properties.AddOrUpdate(name, value);
            return this;
        }

        ValueTask IEvent.SendAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public static IEvent Create()
        {
            return new NetworkMessage();
        }

        public static List<SubscriptionNotificationModel> GenerateSampleSubscriptionNotifications(
            uint numOfMessages, bool eventList = false,
            MessageEncoding encoding = MessageEncoding.Json,
            NetworkMessageContentMask extraNetworkMessageMask = 0,
            bool isSampleMode = false)
        {
            var messages = new List<SubscriptionNotificationModel>();
            const string publisherId = "Publisher";
            var writer = new DataSetWriterModel
            {
                DataSet = new PublishedDataSetModel
                {
                    DataSetSource = new PublishedDataSetSourceModel
                    {
                        PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData = new List<PublishedDataSetVariableModel>()
                        }
                    },
                    DataSetMetaData = new DataSetMetaDataModel
                    {
                        Name = "testdataset",
                        DataSetClassId = Guid.NewGuid()
                    }
                }
            };
            var writerGroup = new WriterGroupModel
            {
                MessageSettings = new WriterGroupMessageSettingsModel
                {
                    NetworkMessageContentMask =
                        NetworkMessageContentMask.PublisherId |
                        NetworkMessageContentMask.WriterGroupId |
                        NetworkMessageContentMask.NetworkMessageNumber |
                        NetworkMessageContentMask.SequenceNumber |
                        NetworkMessageContentMask.PayloadHeader |
                        NetworkMessageContentMask.Timestamp |
                        NetworkMessageContentMask.DataSetClassId |
                        (isSampleMode ? NetworkMessageContentMask.MonitoredItemMessage : NetworkMessageContentMask.NetworkMessageHeader) |
                        NetworkMessageContentMask.DataSetMessageHeader |
                        extraNetworkMessageMask
                },
                MessageType = encoding
            };
            var seq = 1u;
            for (uint i = 0; i < numOfMessages; i++)
            {
                var suffix = $"-{i}";

                var notifications = new List<MonitoredItemNotificationModel>();

                for (uint k = 0; k < i + 1; k++)
                {
                    var notificationSuffix = suffix + $"-{k}";

                    var monitoredItem = new MonitoredItem
                    {
                        DisplayName = "DisplayName" + notificationSuffix,
                        StartNodeId = new NodeId("NodeId" + notificationSuffix),
                        AttributeId = k
                    };
                    if (eventList)
                    {
                        var handle = new OpcUaMonitoredItem(new EventMonitoredItemModel(), Log.Console<OpcUaMonitoredItem>());
                        handle.Fields.Add(("1", default));
                        handle.Fields.Add(("2", default));
                        handle.Fields.Add(("3", default));
                        handle.Fields.Add(("4", default));
                        handle.Fields.Add(("5", default));
                        handle.Fields.Add(("6", default));
                        monitoredItem.Handle = handle;
                        var eventFieldList = new EventFieldList
                        {
                            ClientHandle = k,
                            EventFields = new Variant[] { 1, 2, 3, 4, 5, 6 },
                            Message = new NotificationMessage
                            {
                                SequenceNumber = seq++
                            }
                        };
                        var notification = eventFieldList.ToMonitoredItemNotifications(monitoredItem);
                        notifications.AddRange(notification);
                    }
                    else
                    {
                        var monitoredItemNotification = new MonitoredItemNotification
                        {
                            ClientHandle = k,
                            Value = new DataValue(new Variant(k), new StatusCode(0), DateTime.UtcNow),
                            Message = new NotificationMessage
                            {
                                SequenceNumber = seq++
                            }
                        };
                        var notification = monitoredItemNotification.ToMonitoredItemNotifications(monitoredItem);
                        notifications.AddRange(notification);
                    }
                }

                var message = new SubscriptionNotificationModel
                {
                    Context = new WriterGroupMessageContext
                    {
                        SequenceNumber = i,
                        PublisherId = publisherId,
                        Writer = writer,
                        WriterGroup = writerGroup
                    },
                    Timestamp = DateTime.UtcNow,
                    MetaData = null,
                    MessageType = eventList ? Azure.IIoT.OpcUa.Encoders.PubSub.MessageType.Event : Azure.IIoT.OpcUa.Encoders.PubSub.MessageType.KeyFrame,
                    ServiceMessageContext = new ServiceMessageContext { },
                    Notifications = notifications,
                    SubscriptionId = 22,
                    EndpointUrl = "EndpointUrl" + suffix,
                    ApplicationUri = "ApplicationUri" + suffix
                };

                messages.Add(message);
            }

            return messages;
        }

        public void Dispose()
        {
        }

        public ValueTask SendAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
