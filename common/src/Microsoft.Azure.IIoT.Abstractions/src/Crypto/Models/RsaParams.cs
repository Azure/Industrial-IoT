// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {

    /// <summary>
    /// Rsa parameters
    /// </summary>
    public class RsaParams : KeyParams {

        /// <summary>
        /// Represents the D parameter.
        /// </summary>
        public byte[] D { get; set; }

        /// <summary>
        /// Represents the DP parameter.
        /// </summary>
        public byte[] DP { get; set; }

        /// <summary>
        /// Represents the DQ parameter.
        /// </summary>
        public byte[] DQ { get; set; }

        /// <summary>
        /// Represents the Exponent parameter.
        /// </summary>
        public byte[] E { get; set; }

        /// <summary>
        /// Represents the InverseQ parameter (QI).
        /// </summary>
        public byte[] QI { get; set; }

        /// <summary>
        /// Represents the Modulus parameter (N).
        /// </summary>
        public byte[] N { get; set; }

        /// <summary>
        /// Represents the RSA secret prime (P).
        /// </summary>
        public byte[] P { get; set; }

        /// <summary>
        /// Represents the RSA secret prime, with p &lt; q.
        /// </summary>
        public byte[] Q { get; set; }
    }
}