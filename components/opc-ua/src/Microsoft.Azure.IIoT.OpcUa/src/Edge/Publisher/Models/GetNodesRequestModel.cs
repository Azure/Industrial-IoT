// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Request to get all published nodes on an endpoint
    /// </summary>
    public class GetNodesRequestModel {

        /// <summary>
        /// Endpoint url
        /// </summary>
        [JsonProperty(PropertyName = "EndpointUrl",
            NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Endpoint identifier on the publisher (optional)
        /// </summary>
        [JsonProperty(PropertyName = "EndpointId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointId { get; set; }

        /// <summary>
        /// Continuation token
        /// </summary>
        [JsonProperty(PropertyName = "ContinuationToken",
            NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken { get; set; }
    }
}
