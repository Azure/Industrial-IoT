// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;

    /// <summary>
    /// Node publis request webservice api model
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
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public PublishRequestModel ToServiceModel() {
            return new PublishRequestModel {
                NodeId = NodeId,
                Enabled = Enabled
            };
        }

        /// <summary>
        /// Node to publish or unpublish
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Whether to enable or disable
        /// </summary>
        public bool Enabled { get; set; }
    }
}
