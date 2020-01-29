// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Browse nodes by path
    /// </summary>
    public class BrowsePathRequestApiModel {

        /// <summary>
        /// Node to browse from.
        /// (defaults to root folder).
        /// </summary>
        [JsonProperty(PropertyName = "nodeId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string NodeId { get; set; }

        /// <summary>
        /// The paths to browse from node.
        /// (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "browsePaths")]
        [Required]
        public List<string[]> BrowsePaths { get; set; }

        /// <summary>
        /// Whether to read variable values on target nodes.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "readVariableValues",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? ReadVariableValues { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
