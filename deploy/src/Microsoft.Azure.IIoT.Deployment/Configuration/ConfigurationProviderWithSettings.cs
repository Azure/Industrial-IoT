// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.Deployment.Deployment;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    abstract class ConfigurationProviderWithSettings : IConfigurationProvider {

        protected readonly AppSettings _appSettings;

        public ConfigurationProviderWithSettings(
            AppSettings appSettings
        ) {
            _appSettings = appSettings;
        }

        public abstract RunMode GetRunMode();

        public abstract AuthenticationConfiguration GetAuthenticationConfiguration(
            IEnumerable<AzureEnvironment> azureEnvironments
        );

        public abstract ISubscription GetSubscription(
            IEnumerable<ISubscription> subscriptionsList
        );

        public abstract string GetApplicationName();

        public abstract bool IfUseExistingResourceGroup();

        public abstract IResourceGroup GetExistingResourceGroup(
            IEnumerable<IResourceGroup> resourceGroups
        );

        public abstract Tuple<Region, string> GetNewResourceGroup(
            IEnumerable<Region> regions,
            Func<string, bool> ifResourceGroupExists,
            string resourceGroupDefaultName = null
        );

        public abstract ApplicationRegistrationDefinition GetApplicationRegistrationDefinition();

        public abstract string GetApplicationURL();

        public abstract bool IfSaveEnvFile();

        public abstract bool IfPerformCleanup();
    }
}
