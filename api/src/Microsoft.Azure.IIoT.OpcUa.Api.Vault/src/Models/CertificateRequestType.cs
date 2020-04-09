// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// The certificate request type.
    /// </summary>
    [DataContract]
    public enum CertificateRequestType {

        /// <summary>
        /// Signing request
        /// </summary>
        [EnumMember]
        SigningRequest,

        /// <summary>
        /// Key pair request
        /// </summary>
        [EnumMember]
        KeyPairRequest,
    }
}
