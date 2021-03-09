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
    public static class PublishedDataSetEventModelEx {

        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="publishedEvents"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static EventMonitoredItemModel ToMonitoredItem(
            this PublishedDataSetEventModel publishedEvents,
            string displayName = null) {
            if (publishedEvents?.SelectedFields == null) {
                return null;
            }
            return new EventMonitoredItemModel {
                Id = publishedEvents.Id,
                DisplayName = displayName,
                EventFilter = new EventFilterModel {
                    SelectClauses = publishedEvents.SelectedFields?
                        .Select(s => s.Clone())
                        .ToList(),
                    WhereClause = publishedEvents.Filter.Clone(),
                },
                DiscardNew = publishedEvents.DiscardNew,
                QueueSize = publishedEvents.QueueSize,
                TriggerId = publishedEvents.TriggerId,
                MonitoringMode = publishedEvents.MonitoringMode,
                StartNodeId = publishedEvents.EventNotifier,
                RelativePath = publishedEvents.BrowsePath,
                AttributeId = null,
                IndexRange = null,
                SamplingInterval = null
            };
        }
    }
}