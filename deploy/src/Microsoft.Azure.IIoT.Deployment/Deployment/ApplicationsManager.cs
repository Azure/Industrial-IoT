// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {
    using Microsoft.Azure.IIoT.Deployment.Infrastructure;
    using Microsoft.Graph;
    using Microsoft.Rest;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class ApplicationsManager {

        protected readonly MicrosoftGraphServiceClient _msGraphServiceClient;

        private Application _serviceApplication;
        private ServicePrincipal _serviceApplicationSP;

        private Application _clientApplication;
        private ServicePrincipal _clientApplicationSP;

        private Application _aksApplication;
        private ServicePrincipal _aksApplicationSP;
        private string _aksApplicationPasswordCredentialRbacSecret;

        public ApplicationsManager (
            Guid tenantId,
            TokenCredentials microsoftGraphTokenCredentials,
            CancellationToken cancellationToken
        ) {
            _msGraphServiceClient = new MicrosoftGraphServiceClient(
                tenantId,
                microsoftGraphTokenCredentials,
                cancellationToken
            );
        }

        public Application GetServiceApplication() {
            return _serviceApplication;
        }

        public ServicePrincipal GetServiceApplicationSP() {
            return _serviceApplicationSP;
        }

        public Application GetClientApplication() {
            return _clientApplication;
        }

        public ServicePrincipal GetClientApplicationSP() {
            return _clientApplicationSP;
        }

        public Application GetAKSApplication() {
            return _aksApplication;
        }

        public ServicePrincipal GetAKSApplicationSP() {
            return _aksApplicationSP;
        }

        public string GetAKSApplicationRbacSecret() {
            return _aksApplicationPasswordCredentialRbacSecret;
        }

        public async Task RegisterApplicationsAsync(
            string applicationName,
            DirectoryObject owner,
            IEnumerable<string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            tags ??= new List<string>();

            var serviceApplicationName = applicationName + "-service";
            var clientApplicationName = applicationName + "-client";
            var aksApplicationName = applicationName + "-aks";

            // Service Application /////////////////////////////////////////////
            // Register service application

            Log.Information("Creating service application registration...");

            _serviceApplication = await _msGraphServiceClient
                .RegisterServiceApplicationAsync(
                    serviceApplicationName,
                    owner,
                    tags,
                    cancellationToken
                );

            // Find service principal for service application
            _serviceApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_serviceApplication, cancellationToken);

            // Add current user or service principal as app owner for service
            // application, if it is not owner already.
            await _msGraphServiceClient
                .AddAsApplicationOwnerAsync(
                    _serviceApplication,
                    owner,
                    cancellationToken
                );

            // Client Application //////////////////////////////////////////////
            // Register client application

            Log.Information("Creating client application registration...");

            _clientApplication = await _msGraphServiceClient
                .RegisterClientApplicationAsync(
                    _serviceApplication,
                    clientApplicationName,
                    tags,
                    cancellationToken
                );

            // Find service principal for client application
            _clientApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_clientApplication, cancellationToken);

            // Add current user or service principal as app owner for client
            // application, if it is not owner already.
            await _msGraphServiceClient
                .AddAsApplicationOwnerAsync(
                    _clientApplication,
                    owner,
                    cancellationToken
                );

            // Update service application to include client applicatoin as knownClientApplications
            _serviceApplication = await _msGraphServiceClient
                .AddAsKnownClientApplicationAsync(
                    _serviceApplication,
                    _clientApplication,
                    cancellationToken
                );

            // Grant admin consent for service application "user_impersonation" API permissions of client applicatoin
            // Grant admin consent for Microsoft Graph "User.Read" API permissions of client applicatoin
            await _msGraphServiceClient
                .GrantAdminConsentToClientApplicationAsync(
                    _serviceApplicationSP,
                    _clientApplicationSP,
                    cancellationToken
                );

            // App Registration for AKS ////////////////////////////////////////
            // Register aks application

            Log.Information("Creating AKS application registration...");

            var registrationResult = await _msGraphServiceClient
                .RegisterAKSApplicationAsync(
                    aksApplicationName,
                    tags,
                    cancellationToken
                );

            _aksApplication = registrationResult.Item1;
            _aksApplicationPasswordCredentialRbacSecret = registrationResult.Item2;

            // Find service principal for aks application
            _aksApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_aksApplication, cancellationToken);

            // Add current user or service principal as app owner for aks
            // application, if it is not owner already.
            await _msGraphServiceClient
                .AddAsApplicationOwnerAsync(
                    _aksApplication,
                    owner,
                    cancellationToken
                );
        }

        public async Task LoadAsync(
            Guid serviceApplicationId,
            Guid clientApplicationId,
            Guid aksApplicationId,
            string aksApplicatoinRbacSecret,
            CancellationToken cancellationToken = default
        ) {
            Log.Information("Retrieving service application registration...");
            _serviceApplication = await _msGraphServiceClient
                .GetApplicationAsync(serviceApplicationId, cancellationToken);

            _serviceApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_serviceApplication, cancellationToken);

            Log.Information("Retrieving client application registration...");
            _clientApplication = await _msGraphServiceClient
                .GetApplicationAsync(clientApplicationId, cancellationToken);

            _clientApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_clientApplication, cancellationToken);

            Log.Information("Retrieving AKS application registration...");
            _aksApplication = await _msGraphServiceClient
                .GetApplicationAsync(aksApplicationId, cancellationToken);

            _aksApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_aksApplication, cancellationToken);

            _aksApplicationPasswordCredentialRbacSecret = aksApplicatoinRbacSecret;
        }

        public async Task DeleteApplicationsAsync(
            CancellationToken cancellationToken = default
        ) {
            // Service Application
            if (null != _serviceApplicationSP) {
                try {
                    await _msGraphServiceClient
                        .DeleteServicePrincipalAsync(
                            _serviceApplicationSP,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete ServicePrincipal of Service Application");
                }
            }

            if (null != _serviceApplication) {
                try {
                    await _msGraphServiceClient
                        .DeleteApplicationAsync(
                            _serviceApplication,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete Service Application");
                }
            }

            // Client Application
            if (null != _clientApplicationSP) {
                try {
                    await _msGraphServiceClient
                        .DeleteServicePrincipalAsync(
                            _clientApplicationSP,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete ServicePrincipal of Client Application");
                }
            }

            if (null != _clientApplication) {
                try {
                    await _msGraphServiceClient
                        .DeleteApplicationAsync(
                            _clientApplication,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete Client Application");
                }
            }

            // AKS Application
            if (null != _aksApplicationSP) {
                try {
                    await _msGraphServiceClient
                        .DeleteServicePrincipalAsync(
                            _aksApplicationSP,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete ServicePrincipal of AKS Application");
                }
            }

            if (null != _aksApplication) {
                try {
                    await _msGraphServiceClient
                        .DeleteApplicationAsync(
                            _aksApplication,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete AKS Application");
                }
            }
        }

        public async Task UpdateClientApplicationRedirectUrisAsync(
            string applicationURL,
            CancellationToken cancellationToken = default
        ) {
            if (null == applicationURL) {
                throw new ArgumentNullException("applicationURL");
            }

            if (applicationURL.Trim() == string.Empty) {
                throw new ArgumentException("Input cannot be empty", "applicationURL");
            }

            if (!applicationURL.StartsWith("https://") && !applicationURL.StartsWith("http://")) {
                applicationURL = $"https://{applicationURL}";
            }

            if (!applicationURL.EndsWith("/")) {
                applicationURL += "/";
            }

            var redirectUris = new List<string> {
                $"{applicationURL}",
                $"{applicationURL}registry/",
                $"{applicationURL}twin/",
                $"{applicationURL}history/",
                $"{applicationURL}ua/",
                $"{applicationURL}vault/"
            };

            _clientApplication = await _msGraphServiceClient
                .UpdateRedirectUrisAsync(
                    _clientApplication,
                    redirectUris,
                    cancellationToken
                );
        }
    }
}
