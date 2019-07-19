// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Default {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto.Storage;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Newtonsoft.Json.Linq;
    using Autofac.Extras.Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Xunit;


    /// <summary>
    /// Certificate revoker tests
    /// </summary>
    public class CertificateRevokerTests {

        [Fact]
        public async Task RevokeRSAIssuerAndRSAIssuersTest() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, HandleQuery);
                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=rootca"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(3) });
                var intca = await service.NewIssuerCertificateAsync("rootca", "intca",
                    X500DistinguishedNameEx.Create("CN=intca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(2) });
                var footca1 = await service.NewIssuerCertificateAsync("intca", "footca1",
                    X500DistinguishedNameEx.Create("CN=footca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });
                var footca2 = await service.NewIssuerCertificateAsync("intca", "footca2",
                    X500DistinguishedNameEx.Create("CN=footca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                // Run
                ICertificateRevoker revoker = mock.Create<CertificateRevoker>();
                await revoker.RevokeCertificateAsync(intca.SerialNumber);

                ICertificateStore store = mock.Create<CertificateDatabase>();
                var foundi = await store.FindLatestCertificateAsync("intca");
                var found1 = await store.FindLatestCertificateAsync("footca1");
                var found2 = await store.FindLatestCertificateAsync("footca2");

                ICrlEndpoint crls = mock.Create<CrlDatabase>();
                var chainr = await crls.GetCrlChainAsync(rootca.SerialNumber);

                // Assert
                Assert.NotNull(foundi);
                Assert.NotNull(found1);
                Assert.NotNull(found2);
                Assert.NotNull(foundi.Revoked);
                Assert.NotNull(found1.Revoked);
                Assert.NotNull(found2.Revoked);
                Assert.NotNull(chainr);
                Assert.Single(chainr);
                Assert.True(chainr.Single().HasValidSignature(rootca));
            }
        }

        [Fact]
        public async Task RevokeECCIssuerAndECCIssuersTest() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, HandleQuery);
                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=rootca"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(3) });
                var intca = await service.NewIssuerCertificateAsync("rootca", "intca",
                    X500DistinguishedNameEx.Create("CN=intca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(2) });
                var footca1 = await service.NewIssuerCertificateAsync("intca", "footca1",
                    X500DistinguishedNameEx.Create("CN=footca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });
                var footca2 = await service.NewIssuerCertificateAsync("intca", "footca2",
                    X500DistinguishedNameEx.Create("CN=footca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                // Run
                ICertificateRevoker revoker = mock.Create<CertificateRevoker>();
                await revoker.RevokeCertificateAsync(intca.SerialNumber);

                ICertificateStore store = mock.Create<CertificateDatabase>();
                var foundi = await store.FindLatestCertificateAsync("intca");
                var found1 = await store.FindLatestCertificateAsync("footca1");
                var found2 = await store.FindLatestCertificateAsync("footca2");

                ICrlEndpoint crls = mock.Create<CrlDatabase>();
                // Get crl for root
                var chainr = await crls.GetCrlChainAsync(rootca.SerialNumber);

                // Assert
                Assert.NotNull(foundi);
                Assert.NotNull(found1);
                Assert.NotNull(found2);
                Assert.NotNull(foundi.Revoked);
                Assert.NotNull(found1.Revoked);
                Assert.NotNull(found2.Revoked);
                Assert.NotNull(chainr);
                Assert.Single(chainr);
                Assert.True(chainr.Single().HasValidSignature(rootca));
                Assert.True(chainr.Single().IsRevoked(intca));
            }
        }

        [Fact]
        public async Task RevokeRSAIssuersTest() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, HandleQuery);
                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=rootca"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(3) });
                var intca = await service.NewIssuerCertificateAsync("rootca", "intca",
                    X500DistinguishedNameEx.Create("CN=intca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(2) });
                var footca1 = await service.NewIssuerCertificateAsync("intca", "footca1",
                    X500DistinguishedNameEx.Create("CN=footca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });
                var footca2 = await service.NewIssuerCertificateAsync("intca", "footca2",
                    X500DistinguishedNameEx.Create("CN=footca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                // Run
                ICertificateRevoker revoker = mock.Create<CertificateRevoker>();
                await revoker.RevokeCertificateAsync(footca1.SerialNumber);
                await revoker.RevokeCertificateAsync(footca2.SerialNumber);

                ICertificateStore store = mock.Create<CertificateDatabase>();
                var foundi = await store.FindLatestCertificateAsync("intca");
                var found1 = await store.FindLatestCertificateAsync("footca1");
                var found2 = await store.FindLatestCertificateAsync("footca2");

                ICrlEndpoint crls = mock.Create<CrlDatabase>();
                // Get crl chain for intca and rootca
                var chainr = await crls.GetCrlChainAsync(intca.SerialNumber);

                // Assert
                Assert.NotNull(foundi);
                Assert.NotNull(found1);
                Assert.NotNull(found2);
                Assert.Null(foundi.Revoked);
                Assert.NotNull(found1.Revoked);
                Assert.NotNull(found2.Revoked);
                Assert.NotNull(chainr);
                Assert.NotEmpty(chainr);
                Assert.Equal(2, chainr.Count());
                Assert.True(chainr.ToArray()[1].HasValidSignature(intca));
                Assert.True(chainr.ToArray()[0].HasValidSignature(rootca));
                Assert.True(chainr.Last().IsRevoked(footca1));
                Assert.True(chainr.Last().IsRevoked(footca2));
                Assert.False(chainr.First().IsRevoked(intca));
            }
        }

        [Fact]
        public async Task RevokeECCIssuersTest() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                Setup(mock, HandleQuery);
                ICertificateIssuer service = mock.Create<CertificateIssuer>();
                var rootca = await service.NewRootCertificateAsync("rootca",
                    X500DistinguishedNameEx.Create("CN=rootca"), DateTime.UtcNow, TimeSpan.FromDays(5),
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(3) });
                var intca = await service.NewIssuerCertificateAsync("rootca", "intca",
                    X500DistinguishedNameEx.Create("CN=intca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(2) });
                var footca1 = await service.NewIssuerCertificateAsync("intca", "footca1",
                    X500DistinguishedNameEx.Create("CN=footca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });
                var footca2 = await service.NewIssuerCertificateAsync("intca", "footca2",
                    X500DistinguishedNameEx.Create("CN=footca"), DateTime.UtcNow,
                    new CreateKeyParams { KeySize = 2048, Type = KeyType.ECC, Curve = CurveType.P384 },
                    new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                // Run
                ICertificateRevoker revoker = mock.Create<CertificateRevoker>();
                await revoker.RevokeCertificateAsync(footca1.SerialNumber);
                await revoker.RevokeCertificateAsync(footca2.SerialNumber);

                ICertificateStore store = mock.Create<CertificateDatabase>();
                var foundi = await store.FindLatestCertificateAsync("intca");
                var found1 = await store.FindLatestCertificateAsync("footca1");
                var found2 = await store.FindLatestCertificateAsync("footca2");

                ICrlEndpoint crls = mock.Create<CrlDatabase>();
                var chainr = await crls.GetCrlChainAsync(intca.SerialNumber);

                // Assert
                Assert.NotNull(foundi);
                Assert.NotNull(found1);
                Assert.NotNull(found2);
                Assert.Null(foundi.Revoked);
                Assert.NotNull(found1.Revoked);
                Assert.NotNull(found2.Revoked);
                Assert.NotNull(chainr);
                Assert.NotEmpty(chainr);
                Assert.Equal(2, chainr.Count());
                Assert.True(chainr.ToArray()[1].HasValidSignature(intca));
                Assert.True(chainr.ToArray()[0].HasValidSignature(rootca));
            }
        }

        /// <summary>
        /// Query provider
        /// </summary>
        /// <param name="v"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        private IEnumerable<IDocumentInfo<JObject>> HandleQuery(
            IEnumerable<IDocumentInfo<JObject>> v, string q) {
            var expected = "SELECT TOP 1 * FROM Certificates c " +
                "WHERE c.Type = 'Certificate' " +
                    "AND c.CertificateName = 'intca' " +
                "ORDER BY c.Version DESC";
            if (q == expected) {
                return v
                    .Where(o => ((dynamic)o.Value).Type == "Certificate")
                    .Where(o => ((dynamic)o.Value).CertificateName == "intca")
                    .OrderByDescending(o => ((dynamic)o.Value).Version)
                    .Take(1);
            }
            expected = "SELECT TOP 1 * FROM Certificates c " +
                "WHERE c.Type = 'Certificate' " +
                    "AND c.CertificateName = 'footca1' " +
                "ORDER BY c.Version DESC";
            if (q == expected) {
                return v
                    .Where(o => ((dynamic)o.Value).Type == "Certificate")
                    .Where(o => ((dynamic)o.Value).CertificateName == "footca1")
                    .OrderByDescending(o => ((dynamic)o.Value).Version)
                    .Take(1);
            }
            expected = "SELECT TOP 1 * FROM Certificates c " +
                "WHERE c.Type = 'Certificate' " +
                    "AND c.CertificateName = 'footca2' " +
                "ORDER BY c.Version DESC";
            if (q == expected) {
                return v
                    .Where(o => ((dynamic)o.Value).Type == "Certificate")
                    .Where(o => ((dynamic)o.Value).CertificateName == "footca2")
                    .OrderByDescending(o => ((dynamic)o.Value).Version)
                    .Take(1);
            }
            expected = "SELECT TOP 1 * FROM Certificates c " +
                "WHERE c.Type = 'Certificate' " +
                    "AND c.CertificateName = 'rootca' " +
                "ORDER BY c.Version DESC";
            if (q == expected) {
                return v
                    .Where(o => ((dynamic)o.Value).Type == "Certificate")
                    .Where(o => ((dynamic)o.Value).CertificateName == "rootca")
                    .OrderByDescending(o => ((dynamic)o.Value).Version);
            }
            expected = "SELECT TOP 1 * FROM Certificates c " +
                "WHERE c.Type = 'Certificate' " +
                    "AND c.CertificateId = 'intca' " +
                "ORDER BY c.Version DESC";
            if (q == expected) {
                return v
                    .Where(o => ((dynamic)o.Value).Type == "Certificate")
                    .Where(o => ((dynamic)o.Value).CertificateName == "intca")
                    .OrderByDescending(o => ((dynamic)o.Value).Version)
                    .Take(1);
            }
            expected = "SELECT TOP 1 * FROM Certificates c " +
                "WHERE c.Type = 'Certificate' " +
                    "AND c.CertificateId = 'footca1' " +
                "ORDER BY c.Version DESC";
            if (q == expected) {
                return v
                    .Where(o => ((dynamic)o.Value).Type == "Certificate")
                    .Where(o => ((dynamic)o.Value).CertificateName == "footca1")
                    .OrderByDescending(o => ((dynamic)o.Value).Version)
                    .Take(1);
            }
            expected = "SELECT TOP 1 * FROM Certificates c " +
                "WHERE c.Type = 'Certificate' " +
                    "AND c.CertificateId = 'footca2' " +
                "ORDER BY c.Version DESC";
            if (q == expected) {
                return v
                    .Where(o => ((dynamic)o.Value).Type == "Certificate")
                    .Where(o => ((dynamic)o.Value).CertificateName == "footca2")
                    .OrderByDescending(o => ((dynamic)o.Value).Version)
                    .Take(1);
            }
            expected = "SELECT TOP 1 * FROM Certificates c " +
                "WHERE c.Type = 'Certificate' " +
                    "AND c.CertificateId = 'rootca' " +
                "ORDER BY c.Version DESC";
            if (q == expected) {
                return v
                    .Where(o => ((dynamic)o.Value).Type == "Certificate")
                    .Where(o => ((dynamic)o.Value).CertificateName == "rootca")
                    .OrderByDescending(o => ((dynamic)o.Value).Version)
                    .Take(1);
            }

            var results = v.Where(o => ((dynamic)o.Value).Type == "Certificate");
            if (q.Contains("c.Issuer = 'CN=rootca'")) {
                results = results.Where(o => ((dynamic)o.Value).Issuer == "CN=rootca");
            }
            if (q.Contains("c.Issuer = 'CN=intca'")) {
                results = results.Where(o => ((dynamic)o.Value).Issuer == "CN=intca");
            }
            if (q.Contains("c.Issuer = 'CN=footca'")) {
                results = results.Where(o => ((dynamic)o.Value).Issuer == "CN=footca");
            }
            if (q.Contains("AND NOT IS_DEFINED(c.DisabledSince)")) {
                results = results.Where(o => ((dynamic)o.Value).DisabledSince == null);
            }
            else if (q.Contains("AND IS_DEFINED(c.DisabledSince)")) {
                results = results.Where(o => ((dynamic)o.Value).DisabledSince != null);
            }
            return results.OrderByDescending(o => ((dynamic)o.Value).Version);
        }

        /// <summary>
        /// Setup mock
        /// </summary>
        /// <param name="mock"></param>
        /// <param name="provider"></param>
        private static void Setup(AutoMock mock, Func<IEnumerable<IDocumentInfo<JObject>>,
            string, IEnumerable<IDocumentInfo<JObject>>> provider) {
            mock.Provide<IQueryEngine>(new QueryEngineAdapter(provider));
            mock.Provide<IDatabaseServer, MemoryDatabase>();
            mock.Provide<IItemContainerFactory, ItemContainerFactory>();
            mock.Provide<IKeyStore, KeyDatabase>();
            mock.Provide<IDigestSigner, KeyDatabase>();
            mock.Provide<IKeyHandleSerializer, KeyHandleSerializer>();
            mock.Provide<ICertificateStore, CertificateDatabase>();
            mock.Provide<ICertificateRepository, CertificateDatabase>();
            mock.Provide<ICertificateFactory, CertificateFactory>();
            mock.Provide<ICrlRepository, CrlDatabase>();
            mock.Provide<ICertificateIssuer, CertificateIssuer>();
            mock.Provide<ICrlFactory, CrlFactory>();
        }
    }
}

