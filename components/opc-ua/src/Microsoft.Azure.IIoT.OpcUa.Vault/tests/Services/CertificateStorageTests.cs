// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Tests {

#if FALSE
   [TestCaseOrderer("TestCaseOrdering.PriorityOrderer", "Microsoft.Azure.IIoT.OpcUa.Vault.Tests")]
    public class CertificateStorageTests : IClassFixture<CertificateStorageTestFixture> {
        public CertificateStorageTests(CertificateStorageTestFixture fixture, ITestOutputHelper log) {
            _logger = SerilogTestLogger.Create<CertificateStorageTests>(log);
            _fixture = fixture;
            _services = _fixture.Services;
            _registry = _fixture.Registry;
            _fixture.SkipOnInvalidConfiguration();
        }

        /// <summary>
        /// Initialize the cert group once and for all tests.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1)]
        public void KeyVaultInit() {
            _logger.Information("Initializing KeyVault");
            if (_services is Autofac.IStartable start) {
                start.Start();
            }
            _fixture.KeyVaultInitOk = true;
        }

        /// <summary>
        /// Purge the KeyVault from all certificates and secrets touched by this test.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(100)]
        public async Task KeyVaultPurgeCACertificateAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            await _fixture.PurgeAsync();
        }

        /// <summary>
        /// Create a new IssuerCA Certificate with CRL according to the group configuration.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(200)]
        public async Task KeyVaultCreateCACertificateAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groups = await _registry.ListGroupIdsAsync(null, null);
            foreach (var group in groups.Groups) {
                var result = await _services.RenewIssuerCertificateAsync(group);
                Assert.NotNull(result);
                Assert.False(result.ToStackModel().HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(result.ToStackModel().Issuer, result.Subject));
                var basicConstraints = result.ToStackModel().GetBasicConstraintsExtension();
                Assert.NotNull(basicConstraints);
                Assert.True(basicConstraints.CertificateAuthority);
                Assert.True(basicConstraints.Critical);
                var subjectKeyId = result.ToStackModel().Extensions.OfType<X509SubjectKeyIdentifierExtension>().Single();
                Assert.False(subjectKeyId.Critical);
                var authorityKeyIdentifier = result.ToStackModel().GetAuthorityKeyIdentifierExtension();
                Assert.NotNull(authorityKeyIdentifier);
                Assert.False(authorityKeyIdentifier.Critical);
                Assert.Equal(authorityKeyIdentifier.SerialNumber.ToBase16String(), result.SerialNumber, true);
                Assert.Equal(authorityKeyIdentifier.KeyId, subjectKeyId.SubjectKeyIdentifier, true);
            }
        }

        /// <summary>
        /// Read the list of groud ids supported in the configuration.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(400)]
        public async Task KeyVaultListOfCertGroups() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groups = await _registry.ListGroupIdsAsync(null, null);
            Assert.NotNull(groups);
            Assert.NotEmpty(groups.Groups);
        }

        /// <summary>
        /// Read all certificate group configurations.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(400)]
        public async Task KeyVaultGroupConfigurationCollection() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groupCollection = await _registry.ListGroupsAsync(null, null);
            Assert.NotNull(groupCollection);
            Assert.NotEmpty(groupCollection.Groups);
            foreach (var groupConfig in groupCollection.Groups) {
                Assert.NotNull(groupConfig.Id);
                Assert.NotEmpty(groupConfig.Id);
                Assert.NotNull(groupConfig.SubjectName);
                Assert.NotEmpty(groupConfig.SubjectName);
            }
        }

        /// <summary>
        /// Read the Issuer CA Certificate and CRL Chain for each group.
        /// </summary>
        /// <returns></returns>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(400)]
        public async Task KeyVaultGetCertificateAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groups = await _registry.ListGroupIdsAsync(null, null);
            foreach (var group in groups.Groups) {
                var caChain = await _services.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(caChain);
                Assert.NotNull(caChain.Chain);
                Assert.True(caChain.Chain.Count >= 1);
                foreach (var caCert in caChain.Chain) {
                    Assert.False(caCert.ToStackModel().HasPrivateKey);
                }
                var crlChain = await _services.GetIssuerCACrlChainAsync(group);
                Assert.NotNull(crlChain);
                Assert.True(crlChain.Chain.Count >= 1);
                for (var i = 0; i < caChain.Chain.Count; i++) {
                    crlChain.Chain[i].ToStackModel().Validate(caChain.Chain[i].ToStackModel());
                    Assert.True(Opc.Ua.Utils.CompareDistinguishedName(
                        crlChain.Chain[i].Issuer, caChain.Chain[i].ToStackModel().Issuer));
                }
            }
        }

        /// <summary>
        /// Create a new key pair with a issuer signed certificate in KeyVault.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(500)]
        public async Task<X509CertificateCollection> KeyVaultNewKeyPairRequestAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var certCollection = new X509CertificateCollection();
            var groups = await _registry.ListGroupIdsAsync(null, null);
            foreach (var group in groups.Groups) {
                var randomApp = _fixture.RandomGenerator.RandomApplicationTestData();
                var requestId = Guid.NewGuid();
                var newKeyPair = await _services.CreateNewCertificateAsync(
                    "cert",
                    requestId.ToString(),
                    new Registry.Models.ApplicationInfoModel {
                        ApplicationUri = randomApp.ApplicationRecord.ApplicationUri,
                    },
                    randomApp.Subject,
                    randomApp.DomainNames.ToArray());
                Assert.NotNull(newKeyPair);
                Assert.False(newKeyPair.Certificate.ToStackModel().HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(randomApp.Subject, newKeyPair.Certificate.Subject));
                Assert.False(Opc.Ua.Utils.CompareDistinguishedName(
                    newKeyPair.Certificate.ToStackModel().Issuer, newKeyPair.Certificate.Subject));
                var issuerCerts = await _services.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(issuerCerts);
                Assert.True(issuerCerts.Chain.Count >= 1);

                X509TestUtils.VerifyApplicationCertIntegrity(
                    newKeyPair.Certificate.ToStackModel(),
                    newKeyPair.PrivateKey.ToKey(),
                    issuerCerts.ToStackModel());
                certCollection.Add(newKeyPair.Certificate.ToStackModel());

                // disable and delete private key from KeyVault (requires set/delete rights)
                await _services.AcceptPrivateKeyAsync(requestId.ToString());
                await _services.DeletePrivateKeyAsync(requestId.ToString());
            }
            return certCollection;
        }

        /// <summary>
        /// Create a new issuer signed certificate from a CSR in KeyVault.
        /// Validate the signed certificate aginst the issuer CA chain.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(500)]
        public async Task<X509CertificateCollection> KeyVaultSigningRequestAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var certCollection = new X509CertificateCollection();
            var groups = await _registry.ListGroupIdsAsync(null, null);
            foreach (var group in groups.Groups) {
                var certificateGroupConfiguration = await _registry.GetGroupInfoAsync(group);
                var randomApp = _fixture.RandomGenerator.RandomApplicationTestData();
                var csrCertificate = CertificateFactory.CreateCertificate(
                    null, null, null,
                    randomApp.ApplicationRecord.ApplicationUri,
                    null,
                    randomApp.Subject,
                    randomApp.DomainNames.ToArray(),
                    certificateGroupConfiguration.DefaultCertificateKeySize,
                    DateTime.UtcNow.AddDays(-10),
                    certificateGroupConfiguration.DefaultCertificateLifetime,
                    certificateGroupConfiguration.DefaultCertificateHashSize
                    );
                var certificateRequest = CertificateFactory.CreateSigningRequest(
                    csrCertificate, randomApp.DomainNames);

                var newCert = await _services.ProcessSigningRequestAsync(
                    "cert",
                    new Registry.Models.ApplicationInfoModel {
                        ApplicationUri = randomApp.ApplicationRecord.ApplicationUri,
                    },
                    certificateRequest);
                // get issuer cert used for signing
                var issuerCerts = await _services.GetIssuerCACertificateChainAsync(group);
#if WRITECERT
                // save cert for debugging
                using (var store = Opc.Ua.CertificateStoreIdentifier.CreateStore(
                    Opc.Ua.CertificateStoreType.Directory)) {
                    Assert.NotNull(store);
                    store.Open("d:\\unittest");
                    await store.Add(newCert.ToStackModel());
                    foreach (var cert in issuerCerts.ToStackModel()) await store.Add(cert);
                }
#endif
                Assert.NotNull(issuerCerts);
                Assert.True(issuerCerts.Chain.Count >= 1);
                X509TestUtils.VerifySignedApplicationCert(
                    randomApp, newCert.ToStackModel(), issuerCerts.ToStackModel());
                certCollection.Add(newCert.ToStackModel());
            }
            return certCollection;
        }

        /// <summary>
        /// Create a new key pair with a issuer signed certificate in KeyVault.
        /// Validate the signed certificate, then revoke it. Then verify revocation.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(600)]
        public async Task KeyVaultNewKeyPairAndRevokeCertificateAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groups = await _registry.ListGroupIdsAsync(null, null);
            foreach (var group in groups.Groups) {
                var randomApp = _fixture.RandomGenerator.RandomApplicationTestData();
                var requestId = Guid.NewGuid();
                var newCert = await _services.CreateNewCertificateAsync(

                    requestId.ToString(),
                    new Registry.Models.ApplicationInfoModel {
                        ApplicationUri = randomApp.ApplicationRecord.ApplicationUri,
                    },
                    randomApp.Subject,
                    randomApp.DomainNames.ToArray()
                    );
                Assert.NotNull(newCert);
                Assert.False(newCert.Certificate.ToStackModel().HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(randomApp.Subject, newCert.Certificate.Subject));
                Assert.False(Opc.Ua.Utils.CompareDistinguishedName(
                    newCert.Certificate.ToStackModel().Issuer, newCert.Certificate.Subject));
                var cert = new X509Certificate2(newCert.Certificate.ToRawData());
                await _services.RevokeCertificateAsync(cert.ToServiceModel());
                var crl = _services.GetCrlAsync(group);
                Assert.NotNull(crl);
                var caChain = await _services.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(caChain);
                var caCert = caChain.Chain[0];
                Assert.False(caCert.ToStackModel().HasPrivateKey);
                crl.ToStackModel().Validate(caCert.ToStackModel());
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(crl.Issuer, caCert.ToStackModel().Issuer));
                // disable and delete private key from KeyVault (requires set/delete rights)
                await _services.AcceptPrivateKeyAsync(requestId.ToString());
                await _services.DeletePrivateKeyAsync(requestId.ToString());
            }
        }

        /// <summary>
        /// Create a new key pair with a issuer signed certificate in KeyVault.
        /// Load the private key and validate the public/private key.
        /// Accept and delete the private. Verify the private kay is deleted.
        /// </summary>
        /// <returns></returns>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(600)]
        public async Task KeyVaultNewKeyPairLoadThenDeletePrivateKeyAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groups = await _registry.ListGroupIdsAsync(null, null);
            foreach (var group in groups.Groups) {
                var randomApp = _fixture.RandomGenerator.RandomApplicationTestData();
                var requestId = Guid.NewGuid();
                var newKeyPair = await _services.CreateNewCertificateAsync(

                    requestId.ToString(),
new Registry.Models.ApplicationInfoModel {
    ApplicationUri = randomApp.ApplicationRecord.ApplicationUri,
}, randomApp.Subject,
                    randomApp.DomainNames.ToArray()
                    );
                Assert.NotNull(newKeyPair);
                Assert.False(newKeyPair.Certificate.ToStackModel().HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(randomApp.Subject,
                    newKeyPair.Certificate.Subject));
                Assert.False(Opc.Ua.Utils.CompareDistinguishedName(
                    newKeyPair.Certificate.ToStackModel().Issuer, newKeyPair.Certificate.Subject));

                var issuerCerts = await _services.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(issuerCerts);
                Assert.True(issuerCerts.Chain.Count >= 1);

                X509TestUtils.VerifyApplicationCertIntegrity(
                    newKeyPair.Certificate.ToStackModel(),
                    newKeyPair.PrivateKey.ToKey(),
                    issuerCerts.ToStackModel()
                    );

                // test to load the key from KeyVault
                var privateKey = await _services.RetrievePrivateKeyAsync(requestId.ToString());
                Assert.True(privateKey.HasPrivateKey());

                var privateKeyX509 = newKeyPair.Certificate.ToStackModel();
                privateKeyX509.PrivateKey = privateKey.ToRSA();
                Assert.True(privateKeyX509.HasPrivateKey);

                X509TestUtils.VerifyApplicationCertIntegrity(
                    newKeyPair.Certificate.ToStackModel(),
                    privateKey,
                    issuerCerts.ToStackModel()
                    );

                await _services.AcceptPrivateKeyAsync(requestId.ToString());
                await Assert.ThrowsAsync<KeyVaultErrorException>(async () => privateKey =
                await _services.RetrievePrivateKeyAsync(requestId.ToString()));
                await _services.AcceptPrivateKeyAsync(requestId.ToString());
                await _services.DeletePrivateKeyAsync(requestId.ToString());
                await Assert.ThrowsAsync<KeyVaultErrorException>(() =>
                    _services.DeletePrivateKeyAsync(requestId.ToString()));
                await Assert.ThrowsAsync<KeyVaultErrorException>(async () => privateKey =
                await _services.RetrievePrivateKeyAsync(requestId.ToString()));
            }
        }

        /// <summary>
        /// Get the certificate versions for every try paging..
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(3000)]
        public async Task GetCertificateVersionsAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groups = await _registry.ListGroupIdsAsync(null, null);
            foreach (var group in groups.Groups) {
                // read all certs
                var certCollection = await _services.ListIssuerCACertificateVersionsAsync(
                    null, 2);
                while (certCollection.NextPageLink != null) {
                    var next = await _services.ListIssuerCACertificateVersionsAsync(
                        certCollection.NextPageLink, 2);
                    certCollection.AddRange(next);
                    certCollection.NextPageLink = next.NextPageLink;
                }

                // read all matching cert and crl by thumbprint
                var chainId = await _services.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(chainId);
                Assert.True(chainId.Chain.Count >= 1);
                var crlId = await _services.GetIssuerCACrlChainAsync(group);
                Assert.NotNull(chainId);
                Assert.True(chainId.Chain.Count >= 1);
                foreach (var cert in certCollection.Chain) {
                    var certChain = await _services.GetIssuerCACertificateChainAsync(
                        cert.Thumbprint);
                    Assert.NotNull(certChain);
                    Assert.True(certChain.Chain.Count >= 1);
                    Assert.Equal(cert.Thumbprint, certChain.Chain[0].Thumbprint);

                    var crlChain = await _services.GetIssuerCACrlChainAsync(cert.Thumbprint);
                    Assert.NotNull(crlChain);
                    Assert.True(crlChain.Chain.Count >= 1);
                    crlChain.Chain[0].ToStackModel().Validate(cert.ToStackModel());
                    crlChain.Chain[0].ToStackModel().Validate(certChain.Chain[0].ToStackModel());

                    // invalid parameter test
                    // invalid parameter test
                    await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                        _services.GetIssuerCACrlChainAsync(cert.Thumbprint + "a"));
                    await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                        _services.GetIssuerCACrlChainAsync("abc", cert.Thumbprint));
                }

                // invalid parameters
                await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                    _services.GetIssuerCACrlChainAsync("abcd"));
                await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                    _services.GetIssuerCACertificateChainAsync("abc"));
                await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                    _services.GetIssuerCACrlChainAsync("abc"));
            }
        }

        /// <summary>
        /// Read the trust list for every group.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(3000)]
        public async Task GetTrustListAsync() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groups = await _registry.ListGroupIdsAsync(null, null);
            foreach (var group in groups.Groups) {
                var trustList = await _services.GetGroupTrustListAsync(null, 2);
                var nextPageLink = trustList.NextPageLink;
                while (nextPageLink != null) {
                    var nextTrustList = await _services.GetGroupTrustListAsync(nextPageLink, 2);
                    trustList.AddRange(nextTrustList);
                    nextPageLink = nextTrustList.NextPageLink;
                }
                var validator = X509TestUtils.CreateValidatorAsync(trustList);
            }
        }

        /// <summary>
        /// Create new CA, create a few signed Certs and key pairs.
        /// Repeat. Then revoke all, validate the revocation for each CA cert in the issuer CA history.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(2000)]
        public async Task CreateCAAndAppCertificatesThenRevokeAll() {
            Skip.If(!_fixture.KeyVaultInitOk);
            var certCollection = new X509Certificate2Collection();
            for (var i = 0; i < 3; i++) {
                await KeyVaultCreateCACertificateAsync();
                for (var v = 0; v < 10; v++) {
                    certCollection.AddRange(await KeyVaultSigningRequestAsync());
                    certCollection.AddRange(await KeyVaultNewKeyPairRequestAsync());
                }
            }

            var groups = await _registry.ListGroupIdsAsync(null, null);

            // validate all certificates
            foreach (var group in groups.Groups) {
                var trustList = await _services.GetGroupTrustListAsync(group);
                var nextPageLink = trustList.NextPageLink;
                while (nextPageLink != null) {
                    var nextTrustList = await _services.GetGroupTrustListAsync(nextPageLink);
                    trustList.AddRange(nextTrustList);
                    nextPageLink = nextTrustList.NextPageLink;
                }
                var validator = await X509TestUtils.CreateValidatorAsync(trustList);
                foreach (var cert in certCollection) {
                    validator.Validate(cert);
                }
            }

            // now revoke all certifcates
            var revokeCertificates = new X509Certificate2Collection(certCollection).ToServiceModel(null);
            foreach (var group in groups.Groups) {
                var unrevokedCertificates = await _services.RevokeCertificatesAsync(revokeCertificates);
                Assert.True(unrevokedCertificates.Chain.Count <= revokeCertificates.Chain.Count);
                revokeCertificates = unrevokedCertificates;
            }
            Assert.Empty(revokeCertificates.Chain);

            // reload updated trust list from KeyVault
            var trustListAllGroups = new TrustListModel {
                GroupId = "all"
            };
            foreach (var group in groups.Groups) {
                var trustList = await _services.GetGroupTrustListAsync(group);
                var nextPageLink = trustList.NextPageLink;
                while (nextPageLink != null) {
                    var nextTrustList = await _services.GetGroupTrustListAsync(nextPageLink);
                    trustList.AddRange(nextTrustList);
                    nextPageLink = nextTrustList.NextPageLink;
                }
                trustListAllGroups.AddRange(trustList);
            }

            // verify certificates are revoked
            {
                var validator = await X509TestUtils.CreateValidatorAsync(trustListAllGroups);
                foreach (var cert in certCollection) {
                    Assert.Throws<Opc.Ua.ServiceResultException>(() => validator.Validate(cert));
                }
            }
        }

        private readonly CertificateStorageTestFixture _fixture;
        private readonly ICertificateDirectory _services;
        private readonly ICertificateGroupManager _registry;
        private readonly ILogger _logger;
}
#endif
}
