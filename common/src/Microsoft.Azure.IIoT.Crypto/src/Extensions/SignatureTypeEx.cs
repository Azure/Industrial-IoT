// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Asn1;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Signature type extensions
    /// </summary>
    public static class SignatureTypeEx {

        /// <summary>
        /// To DER algorithm identifier
        /// </summary>
        /// <param name="signature"></param>
        /// <returns></returns>
        public static byte[] ToAlgorithmIdentifier(this SignatureType signature) {
            var hashAlgorithm = signature.ToHashAlgorithmName();
            string oid;
            if (signature.IsRSA()) {
                var padding = signature.ToRSASignaturePadding();
                if (padding == RSASignaturePadding.Pkcs1) {
                    if (hashAlgorithm == HashAlgorithmName.SHA256) {
                        oid = Oids.RsaPkcs1Sha256;
                    }
                    else if (hashAlgorithm == HashAlgorithmName.SHA384) {
                        oid = Oids.RsaPkcs1Sha384;
                    }
                    else if (hashAlgorithm == HashAlgorithmName.SHA512) {
                        oid = Oids.RsaPkcs1Sha512;
                    }
                    else {
                        throw new ArgumentOutOfRangeException(nameof(hashAlgorithm),
                            $"The hash algorithm {hashAlgorithm.Name} is not supported.");
                    }
                    using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                        writer.PushSequence();
                        writer.WriteObjectIdentifier(oid);
                        writer.WriteNull();
                        writer.PopSequence();
                        return writer.Encode();
                    }
                }
                else if (padding == RSASignaturePadding.Pss) {
                    int saltLen;

                    if (hashAlgorithm == HashAlgorithmName.SHA256) {
                        saltLen = 256 / 8;
                        oid = Oids.Sha256;
                    }
                    else if (hashAlgorithm == HashAlgorithmName.SHA384) {
                        saltLen = 384 / 8;
                        oid = Oids.Sha384;
                    }
                    else if (hashAlgorithm == HashAlgorithmName.SHA512) {
                        saltLen = 512 / 8;
                        oid = Oids.Sha512;
                    }
                    else {
                        throw new ArgumentOutOfRangeException(nameof(hashAlgorithm),
                            $"The hash algorithm {hashAlgorithm.Name} is not supported.");
                    }
                    using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                        writer.WritePssSignatureAlgorithmIdentifier(saltLen, oid);
                        return writer.Encode();
                    }
                }
                else {
                    throw new ArgumentOutOfRangeException(
                        "Specified Padding is not supported.");
                }
            }
            else {
                if (hashAlgorithm == HashAlgorithmName.SHA256) {
                    oid = Oids.ECDsaWithSha256;
                }
                else if (hashAlgorithm == HashAlgorithmName.SHA384) {
                    oid = Oids.ECDsaWithSha384;
                }
                else if (hashAlgorithm == HashAlgorithmName.SHA512) {
                    oid = Oids.ECDsaWithSha512;
                }
                else {
                    throw new ArgumentOutOfRangeException(nameof(hashAlgorithm),
                        $"The hash algorithm {hashAlgorithm.Name} is not supported.");
                }
                using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                    writer.PushSequence();
                    writer.WriteObjectIdentifier(oid);
                    writer.PopSequence();
                    return writer.Encode();
                }
            }
        }
    }
}