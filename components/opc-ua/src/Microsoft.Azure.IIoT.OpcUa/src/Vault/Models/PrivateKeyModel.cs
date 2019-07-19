// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    /// <summary>
    /// Private key model - follows Json Web key spec
    /// </summary>
    public sealed class PrivateKeyModel {

        /// <summary>
        /// Private key type
        /// </summary>
        public PrivateKeyType Kty { get; set; }

        /// <summary>
        /// RSA modulus, in Base64.
        /// </summary>
        public byte[] N { get; set; }

        /// <summary>
        /// RSA public exponent, in Base64.
        /// </summary>
        public byte[] E { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        public byte[] DP { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        public byte[] DQ { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        public byte[] QI { get; set; }

        /// <summary>
        /// RSA secret prime
        /// </summary>
        public byte[] P { get; set; }

        /// <summary>
        /// RSA secret prime, with p &lt; q
        /// </summary>
        public byte[] Q { get; set; }

        /// <summary>
        /// The curve for ECC algorithms
        /// </summary>
        public string CurveName { get; set; }

        /// <summary>
        /// X coordinate for the Elliptic Curve point.
        /// </summary>
        public byte[] X { get; set; }

        /// <summary>
        /// Y coordinate for the Elliptic Curve point.
        /// </summary>
        public byte[] Y { get; set; }

        /// <summary>
        /// RSA private exponent or ECC private key.
        /// </summary>
        public byte[] D { get; set; }

        /// <summary>
        /// Symmetric key
        /// </summary>
        public byte[] K { get; set; }

        /// <summary>
        /// HSM Token, used with "Bring Your Own Key"
        /// </summary>
        public byte[] T { get; set; }
    }
}

