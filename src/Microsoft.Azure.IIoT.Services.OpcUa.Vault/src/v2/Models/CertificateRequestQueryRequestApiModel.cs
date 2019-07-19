// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Certificate request query model
    /// </summary>
    public sealed class CertificateRequestQueryRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public CertificateRequestQueryRequestApiModel() {
        }

        /// <summary>
        /// Create query
        /// </summary>
        /// <param name="model"></param>
        public CertificateRequestQueryRequestApiModel(CertificateRequestQueryRequestModel model) {
            EntityId = model.EntityId;
            State = model.State;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public CertificateRequestQueryRequestModel ToServiceModel() {
            return new CertificateRequestQueryRequestModel {
                EntityId = EntityId,
                State = State
            };
        }

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
