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


        private readonly AuthorizationManagementClient _authorizationManagementClient;
        private readonly string _networkContributorRoleDefinitionId;

        public AuthorizationMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _authorizationManagementClient = new AuthorizationManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };

            _networkContributorRoleDefinitionId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{NETWORK_CONTRIBUTOR_ROLE_ID}";
        }

        public async Task<RoleAssignmentInner> AssignNetworkContributorRoleForResourceAsync(
            ServicePrincipal servicePrincipal,
            string resourceId,
            CancellationToken cancellationToken = default
        ) {
            const int retrySeconds = 180;
            const int waitSeconds = 5;

            try {
                Log.Debug($"Assigning NetworkContributor role to Service Principal: {servicePrincipal.DisplayName} ...");

                var roleAssignmentDefinition = new RoleAssignmentCreateParameters {
                    PrincipalId = servicePrincipal.Id,
                    RoleDefinitionId = _networkContributorRoleDefinitionId
                };

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
                                resourceId,
                                Guid.NewGuid().ToString(),
                                roleAssignmentDefinition,
                                cancellationToken
                            );

                        Log.Debug($"Assigned NetworkContributor role to Service Principal: {servicePrincipal.DisplayName}");

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
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to assign NetworkContributor role " +
                    $"to Service Principal: {servicePrincipal.DisplayName}");
                throw;
            }

            // We exited do/while loop without successfully creating role assignment.
            var errorMessage = $"Failed to assign NetworkContributor role within {retrySeconds} seconds " +
                $"to Service Principal: {servicePrincipal.DisplayName}";
            throw new Exception(errorMessage);
        }

        public void Dispose() {
            if (null != _authorizationManagementClient) {
                _authorizationManagementClient.Dispose();
            };
        }
    }
}
