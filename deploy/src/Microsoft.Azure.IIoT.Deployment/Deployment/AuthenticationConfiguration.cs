// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using System;
    using Microsoft.Azure.Management.ResourceManager.Fluent;

    class AuthenticationConfiguration {

        public AzureEnvironment AzureEnvironment { get; }
        public Guid TenantId { get; }
        public Guid ClientId { get; }
        public string ClientSecret { get; }

        public AuthenticationConfiguration(
            AzureEnvironment azureEnvironment,
            Guid tenantId,
            Guid clientId,
            string clientSecret = null
        ) {
            AzureEnvironment = azureEnvironment;
            TenantId = tenantId;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }
    }
}
