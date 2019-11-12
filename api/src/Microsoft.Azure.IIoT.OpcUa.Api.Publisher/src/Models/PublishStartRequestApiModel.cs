// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Publish request
    /// </summary>
    public class PublishStartRequestApiModel {

        /// <summary>
        /// Item to publish
        /// </summary>
        [JsonProperty(PropertyName = "item")]
        public PublishedItemApiModel Item { get; set; }

        /// <summary>
        /// Optional header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
