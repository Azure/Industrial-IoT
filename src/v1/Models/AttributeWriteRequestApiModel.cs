// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Attribute and value to write to it
    /// </summary>
    public class AttributeWriteRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public AttributeWriteRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public AttributeWriteRequestApiModel(AttributeWriteRequestModel model) {
            NodeId = model.NodeId;
            Value = model.Value;
            Attribute = model.Attribute;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public AttributeWriteRequestModel ToServiceModel() {
            return new AttributeWriteRequestModel {
                NodeId = NodeId,
                Value = Value,
                Attribute = Attribute
            };
        }

        /// <summary>
        /// Node to write to (mandatory)
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Attribute to write (mandatory)
        /// </summary>
        public NodeAttribute Attribute { get; set; }

        /// <summary>
        /// Value to write (mandatory)
        /// </summary>
        public JToken Value { get; set; }
    }
}
