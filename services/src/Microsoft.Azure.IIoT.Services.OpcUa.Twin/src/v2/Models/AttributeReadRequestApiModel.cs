// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Attribute to read
    /// </summary>
    public class AttributeReadRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public AttributeReadRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public AttributeReadRequestApiModel(AttributeReadRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            Attribute = model.Attribute;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public AttributeReadRequestModel ToServiceModel() {
            return new AttributeReadRequestModel {
                NodeId = NodeId,
                Attribute = Attribute
            };
        }

        /// <summary>
        /// Node to read from or write to (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Attribute to read or write
        /// </summary>
        [JsonProperty(PropertyName = "attribute")]
        [Required]
        public NodeAttribute Attribute { get; set; }
    }
}
