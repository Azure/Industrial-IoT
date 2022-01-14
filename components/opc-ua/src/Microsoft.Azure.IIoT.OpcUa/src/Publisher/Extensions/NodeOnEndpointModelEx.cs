// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Extensions {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Create a NodeOnEndpoint from OpcNode model
    /// </summary>
    public static class NodeOnEndpointModelEx {

        /// <summary>
        /// Create a NodeOnEndpoint from OpcNode model
        /// </summary>
        public static OpcNodeOnEndpointModel ToNodeOnEndpointModel(
            this OpcNodeModel model) {
            if (model == null) {
                return null;
            }

            return new OpcNodeOnEndpointModel {
                Id = model.Id,
                ExpandedNodeId = model.ExpandedNodeId,
                OpcSamplingInterval = model.OpcSamplingInterval,
                OpcPublishingInterval = model.OpcPublishingInterval,
                DisplayName = model.DisplayName,
                HeartbeatInterval = model.HeartbeatInterval,
                SkipFirst = model.SkipFirst,
            };
        }
    }
}
