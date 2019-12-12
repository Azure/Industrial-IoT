// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using System;
    using System.Collections.Generic;
    using System.Text;

    class AuthenticationConfiguration {
        public AzureEnvironment AzureEnvironment { get; set; }
        public Guid TenantId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientSecret { get; set; }

        public AuthenticationConfiguration() { }

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
