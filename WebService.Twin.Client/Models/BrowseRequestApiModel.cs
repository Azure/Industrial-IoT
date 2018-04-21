// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// browse request model for webservice api
    /// </summary>
    public class BrowseRequestApiModel {

        /// <summary>
        /// Node to browse, or null for root.
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// If not set, implies false
        /// </summary>
        [JsonProperty(PropertyName = "excludeReferences",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? ExcludeReferences { get; set; }

        /// <summary>
        /// If not set, implies false
        /// </summary>
        [JsonProperty(PropertyName = "includePublishing",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IncludePublishingStatus { get; set; }

        /// <summary>
        /// Optional parent node to include in node result
        /// </summary>
        [JsonProperty(PropertyName = "parentId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Parent { get; set; }
    }
}
