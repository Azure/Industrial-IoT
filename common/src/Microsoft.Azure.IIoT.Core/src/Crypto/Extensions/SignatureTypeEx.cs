// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Signature type extensions
    /// </summary>
    public static class SignatureTypeEx {

        /// <summary>
        /// Get signature type
        /// </summary>
        /// <param name="hashAlgorithm"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static SignatureType ToSignatureType(this HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding = null) {
            if (padding == RSASignaturePadding.Pkcs1) {
                if (hashAlgorithm == HashAlgorithmName.SHA256) {
                    return SignatureType.RS256;
                }
                if (hashAlgorithm == HashAlgorithmName.SHA384) {
                    return SignatureType.RS384;
                }
                if (hashAlgorithm == HashAlgorithmName.SHA512) {
                    return SignatureType.RS512;
                }
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
            }
            if (padding == RSASignaturePadding.Pss) {
                if (hashAlgorithm == HashAlgorithmName.SHA256) {
                    return SignatureType.PS256;
                }
                if (hashAlgorithm == HashAlgorithmName.SHA384) {
                    return SignatureType.PS384;
                }
                if (hashAlgorithm == HashAlgorithmName.SHA512) {
                    return SignatureType.PS512;
                }
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
            }
            if (padding == null) {
                if (hashAlgorithm == HashAlgorithmName.SHA256) {
                    return SignatureType.ES256;
                }
                if (hashAlgorithm == HashAlgorithmName.SHA384) {
                    return SignatureType.ES384;
                }
                if (hashAlgorithm == HashAlgorithmName.SHA512) {
                    return SignatureType.ES512;
                }
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
            }
            throw new ArgumentOutOfRangeException(nameof(padding));
        }

        /// <summary>
        /// Get padding
        /// </summary>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public static RSASignaturePadding ToRSASignaturePadding(this SignatureType algorithm) {
            switch (algorithm) {
                case SignatureType.RS256:
                case SignatureType.RS384:
                case SignatureType.RS512:
                    return RSASignaturePadding.Pkcs1;
                case SignatureType.PS256:
                case SignatureType.PS384:
                case SignatureType.PS512:
                    return RSASignaturePadding.Pss;
                default:
                    throw new NotSupportedException("Algorithm not supported");
            }
        }

        /// <summary>
        /// Is RSA algorithm
        /// </summary>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public static bool IsRSA(this SignatureType algorithm) {
            switch (algorithm) {
                case SignatureType.RS256:
                case SignatureType.RS384:
                case SignatureType.RS512:
                case SignatureType.PS256:
                case SignatureType.PS384:
                case SignatureType.PS512:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is ECC algorithm
        /// </summary>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public static bool IsECC(this SignatureType algorithm) {
            switch (algorithm) {
                case SignatureType.ES256:
                case SignatureType.ES384:
                case SignatureType.ES512:
                case SignatureType.ES256K:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get hash algorithm name and padding
        /// </summary>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public static HashAlgorithmName ToHashAlgorithmName(this SignatureType algorithm) {
            switch (algorithm) {
                case SignatureType.RS256:
                case SignatureType.ES256:
                case SignatureType.PS256:
                case SignatureType.ES256K:
                    return HashAlgorithmName.SHA256;
                case SignatureType.RS384:
                case SignatureType.ES384:
                case SignatureType.PS384:
                    return HashAlgorithmName.SHA384;
                case SignatureType.RS512:
                case SignatureType.PS512:
                case SignatureType.ES512:
                    return HashAlgorithmName.SHA512;
                default:
                    throw new NotSupportedException("Algorithm not supported");
            }
        }
    }
}

