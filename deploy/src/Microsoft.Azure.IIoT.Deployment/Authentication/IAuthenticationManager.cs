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

    public interface IAuthenticationManager {

        /// <summary>
        /// Authenticate with Microsoft Identity Platform.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task AuthenticateAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Get Azure Environment containing resource endpoints.
        /// </summary>
        /// <returns></returns>
        public AzureEnvironment GetAzureEnvironment();

        /// <summary>
        /// Get TenantId that has been used.
        /// </summary>
        /// <returns></returns>
        public Guid GetTenantId();

        /// <summary>
        /// Get user account used for authentication. Will be null for non-user
        /// based authentication flows.
        /// </summary>
        /// <returns></returns>
        public IAccount GetAccount();

        /// <summary>
        /// Get AuthenticationResult object containing AccessToken for Microsoft
        /// Graph endpoints (https://graph.microsoft.com/).
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<AuthenticationResult> AcquireMicrosoftGraphAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Get AuthenticationResult object containing AccessToken for Azure
        /// Management endpoints (https://management.azure.com/).
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<AuthenticationResult> AcquireAzureManagementAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Get AuthenticationResult object containing AccessToken for KeyVault
        /// endpoints (https://vault.azure.net/).
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<AuthenticationResult> AcquireKeyVaultAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Shows whether an user-credentials-based authentication flow has been used.
        /// </summary>
        /// <returns></returns>
        public bool IsUserAuthenticationFlow();
    }
}
