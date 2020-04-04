// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Certificate request query model
    /// </summary>
    [DataContract]
    public sealed class CertificateRequestQueryRequestApiModel {

        /// <summary>
        /// The entity id to filter with
        /// </summary>
        [DataMember(Name = "entityId", Order = 0,
            EmitDefaultValue = false)]
        public string EntityId { get; set; }

        /// <summary>
        /// The certificate request state
        /// </summary>
        [DataMember(Name = "state", Order = 1,
            EmitDefaultValue = false)]
        public CertificateRequestState? State { get; set; }
    }
}
