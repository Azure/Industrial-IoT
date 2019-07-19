// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Value write request model for module
    /// </summary>
    public class ValueWriteRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ValueWriteRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ValueWriteRequestApiModel(ValueWriteRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            BrowsePath = model.BrowsePath;
            DataType = model.DataType;
            IndexRange = model.IndexRange;
            Value = model.Value;
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ValueWriteRequestModel ToServiceModel() {
            return new ValueWriteRequestModel {
                NodeId = NodeId,
                BrowsePath = BrowsePath,
                DataType = DataType,
                IndexRange = IndexRange,
                Value = Value,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node id to to write value to.
        /// </summary>
        [JsonProperty(PropertyName = "NodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// the actual node.
        /// </summary>
        [JsonProperty(PropertyName = "BrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Value to write. The system tries to convert
        /// the value according to the data type value,
        /// e.g. convert comma seperated value strings
        /// into arrays.  (Mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "Value")]
        public JToken Value { get; set; }

        /// <summary>
        /// A built in datatype for the value. This can
        /// be a data type from browse, or a built in
        /// type.
        /// (default: best effort)
        /// </summary>
        [JsonProperty(PropertyName = "DataType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; set; }

        /// <summary>
        /// Index range to write
        /// </summary>
        [JsonProperty(PropertyName = "IndexRange",
            NullValueHandling = NullValueHandling.Ignore)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "Header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
