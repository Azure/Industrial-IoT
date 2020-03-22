// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Signature algorithm
    /// </summary>
    [DataContract]
    public enum SignatureAlgorithm {

        /// <summary>
        /// Rsa 256
        /// </summary>
        [EnumMember]
        Rsa256,

        /// <summary>
        /// Rsa 384
        /// </summary>
        [EnumMember]
        Rsa384,

        /// <summary>
        /// Rsa 512
        /// </summary>
        [EnumMember]
        Rsa512,

        /// <summary>
        /// 256 with padding
        /// </summary>
        [EnumMember]
        Rsa256Pss,

        /// <summary>
        /// 384 with padding
        /// </summary>
        [EnumMember]
        Rsa384Pss,

        /// <summary>
        /// 512 with padding
        /// </summary>
        [EnumMember]
        Rsa512Pss,
    }
}
