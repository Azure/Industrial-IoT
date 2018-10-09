// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Services {
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Infrastructure.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Create and read resource group records from management endpoint
    /// </summary>
    public class ResourceGroupFactory : IResourceGroupFactory {

        /// <summary>
        /// Create azure manager
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public ResourceGroupFactory(ICredentialProvider creds,
            ISubscriptionInfoProvider config, ILogger logger) {
            _creds = creds ??
                throw new ArgumentNullException(nameof(creds));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _subscription = config?.GetSubscriptionInfo() ??
                throw new ArgumentNullException(nameof(config));
            if (_subscription == null) {
                throw new ArgumentNullException(nameof(config));
            }
        }

        /// <inheritdoc/>
        public async Task<IResourceGroupResource> GetAsync(string resourceGroup,
            bool deleteOnDispose, ISubscriptionInfo subscription) {
            if (string.IsNullOrEmpty(resourceGroup)) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            subscription = CompositeInfo.Create(subscription, _subscription);
            var client = await CreateClientAsync(subscription);
            var rg = await client.ResourceGroups.GetByNameAsync(resourceGroup);
            if (rg == null) {
                return null;
            }
            return new ResourceGroupResource(this, rg, deleteOnDispose,
                subscription, _logger);
        }

        /// <inheritdoc/>
        public async Task<IResourceGroupResource> CreateAsync(string resourceGroup,
            bool deleteOnDispose, ISubscriptionInfo subscription) {

            subscription = CompositeInfo.Create(subscription, _subscription);

            // Create resource group
            var client = await CreateClientAsync(subscription);
            if (string.IsNullOrEmpty(resourceGroup)) {
                // Create group name
                while (true) {
                    resourceGroup = StringEx.CreateUnique(8, "rg");
                    var exists = await client.ResourceGroups.ContainAsync(
                        resourceGroup);
                    if (!exists) {
                        break;
                    }
                }
            }
            else {
                var exists = await client.ResourceGroups.ContainAsync(
                    resourceGroup);
                if (exists) {
                    throw new ExternalDependencyException("resource group already exists");
                }
            }

            var region = await subscription.GetRegionAsync();
            _logger.Info($"Creating simulation group {resourceGroup} in {region}...");
            var rg = await client.ResourceGroups.Define(resourceGroup)
                .WithRegion(region)
                .CreateAsync();
            _logger.Info($"Created resource group {rg.Name}.");
            return new ResourceGroupResource(this, rg, deleteOnDispose, subscription, _logger);
        }

        /// <summary>
        /// Resource group
        /// </summary>
        private class ResourceGroupResource : IResourceGroupResource {

            /// <summary>
            /// Create resource group resource
            /// </summary>
            /// <param name="manager"></param>
            /// <param name="group"></param>
            /// <param name="deleteOnDispose"></param>
            /// <param name="subscription"></param>
            /// <param name="logger"></param>
            public ResourceGroupResource(ResourceGroupFactory manager,
                IResourceGroup group, bool deleteOnDispose,
                ISubscriptionInfo subscription, ILogger logger) {
                _manager = manager;
                _group = group;
                _logger = logger;
                _deleteOnDispose = deleteOnDispose;
                Subscription = subscription;
            }

            /// <inheritdoc/>
            public string Name => _group.Name;

            /// <inheritdoc/>
            public ISubscriptionInfo Subscription { get; }

            /// <inheritdoc/>
            public async Task DeleteAsync() {
                _logger.Info($"Deleting resource group {_group.Name}...");
                var client = await _manager.CreateClientAsync(Subscription);
                await client.ResourceGroups.DeleteByNameAsync(_group.Name);
                _logger.Info($"Resource group {_group.Name} deleted.");
            }

            /// <inheritdoc/>
            public void Dispose() {
                if (_deleteOnDispose) {
                    try {
                        DeleteAsync().Wait();
                    }
                    catch {
                        return;
                    }
                }
            }

            private readonly ResourceGroupFactory _manager;
            private readonly IResourceGroup _group;
            private readonly ILogger _logger;
            private readonly bool _deleteOnDispose;
        }

        /// <summary>
        /// Helper to create new client
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        private async Task<IAzure> CreateClientAsync(ISubscriptionInfo subscription) {
            // Create azure fluent interface
            var environment = await subscription.GetAzureEnvironmentAsync();
            var subscriptionId = await subscription.GetSubscriptionId();
            var credentials = await _creds.GetAzureCredentialsAsync(environment);
            return Azure
                .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);
        }

        private readonly ISubscriptionInfo _subscription;
        private readonly ICredentialProvider _creds;
        private readonly ILogger _logger;
    }
}
