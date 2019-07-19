// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using Xunit;

    public static class CertificateExTests {

        [Theory]
        [InlineData(SignatureType.PS256)]
        [InlineData(SignatureType.RS256)]
        [InlineData(SignatureType.PS384)]
        [InlineData(SignatureType.RS512)]
        public static void ConvertRSACertToPfxAndBack(SignatureType sig) {

            using (var cert = sig.Create("CN=leaf")) {
                var certificate1 = cert.ToCertificate();
                var pfx = certificate1.ToPfx(cert.GetRSAPrivateKey().ToKey());
                using (var cert2 = new X509Certificate2(pfx, (string)null, X509KeyStorageFlags.Exportable)) {
                    var certificate2 = cert2.ToCertificate();

                    Assert.True(certificate2.SameAs(certificate2));
                    Assert.True(certificate1.IsSelfSigned());
                    Assert.Equal(certificate1.GetIssuerSerialNumberAsString(), certificate1.GetSerialNumberAsString());
                }
            }
        }

        [Theory]
        [InlineData(SignatureType.PS256)]
        [InlineData(SignatureType.RS256)]
        [InlineData(SignatureType.PS384)]
        [InlineData(SignatureType.RS512)]
        public static void ConvertRSACertToPfxAndBackWithPassword(SignatureType sig) {

            var pw = Guid.NewGuid().ToString();
            using (var cert = sig.Create("CN=leaf")) {
                var certificate1 = cert.ToCertificate();
                var pfx = certificate1.ToPfx(cert.GetRSAPrivateKey().ToKey(), pw);
                using (var cert2 = new X509Certificate2(pfx, pw, X509KeyStorageFlags.Exportable)) {
                    var certificate2 = cert2.ToCertificate();

                    Assert.True(certificate2.SameAs(certificate2));
                    Assert.True(certificate1.IsSelfSigned());
                    Assert.Equal(certificate1.GetIssuerSerialNumberAsString(), certificate1.GetSerialNumberAsString());
                }
            }
        }

        [Theory]
        [InlineData(SignatureType.ES256)]
        [InlineData(SignatureType.ES384)]
        [InlineData(SignatureType.ES512)]
        public static void ConvertECCCertToPfxAndBackWithPassword(SignatureType sig) {

            var pw = Guid.NewGuid().ToString();
            using (var cert = sig.Create("CN=leaf")) {
                var certificate1 = cert.ToCertificate();
                var pfx = certificate1.ToPfx(cert.GetECDsaPrivateKey().ToKey(), pw);
                using (var cert2 = new X509Certificate2(pfx, pw,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet)) {
                    var certificate2 = cert2.ToCertificate();

                    Assert.True(certificate2.SameAs(certificate2));
                    Assert.True(certificate1.IsSelfSigned());
                    Assert.Equal(certificate1.GetIssuerSerialNumberAsString(), certificate1.GetSerialNumberAsString());
                }
            }
        }

        [Theory]
        [InlineData(SignatureType.ES256)]
        [InlineData(SignatureType.ES384)]
        [InlineData(SignatureType.ES512)]
        public static void ConvertECCCertToPfxAndBack(SignatureType sig) {

            using (var cert = sig.Create("CN=leaf")) {
                var certificate1 = cert.ToCertificate();
                var pfx = certificate1.ToPfx(cert.GetECDsaPrivateKey().ToKey());
                using (var cert2 = new X509Certificate2(pfx, (string)null,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet)) {

                    var certificate2 = cert2.ToCertificate();

                    Assert.True(certificate2.SameAs(certificate2));
                    Assert.True(certificate1.IsSelfSigned());
                    Assert.Equal(certificate1.GetIssuerSerialNumberAsString(), certificate1.GetSerialNumberAsString());
                }
            }
        }
    }
}

