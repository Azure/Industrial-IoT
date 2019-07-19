// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Trust group identifier list model
    /// </summary>
    public sealed class TrustGroupListApiModel {

        /// <summary>
        /// Groups
        /// </summary>
        [JsonProperty(PropertyName = "groups",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Groups { get; set; }

        /// <summary>
        /// Next link
        /// </summary>
        [JsonProperty(PropertyName = "nextPageLink",
            NullValueHandling = NullValueHandling.Ignore)]
        public string NextPageLink { get; set; }
    }
}
