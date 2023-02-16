// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Messaging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;

    public sealed class NetworkMessage : ITelemetryEvent {
        public DateTime Timestamp { get; set; }
        public string ContentType { get; set; }
        public string ContentEncoding { get; set; }
        public string MessageSchema { get; set; }
        public string RoutingInfo { get; set; }
        public string OutputName { get; set; }
        public bool Retain { get; set; }
        public TimeSpan Ttl { get; set; }
        public IReadOnlyList<byte[]> Buffers { get; set; }

        public static ITelemetryEvent Create() {
            return new NetworkMessage();
        }

        public static List<SubscriptionNotificationModel> GenerateSampleSubscriptionNotifications(
            uint numOfMessages, bool eventList = false,
            MessageEncoding encoding = MessageEncoding.Json,
            NetworkMessageContentMask extraNetworkMessageMask = 0,
            bool isSampleMode = false) {
            var messages = new List<SubscriptionNotificationModel>();
            var publisherId = "Publisher";
            var writer = new DataSetWriterModel {
                DataSet = new PublishedDataSetModel {
                    DataSetSource = new PublishedDataSetSourceModel {
                        PublishedVariables = new PublishedDataItemsModel {
                            PublishedData = new List<PublishedDataSetVariableModel>()
                        }
                    },
                    DataSetMetaData = new DataSetMetaDataModel {
                        Name = "testdataset",
                        DataSetClassId = Guid.NewGuid()
                    }
                }
            };
            var writerGroup = new WriterGroupModel {
                MessageSettings = new WriterGroupMessageSettingsModel {
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
                        var handle = new OpcUaMonitoredItem(new EventMonitoredItemModel(), ConsoleLogger.Create());
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
                            Message = new NotificationMessage {
                                SequenceNumber = seq++
                            }
                        };
                        var notification = eventFieldList.ToMonitoredItemNotifications(monitoredItem);
                        notifications.AddRange(notification);
                    }
                    else {
                        var monitoredItemNotification = new MonitoredItemNotification {
                            ClientHandle = k,
                            Value = new DataValue(new Variant(k), new StatusCode(0), DateTime.UtcNow),
                            Message = new NotificationMessage {
                                SequenceNumber = seq++
                            }
                        };
                        var notification = monitoredItemNotification.ToMonitoredItemNotifications(monitoredItem);
                        notifications.AddRange(notification);
                    }
                }

                var message = new SubscriptionNotificationModel {
                    Context = new WriterGroupMessageContext {
                        SequenceNumber = i,
                        PublisherId = publisherId,
                        Writer = writer,
                        WriterGroup = writerGroup,
                    },
                    Timestamp = DateTime.UtcNow,
                    MetaData = null,
                    MessageType = eventList ? Opc.Ua.PubSub.MessageType.Event : Opc.Ua.PubSub.MessageType.KeyFrame,
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

        public void Dispose() {
        }
    }
}
