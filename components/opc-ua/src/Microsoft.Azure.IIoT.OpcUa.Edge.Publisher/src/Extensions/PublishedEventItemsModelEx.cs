// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    /// <summary>
    /// Published data items extensions
    /// </summary>
    public static class PublishedEventItemsModelEx {

        /// <summary>
        /// Convert to monitored items including heartbeat handling.
        /// </summary>
        /// <param name="dataItems"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemModel> ToMonitoredItems(
            this PublishedEventItemsModel dataItems) {
            if (dataItems?.PublishedData == null) {
                return Enumerable.Empty<MonitoredItemModel>();
            }

            var map = new Dictionary<string, MonitoredItemModel>();
            foreach (var item in dataItems.PublishedData) {
                if (item == null) {
                    continue;
                }
                var monitoredItem = item.ToMonitoredItem();
                map.Add(monitoredItem.Id ?? Guid.NewGuid().ToString(), monitoredItem);
                /*  Heartbeat using triggering mode
                if (monitoredItem.HeartbeatInterval == null) {
                    continue;
                }

                //
                // We add a timer as heartbeat trigger that samples
                // server time, but we configure it so it will not be
                // part of the notifications (Sampling only).
                //
                monitoredItem.TriggerId ??= ("heartbeat_" +
                    monitoredItem.HeartbeatInterval.Value.TotalSeconds.ToString()).ToSha1Hash();
                if (map.ContainsKey(monitoredItem.TriggerId)) {
                    continue;
                }
                monitoredItem.MonitoringMode = MonitoringMode.Sampling;
                map.Add(monitoredItem.TriggerId,
                    new MonitoredItemModel {
                        MonitoringMode = MonitoringMode.Sampling,
                        StartNodeId = "i=2258",
                        SamplingInterval = monitoredItem.HeartbeatInterval,
                        Id = monitoredItem.TriggerId,
                    });
                */
            }
            return map.Values;
        }
    }
}