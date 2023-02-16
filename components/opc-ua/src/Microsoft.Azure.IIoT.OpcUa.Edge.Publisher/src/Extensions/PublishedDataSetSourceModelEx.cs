// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Source model extensions
    /// </summary>
    public static class PublishedDataSetSourceModelEx {

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <param name="dataSetMetaData"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static SubscriptionConfigurationModel ToSubscriptionConfigurationModel(
            this PublishedDataSetSourceModel dataSetSource, DataSetMetaDataModel dataSetMetaData,
            ISubscriptionConfig configuration) {
            if (dataSetSource == null) {
                throw new ArgumentNullException(nameof(dataSetSource));
            }
            return new SubscriptionConfigurationModel {
                Priority = dataSetSource.SubscriptionSettings?.Priority,
                LifetimeCount = dataSetSource.SubscriptionSettings?.LifeTimeCount
                    ?? configuration?.DefaultLifeTimeCount,
                KeepAliveCount = dataSetSource.SubscriptionSettings?.MaxKeepAliveCount
                    ?? configuration?.DefaultKeepAliveCount,
                PublishingInterval = dataSetSource.SubscriptionSettings?.PublishingInterval
                    ?? configuration?.DefaultPublishingInterval,
                ResolveDisplayName = dataSetSource.SubscriptionSettings?.ResolveDisplayName
                    ?? configuration?.ResolveDisplayName,
                MetaData = configuration?.DisableDataSetMetaData == true
                    ? null : dataSetMetaData
            };
        }

        /// <summary>
        /// Convert dataset source to monitored item
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static List<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataSetSourceModel dataSetSource,
            ISubscriptionConfig configuration) {
            var monitoredItems = Enumerable.Empty<BaseMonitoredItemModel>();
            if (dataSetSource.PublishedVariables?.PublishedData != null) {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedVariables.ToMonitoredItems(configuration));
            }
            if (dataSetSource.PublishedEvents?.PublishedData != null) {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedEvents.ToMonitoredItems(configuration));
            }
            return monitoredItems.ToList();
        }
    }
}