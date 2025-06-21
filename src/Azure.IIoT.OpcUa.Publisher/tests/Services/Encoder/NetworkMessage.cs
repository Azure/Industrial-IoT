// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Messaging;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class NetworkMessage : IEvent
    {
        public CloudEventHeader CloudEvent { get; private set; }

        public IEvent AsCloudEvent(CloudEventHeader header)
        {
            CloudEvent = header;
            return this;
        }

        public DateTimeOffset Timestamp { get; private set; }

        public IEvent SetTimestamp(DateTimeOffset value)
        {
            Timestamp = value;
            return this;
        }

        public QoS QoS { get; private set; }

        public IEvent SetQoS(QoS value)
        {
            QoS = value;
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

        public string Topic { get; private set; }

        public IEvent SetTopic(string value)
        {
            Topic = value;
            return this;
        }

        public IEventSchema Schema { get; private set; }

        public IEvent SetSchema(IEventSchema schema)
        {
            Schema = schema;
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

        public IList<ReadOnlySequence<byte>> Buffers { get; } = [];

        public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
        {
            Buffers.AddRange(value);
            return this;
        }

        public Dictionary<string, string> Properties { get; } = [];

        public IEvent AddProperty(string name, string value)
        {
            Properties.AddOrUpdate(name, value);
            return this;
        }

        public static IEvent Create()
        {
            return new NetworkMessage();
        }

        public static IList<OpcUaSubscriptionNotification> GenerateSampleSubscriptionNotifications(
            uint numOfMessages, bool eventList = false,
            MessageEncoding encoding = MessageEncoding.Json,
            NetworkMessageContentFlags extraNetworkMessageMask = 0,
            bool isSampleMode = false, bool randomTopic = false)
        {
            var messages = new List<OpcUaSubscriptionNotification>();
            const string publisherId = "Publisher";
            var writer = new DataSetWriterModel
            {
                Id = string.Empty,
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
                Id = string.Empty,
                MessageSettings = new WriterGroupMessageSettingsModel
                {
                    NetworkMessageContentMask =
                        NetworkMessageContentFlags.PublisherId |
                        NetworkMessageContentFlags.WriterGroupId |
                        NetworkMessageContentFlags.NetworkMessageNumber |
                        NetworkMessageContentFlags.SequenceNumber |
                        NetworkMessageContentFlags.PayloadHeader |
                        NetworkMessageContentFlags.Timestamp |
                        NetworkMessageContentFlags.DataSetClassId |
                        (isSampleMode ? NetworkMessageContentFlags.MonitoredItemMessage : NetworkMessageContentFlags.NetworkMessageHeader) |
                        NetworkMessageContentFlags.DataSetMessageHeader |
                        extraNetworkMessageMask
                },
                MessageType = encoding
            };
            var seq = 1u;

            var subscriber = new Mock<ISubscriber>();
#pragma warning disable CA2000 // Dispose objects before losing scope
            var dataItem = new OpcUaMonitoredItem.DataChange(subscriber.Object, new DataMonitoredItemModel
            {
                StartNodeId = "i=2258"
            }, Log.Console<OpcUaMonitoredItem.DataChange>(), TimeProvider.System);
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning disable CA2000 // Dispose objects before losing scope
            var eventItem = new OpcUaMonitoredItem.Event(subscriber.Object, new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel()
            }, Log.Console<OpcUaMonitoredItem.Event>(), TimeProvider.System);
#pragma warning restore CA2000 // Dispose objects before losing scope
            eventItem.Fields.Add(("1", default));
            eventItem.Fields.Add(("2", default));
            eventItem.Fields.Add(("3", default));
            eventItem.Fields.Add(("4", default));
            eventItem.Fields.Add(("5", default));
            eventItem.Fields.Add(("6", default));

            for (uint i = 0; i < numOfMessages; i++)
            {
                var suffix = $"-{i}";

                var notifications = new OpcUaMonitoredItem.MonitoredItemNotifications();

                for (uint k = 0; k < i + 1; k++)
                {
                    var notificationSuffix = suffix + $"-{k}";

                    var displayName = "DisplayName" + notificationSuffix;
                    var nodeId = "NodeId" + notificationSuffix;
                    if (eventList)
                    {
                        var eventFieldList = new EventFieldList
                        {
                            ClientHandle = k,
                            EventFields = new Variant[] { 1, 2, 3, 4, 5, 6 },
                            Message = new NotificationMessage
                            {
                                SequenceNumber = seq++
                            }
                        };
                        // Fake the item to be created as part of the subscription and grab the data
                        eventItem.Template = eventItem.Template with
                        {
                            StartNodeId = nodeId,
                            DataSetFieldId = nodeId,
                            DataSetFieldName = displayName,
                        };
                        eventItem.DisplayName = displayName;
                        eventItem.StartNodeId = new NodeId(nodeId, 0);
                        eventItem.Handle = eventItem;
                        eventItem.Valid = true;
                        eventItem.TryGetMonitoredItemNotifications(DateTimeOffset.UtcNow, eventFieldList, notifications);
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
                        // Fake the item to be created as part of the subscription and grab the data
                        dataItem.Template = dataItem.Template with
                        {
                            StartNodeId = nodeId,
                            DataSetFieldId = nodeId,
                            DataSetFieldName = displayName,
                        };
                        dataItem.DisplayName = displayName;
                        dataItem.StartNodeId = new NodeId(nodeId, 0);
                        dataItem.Handle = dataItem;
                        dataItem.Valid = true;
                        dataItem.TryGetMonitoredItemNotifications(DateTimeOffset.UtcNow,
                            monitoredItemNotification, notifications);
                    }
                }

#pragma warning disable CA5394 // Do not use insecure randomness
                var message = new OpcUaSubscriptionNotification(DateTimeOffset.UtcNow,
                    notifications: notifications.Notifications[subscriber.Object])
                {
                    Context = new DataSetWriterContext
                    {
                        NextWriterSequenceNumber = () => i,
                        DataSetWriterId = 1,
                        Qos = null,
                        Topic = randomTopic ? Guid.NewGuid().ToString() : string.Empty,
                        Retain = false,
                        Ttl = randomTopic ? TimeSpan.FromSeconds(Random.Shared.Next(60)) : null,
                        PublisherId = publisherId,
                        ExtensionFields = Array.Empty<(string, DataValue)>(),
                        Schema = null, // TODO
                        CloudEvent = null, // TODO
                        Writer = writer,
                        WriterName = writer.DataSetWriterName ?? Constants.DefaultDataSetWriterName,
                        MetaData = null,
                        WriterGroup = writerGroup
                    },
                    PublishTimestamp = DateTimeOffset.UtcNow,
                    MessageType = eventList ? Encoders.PubSub.MessageType.Event : Encoders.PubSub.MessageType.KeyFrame,
                    EndpointUrl = "EndpointUrl" + suffix,
                    ApplicationUri = "ApplicationUri" + suffix
                };
#pragma warning restore CA5394 // Do not use insecure randomness

                messages.Add(message);
            }

            return messages;
        }

        public void Dispose()
        {
        }

        public ValueTask SendAsync(CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
