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

    /// <summary>
    /// Abstract class of IConfigurationProvider interface that has AppSettings.
    /// </summary>
    abstract class ConfigurationProviderWithSettings : IConfigurationProvider {

        protected readonly AppSettings _appSettings;

        /// <summary>
        /// Constructor of the abstract ConfigurationProviderWithSettings class.
        /// </summary>
        /// <param name="appSettings"> Settings provided to the application </param>
        public ConfigurationProviderWithSettings(
            AppSettings appSettings
        ) {
            _appSettings = appSettings;
        }

        /// <inheritdoc/>
        public abstract RunMode GetRunMode();

        /// <inheritdoc/>
        public abstract AuthenticationConfiguration GetAuthenticationConfiguration(
            IEnumerable<AzureEnvironment> azureEnvironments
        );

        /// <inheritdoc/>
        public abstract ISubscription GetSubscription(
            IEnumerable<ISubscription> subscriptionsList
        );

        /// <inheritdoc/>
        public abstract string GetApplicationName();

        /// <inheritdoc/>
        public abstract bool IfUseExistingResourceGroup();

        /// <inheritdoc/>
        public abstract IResourceGroup GetExistingResourceGroup(
            IEnumerable<IResourceGroup> resourceGroups
        );

        /// <inheritdoc/>
        public abstract Tuple<Region, string> GetNewResourceGroup(
            IEnumerable<Region> regions,
            Func<string, bool> ifResourceGroupExists,
            string resourceGroupDefaultName = null
        );

        /// <inheritdoc/>
        public abstract ApplicationRegistrationDefinition GetApplicationRegistrationDefinition();

        /// <inheritdoc/>
        public abstract string GetApplicationUrl();

        /// <inheritdoc/>
        public abstract HelmSettings GetHelmSettings();

        /// <inheritdoc/>
        public abstract bool IfSaveEnvFile();

        /// <inheritdoc/>
        public abstract bool IfPerformCleanup();
    }
}
