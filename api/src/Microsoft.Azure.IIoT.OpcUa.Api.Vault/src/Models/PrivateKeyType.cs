// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Key type
    /// </summary>
    [DataContract]
    public enum PrivateKeyType {

        /// <summary>
        /// RSA key
        /// </summary>
        [EnumMember]
        RSA,

        /// <summary>
        /// ECC key
        /// </summary>
        [EnumMember]
        ECC,

        /// <summary>
        /// Symmetric AES key
        /// </summary>
        [EnumMember]
        AES,
    }
}