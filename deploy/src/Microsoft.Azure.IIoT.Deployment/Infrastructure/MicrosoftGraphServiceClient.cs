// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Rest;
    using Serilog;

    class MicrosoftGraphServiceClient {

        private readonly Guid _tenantGuid;
        private readonly ITokenProvider _microsoftGraphTokenProvider;
        private readonly GraphServiceClient _graphServiceClient;
        private User _me;

        public MicrosoftGraphServiceClient(
            Guid tenantGuid,
            ITokenProvider microsoftGraphTokenProvider,
            CancellationToken cancellationToken = default
        ) {
            _tenantGuid = tenantGuid;
            _microsoftGraphTokenProvider = microsoftGraphTokenProvider;

            var delegateAuthenticationProvider = new DelegateAuthenticationProvider(
                async (requestMessage) => {
                    var authenticationHeaderValue = await microsoftGraphTokenProvider
                        .GetAuthenticationHeaderAsync(cancellationToken);

                    requestMessage.Headers.Authorization = authenticationHeaderValue;
                }
            );

            _graphServiceClient = new GraphServiceClient(delegateAuthenticationProvider);
        }

        public User Me(
            CancellationToken cancellationToken = default
        ) {
            if (null == _me) {
                _me = _graphServiceClient
                    .Me
                    .Request()
                    .GetAsync(cancellationToken)
                    .Result;
            }

            return _me;
        }

        public async Task<Application> RegisterServiceApplicationAsync(
            string servicesApplicationName,
            IEnumerable<string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            if (null == tags) {
                tags = new List<string> { };
            }

            // Setup AppRoles for service application
            var serviceApplicationAppRoles = new List<AppRole>();

            var serviceApplicationApproverRoleIdGuid = Guid.NewGuid();
            serviceApplicationAppRoles.Add(new AppRole {
                DisplayName = "Approver",
                Value = "Sign",
                Description = "Approvers have the ability to issue certificates.",
                AllowedMemberTypes = new List<string> { "User" },
                Id = serviceApplicationApproverRoleIdGuid
            });

            var serviceApplicationWriterRoleIdGuid = Guid.NewGuid();
            serviceApplicationAppRoles.Add(new AppRole {
                DisplayName = "Writer",
                Value = "Write",
                Description = "Writers Have the ability to change entities.",
                AllowedMemberTypes = new List<string> { "User" },
                Id = serviceApplicationWriterRoleIdGuid
            });

            var serviceApplicationAdministratorRoleIdGuid = Guid.NewGuid();
            serviceApplicationAppRoles.Add(new AppRole {
                DisplayName = "Administrator",
                Value = "Admin",
                Description = "Admins can access advanced features.",
                AllowedMemberTypes = new List<string> { "User" },
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

            var me = Me(cancellationToken);

            var approverAppRoleAssignmentRequest = new AppRoleAssignment {
                //PrincipalDisplayName = "",
                PrincipalType = "User",
                PrincipalId = new Guid(me.Id),
                ResourceId = new Guid(serviceApplicationSP.Id),
                ResourceDisplayName = "Approver",
                Id = serviceApplicationApproverRoleIdGuid.ToString(),
                AppRoleId = serviceApplicationApproverRoleIdGuid
            };

            var writerAppRoleAssignmentRequest = new AppRoleAssignment {
                //PrincipalDisplayName = "",
                PrincipalType = "User",
                PrincipalId = new Guid(me.Id),
                ResourceId = new Guid(serviceApplicationSP.Id),
                ResourceDisplayName = "Writer",
                Id = serviceApplicationWriterRoleIdGuid.ToString(),
                AppRoleId = serviceApplicationWriterRoleIdGuid
            };

            var adminAppRoleAssignmentRequest = new AppRoleAssignment {
                //PrincipalDisplayName = "",
                PrincipalType = "User",
                PrincipalId = new Guid(me.Id),
                ResourceId = new Guid(serviceApplicationSP.Id),
                ResourceDisplayName = "Admin",
                Id = serviceApplicationAdministratorRoleIdGuid.ToString(),
                AppRoleId = serviceApplicationAdministratorRoleIdGuid
            };

            //// ToDo: Use AddAsync() instead of the workaround bellow
            //// when AddAsync() is added.
            //var appRoleAssignment = _graphServiceClient
            //    .ServicePrincipals[serviceApplicationSP.Id]
            //    .AppRoleAssignments
            //    .Request()
            //    .AddAsync(appRoleAssignmentRequest);

            // Workaround using HttpClient
            await AddAppRoleAssignmentAsync(serviceApplicationSP, approverAppRoleAssignmentRequest);
            await AddAppRoleAssignmentAsync(serviceApplicationSP, writerAppRoleAssignmentRequest);
            await AddAppRoleAssignmentAsync(serviceApplicationSP, adminAppRoleAssignmentRequest);

            return serviceApplication;
        }

        public async Task<Application> RegisterClientApplicationAsync(
            Application serviceApplication,
            string clientsApplicationName,
            string azureWebsiteName, // ToDo: This should be set after App Service is deployed.
            IEnumerable<string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            if (null == tags) {
                tags = new List<string> { };
            }

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

            // ToDo: RedirectUris should be set after App Service is deployed.
            var clientApplicationWebApplicatoin = new WebApplication {
                RedirectUris = new List<string> {
                    $"https://{azureWebsiteName}.azurewebsites.net/",
                    $"https://{azureWebsiteName}.azurewebsites.net/registry/",
                    $"https://{azureWebsiteName}.azurewebsites.net/twin/",
                    $"https://{azureWebsiteName}.azurewebsites.net/history/",
                    $"https://{azureWebsiteName}.azurewebsites.net/ua/",
                    $"https://{azureWebsiteName}.azurewebsites.net/vault/"
                },
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
            if (null == tags) {
                tags = new List<string> { };
            }

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

        public async Task AddAsKnownClientApplicationAsync(
            Application serviceApplication,
            Application clientApplication,
            CancellationToken cancellationToken = default
        ) {
            // Update service application to include client applicatoin as knownClientApplications
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
        }

        //public async Task<Application> GetApplicationByAppIdAsync(
        //    string AppId,
        //    CancellationToken cancellationToken = default
        //) {
        //    throw new Exception("Currently Application filterring does not work on AppId");

        //    //var appIdFilterClause = $"AppId eq '{AppId}'";

        //    //var applications = await _graphServiceClient
        //    //    .Applications
        //    //    .Request()
        //    //    .Filter(appIdFilterClause)
        //    //    .GetAsync(cancellationToken);

        //    //if (applications.Count != 1) {
        //    //    throw new Exception($"Could not find Application with AppId='{AppId}'");
        //    //}

        //    //var application = applications.First();

        //    //return application;
        //}

        public async Task<ServicePrincipal> GetServicePrincipalByAppIdAsync(
            string AppId,
            CancellationToken cancellationToken = default
        ) {
            var idFilterClause = $"AppId eq '{AppId}'";

            var servicePrincipals = await _graphServiceClient
                .ServicePrincipals
                .Request()
                .Filter(idFilterClause)
                .GetAsync(cancellationToken);

            if (servicePrincipals.Count == 0) {
                var msg = string.Format("Unable to find ServicePrincipal with AppId={0}", AppId);
                throw new System.Exception(msg);
            }

            if (servicePrincipals.Count > 1) {
                var msg = string.Format("Found more than one ServicePrincipal with AppId={0}", AppId);
                throw new System.Exception(msg);
            }

            return servicePrincipals.First();
        }

        public async Task<ServicePrincipal> GetServicePrincipalAsync(
            Application application,
            CancellationToken cancellationToken = default
        ) {
            var appIdFilterClause = $"AppId eq '{application.AppId}'";

            var serviceApplicationSPs = await _graphServiceClient
                .ServicePrincipals
                .Request()
                .Filter(appIdFilterClause)
                .GetAsync(cancellationToken);

            if (serviceApplicationSPs.Count == 0) {
                throw new Exception($"No service principal found for AppId=='{application.AppId}'");
            }

            if (serviceApplicationSPs.Count > 1) {
                throw new Exception($"More than one service principal found for AppId=='{application.AppId}'");
            }

            return serviceApplicationSPs.First();
        }

        public async Task<ServicePrincipal> CreateServicePrincipalAsync(
            Application application,
            IEnumerable<string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            if (null == tags) {
                tags = new List<string> { };
            }

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

        public async Task AddMeAsApplicationOwnerAsync(
            Application application,
            CancellationToken cancellationToken = default
        ) {
            // Try to add current user (_me) as app owner the application, if it is not already.
            var me = Me(cancellationToken);
            var idFilterClause = $"Id eq '{me.Id}'";

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
                    .AddAsync(me, cancellationToken);
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

        public async Task AddAppRoleAssignmentAsync(
            ServicePrincipal servicePrincipal,
            AppRoleAssignment appRoleAssignment,
            CancellationToken cancellationToken = default
        ) {
            const string ROLE_ASSIGNMENT_FORMATTER = "https://graph.microsoft.com/beta/servicePrincipals/{0}/appRoleAssignments";
            var url = string.Format(ROLE_ASSIGNMENT_FORMATTER, servicePrincipal.Id);

            using (var client = new HttpClient()) {
                client.DefaultRequestHeaders.Authorization = await _microsoftGraphTokenProvider
                    .GetAuthenticationHeaderAsync(cancellationToken);

                var content = new StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(appRoleAssignment),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode) {
                    return;
                }
                else {
                    throw new HttpRequestException(response.ReasonPhrase);
                }
            }
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
