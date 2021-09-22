// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.Graph.RBAC.Fluent;
    using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Graph;

    using Serilog;

    class AuthorizationMgmtClient : IDisposable {

        // We will use built-in "Network Contributor" role
        // Docs: https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#network-contributor
        public const string NETWORK_CONTRIBUTOR_ROLE_ID = "4d97b98b-1d4f-4787-a291-c67834d212e7";

        // We will use built-in "Storage Blob Data Contributor" role
        // Docs: https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor
        public const string STORAGE_BLOB_DATA_CONTRIBUTOR_ROLE_ID = "ba92f5b4-2d11-453d-a403-e96b0029c9fe";

        private readonly AuthorizationManagementClient _authorizationManagementClient;
        private readonly string _subscriptionId;

        public AuthorizationMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            if (string.IsNullOrEmpty(subscriptionId)) {
                throw new ArgumentNullException(nameof(subscriptionId));
            }
            if (restClient is null) {
                throw new ArgumentNullException(nameof(restClient));
            }

            _authorizationManagementClient = new AuthorizationManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };

            _subscriptionId = subscriptionId;
        }

        protected string GetRoleDefinitionId(
            string roleId
        ) {
            var networkContributorRoleDefinitionId =
                $"/subscriptions/{_subscriptionId}/providers" +
                $"/Microsoft.Authorization/roleDefinitions/{roleId}";

            return networkContributorRoleDefinitionId;
        }

        /// <summary>
        /// Assign built-in "Network Contributor" role for specified resource.
        /// </summary>
        /// <param name="servicePrincipal"></param>
        /// <param name="resourceId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<RoleAssignmentInner> AssignNetworkContributorRoleForResourceAsync(
            ServicePrincipal servicePrincipal,
            string resourceId,
            CancellationToken cancellationToken = default
        ) {
            if (servicePrincipal is null) {
                throw new ArgumentNullException(nameof(servicePrincipal));
            }
            if (string.IsNullOrEmpty(resourceId)) {
                throw new ArgumentNullException(nameof(resourceId));
            }

            var networkContributorRoleDefinitionId = GetRoleDefinitionId(
                NETWORK_CONTRIBUTOR_ROLE_ID
            );

            var roleAssignmentDefinition = new RoleAssignmentCreateParameters {
                PrincipalId = servicePrincipal.Id,
                RoleDefinitionId = networkContributorRoleDefinitionId
            };

            var scope = resourceId;

            return await CreateRoleAssignmentWithRetryAsync(
                servicePrincipal,
                scope,
                roleAssignmentDefinition,
                "NetworkContributor",
                cancellationToken
            );
        }

        /// <summary>
        /// Assign built-in "Network Contributor" role for the whole subscription.
        /// </summary>
        /// <param name="servicePrincipal"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<RoleAssignmentInner> AssignNetworkContributorRoleForSubscriptionAsync(
            ServicePrincipal servicePrincipal,
            CancellationToken cancellationToken = default
        ) {
            if (servicePrincipal is null) {
                throw new ArgumentNullException(nameof(servicePrincipal));
            }

            var networkContributorRoleDefinitionId = GetRoleDefinitionId(
                NETWORK_CONTRIBUTOR_ROLE_ID
            );

            var roleAssignmentDefinition = new RoleAssignmentCreateParameters {
                PrincipalId = servicePrincipal.Id,
                RoleDefinitionId = networkContributorRoleDefinitionId
            };

            var scope = $"/subscriptions/{_subscriptionId}/";

            return await CreateRoleAssignmentWithRetryAsync(
                servicePrincipal,
                scope,
                roleAssignmentDefinition,
                "NetworkContributor",
                cancellationToken
            );
        }

        /// <summary>
        /// Assign built-in "Storage Blob Data Contributor" role for the whole subscription.
        /// </summary>
        /// <param name="servicePrincipal"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<RoleAssignmentInner> AssignStorageBlobDataContributorRoleForSubscriptionAsync(
            ServicePrincipal servicePrincipal,
            CancellationToken cancellationToken = default
        ) {
            if (servicePrincipal is null) {
                throw new ArgumentNullException(nameof(servicePrincipal));
            }

            var storageBlobDataContributorRoleDefinitionId = GetRoleDefinitionId(
                STORAGE_BLOB_DATA_CONTRIBUTOR_ROLE_ID
            );

            var roleAssignmentDefinition = new RoleAssignmentCreateParameters {
                PrincipalId = servicePrincipal.Id,
                RoleDefinitionId = storageBlobDataContributorRoleDefinitionId
            };

            var scope = $"/subscriptions/{_subscriptionId}/";

            return await CreateRoleAssignmentWithRetryAsync(
                servicePrincipal,
                scope,
                roleAssignmentDefinition,
                "StorageBlobDataContributor",
                cancellationToken
            );
        }

        /// <summary>
        /// Create role assignment, retry for 240 seconds if Service Principal does not exist yet.
        /// </summary>
        /// <param name="servicePrincipal"></param>
        /// <param name="scope"></param>
        /// <param name="roleAssignmentDefinition"></param>
        /// <param name="roleDescription"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<RoleAssignmentInner> CreateRoleAssignmentWithRetryAsync(
            ServicePrincipal servicePrincipal,
            string scope,
            RoleAssignmentCreateParameters roleAssignmentDefinition,
            string roleDescription,
            CancellationToken cancellationToken = default
        ) {
            const int retrySeconds = 240;
            const int waitSeconds = 5;

            Log.Debug($"Assigning {roleDescription} role to Service Principal: {servicePrincipal.DisplayName} ...");

            var spIdFormatted = new Guid(servicePrincipal.Id).ToString("N");
            var spDoesNotExistMessage = $"principal {spIdFormatted} does not exist";

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // It can take some time for new service principal to
            // propagate throughout Azure AD. So we will retry in
            // a loop for retrySeconds.
            do {
                try {
                    var roleAssignmentResult = await _authorizationManagementClient
                        .RoleAssignments
                        .CreateAsync(
                            scope,
                            Guid.NewGuid().ToString(),
                            roleAssignmentDefinition,
                            cancellationToken
                        );

                    Log.Debug($"Assigned {roleDescription} role to Service Principal: {servicePrincipal.DisplayName}");

                    return roleAssignmentResult;
                }
                catch (Rest.Azure.CloudException ex) {
                    if (ex.Message.ToLower().Contains(spDoesNotExistMessage)) {
                        Log.Debug($"ServicePrincipal creation has not propagated correctly. " +
                            $"Waiting for {waitSeconds} seconds before retry.");

                        await Task.Delay(waitSeconds * 1000, cancellationToken);
                    }
                    else {
                        throw;
                    }
                }
            } while (stopwatch.Elapsed < TimeSpan.FromSeconds(retrySeconds));

            // We exited do/while loop without successfully creating role assignment.
            var errorMessage = $"Failed to assign {roleDescription} role within {retrySeconds} " +
                $"seconds to Service Principal: {servicePrincipal.DisplayName}";
            throw new Exception(errorMessage);
        }

        public void Dispose() {
            if (null != _authorizationManagementClient) {
                _authorizationManagementClient.Dispose();
            };
        }
    }
}
