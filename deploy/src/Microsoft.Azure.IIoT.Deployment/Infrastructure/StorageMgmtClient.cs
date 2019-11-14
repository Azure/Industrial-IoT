// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.Storage.Fluent;
    using Microsoft.Azure.Management.Storage.Fluent.Models;
    using Serilog;

    class StorageMgmtClient : IDisposable {

        public const string DEFAULT_STORAGE_ACCOUNT_NAME_PREFIX = "storage";

        public const string STORAGE_ACCOUNT_IOT_HUB_CONTAINER_NAME = "iothub-default";

        private const string STORAGE_ACCOUNT_CONECTION_STRING_FORMAT = 
            "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix={2}";

        private readonly StorageManagementClient _storageManagementClient;
        private readonly AzureEnvironment _azureEnvironment;

        public StorageMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _storageManagementClient = new StorageManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };

            _azureEnvironment = restClient.Environment;
        }

        public static string GenerateStorageAccountName(
            string prefix = DEFAULT_STORAGE_ACCOUNT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        public async Task<bool> CheckNameAvailabilityAsync(
            string storageAccountName,
            CancellationToken cancellationToken = default
        ) {
            var storageAccountNameCheck = await _storageManagementClient
                .StorageAccounts
                .CheckNameAvailabilityAsync(
                    storageAccountName,
                    cancellationToken
                );

            return storageAccountNameCheck.NameAvailable.Value;
        }

        public async Task<StorageAccountInner> CreateStorageAccountAsync(
            IResourceGroup resourceGroup,
            string storageAccountName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                if (null == tags) {
                    tags = new Dictionary<string, string> { };
                }

                Log.Information($"Creating Azure Storage Account: {storageAccountName} ...");

                var storageAccountCreateParameters = new StorageAccountCreateParameters {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Kind = Kind.Storage,
                    Sku = new SkuInner {
                        Name = SkuName.StandardLRS
                    },
                    EnableHttpsTrafficOnly = true,
                    NetworkRuleSet = new NetworkRuleSet {
                        Bypass = Bypass.AzureServices,
                        VirtualNetworkRules = new List<VirtualNetworkRule> { },
                        IpRules = new List<IPRule> { },
                        DefaultAction = DefaultAction.Allow
                    },
                    Encryption = new Encryption {
                        Services = new EncryptionServices {
                            File = new EncryptionService {
                                Enabled = true
                            },
                            Blob = new EncryptionService {
                                Enabled = true
                            }
                        },
                        KeySource = KeySource.MicrosoftStorage
                    }
                };

                storageAccountCreateParameters.Validate();

                var storageAccount = await _storageManagementClient
                    .StorageAccounts
                    .CreateAsync(
                        resourceGroup.Name,
                        storageAccountName,
                        storageAccountCreateParameters,
                        cancellationToken
                    );

                Log.Information($"Created Azure Storage Account: {storageAccountName}");

                return storageAccount;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure Storage Account: {storageAccountName}");
                throw;
            }
        }


        public async Task<StorageAccountInner> GetStorageAccountAsync(
            IResourceGroup resourceGroup,
            string storageAccountName,
            CancellationToken cancellationToken = default
        ) {
            var storageAccount = await _storageManagementClient
                .StorageAccounts
                .GetPropertiesAsync(
                    resourceGroup.Name,
                    storageAccountName,
                    null,
                    cancellationToken
                );

            return storageAccount;
        }


        public async Task<StorageAccountKey> GetStorageAccountKeyAsync(
            IResourceGroup resourceGroup,
            StorageAccountInner storageAccount,
            CancellationToken cancellationToken = default
        ) {
            var keysList = await _storageManagementClient
                .StorageAccounts
                .ListKeysAsync(
                    resourceGroup.Name,
                    storageAccount.Name,
                    cancellationToken
                );

            var storageAccountKey = keysList.Keys.First();

            return storageAccountKey;
        }

        public async Task<string> GetStorageAccountConectionStringAsync(
            IResourceGroup resourceGroup,
            StorageAccountInner storageAccount,
            CancellationToken cancellationToken = default
        ) {
            var storageAccountKey = await GetStorageAccountKeyAsync(resourceGroup, storageAccount, cancellationToken);

            var storageAccountConectionString = string.Format(
                STORAGE_ACCOUNT_CONECTION_STRING_FORMAT,
                storageAccount.Name,
                storageAccountKey.Value,
                _azureEnvironment.StorageEndpointSuffix
            );

            return storageAccountConectionString;
        }

        public async Task<BlobContainerInner> CreateBlobContainerAsync(
            IResourceGroup resourceGroup,
            StorageAccountInner storageAccount,
            string containerName,
            PublicAccess publicAccess = PublicAccess.None,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                if (null == tags) {
                    tags = new Dictionary<string, string> { };
                }

                Log.Information($"Creating Blob Container: {containerName} ...");

                var iotHubBlobContainer = await _storageManagementClient
                    .BlobContainers
                    .CreateAsync(
                        resourceGroup.Name,
                        storageAccount.Name,
                        containerName,
                        publicAccess,
                        tags,
                        cancellationToken
                    );

                Log.Information($"Created Blob Container: {containerName}");

                return iotHubBlobContainer;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Blob Container: {containerName}");
                throw;
            }
        }

        public void Dispose() {
            if (null != _storageManagementClient) {
                _storageManagementClient.Dispose();
            }
        }
    }
}
