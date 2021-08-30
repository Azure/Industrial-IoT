// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
    using Serilog;

    class AzureResourceManager {

        public static readonly Region[] FunctionalRegions = new Region[] {
            Region.USEast,
            Region.USEast2,
            Region.USWest,
            Region.USWest2,
            Region.USCentral,
            Region.EuropeNorth,
            Region.EuropeWest,
            //Region.CanadaCentral, // As of 2019.11.25, SignalR resource is not available in this region.
            //Region.IndiaCentral, // App Service Plan scaling is not available in this region.
            Region.AsiaSouthEast,
            Region.AustraliaEast,
            // ToDo: Uncomment Region.UKSouth once we are able to test the deployment.
            //Region.UKSouth // 2020.03.26: No capacity to test.
        };

        private readonly Azure.IAuthenticated _authenticated;
        private IAzure _azure;

        public AzureResourceManager(
            AzureCredentials azureCredentials
        ) {
            _authenticated = Azure
                .Configure()
                .Authenticate(azureCredentials);
        }

        public void Init(ISubscription subscription) {
            _azure = _authenticated
                .WithSubscription(subscription.SubscriptionId);
        }

        public IEnumerable<ITenant> GetTenants() {
            var tenantsList = _authenticated
                .Tenants
                .List();

            return tenantsList;
        }

        public IEnumerable<ISubscription> GetSubscriptions() {
            var subscriptionsList = _authenticated
                .Subscriptions
                .List();

            return subscriptionsList;
        }

        /// <summary>
        /// Get all resource groups in the subscription.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<IResourceGroup>> GetResourceGroupsAsync(
            CancellationToken cancellationToken = default
        ) {
            var resourceGroups = await _azure
                .ResourceGroups
                .ListAsync(cancellationToken: cancellationToken);

            return resourceGroups;
        }

        /// <summary>
        /// Get resource group by its name.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IResourceGroup> GetResourceGroupAsync(
            string resourceGroupName,
            CancellationToken cancellationToken = default
        ) {
            var resourceGroup = await _azure
                .ResourceGroups
                .GetByNameAsync(
                    resourceGroupName,
                    cancellationToken
                );

            return resourceGroup;
        }

        /// <summary>
        /// Check if a resource group with a give name exists in the subscription.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> CheckIfResourceGroupExistsAsync(
            string resourceGroupName,
            CancellationToken cancellationToken = default
        ) {
            var resourceGroupAlreadyExists = await _azure
                .ResourceGroups
                .ContainAsync(
                    resourceGroupName,
                    cancellationToken
                );

            return resourceGroupAlreadyExists;
        }

        /// <summary>
        /// Create a resource group in the given region.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IResourceGroup> CreateResourceGroupAsync(
            Region region,
            string resourceGroupName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Resource Group: {resourceGroupName} ...");

                var resourceGroup = await _azure
                    .ResourceGroups
                    .Define(resourceGroupName)
                    .WithRegion(region)
                    .WithTags(tags)
                    .CreateAsync(cancellationToken);

                Log.Information($"Created Resource Group: {resourceGroupName}");

                return resourceGroup;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Resource Group: {resourceGroupName}");
                throw;
            }
        }

        /// <summary>
        /// Initiate deletion of the resource group.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BeginDeleteResourceGroupAsync(
            IResourceGroup resourceGroup,
            CancellationToken cancellationToken = default
        ) {
            try {
                await _azure
                    .ResourceGroups
                    .BeginDeleteByNameAsync(
                        resourceGroup.Name,
                        cancellationToken
                    );

                Log.Information($"Initiated deletion of Resource Group: {resourceGroup.Name}");
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to initiate deletion of Resource Group: {resourceGroup.Name}");
                throw;
            }
        }

        public static async Task<IEnumerable<SubscriptionInner>> GetSubscriptionsUsingRestClientAsync(
            AzureEnvironment azureEnvironment,
            AzureCredentials azureCredentials,
            CancellationToken cancellationToken = default
        ) {
            var restClient = RestClient
                .Configure()
                .WithEnvironment(azureEnvironment)
                .WithCredentials(azureCredentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            var subscriptions = new List<SubscriptionInner>();

            using (var subscriptionClient = new SubscriptionClient(restClient)) {
                var subscriptionsList = await subscriptionClient
                    .Subscriptions
                    .ListAsync(cancellationToken);

                foreach (var subscription in subscriptionsList) {
                    subscriptions.Add(subscription);
                }
            };

            return subscriptions;
        }

        /// <summary>
        /// Get list of Kubernetes versions available for the given Azure region.
        /// </summary>
        public async Task<List<string>> ListKubernetesVersionsAsync(
            Region region,
            CancellationToken cancellationToken = default
        ) {
            // This is the resource type that should be used to get versions of AKS clusters.
            const string aksResourceType = "managedClusters";

            var result = await _azure
                .KubernetesClusters
                .Manager
                .Inner
                .ContainerServices
                .ListOrchestratorsWithHttpMessagesAsync(
                    region.ToString(),
                    aksResourceType,
                    cancellationToken: cancellationToken
                );

            var kubernetesVersions = result.Body.Orchestrators
                .Select(orch => orch.OrchestratorVersion).ToList();

            return kubernetesVersions;
        }
    }
}
