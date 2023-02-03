// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System.Collections.Generic;
    using System.Configuration;

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
