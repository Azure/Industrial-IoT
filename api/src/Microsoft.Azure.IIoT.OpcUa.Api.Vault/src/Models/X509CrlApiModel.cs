// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// A X509 certificate revocation list.
    /// </summary>
    [DataContract]
    public sealed class X509CrlApiModel {

        /// <summary>
        /// The Issuer name of the revocation list.
        /// </summary>
        [DataMember(Name = "issuer", Order = 0,
            EmitDefaultValue = false)]
        public string Issuer { get; set; }

        /// <summary>
        /// The certificate revocation list.
        /// </summary>
        [DataMember(Name = "crl", Order = 1)]
        public VariantValue Crl { get; set; }
    }
}
