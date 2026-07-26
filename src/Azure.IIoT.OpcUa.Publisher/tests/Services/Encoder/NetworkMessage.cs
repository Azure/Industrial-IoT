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
    using Azure.IIoT.OpcUa.Core.Logging;
    using Azure.IIoT.OpcUa.Core.Messaging;
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
            bool randomTopic = false)
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
                        NetworkMessageContentFlags.NetworkMessageHeader |
                        NetworkMessageContentFlags.DataSetMessageHeader |
                        extraNetworkMessageMask
                },
                MessageType = encoding
            };
            var seq = 1u;

            var subscriber = new Mock<ISubscriber>();
            var eventFieldNames = new[] { "1", "2", "3", "4", "5", "6" };
            var itemSequenceNumber = 0u;

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
                        var eventFields = new Variant[] { 1, 2, 3, 4, 5, 6 };
                        //
                        // Important - so the event is properly batched during encoding
                        // the same sequence number must be used for all notifications!
                        //
                        var sequenceNumber = ++itemSequenceNumber;
                        for (var f = 0; f < eventFields.Length; f++)
                        {
                            notifications.Add(new MonitoredItemNotificationModel
                            {
                                Id = nodeId,
                                DataSetName = displayName,
                                DataSetFieldName = eventFieldNames[f],
                                NodeId = nodeId,
                                Value = new DataValue(eventFields[f]),
                                Flags = 0,
                                SequenceNumber = sequenceNumber
                            });
                        }
                    }
                    else
                    {
                        notifications.Add(new MonitoredItemNotificationModel
                        {
                            Id = nodeId,
                            DataSetName = displayName,
                            DataSetFieldName = displayName,
                            NodeId = nodeId,
                            Value = new DataValue(new Variant(k), new StatusCode(0),
                                DateTime.UtcNow),
                            Flags = 0,
                            Overflow = 0,
                            SequenceNumber = ++itemSequenceNumber
                        });
                    }
                }

#pragma warning disable CA5394 // Do not use insecure randomness
                var message = new OpcUaSubscriptionNotification(DateTimeOffset.UtcNow,
                    notifications: notifications)
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
                        ExtensionFields = Array.Empty<(string, DataValue?)>(),
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
