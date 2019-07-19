// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.KeyVault.WebKey {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;

    /// <summary>
    /// Key extensions
    /// </summary>
    public static class KeyEx {

        /// <summary>
        /// Convert to key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Key ToKey(this JsonWebKey key) {
            switch (key.Kty) {
                case JsonWebKeyType.Rsa:
                case JsonWebKeyType.RsaHsm:
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
                case JsonWebKeyType.EllipticCurve:
                case JsonWebKeyType.EllipticCurveHsm:
                    return new Key {
                        Type = KeyType.ECC,
                        Parameters = new EccParams {
                            D = key.D,
                            Curve = FromJsonWebKeyCurveName(key.CurveName),
                            X = key.X,
                            Y = key.Y,
                            T = key.T
                        }
                    };
                case JsonWebKeyType.Octet:
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
        public static JsonWebKey ToJsonWebKey(this Key key) {
            switch (key.Type) {
                case KeyType.RSA:
                    var rsa = key.Parameters as RsaParams;
                    return new JsonWebKey {
                        Kty = rsa.T == null ?
                            JsonWebKeyType.Rsa : JsonWebKeyType.RsaHsm,
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
                    return new JsonWebKey {
                        Kty = ecc.T == null ?
                            JsonWebKeyType.EllipticCurve : JsonWebKeyType.EllipticCurveHsm,
                        D = ecc.D,
                        CurveName = ToJsonWebKeyCurveName(ecc.Curve),
                        X = ecc.X,
                        Y = ecc.Y,
                        T = ecc.T
                    };
                case KeyType.AES:
                    var aes = key.Parameters as AesParams;
                    return new JsonWebKey {
                        Kty =
                            JsonWebKeyType.Octet,
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
        /// <param name="useHsm"></param>
        /// <returns></returns>
        public static string ToKty(this KeyType type, bool useHsm) {
            switch (type) {
                case KeyType.RSA:
                    return useHsm ?
                        JsonWebKeyType.Rsa : JsonWebKeyType.RsaHsm;
                case KeyType.ECC:
                    return useHsm ?
                        JsonWebKeyType.EllipticCurve : JsonWebKeyType.EllipticCurveHsm;
                case KeyType.AES:
                    return JsonWebKeyType.Octet;
                default:
                    throw new NotSupportedException($"{type} is unknown");
            }
        }

        /// <summary>
        /// Convert to curve name
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static string ToJsonWebKeyCurveName(this CurveType curve) {
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
                    throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }

        /// <summary>
        /// Convert to curve type
        /// </summary>
        /// <param name="curveName"></param>
        /// <returns></returns>
        public static CurveType FromJsonWebKeyCurveName(string curveName) {
            if (string.IsNullOrEmpty(curveName)) {
                throw new ArgumentNullException(nameof(curveName));
            }
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
