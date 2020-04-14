// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Trust group types
    /// </summary>
    [DataContract]
    public enum TrustGroupType {

        /// <summary>
        /// Application certificate
        /// </summary>
        [EnumMember]
        ApplicationInstanceCertificate,

        /// <summary>
        /// Https certificate type
        /// </summary>
        [EnumMember]
        HttpsCertificate,

        /// <summary>
        /// User credential certificate type
        /// </summary>
        [EnumMember]
        UserCredentialCertificate
    }
}
