// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.IIoT.Deployment.Cli;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    abstract class ConfigurationProviderWithSettings : IConfigurationProvider {

        protected readonly AppSettings _appSettings;

        public ConfigurationProviderWithSettings(
            AppSettings appSettings = null
        ) {
            _appSettings = appSettings;
        }

        protected abstract AzureEnvironment SelectEnvironmentImp(
            IEnumerable<AzureEnvironment> azureEnvironments
        );

        public AzureEnvironment SelectEnvironment(
            IEnumerable<AzureEnvironment> azureEnvironments
        ) {
            if (null != _appSettings?.Auth?.AzureEnvironment && _appSettings.Auth.AzureEnvironment.HasValue) {
                var azureEnvironment = _appSettings.Auth.AzureEnvironment.Value.ToAzureEnvironment();

                if (azureEnvironments.Contains(azureEnvironment)) {
                    Log.Information($"Configured Azure environment will be used: {ToString(azureEnvironment)}");
                    return azureEnvironment;
                }
                else {
                    throw new ArgumentException($"Configured '{ToString(azureEnvironment)}' Azure environment not found.");
                }
            }

            return SelectEnvironmentImp(azureEnvironments);
        }

        protected abstract Guid GetTenantIdImp();

        public Guid GetTenantId() {
            if (null != _appSettings?.Auth?.TenantId && _appSettings.Auth.TenantId.HasValue) {
                return _appSettings.Auth.TenantId.Value;
            }

            return GetTenantIdImp();
        }

        protected abstract ISubscription SelectSubscriptionImp(
            IEnumerable<ISubscription> subscriptionsList
        );

        public ISubscription SelectSubscription(
            IEnumerable<ISubscription> subscriptionsList
        ) {
            if (null != _appSettings?.SubscriptionId && _appSettings.SubscriptionId.HasValue) {
                var results = subscriptionsList
                    .Where(subscriptions => subscriptions.SubscriptionId == _appSettings.SubscriptionId.Value.ToString())
                    .ToList();

                if (results.Count > 0) {
                    Log.Information($"Configured subscription will be used: {ToString(results.First())}");
                    return results.First();
                }
                else {
                    throw new ArgumentException($"Configured '{_appSettings.SubscriptionId.Value}' subscription not found.");
                }
            }

            return SelectSubscriptionImp(subscriptionsList);
        }

        protected abstract string GetApplicationNameImp();

        public string GetApplicationName() {
            if (!string.IsNullOrEmpty(_appSettings?.ApplicationName)) {
                Log.Information($"Configured application name will be used: {_appSettings.ApplicationName}");
                return _appSettings.ApplicationName;
            }

            return GetApplicationNameImp();
        }

        protected abstract bool CheckIfUseExistingResourceGroupImp();

        public bool CheckIfUseExistingResourceGroup() {
            if (null != _appSettings?.ResourceGroup?.UseExisting && _appSettings.ResourceGroup.UseExisting.HasValue) {
                Log.Information($"Configured to use existing resource group: {_appSettings.ResourceGroup.UseExisting.Value}");
                return _appSettings.ResourceGroup.UseExisting.Value;
            }

            return CheckIfUseExistingResourceGroupImp();
        }

        protected abstract IResourceGroup SelectExistingResourceGroupImp(
            IEnumerable<IResourceGroup> resourceGroups
        );

        public IResourceGroup SelectExistingResourceGroup(
            IEnumerable<IResourceGroup> resourceGroups
        ) {
            if (!string.IsNullOrEmpty(_appSettings?.ResourceGroup?.Name)) {
                var results = resourceGroups
                    .Where(rg => rg.Name == _appSettings.ResourceGroup.Name)
                    .ToList();

                if (results.Count > 0) {
                    Log.Information($"Configured resource group will be used: {ToString(results.First())}");
                    return results.First();
                }
                else {
                    throw new ArgumentException($"Configured '{_appSettings.ResourceGroup.Name}' resource group not found.");
                }
            }

            return SelectExistingResourceGroupImp(resourceGroups);
        }

        protected abstract Region SelectResourceGroupRegionImp(
            IEnumerable<Region> regions
        );

        public Region SelectResourceGroupRegion(
            IEnumerable<Region> regions
        ) {
            if (null != _appSettings?.ResourceGroup?.Region && _appSettings.ResourceGroup.Region.HasValue) {
                var region = _appSettings.ResourceGroup.Region.Value.ToRegion();

                if (regions.Contains(region)) {
                    Log.Information($"Configured region will be used: {ToString(region)}");
                    return region;
                }
                else {
                    throw new ArgumentException($"Configured '{ToString(region)}' region not found.");
                }
            }

            return SelectResourceGroupRegionImp(regions);
        }

        protected abstract string SelectNewResourceGroupNameImp(
            Func<string, bool> checkIfResourceGroupExists,
            string resourceGroupDefaultName = null
        );

        public string SelectNewResourceGroupName(
            Func<string, bool> checkIfResourceGroupExists,
            string resourceGroupDefaultName = null
        ) {
            if (!string.IsNullOrEmpty(_appSettings?.ResourceGroup?.Name)) {
                var resourceGroupName = _appSettings.ResourceGroup.Name;

                if (!checkIfResourceGroupExists(resourceGroupName)) {
                    Log.Information($"Configured resource group name will be used: {resourceGroupName}");
                    return resourceGroupName;
                }
                else {
                    throw new ArgumentException($"Configured '{resourceGroupName}' resource group already exists.");
                }
            }

            return SelectNewResourceGroupNameImp(checkIfResourceGroupExists, resourceGroupDefaultName);
        }

        protected abstract bool CheckIfSaveEnvFileImp();

        public bool CheckIfSaveEnvFile() {
            if (null != _appSettings?.SaveEnvFile && _appSettings.SaveEnvFile.HasValue) {
                Log.Information($"Configured to save .env file: {_appSettings.SaveEnvFile.Value}");
                return _appSettings.SaveEnvFile.Value;
            }

            return CheckIfSaveEnvFileImp();
        }

        protected abstract bool CheckIfPerformCleanupImp();

        public bool CheckIfPerformCleanup() {
            if (null != _appSettings?.NoCleanup && _appSettings.NoCleanup.HasValue) {
                var performCleanup = !_appSettings.NoCleanup.Value;

                Log.Information($"Configured to perform cleanup: {performCleanup}");
                return performCleanup;
            }

            return CheckIfPerformCleanupImp();
        }

        protected string ToString(AzureEnvironment azureEnvironment) {
            return $"{azureEnvironment.Name}";
        }

        protected string ToString(ISubscription subscription) {
            return $"DisplayName: '{subscription.DisplayName}', SubscriptionId: '{subscription.SubscriptionId}'";
        }

        protected string ToString(IResourceGroup resourceGroup) {
            return $"Name: '{resourceGroup.Name}', Id: '{resourceGroup.Id}'";
        }

        protected string ToString(Region region) {
            return $"{region.Name}";
        }
    }
}
