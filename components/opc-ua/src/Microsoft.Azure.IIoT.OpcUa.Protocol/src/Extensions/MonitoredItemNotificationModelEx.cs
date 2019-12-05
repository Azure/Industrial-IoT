// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
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
        /// Convert to monitored item notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="monitoredItems"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
            this DataChangeNotification notification, IEnumerable<MonitoredItem> monitoredItems) {
            for (var i = 0; i < notification.MonitoredItems.Count; i++) {
                var monitoredItem = monitoredItems.SingleOrDefault(
                        m => m.ClientHandle == notification.MonitoredItems[i].ClientHandle);
                if (monitoredItem == null) {
                    continue;
                }
                var message = notification.MonitoredItems[i].ToMonitoredItemNotification(monitoredItem);
                if (message == null) {
                    continue;
                }
                if (message.DiagnosticInfo == null && notification.DiagnosticInfos != null &&
                    i < notification.DiagnosticInfos.Count) {
                    message.DiagnosticInfo = notification.DiagnosticInfos[i];
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
                return null; // todo
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
                NotificationData = notification.Message == null ||
                                    notification.Message.IsEmpty ? null :
                    notification.Message.NotificationData.ToList(),
                PublishTime = notification.Message == null ||
                                    notification.Message.IsEmpty ? (DateTime?)null :
                    notification.Message.PublishTime,
                SequenceNumber = notification.Message == null ||
                                    notification.Message.IsEmpty ? (uint?)null :
                    notification.Message.SequenceNumber,
                StringTable = notification.Message == null ||
                                    notification.Message.IsEmpty ? null :
                    notification.Message.StringTable,
                DiagnosticInfo = notification.DiagnosticInfo
            };
        }
    }
}