// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Diagnostics;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Asn1;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// EccParams extensions
    /// </summary>
    public static class EccParamsEx {

        /// <summary>
        /// Convert to public key
        /// </summary>
        /// <param name="ecc"></param>
        /// <returns></returns>
        public static PublicKey ToPublicKey(this EccParams ecc) {
            var ecParameters = ecc.ToECParameters();
            var curveOid = ecParameters.Curve.Oid.Value;
            byte[] curveOidEncoded;
            if (string.IsNullOrEmpty(curveOid)) {
                var friendlyName = ecParameters.Curve.Oid.FriendlyName;
                switch (friendlyName) {
                    case "nistP256":
                        curveOid = Oids.Secp256r1;
                        break;
                    case "nistP384":
                        curveOid = Oids.Secp384r1;
                        break;
                    case "nistP521":
                        curveOid = Oids.Secp521r1;
                        break;
                    default:
                        curveOid = new Oid(friendlyName).Value;
                        break;
                }
            }
            using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                writer.WriteObjectIdentifier(curveOid);
                curveOidEncoded = writer.Encode();
            }
            Debug.Assert(ecParameters.Q.X.Length == ecParameters.Q.Y.Length);
            var uncompressedPoint =
                new byte[1 + ecParameters.Q.X.Length + ecParameters.Q.Y.Length];

            // Uncompressed point (0x04)
            uncompressedPoint[0] = 0x04;
            Buffer.BlockCopy(ecParameters.Q.X, 0, uncompressedPoint,
                1, ecParameters.Q.X.Length);
            Buffer.BlockCopy(ecParameters.Q.Y, 0, uncompressedPoint,
                1 + ecParameters.Q.X.Length, ecParameters.Q.Y.Length);
            var ecPublicKey = new Oid(Oids.EcPublicKey);
            return new PublicKey(
                ecPublicKey,
                new AsnEncodedData(ecPublicKey, curveOidEncoded),
                new AsnEncodedData(ecPublicKey, uncompressedPoint));
        }
    }
}