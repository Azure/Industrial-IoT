// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Messaging;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class NetworkMessage : IEvent
    {
        public DateTime Timestamp { get; private set; }

        public IEvent SetTimestamp(DateTime value)
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

        public IList<ReadOnlySequence<byte>> Buffers { get; } = new List<ReadOnlySequence<byte>>();

        public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
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

        public static IEvent Create()
        {
            return new NetworkMessage();
        }

        public static IList<SubscriptionNotificationModel> GenerateSampleSubscriptionNotifications(
            uint numOfMessages, bool eventList = false,
            MessageEncoding encoding = MessageEncoding.Json,
            NetworkMessageContentFlags extraNetworkMessageMask = 0,
            bool isSampleMode = false)
        {
            var messages = new List<SubscriptionNotificationModel>();
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

#pragma warning disable CA2000 // Dispose objects before losing scope
            var dataItem = new OpcUaMonitoredItem.DataChange(new DataMonitoredItemModel
            {
                StartNodeId = "i=2258"
            }, Log.Console<OpcUaMonitoredItem.DataChange>());
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning disable CA2000 // Dispose objects before losing scope
            var eventItem = new OpcUaMonitoredItem.Event(new EventMonitoredItemModel
            {
                StartNodeId = "i=2258",
                EventFilter = new EventFilterModel()
            }, Log.Console<OpcUaMonitoredItem.Event>());
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

                var notifications = new List<MonitoredItemNotificationModel>();

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
                        eventItem.TryGetMonitoredItemNotifications(seq, DateTime.UtcNow, eventFieldList, notifications);
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
                        dataItem.TryGetMonitoredItemNotifications(seq, DateTime.UtcNow, monitoredItemNotification, notifications);
                    }
                }

                var message = new SubscriptionNotificationModel
                {
                    Context = new WriterGroupContext
                    {
                        NextWriterSequenceNumber = () => i,
                        Qos = null,
                        Topic = string.Empty,
                        PublisherId = publisherId,
                        Writer = writer,
                        WriterGroup = writerGroup
                    },
                    PublishTimestamp = DateTime.UtcNow,
                    MetaData = null,
                    MessageType = eventList ? Encoders.PubSub.MessageType.Event : Encoders.PubSub.MessageType.KeyFrame,
                    ServiceMessageContext = new ServiceMessageContext(),
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
            return ValueTask.CompletedTask;
        }
    }
}
