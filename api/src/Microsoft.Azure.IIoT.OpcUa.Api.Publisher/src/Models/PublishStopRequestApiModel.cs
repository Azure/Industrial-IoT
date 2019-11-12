// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Unpublish request
    /// </summary>
    public class PublishStopRequestApiModel {

        /// <summary>
        /// Node of item to unpublish
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
