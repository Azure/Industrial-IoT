// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Notification model extensions
    /// </summary>
    public static class MonitoredItemNotificationModelEx {

        /// <summary>
        /// Clone notification
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static MonitoredItemNotificationModel Clone(this MonitoredItemNotificationModel model) {
            if (model == null) {
                return null;
            }
            return new MonitoredItemNotificationModel {
                Id = model.Id,
                DataSetFieldName = model.DataSetFieldName,
                DisplayName = model.DisplayName,
                NodeId = model.NodeId,
                AttributeId = model.AttributeId,
                Value = model.Value, // Not cloning, should be immutable
                MessageId = model.MessageId,
                SequenceNumber = model.SequenceNumber,
                IsHeartbeat = model.IsHeartbeat,
            };
        }

        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="monitoredItems"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
            this DataChangeNotification notification, IEnumerable<MonitoredItem> monitoredItems) {
            if (notification?.MonitoredItems != null) {
                for (var i = 0; i < notification.MonitoredItems.Count; i++) {
                    var monitoredItem = monitoredItems.SingleOrDefault(
                            m => m.ClientHandle == notification?.MonitoredItems[i]?.ClientHandle);
                    if (monitoredItem != null) {
                        var messages = notification.MonitoredItems[i]?.ToMonitoredItemNotifications(monitoredItem);
                        foreach (var message in messages) {
                            yield return message;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
            this IEncodeable notification, MonitoredItem monitoredItem,
            Func<MonitoredItemNotificationModel> defaultValue = null) {
            if (notification != null && monitoredItem != null) {
                if (notification is MonitoredItemNotification m) {
                    return m.ToMonitoredItemNotifications(monitoredItem);
                }
                if (notification is EventFieldList e) {
                    return e.ToMonitoredItemNotifications(monitoredItem);
                }
            }
            var def = defaultValue?.Invoke();
            return def == null ? Enumerable.Empty<MonitoredItemNotificationModel>() : def.YieldReturn();
        }

        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="monitoredItem"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
            this MonitoredItemNotification notification, MonitoredItem monitoredItem) {
            if (notification == null || monitoredItem == null) {
                yield break;
            }
            var handleId = monitoredItem.Handle as MonitoredItemWrapper;
            if (handleId?.SkipMonitoredItemNotification() ?? false) {
                // Skip change notification
                yield break;
            }
            var sequence = notification.Message == null || notification.Message.IsEmpty
                ? (uint?)null
                : notification.Message.SequenceNumber;
            yield return new MonitoredItemNotificationModel {
                Id = handleId?.Template?.Id,
                DataSetFieldName = handleId?.Template?.DataSetFieldName ?? monitoredItem.DisplayName,
                DisplayName = monitoredItem.DisplayName,
                NodeId = handleId?.Template?.StartNodeId,
                AttributeId = monitoredItem.AttributeId,
                Value = notification.Value,
                MessageId = sequence ?? (uint)notification.GetHashCode(),
                SequenceNumber = sequence,
                IsHeartbeat = false
            };
        }

        /// <summary>
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="eventFieldList"></param>
        /// <param name="monitoredItem"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
            this EventFieldList eventFieldList, MonitoredItem monitoredItem) {
            var handleId = monitoredItem.Handle as MonitoredItemWrapper;
            if (eventFieldList != null && monitoredItem != null) {
                for (var i = 0; i < eventFieldList.EventFields.Count; i++) {
                    var sequenceNumber = eventFieldList.Message == null || eventFieldList.Message.IsEmpty
                            ? (uint?)null
                            : eventFieldList.Message.SequenceNumber;
                    yield return new MonitoredItemNotificationModel {
                        Id = handleId?.Template?.Id,
                        DataSetFieldName = handleId?.Fields[i].Name,
                        DisplayName = monitoredItem.DisplayName,
                        NodeId = handleId?.Template?.StartNodeId,
                        AttributeId = monitoredItem.AttributeId,
                        Value = new DataValue(eventFieldList.EventFields[i]),
                        SequenceNumber = sequenceNumber,
                        MessageId = sequenceNumber ?? (uint)eventFieldList.GetHashCode(),
                        IsHeartbeat = false,
                    };
                }
            }
        }
    }
}