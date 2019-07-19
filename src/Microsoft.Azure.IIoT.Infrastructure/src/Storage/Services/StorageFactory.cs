// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Storage.Services {
    using Serilog;
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Infrastructure.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.Storage.Fluent;
    using Microsoft.Azure.Management.Storage.Fluent.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Storage resource factory
    /// </summary>
    public class StorageFactory : BaseFactory, IStorageFactory {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="logger"></param>
        public StorageFactory(ICredentialProvider creds, ILogger logger) :
            base(creds, logger) {
        }

        /// <inheritdoc/>
        public async Task<IStorageResource> GetAsync(
            IResourceGroupResource resourceGroup, string name) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            var client = await CreateClientAsync(resourceGroup);

            var storage = await client.StorageAccounts.GetByResourceGroupAsync(
                resourceGroup.Name, name);
            if (storage == null) {
                return null;
            }
            var keys = await storage.GetKeysAsync();
            if (keys == null) {
                return null;
            }
            return new StorageResource(this, resourceGroup, storage, keys, _logger);
        }

        /// <inheritdoc/>
        public async Task<IStorageResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            var client = await CreateClientAsync(resourceGroup);
            name = await client.StorageAccounts.SelectResourceNameAsync(
                resourceGroup.Name, "stg", name);

            var region = await resourceGroup.Subscription.GetRegionAsync();
            _logger.Information("Trying to create storage {name}...", name);
            var storage = await client.StorageAccounts
                .Define(name)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroup.Name)
                    .WithAccessFromAllNetworks()
                    .WithGeneralPurposeAccountKindV2()
                    .CreateAsync();

            _logger.Information("Created storage {name}.", name);
            var keys = await storage.GetKeysAsync();
            return new StorageResource(this, resourceGroup, storage, keys, _logger);
        }

        /// <summary>
        /// Storage resource
        /// </summary>
        private class StorageResource : IStorageResource {

            /// <summary>
            /// Create resource
            /// </summary>
            /// <param name="manager"></param>
            /// <param name="resourceGroup"></param>
            /// <param name="keys"></param>
            /// <param name="logger"></param>
            /// <param name="storage"></param>
            public StorageResource(StorageFactory manager,
                IResourceGroupResource resourceGroup, IStorageAccount storage,
                IReadOnlyList<StorageAccountKey> keys, ILogger logger) {

                _resourceGroup = resourceGroup;
                _storage = storage;
                _manager = manager;
                _logger = logger;
                var key = keys.FirstOrDefault().Value;
                StorageConnectionString =
                    $"DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;" +
                    $"AccountName={storage.Name};AccountKey={key}";
            }

            /// <inheritdoc/>
            public string Name => _storage.Name;

            /// <inheritdoc/>
            public string StorageConnectionString { get; }

            /// <inheritdoc/>
            public async Task DeleteAsync() {
                _logger.Information("Deleting storage {storage}...", _storage.Id);
                await _manager.TryDeleteStorageAsync(_resourceGroup, _storage.Id);
                _logger.Information("Storage {storage} deleted.", _storage.Id);
            }

            private readonly IResourceGroupResource _resourceGroup;
            private readonly IStorageAccount _storage;
            private readonly ILogger _logger;
            private readonly StorageFactory _manager;
        }

        /// <summary>
        /// Delete all vm resources if possible
        /// </summary>
        /// <returns></returns>
        public async Task TryDeleteStorageAsync(
            IResourceGroupResource resourceGroup, string id) {
            var client = await CreateClientAsync(resourceGroup);
            await Try.Async(() => client.StorageAccounts.DeleteByIdAsync(id));
        }
    }
}
