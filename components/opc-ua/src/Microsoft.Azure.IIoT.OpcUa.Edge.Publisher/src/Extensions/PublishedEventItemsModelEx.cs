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
    /// Published event items extensions
    /// </summary>
    public static class PublishedEventItemsModelEx {

        /// <summary>
        /// Convert to monitored items
        /// </summary>
        /// <param name="eventItems"></param>
        /// <returns></returns>
        public static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedEventItemsModel eventItems) {
            if (eventItems?.PublishedData == null) {
                return Enumerable.Empty<BaseMonitoredItemModel>();
            }

            var map = new Dictionary<string, BaseMonitoredItemModel>();
            foreach (var item in eventItems.PublishedData) {
                if (item == null) {
                    continue;
                }
                var monitoredItem = item.ToMonitoredItem();
                map.AddOrUpdate(monitoredItem.Id ?? Guid.NewGuid().ToString(), monitoredItem);
            }
            return map.Values;
        }
    }
}