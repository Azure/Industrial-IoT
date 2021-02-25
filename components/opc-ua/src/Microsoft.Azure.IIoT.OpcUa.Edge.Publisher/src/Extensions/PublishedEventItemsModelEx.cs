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
        /// <param name="eventItems"></param>
        /// <returns></returns>
        public static IEnumerable<MonitoredItemModel> ToMonitoredItems(
            this PublishedEventItemsModel eventItems) {
            if (eventItems?.PublishedEvents == null) {
                return Enumerable.Empty<MonitoredItemModel>();
            }

            var map = new Dictionary<string, MonitoredItemModel>();
            foreach (var item in eventItems.PublishedEvents) {
                if (item == null) {
                    continue;
                }
                var monitoredItem = item.ToMonitoredItem();
                map.Add(monitoredItem.Id ?? Guid.NewGuid().ToString(), monitoredItem);
            }
            return map.Values;
        }
    }
}