// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.Deployment.Configuration;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    interface IConfigurationProvider {

        /// <summary>
        /// Get run mode for the application.
        /// </summary>
        /// <returns></returns>
        public RunMode GetRunMode();

        /// <summary>
        /// Get authentication details for Azure.
        /// </summary>
        /// <param name="azureEnvironments"></param>
        /// <returns> List of available Azure environments </returns>
        public AuthenticationConfiguration GetAuthenticationConfiguration(
            IEnumerable<AzureEnvironment> azureEnvironments
        );

        /// <summary>
        /// Select a subscription from a list of available ones.
        /// </summary>
        /// <param name="subscriptionsList"></param>
        /// <returns></returns>
        ISubscription GetSubscription(
            IEnumerable<ISubscription> subscriptionsList
        );

        /// <summary>
        /// Get name of Azure IIoT deployment instance.
        /// </summary>
        /// <returns></returns>
        string GetApplicationName();

        /// <summary>
        /// Check whether to use an existing resource group for the deployment.
        /// </summary>
        /// <returns></returns>
        bool IfUseExistingResourceGroup();

        /// <summary>
        /// Select a resource group that should be used for deployment from a list of existing ones.
        /// </summary>
        /// <param name="resourceGroups"> List of existing resource groups </param>
        /// <returns></returns>
        IResourceGroup GetExistingResourceGroup(
            IEnumerable<IResourceGroup> resourceGroups
        );

        /// <summary>
        /// Get new resource group region and name.
        /// </summary>
        /// <param name="regions"> List of valid regions </param>
        /// <param name="ifResourceGroupExists"> Callback to check if resource group already exists </param>
        /// <param name="resourceGroupDefaultName"> Default name for the resource group </param>
        /// <returns></returns>
        Tuple<Region, string> GetNewResourceGroup(
            IEnumerable<Region> regions,
            Func<string, bool> ifResourceGroupExists,
            string resourceGroupDefaultName = null
        );

        /// <summary>
        /// Get Azure App Registration details for service, client and aks apps.
        /// </summary>
        /// <returns></returns>
        ApplicationRegistrationDefinition GetApplicationRegistrationDefinition();

        /// <summary>
        /// Get application base URL that should be used for generating RedirectUris.
        /// </summary>
        /// <returns></returns>
        string GetApplicationUrl();

        /// <summary>
        /// Get Helm chart settings.
        /// </summary>
        /// <returns></returns>
        HelmSettings GetHelmSettings();

        /// <summary>
        /// Check whether to create .env file after deployment or not.
        /// </summary>
        /// <returns></returns>
        bool IfSaveEnvFile();

        /// <summary>
        /// Check whether to perform cleanup if deployment error occurs.
        /// </summary>
        /// <returns></returns>
        bool IfPerformCleanup();
    }
}
