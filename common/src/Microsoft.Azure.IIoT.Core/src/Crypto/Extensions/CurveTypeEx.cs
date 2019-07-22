// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Elliptic Curve Cryptography (ECC) curves.
    /// </summary>
    public static class CurveTypeEx {

        /// <summary>
        /// Get curve name
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetName(this CurveType type) {
            switch (type) {
                case CurveType.P256:
                    return "P-256";
                case CurveType.P384:
                    return "P-384";
                case CurveType.P521:
                    return "P-521";
                case CurveType.P256K:
                    return "P-256K";
                case CurveType.Brainpool_P160r1:
                    return "Brainpool-P-160-r1";
                case CurveType.Brainpool_P160t1:
                    return "Brainpool-P-160-t1";
                case CurveType.Brainpool_P192r1:
                    return "Brainpool-P-192-r1";
                case CurveType.Brainpool_P192t1:
                    return "Brainpool-P-192-t1";
                case CurveType.Brainpool_P224r1:
                    return "Brainpool-P-224-r1";
                case CurveType.Brainpool_P224t1:
                    return "Brainpool-P-224-t1";
                case CurveType.Brainpool_P256r1:
                    return "Brainpool-P-256-r1";
                case CurveType.Brainpool_P256t1:
                    return "Brainpool-P-256-t1";
                case CurveType.Brainpool_P320r1:
                    return "Brainpool-P-320-r1";
                case CurveType.Brainpool_P320t1:
                    return "Brainpool-P-320-t1";
                case CurveType.Brainpool_P384r1:
                    return "Brainpool-P-384-r1";
                case CurveType.Brainpool_P384t1:
                    return "Brainpool-P-384-t1";
                case CurveType.Brainpool_P512r1:
                    return "Brainpool-P-512-r1";
                case CurveType.Brainpool_P512t1:
                    return "Brainpool-P-512-t1";
            }
            throw new ArgumentException($"Unknown curve {type}");
        }

        /// <summary>
        /// Returns the required size, in bytes, of each key parameters
        /// (X, Y and D), or -1 if the curve is unsupported.
        /// </summary>
        /// <param name="type">The curve for which key parameter size is
        /// required.</param>
        /// <returns></returns>
        public static int GetKeyParameterSize(this CurveType type) {
            switch (type) {
                case CurveType.P256:
                    return 32;
                case CurveType.P384:
                    return 48;
                case CurveType.P521:
                    return 66;
                case CurveType.P256K:
                    return 32;
                case CurveType.Brainpool_P160r1:
                    break; // TODO
                case CurveType.Brainpool_P160t1:
                    break;
                case CurveType.Brainpool_P192r1:
                    break;
                case CurveType.Brainpool_P192t1:
                    break;
                case CurveType.Brainpool_P224r1:
                    break;
                case CurveType.Brainpool_P224t1:
                    break;
                case CurveType.Brainpool_P256r1:
                    break;
                case CurveType.Brainpool_P256t1:
                    break;
                case CurveType.Brainpool_P320r1:
                    break;
                case CurveType.Brainpool_P320t1:
                    break;
                case CurveType.Brainpool_P384r1:
                    break;
                case CurveType.Brainpool_P384t1:
                    break;
                case CurveType.Brainpool_P512r1:
                    break;
                case CurveType.Brainpool_P512t1:
                    break;
            }
            throw new ArgumentException($"Unknown curve {type}");
        }

        /// <summary>
        /// Get curve name
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ECCurve ToECCurve(this CurveType type) {
            switch (type) {
                case CurveType.P256:
                    return ECCurve.NamedCurves.nistP256;
                case CurveType.P384:
                    return ECCurve.NamedCurves.nistP384;
                case CurveType.P521:
                    return ECCurve.NamedCurves.nistP521;
                case CurveType.P256K:
                    return ECCurve.CreateFromFriendlyName("secP256r1");
                case CurveType.Brainpool_P160r1:
                    return ECCurve.NamedCurves.brainpoolP160r1;
                case CurveType.Brainpool_P160t1:
                    return ECCurve.NamedCurves.brainpoolP160t1;
                case CurveType.Brainpool_P192r1:
                    return ECCurve.NamedCurves.brainpoolP192r1;
                case CurveType.Brainpool_P192t1:
                    return ECCurve.NamedCurves.brainpoolP192t1;
                case CurveType.Brainpool_P224r1:
                    return ECCurve.NamedCurves.brainpoolP224r1;
                case CurveType.Brainpool_P224t1:
                    return ECCurve.NamedCurves.brainpoolP224t1;
                case CurveType.Brainpool_P256r1:
                    return ECCurve.NamedCurves.brainpoolP256r1;
                case CurveType.Brainpool_P256t1:
                    return ECCurve.NamedCurves.brainpoolP256t1;
                case CurveType.Brainpool_P320r1:
                    return ECCurve.NamedCurves.brainpoolP320r1;
                case CurveType.Brainpool_P320t1:
                    return ECCurve.NamedCurves.brainpoolP320t1;
                case CurveType.Brainpool_P384r1:
                    return ECCurve.NamedCurves.brainpoolP384r1;
                case CurveType.Brainpool_P384t1:
                    return ECCurve.NamedCurves.brainpoolP384t1;
                case CurveType.Brainpool_P512r1:
                    return ECCurve.NamedCurves.brainpoolP512r1;
                case CurveType.Brainpool_P512t1:
                    return ECCurve.NamedCurves.brainpoolP512t1;
            }
            throw new ArgumentException($"Unknown curve {type}");
        }

        /// <summary>
        /// Get curve name
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static CurveType ToCurveType(this ECCurve curve) {
            switch (curve.Oid.Value) {
                case "1.2.840.10045.3.1.7":
                    return CurveType.P256;
                case "1.3.132.0.34":
                    return CurveType.P384;
                case "1.3.132.0.35":
                    return CurveType.P521;
                case "1.3.132.0.10":
                    return CurveType.P256K;

                // ...
            }

            // Try match on friendly name
            switch (curve.Oid.FriendlyName) {
                case "secP256r1":
                case "ECDSA_P256":
                case nameof(ECCurve.NamedCurves.nistP256):
                    return CurveType.P256;
                case "secP384r1":
                case "ECDSA_P384":
                case nameof(ECCurve.NamedCurves.nistP384):
                    return CurveType.P384;
                case "secP521r1":
                case "ECDSA_P521":
                case nameof(ECCurve.NamedCurves.nistP521):
                    return CurveType.P521;
                case "secP256k1":
                    return CurveType.P256K;
                case nameof(ECCurve.NamedCurves.brainpoolP160r1):
                    return CurveType.Brainpool_P160r1;
                case nameof(ECCurve.NamedCurves.brainpoolP160t1):
                    return CurveType.Brainpool_P160t1;
                case nameof(ECCurve.NamedCurves.brainpoolP192r1):
                    return CurveType.Brainpool_P192r1;
                case nameof(ECCurve.NamedCurves.brainpoolP192t1):
                    return CurveType.Brainpool_P192t1;
                case nameof(ECCurve.NamedCurves.brainpoolP224r1):
                    return CurveType.Brainpool_P224r1;
                case nameof(ECCurve.NamedCurves.brainpoolP224t1):
                    return CurveType.Brainpool_P224t1;
                case nameof(ECCurve.NamedCurves.brainpoolP256r1):
                    return CurveType.Brainpool_P256r1;
                case nameof(ECCurve.NamedCurves.brainpoolP256t1):
                    return CurveType.Brainpool_P256t1;
                case nameof(ECCurve.NamedCurves.brainpoolP320r1):
                    return CurveType.Brainpool_P320r1;
                case nameof(ECCurve.NamedCurves.brainpoolP320t1):
                    return CurveType.Brainpool_P320t1;
                case nameof(ECCurve.NamedCurves.brainpoolP384r1):
                    return CurveType.Brainpool_P384r1;
                case nameof(ECCurve.NamedCurves.brainpoolP384t1):
                    return CurveType.Brainpool_P384t1;
                case nameof(ECCurve.NamedCurves.brainpoolP512r1):
                    return CurveType.Brainpool_P512r1;
                case nameof(ECCurve.NamedCurves.brainpoolP512t1):
                    return CurveType.Brainpool_P512t1;
            }
            throw new ArgumentException(
                $"Unknown curve {curve.Oid.Value} ({curve.Oid.FriendlyName})");
        }
    }
}