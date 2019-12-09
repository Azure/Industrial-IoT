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
    using Microsoft.Graph;
    using Microsoft.Rest;
    using Serilog;

    class MicrosoftGraphServiceClient {

        private readonly Guid _tenantGuid;
        private readonly GraphServiceClient _graphServiceClient;

        public MicrosoftGraphServiceClient(
            Guid tenantGuid,
            ITokenProvider microsoftGraphTokenProvider,
            CancellationToken cancellationToken = default
        ) {
            _tenantGuid = tenantGuid;

            var delegateAuthenticationProvider = new DelegateAuthenticationProvider(
                async (httpRequestMessage) => {
                    var authenticationHeaderValue = await microsoftGraphTokenProvider
                        .GetAuthenticationHeaderAsync(cancellationToken);

                    httpRequestMessage.Headers.Authorization = authenticationHeaderValue;
                }
            );

            _graphServiceClient = new GraphServiceClient(delegateAuthenticationProvider);
        }

        public MicrosoftGraphServiceClient(
            Guid tenantGuid,
            TokenCredentials microsoftGraphTokenCredentials,
            CancellationToken cancellationToken = default
        ) {
            _tenantGuid = tenantGuid;

            var delegateAuthenticationProvider = new DelegateAuthenticationProvider(
                async (httpRequestMessage) => {
                    await microsoftGraphTokenCredentials
                        .ProcessHttpRequestAsync(
                            httpRequestMessage,
                            cancellationToken
                        );
                }
            );

            _graphServiceClient = new GraphServiceClient(delegateAuthenticationProvider);
        }

        public async Task<User> GetMeAsync(
            CancellationToken cancellationToken = default
        ) {
            return await _graphServiceClient
                .Me
                .Request()
                .GetAsync(cancellationToken);
        }

        public async Task<ServicePrincipal> GetServicePrincipalByIdAsync(
            Guid servicePrincipalId,
            CancellationToken cancellationToken = default
        ) {
            return await _graphServiceClient
                .ServicePrincipals[servicePrincipalId.ToString()]
                .Request()
                .GetAsync(cancellationToken);
        }

        public async Task<Application> RegisterServiceApplicationAsync(
            string servicesApplicationName,
            DirectoryObject owner,
            IEnumerable<string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            tags ??= new List<string>();

            // Setup AppRoles for service application
            var serviceApplicationAppRoles = new List<AppRole>();

            var serviceApplicationApproverRoleIdGuid = Guid.NewGuid();
            serviceApplicationAppRoles.Add(new AppRole {
                DisplayName = "Approver",
                Value = "Sign",
                Description = "Approvers have the ability to issue certificates.",
                AllowedMemberTypes = new List<string> { "User", "Application" },
                Id = serviceApplicationApproverRoleIdGuid
            });

            var serviceApplicationWriterRoleIdGuid = Guid.NewGuid();
            serviceApplicationAppRoles.Add(new AppRole {
                DisplayName = "Writer",
                Value = "Write",
                Description = "Writers Have the ability to change entities.",
                AllowedMemberTypes = new List<string> { "User", "Application" },
                Id = serviceApplicationWriterRoleIdGuid
            });

            var serviceApplicationAdministratorRoleIdGuid = Guid.NewGuid();
            serviceApplicationAppRoles.Add(new AppRole {
                DisplayName = "Administrator",
                Value = "Admin",
                Description = "Admins can access advanced features.",
                AllowedMemberTypes = new List<string> { "User", "Application" },
                Id = serviceApplicationAdministratorRoleIdGuid
            });

            // Setup RequiredResourceAccess for service application

            //// This flow is not supported yet.
            //var keyVaultUserImpersonationRequiredResourceAccess = 
            //    await GetRequiredResourceAccessByDisplayNameAsync(
            //        "Azure Key Vault",
            //        new List<string> { "user_impersonation" },
            //        cancellationToken
            //    );

            var keyVaultUserImpersonationRequiredResourceAccess = new RequiredResourceAccess {
                ResourceAppId = AzureAppsConstants.AzureKeyVault.AppId,
                ResourceAccess = new List<ResourceAccess> {
                    new ResourceAccess {
                        Id = AzureAppsConstants.AzureKeyVault.ResourceAccess["user_impersonation"],
                        Type = "Scope"
                    }
                }
            };

            //// This flow is not supported yet.
            //var microsoftGraphUserReadRequiredResourceAccess =
            //    await GetRequiredResourceAccessByDisplayNameAsync(
            //        "Microsoft Graph",
            //        new List<string> { "User.Read" },
            //        cancellationToken
            //    );

            var microsoftGraphUserReadRequiredResourceAccess = new RequiredResourceAccess {
                ResourceAppId = AzureAppsConstants.MicrosoftGraph.AppId,
                ResourceAccess = new List<ResourceAccess> {
                    new ResourceAccess {
                        Id = AzureAppsConstants.MicrosoftGraph.ResourceAccess["User.Read"],
                        Type = "Scope"
                    }
                }
            };

            var serviceApplicationRequiredResourceAccess = new List<RequiredResourceAccess>() {
                keyVaultUserImpersonationRequiredResourceAccess,
                microsoftGraphUserReadRequiredResourceAccess
            };

            // Add OAuth2Permissions
            var oauth2Permissions = new List<PermissionScope> {
                new PermissionScope {
                    AdminConsentDescription = $"Allow the app to access {servicesApplicationName} on behalf of the signed-in user.",
                    AdminConsentDisplayName = $"Access {servicesApplicationName}",
                    Id = Guid.NewGuid(),
                    IsEnabled = true,
                    Type = "User",
                    UserConsentDescription = $"Allow the application to access {servicesApplicationName} on your behalf.",
                    UserConsentDisplayName = $"Access {servicesApplicationName}",
                    Value = "user_impersonation"
                }
            };

            var serviceApplicationApiApplication = new ApiApplication {
                Oauth2PermissionScopes = oauth2Permissions
            };

            var serviceApplicationWebApplication = new WebApplication {
                ImplicitGrantSettings = new ImplicitGrantSettings {
                    EnableIdTokenIssuance = true
                }
            };

            var serviceApplicationIdentifierUri = $"https://{_tenantGuid.ToString()}/{servicesApplicationName}";

            var serviceApplicationRequest = new Application {
                DisplayName = servicesApplicationName,
                IsFallbackPublicClient = false,
                IdentifierUris = new List<string> { serviceApplicationIdentifierUri },
                Tags = tags,
                SignInAudience = "AzureADMyOrg",
                AppRoles = serviceApplicationAppRoles,
                RequiredResourceAccess = serviceApplicationRequiredResourceAccess,
                Api = serviceApplicationApiApplication,
                Web = serviceApplicationWebApplication,
                PasswordCredentials = new List<PasswordCredential> { }
            };

            var serviceApplication = await _graphServiceClient
                .Applications
                .Request()
                .AddAsync(serviceApplicationRequest, cancellationToken);

            // Add Service Key PasswordCredential
            var serviceKeyName = "Service Key";

            var serviceApplicationServiceKeyPasswordCredentialDefinition = new PasswordCredential {
                StartDateTime = DateTimeOffset.UtcNow,
                EndDateTime = DateTimeOffset.UtcNow.AddYears(2),
                CustomKeyIdentifier = ToBase64Bytes(serviceKeyName),
                DisplayName = serviceKeyName
            };

            await _graphServiceClient
                .Applications[serviceApplication.Id]
                .AddPassword(serviceApplicationServiceKeyPasswordCredentialDefinition)
                .Request()
                .PostAsync(cancellationToken);

            // We need to create ServicePrincipal for this application.
            var serviceApplicationSP = await CreateServicePrincipalAsync(
                serviceApplication,
                tags,
                cancellationToken
            );

            // Add app role assignment to owner as Approver, Writer and Administrator.
            string PrincipalType;

            if (owner is User) {
                PrincipalType = "User";
            } else if (owner is ServicePrincipal) {
                PrincipalType = "ServicePrincipal";
            } else if (owner is Group) {
                PrincipalType = "Group";
            } else {
                throw new ArgumentException($"Owner is of unknown type: {owner.GetType()}");
            }

            var approverAppRoleAssignmentRequest = new AppRoleAssignment {
                //PrincipalDisplayName = "",
                PrincipalType = PrincipalType,
                PrincipalId = new Guid(owner.Id),
                ResourceId = new Guid(serviceApplicationSP.Id),
                ResourceDisplayName = "Approver",
                Id = serviceApplicationApproverRoleIdGuid.ToString(),
                AppRoleId = serviceApplicationApproverRoleIdGuid
            };

            var writerAppRoleAssignmentRequest = new AppRoleAssignment {
                //PrincipalDisplayName = "",
                PrincipalType = PrincipalType,
                PrincipalId = new Guid(owner.Id),
                ResourceId = new Guid(serviceApplicationSP.Id),
                ResourceDisplayName = "Writer",
                Id = serviceApplicationWriterRoleIdGuid.ToString(),
                AppRoleId = serviceApplicationWriterRoleIdGuid
            };

            var administratorAppRoleAssignmentRequest = new AppRoleAssignment {
                //PrincipalDisplayName = "",
                PrincipalType = PrincipalType,
                PrincipalId = new Guid(owner.Id),
                ResourceId = new Guid(serviceApplicationSP.Id),
                ResourceDisplayName = "Administrator",
                Id = serviceApplicationAdministratorRoleIdGuid.ToString(),
                AppRoleId = serviceApplicationAdministratorRoleIdGuid
            };

            await _graphServiceClient
                .ServicePrincipals[serviceApplicationSP.Id]
                .AppRoleAssignments
                .Request()
                .AddAsync(
                    approverAppRoleAssignmentRequest,
                    cancellationToken
                );

            await _graphServiceClient
                .ServicePrincipals[serviceApplicationSP.Id]
                .AppRoleAssignments
                .Request()
                .AddAsync(
                    writerAppRoleAssignmentRequest,
                    cancellationToken
                );

            await _graphServiceClient
                .ServicePrincipals[serviceApplicationSP.Id]
                .AppRoleAssignments
                .Request()
                .AddAsync(
                    administratorAppRoleAssignmentRequest,
                    cancellationToken
                );

            return serviceApplication;
        }

        public async Task<Application> RegisterClientApplicationAsync(
            Application serviceApplication,
            string clientsApplicationName,
            IEnumerable<string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            tags ??= new List<string>();

            // Extract id of Oauth2PermissionScope for user impersonation
            var saApiOauth2PermissionScopeUserImpersonationList = serviceApplication
                .Api
                .Oauth2PermissionScopes
                .Where(scope => "User" == scope.Type
                    && "user_impersonation" == scope.Value
                    && scope.IsEnabled.GetValueOrDefault(false)
                )
                .ToList();

            if (saApiOauth2PermissionScopeUserImpersonationList.Count != 1
                || !saApiOauth2PermissionScopeUserImpersonationList.First().Id.HasValue) {
                throw new Exception("Service appplication does not expose Oauth2PermissionScope for user impersonation.");
            }

            var saApiOauth2PermissionScopeUserImpersonationId = 
                saApiOauth2PermissionScopeUserImpersonationList.First().Id.Value;

            var serviceApplicationUserImpersonationRequiredResourceAccess = new RequiredResourceAccess {
                ResourceAppId = serviceApplication.AppId,  // service application
                ResourceAccess = new List<ResourceAccess> {
                    new ResourceAccess {
                        Id = saApiOauth2PermissionScopeUserImpersonationId,  // "user_impersonation"
                        Type = "Scope"
                    }
                }
            };

            var microsoftGraphUserReadRequiredResourceAccess = new RequiredResourceAccess {
                ResourceAppId = AzureAppsConstants.MicrosoftGraph.AppId,
                ResourceAccess = new List<ResourceAccess> {
                    new ResourceAccess {
                        Id = AzureAppsConstants.MicrosoftGraph.ResourceAccess["User.Read"],
                        Type = "Scope"
                    }
                }
            };

            var clientApplicationRequiredResourceAccess = new List<RequiredResourceAccess>() {
                serviceApplicationUserImpersonationRequiredResourceAccess,
                microsoftGraphUserReadRequiredResourceAccess
            };

            var clientApplicationPublicClientApplication = new Microsoft.Graph.PublicClientApplication {
                RedirectUris = new List<string> {
                    "urn:ietf:wg:oauth:2.0:oob"
                }
            };

            // Note: Oauth2AllowImplicitFlow will be enabled automatically since both
            // EnableIdTokenIssuance and EnableAccessTokenIssuance are set to true.

            var clientApplicationWebApplicatoin = new WebApplication {
                //Oauth2AllowImplicitFlow = true,
                ImplicitGrantSettings = new ImplicitGrantSettings {
                    EnableIdTokenIssuance = true,
                    EnableAccessTokenIssuance = true
                }
            };

            var clientApplicationRequest = new Application {
                DisplayName = clientsApplicationName,
                IsFallbackPublicClient = true,
                Tags = tags,
                SignInAudience = "AzureADMyOrg",
                RequiredResourceAccess = clientApplicationRequiredResourceAccess,
                PublicClient = clientApplicationPublicClientApplication,
                Web = clientApplicationWebApplicatoin,
                PasswordCredentials = new List<PasswordCredential> { }
            };

            var clientApplication = await _graphServiceClient
                .Applications
                .Request()
                .AddAsync(clientApplicationRequest, cancellationToken);

            // Add Client Key PasswordCredential
            var clientKeyName = "Client Key";

            var clientApplicationClientKeyPasswordCredentialDefinition = new PasswordCredential {
                StartDateTime = DateTimeOffset.UtcNow,
                EndDateTime = DateTimeOffset.UtcNow.AddYears(2),
                CustomKeyIdentifier = ToBase64Bytes(clientKeyName),
                DisplayName = clientKeyName
            };

            await _graphServiceClient
                .Applications[clientApplication.Id]
                .AddPassword(clientApplicationClientKeyPasswordCredentialDefinition)
                .Request()
                .PostAsync(cancellationToken);

            // We need to create ServicePrincipal for this application.
            await CreateServicePrincipalAsync(
                clientApplication,
                tags,
                cancellationToken
            );

            return clientApplication;
        }

        public async Task<Tuple<Application, string>> RegisterAKSApplicationAsync(
            string aksApplicationName,
            IEnumerable<string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            tags ??= new List<string>();

            // Add OAuth2Permissions for user impersonation
            var aksOauth2Permissions = new List<PermissionScope> {
                new PermissionScope {
                    AdminConsentDescription = $"Allow the app to access {aksApplicationName} on behalf of the signed-in user.",
                    AdminConsentDisplayName = $"Access {aksApplicationName}",
                    Id = Guid.NewGuid(),
                    IsEnabled = true,
                    Type = "User",
                    UserConsentDescription = $"Allow the application to access {aksApplicationName} on your behalf.",
                    UserConsentDisplayName = $"Access {aksApplicationName}",
                    Value = "user_impersonation"
                }
            };

            var aksApplicationApiApplication = new ApiApplication {
                Oauth2PermissionScopes = aksOauth2Permissions
            };

            var aksApplicationWebApplication = new WebApplication {
                ImplicitGrantSettings = new ImplicitGrantSettings {
                    EnableIdTokenIssuance = true
                }
            };

            var aksApplicationIdentifierUri = $"https://{_tenantGuid.ToString()}/{aksApplicationName}";

            var aksApplicationDefinition = new Application {
                DisplayName = aksApplicationName,
                IsFallbackPublicClient = false,
                IdentifierUris = new List<string> { aksApplicationIdentifierUri },
                Tags = tags,
                SignInAudience = "AzureADMyOrg",
                AppRoles = new List<AppRole>(),
                RequiredResourceAccess = new List<RequiredResourceAccess>(),
                Api = aksApplicationApiApplication,
                Web = aksApplicationWebApplication,
                PasswordCredentials = new List<PasswordCredential> { }
            };

            var aksApplication = await _graphServiceClient
                .Applications
                .Request()
                .AddAsync(aksApplicationDefinition, cancellationToken);

            // Add RBAC Key PasswordCredential
            var rbacKeyName = "rbac";

            var aksApplicationRBACPasswordCredentialDefinition = new PasswordCredential {
                StartDateTime = DateTimeOffset.UtcNow,
                EndDateTime = DateTimeOffset.UtcNow.AddYears(2),
                CustomKeyIdentifier = ToBase64Bytes(rbacKeyName),
                DisplayName = rbacKeyName
            };

            var aksApplicationRBACPasswordCredential = await _graphServiceClient
                .Applications[aksApplication.Id]
                .AddPassword(aksApplicationRBACPasswordCredentialDefinition)
                .Request()
                .PostAsync(cancellationToken);

            if (string.IsNullOrEmpty(aksApplicationRBACPasswordCredential.SecretText)) {
                throw new Exception($"Failed to retrieve password credentials for AKS Application: {rbacKeyName}");
            }

            var aksApplicationRBACPasswordCredentialSecret = aksApplicationRBACPasswordCredential.SecretText;

            // We need to create ServicePrincipal for this application.
            await CreateServicePrincipalAsync(
                aksApplication,
                tags,
                cancellationToken
            );

            var result = new Tuple<Application, string>(
                aksApplication,
                aksApplicationRBACPasswordCredentialSecret
            );

            return result;
        }

        /// <summary>
        /// Delete registered application.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeleteApplicationAsync(
            Application application,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Information($"Deleting application: {application.DisplayName} ...");

                await _graphServiceClient
                    .Applications[application.Id]
                    .Request()
                    .DeleteAsync(cancellationToken);

                Log.Information($"Deleted application: {application.DisplayName}");
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to delete application: {application.DisplayName}");
                throw;
            }
        }

        /// <summary>
        /// Update service application to include client application 
        /// as knownClientApplications.
        /// </summary>
        /// <param name="serviceApplication"></param>
        /// <param name="clientApplication"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Updated service application</returns>
        public async Task<Application> AddAsKnownClientApplicationAsync(
            Application serviceApplication,
            Application clientApplication,
            CancellationToken cancellationToken = default
        ) {
            try {
                await _graphServiceClient
                    .Applications[serviceApplication.Id]
                    .Request()
                    .UpdateAsync(
                        new Application {
                            Api = new ApiApplication {
                                KnownClientApplications = new List<Guid> {
                                    new Guid(clientApplication.AppId)
                                }
                            }
                        },
                        cancellationToken
                    );

                // UpdateAsync(...) is a wrapper around HTTP PATCH method and
                // is supposed to return null, despite its documentation. So we
                // will have to get updated application definition to return it.
                var updatedServiceApplication = await _graphServiceClient
                    .Applications[serviceApplication.Id]
                    .Request()
                    .GetAsync(cancellationToken);

                return updatedServiceApplication;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to add {clientApplication.DisplayName} as " +
                    $"knownClientApplications for application: {serviceApplication.DisplayName}");
                throw;
            }
        }

        /// <summary>
        /// Update redirect URIs of the application with provided list.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="redirectUris"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Updated application.</returns>
        public async Task<Application> UpdateRedirectUrisAsync(
            Application application,
            IEnumerable<string> redirectUris,
            CancellationToken cancellationToken = default
        ) {
            try {
                await _graphServiceClient
                    .Applications[application.Id]
                    .Request()
                    .UpdateAsync(
                        new Application {
                            Web = new WebApplication {
                                RedirectUris = redirectUris
                            }
                        },
                        cancellationToken
                    );

                // UpdateAsync(...) is a wrapper around HTTP PATCH method and
                // is supposed to return null, despite its documentation. So we
                // will have to get updated application definition to return it.
                var updatedApplication = await _graphServiceClient
                    .Applications[application.Id]
                    .Request()
                    .GetAsync(cancellationToken);

                return updatedApplication;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to update RedirectUris of application: {application.DisplayName}");
                throw;
            }
        }

        /// <summary>
        /// Get ServicePrincipal by AppId of the application.
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ServicePrincipal object.</returns>
        public async Task<ServicePrincipal> GetServicePrincipalByAppIdAsync(
            string AppId,
            CancellationToken cancellationToken = default
        ) {
            var appIdFilterClause = $"AppId eq '{AppId}'";

            var applicationSPs = await _graphServiceClient
                .ServicePrincipals
                .Request()
                .Filter(appIdFilterClause)
                .GetAsync(cancellationToken);

            if (applicationSPs.Count == 0) {
                throw new Exception($"No service principal found for AppId='{AppId}'");
            }

            if (applicationSPs.Count > 1) {
                throw new Exception($"More than one service principal found for AppId='{AppId}'");
            }

            return applicationSPs.First();
        }

        /// <summary>
        /// Get ServicePrincipal of the application.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ServicePrincipal> GetServicePrincipalAsync(
            Application application,
            CancellationToken cancellationToken = default
        ) {
            return await GetServicePrincipalByAppIdAsync(
                application.AppId,
                cancellationToken
            );
        }

        public async Task<ServicePrincipal> CreateServicePrincipalAsync(
            Application application,
            IEnumerable<string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            tags ??= new List<string>();

            var servicePrincipalDefinition = new ServicePrincipal {
                Tags = tags, // Add WindowsAzureActiveDirectoryIntegratedApp

                DisplayName = application.DisplayName,
                AppId = application.AppId,
            };

            // Create new service principal
            var servicePrincipal = await _graphServiceClient
                .ServicePrincipals
                .Request()
                .AddAsync(servicePrincipalDefinition, cancellationToken);

            return servicePrincipal;
        }

        public async Task DeleteServicePrincipalAsync(
            ServicePrincipal servicePrincipal,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Verbose($"Deleting ServicePrincipal: {servicePrincipal.DisplayName} ...");

                await _graphServiceClient
                    .ServicePrincipals[servicePrincipal.Id]
                    .Request()
                    .DeleteAsync(cancellationToken);

                Log.Verbose($"Deleted ServicePrincipal: {servicePrincipal.DisplayName}");
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to delete ServicePrincipal: {servicePrincipal.DisplayName}");
                throw;
            }
        }

        /// <summary>
        /// Add DirectoryObject as owner of the application.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddAsApplicationOwnerAsync(
            Application application,
            DirectoryObject owner,
            CancellationToken cancellationToken = default
        ) {
            var idFilterClause = $"Id eq '{owner.Id}'";

            var applicationOwners = await _graphServiceClient
                .Applications[application.Id]
                .Owners
                .Request()
                .Filter(idFilterClause)
                .GetAsync(cancellationToken);

            if (applicationOwners.Count == 0) {
                await _graphServiceClient
                    .Applications[application.Id]
                    .Owners
                    .References
                    .Request()
                    .AddAsync(owner, cancellationToken);
            }
        }

        public async Task GrantAdminConsentToClientApplicationAsync(
            ServicePrincipal serviceApplicationSP,
            ServicePrincipal clientApplicationSP,
            CancellationToken cancellationToken = default
        ) {
            // Grant admin consent for service application "user_impersonation" API permissions of client applicatoin
            var clientApplicationOAuth2PermissionGrantUserImpersonationRequest = new OAuth2PermissionGrant {
                ConsentType = "AllPrincipals",
                ClientId = clientApplicationSP.Id,
                ResourceId = serviceApplicationSP.Id,
                Scope = "user_impersonation",
                StartTime = DateTimeOffset.UtcNow,
                ExpiryTime = DateTimeOffset.UtcNow.AddYears(2)
            };

            await _graphServiceClient
                .Oauth2PermissionGrants
                .Request()
                .AddAsync(
                    clientApplicationOAuth2PermissionGrantUserImpersonationRequest,
                    cancellationToken
                );

            // Grant admin consent for Microsoft Graph "User.Read" API permissions of client applicatoin
            var microsoftGraphApplicationSP = await GetServicePrincipalByAppIdAsync(
                AzureAppsConstants.MicrosoftGraph.AppId,
                cancellationToken
            );

            var clientApplicationOAuth2PermissionGrantUserReadRequest = new OAuth2PermissionGrant {
                ConsentType = "AllPrincipals",
                ClientId = clientApplicationSP.Id,
                ResourceId = microsoftGraphApplicationSP.Id,
                Scope = "User.Read",
                StartTime = DateTimeOffset.UtcNow,
                ExpiryTime = DateTimeOffset.UtcNow.AddYears(2)
            };

            await _graphServiceClient
                .Oauth2PermissionGrants
                .Request()
                .AddAsync(
                    clientApplicationOAuth2PermissionGrantUserReadRequest,
                    cancellationToken
                );
        }

        private static byte[] ToBase64Bytes(string message) {
            return System.Text.Encoding.UTF8.GetBytes(message);
        }

        private async Task<RequiredResourceAccess> GetRequiredResourceAccessByDisplayNameAsync(
            string displayName,
            IEnumerable<string> requiredDelegatedPermissions,
            CancellationToken cancellationToken = default
        ) {
            var displayNameFilterClause = $"DisplayName eq '{displayName}'";

            var servicePrincipals = await _graphServiceClient
                .ServicePrincipals
                .Request()
                .Filter(displayNameFilterClause)
                .GetAsync(cancellationToken);

            if (servicePrincipals.Count != 1) {
                var msg = $"Could not find ServicePrincipal with '{displayName}' DisplayName";
                throw new SystemException(msg);
            }

            var servicePrincipal = servicePrincipals.First();

            var resourceAccesses = new List<ResourceAccess>();

            foreach (var requiredDelegatedPermission in requiredDelegatedPermissions) {
                var oauth2Permissions = servicePrincipal
                    .Oauth2Permissions
                    .Where(permission => permission.Value == requiredDelegatedPermission)
                    .ToList();

                if (oauth2Permissions.Count != 1) {
                    var msg = $"Could not  find Oauth2Permission with '{requiredDelegatedPermission}' Value";
                    throw new System.Exception(msg);
                }

                var oauth2Permission = oauth2Permissions.First();

                var resourceAccess = new ResourceAccess {
                    Type = "Scope",
                    Id = oauth2Permission.Id
                };

                resourceAccesses.Add(resourceAccess);
            }

            var requiredResourceAccess = new RequiredResourceAccess {
                ResourceAppId = servicePrincipal.AppId,
                ResourceAccess = resourceAccesses
            };

            return requiredResourceAccess;
        }
    }
}
