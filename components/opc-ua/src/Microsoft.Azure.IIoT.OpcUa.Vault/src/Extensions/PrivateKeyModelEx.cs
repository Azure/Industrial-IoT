// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System;
    using Microsoft.Azure.IIoT.Crypto.Models;

    /// <summary>
    /// Key extensions
    /// </summary>
    public static class PrivateKeyModelEx {

        /// <summary>
        /// Convert to key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Key ToKey(this PrivateKeyModel key) {
            if (key == null) {
                return null;
            }
            switch (key.Kty) {
                case PrivateKeyType.RSA:
                    return new Key {
                        Type = KeyType.RSA,
                        Parameters = new RsaParams {
                            D = key.D,
                            DP = key.DP,
                            DQ = key.DQ,
                            E = key.E,
                            N = key.N,
                            P = key.P,
                            Q = key.Q,
                            QI = key.QI,
                            T = key.T
                        }
                    };
                case PrivateKeyType.ECC:
                    return new Key {
                        Type = KeyType.ECC,
                        Parameters = new EccParams {
                            D = key.D,
                            Curve = FromWebKeyModelCurveName(key.CurveName),
                            X = key.X,
                            Y = key.Y,
                            T = key.T
                        }
                    };
                case PrivateKeyType.AES:
                    return new Key {
                        Type = KeyType.AES,
                        Parameters = new AesParams {
                            K = key.K,
                            T = key.T
                        }
                    };
                default:
                    throw new NotSupportedException($"{key.Kty} is unknown");
            }
        }

        /// <summary>
        /// Convert to json web key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static PrivateKeyModel ToServiceModel(this Key key) {
            if (key == null) {
                return null;
            }
            switch (key.Type) {
                case KeyType.RSA:
                    var rsa = key.Parameters as RsaParams;
                    return new PrivateKeyModel {
                        Kty = PrivateKeyType.RSA,
                        D = rsa.D,
                        DP = rsa.DP,
                        DQ = rsa.DQ,
                        E = rsa.E,
                        N = rsa.N,
                        P = rsa.P,
                        Q = rsa.Q,
                        QI = rsa.QI,
                        T = rsa.T
                    };
                case KeyType.ECC:
                    var ecc = key.Parameters as EccParams;
                    return new PrivateKeyModel {
                        Kty = PrivateKeyType.ECC,
                        D = ecc.D,
                        CurveName = ToWebKeyModelCurveName(ecc.Curve),
                        X = ecc.X,
                        Y = ecc.Y,
                        T = ecc.T
                    };
                case KeyType.AES:
                    var aes = key.Parameters as AesParams;
                    return new PrivateKeyModel {
                        Kty =
                            PrivateKeyType.AES,
                        K = aes.K,
                        T = aes.T
                    };
                default:
                    throw new NotSupportedException($"{key.Type} is unknown");
            }
        }

        /// <summary>
        /// Convert to kty
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static PrivateKeyType ToKty(this KeyType type) {
            switch (type) {
                case KeyType.RSA:
                    return PrivateKeyType.RSA;
                case KeyType.ECC:
                    return PrivateKeyType.ECC;
                case KeyType.AES:
                    return PrivateKeyType.AES;
                default:
                    throw new NotSupportedException($"{type} is unknown");
            }
        }

        /// <summary>
        /// Convert to curve name
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        private static string ToWebKeyModelCurveName(CurveType curve) {
            switch (curve) {
                case CurveType.P256:
                    return "P-256";
                case CurveType.P384:
                    return "P-384";
                case CurveType.P521:
                    return "P-521";
                case CurveType.P256K:
                    return "P-256K";
                case CurveType.Brainpool_P160r1:
                case CurveType.Brainpool_P160t1:
                case CurveType.Brainpool_P192r1:
                case CurveType.Brainpool_P192t1:
                case CurveType.Brainpool_P224r1:
                case CurveType.Brainpool_P224t1:
                case CurveType.Brainpool_P256r1:
                case CurveType.Brainpool_P256t1:
                case CurveType.Brainpool_P320r1:
                case CurveType.Brainpool_P320t1:
                case CurveType.Brainpool_P384r1:
                case CurveType.Brainpool_P384t1:
                case CurveType.Brainpool_P512r1:
                case CurveType.Brainpool_P512t1:
                    throw new NotSupportedException("Curve not supported");
                default:
                    throw new ArgumentException(nameof(curve));
            }
        }

        /// <summary>
        /// Convert to curve type
        /// </summary>
        /// <param name="curveName"></param>
        /// <returns></returns>
        private static CurveType FromWebKeyModelCurveName(string curveName) {
            switch (curveName) {
                case "P-256":
                    return CurveType.P256;
                case "P-384":
                    return CurveType.P384;
                case "P-521":
                    return CurveType.P521;
                case "P-256K":
                    return CurveType.P256K;
                default:
                    throw new ArgumentException(nameof(curveName));
            }
        }
    }
}
