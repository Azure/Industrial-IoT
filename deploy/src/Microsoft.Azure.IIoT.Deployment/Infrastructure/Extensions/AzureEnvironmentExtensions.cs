// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure.Extensions {

    using System;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Identity.Client;

    public static class AzureEnvironmentExtensions {

        /// <summary>
        /// Map AzureEnvironment to AzureCloudInstance
        /// </summary>
        /// <param name="azureEnvironment"></param>
        /// <returns></returns>
        public static AzureCloudInstance ToAzureCloudInstance(this AzureEnvironment azureEnvironment) {
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
                throw new Exception($"Unknown AzureEnvironment: {azureEnvironment.Name}");
            }
        }
    }
}
