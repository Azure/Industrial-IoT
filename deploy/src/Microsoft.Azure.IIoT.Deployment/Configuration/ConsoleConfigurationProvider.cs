// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Azure.IIoT.Deployment.Deployment;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;
    using Microsoft.Azure.IIoT.Deployment.Configuration.Extension;

    /// <summary>
    /// Configuration provider that prompts user to provide missing details.
    /// </summary>
    class ConsoleConfigurationProvider : ConfigurationProviderWithSettings {

        // ClientId (or AppId) of AzureIndustrialIoTDeployment Application
        public const string AzureIndustrialIoTDeploymentClientID = "fb2ca262-60d8-4167-ac33-1998d6d5c50b";

        private RunMode? _runMode;

        private static readonly Regex kApplicationNameRgx = new Regex(@"^[a-zA-Z0-9-]*$");

        public ConsoleConfigurationProvider(
            AppSettings appSettings = null
        ) : base(appSettings) {}

        /// <inheritdoc/>
        public override RunMode GetRunMode() {
            if (_runMode.HasValue) {
                return _runMode.Value;
            }

            if (null != _appSettings?.RunMode && _appSettings.RunMode.HasValue) {
                _runMode = _appSettings.RunMode.Value;
            } else {
                Log.Information($"Application RunMode is not configured, default will be used: {RunMode.Full}");
                _runMode = RunMode.Full;
            }

            return _runMode.Value;
        }

        /// <inheritdoc/>
        public override AuthenticationConfiguration GetAuthenticationConfiguration(
            IEnumerable<AzureEnvironment> azureEnvironments
        ) {
            var authSettings = _appSettings?.Auth;

            // Set azureEnvironment
            AzureEnvironment azureEnvironment;

            if (null != authSettings?.AzureEnvironment && authSettings.AzureEnvironment.HasValue) {
                azureEnvironment = authSettings.AzureEnvironment.Value.ToAzureEnvironment();

                if (azureEnvironments.Contains(azureEnvironment)) {
                    Log.Information($"Configured Azure environment will be used: {ToString(azureEnvironment)}");
                }
                else {
                    throw new Exception($"Configured '{ToString(azureEnvironment)}' Azure environment not found.");
                }
            } else {
                azureEnvironment = GetAzureEnvironmentFromConsole(azureEnvironments);
            }

            // Set tenantId
            Guid? tenantId;

            if (null != authSettings?.TenantId && authSettings.TenantId.HasValue) {
                Log.Information($"Configured TenantId will be used: {authSettings.TenantId.Value}");
                tenantId = authSettings.TenantId.Value;
            } else {
                tenantId = GetTenantIdFromConsole();
            }

            // Set clientId
            Guid? clientId;

            if (null != authSettings?.ClientId && authSettings.ClientId.HasValue) {
                Log.Information($"Configured ClientId will be used: {authSettings.ClientId.Value}");
                clientId = authSettings.ClientId.Value;
            }
            else {
                // We will use ClientId of AzureIndustrialIoTDeployment application by default.
                clientId = new Guid(AzureIndustrialIoTDeploymentClientID);
            }

            // Set clientSecret
            string clientSecret;

            if (!string.IsNullOrEmpty(authSettings?.ClientSecret)) {
                Log.Information($"Configured ClientSecret will be used.");
                clientSecret = authSettings.ClientSecret;
            } else {
                clientSecret = null;
            }

            var authConf = new AuthenticationConfiguration(
                azureEnvironment,
                tenantId.Value,
                clientId.Value,
                clientSecret
            );

            return authConf;
        }

        /// <summary>
        /// Prompt user to select Azure environment from a list of available ones.
        /// </summary>
        /// <param name="azureEnvironments"></param>
        /// <returns></returns>
        protected AzureEnvironment GetAzureEnvironmentFromConsole(
            IEnumerable<AzureEnvironment> azureEnvironments
        ) {
            Console.WriteLine("Please select Azure environment to use:");
            Console.WriteLine();

            Console.WriteLine("Available Azure environments:");

            var index = 0;
            foreach (var environment in azureEnvironments) {
                Console.WriteLine($"{index}: {ToString(environment)}");
                ++index;
            }

            var selection = ReadIndex(azureEnvironments.Count(), "Select Azure environment (using its index): ");

            return azureEnvironments.ElementAt(selection);
        }

        /// <summary>
        /// Prompt user to provide tenant id.
        /// </summary>
        /// <returns></returns>
        protected Guid GetTenantIdFromConsole() {
            Console.WriteLine("Please provide your TenantId:");

            while (true) {
                try {
                    var tenantStr = ReadNonEmptyString();
                    var tenantId = Guid.Parse(tenantStr);
                    return tenantId;
                }
                catch (FormatException) {
                    Console.WriteLine("Provided value is not a valid Guid. Please provide your TenantId again:");
                }
            }
        }

        /// <inheritdoc/>
        public override ISubscription GetSubscription(
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
                    throw new Exception($"Configured '{_appSettings.SubscriptionId.Value}' subscription not found.");
                }
            }

            return GetSubscriptionFromConsole(subscriptionsList);
        }

        /// <summary>
        /// Prompt user to select a subscription from a list of available ones.
        /// </summary>
        /// <param name="subscriptionsList"></param>
        /// <returns></returns>
        protected ISubscription GetSubscriptionFromConsole(
            IEnumerable<ISubscription> subscriptionsList
        ) {
            var subscriptionsCount = subscriptionsList.Count();

            ISubscription subscription;

            if (subscriptionsCount == 0) {
                throw new Exception("The account does not contain any subscription");
            }
            else if (subscriptionsCount == 1) {
                subscription = subscriptionsList.First();

                Console.WriteLine($"The following subscription will be used:\n{ToString(subscription)}");
            }
            else {
                Console.WriteLine("The following subscriptions are available:");

                var index = 0;
                foreach (var curSubscription in subscriptionsList) {
                    Console.WriteLine($"{index}: {ToString(curSubscription)}");

                    ++index;
                }

                var selection = ReadIndex(subscriptionsCount, "Select subscription to use (using its index): ");
                subscription = subscriptionsList.ElementAt(selection);
            }

            Console.WriteLine();

            return subscription;
        }

        /// <inheritdoc/>
        public override string GetApplicationName() {
            if (!string.IsNullOrEmpty(_appSettings?.ApplicationName)) {
                if (!kApplicationNameRgx.IsMatch(_appSettings.ApplicationName)) {
                    throw new Exception($"Configured '{_appSettings.ApplicationName}' ApplicationName is invalid. " +
                        $"Use only alphanumeric characters and '-'.");
                }

                Log.Information($"Configured application name will be used: {_appSettings.ApplicationName}");
                return _appSettings.ApplicationName;
            }

            return GetApplicationNameFromConsole();
        }

        /// <summary>
        /// Prompt user to provide a name for Azure IIoT deployment instance.
        /// </summary>
        /// <returns></returns>
        protected string GetApplicationNameFromConsole() {
            while(true) {
                Console.WriteLine("Please provide a name for the AAD application to register. " +
                    "Use only alphanumeric characters and '-':");

                var applicationName = ReadNonEmptyString();
                if (kApplicationNameRgx.IsMatch(applicationName)) {
                    return applicationName;
                }

                Console.WriteLine("Provided AAD application name is invalid.");
            }
        }

        /// <inheritdoc/>
        public override bool IfUseExistingResourceGroup() {
            if (null != _appSettings?.ResourceGroup?.UseExisting && _appSettings.ResourceGroup.UseExisting.HasValue) {
                Log.Information($"Configured to use existing resource group: {_appSettings.ResourceGroup.UseExisting.Value}");
                return _appSettings.ResourceGroup.UseExisting.Value;
            }

            return IfUseExistingResourceGroupFromConsole();
        }

        /// <summary>
        /// Prompt user to select whether an existing resource group should be used for deployment.
        /// </summary>
        /// <returns></returns>
        protected bool IfUseExistingResourceGroupFromConsole() {
            Console.WriteLine("Do you want to use existing Resource Group or create a new one ? ");
            return ChoisePrompt('E', "existing", 'N', "new");
        }

        /// <inheritdoc/>
        public override IResourceGroup GetExistingResourceGroup(
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
                    throw new Exception($"Configured '{_appSettings.ResourceGroup.Name}' resource group not found.");
                }
            }

            return GetExistingResourceGroupFromConsole(resourceGroups);
        }

        /// <summary>
        /// Prompt user to select a resource group that should be used for deployment from a list of existing ones.
        /// </summary>
        /// <param name="resourceGroups"> List of existing resource groups </param>
        /// <returns></returns>
        protected IResourceGroup GetExistingResourceGroupFromConsole(
            IEnumerable<IResourceGroup> resourceGroups
        ) {
            Console.WriteLine("Available resource groups:");

            var index = 0;
            foreach (var rg in resourceGroups) {
                Console.WriteLine($"{index}: {ToString(rg)}");
                ++index;
            }

            var selection = ReadIndex(resourceGroups.Count(), "Select resource group (using its index): ");
            return resourceGroups.ElementAt(selection);
        }

        /// <inheritdoc/>
        public override Tuple<Region, string> GetNewResourceGroup(
            IEnumerable<Region> regions,
            Func<string, bool> ifResourceGroupExists,
            string resourceGroupDefaultName = null
        ) {
            Region resourceGroupRegion;
            string resourceGroupName = null;

            var resourceGroupSettings = _appSettings?.ResourceGroup;

            // Get resourceGroupRegion
            if (null != resourceGroupSettings?.Region && resourceGroupSettings.Region.HasValue) {
                resourceGroupRegion = resourceGroupSettings.Region.Value.ToRegion();

                if (regions.Contains(resourceGroupRegion)) {
                    Log.Information($"Configured region will be used: {ToString(resourceGroupRegion)}");
                }
                else {
                    throw new Exception($"Configured '{ToString(resourceGroupRegion)}' region not found.");
                }
            }
            else {
                resourceGroupRegion = GetNewResourceGroupRegionFromConsole(regions);
            }

            // Get resourceGroupName
            if (!string.IsNullOrEmpty(resourceGroupSettings?.Name)) {
                resourceGroupName = resourceGroupSettings.Name;

                if (!ifResourceGroupExists(resourceGroupName)) {
                    Log.Information($"Configured resource group name will be used: {resourceGroupName}");
                }
                else {
                    throw new Exception($"Configured '{resourceGroupName}' resource group already exists.");
                }
            }
            else {
                resourceGroupName = GetNewResourceGroupNameFromConsole(
                    ifResourceGroupExists,
                    resourceGroupDefaultName
                );
            }

            return new Tuple<Region, string>(resourceGroupRegion, resourceGroupName);
        }

        /// <summary>
        /// Get new resource group region from console.
        /// </summary>
        /// <param name="regions"> List of valid regions </param>
        /// <returns></returns>
        protected Region GetNewResourceGroupRegionFromConsole(
            IEnumerable<Region> regions
        ) {
            Console.WriteLine("Please select region where resource group will be created.");
            Console.WriteLine();

            Console.WriteLine("Available regions:");

            var index = 0;
            foreach (var curRegion in regions) {
                Console.WriteLine($"{index}: {ToString(curRegion)}");
                ++index;
            }

            var regionSelection = ReadIndex(regions.Count(), "Select region (using its index): ");
            var region = regions.ElementAt(regionSelection);

            return region;
        }

        /// <summary>
        /// Get new resource group name from console.
        /// </summary>
        /// <param name="checkIfResourceGroupExists"> Callback to check if resource group already exists </param>
        /// <param name="resourceGroupDefaultName"> Default name for the resource group </param>
        /// <returns></returns>
        protected string GetNewResourceGroupNameFromConsole(
            Func<string, bool> checkIfResourceGroupExists,
            string resourceGroupDefaultName = null
        ) {
            if (string.IsNullOrEmpty(resourceGroupDefaultName)) {
                Console.WriteLine("Select resource group name:");
            }
            else {
                Console.WriteLine("Select resource group name, press Enter to use '{0}':", resourceGroupDefaultName);
            }

            string resourceGroupName;

            do {
                resourceGroupName = Console.ReadLine();

                if (string.IsNullOrEmpty(resourceGroupName) && !string.IsNullOrEmpty(resourceGroupDefaultName)) {
                    resourceGroupName = resourceGroupDefaultName;
                }

                if (checkIfResourceGroupExists(resourceGroupName)) {
                    Console.WriteLine(
                        $"Resource group with name '{resourceGroupName}' already exists. " +
                        $"Select something else."
                    );

                    resourceGroupName = null;
                }
            } while (null == resourceGroupName);

            return resourceGroupName;
        }

        /// <inheritdoc/>
        public override ApplicationRegistrationDefinition GetApplicationRegistrationDefinition() {
            var applicationRegistration = _appSettings?.ApplicationRegistration;

            if (null == applicationRegistration) {
                return null;
            }

            // Validate that all configuration properties are set.
            applicationRegistration.Validate("ApplicationRegistration");

            return applicationRegistration.ToApplicationRegistrationDefinition();
        }

        /// <inheritdoc/>
        public override string GetApplicationUrl() {
            if (!string.IsNullOrEmpty(_appSettings?.ApplicationUrl)) {
                return _appSettings.ApplicationUrl;
            }

            return null;
        }

        /// <inheritdoc/>
        public override HelmSettings GetHelmSettings() {
            return _appSettings?.Helm;
        }

        /// <inheritdoc/>
        public override bool IfSaveEnvFile() {
            if (null != _appSettings?.SaveEnvFile && _appSettings.SaveEnvFile.HasValue) {
                Log.Information($"Configured to save .env file: {_appSettings.SaveEnvFile.Value}");
                return _appSettings.SaveEnvFile.Value;
            }

            Console.Write("Do you want to save connection details of deployed resources to '.env' file ? ");
            return ChoisePrompt('Y', "yes", 'N', "no");
        }

        /// <inheritdoc/>
        public override bool IfPerformCleanup() {
            if (null != _appSettings?.NoCleanup && _appSettings.NoCleanup.HasValue) {
                var performCleanup = !_appSettings.NoCleanup.Value;
                Log.Information($"Configured to perform cleanup: {performCleanup}");
                return performCleanup;
            }

            Console.Write("Do you want to delete registered Applications and the Resource Group ? ");
            return ChoisePrompt('Y', "yes", 'N', "no");
        }

        /// <summary>
        /// Read non-empty string from console.
        /// </summary>
        /// <returns></returns>
        protected string ReadNonEmptyString() {
            string inputStr;

            do {
                inputStr = Console.ReadLine();
            } while (string.IsNullOrEmpty(inputStr));

            return inputStr;
        }

        /// <summary>
        /// Prompt user to select an index from [0, indexMaxValue) interval.
        /// </summary>
        /// <param name="indexMaxValue"></param>
        /// <param name="selectionQuestion"></param>
        /// <returns></returns>
        protected int ReadIndex(
            int indexMaxValue,
            string selectionQuestion
        ) {
            if (indexMaxValue == 0) {
                throw new ArgumentException(nameof(indexMaxValue));
            }
            if (string.IsNullOrEmpty(selectionQuestion)) {
                throw new ArgumentNullException(nameof(selectionQuestion));
            }

            int? selection = null;

            while (!selection.HasValue) {
                try {
                    Console.WriteLine(selectionQuestion);

                    var selectionTmp = Convert.ToInt32(Console.ReadLine());

                    if (selectionTmp < 0 || selectionTmp >= indexMaxValue) {
                        Console.WriteLine($"Invalid Value. Please select a number in range 0 to {indexMaxValue - 1}.");
                    }
                    else {
                        selection = selectionTmp;
                    }
                }
                catch (Exception ex) {
                    if (!(ex is FormatException || ex is OverflowException)) {
                        throw;
                    }
                }
            }

            return selection.Value;
        }

        /// <summary>
        /// Prompt user to select between two choices. Input not matching (case-insensitive)
        /// either of provided choices (short or long) will be discarded.
        /// </summary>
        /// <param name="firstCh">First choice character, e.g. 'Y'.</param>
        /// <param name="firstDesc">One word description of first choice, e.g. 'yes'.</param>
        /// <param name="secondCh">Second choice character, e.g. 'N'.</param>
        /// <param name="secondDesc">One word description of second choice, e.g. 'no'.</param>
        /// <returns>Returns true if first choise has been selected, and false for second one.</returns>
        protected bool ChoisePrompt(
            char firstCh, string firstDesc,
            char secondCh, string secondDesc
        ) {
            while (true) {
                Console.WriteLine($"Please select {firstCh}[{firstDesc}] or {secondCh}[{secondDesc}]");

                var inputLower = ReadNonEmptyString().ToLower();
                if (firstCh.ToString().ToLower() == inputLower || firstDesc.ToLower() == inputLower) {
                    return true;
                }
                if (secondCh.ToString().ToLower() == inputLower || secondDesc.ToLower() == inputLower) {
                    return false;
                }
            }
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
