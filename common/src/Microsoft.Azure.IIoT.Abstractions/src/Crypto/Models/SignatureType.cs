// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {

    /// <summary>
    /// Signature algorithms
    /// </summary>
    public enum SignatureType {

        /// <summary>
        /// Rsa 256
        /// </summary>
        RS256,

        /// <summary>
        /// Rsa 384
        /// </summary>
        RS384,

        /// <summary>
        /// Rsa 512
        /// </summary>
        RS512,

        /// <summary>
        /// 256 with padding
        /// </summary>
        PS256,

        /// <summary>
        /// 384 with padding
        /// </summary>
        PS384,

        /// <summary>
        /// 512 with padding
        /// </summary>
        PS512,

        /// <summary>
        /// Ecc 256
        /// </summary>
        ES256,

        /// <summary>
        /// Ecc 384
        /// </summary>
        ES384,

        /// <summary>
        /// Ecc 512
        /// </summary>
        ES512,

        /// <summary>
        /// Ecc 256k
        /// </summary>
        ES256K,
    }
}