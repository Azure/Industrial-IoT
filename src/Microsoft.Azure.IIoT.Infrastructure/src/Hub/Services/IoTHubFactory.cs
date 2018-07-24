// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Hub.Services {
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Management.IotHub;
    using Microsoft.Azure.Management.IotHub.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class IoTHubFactory : IIoTHubFactory {

        /// <summary>
        /// Create iot hub manager
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="logger"></param>
        public IoTHubFactory(ICredentialProvider creds, ILogger logger) {
            _creds = creds ??
                throw new ArgumentNullException(nameof(creds));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IIoTHubResource> GetAsync(
            IResourceGroupResource resourceGroup, string hubName) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (string.IsNullOrEmpty(hubName)) {
                throw new ArgumentNullException(nameof(hubName));
            }
            var client = await CreateClientAsync(resourceGroup);
            var hub = await client.IotHubResource.GetAsync(
                resourceGroup.Name, hubName);
            var keys = await client.IotHubResource.GetKeysForKeyNameAsync(
                resourceGroup.Name, hubName, kIoTHubOwner);
            return new IoTHubResource(this, resourceGroup, hubName,
                hub.Properties, _logger, keys);
        }

        /// <inheritdoc/>
        public async Task<IIoTHubResource> CreateAsync(
            IResourceGroupResource resourceGroup, string hubName) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }

            var client = await CreateClientAsync(resourceGroup);

            // Check quota
            var quota = await client.ResourceProviderCommon
                .GetSubscriptionQuotaAsync();
            var limit = quota.Value
                .FirstOrDefault(x => x.Name.Value.Equals("paidIotHubCount"));
            if (limit?.Limit == limit?.CurrentValue) {
                throw new ExternalDependencyException(
                    $"Subscription limit reached at {(limit?.Limit ?? -1)}");
            }

            // Check name -  null means we need to create one
            if (string.IsNullOrEmpty(hubName)) {
                while (true) {
                    hubName = StringEx.CreateUnique(10, "iothub");
                    var result = client.IotHubResource.CheckNameAvailability(
                        new OperationInputs { Name = hubName });
                    if (result.NameAvailable ?? false) {
                        break;
                    }
                }
            }
            else {
                var result = client.IotHubResource.CheckNameAvailability(
                    new OperationInputs { Name = hubName });
                if (!(result.NameAvailable ?? false)) {
                    throw new ArgumentException("hub exists with this name",
                        nameof(hubName));
                }
            }

            // Create hub
            var hub = await client.IotHubResource.CreateOrUpdateAsync(
                resourceGroup.Name, hubName, new IotHubDescription {
                    Location = await resourceGroup.Subscription.GetRegion(),
                    Sku = new IotHubSkuInfo {
                        Name = IotHubSku.S1,
                        Capacity = 1
                    },
                    Properties = new IotHubProperties {
                        AuthorizationPolicies =
                            new List<SharedAccessSignatureAuthorizationRule> {
                                new SharedAccessSignatureAuthorizationRule (
                                    kIoTHubOwner,
            AccessRights.RegistryReadRegistryWriteServiceConnectDeviceConnect)
                    }
                    }
                });

            _logger.Info($"Created iot hub {hubName} in resource " +
                $"group {resourceGroup.Name}...", () => { });

            var keys = await client.IotHubResource.GetKeysForKeyNameAsync(
                resourceGroup.Name, hubName, kIoTHubOwner);
            return new IoTHubResource(this, resourceGroup, hubName,
                hub.Properties, _logger, keys);
        }

        /// <summary>
        /// IoT hub resource
        /// </summary>
        private class IoTHubResource : IIoTHubResource {

            /// <summary>
            /// Create iot hub
            /// </summary>
            /// <param name="manager"></param>
            /// <param name="resourceGroup"></param>
            /// <param name="name"></param>
            /// <param name="properties"></param>
            /// <param name="rule"></param>
            public IoTHubResource(IoTHubFactory manager,
                IResourceGroupResource resourceGroup, string name,
                IotHubProperties properties, ILogger logger,
                SharedAccessSignatureAuthorizationRule rule) {

                _resourceGroup = resourceGroup;
                Name = name;

                _manager = manager;
                _properties = properties;
                _logger = logger;

                PrimaryConnectionString = ConnectionString.CreateServiceConnectionString(
                    properties.HostName, rule.KeyName, rule.PrimaryKey).ToString();
                SecondaryConnectionString = ConnectionString.CreateServiceConnectionString(
                    properties.HostName, rule.KeyName, rule.SecondaryKey).ToString();
            }

            /// <inheritdoc/>
            public string Name { get; }

            /// <inheritdoc/>
            public string PrimaryConnectionString { get; }

            /// <inheritdoc/>
            public string SecondaryConnectionString { get; }

            /// <inheritdoc/>
            public async Task<bool> IsHealthyAsync() {
                try {
                    // Check health
                    var client = await _manager.CreateClientAsync(_resourceGroup);
                    var eps = await client.IotHubResource.GetEndpointHealthAsync(
                        _resourceGroup.Name, Name);
                    return eps.All(q => q.HealthStatus.Equals("healthy",
                        StringComparison.OrdinalIgnoreCase));
                }
                catch (Exception ex) {
                    _manager._logger.Error("Exception during health check",
                        () => ex);
                    return false;
                }
            }

            /// <inheritdoc/>
            public async Task DeleteAsync() {
                try {
                    _logger.Info($"Deleting iot hub {Name}...", () => { });
                    var client = await _manager.CreateClientAsync(_resourceGroup);
                    await client.IotHubResource.DeleteAsync(_resourceGroup.Name,
                        Name);
                    _logger.Info($"iot hub {Name} deleted.", () => { });
                }
                catch (Exception ex) {
                    _manager._logger.Error("Exception during delete of iot hub",
                        () => ex);
                }
            }

            private readonly IResourceGroupResource _resourceGroup;
            private readonly ILogger _logger;
            private readonly IoTHubFactory _manager;
            private readonly IotHubProperties _properties;
        }

        /// <summary>
        /// Helper to create new client
        /// </summary>
        /// <returns></returns>
        private async Task<IotHubClient> CreateClientAsync(
            IResourceGroupResource resourceGroup) {
            var environment = await resourceGroup.Subscription.GetAzureEnvironmentAsync();
            var credentials = await _creds.GetTokenCredentialsAsync(
                environment.ManagementEndpoint);
            var client = new IotHubClient(credentials) {
                SubscriptionId = await resourceGroup.Subscription.GetSubscriptionId()
            };
            return client;
        }

        private const string kIoTHubOwner = "iothubowner";
        private readonly ICredentialProvider _creds;
        private readonly ILogger _logger;
    }
}
