// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Linq;
    using System;

    /// <summary>
    /// Published data set events extensions
    /// </summary>
    public static class PublishedDataSetEventModelEx {

        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="publishedEvent"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static EventMonitoredItemModel ToMonitoredItem(
            this PublishedDataSetEventModel publishedEvent,
            string displayName = null) {
            if (publishedEvent == null) {
                return null;
            }

            return new EventMonitoredItemModel {
                Id = publishedEvent.Id,
                DisplayName = displayName,
                EventFilter = new EventFilterModel {
                    SelectClauses = publishedEvent.SelectClauses?
                        .Select(s => s.Clone())
                        .ToList(),
                    WhereClause = publishedEvent.WhereClause?.Clone(),
                    TypeDefinitionId = publishedEvent.TypeDefinitionId,
                },
                DiscardNew = publishedEvent.DiscardNew,

                //
                // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                // 0 the Server returns the default queue size for Event Notifications
                // as revisedQueueSize for event monitored items.
                //
                QueueSize = publishedEvent.QueueSize ?? 0,
                TriggerId = publishedEvent.TriggerId,
                MonitoringMode = publishedEvent.MonitoringMode,
                StartNodeId = publishedEvent.EventNotifier,
                RelativePath = publishedEvent.BrowsePath,
                AttributeId = null,
                IndexRange = null,
                SamplingInterval = TimeSpan.Zero,
                ConditionHandling = publishedEvent.ConditionHandling.Clone(),
            };
        }
    }
}