// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Discovery cancel request
    /// </summary>
    public class DiscoveryCancelApiModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
    }
}
