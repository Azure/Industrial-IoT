﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {

    /// <summary>
    /// Publisher sample model extensions
    /// </summary>
    public static class MonitoredItemMessageModelEx {

        /// <summary>
        /// Clone sample
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static MonitoredItemMessageModel Clone(this MonitoredItemMessageModel model) {
            if (model == null) {
                return null;
            }
            return new MonitoredItemMessageModel {
                SubscriptionId = model.SubscriptionId,
                EndpointId = model.EndpointId,
                DataSetId = model.DataSetId,
                NodeId = model.NodeId,
                DisplayName = model.DisplayName,
                ServerPicoseconds = model.ServerPicoseconds,
                ServerTimestamp = model.ServerTimestamp,
                SourcePicoseconds = model.SourcePicoseconds,
                SourceTimestamp = model.SourceTimestamp,
                Timestamp = model.Timestamp,
                TypeId = model.TypeId,
                Value = model.Value?.Copy(),
                Status = model.Status
            };
        }
    }
}
