// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.ServiceBus.Services {
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Infrastructure.Services;
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ServiceBus.Fluent;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Service bus resource factory
    /// </summary>
    public class ServiceBusFactory : BaseFactory, IServiceBusFactory {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="logger"></param>
        public ServiceBusFactory(ICredentialProvider creds, ILogger logger) :
            base (creds, logger) {
        }

        /// <inheritdoc/>
        public async Task<IServiceBusResource> GetAsync(
            IResourceGroupResource resourceGroup, string name) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }

            var client = await CreateClientAsync(resourceGroup);
            var nameSpace = await client.ServiceBusNamespaces.GetByResourceGroupAsync(
                resourceGroup.Name, name);
            if (nameSpace == null) {
                return null;
            }

            var keys = await ReadKeysFromNamespace(nameSpace);
            return new ServiceBusResource(this, resourceGroup, nameSpace, keys, _logger);
        }

        /// <inheritdoc/>
        public async Task<IServiceBusResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }

            var client = await CreateClientAsync(resourceGroup);
            name = await client.ServiceBusNamespaces.SelectResourceNameAsync(
                resourceGroup.Name, "sb", name);

            _logger.Information("Trying to create namespace {name}...", name);
            var region = await resourceGroup.Subscription.GetRegionAsync();
            var nameSpace = await client.ServiceBusNamespaces
                .Define(name)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroup.Name)
                    .WithNewManageRule("manage")
                    .WithNewListenRule("listen")
                    .WithNewSendRule("send")
                    .CreateAsync();

            _logger.Information("Created namespace {name}.", name);

            var keys = await ReadKeysFromNamespace(nameSpace);
            return new ServiceBusResource(this, resourceGroup, nameSpace, keys, _logger);
        }

        /// <summary>
        /// Helper to read all pre-defined keys
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <returns></returns>
        private static async Task<Dictionary<string, IAuthorizationKeys>> ReadKeysFromNamespace(
            IServiceBusNamespace nameSpace) {
            var ruleTasks = new[] {
                nameSpace.AuthorizationRules.GetByNameAsync("manage"),
                nameSpace.AuthorizationRules.GetByNameAsync("listen"),
                nameSpace.AuthorizationRules.GetByNameAsync("send")
            };
            await Task.WhenAll(ruleTasks);
            var keyTasks = ruleTasks.ToDictionary(r => r.Result?.Name,
                r => r.Result?.GetKeysAsync() ?? Task.FromResult<IAuthorizationKeys>(null));
            await Task.WhenAll(keyTasks.Values);
            var result = keyTasks.ToDictionary(k => k.Key, k => k.Value.Result);
            return result;
        }

        /// <summary>
        /// Service bus resource
        /// </summary>
        private class ServiceBusResource : IServiceBusResource {

            /// <summary>
            /// Create resource
            /// </summary>
            /// <param name="manager"></param>
            /// <param name="resourceGroup"></param>
            /// <param name="nameSpace"></param>
            /// <param name="result"></param>
            /// <param name="logger"></param>
            public ServiceBusResource(ServiceBusFactory manager,
                IResourceGroupResource resourceGroup, IServiceBusNamespace nameSpace,
                Dictionary<string, IAuthorizationKeys> result, ILogger logger) {

                _resourceGroup = resourceGroup;
                _nameSpace = nameSpace;
                _manager = manager;
                _logger = logger;

                if (result.TryGetValue("manage", out var key)) {
                    PrimaryManageConnectionString = key.PrimaryConnectionString;
                    SecondaryManageConnectionString = key.SecondaryConnectionString;
                }
                if (result.TryGetValue("listen", out key)) {
                    PrimaryListenConnectionString = key.PrimaryConnectionString;
                    SecondaryListenConnectionString = key.SecondaryConnectionString;
                }
                if (result.TryGetValue("send", out key)) {
                    PrimarySendConnectionString = key.PrimaryConnectionString;
                    SecondarySendConnectionString = key.SecondaryConnectionString;
                }
            }

            /// <inheritdoc/>
            public string Name => _nameSpace.Name;

            /// <inheritdoc/>
            public string PrimaryManageConnectionString { get; }

            /// <inheritdoc/>
            public string PrimarySendConnectionString { get; }

            /// <inheritdoc/>
            public string PrimaryListenConnectionString { get; }

            /// <inheritdoc/>
            public string SecondaryManageConnectionString { get; }

            /// <inheritdoc/>
            public string SecondarySendConnectionString { get; }

            /// <inheritdoc/>
            public string SecondaryListenConnectionString { get; }

            /// <inheritdoc/>
            public async Task CreateTopicAsync(string name, int? maxSizeInMb) {
                await _nameSpace.Update().WithNewTopic(name, maxSizeInMb ?? 32).ApplyAsync();
            }

            /// <inheritdoc/>
            public async Task DeleteTopicAsync(string name) {
                await _nameSpace.Update().WithoutTopic(name).ApplyAsync();
            }

            /// <inheritdoc/>
            public async Task CreateQueueAsync(string name, int? maxSizeInMb) {
                await _nameSpace.Update().WithNewQueue(name, maxSizeInMb ?? 32).ApplyAsync();
            }

            /// <inheritdoc/>
            public async Task DeleteQueueAsync(string name) {
                await _nameSpace.Update().WithoutQueue(name).ApplyAsync();
            }

            /// <inheritdoc/>
            public async Task DeleteAsync() {
                _logger.Information("Deleting namespace {nameSpace}...", _nameSpace.Id);
                await _manager.TryDeleteServiceBusAsync(_resourceGroup, _nameSpace.Id);
                _logger.Information("Namespace {nameSpace} deleted.", _nameSpace.Id);
            }


            private readonly IResourceGroupResource _resourceGroup;
            private readonly IServiceBusNamespace _nameSpace;
            private readonly ILogger _logger;
            private readonly ServiceBusFactory _manager;
        }

        /// <summary>
        /// Delete all vm resources if possible
        /// </summary>
        /// <returns></returns>
        public async Task TryDeleteServiceBusAsync(
            IResourceGroupResource resourceGroup, string id) {
            var client = await CreateClientAsync(resourceGroup);
            await Try.Async(() => client.ServiceBusNamespaces.DeleteByIdAsync(id));
        }
    }
}
