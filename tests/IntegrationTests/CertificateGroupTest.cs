// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.IIoT.Auth.Clients;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Opc.Ua;
using TestCaseOrdering;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test
{

    public class CertificateGroupTestFixture : IDisposable
    {
        IConfigurationRoot _configuration;
        ServicesConfig _serviceConfig = new ServicesConfig();
        IClientConfig _clientConfig = new ClientConfig();
        TraceLogger _logger = new TraceLogger(new LogConfig());
        public ApplicationTestDataGenerator RandomGenerator;
        public KeyVaultCertificateGroup KeyVault;
        public bool KeyVaultInitOk;

        const int _randomStart = 3388;
        const int _testSetSize = 10;

        public CertificateGroupTestFixture()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("testsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            _configuration = builder.Build();
            _configuration.Bind("OpcVault", _serviceConfig);
            _configuration.Bind("Auth", _clientConfig);
            if (!InvalidConfiguration())
            {
                RandomGenerator = new ApplicationTestDataGenerator();
                KeyVault = new KeyVaultCertificateGroup(_serviceConfig, _clientConfig, _logger);
            }
            KeyVaultInitOk = false;
        }

        public void SkipOnInvalidConfiguration()
        {
            Skip.If(InvalidConfiguration(), "Missing valid KeyVault configuration.");
        }

        private bool InvalidConfiguration()
        {
            return
                _serviceConfig.KeyVaultBaseUrl == null ||
                _serviceConfig.KeyVaultResourceId == null ||
                _clientConfig.AppId == null ||
                _clientConfig.AppSecret == null;
        }

        public void Dispose()
        {
        }
    }

    [TestCaseOrderer("TestCaseOrdering.PriorityOrderer", "Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test")]
    public class CertificateGroupTest : IClassFixture<CertificateGroupTestFixture>
    {
        CertificateGroupTestFixture _fixture;
        KeyVaultCertificateGroup _keyVault;

        public CertificateGroupTest(CertificateGroupTestFixture fixture, ITestOutputHelper log)
        {
            _log = log;
            _fixture = fixture;
            _keyVault = _fixture.KeyVault;
            _fixture.SkipOnInvalidConfiguration();
        }

        /// <summary>
        /// Initialize the cert group once and for all tests.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1)]
        public async Task KeyVaultInit()
        {
            await _keyVault.Init();
            _fixture.KeyVaultInitOk = true;
        }

        /// <summary>
        /// Purge the KeyVault from all certificates and secrets touched by this test.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(100)]
        private async Task KeyVaultPurgeCACertificateAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            await _keyVault.PurgeAsync();
        }

        /// <summary>
        /// Create a new IssuerCA Certificate with CRL according to the group configuration.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(200)]
        public async Task KeyVaultCreateCACertificateAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            string[] groups = await _keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                X509Certificate2 result = await _keyVault.CreateIssuerCACertificateAsync(group);
                Assert.NotNull(result);
                Assert.False(result.HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(result.Issuer, result.Subject));
                X509BasicConstraintsExtension basicConstraints = X509TestUtils.FindBasicConstraintsExtension(result);
                Assert.NotNull(basicConstraints);
                Assert.True(basicConstraints.CertificateAuthority);
                Assert.True(basicConstraints.Critical);
                var subjectKeyId = result.Extensions.OfType<X509SubjectKeyIdentifierExtension>().Single();
                Assert.False(subjectKeyId.Critical);
                var authorityKeyIdentifier = X509TestUtils.FindAuthorityKeyIdentifier(result);
                Assert.NotNull(authorityKeyIdentifier);
                Assert.False(authorityKeyIdentifier.Critical);
                Assert.Equal(authorityKeyIdentifier.SerialNumber, result.SerialNumber, ignoreCase: true);
                Assert.Equal(authorityKeyIdentifier.KeyId, subjectKeyId.SubjectKeyIdentifier, ignoreCase: true);
            }
        }

        /// <summary>
        /// Read the list of groud ids supported in the configuration.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(400)]
        public async Task KeyVaultListOfCertGroups()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            string[] groups = await _keyVault.GetCertificateGroupIds();
            Assert.NotNull(groups);
            Assert.NotEmpty(groups);
        }

        /// <summary>
        /// Read all certificate group configurations.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(400)]
        public async Task KeyVaultGroupConfigurationCollection()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            var groupCollection = await _keyVault.GetCertificateGroupConfigurationCollection();
            Assert.NotNull(groupCollection);
            Assert.NotEmpty(groupCollection);
            foreach (var groupConfig in groupCollection)
            {
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
        public async Task KeyVaultGetCertificateAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            string[] groups = await _keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                X509Certificate2Collection caChain = await _keyVault.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(caChain);
                Assert.True(caChain.Count >= 1);
                foreach (X509Certificate2 caCert in caChain)
                {
                    Assert.False(caCert.HasPrivateKey);
                }
                System.Collections.Generic.IList<X509CRL> crlChain = await _keyVault.GetIssuerCACrlChainAsync(group);
                Assert.NotNull(crlChain);
                Assert.True(crlChain.Count >= 1);
                for (int i = 0; i < caChain.Count; i++)
                {
                    crlChain[i].VerifySignature(caChain[i], true);
                    Assert.True(Opc.Ua.Utils.CompareDistinguishedName(crlChain[i].Issuer, caChain[i].Issuer));
                }
            }
        }

        /// <summary>
        /// Create a new key pair with a issuer signed certificate in KeyVault.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(500)]
        public async Task<X509CertificateCollection> KeyVaultNewKeyPairRequestAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            X509CertificateCollection certCollection = new X509CertificateCollection();
            string[] groups = await _keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                ApplicationTestData randomApp = _fixture.RandomGenerator.RandomApplicationTestData();
                Guid requestId = Guid.NewGuid();
                Opc.Ua.Gds.Server.X509Certificate2KeyPair newKeyPair = await _keyVault.NewKeyPairRequestAsync(
                    group,
                    requestId.ToString(),
                    randomApp.ApplicationRecord.ApplicationUri,
                    randomApp.Subject,
                    randomApp.DomainNames.ToArray(),
                    randomApp.PrivateKeyFormat,
                    randomApp.PrivateKeyPassword);
                Assert.NotNull(newKeyPair);
                Assert.False(newKeyPair.Certificate.HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(randomApp.Subject, newKeyPair.Certificate.Subject));
                Assert.False(Opc.Ua.Utils.CompareDistinguishedName(newKeyPair.Certificate.Issuer, newKeyPair.Certificate.Subject));
                X509Certificate2Collection issuerCerts = await _keyVault.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(issuerCerts);
                Assert.True(issuerCerts.Count >= 1);

                X509TestUtils.VerifyApplicationCertIntegrity(
                    newKeyPair.Certificate,
                    newKeyPair.PrivateKey,
                    randomApp.PrivateKeyPassword,
                    randomApp.PrivateKeyFormat,
                    issuerCerts
                    );
                certCollection.Add(newKeyPair.Certificate);

                // disable and delete private key from KeyVault (requires set/delete rights)
                await _keyVault.AcceptPrivateKeyAsync(group, requestId.ToString());
                await _keyVault.DeletePrivateKeyAsync(group, requestId.ToString());
            }
            return certCollection;
        }

        /// <summary>
        /// Create a new issuer signed certificate from a CSR in KeyVault.
        /// Validate the signed certificate aginst the issuer CA chain.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(500)]
        public async Task<X509CertificateCollection> KeyVaultSigningRequestAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            X509CertificateCollection certCollection = new X509CertificateCollection();
            string[] groups = await _keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                var certificateGroupConfiguration = await _keyVault.GetCertificateGroupConfiguration(group);
                ApplicationTestData randomApp = _fixture.RandomGenerator.RandomApplicationTestData();
                X509Certificate2 csrCertificate = CertificateFactory.CreateCertificate(
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
                byte[] certificateRequest = CertificateFactory.CreateSigningRequest(csrCertificate, randomApp.DomainNames);

                X509Certificate2 newCert = await _keyVault.SigningRequestAsync(
                    group,
                    randomApp.ApplicationRecord.ApplicationUri,
                    certificateRequest);
                // get issuer cert used for signing
                X509Certificate2Collection issuerCerts = await _keyVault.GetIssuerCACertificateChainAsync(group);
#if WRITECERT
                // save cert for debugging
                using (ICertificateStore store = CertificateStoreIdentifier.CreateStore(CertificateStoreType.Directory))
                {
                    Assert.NotNull(store);
                    store.Open("d:\\unittest");
                    await store.Add(newCert);
                    foreach (var cert in issuerCerts) await store.Add(cert);
                }
#endif
                Assert.NotNull(issuerCerts);
                Assert.True(issuerCerts.Count >= 1);
                X509TestUtils.VerifySignedApplicationCert(randomApp, newCert, issuerCerts);
                certCollection.Add(newCert);
            }
            return certCollection;
        }

        /// <summary>
        /// Create a new key pair with a issuer signed certificate in KeyVault.
        /// Validate the signed certificate, then revoke it. Then verify revocation.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(600)]
        public async Task KeyVaultNewKeyPairAndRevokeCertificateAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            string[] groups = await _keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                ApplicationTestData randomApp = _fixture.RandomGenerator.RandomApplicationTestData();
                Guid requestId = Guid.NewGuid();
                Opc.Ua.Gds.Server.X509Certificate2KeyPair newCert = await _keyVault.NewKeyPairRequestAsync(
                    group,
                    requestId.ToString(),
                    randomApp.ApplicationRecord.ApplicationUri,
                    randomApp.Subject,
                    randomApp.DomainNames.ToArray(),
                    randomApp.PrivateKeyFormat,
                    randomApp.PrivateKeyPassword
                    );
                Assert.NotNull(newCert);
                Assert.False(newCert.Certificate.HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(randomApp.Subject, newCert.Certificate.Subject));
                Assert.False(Opc.Ua.Utils.CompareDistinguishedName(newCert.Certificate.Issuer, newCert.Certificate.Subject));
                X509Certificate2 cert = new X509Certificate2(newCert.Certificate.RawData);
                X509CRL crl = await _keyVault.RevokeCertificateAsync(group, cert);
                Assert.NotNull(crl);
                X509Certificate2Collection caChain = await _keyVault.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(caChain);
                X509Certificate2 caCert = caChain[0];
                Assert.False(caCert.HasPrivateKey);
                crl.VerifySignature(caCert, true);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(crl.Issuer, caCert.Issuer));
                // disable and delete private key from KeyVault (requires set/delete rights)
                await _keyVault.AcceptPrivateKeyAsync(group, requestId.ToString());
                await _keyVault.DeletePrivateKeyAsync(group, requestId.ToString());
            }
        }

        /// <summary>
        /// Create a new key pair with a issuer signed certificate in KeyVault.
        /// Load the private key and validate the public/private key.
        /// Accept and delete the private. Verify the private kay is deleted.
        /// </summary>
        /// <returns></returns>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(600)]
        public async Task KeyVaultNewKeyPairLoadThenDeletePrivateKeyAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            string[] groups = await _keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                ApplicationTestData randomApp = _fixture.RandomGenerator.RandomApplicationTestData();
                Guid requestId = Guid.NewGuid();
                Opc.Ua.Gds.Server.X509Certificate2KeyPair newKeyPair = await _keyVault.NewKeyPairRequestAsync(
                    group,
                    requestId.ToString(),
                    randomApp.ApplicationRecord.ApplicationUri,
                    randomApp.Subject,
                    randomApp.DomainNames.ToArray(),
                    randomApp.PrivateKeyFormat,
                    randomApp.PrivateKeyPassword
                    );
                Assert.NotNull(newKeyPair);
                Assert.False(newKeyPair.Certificate.HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(randomApp.Subject, newKeyPair.Certificate.Subject));
                Assert.False(Opc.Ua.Utils.CompareDistinguishedName(newKeyPair.Certificate.Issuer, newKeyPair.Certificate.Subject));

                X509Certificate2Collection issuerCerts = await _keyVault.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(issuerCerts);
                Assert.True(issuerCerts.Count >= 1);

                X509TestUtils.VerifyApplicationCertIntegrity(
                    newKeyPair.Certificate,
                    newKeyPair.PrivateKey,
                    randomApp.PrivateKeyPassword,
                    randomApp.PrivateKeyFormat,
                    issuerCerts
                    );

                // test to load the key from KeyVault
                var privateKey = await _keyVault.LoadPrivateKeyAsync(group, requestId.ToString(), randomApp.PrivateKeyFormat);
                X509Certificate2 privateKeyX509;
                if (randomApp.PrivateKeyFormat == "PFX")
                {
                    privateKeyX509 = CertificateFactory.CreateCertificateFromPKCS12(privateKey, randomApp.PrivateKeyPassword);
                }
                else
                {
                    privateKeyX509 = CertificateFactory.CreateCertificateWithPEMPrivateKey(newKeyPair.Certificate, privateKey, randomApp.PrivateKeyPassword);
                }
                Assert.True(privateKeyX509.HasPrivateKey);

                X509TestUtils.VerifyApplicationCertIntegrity(
                    newKeyPair.Certificate,
                    privateKey,
                    randomApp.PrivateKeyPassword,
                    randomApp.PrivateKeyFormat,
                    issuerCerts
                    );

                await _keyVault.AcceptPrivateKeyAsync(group, requestId.ToString());
                await Assert.ThrowsAsync<KeyVaultErrorException>(async () =>
                {
                    privateKey = await _keyVault.LoadPrivateKeyAsync(group, requestId.ToString(), randomApp.PrivateKeyFormat);
                });
                await _keyVault.AcceptPrivateKeyAsync(group, requestId.ToString());
                await _keyVault.DeletePrivateKeyAsync(group, requestId.ToString());
                await Assert.ThrowsAsync<KeyVaultErrorException>(async () =>
                {
                    await _keyVault.DeletePrivateKeyAsync(group, requestId.ToString());
                });
                await Assert.ThrowsAsync<KeyVaultErrorException>(async () =>
                {
                    privateKey = await _keyVault.LoadPrivateKeyAsync(group, requestId.ToString(), randomApp.PrivateKeyFormat);
                });
            }
        }

        /// <summary>
        /// Read the trust list for every group.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(3000)]
        public async Task GetTrustListAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            string[] groups = await _keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                var trustList = await _keyVault.GetTrustListAsync(group, null, 2);
                string nextPageLink = trustList.NextPageLink;
                while (nextPageLink != null)
                {
                    var nextTrustList = await _keyVault.GetTrustListAsync(group, nextPageLink, 2);
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
        public async Task CreateCAAndAppCertificatesThenRevokeAll()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            X509Certificate2Collection certCollection = new X509Certificate2Collection();
            for (int i = 0; i < 3; i++)
            {
                await KeyVaultCreateCACertificateAsync();
                for (int v = 0; v < 10; v++)
                {
                    certCollection.AddRange(await KeyVaultSigningRequestAsync());
                    certCollection.AddRange(await KeyVaultNewKeyPairRequestAsync());
                }
            }

            string[] groups = await _keyVault.GetCertificateGroupIds();

            // validate all certificates
            foreach (string group in groups)
            {
                var trustList = await _keyVault.GetTrustListAsync(group);
                string nextPageLink = trustList.NextPageLink;
                while (nextPageLink != null)
                {
                    var nextTrustList = await _keyVault.GetTrustListAsync(group, nextPageLink);
                    trustList.AddRange(nextTrustList);
                    nextPageLink = nextTrustList.NextPageLink;
                }
                var validator = await X509TestUtils.CreateValidatorAsync(trustList);
                foreach (var cert in certCollection)
                {
                    validator.Validate(cert);
                }
            }

            // now revoke all certifcates
            var revokeCertificates = new X509Certificate2Collection(certCollection);
            foreach (string group in groups)
            {
                var unrevokedCertificates = await _keyVault.RevokeCertificatesAsync(group, revokeCertificates);
                Assert.True(unrevokedCertificates.Count <= revokeCertificates.Count);
                revokeCertificates = unrevokedCertificates;
            }
            Assert.Empty(revokeCertificates);

            // reload updated trust list from KeyVault
            var trustListAllGroups = new KeyVaultTrustListModel("all");
            foreach (string group in groups)
            {
                var trustList = await _keyVault.GetTrustListAsync(group);
                string nextPageLink = trustList.NextPageLink;
                while (nextPageLink != null)
                {
                    var nextTrustList = await _keyVault.GetTrustListAsync(group, nextPageLink);
                    trustList.AddRange(nextTrustList);
                    nextPageLink = nextTrustList.NextPageLink;
                }
                trustListAllGroups.AddRange(trustList);
            }

            // verify certificates are revoked
            {
                var validator = await X509TestUtils.CreateValidatorAsync(trustListAllGroups);
                foreach (var cert in certCollection)
                {
                    Assert.Throws<Opc.Ua.ServiceResultException>(() =>
                    {
                        validator.Validate(cert);
                    });
                }
            }
        }

        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper _log;
    }


}
