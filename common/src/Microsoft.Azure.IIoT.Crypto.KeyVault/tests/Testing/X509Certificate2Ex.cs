// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Linq;

    /// <summary>
    /// X509 cert extensions
    /// </summary>
    internal static class X509Certificate2Ex {

        /// <summary>
        /// Find extension by oid
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        internal static X509Extension GetExtensionByOid(this X509Certificate2 certificate,
            string oid) {
            return certificate.Extensions
                .OfType<X509Extension>()
                .FirstOrDefault(e => e.Oid.Value == oid);
        }

        /// <summary>
        /// Export private key
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        internal static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate,
            AsymmetricAlgorithm key) {
            if (key is RSA rsa) {
                return certificate.CopyWithPrivateKey(rsa);
            }
            if (key is ECDsa ecdsa) {
                return certificate.CopyWithPrivateKey(ecdsa);
            }
            return null;
        }

        /// <summary>
        /// Create self signed cert
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        internal static AsymmetricAlgorithm CreateCsr(this SignatureType signature, string subject,
            out CertificateRequest csr) {
            AsymmetricAlgorithm alg = null;
            try {
                if (signature.IsRSA()) {
                    var rsa = RSA.Create();
                    alg = rsa;
                    csr = new CertificateRequest(X500DistinguishedNameEx.Create(subject),
                        rsa, signature.ToHashAlgorithmName(), signature.ToRSASignaturePadding());
                    return alg;
                }
                if (signature.IsECC()) {
                    var ecdsa = ECDsa.Create();
                    alg = ecdsa;
                    csr = new CertificateRequest(X500DistinguishedNameEx.Create(subject),
                        ecdsa, signature.ToHashAlgorithmName());
                    return alg;
                }
                throw new ArgumentException("Bad signature");
            }
            catch {
                alg?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Create self signed ca cert
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        internal static AsymmetricAlgorithm CreateCsr(this SignatureType signature,
            string subject, bool ca, out CertificateRequest csr) {
            var key = CreateCsr(signature, subject, out csr);
            csr.CertificateExtensions.Add(new X509BasicConstraintsExtension(
                ca, false, 0, true));
            if (ca) {
                csr.CertificateExtensions.Add(new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
            }
            return key;
        }
    }
}
