// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using System;
    using System.Linq;

    /// <summary>
    /// Published data set events extensions
    /// </summary>
    public static class PublishedDataSetEventModelEx
    {
        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="publishedEvent"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static EventMonitoredItemModel ToMonitoredItem(
            this PublishedDataSetEventModel publishedEvent,
            ISubscriptionConfig configuration)
        {
            if (publishedEvent == null)
            {
                return null;
            }

            return new EventMonitoredItemModel
            {
                Id = publishedEvent.Id,
                DisplayName = publishedEvent.PublishedEventName,
                EventFilter = new EventFilterModel
                {
                    SelectClauses = publishedEvent.SelectClauses?
                        .Select(s => s.Clone())
                        .ToList(),
                    WhereClause = publishedEvent.WhereClause?.Clone(),
                    TypeDefinitionId = publishedEvent.TypeDefinitionId
                },
                DiscardNew = publishedEvent.DiscardNew
                    ?? configuration?.DefaultDiscardNew,

                //
                // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                // 0 the Server returns the default queue size for Event Notifications
                // as revisedQueueSize for event monitored items.
                //
                QueueSize = publishedEvent.QueueSize
                    ?? configuration?.DefaultQueueSize ?? 0,
                MonitoringMode = publishedEvent.MonitoringMode,
                StartNodeId = publishedEvent.EventNotifier,
                RelativePath = publishedEvent.BrowsePath,
                AttributeId = null,
                IndexRange = null,
                SamplingInterval = TimeSpan.Zero,
                ConditionHandling = publishedEvent.ConditionHandling.Clone()
            };
        }
    }
}
