// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Value write request model
    /// </summary>
    public class ValueWriteRequestApiModel {

        /// <summary>
        /// Node id to to write value to.
        /// </summary>
        [JsonProperty(PropertyName = "nodeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// the actual node.
        /// </summary>
        [JsonProperty(PropertyName = "browsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Value to write. The system tries to convert
        /// the value according to the data type value,
        /// e.g. convert comma seperated value strings
        /// into arrays.  (Mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        [Required]
        public JToken Value { get; set; }

        /// <summary>
        /// A built in datatype for the value. This can
        /// be a data type from browse, or a built in
        /// type.
        /// (default: best effort)
        /// </summary>
        [JsonProperty(PropertyName = "dataType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; set; }

        /// <summary>
        /// Index range to write
        /// </summary>
        [JsonProperty(PropertyName = "indexRange",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
