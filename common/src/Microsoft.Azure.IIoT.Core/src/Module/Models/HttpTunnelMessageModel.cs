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
    public class HttpTunnelMessageModel {

        /// <summary>
        /// Correlation id
        /// </summary>
        [JsonProperty(PropertyName = "correlationId")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Method
        /// </summary>
        [JsonProperty(PropertyName = "method",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Method { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        [JsonProperty(PropertyName = "headers",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, List<string>> Headers { get; set; }

        /// <summary>
        /// Payload chunk or null for upload responses and
        /// response continuation requests.
        /// </summary>
        [JsonProperty(PropertyName = "payload",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Payload { get; set; }

        /// <summary>
        /// Status code of call - in first response chunk.
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public int Status { get; set; }
    }
}
