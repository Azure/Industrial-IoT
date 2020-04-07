// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    /// <summary>
    /// Key type
    /// </summary>
    public enum PrivateKeyType {

        /// <summary>
        /// RSA key
        /// </summary>
        RSA,

        /// <summary>
        /// ECC key
        /// </summary>
        ECC,

        /// <summary>
        /// Symmetric AES key
        /// </summary>
        AES,
    }
}