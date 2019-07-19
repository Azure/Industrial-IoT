// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Math;

    /// <summary>
    /// Rsa param ex
    /// </summary>
    internal static class RsaParamsEx {

        /// <summary>
        /// Convert key parameters.
        /// </summary>
        internal static RsaKeyParameters ToRsaKeyParameters(this RsaParams rsaParams) {
            if (rsaParams.HasPrivateKey()) {
                return rsaParams.ToRsaPrivateCrtKeyParameters();
            }
            return new RsaKeyParameters(false,
                new BigInteger(1, rsaParams.N),
                new BigInteger(1, rsaParams.E));
        }

        /// <summary>
        /// Convert key parameters.
        /// </summary>
        internal static RsaParams ToRsaParams(this RsaKeyParameters rsaParams) {
            if (rsaParams is RsaPrivateCrtKeyParameters priv) {
                return priv.ToRsaParams();
            }
            return new RsaParams {
                E = rsaParams.Exponent?.ToByteArrayUnsigned(),
                N = rsaParams.Modulus?.ToByteArrayUnsigned()
            };
        }

        /// <summary>
        /// Convert key parameters.
        /// </summary>
        internal static RsaPrivateCrtKeyParameters ToRsaPrivateCrtKeyParameters(
            this RsaParams rsaParams) {
            return new RsaPrivateCrtKeyParameters(
                new BigInteger(1, rsaParams.N),
                new BigInteger(1, rsaParams.E),
                new BigInteger(1, rsaParams.D),
                new BigInteger(1, rsaParams.P),
                new BigInteger(1, rsaParams.Q),
                new BigInteger(1, rsaParams.DP),
                new BigInteger(1, rsaParams.DQ),
                new BigInteger(1, rsaParams.QI));
        }

        /// <summary>
        /// Convert key parameters.
        /// </summary>
        internal static RsaParams ToRsaParams(this RsaPrivateCrtKeyParameters rsaParams) {
            return new RsaParams {
                D = rsaParams.Exponent?.ToByteArrayUnsigned(),
                DP = rsaParams.DP?.ToByteArrayUnsigned(),
                DQ = rsaParams.DQ?.ToByteArrayUnsigned(),
                E = rsaParams.PublicExponent?.ToByteArrayUnsigned(),
                N = rsaParams.Modulus?.ToByteArrayUnsigned(),
                P = rsaParams.P?.ToByteArrayUnsigned(),
                Q = rsaParams.Q?.ToByteArrayUnsigned(),
                QI = rsaParams.QInv?.ToByteArrayUnsigned(),
                T = null
            };
        }
    }
}
