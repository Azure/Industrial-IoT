// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// A monitored and published node
    /// </summary>
    public class PublishedNodeApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedNodeApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedNodeApiModel(PublishedNodeModel model) {
            NodeId = model.NodeId;
            SamplingInterval = model.SamplingInterval;
            PublishingInterval = model.PublishingInterval;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedNodeModel ToServiceModel() {
            return new PublishedNodeModel {
                NodeId = NodeId,
                SamplingInterval = SamplingInterval,
                PublishingInterval = PublishingInterval
            };
        }

        /// <summary>
        /// Node to monitor
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Publishing interval to use
        /// </summary>
        public int? PublishingInterval { get; set; }

        /// <summary>
        /// Sampling interval to use
        /// </summary>
        public int? SamplingInterval { get; set; }
    }
}
