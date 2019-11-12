// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Asn1.X9;
    using Org.BouncyCastle.Crypto.Parameters;
    using System;

    /// <summary>
    /// Elliptic Curve Cryptography (ECC) curves.
    /// </summary>
    internal static class CurveTypeEx {

        /// <summary>
        /// Get curve parameters
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static X9ECParameters ToX9ECParameters(this CurveType type) {
            switch (type) {
                case CurveType.P256:
                    return ECNamedCurveTable.GetByName("P-256");
                case CurveType.P384:
                    return ECNamedCurveTable.GetByName("P-384");
                case CurveType.P521:
                    return ECNamedCurveTable.GetByName("P-521");
                case CurveType.P256K:
                    return ECNamedCurveTable.GetByName("K-283");
                case CurveType.Brainpool_P160r1:
                    return ECNamedCurveTable.GetByName("brainpoolP160r1");
                case CurveType.Brainpool_P160t1:
                    return ECNamedCurveTable.GetByName("brainpoolP160t1");
                case CurveType.Brainpool_P192r1:
                    return ECNamedCurveTable.GetByName("brainpoolP192r1");
                case CurveType.Brainpool_P192t1:
                    return ECNamedCurveTable.GetByName("brainpoolP192t1");
                case CurveType.Brainpool_P224r1:
                    return ECNamedCurveTable.GetByName("brainpoolP224r1");
                case CurveType.Brainpool_P224t1:
                    return ECNamedCurveTable.GetByName("brainpoolP224t1");
                case CurveType.Brainpool_P256r1:
                    return ECNamedCurveTable.GetByName("brainpoolP256r1");
                case CurveType.Brainpool_P256t1:
                    return ECNamedCurveTable.GetByName("brainpoolP256t1");
                case CurveType.Brainpool_P320r1:
                    return ECNamedCurveTable.GetByName("brainpoolP320r1");
                case CurveType.Brainpool_P320t1:
                    return ECNamedCurveTable.GetByName("brainpoolP320t1");
                case CurveType.Brainpool_P384r1:
                    return ECNamedCurveTable.GetByName("brainpoolP384r1");
                case CurveType.Brainpool_P384t1:
                    return ECNamedCurveTable.GetByName("brainpoolP384t1");
                case CurveType.Brainpool_P512r1:
                    return ECNamedCurveTable.GetByName("brainpoolP512r1");
                case CurveType.Brainpool_P512t1:
                    return ECNamedCurveTable.GetByName("brainpoolP512t1");
            }
            throw new ArgumentException($"Unknown curve {type}");
        }

        /// <summary>
        /// Convert to domain parameters
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static ECDomainParameters ToECDomainParameters(this CurveType type) {
            var curveParams = type.ToX9ECParameters();
            return new ECDomainParameters(curveParams.Curve, curveParams.G,
                curveParams.N, curveParams.H, curveParams.GetSeed());
        }

        /// <summary>
        /// Get curve name
        /// </summary>
        /// <param name="ecParameterSpec"></param>
        /// <returns></returns>
        internal static string ToCurveName(this ECDomainParameters ecParameterSpec) {
            foreach (var name in ECNamedCurveTable.Names) {
                var parameters = ECNamedCurveTable.GetByName((string)name);
                if (parameters.N.Equals(ecParameterSpec.N)
                    && parameters.H.Equals(ecParameterSpec.H)
                    && parameters.Curve.Equals(ecParameterSpec.Curve)) {
                    return (string)name;
                }
            }
            return null;
        }

        /// <summary>
        /// Get curve name
        /// </summary>
        /// <param name="ecParameterSpec"></param>
        /// <returns></returns>
        internal static CurveType ToCurveType(this ECDomainParameters ecParameterSpec) {
            var name = ToCurveName(ecParameterSpec);
            if (name == null) {
                throw new ArgumentException(
                    "Failed to find name from parameter spec - unknown curve.");
            }
            switch (name) {
                case "secp256r1":
                    return CurveType.P256;
                case "secp384r1":
                    return CurveType.P384;
                case "secp521r1":
                    return CurveType.P521;
                case "secp256t1": // TODO
                    return CurveType.P256K;
                case "brainpoolP160r1":
                    return CurveType.Brainpool_P160r1;
                case "brainpoolP160t1":
                    return CurveType.Brainpool_P160t1;
                case "brainpoolP192r1":
                    return CurveType.Brainpool_P192r1;
                case "brainpoolP192t1":
                    return CurveType.Brainpool_P192t1;
                case "brainpoolP224r1":
                    return CurveType.Brainpool_P224r1;
                case "brainpoolP224t1":
                    return CurveType.Brainpool_P224t1;
                case "brainpoolP256r1":
                    return CurveType.Brainpool_P256r1;
                case "brainpoolP256t1":
                    return CurveType.Brainpool_P256t1;
                case "brainpoolP320r1":
                    return CurveType.Brainpool_P320r1;
                case "brainpoolP320t1":
                    return CurveType.Brainpool_P320t1;
                case "brainpoolP384r1":
                    return CurveType.Brainpool_P384r1;
                case "brainpoolP384t1":
                    return CurveType.Brainpool_P384t1;
                case "brainpoolP512r1":
                    return CurveType.Brainpool_P512r1;
                case "brainpoolP512t1":
                    return CurveType.Brainpool_P512t1;
            }
            throw new ArgumentException($"Unsupported curve {name}.");
        }
    }
}