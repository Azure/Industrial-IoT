// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Tests {
#if false
    public class CertificateAuthorityTestFixture : IDisposable {
        public IApplicationRegistry ApplicationsDatabase { get; set; }
        public ITrustGroupStore Registry { get; set; }
        public ITrustGroupServices Services { get; set; }
        public ISigningRequestProcessor CertificateAuthority { get; set; }
        public IRequestManagement RequestManagement { get; set; }
        public IList<ApplicationTestData> ApplicationTestSet { get; set; }
        public ApplicationTestDataGenerator RandomGenerator { get; set; }
        public bool RegistrationOk { get; set; }

        public CertificateAuthorityTestFixture() {
            RandomGenerator = new ApplicationTestDataGenerator(kRandomStart);
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testsettings.json", false, true)
                .AddJsonFile("testsettings.Development.json", true, true)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables();
            var configuration = builder.Build();
            _serviceConfig = new VaultConfig(configuration);
            _clientConfig = new ClientConfig(configuration);
            _vaultConfig = new KeyVaultConfig(configuration);
            _logger = SerilogTestLogger.Create<CertificateAuthorityTestFixture>();
            if (!InvalidConfiguration()) {
                ApplicationsDatabase = new ApplicationRegistry(new ApplicationDatabase(
                    new ItemContainerFactory(new CosmosDbServiceClient(_serviceConfig, _logger)), _logger),
                    new EndpointRegistryStub(), new EndpointRegistryStub(),
                    new ApplicationEventBrokerStub(), _logger);

                var timeid = DateTime.UtcNow.ToFileTimeUtc() / 1000 % 10000;

                // Create group registry
                Registry = new TrustGroupDatabase(new ItemContainerFactory(
                    new CosmosDbServiceClient(_serviceConfig, _logger)), _logger);
                _groupId = Registry.CreateGroupAsync(new Models.TrustGroupRegistrationRequestModel {
                    Name = "CertReqConfig" + timeid.ToString(),
                    SubjectName = "CN=OPC Vault Cert Request Test CA, O=Microsoft, OU=Azure IoT",
                }).Result.Id;

                // Create client
                var serializer = new KeyVaultKeyHandleSerializer();
                var repo = new CertificateDatabase(new ItemContainerFactory(
                    new CosmosDbServiceClient(_serviceConfig, _logger)), serializer);
                _keyVaultServiceClient = new KeyVaultServiceClient(_vaultConfig,
                    new AppAuthenticationProvider(_clientConfig), repo, _logger);

                // Create services
                _keyVaultCertificateGroup = new RequestDatabase(
                    repo,
                    _keyVaultServiceClient,  // keystore
                    Registry,
                    _keyVaultServiceClient,  // issuer
                    new CertificateRevoker(repo, _keyVaultServiceClient, _keyVaultServiceClient),
                    new EntityExtensionFactory(_keyVaultServiceClient),
                    _serviceConfig);
                _keyVaultServiceClient.PurgeAsync("groups", _groupId, CancellationToken.None).Wait();
                Services = _keyVaultCertificateGroup;

                CertificateAuthority = new CertificateRequestManager(ApplicationsDatabase, Services,
                    new ItemContainerFactory(new CosmosDbServiceClient(_serviceConfig, _logger)), _logger);
                RequestManagement = (IRequestManagement)CertificateAuthority;

                // create test set
                ApplicationTestSet = new List<ApplicationTestData>();
                for (var i = 0; i < kTestSetSize; i++) {
                    var randomApp = RandomGenerator.RandomApplicationTestData();
                    ApplicationTestSet.Add(randomApp);
                }
            }
            RegistrationOk = false;
        }

        public void Dispose() {
            _keyVaultServiceClient?.PurgeAsync("groups", _groupId, CancellationToken.None).Wait();
        }

        public void SkipOnInvalidConfiguration() {
            Skip.If(InvalidConfiguration(), "Missing valid CosmosDB or KeyVault configuration.");
        }

        private bool InvalidConfiguration() {
            return
                string.IsNullOrEmpty(_vaultConfig.KeyVaultBaseUrl) ||
                string.IsNullOrEmpty(_vaultConfig.KeyVaultResourceId) ||
                string.IsNullOrEmpty(_clientConfig.AppId) ||
                string.IsNullOrEmpty(_clientConfig.AppSecret) ||
                string.IsNullOrEmpty(_serviceConfig.ContainerName) ||
                string.IsNullOrEmpty(_serviceConfig.DatabaseName) ||
                string.IsNullOrEmpty(_serviceConfig.DbConnectionString)
                ;
        }

        private readonly IClientConfig _clientConfig;
        private readonly VaultConfig _serviceConfig;
        private readonly KeyVaultConfig _vaultConfig;
        private readonly KeyVaultServiceClient _keyVaultServiceClient;
        private readonly RequestDatabase _keyVaultCertificateGroup;
        private readonly ILogger _logger;
        private const int kRandomStart = 1234;
        private const int kTestSetSize = 10;
        private readonly string _groupId;
    }
#endif
}
