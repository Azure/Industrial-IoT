// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Filter operand
    /// </summary>
    public class FilterOperandApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public FilterOperandApiModel() {
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public FilterOperandApiModel(FilterOperandModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            AttributeId = model.AttributeId;
            BrowsePath = model.BrowsePath;
            IndexRange = model.IndexRange;
            Index = model.Index;
            Alias = model.Alias;
            Value = model.Value;
            NodeId = model.NodeId;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public FilterOperandModel ToServiceModel() {
            return new FilterOperandModel {
                Index = Index,
                Alias = Alias,
                Value = Value,
                NodeId = NodeId,
                AttributeId = AttributeId,
                BrowsePath = BrowsePath,
                IndexRange = IndexRange
            };
        }

        /// <summary>
        /// Element reference in the outer list if
        /// operand is an element operand
        /// </summary>
        [JsonProperty(PropertyName = "index",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? Index { get; set; }

        /// <summary>
        /// Variant value if operand is a literal
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Value { get; set; }

        /// <summary>
        /// Type definition node id if operand is
        /// simple or full attribute operand.
        /// </summary>
        [JsonProperty(PropertyName = "nodeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string NodeId { get; set; }

        /// <summary>
        /// Browse path of attribute operand
        /// </summary>
        [JsonProperty(PropertyName = "browsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Attribute id
        /// </summary>
        [JsonProperty(PropertyName = "attributeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAttribute? AttributeId { get; set; }

        /// <summary>
        /// Index range of attribute operand
        /// </summary>
        [JsonProperty(PropertyName = "indexRange",
            NullValueHandling = NullValueHandling.Ignore)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional alias to refer to it makeing it a
        /// full blown attribute operand
        /// </summary>
        [JsonProperty(PropertyName = "alias",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Alias { get; set; }
    }
}