// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// Signing request
    /// </summary>
    [DataContract]
    public sealed class StartSigningRequestApiModel {

        /// <summary>
        /// Id of entity to sign a certificate for
        /// </summary>
        [DataMember(Name = "entityId", Order = 0)]
        public string EntityId { get; set; }

        /// <summary>
        /// Certificate group id
        /// </summary>
        [DataMember(Name = "groupId", Order = 1)]
        public string GroupId { get; set; }

        /// <summary>
        /// Request
        /// </summary>
        [DataMember(Name = "certificateRequest", Order = 2)]
        public VariantValue CertificateRequest { get; set; }
    }
}
