// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto;
    using System.Security.Cryptography.Asn1;

    /// <summary>
    /// Digest signer extensions
    /// </summary>
    public static class DigestSignerEx {

        /// <summary>
        /// Create signature generator
        /// </summary>
        /// <param name="signer"></param>
        /// <param name="key"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public static X509SignatureGenerator CreateX509SignatureGenerator(
            this IDigestSigner signer, KeyHandle key, SignatureType signature) {
            return new X509SignatureGeneratorAdapter(signer, key, signature);
        }

        /// <summary>
        /// The X509 signature generator to sign a digest using
        /// <see cref="IDigestSigner"/>
        /// </summary>
        public sealed class X509SignatureGeneratorAdapter : X509SignatureGenerator {

            /// <summary>
            /// Create signature generator.
            /// </summary>
            /// <param name="signKey">The signing key</param>
            /// <param name="signer">Digest signer to use</param>
            /// <param name="signature"></param>
            public X509SignatureGeneratorAdapter(IDigestSigner signer, KeyHandle signKey,
                SignatureType signature) {
                _signKey = signKey ?? throw new ArgumentNullException(nameof(signKey));
                _signer = signer ?? throw new ArgumentNullException(nameof(signer));
                _signature = signature;
            }

            /// <inheritdoc/>
            public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) {
                if (_signature.ToHashAlgorithmName() != hashAlgorithm) {
                    throw new ArgumentException(
                        $"Hash algorithm {hashAlgorithm} not part of signature {_signature}",
                            nameof(hashAlgorithm));
                }
                using (var hash = CreateHashAlgorithm(hashAlgorithm)) {
                    var digest = hash.ComputeHash(data);
                    var result = _signer.SignAsync(_signKey, digest, _signature).Result;
                    if (_signature.IsECC()) {
                        using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                            writer.WriteIeee1363(result);
                            return writer.Encode();
                        }
                    }
                    return result;
                }
            }

            /// <inheritdoc/>
            protected override PublicKey BuildPublicKey() {
                throw new NotSupportedException(); // Should not be used
            }

            /// <inheritdoc/>
            public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm) {
                if (_signature.ToHashAlgorithmName() != hashAlgorithm) {
                    throw new ArgumentException(
                        $"Hash algorithm {hashAlgorithm} not part of signature {_signature}",
                            nameof(hashAlgorithm));
                }
                return _signature.ToAlgorithmIdentifier();
            }

            /// <summary>
            /// Create hash algorithm implementation
            /// </summary>
            /// <param name="hashAlgorithm"></param>
            /// <returns></returns>
            private static HashAlgorithm CreateHashAlgorithm(HashAlgorithmName hashAlgorithm) {
                if (hashAlgorithm == HashAlgorithmName.SHA256) {
                    return SHA256.Create();
                }
                if (hashAlgorithm == HashAlgorithmName.SHA384) {
                    return SHA384.Create();
                }
                if (hashAlgorithm == HashAlgorithmName.SHA512) {
                    return SHA512.Create();
                }
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm),
                    $"The hash algorithm {hashAlgorithm.Name} is not supported.");
            }

            private readonly KeyHandle _signKey;
            private readonly SignatureType _signature;
            private readonly IDigestSigner _signer;
        }
    }
}
