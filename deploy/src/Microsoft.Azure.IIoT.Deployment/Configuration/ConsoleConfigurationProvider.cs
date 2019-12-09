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
    using Microsoft.Identity.Client;

    class ConsoleConfigurationProvider : IConfigurationProvider {

        public ConsoleConfigurationProvider() { }

        public AzureEnvironment SelectEnvironment(
            IEnumerable<AzureEnvironment> azureEnvironments
        ) {
            Console.WriteLine("Please select Azure environment to use:");
            Console.WriteLine();

            Console.WriteLine("Available Azure environments:");

            var index = 0;
            foreach (var environment in azureEnvironments) {
                Console.WriteLine("{0}: {1}", index, environment.Name);
                ++index;
            }

            var selection = ReadIndex(azureEnvironments.Count(), "Select Azure environment: ");

            return azureEnvironments.ElementAt(selection);
        }

        public Guid GetTenantId() {
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

        public ISubscription SelectSubscription(
            IEnumerable<ISubscription> subscriptionsList
        ) {
            var subscriptionsCount = subscriptionsList.Count();

            ISubscription subscription;

            if (subscriptionsCount == 0) {
                throw new SystemException("The account does not contain any subscription");
            }
            else if (subscriptionsCount == 1) {
                subscription = subscriptionsList.First();

                Console.WriteLine("The following subscription will be used:\nSubscriptionId: {0}, DisplayName: {1}",
                    subscription.SubscriptionId, subscription.DisplayName);
            }
            else {
                Console.WriteLine("The following subscriptions are available:");

                var index = 0;
                foreach (var curSubscription in subscriptionsList) {
                    Console.WriteLine("{0}: SubscriptionId: {1}, DisplayName: {2}",
                        index, curSubscription.SubscriptionId, curSubscription.DisplayName);

                    ++index;
                }

                var selection = ReadIndex(subscriptionsCount, "Please select which subscription to use: ");
                subscription = subscriptionsList.ElementAt(selection);
            }

            Console.WriteLine();

            return subscription;
        }

        public IAccount SelectAccount(
            IEnumerable<IAccount> accounts
        ) {
            IAccount account;

            var accountsCount = accounts.Count();

            if (accountsCount == 0) {
                throw new System.SystemException("The program was not able to find account for current user.");
            }
            else if (accountsCount == 1) {
                account = accounts.FirstOrDefault();

                Console.WriteLine("The following account will be used:\n{0}.", account.HomeAccountId.ObjectId);
            }
            else {
                Console.WriteLine("The following accounts are available:");

                var index = 0;
                foreach (var curAccount in accounts) {
                    Console.WriteLine("{0}: {1} {2}",
                        index, curAccount.HomeAccountId.ObjectId, curAccount.Username);
                    ++index;
                }

                var selection = ReadIndex(accountsCount, "Please choose which account to use: ");
                account = accounts.ElementAt(selection);
            }

            Console.WriteLine();

            return account;
        }

        public string GetApplicationName() {
            Console.WriteLine("Please provide a name for the AAD application to register:");
            var applicationName = ReadNonEmptyString();
            return applicationName;
        }

        public bool CheckIfUseExistingResourceGroup() {
            Console.WriteLine("Do you want to create a new ResourceGroup or use an existing one ? " +
                "Please select N[new] or E[existing]");

            var response = ConsoleKey.Escape;

            while (!ConsoleKey.N.Equals(response) && !ConsoleKey.E.Equals(response)) {
                response = Console.ReadKey(false).Key;

                if (response != ConsoleKey.Enter) {
                    Console.WriteLine();
                }
            }

            return ConsoleKey.E.Equals(response);
        }

        public IResourceGroup SelectExistingResourceGroup(
            IEnumerable<IResourceGroup> resourceGroups
        ) {
            Console.WriteLine("Available resource groups:");

            var index = 0;
            foreach (var resourceGroup in resourceGroups) {
                Console.WriteLine("{0}: {1} {2}", index, resourceGroup.Id, resourceGroup.Name);
                ++index;
            }

            var selection = ReadIndex(resourceGroups.Count(), "Select an option: ");
            return resourceGroups.ElementAt(selection);
        }

        public Region SelectResourceGroupRegion(
            IEnumerable<Region> regions
        ) {
            Console.WriteLine("Please select region where resource group will be created.");
            Console.WriteLine();

            Console.WriteLine("Available regions:");

            var index = 0;
            foreach (var _region in regions) {
                Console.WriteLine("{0}: {1}", index, _region.Name);
                ++index;
            }

            var regionSelection = ReadIndex(regions.Count(), "Select a region: ");
            var region = regions.ElementAt(regionSelection);

            return region;
        }

        public string SelectResourceGroupName(
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

        private string ReadNonEmptyString() {
            string inputStr;

            do {
                inputStr = Console.ReadLine();
            } while (string.IsNullOrEmpty(inputStr));

            return inputStr;
        }

        private int ReadIndex(
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

        public bool CheckIfSaveEnvFile() {
            Console.WriteLine("Do you want to save connection details of deployed resources to '.env' file ? " +
                "Please select Y[yes] or N[no]");

            var response = ConsoleKey.Escape;

            while (!ConsoleKey.Y.Equals(response) && !ConsoleKey.N.Equals(response)) {
                response = Console.ReadKey(false).Key;

                if (response != ConsoleKey.Enter) {
                    Console.WriteLine();
                }
            }

            return ConsoleKey.Y.Equals(response);
        }

        public bool CheckIfPerformCleanup() {
            Console.WriteLine("Do you want to delete registered Applications and the Resource Group ? " +
                "Please select Y[yes] or N[no]");

            var response = ConsoleKey.Escape;

            while (!ConsoleKey.Y.Equals(response) && !ConsoleKey.N.Equals(response)) {
                response = Console.ReadKey(false).Key;

                if (response != ConsoleKey.Enter) {
                    Console.WriteLine();
                }
            }

            return ConsoleKey.Y.Equals(response);
        }
    }
}
