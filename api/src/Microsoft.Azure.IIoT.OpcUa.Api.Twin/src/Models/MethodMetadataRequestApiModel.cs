// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Method metadata request model
    /// </summary>
    public class MethodMetadataRequestApiModel {

        /// <summary>
        /// Method id of method to call.
        /// (Required)
        /// </summary>
        [JsonProperty(PropertyName = "methodId")]
        public string MethodId { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// MethodId to the actual method node.  
        /// </summary>
        [JsonProperty(PropertyName = "methodBrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] MethodBrowsePath { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
