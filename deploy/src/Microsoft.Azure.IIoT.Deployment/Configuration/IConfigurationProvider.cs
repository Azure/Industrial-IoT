// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Identity.Client;

    interface IConfigurationProvider {

        AzureEnvironment SelectEnvironment(
            IEnumerable<AzureEnvironment> azureEnvironments
        );

        Guid GetTenantId();

        ISubscription SelectSubscription(
            IEnumerable<ISubscription> subscriptionsList
        );

        IAccount SelectAccount(
            IEnumerable<IAccount> accounts
        );

        string GetApplicationName();

        bool CheckIfUseExistingResourceGroup();

        IResourceGroup SelectExistingResourceGroup(
            IEnumerable<IResourceGroup> resourceGroups
        );

        Region SelectResourceGroupRegion(
            IEnumerable<Region> regions
        );

        string SelectResourceGroupName(
            Func<string, bool> checkIfResourceGroupExists,
            string resourceGroupDefaultName = null
        );

        bool CheckIfSaveEnvFile();

        bool CheckIfPerformCleanup();
    }
}
