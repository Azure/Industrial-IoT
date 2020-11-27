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
    /// Authentication manager that will use device code authentication flow for Unix systems.
    /// </summary>
    class CodeFlowAuthenticationManager : IAuthenticationManager {

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

        public CodeFlowAuthenticationManager(
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
            // We will use device code authentication flow for Unix systems.
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Device-Code-Flow

            // ToDo: Add timeout.
            var microsoftGraphAuthenticationResult = await _publicClientApplication
                .AcquireTokenWithDeviceCode(
                    MicrosoftGraphIAIScopes,
                    deviceCodeResult => {
                        // This will print the message on the console which tells the user where to go sign-in using
                        // a separate browser and the code to enter once they sign in.
                        // The AcquireTokenWithDeviceCode() method will poll the server after firing this
                        // device code callback to look for the successful login of the user via that browser.
                        // This background polling (whose interval and timeout data is also provided as fields in the
                        // deviceCodeCallback class) will occur until:
                        // * The user has successfully logged in via browser and entered the proper code
                        // * The timeout specified by the server for the lifetime of this code (typically ~15 minutes) has been reached
                        // * The developing application calls the Cancel() method on a CancellationToken sent into the method.
                        //   If this occurs, an OperationCanceledException will be thrown (see catch below for more details).
                        Console.WriteLine(deviceCodeResult.Message);
                        return Task.FromResult(0);
                    })
                .ExecuteAsync();

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
