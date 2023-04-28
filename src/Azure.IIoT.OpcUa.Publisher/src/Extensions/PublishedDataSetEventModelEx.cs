// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
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
        public static EventMonitoredItemModel? ToMonitoredItem(
            this PublishedDataSetEventModel publishedEvent,
            OpcUaSubscriptionOptions configuration)
        {
            if (publishedEvent == null)
            {
                return null;
            }

            var eventNotifier = publishedEvent.EventNotifier ?? ObjectIds.Server.ToString();
            return new EventMonitoredItemModel
            {
                Id = publishedEvent.Id ?? eventNotifier,
                DisplayName = publishedEvent.PublishedEventName,
                EventFilter = new EventFilterModel
                {
                    SelectClauses = publishedEvent.SelectClauses?
                        .Select(s => s.Clone()!)
                        .Where(s => s != null)
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
                StartNodeId = eventNotifier,
                RelativePath = publishedEvent.BrowsePath,
                AttributeId = null,
                IndexRange = null,
                SamplingInterval = TimeSpan.Zero,
                ConditionHandling = publishedEvent.ConditionHandling.Clone()
            };
        }
    }
}
