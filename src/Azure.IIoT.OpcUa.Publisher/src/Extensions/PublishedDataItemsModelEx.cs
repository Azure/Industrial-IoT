// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using System.Collections.Generic;

    /// <summary>
    /// Published data items extensions
    /// </summary>
    public static class PublishedDataItemsModelEx {

        /// <summary>
        /// Convert to monitored items including heartbeat handling.
        /// </summary>
        /// <param name="dataItems"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataItemsModel dataItems,
            ISubscriptionConfig configuration = null) {
            if (dataItems?.PublishedData != null) {
                foreach (var item in dataItems.PublishedData) {
                    if (item == null) {
                        continue;
                    }
                    yield return item.ToMonitoredItem(configuration);
                }
            }
        }
    }
}
