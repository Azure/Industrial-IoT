// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Tests {
#if false
    public class CertificateStorageTestFixture : IDisposable {

        public ITrustGroupStore Registry { get; set; }
        public ApplicationTestDataGenerator RandomGenerator { get; set; }
        public RequestDatabase Services { get; set; }
        public bool KeyVaultInitOk { get; set; }
        public string GroupId { get; }

        private readonly KeyVaultConfig _vaultConfig;

        public CertificateStorageTestFixture() {
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
            _logger = SerilogTestLogger.Create<CertificateStorageTestFixture>();
            if (!InvalidConfiguration()) {
                RandomGenerator = new ApplicationTestDataGenerator();
                var timeid = DateTime.UtcNow.ToFileTimeUtc() / 1000 % 10000;

                // Create registry
                GroupId = "test";

                Registry = new TrustGroupDatabase(new ItemContainerFactory(
                    new CosmosDbServiceClient(_serviceConfig, _logger)), _logger);

                // Registry.CreateGroupAsync(new CertificateGroupCreateRequestModel {
                //     Name = "GroupTestIssuerCA" + timeid.ToString(),
                //     SubjectName = "CN=OPC Vault Cert Request Test CA, O=Microsoft, OU=Azure IoT",
                //     CertificateType = CertificateType.ApplicationInstanceCertificate
                // }, CancellationToken.None).Result.Id

                // Create client
                var serializer = new KeyVaultKeyHandleSerializer();
                var repo = new CertificateDatabase(new ItemContainerFactory(
                    new CosmosDbServiceClient(_serviceConfig, _logger)), serializer);
                _keyVaultServiceClient = new KeyVaultServiceClient(_vaultConfig,
                    new AppAuthenticationProvider(_clientConfig), repo, _logger);

                // Create services
                Services = new RequestDatabase(
                    repo,
                    _keyVaultServiceClient,  // keystore
                    Registry,
                    _keyVaultServiceClient,  // issuer
                    new CertificateRevoker(repo, _keyVaultServiceClient, _keyVaultServiceClient),
                    new EntityExtensionFactory(_keyVaultServiceClient),
                    _serviceConfig);

                // Clear
                _keyVaultServiceClient.PurgeAsync("groups", GroupId, CancellationToken.None).Wait();
            }
            KeyVaultInitOk = false;
        }

        public void SkipOnInvalidConfiguration() {
            Skip.If(InvalidConfiguration(), "Missing valid KeyVault configuration.");
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

        public void Dispose() {
            PurgeAsync().Wait();
        }

        public Task PurgeAsync() =>
            _keyVaultServiceClient?.PurgeAsync("groups", GroupId, CancellationToken.None) ?? Task.CompletedTask;

        private readonly KeyVaultServiceClient _keyVaultServiceClient;
        private readonly VaultConfig _serviceConfig;
        private readonly IClientConfig _clientConfig;
        private readonly ILogger _logger;
    }
#endif
}
