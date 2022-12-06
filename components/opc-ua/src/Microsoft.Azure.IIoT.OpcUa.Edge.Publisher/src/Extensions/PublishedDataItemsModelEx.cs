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
    public static class PublishedDataItemsModelEx {

        /// <summary>
        /// Convert to monitored items including heartbeat handling.
        /// </summary>
        /// <param name="dataItems"></param>
        /// <returns></returns>
        public static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataItemsModel dataItems) {
            if (dataItems?.PublishedData == null) {
                return Enumerable.Empty<BaseMonitoredItemModel>();
            }

            var map = new Dictionary<string, BaseMonitoredItemModel>();
            foreach (var item in dataItems.PublishedData) {
                if (item == null) {
                    continue;
                }
                var monitoredItem = item.ToMonitoredItem();

                map.Add(monitoredItem.StartNodeId, monitoredItem);
            }
            return map.Values;
        }
    }
}
