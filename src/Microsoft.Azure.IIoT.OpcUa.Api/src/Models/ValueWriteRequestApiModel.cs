// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Value write request model for webservice api
    /// </summary>
    public class ValueWriteRequestApiModel {

        /// <summary>
        /// Node information to allow writing - from browse.
        /// </summary>
        [JsonProperty(PropertyName = "node")]
        public NodeApiModel Node { get; set; }

        /// <summary>
        /// Value to write
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
