// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Auth.Clients;
using Microsoft.Azure.IIoT.Exceptions;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Opc.Ua;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TestCaseOrdering;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test
{

    public class CertificateGroupTestFixture : IDisposable
    {
        private readonly ServicesConfig _serviceConfig = new ServicesConfig();
        private readonly IClientConfig _clientConfig = new ClientConfig();
        private readonly ILogger _logger;
        public ApplicationTestDataGenerator RandomGenerator;
        public KeyVaultCertificateGroup KeyVault;
        public bool KeyVaultInitOk;
        public readonly string ConfigId;
        public readonly string GroupId;

        const int _randomStart = 3388;
        const int _testSetSize = 10;

        public CertificateGroupTestFixture()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("testsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            IConfigurationRoot configuration = builder.Build();
            configuration.Bind("OpcVault", _serviceConfig);
            configuration.Bind("Auth", _clientConfig);
            _logger = SerilogTestLogger.Create<CertificateGroupTestFixture>();
            if (!InvalidConfiguration())
            {
                RandomGenerator = new ApplicationTestDataGenerator();
                var timeid = (DateTime.UtcNow.ToFileTimeUtc() / 1000) % 10000;
                GroupId = "GroupTestIssuerCA" + timeid.ToString();
                ConfigId = "GroupTestConfig" + timeid.ToString();
                var keyVaultServiceClient = KeyVaultServiceClient.Get(ConfigId, _serviceConfig, _clientConfig, _logger);
                KeyVault = new KeyVaultCertificateGroup(keyVaultServiceClient, _serviceConfig, _clientConfig, _logger);
                KeyVault.PurgeAsync(ConfigId, GroupId).Wait();
                KeyVault.CreateCertificateGroupConfiguration(GroupId, "CN=OPC Vault Cert Request Test CA, O=Microsoft, OU=Azure IoT", null).Wait();
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
            KeyVault?.PurgeAsync(ConfigId, GroupId).Wait();
        }
    }

    [TestCaseOrderer("TestCaseOrdering.PriorityOrderer", "Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test")]
    public class CertificateGroupTest : IClassFixture<CertificateGroupTestFixture>
    {
        private readonly CertificateGroupTestFixture _fixture;
        private readonly KeyVaultCertificateGroup _keyVault;
        private readonly ILogger _logger;

        public CertificateGroupTest(CertificateGroupTestFixture fixture, ITestOutputHelper log)
        {
            _logger = SerilogTestLogger.Create<CertificateGroupTest>(log);
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
            _logger.Information("Initializing KeyVault");
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
            await _keyVault.PurgeAsync(null, _fixture.GroupId);
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
        /// Get the certificate versions for every group, try paging..
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(3000)]
        public async Task GetCertificateVersionsAsync()
        {
            Skip.If(!_fixture.KeyVaultInitOk);
            string[] groups = await _keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                // read all certs
                X509Certificate2Collection certCollection;
                string nextPageLink;
                (certCollection, nextPageLink) = await _keyVault.GetIssuerCACertificateVersionsAsync(group, true, null, 2);
                while (nextPageLink != null)
                {
                    X509Certificate2Collection result;
                    (result, nextPageLink) = await _keyVault.GetIssuerCACertificateVersionsAsync(group, true, nextPageLink, 2);
                    certCollection.AddRange(result);
                }

                // read all matching cert and crl by thumbprint
                var chainId = await _keyVault.GetIssuerCACertificateChainAsync(group);
                Assert.NotNull(chainId);
                Assert.True(chainId.Count >= 1);
                var crlId = await _keyVault.GetIssuerCACrlChainAsync(group);
                Assert.NotNull(chainId);
                Assert.True(chainId.Count >= 1);
                foreach (var cert in certCollection)
                {
                    var certChain = await _keyVault.GetIssuerCACertificateChainAsync(group, cert.Thumbprint);
                    Assert.NotNull(certChain);
                    Assert.True(certChain.Count >= 1);
                    Assert.Equal(cert.Thumbprint, certChain[0].Thumbprint);

                    var crlChain = await _keyVault.GetIssuerCACrlChainAsync(group, cert.Thumbprint);
                    Assert.NotNull(crlChain);
                    Assert.True(crlChain.Count >= 1);
                    crlChain[0].VerifySignature(cert, true);
                    crlChain[0].VerifySignature(certChain[0], true);

                    // invalid parameter test
                    // invalid parameter test
                    await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
                    {
                        await _keyVault.GetIssuerCACrlChainAsync(group, cert.Thumbprint + "a");
                    });
                    await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
                    {
                        await _keyVault.GetIssuerCACrlChainAsync("abc", cert.Thumbprint);
                    });
                }

                // invalid parameters
                await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
                {
                    await _keyVault.GetIssuerCACrlChainAsync(group, "abcd");
                });
                await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
                {
                    await _keyVault.GetIssuerCACertificateChainAsync("abc");
                });
                await Assert.ThrowsAsync<ResourceNotFoundException>(async () =>
                {
                    await _keyVault.GetIssuerCACrlChainAsync("abc");
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
    }


}
