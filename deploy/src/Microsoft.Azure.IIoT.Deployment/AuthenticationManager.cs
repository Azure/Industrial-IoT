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
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Identity.Client;
    using Microsoft.Rest;

    class AuthenticationManager {

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
            var azureCloudInstance = ToAzureCloudInstance(azureEnvironment);

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
            await AcquireMicrosoftGraphTokenAsync(cancellationToken);
            await AcquireAzureManagementTokenAsync(cancellationToken);
            await AcquireKeyVaultTokenAsync(cancellationToken);
        }

        public async Task<AuthenticationResult> AcquireMicrosoftGraphTokenAsync(
            CancellationToken cancellationToken = default
        ) {
            // Fetch AccessToken from cache
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token
            var authenticationResult = await _publicClientApplication
                .AcquireTokenSilent(MicrosoftGraphIAIScopes, _account)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        public async Task<AuthenticationResult> AcquireAzureManagementTokenAsync(
            CancellationToken cancellationToken = default
        ) {
            // Fetch AccessToken from cache
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token
            var authenticationResult = await _publicClientApplication
                .AcquireTokenSilent(AzureManagementIAIScopes, _account)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        public async Task<AuthenticationResult> AcquireKeyVaultTokenAsync(
            CancellationToken cancellationToken = default
        ) {
            // Fetch AccessToken from cache
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token
            var authenticationResult = await _publicClientApplication
                .AcquireTokenSilent(KeyVaultIAIScopes, _account)
                .ExecuteAsync(cancellationToken);

            return authenticationResult;
        }

        public async Task<TokenCredentials> GetMicrosoftGraphTokenCredentialsAsync(
            CancellationToken cancellationToken = default
        ) {
            var microsoftGraphAuthenticatoinResult = await AcquireMicrosoftGraphTokenAsync(cancellationToken);
            var microsoftGraphTokenCredentials = GenerateTokenCredentials(microsoftGraphAuthenticatoinResult);
            return microsoftGraphTokenCredentials;
        }

        public TokenCredentials GetMicrosoftGraphDelegatingTokenCredentials() {
            var microsoftGraphTokenCredentials = GenerateDelegatingTokenCredentials(AcquireMicrosoftGraphTokenAsync);
            return microsoftGraphTokenCredentials;
        }

        public async Task<TokenCredentials> GetAzureManagementTokenCredentialsAsync(
            CancellationToken cancellationToken = default
        ) {
            var azureManagementAuthenticatoinResult = await AcquireAzureManagementTokenAsync(cancellationToken);
            var azureManagementTokenCredentials = GenerateTokenCredentials(azureManagementAuthenticatoinResult);
            return azureManagementTokenCredentials;
        }

        public TokenCredentials GetAzureManagementDelegatingTokenCredentials() {
            var azureManagementTokenCredentials = GenerateDelegatingTokenCredentials(AcquireAzureManagementTokenAsync);
            return azureManagementTokenCredentials;
        }

        public async Task<TokenCredentials> GetKeyVaultTokenCredentialsAsync(
            CancellationToken cancellationToken = default
        ) {
            var keyVaultAuthenticatoinResult = await AcquireKeyVaultTokenAsync(cancellationToken);
            var keyVaultTokenCredentials = GenerateTokenCredentials(keyVaultAuthenticatoinResult);
            return keyVaultTokenCredentials;
        }

        public TokenCredentials GetKeyVaultDelegatingTokenCredentials() {
            var keyVaultTokenCredentials = GenerateDelegatingTokenCredentials(AcquireKeyVaultTokenAsync);
            return keyVaultTokenCredentials;
        }

        public IAccount GetAccount() {
            return _account;
        }

        public Guid GetTenantId() {
            return _tenantId;
        }

        public async Task<AzureCredentials> GetAzureCredentialsAsync(
            CancellationToken cancellationToken = default
        ) {
            var azureManagementTokenCredentials = await GetAzureManagementTokenCredentialsAsync(cancellationToken);
            var microsoftGraphTokenCredentials = await GetMicrosoftGraphTokenCredentialsAsync(cancellationToken);

            var azureCredentials = new AzureCredentials(
                azureManagementTokenCredentials,
                microsoftGraphTokenCredentials,
                _tenantId.ToString(),
                _azureEnvironment
            );

            return azureCredentials;
        }

        public AzureCredentials GetDelegatingAzureCredentials() {
            var azureManagementTokenCredentials = GetAzureManagementDelegatingTokenCredentials();
            var microsoftGraphTokenCredentials = GetMicrosoftGraphDelegatingTokenCredentials();

            var azureCredentials = new AzureCredentials(
                azureManagementTokenCredentials,
                microsoftGraphTokenCredentials,
                _tenantId.ToString(),
                _azureEnvironment
            );

            return azureCredentials;
        }

        public static AzureCloudInstance ToAzureCloudInstance(AzureEnvironment azureEnvironment) {
            if (azureEnvironment.Equals(AzureEnvironment.AzureGlobalCloud)) {
                return AzureCloudInstance.AzurePublic;
            }
            else if (azureEnvironment.Equals(AzureEnvironment.AzureChinaCloud)) {
                return AzureCloudInstance.AzureChina;
            }
            else if (azureEnvironment.Equals(AzureEnvironment.AzureGermanCloud)) {
                return AzureCloudInstance.AzureGermany;
            }
            else if (azureEnvironment.Equals(AzureEnvironment.AzureUSGovernment)) {
                return AzureCloudInstance.AzureUsGovernment;
            }
            else {
                throw new SystemException("Unknown AzureEnvironment: " + azureEnvironment.Name);
            }
        }

        public static TokenCredentials GenerateTokenCredentials(
            AuthenticationResult authenticationResult
        ) {
            ITokenProvider tokenProvider = new StringTokenProvider(authenticationResult.AccessToken, "Bearer");

            var tokenCredentials = new TokenCredentials(
                tokenProvider,
                authenticationResult.TenantId,
                authenticationResult.Account.Username
            );

            return tokenCredentials;
        }

        public ITokenProvider GenerateDelegatingTokenProvider(
            Func<CancellationToken, Task<AuthenticationResult>> authenticationResultProvider
        ) {
            async Task<string> accessTokenProvider(CancellationToken cancellationToken) {
                var authenticationResult = await authenticationResultProvider(cancellationToken);
                return authenticationResult.AccessToken;
            };

            ITokenProvider tokenProvider = new DelegatingTokenProvider(accessTokenProvider);

            return tokenProvider;
        }

        public TokenCredentials GenerateDelegatingTokenCredentials(
            Func<CancellationToken, Task<AuthenticationResult>> authenticationResultProvider
        ) {
            var tokenProvider = GenerateDelegatingTokenProvider(authenticationResultProvider);

            var tokenCredentials = new TokenCredentials(
                tokenProvider,
                _tenantId.ToString(),
                _account.Username
            );

            return tokenCredentials;
        }
    }
}
