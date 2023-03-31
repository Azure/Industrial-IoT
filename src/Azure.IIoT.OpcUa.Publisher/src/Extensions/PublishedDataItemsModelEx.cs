// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Published data items extensions
    /// </summary>
    public static class PublishedDataItemsModelEx
    {
        /// <summary>
        /// Convert to monitored items including heartbeat handling.
        /// </summary>
        /// <param name="dataItems"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataItemsModel dataItems, SubscriptionOptions options)
        {
            if (dataItems?.PublishedData != null)
            {
                foreach (var publishedData in dataItems.PublishedData)
                {
                    var item = publishedData?.ToMonitoredItem(options);
                    if (item != null)
                    {
                        yield return item;
                    }
                }
            }
        }
    }
}
