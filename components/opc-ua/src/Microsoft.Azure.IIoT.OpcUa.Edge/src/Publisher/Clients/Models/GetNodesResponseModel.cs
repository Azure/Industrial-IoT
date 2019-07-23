// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// All published nodes on an endpoint response
    /// </summary>
    public class GetNodesResponseModel {

        /// <summary>
        /// Nodes that are published
        /// </summary>
        [JsonProperty(PropertyName = "OpcNodes",
            NullValueHandling = NullValueHandling.Include)]
        public List<PublisherNodeModel> OpcNodes { get; set; }

        /// <summary>
        /// Continuation token
        /// </summary>
        [JsonProperty(PropertyName = "ContinuationToken",
            NullValueHandling = NullValueHandling.Include)]
        public ulong? ContinuationToken { get; set; }
    }
}
