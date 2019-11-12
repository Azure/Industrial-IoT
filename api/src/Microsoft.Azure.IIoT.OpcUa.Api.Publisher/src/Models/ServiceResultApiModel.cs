// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Service result
    /// </summary>
    public class ServiceResultApiModel {

        /// <summary>
        /// Error code - if null operation succeeded.
        /// </summary>
        [JsonProperty(PropertyName = "statusCode",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? StatusCode { get; set; }

        /// <summary>
        /// Error message in case of error or null.
        /// </summary>
        [JsonProperty(PropertyName = "errorMessage",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Additional diagnostics information
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }
    }
}
