// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Tunneled message
    /// </summary>
    public class HttpTunnelRequestModel {

        /// <summary>
        /// Message contains request
        /// </summary>
        public const string SchemaName =
            "application/x-http-tunnel-request-v1";

        /// <summary>
        /// Method
        /// </summary>
        [JsonProperty(PropertyName = "method")]
        public string Method { get; set; }

        /// <summary>
        /// Resource id
        /// </summary>
        [JsonProperty(PropertyName = "resourceId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ResourceId { get; internal set; }

        /// <summary>
        /// Uri to call
        /// </summary>
        [JsonProperty(PropertyName = "uri")]
        public string Uri { get; internal set; }

        /// <summary>
        /// Headers
        /// </summary>
        [JsonProperty(PropertyName = "requestHeaders",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, List<string>> RequestHeaders { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        [JsonProperty(PropertyName = "contentHeaders",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, List<string>> ContentHeaders { get; set; }
    }
}
