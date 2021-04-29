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

        private readonly GraphServiceClient _graphServiceClient;

        public MicrosoftGraphServiceClient(
            ITokenProvider microsoftGraphTokenProvider,
            CancellationToken cancellationToken = default
        ) {
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
            TokenCredentials microsoftGraphTokenCredentials,
            CancellationToken cancellationToken = default
        ) {
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

        public async Task<Application> GetApplicationAsync(
            Guid applicationId,
            CancellationToken cancellationToken = default
        ) {
            return await _graphServiceClient
                .Applications[applicationId.ToString()]
                .Request()
                .GetAsync(cancellationToken);
        }

        public async Task<Application> CreateApplicationAsync(
            Application applicationDefinition,
            CancellationToken cancellationToken = default
        ) {
            var application = await _graphServiceClient
                .Applications
                .Request()
                .AddAsync(
                    applicationDefinition,
                    cancellationToken
                );

            return application;
        }

        public async Task<PasswordCredential> AddApplicationPasswordCredentialAsync(
            Application application,
            PasswordCredential passwordCredentialDefinition,
            CancellationToken cancellationToken = default
        ) {
            var passwordCredential = await _graphServiceClient
                .Applications[application.Id]
                .AddPassword(passwordCredentialDefinition)
                .Request()
                .PostAsync(cancellationToken);

            return passwordCredential;
        }

        /// <summary>
        /// Add PasswordCredential to an Application that is valid for
        /// 2 years starting from now.
        /// </summary>
        /// <param name="application">Application to which PasswordCredential will be added.</param>
        /// <param name="keyName">Display name of PasswordCredential.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<PasswordCredential> AddApplication2YPasswordCredentialAsync(
            Application application,
            string keyName,
            CancellationToken cancellationToken = default
        ) {
            var passwordCredentialDefinition = new PasswordCredential {
                StartDateTime = DateTimeOffset.UtcNow,
                EndDateTime = DateTimeOffset.UtcNow.AddYears(2),
                CustomKeyIdentifier = ToBase64Bytes(keyName),
                DisplayName = keyName
            };

            var passwordCredential = await AddApplicationPasswordCredentialAsync(
                application,
                passwordCredentialDefinition,
                cancellationToken
             );

            if (string.IsNullOrEmpty(passwordCredential?.SecretText)) {
                throw new Exception($"Failed to get password credentials for application: {application.DisplayName}");
            }

            return passwordCredential;
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
        public async Task<Application> UpdateApplicationRedirectUrisAsync(
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
        /// Get ServicePrincipal by its Id.
        /// </summary>
        /// <param name="servicePrincipalId">Guid representing servicePrincipal Id.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ServicePrincipal> GetServicePrincipalAsync(
            Guid servicePrincipalId,
            CancellationToken cancellationToken = default
        ) {
            return await _graphServiceClient
                .ServicePrincipals[servicePrincipalId.ToString()]
                .Request()
                .GetAsync(cancellationToken);
        }

        public async Task<AppRoleAssignment> AddServicePrincipalAppRoleAssignmentAsync(
            ServicePrincipal servicePrincipal,
            AppRoleAssignment appRoleAssignmentDefinition,
            CancellationToken cancellationToken = default
        ) {
            var appRoleAssignment = await _graphServiceClient
                .ServicePrincipals[servicePrincipal.Id]
                .AppRoleAssignments
                .Request()
                .AddAsync(
                    appRoleAssignmentDefinition,
                    cancellationToken
                );

            return appRoleAssignment;
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
        public async Task<ServicePrincipal> GetApplicationServicePrincipalAsync(
            Application application,
            CancellationToken cancellationToken = default
        ) {
            return await GetServicePrincipalByAppIdAsync(
                application.AppId,
                cancellationToken
            );
        }

        public async Task<ServicePrincipal> CreateApplicationServicePrincipalAsync(
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
            // Grant admin consent for service application "user_impersonation" API permissions of client application
            var clientApplicationOAuth2PermissionGrantUserImpersonationDefinition = new OAuth2PermissionGrant {
                ConsentType = "AllPrincipals",
                ClientId = clientApplicationSP.Id,
                ResourceId = serviceApplicationSP.Id,
                Scope = "user_impersonation",
                //StartTime = DateTimeOffset.UtcNow,
                //ExpiryTime = DateTimeOffset.UtcNow.AddYears(2)
            };

            await _graphServiceClient
                .Oauth2PermissionGrants
                .Request()
                .AddAsync(
                    clientApplicationOAuth2PermissionGrantUserImpersonationDefinition,
                    cancellationToken
                );

            // Grant admin consent for Microsoft Graph "User.Read" API permissions of client application
            var microsoftGraphApplicationSP = await GetServicePrincipalByAppIdAsync(
                AzureAppsConstants.MicrosoftGraph.AppId,
                cancellationToken
            );

            var clientApplicationOAuth2PermissionGrantUserReadDefinition = new OAuth2PermissionGrant {
                ConsentType = "AllPrincipals",
                ClientId = clientApplicationSP.Id,
                ResourceId = microsoftGraphApplicationSP.Id,
                Scope = "User.Read",
                //StartTime = DateTimeOffset.UtcNow,
                //ExpiryTime = DateTimeOffset.UtcNow.AddYears(2)
            };

            await _graphServiceClient
                .Oauth2PermissionGrants
                .Request()
                .AddAsync(
                    clientApplicationOAuth2PermissionGrantUserReadDefinition,
                    cancellationToken
                );
        }

        private static byte[] ToBase64Bytes(string message) {
            return System.Text.Encoding.UTF8.GetBytes(message);
        }
    }
}
