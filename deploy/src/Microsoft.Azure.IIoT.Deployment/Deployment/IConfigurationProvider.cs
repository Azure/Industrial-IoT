// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    interface IConfigurationProvider {

        public RunMode GetRunMode();

        public AuthenticationConfiguration GetAuthenticationConfiguration(
            IEnumerable<AzureEnvironment> azureEnvironments
        );

        ISubscription GetSubscription(
            IEnumerable<ISubscription> subscriptionsList
        );

        string GetApplicationName();

        bool IfUseExistingResourceGroup();

        IResourceGroup GetExistingResourceGroup(
            IEnumerable<IResourceGroup> resourceGroups
        );

        Tuple<Region, string> GetNewResourceGroup(
            IEnumerable<Region> regions,
            Func<string, bool> ifResourceGroupExists,
            string resourceGroupDefaultName = null
        );

        ApplicationRegistrationDefinition GetApplicationRegistrationDefinition();

        string GetApplicationURL();

        bool IfSaveEnvFile();

        bool IfPerformCleanup();
    }
}
