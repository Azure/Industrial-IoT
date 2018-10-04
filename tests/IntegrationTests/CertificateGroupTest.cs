// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Auth.Azure;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers;
using Opc.Ua;
using Opc.Ua.Gds;
using Opc.Ua.Test;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test
{
    public class ApplicationTestData
    {
        public ApplicationTestData()
        {
            Initialize();
        }

        private void Initialize()
        {
            ApplicationRecord = new ApplicationRecordDataType();
            CertificateGroupId = null;
            CertificateTypeId = null;
            CertificateRequestId = null;
            DomainNames = new StringCollection();
            Subject = null;
            PrivateKeyFormat = "PFX";
            PrivateKeyPassword = "";
            Certificate = null;
            PrivateKey = null;
            IssuerCertificates = null;
        }

        public ApplicationRecordDataType ApplicationRecord;
        public NodeId CertificateGroupId;
        public NodeId CertificateTypeId;
        public NodeId CertificateRequestId;
        public StringCollection DomainNames;
        public string Subject;
        public string PrivateKeyFormat;
        public string PrivateKeyPassword;
        public byte[] Certificate;
        public byte[] PrivateKey;
        public byte[][] IssuerCertificates;
    }

    public class ClientConfig : IClientConfig
    {

        /// <summary>
        /// The AAD application id for the client.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// AAD Client / Application secret (optional)
        /// </summary>
        public string AppSecret { get; set; }

        /// <summary>
        /// Tenant id if any (optional)
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Instance or authority (optional)
        /// </summary>
        public string Authority { get; set; }
    }

    public class LogConfig : ILogConfig
    {
        public LogLevel LogLevel
        {
            get { return LogLevel.Debug; }
        }

        public string ProcessId => "Vault.Test";
    }

    public class CertificateGroupTest
    {
        private ServicesConfig serviceConfig = new ServicesConfig()
        {
            KeyVaultResourceID =  "https://vault.azure.net",
            // TODO: new test vault
            KeyVaultApiUrl = "https://gdsvault-test.vault.azure.net"
        };

        private IClientConfig clientConfig = new ClientConfig()
        {
            // TODO: new OpcVault app service
            // use GDSVault.Service.Test
            AppId = "f70b169e-8d98-40df-8581-f61fa48faa8f",
            AppSecret = null, // TODO: Need to read secrets out of keyvault
            // For now skipping test
            TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
            Authority = "https://login.microsoftonline.com/"
        };

        private TraceLogger logger = new TraceLogger(new LogConfig());

        public CertificateGroupTest(ITestOutputHelper log)
        {
            _log = log;
            _randomSource = new RandomSource(randomStart);
            _dataGenerator = new DataGenerator(_randomSource);
        }


        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task KeyVaultInit()
        {
            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            await keyVault.Init();

        }

        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task KeyVaultListOfCertGroups()
        {
            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            string[] groups = await keyVault.GetCertificateGroupIds();
        }

        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task KeyVaultGroupConfigurationCollection()
        {

            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            Opc.Ua.Gds.Server.CertificateGroupConfigurationCollection groupCollection = await keyVault.GetCertificateGroupConfigurationCollection();
            Assert.NotNull(groupCollection);
            Assert.NotEmpty(groupCollection);
        }

        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task KeyVaultGetCertificateAsync()
        {
            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            await keyVault.Init();
            string[] groups = await keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                X509Certificate2Collection caChain = await keyVault.GetCACertificateChainAsync(group);
                Assert.NotNull(caChain);
                Assert.True(caChain.Count >= 1);
                foreach (X509Certificate2 caCert in caChain)
                {
                    Assert.False(caCert.HasPrivateKey);
                }
                System.Collections.Generic.IList<X509CRL> crlChain = await keyVault.GetCACrlChainAsync(group);
                Assert.NotNull(crlChain);
                Assert.True(crlChain.Count >= 1);
                for (int i = 0; i < caChain.Count; i++)
                {
                    crlChain[i].VerifySignature(caChain[i], true);
                    Assert.True(Opc.Ua.Utils.CompareDistinguishedName(crlChain[i].Issuer, caChain[i].Issuer));
                }
            }
        }

        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task KeyVaultCreateCACertificateAsync()
        {
            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            string[] groups = await keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                X509Certificate2 result = await keyVault.CreateCACertificateAsync(group);
                Assert.NotNull(result);
                Assert.False(result.HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(result.Issuer, result.Subject));
                X509BasicConstraintsExtension basicConstraints = X509TestUtils.FindBasicConstraintsExtension(result);
                Assert.NotNull(basicConstraints);
                Assert.True(basicConstraints.CertificateAuthority);
            }
        }

        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task KeyVaultNewKeyPairRequestAsync()
        {
            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            string[] groups = await keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                ApplicationTestData randomApp = RandomApplicationTestData();
                Opc.Ua.Gds.Server.X509Certificate2KeyPair newKeyPair = await keyVault.NewKeyPairRequestAsync(
                    group,
                    randomApp.ApplicationRecord.ApplicationUri,
                    randomApp.Subject,
                    randomApp.DomainNames.ToArray(),
                    randomApp.PrivateKeyFormat,
                    randomApp.PrivateKeyPassword);
                Assert.NotNull(newKeyPair);
                Assert.False(newKeyPair.Certificate.HasPrivateKey);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(randomApp.Subject, newKeyPair.Certificate.Subject));
                Assert.False(Opc.Ua.Utils.CompareDistinguishedName(newKeyPair.Certificate.Issuer, newKeyPair.Certificate.Subject));
                X509Certificate2Collection issuerCerts = await keyVault.GetCACertificateChainAsync(group);
                Assert.NotNull(issuerCerts);
                Assert.True(issuerCerts.Count >= 1);

                X509TestUtils.VerifyApplicationCertIntegrity(
                    newKeyPair.Certificate,
                    newKeyPair.PrivateKey,
                    randomApp.PrivateKeyPassword,
                    randomApp.PrivateKeyFormat,
                    issuerCerts
                    );
            }
        }

        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task KeyVaultSigningRequestAsync()
        {
            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            string[] groups = await keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                Opc.Ua.Gds.Server.CertificateGroupConfiguration certificateGroupConfiguration = await keyVault.GetCertificateGroupConfiguration(group);
                ApplicationTestData randomApp = RandomApplicationTestData();
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

                X509Certificate2 newCert = await keyVault.SigningRequestAsync(
                    group,
                    randomApp.ApplicationRecord.ApplicationUri,
                    certificateRequest);
#if !WRITECERT
                // save cert for debugging
                using (ICertificateStore store = CertificateStoreIdentifier.CreateStore(CertificateStoreType.Directory))
                {
                    Assert.NotNull(store);
                    store.Open("d:\\unittest");
                    await store.Add(newCert);
                }
#endif
                // get issuer cert used for signing
                X509Certificate2Collection issuerCerts = await keyVault.GetCACertificateChainAsync(group);
                Assert.NotNull(issuerCerts);
                Assert.True(issuerCerts.Count >= 1);
                X509TestUtils.VerifySignedApplicationCert(randomApp, newCert, issuerCerts);
            }
        }


        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task KeyVaultNewKeyPairAndRevokeCertificateAsync()
        {
            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            await keyVault.Init();
            string[] groups = await keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                ApplicationTestData randomApp = RandomApplicationTestData();
                Opc.Ua.Gds.Server.X509Certificate2KeyPair newCert = await keyVault.NewKeyPairRequestAsync(
                    group,
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
                X509CRL crl = await keyVault.RevokeCertificateAsync(group, cert);
                Assert.NotNull(crl);
                X509Certificate2Collection caChain = await keyVault.GetCACertificateChainAsync(group);
                Assert.NotNull(caChain);
                X509Certificate2 caCert = caChain[0];
                Assert.False(caCert.HasPrivateKey);
                crl.VerifySignature(caCert, true);
                Assert.True(Opc.Ua.Utils.CompareDistinguishedName(crl.Issuer, caCert.Issuer));
            }
        }

        [Fact(Skip = "no hard coded app secret"), Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetTrustListAsync()
        {
            KeyVaultCertificateGroup keyVault = new KeyVaultCertificateGroup(serviceConfig, clientConfig, logger);
            await keyVault.Init();
            string[] groups = await keyVault.GetCertificateGroupIds();
            foreach (string group in groups)
            {
                await keyVault.GetTrustListAsync(group);
            }
        }

        private ApplicationTestData RandomApplicationTestData()
        {
            ApplicationType appType = (ApplicationType)_randomSource.NextInt32((int)ApplicationType.ClientAndServer);
            string pureAppName = _dataGenerator.GetRandomString("en");
            pureAppName = Regex.Replace(pureAppName, @"[^\w\d\s]", "");
            string pureAppUri = Regex.Replace(pureAppName, @"[^\w\d]", "");
            string appName = "UA " + pureAppName;
            StringCollection domainNames = RandomDomainNames();
            string localhost = domainNames[0];
            string privateKeyFormat = _randomSource.NextInt32(1) == 0 ? "PEM" : "PFX";
            string appUri = ("urn:localhost:opcfoundation.org:" + pureAppUri.ToLower()).Replace("localhost", localhost);
            string prodUri = "http://opcfoundation.org/UA/" + pureAppUri;
            StringCollection discoveryUrls = new StringCollection();
            StringCollection serverCapabilities = new StringCollection();
            switch (appType)
            {
                case ApplicationType.Client:
                    appName += " Client";
                    break;
                case ApplicationType.ClientAndServer:
                    appName += " Client and";
                    goto case ApplicationType.Server;
                case ApplicationType.Server:
                    appName += " Server";
                    int port = (_dataGenerator.GetRandomInt16() & 0x1fff) + 50000;
                    discoveryUrls = RandomDiscoveryUrl(domainNames, port, pureAppUri);
                    break;
            }
            ApplicationTestData testData = new ApplicationTestData
            {
                ApplicationRecord = new ApplicationRecordDataType
                {
                    ApplicationNames = new LocalizedTextCollection { new LocalizedText("en-us", appName) },
                    ApplicationUri = appUri,
                    ApplicationType = appType,
                    ProductUri = prodUri,
                    DiscoveryUrls = discoveryUrls,
                    ServerCapabilities = serverCapabilities
                },
                DomainNames = domainNames,
                Subject = string.Format("CN={0},DC={1},O=OPC Foundation", appName, localhost),
                PrivateKeyFormat = privateKeyFormat
            };
            return testData;
        }

        private string RandomLocalHost()
        {
            string localhost = Regex.Replace(_dataGenerator.GetRandomSymbol("en").Trim().ToLower(), @"[^\w\d]", "");
            if (localhost.Length >= 12)
            {
                localhost = localhost.Substring(0, 12);
            }
            return localhost;
        }

        private string[] RandomDomainNames()
        {
            int count = _randomSource.NextInt32(8) + 1;
            string[] result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = RandomLocalHost();
            }
            return result;
        }

        private StringCollection RandomDiscoveryUrl(StringCollection domainNames, int port, string appUri)
        {
            StringCollection result = new StringCollection();
            foreach (string name in domainNames)
            {
                int random = _randomSource.NextInt32(7);
                if ((result.Count == 0) || (random & 1) == 0)
                {
                    result.Add(string.Format("opc.tcp://{0}:{1}/{2}", name, (port++).ToString(), appUri));
                }
                if ((random & 2) == 0)
                {
                    result.Add(string.Format("http://{0}:{1}/{2}", name, (port++).ToString(), appUri));
                }
                if ((random & 4) == 0)
                {
                    result.Add(string.Format("https://{0}:{1}/{2}", name, (port++).ToString(), appUri));
                }
            }
            return result;
        }

        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper _log;
        private const int randomStart = 1;
        private RandomSource _randomSource;
        private DataGenerator _dataGenerator;

    }
}
