// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Default {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto.Storage;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Autofac.Extras.Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;
    using Xunit;
    using Xunit.Sdk;
    using Autofac;

    /// <summary>
    /// Certificate Issuer tests
    /// </summary>
    public class CertificateIssuerTests {

        [Fact]
        public async Task ImportCertificateWithoutKeyTestAsync() {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();
                IKeyStore keys = mock.Create<KeyDatabase>();

                var now = DateTime.UtcNow;
#pragma warning disable IDE0067 // Dispose objects before losing scope
                var rkey = SignatureType.RS256.CreateCsr("CN=me", true, out var request);
#pragma warning restore IDE0067 // Dispose objects before losing scope
                var cert = request.CreateSelfSigned(now, now + TimeSpan.FromDays(5));

                // Run
                var rootca = await service.ImportCertificateAsync("rootca",
                    cert.ToCertificate(new IssuerPolicies {
                        SignatureType = SignatureType.RS256,
                        IssuedLifetime = TimeSpan.FromHours(1)
                    }));

                var found = await store.FindLatestCertificateAsync("rootca");

                // Assert
                Assert.NotNull(rootca);
                Assert.NotNull(found);
                Assert.Null(rootca.KeyHandle); // no key
                Assert.Null(rootca.Revoked);
                Assert.Equal(TimeSpan.FromDays(5), rootca.NotAfterUtc - rootca.NotBeforeUtc);
                // Cannot issue without private key
                Assert.Null(rootca.IssuerPolicies);
                Assert.False(rootca.IsIssuer());
                Assert.True(rootca.IsValidChain());
                rootca.Verify(rootca);
                Assert.True(rootca.IsSelfSigned());
                Assert.True(rootca.SameAs(found));
                Assert.NotNull(rootca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), rootca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(rootca.Issuer));
                using (var rcert = rootca.ToX509Certificate2()) {
                    Assert.Equal(rcert.GetSerialNumber(), rootca.GetSerialNumberAsBytesLE());
                    Assert.Equal(rcert.SerialNumber, rootca.GetSerialNumberAsString());
                    Assert.Equal(rcert.Thumbprint, rootca.Thumbprint);
                }
                Assert.Equal(rootca.GetSerialNumberAsString(), rootca.GetIssuerSerialNumberAsString());
            }
        }

        [Fact]
        public async Task ImportCertificateWithKeyTestAsync() {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();
                IKeyStore keys = mock.Create<KeyDatabase>();

                var now = DateTime.UtcNow;
                var rkey = SignatureType.RS256.CreateCsr("CN=me", true, out var request);
                var cert = request.CreateSelfSigned(now, now + TimeSpan.FromDays(5));

                // Run
                var rootca = await service.ImportCertificateAsync("rootca",
                    cert.ToCertificate(new IssuerPolicies {
                        SignatureType = SignatureType.RS256,
                        IssuedLifetime = TimeSpan.FromHours(1)
                    }), rkey.ToKey());

                var found = await store.FindLatestCertificateAsync("rootca");
                var export = keys.ExportKeyAsync(found.KeyHandle);

                // Assert
                Assert.NotNull(rootca);
                Assert.NotNull(found);
                Assert.NotNull(rootca.IssuerPolicies);
                Assert.NotNull(rootca.KeyHandle);
                await Assert.ThrowsAsync<InvalidOperationException>(() => export);
                Assert.Null(rootca.Revoked);
                Assert.Equal(TimeSpan.FromDays(5), rootca.NotAfterUtc - rootca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), rootca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(SignatureType.RS256, rootca.IssuerPolicies.SignatureType);
                Assert.True(rootca.IsValidChain());
                rootca.Verify(rootca);
                Assert.True(rootca.IsSelfSigned());
                Assert.True(rootca.IsIssuer());
                Assert.True(rootca.SameAs(found));
                Assert.NotNull(rootca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), rootca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(rootca.Issuer));
                using (var rcert = rootca.ToX509Certificate2()) {
                    Assert.Equal(rcert.GetSerialNumber(), rootca.GetSerialNumberAsBytesLE());
                    Assert.Equal(rcert.SerialNumber, rootca.GetSerialNumberAsString());
                    Assert.Equal(rcert.Thumbprint, rootca.Thumbprint);
                }
                Assert.Equal(rootca.GetSerialNumberAsString(), rootca.GetIssuerSerialNumberAsString());
            }
        }

        [Theory]
        [InlineData(SignatureType.RS256, 2048)]
        [InlineData(SignatureType.RS384, 4096)]
        [InlineData(SignatureType.RS384, 2048)]
        [InlineData(SignatureType.RS512, 2048)]
        [InlineData(SignatureType.PS256, 2048)]
        [InlineData(SignatureType.PS384, 2048)]
        [InlineData(SignatureType.PS512, 2048)]
        [InlineData(SignatureType.PS512, 4096)]
        public async Task CreateRSARootTestAsync(SignatureType signature, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();
                IKeyStore keys = mock.Create<KeyDatabase>();

                // Run
                var footca = await service.NewRootCertificateAsync("footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.RSA },
                    new IssuerPolicies {
                        SignatureType = signature,
                        IssuedLifetime = TimeSpan.FromHours(1)
                    });

                var found = await store.FindLatestCertificateAsync("footca");
                var export = keys.ExportKeyAsync(found.KeyHandle);

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.NotNull(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                await Assert.ThrowsAsync<InvalidOperationException>(() => export);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromDays(5), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(signature, footca.IssuerPolicies.SignatureType);
                Assert.True(footca.IsValidChain());
                footca.Verify(footca);
                Assert.True(footca.IsSelfSigned());
                Assert.True(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(footca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(footca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(footca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
            }
        }

        [Theory]
        [InlineData(SignatureType.ES256, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES256, CurveType.P384, 2048)]
        [InlineData(SignatureType.ES256, CurveType.P521, 2048)]
        [InlineData(SignatureType.ES384, CurveType.P521, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 4096)]
        // TODO: [InlineData(SignatureType.ES256, CurveType.Brainpool_P160r1)]
        public async Task CreateECCRootTestAsync(SignatureType signature, CurveType curve, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();
                IKeyStore keys = mock.Create<KeyDatabase>();

                // Run
                var footca = await service.NewRootCertificateAsync("footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.ECC, Curve = curve },
                    new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(1) });

                var found = await store.FindLatestCertificateAsync("footca");
                var export = keys.ExportKeyAsync(found.KeyHandle);

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.NotNull(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                await Assert.ThrowsAsync<InvalidOperationException>(() => export);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromDays(5), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(signature, footca.IssuerPolicies.SignatureType);
                footca.Verify(footca);
                Assert.True(footca.IsSelfSigned());
                Assert.True(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(footca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(footca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(footca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
            }
        }

        [Theory]
        //[InlineData(SignatureType.ES256, CurveType.P256, 2048)]
        //[InlineData(SignatureType.ES256, CurveType.P384, 2048)]
        //[InlineData(SignatureType.ES256, CurveType.P521, 2048)]
        //[InlineData(SignatureType.ES384, CurveType.P521, 2048)]
        //[InlineData(SignatureType.ES512, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 4096)]
        public async Task CreateECCRoot100TestAsync(SignatureType signature, CurveType curve, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();
                IKeyStore keys = mock.Create<KeyDatabase>();

                for (var i = 0; i < 100; i++) {
                    // Run
                    var footca = await service.NewRootCertificateAsync("footca",
                        X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow, TimeSpan.FromDays(5),
                        new CreateKeyParams { KeySize = keySize, Type = KeyType.ECC, Curve = curve },
                        new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(1) });

                    var found = await store.FindLatestCertificateAsync("footca");
                    var export = keys.ExportKeyAsync(found.KeyHandle);
                    footca.Verify(footca);
                    Assert.True(footca.IsSelfSigned());
                    Assert.True(footca.IsIssuer());
                    Assert.True(footca.SameAs(found));
                }
            }
        }

        [Fact]
        public async Task CreateRootTwiceTestAsync() {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();

                // Run
                var first = await service.NewRootCertificateAsync("footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow, TimeSpan.FromDays(3),
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P256 },
                    new IssuerPolicies { SignatureType = SignatureType.ES256, IssuedLifetime = TimeSpan.FromHours(1) });
                var footca = await service.NewRootCertificateAsync("footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                var found = await store.FindLatestCertificateAsync("footca");

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.NotNull(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromDays(5), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(SignatureType.RS256, footca.IssuerPolicies.SignatureType);
                footca.Verify(footca);
                Assert.True(footca.IsSelfSigned());
                Assert.True(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(footca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(footca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(footca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
            }
        }

        [Theory]
        [InlineData(SignatureType.RS256, 2048)]
        [InlineData(SignatureType.RS384, 4096)]
        [InlineData(SignatureType.RS384, 2048)]
        [InlineData(SignatureType.RS512, 2048)]
        [InlineData(SignatureType.PS256, 2048)]
        [InlineData(SignatureType.PS384, 2048)]
        [InlineData(SignatureType.PS512, 2048)]
        [InlineData(SignatureType.PS512, 4096)]
        public async Task CreateRSARootAndRSAIssuerTestAsync(SignatureType signature, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();

                // Run
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=thee"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.RSA },
                    new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(3) });
                var footca = await service.NewIssuerCertificateAsync("rootca", "footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                var found = await store.FindLatestCertificateAsync("footca");

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.NotNull(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromHours(3), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(SignatureType.RS256, footca.IssuerPolicies.SignatureType);
                Assert.False(footca.IsSelfSigned());
                Assert.True(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(rootca.SerialNumber, footca.IssuerSerialNumber);
                Assert.Equal(rootca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
                Assert.True(footca.IsValidChain(rootca.YieldReturn()));
            }
        }

        [Theory]
        [InlineData(SignatureType.RS256, 2048)]
        [InlineData(SignatureType.RS384, 4096)]
        [InlineData(SignatureType.RS384, 2048)]
        [InlineData(SignatureType.RS512, 2048)]
        [InlineData(SignatureType.PS256, 2048)]
        [InlineData(SignatureType.PS384, 2048)]
        [InlineData(SignatureType.PS512, 2048)]
        [InlineData(SignatureType.PS512, 4096)]
        public async Task CreateRSARootAndECCIssuerTestAsync(SignatureType signature, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();

                // Run
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=thee"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.RSA },
                    new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(3) });
                var footca = await service.NewIssuerCertificateAsync("rootca", "footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies {
                        SignatureType = SignatureType.ES384,
                        IssuedLifetime = TimeSpan.FromHours(1)
                    });

                var found = await store.FindLatestCertificateAsync("footca");

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.NotNull(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromHours(3), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(SignatureType.ES384, footca.IssuerPolicies.SignatureType);
                Assert.False(footca.IsSelfSigned());
                Assert.True(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(rootca.SerialNumber, footca.IssuerSerialNumber);
                Assert.Equal(rootca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
                Assert.True(footca.IsValidChain(rootca.YieldReturn()));
            }
        }

        [Theory]
        [InlineData(SignatureType.ES256, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES256, CurveType.P384, 2048)]
        [InlineData(SignatureType.ES256, CurveType.P521, 2048)]
        [InlineData(SignatureType.ES384, CurveType.P521, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 4096)]
        // TODO: [InlineData(SignatureType.ES256, CurveType.Brainpool_P160r1)]
        public async Task CreateECCRootAndRSAIssuerTestAsync(SignatureType signature, CurveType curve, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();

                // Run
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=thee"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.ECC, Curve = curve },
                    new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(3) });
                var footca = await service.NewIssuerCertificateAsync("rootca", "footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                var found = await store.FindLatestCertificateAsync("footca");

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.NotNull(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromHours(3), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(SignatureType.RS256, footca.IssuerPolicies.SignatureType);
                Assert.False(footca.IsSelfSigned());
                Assert.True(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(rootca.SerialNumber, footca.IssuerSerialNumber);
                Assert.Equal(rootca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
                Assert.True(footca.IsValidChain(rootca.YieldReturn()));
            }
        }

        [Theory]
        [InlineData(SignatureType.ES256, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES256, CurveType.P384, 2048)]
        [InlineData(SignatureType.ES256, CurveType.P521, 2048)]
        [InlineData(SignatureType.ES384, CurveType.P521, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 4096)]
        // TODO: [InlineData(SignatureType.ES256, CurveType.Brainpool_P160r1)]
        public async Task CreateECCRootAndECCIssuerTestAsync(SignatureType signature, CurveType curve, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();

                // Run
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=thee"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.ECC, Curve = curve },
                    new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(3) });
                var footca = await service.NewIssuerCertificateAsync("rootca", "footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.ECC, Curve = curve },
                    new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(1) });

                var found = await store.FindLatestCertificateAsync("footca");

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.NotNull(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromHours(3), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(signature, footca.IssuerPolicies.SignatureType);
                Assert.False(footca.IsSelfSigned());
                Assert.True(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(rootca.SerialNumber, footca.IssuerSerialNumber);
                Assert.Equal(rootca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
                Assert.True(footca.IsValidChain(rootca.YieldReturn()));
            }
        }

        [Fact]
        public async Task CreateRSARootAndRSAIssuerAndRSAIssuerTestAsync() {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'intca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "intca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();

                // Run
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=thee"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(3) });
                var intca = await service.NewIssuerCertificateAsync("rootca", "intca",
                    X500DistinguishedNameEx.Create("CN=be"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(2) });
                var footca = await service.NewIssuerCertificateAsync("intca", "footca",
                    X500DistinguishedNameEx.Create("CN=fee"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                var found = await store.FindLatestCertificateAsync("footca");
                var chain = await store.ListCompleteCertificateChainAsync(footca.SerialNumber);

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.NotNull(chain);
                Assert.NotNull(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromHours(2), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(SignatureType.RS256, footca.IssuerPolicies.SignatureType);
                Assert.False(footca.IsSelfSigned(), "Self signed");
                Assert.True(footca.IsIssuer(), "Not issuer");
                Assert.True(footca.SameAs(found), "Found is not root");
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(intca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(intca.Subject.SameAs(footca.Issuer), "Issuer not same as subject");
                Assert.Equal(intca.SerialNumber, footca.IssuerSerialNumber);
                Assert.Equal(intca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
                Assert.True(footca.HasValidSignature(intca), "No valid signature");

                Assert.Equal(3, chain.Count());
                Assert.Contains(chain, i => i.SameAs(footca));
                Assert.Contains(chain, i => i.SameAs(rootca));
                Assert.Contains(chain, i => i.SameAs(intca));

                Assert.True(intca.IsValidChain(rootca.YieldReturn(), out var status),
                      status.AsString("Intermediate chain invalid:"));

                // TODO: Mac crashes - windows only works if subject names are same - linux works only if they are not.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Assert.True(footca.IsValidChain(rootca.YieldReturn().Append(intca), out status),
                        status.AsString("Leaf chain invalid1:"));
                    Assert.True(footca.IsValidChain(rootca, intca), "Leaf chain invalid2");
                    Assert.True(footca.IsValidChain(chain, out status),
                        status.AsString("Leaf chain invalid3:"));
                }

                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
            }
        }

        [Theory]
        [InlineData(SignatureType.RS256, 2048)]
        [InlineData(SignatureType.RS384, 4096)]
        [InlineData(SignatureType.RS384, 2048)]
        [InlineData(SignatureType.RS512, 2048)]
        [InlineData(SignatureType.PS256, 2048)]
        [InlineData(SignatureType.PS384, 2048)]
        [InlineData(SignatureType.PS512, 2048)]
        [InlineData(SignatureType.PS512, 4096)]
        public async Task CreateRSACertificateAndPrivateKeyTestAsync(SignatureType signature, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();
                IKeyStore keys = mock.Create<KeyDatabase>();

                // Run
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=thee"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.RSA },
                    new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(3) });
                var footca = await service.CreateCertificateAndPrivateKeyAsync("rootca", "footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.RSA });

                var found = await store.FindLatestCertificateAsync("footca");
                var privateKey = await keys.ExportKeyAsync(found.KeyHandle);

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.Null(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                Assert.NotNull(privateKey);
                Assert.Equal(KeyType.RSA, privateKey.Type);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromHours(3), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.False(footca.IsSelfSigned());
                Assert.False(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(rootca.SerialNumber, footca.IssuerSerialNumber);
                Assert.Equal(rootca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
                Assert.True(footca.IsValidChain(rootca.YieldReturn()));
                Assert.True(footca.HasValidSignature(rootca));
            }
        }

        [Theory]
        [InlineData(SignatureType.ES256, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES256, CurveType.P384, 2048)]
        [InlineData(SignatureType.ES256, CurveType.P521, 2048)]
        [InlineData(SignatureType.ES384, CurveType.P521, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 2048)]
        [InlineData(SignatureType.ES512, CurveType.P256, 4096)]
        // TODO: [InlineData(SignatureType.ES256, CurveType.Brainpool_P160r1)]
        public async Task CreateECCCertificateAndPrivateKeyTestAsync(SignatureType signature,
            CurveType curveType, uint keySize) {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();
                IKeyStore keys = mock.Create<KeyDatabase>();

                // Run
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=thee"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.ECC, Curve = curveType },
                    new IssuerPolicies { SignatureType = signature, IssuedLifetime = TimeSpan.FromHours(3) });
                var footca = await service.CreateCertificateAndPrivateKeyAsync("rootca", "footca",
                    X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = keySize, Type = KeyType.ECC, Curve = curveType });

                var found = await store.FindLatestCertificateAsync("footca");
                var privateKey = await keys.ExportKeyAsync(found.KeyHandle);

                // Assert
                Assert.NotNull(footca);
                Assert.NotNull(found);
                Assert.Null(footca.IssuerPolicies);
                Assert.NotNull(footca.KeyHandle);
                Assert.NotNull(privateKey);
                Assert.Equal(KeyType.ECC, privateKey.Type);
                Assert.Null(footca.Revoked);
                Assert.Equal(TimeSpan.FromHours(3), footca.NotAfterUtc - footca.NotBeforeUtc);
                Assert.False(footca.IsSelfSigned());
                Assert.False(footca.IsIssuer());
                Assert.True(footca.SameAs(found));
                Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), footca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(footca.Issuer));
                using (var cert = footca.ToX509Certificate2()) {
                    Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                    Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                    Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                }
                Assert.Equal(rootca.SerialNumber, footca.IssuerSerialNumber);
                Assert.Equal(rootca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
                Assert.True(footca.IsValidChain(rootca.YieldReturn()));
                Assert.True(footca.HasValidSignature(rootca));
            }
        }

        [Fact]
        public async Task CreateRSASignedCertificateTestAsync() {

            using (var mock = Setup((v, q) => {
                var expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'footca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "footca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                expected = "SELECT TOP 1 * FROM Certificates c " +
                    "WHERE c.Type = 'Certificate' " +
                        "AND c.CertificateName = 'rootca' " +
                    "ORDER BY c.Version DESC";
                if (q == expected) {
                    return v
                        .Where(o => o.Value["Type"] == "Certificate")
                        .Where(o => o.Value["CertificateName"] == "rootca")
                        .OrderByDescending(o => o.Value["Version"]);
                }
                throw new AssertActualExpectedException(expected, q, "Query");
            })) {

                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                ICertificateStore store = mock.Create<CertificateDatabase>();

                // Run
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=thee"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(3) });

                using (var key = RSA.Create()) {
                    var footca = await service.CreateSignedCertificateAsync("rootca", "footca",
                        key.ToPublicKey(), X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow);

                    var found = await store.FindLatestCertificateAsync("footca");

                    // Assert
                    Assert.NotNull(footca);
                    Assert.NotNull(found);
                    Assert.Null(footca.IssuerPolicies);
                    Assert.Null(footca.KeyHandle);
                    Assert.Null(footca.Revoked);
                    Assert.Equal(TimeSpan.FromHours(3), footca.NotAfterUtc - footca.NotBeforeUtc);
                    Assert.False(footca.IsSelfSigned());
                    Assert.False(footca.IsIssuer());
                    Assert.True(footca.SameAs(found));
                    Assert.NotNull(footca.GetIssuerSerialNumberAsString());
                    Assert.Equal(rootca.GetSubjectName(), footca.GetIssuerSubjectName());
                    Assert.True(rootca.Subject.SameAs(footca.Issuer));
                    using (var cert = footca.ToX509Certificate2()) {
                        Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                        Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                        Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                    }
                    Assert.Equal(rootca.SerialNumber, footca.IssuerSerialNumber);
                    Assert.Equal(rootca.GetSerialNumberAsString(), footca.GetIssuerSerialNumberAsString());
                    Assert.True(footca.IsValidChain(rootca.YieldReturn()));
                    Assert.True(footca.HasValidSignature(rootca));
                }
            }
        }

        /// <summary>
        /// Setup mock
        /// </summary>
        /// <param name="mock"></param>
        /// <param name="provider"></param>
        private static AutoMock Setup(Func<IEnumerable<IDocumentInfo<VariantValue>>,
            string, IEnumerable<IDocumentInfo<VariantValue>>> provider) {
            var mock = AutoMock.GetLoose(builder => {
                builder.RegisterType<NewtonSoftJsonConverters>().As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonSoftJsonSerializer>().As<IJsonSerializer>();
                builder.RegisterInstance(new QueryEngineAdapter(provider)).As<IQueryEngine>();
                builder.RegisterType<MemoryDatabase>().SingleInstance().As<IDatabaseServer>();
                builder.RegisterType<ItemContainerFactory>().As<IItemContainerFactory>();
                builder.RegisterType<KeyDatabase>().As<IKeyStore>();
                builder.RegisterType<KeyHandleSerializer>().As<IKeyHandleSerializer>();
                builder.RegisterType<CertificateDatabase>().As<ICertificateStore>();
                builder.RegisterType<CertificateDatabase>().As<ICertificateRepository>();
                builder.RegisterType<CertificateFactory>().As<ICertificateFactory>();
            });

            return mock;
        }
    }
}

