// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Twin service method results model
    /// </summary>
    public class MethodResultModel {

        /// <summary>
        /// Status
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }

        /// <summary>
        /// Response payload
        /// </summary>
        [JsonProperty(PropertyName = "jsonPayload")]
        public string JsonPayload { get; set; }
    }
}
