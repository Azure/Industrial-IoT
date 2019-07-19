// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Crypto.BouncyCastle;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Xunit;

    public class CertificateRequestExTests {

        [Fact]
        public static void EcdsaCertificateRequestTest() {
            const string TestCN = "CN=Test";
            const string LeafCN = "CN=Leaf";
            var serialNumber = new byte[20];
            var rand = new Random();
            rand.NextBytes(serialNumber);
            serialNumber[0] = 0x80;

            using (var ecdsa1 = ECDsa.Create())
            using (var ecdsa2 = ECDsa.Create()) {
                var request1 = new CertificateRequest(TestCN, ecdsa1, HashAlgorithmName.SHA256);
                request1.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, false));
                Assert.NotNull(request1.PublicKey);
                Assert.NotNull(request1.CertificateExtensions);
                Assert.NotEmpty(request1.CertificateExtensions);
                Assert.Equal(TestCN, request1.SubjectName.Name);

                var ca = request1.CreateSelfSigned(DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromDays(1));
                Assert.Equal(ca.Subject, TestCN);

                var request2 = new CertificateRequest(LeafCN, ecdsa2, HashAlgorithmName.SHA256);
                Assert.NotNull(request2.PublicKey);
                Assert.NotNull(request2.CertificateExtensions);
                Assert.Empty(request2.CertificateExtensions);
                Assert.Equal(LeafCN, request2.SubjectName.Name);

                var leaf = request2.Create(ca, DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(1), serialNumber);
                Assert.Equal(leaf.Subject, LeafCN);
                Assert.Equal(new SerialNumber(serialNumber).ToString(), leaf.SerialNumber);

                var buffer = request2.CreateSigningRequest();

                var info = buffer.ToCertificationRequestInfo();
                Assert.True(info.GetPublicKey().SameAs(ecdsa2.ToPublicKey()));
                Assert.Equal(LeafCN, new X500DistinguishedName(info.Subject.GetEncoded()).Name);

                var csr = buffer.ToCertificateRequest(SignatureType.ES256);
                //  var pubk = csr.PublicKey.Key;
                //  Assert.True(pubk.ToPublicKey().SameAs(ecdsa2.ToPublicKey()));
                Assert.Equal(LeafCN, csr.SubjectName.Name);

                var leaf2 = csr.Create(ca, DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(1), serialNumber);
                Assert.True(leaf2.GetECDsaPublicKey().ToPublicKey().SameAs(ecdsa2.ToPublicKey()));
                Assert.Equal(leaf.Subject, leaf2.Subject);
                Assert.Equal(leaf.SerialNumber, leaf2.SerialNumber);
            }
        }

        [Fact]
        public static void RsaCertificateRequestTest() {
            const string TestCN = "CN=Test";
            const string LeafCN = "CN=Leaf";
            var serialNumber = new byte[20];
            var rand = new Random();
            rand.NextBytes(serialNumber);
            serialNumber[0] = 0x80;

            using (var rsa1 = RSA.Create())
            using (var rsa2 = RSA.Create()) {
                var request1 = new CertificateRequest(TestCN, rsa1, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request1.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, false));
                Assert.NotNull(request1.PublicKey);
                Assert.NotNull(request1.CertificateExtensions);
                Assert.NotEmpty(request1.CertificateExtensions);
                Assert.Equal(TestCN, request1.SubjectName.Name);

                var ca = request1.CreateSelfSigned(DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromDays(1));
                Assert.Equal(ca.Subject, TestCN);

                var request2 = new CertificateRequest(LeafCN, rsa2, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                Assert.NotNull(request2.PublicKey);
                Assert.NotNull(request2.CertificateExtensions);
                Assert.Empty(request2.CertificateExtensions);
                Assert.Equal(LeafCN, request2.SubjectName.Name);

                var leaf = request2.Create(ca, DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(1), serialNumber);
                Assert.Equal(leaf.Subject, LeafCN);
                Assert.Equal(new SerialNumber(serialNumber).ToString(), leaf.SerialNumber);

                var buffer = request2.CreateSigningRequest();

                var info = buffer.ToCertificationRequestInfo();
                Assert.True(info.GetPublicKey().SameAs(rsa2.ToPublicKey()));
                Assert.Equal(LeafCN, new X500DistinguishedName(info.Subject.GetEncoded()).Name);

                var csr = buffer.ToCertificateRequest(SignatureType.PS256);
                var pubk = csr.PublicKey.ToKey();
                Assert.True(pubk.SameAs(rsa2.ToPublicKey()));
                Assert.Equal(LeafCN, csr.SubjectName.Name);

                var leaf2 = csr.Create(ca.IssuerName,
                    X509SignatureGenerator.CreateForRSA(ca.GetRSAPrivateKey(), RSASignaturePadding.Pkcs1),
                    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow + TimeSpan.FromHours(1), serialNumber);
                Assert.True(leaf2.GetRSAPublicKey().ToPublicKey().SameAs(rsa2.ToPublicKey()));
                Assert.Equal(leaf.Subject, leaf2.Subject);
                Assert.Equal(leaf.SerialNumber, leaf2.SerialNumber);
            }
        }

        [Fact]
        public static void RsaPssCertificateRequestTest() {
            const string TestCN = "CN=Test";
            const string LeafCN = "CN=Leaf";
            var serialNumber = new byte[20];
            var rand = new Random();
            rand.NextBytes(serialNumber);
            serialNumber[0] = 0x80;

            using (var rsa1 = RSA.Create())
            using (var rsa2 = RSA.Create()) {
                var request1 = new CertificateRequest(TestCN, rsa1, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                request1.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, false));
                Assert.NotNull(request1.PublicKey);
                Assert.NotNull(request1.CertificateExtensions);
                Assert.NotEmpty(request1.CertificateExtensions);
                Assert.Equal(TestCN, request1.SubjectName.Name);

                var ca = request1.CreateSelfSigned(DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromDays(1));
                Assert.Equal(ca.Subject, TestCN);

                var request2 = new CertificateRequest(LeafCN, rsa2, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                Assert.NotNull(request2.PublicKey);
                Assert.NotNull(request2.CertificateExtensions);
                Assert.Empty(request2.CertificateExtensions);
                Assert.Equal(LeafCN, request2.SubjectName.Name);

                var leaf = request2.Create(ca, DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(1), serialNumber);
                Assert.Equal(leaf.Subject, LeafCN);
                Assert.Equal(new SerialNumber(serialNumber).ToString(), leaf.SerialNumber);

                var buffer = request2.CreateSigningRequest();

                var info = buffer.ToCertificationRequestInfo();
                Assert.True(info.GetPublicKey().SameAs(rsa2.ToPublicKey()));
                Assert.Equal(LeafCN, new X500DistinguishedName(info.Subject.GetEncoded()).Name);

                var csr = buffer.ToCertificateRequest(SignatureType.PS256);
                var pubk = csr.PublicKey.ToKey();
                Assert.True(pubk.SameAs(rsa2.ToPublicKey()));
                Assert.Equal(LeafCN, csr.SubjectName.Name);

                var leaf2 = csr.Create(ca.IssuerName,
                    X509SignatureGenerator.CreateForRSA(ca.GetRSAPrivateKey(), RSASignaturePadding.Pss),
                    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow + TimeSpan.FromHours(1), serialNumber);
                Assert.True(leaf2.GetRSAPublicKey().ToPublicKey().SameAs(rsa2.ToPublicKey()));
                Assert.Equal(leaf.Subject, leaf2.Subject);
                Assert.Equal(leaf.SerialNumber, leaf2.SerialNumber);
            }
        }

        [Fact]
        public static void HybridCertificateRequestTest() {
            const string TestCN = "CN=Test";
            const string LeafCN = "CN=Leaf";
            var serialNumber = new byte[20];
            var rand = new Random();
            rand.NextBytes(serialNumber);
            serialNumber[0] = 0x80;

            using (var rsa1 = RSA.Create())
            using (var ecdsa2 = ECDsa.Create()) {
                var request1 = new CertificateRequest(TestCN, rsa1, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request1.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, false));
                Assert.NotNull(request1.PublicKey);
                Assert.NotNull(request1.CertificateExtensions);
                Assert.NotEmpty(request1.CertificateExtensions);
                Assert.Equal(TestCN, request1.SubjectName.Name);

                var ca = request1.CreateSelfSigned(DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromDays(1));
                Assert.Equal(ca.Subject, TestCN);

                var request2 = new CertificateRequest(LeafCN, ecdsa2, HashAlgorithmName.SHA512);
                request2.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                Assert.NotNull(request2.PublicKey);
                Assert.NotNull(request2.CertificateExtensions);
                Assert.NotEmpty(request2.CertificateExtensions);
                Assert.Equal(LeafCN, request2.SubjectName.Name);

                var signer = X509SignatureGenerator.CreateForRSA(rsa1, RSASignaturePadding.Pkcs1);
                var leaf = request2.Create(ca.SubjectName, signer, DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(1), serialNumber);
                Assert.Equal(leaf.Subject, LeafCN);
                Assert.Equal(new SerialNumber(serialNumber).ToString(), leaf.SerialNumber);
                Assert.NotNull(leaf.Extensions);
                Assert.Single(leaf.Extensions);
                Assert.NotNull(leaf.GetBasicConstraintsExtension());

                var buffer = request2.CreateSigningRequest();

                var info = buffer.ToCertificationRequestInfo();
                Assert.True(info.GetPublicKey().SameAs(ecdsa2.ToPublicKey()));
                Assert.Equal(LeafCN, new X500DistinguishedName(info.Subject.GetEncoded()).Name);

                var csr = buffer.ToCertificateRequest(SignatureType.PS256);
                //  var pubk = csr.PublicKey.Key;
                //  Assert.True(pubk.ToPublicKey().SameAs(rsa2.ToPublicKey()));
                Assert.Equal(LeafCN, csr.SubjectName.Name);

                var leaf2 = csr.Create(ca.SubjectName, signer, DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromHours(1), serialNumber);
                Assert.True(leaf2.GetECDsaPublicKey().ToPublicKey().SameAs(ecdsa2.ToPublicKey()));
                Assert.NotNull(leaf2.PublicKey);
                Assert.NotNull(leaf2.Extensions);
                Assert.Single(leaf2.Extensions);
                Assert.NotNull(leaf2.GetBasicConstraintsExtension());
                Assert.Equal(leaf.Subject, leaf2.Subject);
                Assert.Equal(leaf.SerialNumber, leaf2.SerialNumber);
            }
        }
    }
}
