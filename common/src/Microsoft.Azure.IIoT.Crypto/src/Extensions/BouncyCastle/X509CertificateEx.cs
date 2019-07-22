// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Pkcs;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.X509;
    using System.IO;

    /// <summary>
    /// Certificate extensions
    /// </summary>
    internal static class X509CertificateEx {

        /// <summary>
        /// Create pfx
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="privateKey"></param>
        /// <param name="password"></param>
        internal static byte[] ToPfx(this X509Certificate certificate,
            AsymmetricKeyParameter privateKey, string password = null) {

            var builder = new Pkcs12StoreBuilder();
            builder.SetUseDerEncoding(true);
            var store = builder.Build();

            // create store entry
            var friendlyName = certificate.SubjectDN.ToString();
            store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(privateKey),
                new X509CertificateEntry[] {
                    new X509CertificateEntry(certificate)
                });
            using (var stream = new MemoryStream()) {
                using (var cfrg = new RandomGeneratorAdapter()) {
                    store.Save(stream, (password ?? string.Empty).ToCharArray(),
                        new SecureRandom(cfrg));
                }
                return Pkcs12Utilities.ConvertToDefiniteLength(stream.ToArray());
            }
        }

        /// <summary>
        /// Convert to bouncy castle cert
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        internal static X509Certificate ToX509Certificate(
            this System.Security.Cryptography.X509Certificates.X509Certificate2 cert) {
            return ToX509Certificate(cert.RawData);
        }

        /// <summary>
        /// Convert to bouncy castle cert
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static X509Certificate ToX509Certificate(byte[] buffer) {
            return new X509CertificateParser().ReadCertificate(buffer);
        }

        /// <summary>
        /// Convert to bouncy castle cert
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        internal static X509Certificate ToX509Certificate(this Certificate certificate) {
            return ToX509Certificate(certificate.RawData);
        }
    }
}
