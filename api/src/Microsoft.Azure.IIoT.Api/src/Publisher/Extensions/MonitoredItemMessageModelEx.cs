// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;

    /// <summary>
    /// Publisher sample model extensions
    /// </summary>
    public static class MonitoredItemMessageModelEx {

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static MonitoredItemMessageApiModel ToApiModel(
            this MonitoredItemMessageModel model) {
            if (model == null) {
                return null;
            }
            return new MonitoredItemMessageApiModel {
                PublisherId = model.PublisherId,
                DataSetWriterId = model.DataSetWriterId,
                EndpointId = model.EndpointId,
                NodeId = model.NodeId,
                DisplayName = model.DisplayName,
                ServerTimestamp = model.ServerTimestamp,
                SourceTimestamp = model.SourceTimestamp,
                Timestamp = model.Timestamp,
                Value = model.Value.Copy(),
                TypeId = model.TypeId,
                Status = model.Status
            };
        }
    }
}
