// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Trust group registration collection model
    /// </summary>
    public sealed class TrustGroupRegistrationListApiModel {

        /// <summary>
        /// Group registrations
        /// </summary>
        [JsonProperty(PropertyName = "registrations")]
        public List<TrustGroupRegistrationApiModel> Registrations { get; set; }

        /// <summary>
        /// Next link
        /// </summary>
        [JsonProperty(PropertyName = "nextPageLink",
            NullValueHandling = NullValueHandling.Ignore)]
        public string NextPageLink { get; set; }
    }
}
