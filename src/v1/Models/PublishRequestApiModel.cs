// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;

    /// <summary>
    /// Node publis request twin module model
    /// </summary>
    public class PublishRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishRequestApiModel(PublishRequestModel model) {
            NodeId = model.NodeId;
            Enabled = model.Enabled;
            DisplayName = model.DisplayName;
            PublishingInterval = model.PublishingInterval;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public PublishRequestModel ToServiceModel() {
            return new PublishRequestModel {
                NodeId = NodeId,
                Enabled = Enabled,
                DisplayName = DisplayName,
                PublishingInterval = PublishingInterval
            };
        }

        /// <summary>
        /// Node to publish or unpublish
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Publishing interval of the item
        /// </summary>
        public int? PublishingInterval { get; set; }

        /// <summary>
        /// Whether to enable or disable
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// Display name to use for publishing
        /// </summary>
        public string DisplayName { get; set; }
    }
}
