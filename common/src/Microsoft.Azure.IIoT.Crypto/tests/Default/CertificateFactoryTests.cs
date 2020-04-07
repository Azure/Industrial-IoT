// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Default {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto.Storage;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Autofac.Extras.Moq;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    /// <summary>
    /// Certificate factory tests
    /// </summary>
    public static class CertificateFactoryTests {

        [Fact]
        public static async Task RsaCertificateCreateSelfSignedTestAsync() {

            using (var mock = Setup()) {

                IKeyStore keys = mock.Create<KeyDatabase>();
                IDigestSigner signer = mock.Create<KeyDatabase>();
                ICertificateFactory factory = mock.Create<CertificateFactory>();

                KeyHandle issuerKey;
                Key issuerPublicKey;
                using (var rsa1 = RSA.Create()) {
                    issuerKey = await keys.ImportKeyAsync("rsa1", rsa1.ToKey(),
                        new KeyStoreProperties { Exportable = true });
                    issuerPublicKey = rsa1.ToKey().GetPublicKey();
                }

                var now = DateTime.UtcNow;
                var cert = await factory.CreateCertificateAsync(signer, issuerKey,
                    X500DistinguishedNameEx.Create("CN=leaf"), issuerPublicKey,
                    now, now + TimeSpan.FromMinutes(1), SignatureType.RS256, false, sn => {
                        return new List<X509Extension>();
                    });

                var privateKey = await keys.ExportKeyAsync(issuerKey);

                using (cert) {
                    var certificate = cert.ToCertificate();
                    Assert.True(certificate.IsSelfSigned());
                    Assert.Equal(certificate.GetIssuerSerialNumberAsString(), certificate.GetSerialNumberAsString());
                }
            }
        }

        [Fact]
        public static async Task RsaCreateLeafCertificateTestAsync() {

            using (var mock = Setup()) {

                IKeyStore keys = mock.Create<KeyDatabase>();
                IDigestSigner signer = mock.Create<KeyDatabase>();
                ICertificateFactory factory = mock.Create<CertificateFactory>();

                Key publicKey;
                KeyHandle issuerKey;
                Key issuerPublicKey;
                using (var rsa1 = RSA.Create()) {
                    issuerKey = await keys.ImportKeyAsync("rsa1", rsa1.ToKey());
                    issuerPublicKey = rsa1.ToKey().GetPublicKey();
                }
                using (var rsa2 = RSA.Create()) {
                    await keys.ImportKeyAsync("rsa2", rsa2.ToKey());
                    publicKey = rsa2.ToKey().GetPublicKey();
                }

                var now = DateTime.UtcNow;
                var leaf = await factory.CreateCertificateAsync(signer, issuerKey,
                    X500DistinguishedNameEx.Create("CN=leaf"), publicKey,
                    now, now + TimeSpan.FromMinutes(1), SignatureType.RS256, false, sn => {
                        return new List<X509Extension>();
                    });
            }
        }

        [Fact]
        public static async Task RsaCertificateCreateIntermediateCaTestAsync() {

            using (var mock = Setup()) {

                IKeyStore keys = mock.Create<KeyDatabase>();
                IDigestSigner signer = mock.Create<KeyDatabase>();
                ICertificateFactory factory = mock.Create<CertificateFactory>();

                Key publicKey;
                KeyHandle issuerKey;
                Key issuerPublicKey;
                using (var rsa1 = RSA.Create()) {
                    issuerKey = await keys.ImportKeyAsync("rsa1", rsa1.ToKey());
                    issuerPublicKey = rsa1.ToKey().GetPublicKey();
                }
                using (var rsa2 = RSA.Create()) {
                    await keys.ImportKeyAsync("rsa2", rsa2.ToKey());
                    publicKey = rsa2.ToKey().GetPublicKey();
                }

                var now = DateTime.UtcNow;
                var intca = await factory.CreateCertificateAsync(signer, issuerKey,
                    X500DistinguishedNameEx.Create("CN=leaf"), publicKey,
                    now, now + TimeSpan.FromMinutes(1), SignatureType.PS256, true, sn => {
                        return new List<X509Extension>();
                    });

            }
        }

        /// <summary>
        /// Setup mock
        /// </summary>
        /// <param name="mock"></param>
        private static AutoMock Setup() {
            var mock = AutoMock.GetLoose(builder => {
                builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonSoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterType<MemoryDatabase>().SingleInstance().As<IDatabaseServer>();
                builder.RegisterType<ItemContainerFactory>().As<IItemContainerFactory>();
                builder.RegisterType<KeyDatabase>().As<IKeyStore>();
                builder.RegisterType<KeyHandleSerializer>().As<IKeyHandleSerializer>();
                builder.RegisterType<CertificateFactory>().As<ICertificateFactory>();
            });
            return mock;
        }
    }
}

