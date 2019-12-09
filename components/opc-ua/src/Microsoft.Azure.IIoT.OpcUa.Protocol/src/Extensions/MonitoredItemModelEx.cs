// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    /// <summary>
    /// Monitored item model extensions
    /// </summary>
    public static class MonitoredItemModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static MonitoredItemModel Clone(this MonitoredItemModel model) {
            if (model == null) {
                return null;
            }
            return new MonitoredItemModel {
                Id = model.Id,
                TriggerId = model.TriggerId,
                StartNodeId = model.StartNodeId,
                SamplingInterval = model.SamplingInterval,
                QueueSize = model.QueueSize,
                DiscardNew = model.DiscardNew,
                DataChangeFilter = model.DataChangeFilter, // TODO
                EventFilter = model.EventFilter, // TODO
                AggregateFilter = model.AggregateFilter, // TODO
                AttributeId = model.AttributeId,
                IndexRange = model.IndexRange,
                NodeClass = model.NodeClass,
                MonitoringMode = model.MonitoringMode,
                DisplayName = model.DisplayName,
                RelativePath = model.RelativePath
            };
        }
    }
}