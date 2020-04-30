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

    using Microsoft.Azure.Management.KeyVault.Fluent;
    using Microsoft.Azure.Management.KeyVault.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    using Microsoft.Graph;
    using Serilog;

    class KeyVaultMgmtClient : IDisposable {

        public const string DEFAULT_NAME_PREFIX = "keyvault-";
        public const int NUM_OF_MAX_NAME_AVAILABILITY_CHECKS = 5;

        private readonly KeyVaultManagementClient _keyVaultManagementClient;

        public KeyVaultMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentNullException(nameof(subscriptionId));
            }
            if (restClient is null) {
                throw new ArgumentNullException(nameof(restClient));
            }

            _keyVaultManagementClient = new KeyVaultManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        /// <summary>
        /// Generate somewhat randomized name for a KeyVault with
        /// given prefix and random suffix of given length.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="suffixLen"></param>
        /// <returns></returns>
        public static string GenerateName(
            string prefix = DEFAULT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        /// <summary>
        /// Get default KeyVault creation parameters.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="serviceApplicationSP"></param>
        /// <param name="owner"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public VaultCreateOrUpdateParameters GetCreationParameters(
            Guid tenantId,
            IResourceGroup resourceGroup,
            ServicePrincipal serviceApplicationSP,
            DirectoryObject owner,
            IDictionary<string, string> tags = null
        ) {
            if (resourceGroup is null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (serviceApplicationSP is null) {
                throw new ArgumentNullException(nameof(serviceApplicationSP));
            }
            if (owner is null) {
                throw new ArgumentNullException(nameof(owner));
            }

            tags ??= new Dictionary<string, string>();

            var keyVaultAccessPolicies = new List<AccessPolicyEntry> {
                    new AccessPolicyEntry {
                        TenantId = tenantId,
                        ObjectId = serviceApplicationSP.Id,
                        Permissions = new Permissions {
                            Keys = new List<KeyPermissions> {
                                KeyPermissions.Get,
                                KeyPermissions.List,
                                KeyPermissions.Sign,
                                KeyPermissions.UnwrapKey,
                                KeyPermissions.WrapKey,
                                KeyPermissions.Create
                            },
                            Secrets = new List<SecretPermissions> {
                                SecretPermissions.Get,
                                SecretPermissions.List,
                                SecretPermissions.Set,
                                SecretPermissions.Delete
                            },
                            Certificates = new List<CertificatePermissions> {
                                CertificatePermissions.Get,
                                CertificatePermissions.List,
                                CertificatePermissions.Update,
                                CertificatePermissions.Create,
                                CertificatePermissions.Import
                            }
                        }
                    },
                    new AccessPolicyEntry {
                        TenantId = tenantId,
                        ObjectId = owner.Id,
                        Permissions = new Permissions {
                            Keys = new List<KeyPermissions> {
                                KeyPermissions.Get,
                                KeyPermissions.List,
                                KeyPermissions.Sign,
                                KeyPermissions.Create
                            },
                            Secrets = new List<SecretPermissions> {
                                SecretPermissions.Get,
                                SecretPermissions.List,
                                SecretPermissions.Set,
                                SecretPermissions.Delete
                            },
                            Certificates = new List<CertificatePermissions> {
                                CertificatePermissions.Get,
                                CertificatePermissions.List,
                                CertificatePermissions.Update,
                                CertificatePermissions.Create,
                                CertificatePermissions.Import
                            }
                        }
                    }
                };

            keyVaultAccessPolicies.ElementAt(0).Validate();
            keyVaultAccessPolicies.ElementAt(1).Validate();

            var keyVaultParameters = new VaultCreateOrUpdateParameters {
                Location = resourceGroup.RegionName,
                Tags = tags,

                Properties = new VaultProperties {
                    EnabledForDeployment = false,
                    EnabledForTemplateDeployment = false,
                    EnabledForDiskEncryption = false,
                    TenantId = tenantId,
                    Sku = new Sku {
                        Name = SkuName.Premium,
                        //Family = "A"
                    },
                    AccessPolicies = keyVaultAccessPolicies
                }
            };

            keyVaultParameters.Validate();

            return keyVaultParameters;
        }

        /// <summary>
        /// Create a KeyVault.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="keyVaultName"></param>
        /// <param name="keyVaultParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<VaultInner> CreateAsync(
            IResourceGroup resourceGroup,
            string keyVaultName,
            VaultCreateOrUpdateParameters keyVaultParameter,
            CancellationToken cancellationToken = default
        ) {
            if (resourceGroup is null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (string.IsNullOrWhiteSpace(keyVaultName)) {
                throw new ArgumentNullException(nameof(keyVaultName));
            }
            if (keyVaultParameter is null) {
                throw new ArgumentNullException(nameof(keyVaultParameter));
            }

            try {
                Log.Information($"Creating Azure KeyVault: {keyVaultName} ...");

                var keyVault = await _keyVaultManagementClient
                    .Vaults
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        keyVaultName,
                        keyVaultParameter,
                        cancellationToken
                    );

                Log.Information($"Created Azure KeyVault: {keyVaultName}");

                return keyVault;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure KeyVault: {keyVaultName}");
                throw;
            }
        }

        /// <summary>
        /// Get KeyVault details by its name.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="keyVaultName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<VaultInner> GetAsync(
            IResourceGroup resourceGroup,
            string keyVaultName,
            CancellationToken cancellationToken = default
        ) {
            if (resourceGroup is null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (string.IsNullOrWhiteSpace(keyVaultName)) {
                throw new ArgumentNullException(nameof(keyVaultName));
            }

            return await _keyVaultManagementClient
                .Vaults
                .GetAsync(
                    resourceGroup.Name,
                    keyVaultName,
                    cancellationToken
                );
        }

        /// <summary>
        /// Checks whether given KeyVault name is available.
        /// </summary>
        /// <param name="keyVaultName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if name is available, False otherwise.</returns>
        public async Task<bool> CheckNameAvailabilityAsync(
            string keyVaultName,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(keyVaultName)) {
                throw new ArgumentNullException(nameof(keyVaultName));
            }

            try {
                var result = await _keyVaultManagementClient
                    .Vaults
                    .CheckNameAvailabilityAsync(
                        keyVaultName,
                        cancellationToken
                    );

                if (result.NameAvailable.HasValue) {
                    return result.NameAvailable.Value;
                }
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                // Will be thrown if there is no registered resource provider
                // found for specified location and/or api version to perform
                // name availability check.
                throw;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to check KeyVault Service name availability for {keyVaultName}");
                throw;
            }

            // !result.NameAvailable.HasValue
            throw new Exception($"Failed to check KeyVault Service name availability for {keyVaultName}");
        }

        /// <summary>
        /// Tries to generate KeyVault name that is available.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>An available name for KeyVault.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<string> GenerateAvailableNameAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                for (var numOfChecks = 0; numOfChecks < NUM_OF_MAX_NAME_AVAILABILITY_CHECKS; ++numOfChecks) {
                    var keyVaultName = GenerateName();
                    var nameAvailable = await CheckNameAvailabilityAsync(
                        keyVaultName,
                        cancellationToken
                    );

                    if (nameAvailable) {
                        return keyVaultName;
                    }
                }
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                // Will be thrown if there is no registered resource provider
                // found for specified location and/or api version to perform
                // name availability check.
                throw;
            }
            catch (Exception ex) {
                Log.Error(ex, "Failed to generate unique KeyVault service name");
                throw;
            }

            var errorMessage = $"Failed to generate unique KeyVault service name " +
                $"after {NUM_OF_MAX_NAME_AVAILABILITY_CHECKS} retries";

            Log.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        public void Dispose() {
            if (null != _keyVaultManagementClient) {
                _keyVaultManagementClient.Dispose();
            }
        }
    }
}
