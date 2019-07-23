// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Trust group registration model
    /// </summary>
    public sealed class TrustGroupRegistrationApiModel {

        /// <summary>
        /// The registered id of the trust group
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Trust group
        /// </summary>
        [JsonProperty(PropertyName = "group")]
        public TrustGroupApiModel Group { get; set; }
    }
}
