// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Info about a published nodes
    /// </summary>
    public class PublishedNodeApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedNodeApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedNodeApiModel(PublishedNodeModel model) {
            NodeId = model.NodeId;
            Enabled = model.Enabled;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public PublishedNodeModel ToServiceModel() {
            return new PublishedNodeModel {
                NodeId = NodeId,
                Enabled = Enabled
            };
        }

        /// <summary>
        /// Node
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Enabled or disabled 
        /// </summary>
        [JsonProperty(PropertyName = "enabled")]
        [Required]
        public bool Enabled { get; set; }
    }
}
