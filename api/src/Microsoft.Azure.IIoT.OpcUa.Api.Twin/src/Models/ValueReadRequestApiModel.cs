// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Node value read request webservice api model
    /// </summary>
    public class ValueReadRequestApiModel {

        /// <summary>
        /// Node to read from (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "nodeId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
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
        /// Index range to read, e.g. 1:2,0:1 for 2 slices
        /// out of a matrix or 0:1 for the first item in
        /// an array, string or bytestring.
        /// See 7.22 of part 4: NumericRange.
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
