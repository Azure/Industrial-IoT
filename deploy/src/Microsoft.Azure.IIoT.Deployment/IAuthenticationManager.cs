// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Identity.Client;

    public interface IAuthenticationManager {

        public  Task AuthenticateAsync(
            CancellationToken cancellationToken = default
        );

        public AzureEnvironment GetAzureEnvironment();

        public Guid GetTenantId();

        public IAccount GetAccount();

        public Task<AuthenticationResult> AcquireMicrosoftGraphAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        );

        public Task<AuthenticationResult> AcquireAzureManagementAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        );

        public Task<AuthenticationResult> AcquireKeyVaultAuthenticationResultAsync(
            CancellationToken cancellationToken = default
        );
    }
}
