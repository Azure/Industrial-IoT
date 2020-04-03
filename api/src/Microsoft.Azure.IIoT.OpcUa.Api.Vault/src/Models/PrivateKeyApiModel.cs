// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Private key
    /// </summary>
    [DataContract]
    public sealed class PrivateKeyApiModel {

        /// <summary>
        /// Key type
        /// </summary>
        [DataMember(Name = "kty", Order = 0,
            EmitDefaultValue = false)]
        public PrivateKeyType Kty { get; set; }

        /// <summary>
        /// RSA modulus.
        /// </summary>
        [DataMember(Name = "n", Order = 1,
            EmitDefaultValue = false)]
        public byte[] N { get; set; }

        /// <summary>
        /// RSA public exponent, in Base64.
        /// </summary>
        [DataMember(Name = "e", Order = 2,
            EmitDefaultValue = false)]
        public byte[] E { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        [DataMember(Name = "dp", Order = 3,
            EmitDefaultValue = false)]
        public byte[] DP { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        [DataMember(Name = "dq", Order = 4,
            EmitDefaultValue = false)]
        public byte[] DQ { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        [DataMember(Name = "qi", Order = 5,
            EmitDefaultValue = false)]
        public byte[] QI { get; set; }

        /// <summary>
        /// RSA secret prime
        /// </summary>
        [DataMember(Name = "p", Order = 6,
            EmitDefaultValue = false)]
        public byte[] P { get; set; }

        /// <summary>
        /// RSA secret prime, with p &lt; q
        /// </summary>
        [DataMember(Name = "q", Order = 7,
            EmitDefaultValue = false)]
        public byte[] Q { get; set; }

        /// <summary>
        /// The curve for ECC algorithms
        /// </summary>
        [DataMember(Name = "crv", Order = 8,
            EmitDefaultValue = false)]
        public string CurveName { get; set; }

        /// <summary>
        /// X coordinate for the Elliptic Curve point.
        /// </summary>
        [DataMember(Name = "x", Order = 9,
            EmitDefaultValue = false)]
        public byte[] X { get; set; }

        /// <summary>
        /// Y coordinate for the Elliptic Curve point.
        /// </summary>
        [DataMember(Name = "y", Order = 10,
            EmitDefaultValue = false)]
        public byte[] Y { get; set; }

        /// <summary>
        /// RSA private exponent or ECC private key.
        /// </summary>
        [DataMember(Name = "d", Order = 11,
            EmitDefaultValue = false)]
        public byte[] D { get; set; }

        /// <summary>
        /// Symmetric key
        /// </summary>
        [DataMember(Name = "k", Order = 12,
            EmitDefaultValue = false)]
        public byte[] K { get; set; }

        /// <summary>
        /// HSM Token, used with "Bring Your Own Key"
        /// </summary>
        [DataMember(Name = "key_hsm", Order = 13,
            EmitDefaultValue = false)]
        public byte[] T { get; set; }
    }
}