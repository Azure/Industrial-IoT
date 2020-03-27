﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
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
        /// <returns></returns>
        public static SubscriptionConfigurationModel ToSubscriptionConfigurationModel(
            this PublishedDataSetSourceModel dataSetSource) {
            if (dataSetSource == null) {
                throw new ArgumentNullException(nameof(dataSetSource));
            }
            return new SubscriptionConfigurationModel {
                Priority = dataSetSource.SubscriptionSettings?.Priority,
                LifetimeCount = dataSetSource.SubscriptionSettings?.LifeTimeCount,
                KeepAliveCount = dataSetSource.SubscriptionSettings?.MaxKeepAliveCount,
                MaxNotificationsPerPublish = dataSetSource.SubscriptionSettings?.MaxNotificationsPerPublish,
                PublishingInterval = dataSetSource.SubscriptionSettings?.PublishingInterval,
                ResolveDisplayName = dataSetSource.SubscriptionSettings?.ResolveDisplayName
            };
        }

        /// <summary>
        /// Convert dataset source to monitored item
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <returns></returns>
        public static List<MonitoredItemModel> ToMonitoredItems(
            this PublishedDataSetSourceModel dataSetSource) {
            var monitoredItems = Enumerable.Empty<MonitoredItemModel>();
            if (dataSetSource.PublishedVariables?.PublishedData != null) {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedVariables.ToMonitoredItems());
            }
            if (dataSetSource.PublishedEvents?.SelectedFields != null) {
                monitoredItems = monitoredItems
                    .Append(dataSetSource.PublishedEvents.ToMonitoredItem());
            }
            return monitoredItems.ToList();
        }
    }
}