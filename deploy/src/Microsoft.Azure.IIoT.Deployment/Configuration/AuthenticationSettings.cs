// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;

    class AuthenticationSettings {
        public AzureEnvironmentType? AzureEnvironment { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
