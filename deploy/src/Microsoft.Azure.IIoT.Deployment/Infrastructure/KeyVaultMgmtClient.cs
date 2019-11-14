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

        private readonly KeyVaultManagementClient _keyVaultManagementClient;

        public KeyVaultMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _keyVaultManagementClient = new KeyVaultManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateName(
            string prefix = DEFAULT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        public VaultCreateOrUpdateParameters GetCreationParameters(
            Guid tenantIdGuid,
            IResourceGroup resourceGroup,
            ServicePrincipal serviceApplicationSP,
            User user,
            IDictionary<string, string> tags = null
        ) {
            if (null == tags) {
                tags = new Dictionary<string, string> { };
            }

            var keyVaultAccessPolicies = new List<AccessPolicyEntry> {
                    new AccessPolicyEntry {
                        TenantId = tenantIdGuid,
                        ObjectId = serviceApplicationSP.Id,
                        Permissions = new Permissions {
                            Secrets = new List<SecretPermissions> {
                                SecretPermissions.Get
                            },
                            Certificates = new List<CertificatePermissions> {
                                CertificatePermissions.Get,
                                CertificatePermissions.List
                            }
                        }
                    },
                    new AccessPolicyEntry {
                        TenantId = tenantIdGuid,
                        ObjectId = user.Id,
                        Permissions = new Permissions {
                            Keys = new List<KeyPermissions> {
                                KeyPermissions.Get,
                                KeyPermissions.List,
                                KeyPermissions.Sign 
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
                    TenantId = tenantIdGuid,
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

        public async Task<VaultInner> CreateAsync(
            IResourceGroup resourceGroup,
            string keyVaultName,
            VaultCreateOrUpdateParameters keyVaultParameter,
            CancellationToken cancellationToken = default
        ) {
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

        public async Task<VaultInner> GetAsync(
            IResourceGroup resourceGroup,
            string keyVaultName,
            CancellationToken cancellationToken = default
        ) {
            return await _keyVaultManagementClient
                .Vaults
                .GetAsync(
                    resourceGroup.Name,
                    keyVaultName,
                    cancellationToken
                );
        }

        public async Task<bool> CheckNameAvailabilityAsync(
            string keyVaultName,
            CancellationToken cancellationToken = default
        ) {
            var result = await _keyVaultManagementClient
                .Vaults
                .CheckNameAvailabilityAsync(
                    keyVaultName,
                    cancellationToken
                );

            return result.NameAvailable.Value;
        }

        public void Dispose() {
            if (null != _keyVaultManagementClient) {
                _keyVaultManagementClient.Dispose();
            }
        }
    }
}
