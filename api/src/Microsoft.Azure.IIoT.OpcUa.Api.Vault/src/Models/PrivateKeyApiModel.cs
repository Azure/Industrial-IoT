// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Private key
    /// </summary>
    public sealed class PrivateKeyApiModel {

        /// <summary>
        /// Key type
        /// </summary>
        [JsonProperty(PropertyName = "kty",
            NullValueHandling = NullValueHandling.Ignore)]
        public PrivateKeyType Kty { get; set; }

        /// <summary>
        /// RSA modulus.
        /// </summary>
        [JsonProperty(PropertyName = "n",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] N { get; set; }

        /// <summary>
        /// RSA public exponent, in Base64.
        /// </summary>
        [JsonProperty(PropertyName = "e",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] E { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        [JsonProperty(PropertyName = "dp",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] DP { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        [JsonProperty(PropertyName = "dq",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] DQ { get; set; }

        /// <summary>
        /// RSA Private Key Parameter
        /// </summary>
        [JsonProperty(PropertyName = "qi",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] QI { get; set; }

        /// <summary>
        /// RSA secret prime
        /// </summary>
        [JsonProperty(PropertyName = "p",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] P { get; set; }

        /// <summary>
        /// RSA secret prime, with p &lt; q
        /// </summary>
        [JsonProperty(PropertyName = "q",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Q { get; set; }

        /// <summary>
        /// The curve for ECC algorithms
        /// </summary>
        [JsonProperty(PropertyName = "crv",
            NullValueHandling = NullValueHandling.Ignore)]
        public string CurveName { get; set; }

        /// <summary>
        /// X coordinate for the Elliptic Curve point.
        /// </summary>
        [JsonProperty(PropertyName = "x",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] X { get; set; }

        /// <summary>
        /// Y coordinate for the Elliptic Curve point.
        /// </summary>
        [JsonProperty(PropertyName = "y",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Y { get; set; }

        /// <summary>
        /// RSA private exponent or ECC private key.
        /// </summary>
        [JsonProperty(PropertyName = "d",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] D { get; set; }

        /// <summary>
        /// Symmetric key
        /// </summary>
        [JsonProperty(PropertyName = "k",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] K { get; set; }

        /// <summary>
        /// HSM Token, used with "Bring Your Own Key"
        /// </summary>
        [JsonProperty(PropertyName = "key_hsm",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] T { get; set; }
    }
}