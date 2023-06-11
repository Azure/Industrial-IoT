// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Source model extensions
    /// </summary>
    public static class PublishedDataSetSourceModelEx
    {
        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <param name="dataSetMetaData"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetSource"/> is <c>null</c>.</exception>
        public static SubscriptionConfigurationModel ToSubscriptionConfigurationModel(
            this PublishedDataSetSourceModel dataSetSource, DataSetMetaDataModel? dataSetMetaData,
            OpcUaSubscriptionOptions configuration)
        {
            return new SubscriptionConfigurationModel
            {
                Priority = dataSetSource.SubscriptionSettings?.Priority,
                MaxNotificationsPerPublish = dataSetSource.SubscriptionSettings?.MaxNotificationsPerPublish,
                LifetimeCount = dataSetSource.SubscriptionSettings?.LifeTimeCount
                    ?? configuration?.DefaultLifeTimeCount,
                KeepAliveCount = dataSetSource.SubscriptionSettings?.MaxKeepAliveCount
                    ?? configuration?.DefaultKeepAliveCount,
                PublishingInterval = dataSetSource.SubscriptionSettings?.PublishingInterval
                    ?? configuration?.DefaultPublishingInterval,
                ResolveDisplayName = dataSetSource.SubscriptionSettings?.ResolveDisplayName
                    ?? configuration?.ResolveDisplayName,
                UseDeferredAcknoledgements = dataSetSource.SubscriptionSettings?.UseDeferredAcknoledgements
                    ?? configuration?.UseDeferredAcknoledgements,
                MetaData = configuration?.DisableDataSetMetaData == true
                    ? null : dataSetMetaData
            };
        }

        /// <summary>
        /// Convert dataset source to monitored item
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static List<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataSetSourceModel dataSetSource, OpcUaSubscriptionOptions options)
        {
            var monitoredItems = Enumerable.Empty<BaseMonitoredItemModel>();
            if (dataSetSource.PublishedVariables?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedVariables.ToMonitoredItems(options));
            }
            if (dataSetSource.PublishedEvents?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedEvents.ToMonitoredItems(options));
            }
            return monitoredItems.ToList();
        }
    }
}
