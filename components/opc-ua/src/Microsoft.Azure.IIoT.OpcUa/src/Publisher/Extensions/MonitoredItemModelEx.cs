// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
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
                NodeId = model.NodeId,
                SamplingInterval = model.SamplingInterval,
                HeartbeatInterval = model.HeartbeatInterval,
                QueueSize = model.QueueSize,
                DiscardNew = model.DiscardNew,
                DataChangeFilter = model.DataChangeFilter,
                DeadBandType = model.DeadBandType,
                DeadBandValue = model.DeadBandValue,
                SkipFirst = model.SkipFirst
            };
        }
    }
}