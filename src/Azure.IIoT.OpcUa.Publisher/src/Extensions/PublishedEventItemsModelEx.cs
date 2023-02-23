// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Published event items extensions
    /// </summary>
    public static class PublishedEventItemsModelEx {
        /// <summary>
        /// Convert to monitored items
        /// </summary>
        /// <param name="eventItems"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedEventItemsModel eventItems, ISubscriptionConfig configuration = null) {
            if (eventItems?.PublishedData == null) {
                return Enumerable.Empty<BaseMonitoredItemModel>();
            }

            var map = new Dictionary<string, BaseMonitoredItemModel>();
            foreach (var item in eventItems.PublishedData) {
                if (item == null) {
                    continue;
                }
                var monitoredItem = item.ToMonitoredItem(configuration);
                map.AddOrUpdate(monitoredItem.Id ?? Guid.NewGuid().ToString(), monitoredItem);
            }
            return map.Values;
        }
    }
}