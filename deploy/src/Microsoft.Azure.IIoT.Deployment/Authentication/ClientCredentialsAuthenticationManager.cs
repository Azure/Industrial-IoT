// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Authentication {

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Identity.Client;

    using Microsoft.Azure.IIoT.Deployment.Infrastructure.Extensions;

    class ClientCredentialsAuthenticationManager : IAuthenticationManager {

        public static readonly string[] MicrosoftGraphIAIScopes = new string[] {
            "https://graph.microsoft.com/.default"
        };

        public static readonly string[] AzureManagementIAIScopes = new string[] {
            "https://management.azure.com/.default"
        };

        public static readonly string[] KeyVaultIAIScopes = new string[] {
            "https://vault.azure.net/.default"
        };


        private readonly AzureEnvironment _azureEnvironment;
        private readonly Guid _tenantId;
        private readonly IConfidentialClientApplication _confidentialClientApplication;

        private IAccount _account;

        public ClientCredentialsAuthenticationManager(
            AzureEnvironment azureEnvironment,
            Guid tenantId,
            Guid applicationClientId,
            string clientSecret
        ) {
            _azureEnvironment = azureEnvironment;
            var azureCloudInstance = azureEnvironment.ToAzureCloudInstance();

            _tenantId = tenantId;

            _confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(applicationClientId.ToString())
                .WithAuthority(azureCloudInstance, tenantId)
                .WithClientSecret(clientSecret)
                .Build();
        }

        public async Task AuthenticateAsync(
            CancellationToken cancellationToken = default
        ) {
            // We will use client credential authentication flow.
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-credential-flows

            var microsoftGraphAuthenticationResult = 
                await AcquireMicrosoftGraphAuthenticationResultAsync(cancellationToken);

            // Extract account from microsoftGraphAuthenticationResult
            _account = microsoftGraphAuthenticationResult.Account;

            // Validate that we have received Tokens.
            await AcquireMicrosoftGraphAuthenticationResultAsync(cancellationToken);
            await AcquireAzureManagementAuthenticationResultAsync(cancellationToken);
            await AcquireKeyVaultAuthenticationResultAsync(cancellationToken);
        }

        public AzureEnvironment GetAzureEnvironment() {
            return _azureEnvironment;
        }

        public Guid GetTenantId() {
            return _tenantId;
        }

        public IAccount GetAccount() {
            return _account;
        }

        public async Task<AuthenticationResult> AcquireMicrosoftGraphAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        ) {
            var authenticationResult = await _confidentialClientApplication
                .AcquireTokenForClient(MicrosoftGraphIAIScopes)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        public async Task<AuthenticationResult> AcquireAzureManagementAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        ) {
            var authenticationResult = await _confidentialClientApplication
                .AcquireTokenForClient(AzureManagementIAIScopes)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        public async Task<AuthenticationResult> AcquireKeyVaultAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        ) {
            var authenticationResult = await _confidentialClientApplication
                .AcquireTokenForClient(KeyVaultIAIScopes)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        public bool IsUserAuthenticationFlow() {
            return false;
        }
    }
}
