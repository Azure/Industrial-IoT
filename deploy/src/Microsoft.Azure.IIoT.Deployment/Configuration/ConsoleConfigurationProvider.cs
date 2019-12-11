// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    class ConsoleConfigurationProvider : ConfigurationProviderWithSettings {

        public ConsoleConfigurationProvider(
            AppSettings appSettings = null
        ) : base(appSettings) {}

        protected override AzureEnvironment SelectEnvironmentImp(
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

            var selection = ReadIndex(azureEnvironments.Count(), "Select Azure environment: ");

            return azureEnvironments.ElementAt(selection);
        }

        protected override Guid GetTenantIdImp() {
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

        protected override ISubscription SelectSubscriptionImp(
            IEnumerable<ISubscription> subscriptionsList
        ) {
            var subscriptionsCount = subscriptionsList.Count();

            ISubscription subscription;

            if (subscriptionsCount == 0) {
                throw new SystemException("The account does not contain any subscription");
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

                var selection = ReadIndex(subscriptionsCount, "Please select which subscription to use: ");
                subscription = subscriptionsList.ElementAt(selection);
            }

            Console.WriteLine();

            return subscription;
        }

        protected override string GetApplicationNameImp() {
            Console.WriteLine("Please provide a name for the AAD application to register:");
            var applicationName = ReadNonEmptyString();
            return applicationName;
        }

        protected override bool CheckIfUseExistingResourceGroupImp() {
            Console.WriteLine("Do you want to use existing Resource Group or create a new one ? ");
            return ChoisePrompt('E', "existing", 'N', "new");
        }

        protected override IResourceGroup SelectExistingResourceGroupImp(
            IEnumerable<IResourceGroup> resourceGroups
        ) {
            Console.WriteLine("Available resource groups:");

            var index = 0;
            foreach (var rg in resourceGroups) {
                Console.WriteLine($"{index}: {ToString(rg)}");
                ++index;
            }

            var selection = ReadIndex(resourceGroups.Count(), "Select an option: ");
            return resourceGroups.ElementAt(selection);
        }

        protected override Region SelectResourceGroupRegionImp(
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

            var regionSelection = ReadIndex(regions.Count(), "Select a region: ");
            var region = regions.ElementAt(regionSelection);

            return region;
        }

        protected override string SelectNewResourceGroupNameImp(
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

        protected override bool CheckIfSaveEnvFileImp() {
            Console.Write("Do you want to save connection details of deployed resources to '.env' file ? ");
            return ChoisePrompt('Y', "yes", 'N', "no");
        }

        protected override bool CheckIfPerformCleanupImp() {
            Console.Write("Do you want to delete registered Applications and the Resource Group ? ");
            return ChoisePrompt('Y', "yes", 'N', "no");
        }

        protected string ReadNonEmptyString() {
            string inputStr;

            do {
                inputStr = Console.ReadLine();
            } while (string.IsNullOrEmpty(inputStr));

            return inputStr;
        }

        protected int ReadIndex(
            int indexMaxValue,
            string selectionPrefix
        ) {
            int? selection = null;

            while (!selection.HasValue) {
                try {
                    if (!string.IsNullOrEmpty(selectionPrefix)) {
                        Console.WriteLine(selectionPrefix);
                    }

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
    }
}
