// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Encoders;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Notification model extensions
    /// </summary>
    public static class MonitoredItemNotificationModelEx {

        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="monitoredItems"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
            this DataChangeNotification notification, IEnumerable<MonitoredItem> monitoredItems) {
            for (var i = 0; i < notification.MonitoredItems.Count; i++) {
                var monitoredItem = monitoredItems.SingleOrDefault(
                        m => m.ClientHandle == notification?.MonitoredItems[i]?.ClientHandle);
                if (monitoredItem == null) {
                    continue;
                }
                var message = notification?.MonitoredItems[i]?
                    .ToMonitoredItemNotification(monitoredItem);
                if (message == null) {
                    continue;
                }
                yield return message;
            }
        }

        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="monitoredItems"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
            this EventNotificationList notification, IEnumerable<MonitoredItem> monitoredItems) {
            for (var i = 0; i < notification.Events.Count; i++) {
                var monitoredItem = monitoredItems.SingleOrDefault(
                        m => m.ClientHandle == notification?.Events[i]?.ClientHandle);
                if (monitoredItem == null) {
                    continue;
                }

                var message = notification?.Events[i]?
                    .ToMonitoredItemNotification(monitoredItem);
                if (message == null) {
                    continue;
                }
                yield return message;
            }
        }


        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static MonitoredItemNotificationModel ToMonitoredItemNotification(
            this IEncodeable notification, MonitoredItem monitoredItem,
            Func<MonitoredItemNotificationModel> defaultValue = null) {
            if (notification == null || monitoredItem == null) {
                return defaultValue?.Invoke();
            }
            if (notification is MonitoredItemNotification m) {
                return m.ToMonitoredItemNotification(monitoredItem);
            }
            if (notification is EventFieldList e) {
                return e.ToMonitoredItemNotification(monitoredItem);
            }

            return defaultValue?.Invoke();
        }

        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="monitoredItem"></param>
        /// <returns></returns>
        public static MonitoredItemNotificationModel ToMonitoredItemNotification(
           this MonitoredItemNotification notification, MonitoredItem monitoredItem) {
            if (notification == null || monitoredItem == null) {
                return null;
            }
            return new MonitoredItemNotificationModel {
                Id = monitoredItem.DisplayName,
                DisplayName = monitoredItem.DisplayName,
                NodeId = monitoredItem.StartNodeId,
                AttributeId = monitoredItem.AttributeId,
                ClientHandle = notification.ClientHandle,
                Value = notification.Value,
                Overflow = notification.Value?.StatusCode.Overflow,
                NotificationData = notification.Message == null || notification.Message.IsEmpty
                    ? null
                    : notification.Message.NotificationData.ToList(),
                PublishTime = notification.Message == null || notification.Message.IsEmpty
                    ? (DateTime?)null
                    : notification.Message.PublishTime,
                SequenceNumber = notification.Message == null || notification.Message.IsEmpty
                    ? (uint?)null
                    : notification.Message.SequenceNumber,
                StringTable = notification.Message == null || notification.Message.IsEmpty
                    ? null
                    : notification.Message.StringTable,
                DiagnosticInfo = notification.DiagnosticInfo,
                IsHeartbeat = false
            };
        }

        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="eventFieldList"></param>
        /// <param name="monitoredItem"></param>
        /// <returns></returns>
        public static MonitoredItemNotificationModel ToMonitoredItemNotification(
           this EventFieldList eventFieldList, MonitoredItem monitoredItem) {
            if (eventFieldList == null || monitoredItem == null) {
                return null;
            }
            return new MonitoredItemNotificationModel {
                Id = monitoredItem.DisplayName,
                DisplayName = monitoredItem.DisplayName,
                NodeId = monitoredItem.StartNodeId,
                AttributeId = monitoredItem.AttributeId,
                ClientHandle = eventFieldList.ClientHandle,
                Value = ToDataValue(eventFieldList, monitoredItem),
                NotificationData = eventFieldList.Message == null || eventFieldList.Message.IsEmpty
                    ? null
                    : eventFieldList.Message.NotificationData.ToList(),
                PublishTime = eventFieldList.Message == null || eventFieldList.Message.IsEmpty
                    ? (DateTime?)null
                    : eventFieldList.Message.PublishTime,
                SequenceNumber = eventFieldList.Message == null || eventFieldList.Message.IsEmpty
                    ? (uint?)null
                    : eventFieldList.Message.SequenceNumber,
                StringTable = eventFieldList.Message == null || eventFieldList.Message.IsEmpty
                    ? null
                    : eventFieldList.Message.StringTable,
                IsHeartbeat = false

            };
        }


        /// <summary>
        /// Convert to Datavalue
        /// </summary>
        /// <param name="eventFields"></param>
        /// <param name="monitoredItem"></param>
        /// <returns></returns>
        public static DataValue ToDataValue(this EventFieldList eventFields,
            MonitoredItem monitoredItem) {
            if (eventFields == null) {
                return new DataValue(StatusCodes.BadNoData);
            }
            return new DataValue {
                ServerPicoseconds = 0,
                ServerTimestamp = eventFields.GetEventValue<DateTime>(
                    BrowseNames.Time, monitoredItem),
                SourcePicoseconds = 0,
                SourceTimestamp = eventFields.GetEventValue<DateTime>(
                    BrowseNames.ReceiveTime, monitoredItem),
                StatusCode = eventFields.GetEventValue<StatusCode>(
                    BrowseNames.StatusCode, monitoredItem),
                Value = new EncodeableDictionary {
                    Fields = new KeyValuePairCollection(eventFields.EventFields
                        .Select((value, i) => new Opc.Ua.KeyValuePair {
                            Key = monitoredItem.GetFieldName(i),
                            Value = value
                        }))
                }
            };
        }

        /// <summary>
        /// Returns value of the field name containing the event type.
        /// </summary>
        public static T GetEventValue<T>(this EventFieldList eventFields, string name,
            MonitoredItem monitoredItem, T defaultValue = default) {
            // get value
            var value = monitoredItem.GetFieldValue(eventFields, ObjectTypes.BaseEventType, name);
            if (value != null) {
                return value.As<T>();
            }
            return defaultValue;
        }
    }
}