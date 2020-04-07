// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    /// <summary>
    /// Signature algorithm
    /// </summary>
    public enum SignatureAlgorithm {

        /// <summary>
        /// Rsa 256
        /// </summary>
        Rsa256,

        /// <summary>
        /// Rsa 384
        /// </summary>
        Rsa384,

        /// <summary>
        /// Rsa 512
        /// </summary>
        Rsa512,

        /// <summary>
        /// 256 with padding
        /// </summary>
        Rsa256Pss,

        /// <summary>
        /// 384 with padding
        /// </summary>
        Rsa384Pss,

        /// <summary>
        /// 512 with padding
        /// </summary>
        Rsa512Pss,
    }
}