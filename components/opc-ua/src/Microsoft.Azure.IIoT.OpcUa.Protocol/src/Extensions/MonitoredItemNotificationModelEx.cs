// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
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
            var handleId = monitoredItem.Handle as MonitoredItemWrapper;
            if (handleId?.SkipMonitoredItemNotification() ?? false) {
                // Skip change notification
                return null;
            }
            return new MonitoredItemNotificationModel {
                Id = handleId?.Template?.Id,
                DataSetFieldName = handleId?.Template?.DataSetFieldName ?? monitoredItem.DisplayName,
                DisplayName = monitoredItem.DisplayName,
                NodeId = handleId?.Template?.StartNodeId,
                AttributeId = monitoredItem.AttributeId,
                Value = notification.Value,
                SequenceNumber = notification.Message == null || notification.Message.IsEmpty
                    ? (uint?)null
                    : notification.Message.SequenceNumber,
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

            // TODO: Convert to list of notifications using select clauses!

            var handleId = monitoredItem.Handle as MonitoredItemWrapper;
            return new MonitoredItemNotificationModel {
                Id = handleId?.Template?.Id,
                DataSetFieldName = handleId?.Template?.DataSetFieldName ?? monitoredItem.DisplayName,
                DisplayName = monitoredItem.DisplayName,
                NodeId = handleId?.Template?.StartNodeId,
                AttributeId = monitoredItem.AttributeId,
                Value = ToDataValue(eventFieldList, monitoredItem),
                SequenceNumber = eventFieldList.Message == null || eventFieldList.Message.IsEmpty
                    ? (uint?)null
                    : eventFieldList.Message.SequenceNumber,
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
                Value = new EncodeableDictionary(eventFields.EventFields
                    .Select((value, i) => {
                        return new KeyDataValuePair {
                            Key = (monitoredItem.Handle as MonitoredItemWrapper)?.Fields[i].Name,
                            Value = new DataValue(value)
                        };
                    }))
            };
        }
    }
}