﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;

    /// <summary>
    /// Event monitored item
    /// </summary>
    public class EventMonitoredItemModel : BaseMonitoredItemModel
    {
        /// <summary>
        /// Event filter
        /// </summary>
        public EventFilterModel EventFilter { get; set; }

        /// <summary>
        /// Condition handling settings
        /// </summary>
        public ConditionHandlingOptionsModel ConditionHandling { get; set; }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>A copy of this object</returns>
        public override BaseMonitoredItemModel Clone()
        {
            return new EventMonitoredItemModel
            {
                Id = Id,
                StartNodeId = StartNodeId,
                SamplingInterval = SamplingInterval,
                QueueSize = QueueSize,
                DiscardNew = DiscardNew,
                EventFilter = EventFilter.Clone(),
                AttributeId = AttributeId,
                IndexRange = IndexRange,
                MonitoringMode = MonitoringMode,
                DisplayName = DisplayName,
                RelativePath = RelativePath,
                ConditionHandling = ConditionHandling?.Clone()
            };
        }

        /// <summary>
        /// Equals function
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>If the objects are equal</returns>
        public override bool Equals(object obj)
        {
            return obj is EventMonitoredItemModel model &&
                   base.Equals(obj) &&
                   EventFilter.IsSameAs(model.EventFilter) &&
                   ConditionHandling.IsSameAs(model.ConditionHandling);
        }

        /// <summary>
        /// Calculate hash code
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(EventFilter);
            hash.Add(ConditionHandling);
            return hash.ToHashCode();
        }
    }
}
