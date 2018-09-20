// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Value write request model
    /// </summary>
    public class ValueWriteRequestApiModel {

        /// <summary>
        /// Node id to to write value to. (Mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Value to write. The system tries to convert
        /// the value according to the data type value,
        /// e.g. convert comma seperated value strings
        /// into arrays.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
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
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
