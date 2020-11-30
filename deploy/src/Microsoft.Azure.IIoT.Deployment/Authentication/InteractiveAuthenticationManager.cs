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

    /// <summary>
    /// Authentication manager that will use interactive authentication flow for Windows.
    /// </summary>
    class InteractiveAuthenticationManager : IAuthenticationManager {

        public static readonly string[] MicrosoftGraphIAIScopes = new string[] {
            "https://graph.microsoft.com/Directory.AccessAsUser.All"
        };

        public static readonly string[] AzureManagementIAIScopes = new string[] {
            "https://management.azure.com/user_impersonation"
        };

        public static readonly string[] KeyVaultIAIScopes = new string[] {
            "https://vault.azure.net/user_impersonation"
        };


        private readonly AzureEnvironment _azureEnvironment;
        private readonly Guid _tenantId;
        private readonly IPublicClientApplication _publicClientApplication;

        private IAccount _account;

        public InteractiveAuthenticationManager(
            AzureEnvironment azureEnvironment,
            Guid tenantId,
            Guid applicationClientId
        ) {
            _azureEnvironment = azureEnvironment;
            var azureCloudInstance = azureEnvironment.ToAzureCloudInstance();

            _tenantId = tenantId;

            _publicClientApplication = PublicClientApplicationBuilder
                .Create(applicationClientId.ToString())
                .WithAuthority(azureCloudInstance, tenantId)
                //.WithAuthority(azureCloudInstance, AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithDefaultRedirectUri()
                .Build();
        }

        /// <inheritdoc/>
        public async Task AuthenticateAsync(
            CancellationToken cancellationToken = default
        ) {
            // We will use interactive authentication flow for Windows.
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Acquiring-tokens-interactively

            // ToDo: Add timeout.
            var microsoftGraphAuthenticationResult = await _publicClientApplication
                .AcquireTokenInteractive(MicrosoftGraphIAIScopes)
                .WithExtraScopesToConsent(AzureManagementIAIScopes)
                .WithExtraScopesToConsent(KeyVaultIAIScopes)
                //.WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(cancellationToken);

            // Extract account from microsoftGraphAuthenticationResult
            _account = microsoftGraphAuthenticationResult.Account;

            // Validate that we have received Tokens.
            await AcquireMicrosoftGraphAuthenticationResultAsync(cancellationToken);
            await AcquireAzureManagementAuthenticationResultAsync(cancellationToken);
            await AcquireKeyVaultAuthenticationResultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public AzureEnvironment GetAzureEnvironment() {
            return _azureEnvironment;
        }

        /// <inheritdoc/>
        public Guid GetTenantId() {
            return _tenantId;
        }

        /// <inheritdoc/>
        public IAccount GetAccount() {
            return _account;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> AcquireMicrosoftGraphAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        ) {
            // Fetch AccessToken from cache
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token
            var authenticationResult = await _publicClientApplication
                .AcquireTokenSilent(MicrosoftGraphIAIScopes, _account)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> AcquireAzureManagementAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        ) {
            // Fetch AccessToken from cache
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token
            var authenticationResult = await _publicClientApplication
                .AcquireTokenSilent(AzureManagementIAIScopes, _account)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> AcquireKeyVaultAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        ) {
            // Fetch AccessToken from cache
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token
            var authenticationResult = await _publicClientApplication
                .AcquireTokenSilent(KeyVaultIAIScopes, _account)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        /// <inheritdoc/>
        public bool IsUserAuthenticationFlow() {
            return true;
        }
    }
}
