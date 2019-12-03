// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Identity.Client;

    using Microsoft.Azure.IIoT.Deployment.Infrastructure.Extensions;

    class AuthenticationManager : IAuthenticationManager {

        // ClientId of AzureIndustrialIoTIAI Application
        public const string AzureIndustrialIoTDeploymentClientID = "fb2ca262-60d8-4167-ac33-1998d6d5c50b";

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
        private readonly IPublicClientApplication _publicClientApplication;

        private IAccount _account;
        private Guid _tenantId;

        public AuthenticationManager(
            AzureEnvironment azureEnvironment,
            string tenant
        ) {
            _azureEnvironment = azureEnvironment;
            var azureCloudInstance = azureEnvironment.ToAzureCloudInstance();

            _publicClientApplication = PublicClientApplicationBuilder
                .Create(AzureIndustrialIoTDeploymentClientID)
                .WithAuthority(azureCloudInstance, tenant)
                //.WithAuthority(azureCloudInstance, AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithDefaultRedirectUri()
                .Build();
        }

        public async Task AuthenticateAsync(
            CancellationToken cancellationToken = default
        ) {
            AuthenticationResult microsoftGraphAuthenticatoinResult;

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (isWindows) {
                // We will use interactive authentication flow for Windows.
                // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Acquiring-tokens-interactively

                // ToDo: Add timeout.
                microsoftGraphAuthenticatoinResult = await _publicClientApplication
                    .AcquireTokenInteractive(MicrosoftGraphIAIScopes)
                    .WithExtraScopesToConsent(AzureManagementIAIScopes)
                    .WithExtraScopesToConsent(KeyVaultIAIScopes)
                    //.WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(cancellationToken);
            }
            else {
                // We will use device code flow authentication for Unix systems.
                // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Device-Code-Flow

                // ToDo: Add timeout.
                microsoftGraphAuthenticatoinResult = await _publicClientApplication
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
            }

            // Extract account and tenant ID from microsoftGraphAuthenticatoinResult
            _account = microsoftGraphAuthenticatoinResult.Account;
            _tenantId = new Guid(microsoftGraphAuthenticatoinResult.TenantId);

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
            // Fetch AccessToken from cache
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token
            var authenticationResult = await _publicClientApplication
                .AcquireTokenSilent(MicrosoftGraphIAIScopes, _account)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

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
    }
}
