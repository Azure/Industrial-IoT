// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Cli {

    using System;
    using Microsoft.Azure.Management.ResourceManager.Fluent;

    public static class AzureEnvironmentTypeExtension {
        public static AzureEnvironment ToAzureEnvironment(this AzureEnvironmentType azureEnvironment) {
            return azureEnvironment switch
            {
                AzureEnvironmentType.AzureGlobalCloud => AzureEnvironment.AzureGlobalCloud,
                AzureEnvironmentType.AzureChinaCloud => AzureEnvironment.AzureChinaCloud,
                AzureEnvironmentType.AzureUSGovernment => AzureEnvironment.AzureUSGovernment,
                AzureEnvironmentType.AzureGermanCloud => AzureEnvironment.AzureGermanCloud,
                _ => throw new ArgumentException($"Unrecognized AzureEnvironmentType: {azureEnvironment.ToString()}")
            };
        }
    }
}
