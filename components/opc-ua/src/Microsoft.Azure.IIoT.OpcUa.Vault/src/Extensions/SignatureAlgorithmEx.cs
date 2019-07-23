// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System;

    /// <summary>
    /// Signature algorithm extensions
    /// </summary>
    public static class SignatureAlgorithmEx {

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public static SignatureType ToSignatureType(this SignatureAlgorithm algorithm) {
            switch (algorithm) {
                case SignatureAlgorithm.Rsa256:
                    return SignatureType.RS256;
                case SignatureAlgorithm.Rsa384:
                    return SignatureType.RS384;
                case SignatureAlgorithm.Rsa512:
                    return SignatureType.RS512;
                case SignatureAlgorithm.Rsa256Pss:
                    return SignatureType.PS256;
                case SignatureAlgorithm.Rsa384Pss:
                    return SignatureType.PS384;
                case SignatureAlgorithm.Rsa512Pss:
                    return SignatureType.PS512;
                default:
                    throw new ArgumentException("Unknown algorithm", nameof(algorithm));
            }
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="signatureType"></param>
        /// <returns></returns>
        public static SignatureAlgorithm ToSignatureAlgorithm(this SignatureType signatureType) {
            switch (signatureType) {
                case SignatureType.RS256:
                    return SignatureAlgorithm.Rsa256;
                case SignatureType.RS384:
                    return SignatureAlgorithm.Rsa384;
                case SignatureType.RS512:
                    return SignatureAlgorithm.Rsa512;
                case SignatureType.PS256:
                    return SignatureAlgorithm.Rsa256Pss;
                case SignatureType.PS384:
                    return SignatureAlgorithm.Rsa384Pss;
                case SignatureType.PS512:
                    return SignatureAlgorithm.Rsa512Pss;
                case SignatureType.ES256:
                case SignatureType.ES384:
                case SignatureType.ES512:
                case SignatureType.ES256K:
                    throw new NotSupportedException("Ecc not yet supported");
                default:
                    throw new ArgumentException("Unknown signature type",
                        nameof(signatureType));
            }
        }
    }
}
