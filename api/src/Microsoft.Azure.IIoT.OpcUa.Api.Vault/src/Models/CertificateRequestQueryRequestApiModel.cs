// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Certificate request query model
    /// </summary>
    public sealed class CertificateRequestQueryRequestApiModel {

        /// <summary>
        /// The entity id to filter with
        /// </summary>
        [JsonProperty(PropertyName = "entityId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string EntityId { get; set; }

        /// <summary>
        /// The certificate request state
        /// </summary>
        [JsonProperty(PropertyName = "state",
            NullValueHandling = NullValueHandling.Ignore)]
        public CertificateRequestState? State { get; set; }
    }
}
