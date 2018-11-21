// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// A monitored and published item
    /// </summary>
    public class PublishedItemApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedItemApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedItemApiModel(PublishedItemModel model) {
            NodeId = model.NodeId;
            NodeAttribute = model.NodeAttribute;
            SamplingInterval = model.SamplingInterval;
            PublishingInterval = model.PublishingInterval;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedItemModel ToServiceModel() {
            return new PublishedItemModel {
                NodeId = NodeId,
                NodeAttribute = NodeAttribute,
                SamplingInterval = SamplingInterval,
                PublishingInterval = PublishingInterval
            };
        }

        /// <summary>
        /// Node to monitor
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Attribute to monitor
        /// </summary>
        public NodeAttribute? NodeAttribute { get; set; }

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
