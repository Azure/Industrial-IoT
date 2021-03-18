// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Linq;

    /// <summary>
    /// Published data set events extensions
    /// </summary>
    public static class PublishedDataSetEventsModelEx {

        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="publishedEvent"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static MonitoredItemModel ToMonitoredItem(
            this PublishedDataSetEventModel publishedEvent,
            string displayName = null) {
            if (publishedEvent?.SelectedFields == null) {
                return null;
            }
            return new MonitoredItemModel {
                Id = publishedEvent.Id,
                DisplayName = displayName,
                EventFilter = new EventFilterModel {
                    SelectClauses = publishedEvent.SelectedFields?
                        .Select(s => s.Clone())
                        .ToList(),
                    WhereClause = publishedEvent.Filter.Clone(),
                },
                AggregateFilter = null,
                DiscardNew = publishedEvent.DiscardNew,
                QueueSize = publishedEvent.QueueSize,
                TriggerId = publishedEvent.TriggerId,
                MonitoringMode = publishedEvent.MonitoringMode,
                StartNodeId = publishedEvent.EventNotifier,
                RelativePath = publishedEvent.BrowsePath,
                AttributeId = null,
                DataChangeFilter = null,
                IndexRange = null,
                SamplingInterval = null
            };
        }
    }
}