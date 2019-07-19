// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.KeyVault.WebKey {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;

    /// <summary>
    /// Signature type extensions
    /// </summary>
    public static class SignatureTypeEx {

        /// <summary>
        /// Convert to algorithm
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToJsonWebKeySignatureAlgorithm(this SignatureType type) {
            switch (type) {
                case SignatureType.RS256:
                    return JsonWebKeySignatureAlgorithm.RS256;
                case SignatureType.RS384:
                    return JsonWebKeySignatureAlgorithm.RS384;
                case SignatureType.RS512:
                    return JsonWebKeySignatureAlgorithm.RS512;
                case SignatureType.PS256:
                    return JsonWebKeySignatureAlgorithm.PS256;
                case SignatureType.PS384:
                    return JsonWebKeySignatureAlgorithm.PS384;
                case SignatureType.PS512:
                    return JsonWebKeySignatureAlgorithm.PS512;
                case SignatureType.ES256:
                    return JsonWebKeySignatureAlgorithm.ES256;
                case SignatureType.ES384:
                    return JsonWebKeySignatureAlgorithm.ES384;
                case SignatureType.ES512:
                    return JsonWebKeySignatureAlgorithm.ES512;
                case SignatureType.ES256K:
                    return JsonWebKeySignatureAlgorithm.ES256K;
                default:
                    throw new NotSupportedException("Unknown signature algorithm");
            }
        }
    }
}
