// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// reference model for webservice api
    /// </summary>
    public class NodeReferenceApiModel {

        /// <summary>
        /// Reference Type id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Browse name of reference
        /// </summary>
        [JsonProperty(PropertyName = "browseName")]
        public string BrowseName { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "target")]
        public NodeApiModel Target { get; set; }

        /// <summary>
        /// Display name of reference
        /// </summary>
        [JsonProperty(PropertyName = "text",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }
}
