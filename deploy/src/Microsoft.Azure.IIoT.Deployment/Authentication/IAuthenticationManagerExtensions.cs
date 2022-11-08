// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Authentication {

    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Identity.Client;
    using Microsoft.Rest;

    public static class IAuthenticationManagerExtensions {

        public static async Task<TokenCredentials> GetMicrosoftGraphTokenCredentialsAsync(
            this IAuthenticationManager authenticationManager,
            CancellationToken cancellationToken = default
        ) {
            var microsoftGraphAuthenticationResult = await authenticationManager
                .AcquireMicrosoftGraphAuthenticationResultAsync(cancellationToken);

            var microsoftGraphTokenCredentials = authenticationManager
                .GenerateTokenCredentials(microsoftGraphAuthenticationResult);
            return microsoftGraphTokenCredentials;
        }

        public static TokenCredentials GetMicrosoftGraphDelegatingTokenCredentials(
            this IAuthenticationManager authenticationManager
        ) {
            var microsoftGraphTokenCredentials = authenticationManager
                .GenerateDelegatingTokenCredentials(
                    authenticationManager.AcquireMicrosoftGraphAuthenticationResultAsync
                );
            return microsoftGraphTokenCredentials;
        }

        public static async Task<TokenCredentials> GetAzureManagementTokenCredentialsAsync(
            this IAuthenticationManager authenticationManager,
            CancellationToken cancellationToken = default
        ) {
            var azureManagementAuthenticationResult = await authenticationManager
                .AcquireAzureManagementAuthenticationResultAsync(cancellationToken);

            var azureManagementTokenCredentials = authenticationManager
                .GenerateTokenCredentials(azureManagementAuthenticationResult);
            return azureManagementTokenCredentials;
        }

        public static TokenCredentials GetAzureManagementDelegatingTokenCredentials(
            this IAuthenticationManager authenticationManager
        ) {
            var azureManagementTokenCredentials = authenticationManager
                .GenerateDelegatingTokenCredentials(
                    authenticationManager.AcquireAzureManagementAuthenticationResultAsync
                );
            return azureManagementTokenCredentials;
        }

        public static async Task<TokenCredentials> GetKeyVaultTokenCredentialsAsync(
            this IAuthenticationManager authenticationManager,
            CancellationToken cancellationToken = default
        ) {
            var keyVaultAuthenticationResult = await authenticationManager
                .AcquireKeyVaultAuthenticationResultAsync(cancellationToken);

            var keyVaultTokenCredentials = authenticationManager
                .GenerateTokenCredentials(keyVaultAuthenticationResult);
            return keyVaultTokenCredentials;
        }

        public static TokenCredentials GetKeyVaultDelegatingTokenCredentials(
            this IAuthenticationManager authenticationManager
        ) {
            var keyVaultTokenCredentials = authenticationManager
                .GenerateDelegatingTokenCredentials(
                    authenticationManager.AcquireKeyVaultAuthenticationResultAsync
                 );
            return keyVaultTokenCredentials;
        }

        public static async Task<AzureCredentials> GetAzureCredentialsAsync(
            this IAuthenticationManager authenticationManager,
            CancellationToken cancellationToken = default
        ) {
            var azureManagementTokenCredentials = await authenticationManager
                .GetAzureManagementTokenCredentialsAsync(cancellationToken);
            var microsoftGraphTokenCredentials = await authenticationManager
                .GetMicrosoftGraphTokenCredentialsAsync(cancellationToken);

            var azureEnvironment = authenticationManager.GetAzureEnvironment();
            var tenantId = authenticationManager.GetTenantId();

            var azureCredentials = new AzureCredentials(
                azureManagementTokenCredentials,
                microsoftGraphTokenCredentials,
                tenantId.ToString(),
                azureEnvironment
            );

            return azureCredentials;
        }

        public static AzureCredentials GetDelegatingAzureCredentials(
            this IAuthenticationManager authenticationManager
        ) {
            var azureManagementTokenCredentials = authenticationManager
                .GetAzureManagementDelegatingTokenCredentials();
            var microsoftGraphTokenCredentials = authenticationManager
                .GetMicrosoftGraphDelegatingTokenCredentials();

            var azureEnvironment = authenticationManager.GetAzureEnvironment();
            var tenantId = authenticationManager.GetTenantId();

            var azureCredentials = new AzureCredentials(
                azureManagementTokenCredentials,
                microsoftGraphTokenCredentials,
                tenantId.ToString(),
                azureEnvironment
            );

            return azureCredentials;
        }

        public static TokenCredentials GenerateTokenCredentials(
            this IAuthenticationManager authenticationManager,
            AuthenticationResult authenticationResult
        ) {
            ITokenProvider tokenProvider = new StringTokenProvider(authenticationResult.AccessToken, "Bearer");

            var tenantId = authenticationManager.GetTenantId();
            var account = authenticationManager.GetAccount();

            TokenCredentials tokenCredentials;

            // Some authentication flows result in null account.
            if (account?.Username != null) {
                tokenCredentials = new TokenCredentials(
                    tokenProvider,
                    tenantId.ToString(),
                    account.Username
                );
            }
            else {
                tokenCredentials = new TokenCredentials(tokenProvider);
            }

            return tokenCredentials;
        }

        public static ITokenProvider GenerateDelegatingTokenProvider(
            this IAuthenticationManager authenticationManager,
            Func<CancellationToken, Task<AuthenticationResult>> authenticationResultProvider
        ) {
            async Task<string> accessTokenProvider(CancellationToken cancellationToken) {
                var authenticationResult = await authenticationResultProvider(cancellationToken);
                return authenticationResult.AccessToken;
            };

            ITokenProvider tokenProvider = new DelegatingTokenProvider(accessTokenProvider);

            return tokenProvider;
        }

        public static TokenCredentials GenerateDelegatingTokenCredentials(
            this IAuthenticationManager authenticationManager,
            Func<CancellationToken, Task<AuthenticationResult>> authenticationResultProvider
        ) {
            var tokenProvider = authenticationManager.GenerateDelegatingTokenProvider(authenticationResultProvider);

            var tenantId = authenticationManager.GetTenantId();
            var account = authenticationManager.GetAccount();

            TokenCredentials tokenCredentials;

            // Some authentication flows result in null account.
            if (account?.Username != null) {
                tokenCredentials = new TokenCredentials(
                    tokenProvider,
                    tenantId.ToString(),
                    account.Username
                );
            } else {
                tokenCredentials = new TokenCredentials(tokenProvider);
            }

            return tokenCredentials;
        }
    }
}
