// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.ServiceBus.Fluent;
    using Microsoft.Azure.Management.ServiceBus.Fluent.Models;
    using Serilog;

    class ServiceBusMgmtClient : IDisposable {

        public const string DEFAULT_SERVICE_BUS_NAMESPACE_NAME_PREFIX = "sb-";
        public const int NUM_OF_MAX_NAME_AVAILABILITY_CHECKS = 5;

        public const string SERVICE_BUS_AUTHORIZATION_RULE = "RootManageSharedAccessKey";

        private readonly ServiceBusManagementClient _serviceBusManagementClient;

        public ServiceBusMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _serviceBusManagementClient = new ServiceBusManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateNamespaceName(
            string prefix = DEFAULT_SERVICE_BUS_NAMESPACE_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        /// <summary>
        /// Checks whether given ServiceBus Namespace name is available.
        /// </summary>
        /// <param name="serviceBusNamespaceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if name is available, False otherwise.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<bool> CheckNamespaceNameAvailabilityAsync(
            string serviceBusNamespaceName,
            CancellationToken cancellationToken = default
        ) {
            try {
                var nameAvailabilityInfo = await _serviceBusManagementClient
                    .Namespaces
                    .CheckNameAvailabilityMethodAsync(
                        serviceBusNamespaceName,
                        cancellationToken
                    );

                if (nameAvailabilityInfo.NameAvailable.HasValue) {
                    return nameAvailabilityInfo.NameAvailable.Value;
                }
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                // Will be thrown if there is no registered resource provider
                // found for specified location and/or api version to perform
                // name availability check.
                throw;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to check ServiceBus Namespace name availability for {serviceBusNamespaceName}");
                throw;
            }

            // !nameAvailabilityInfo.NameAvailable.HasValue
            throw new Exception($"Failed to check ServiceBus Namespace name availability for {serviceBusNamespaceName}");
        }

        /// <summary>
        /// Tries to generate ServiceBus Namespace name that is available.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>An available name for ServiceBus Namespace.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<string> GenerateAvailableNamespaceNameAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                for (var numOfChecks = 0; numOfChecks < NUM_OF_MAX_NAME_AVAILABILITY_CHECKS; ++numOfChecks) {
                    var serviceBusNamespaceName = GenerateNamespaceName();
                    var nameAvailable = await CheckNamespaceNameAvailabilityAsync(
                            serviceBusNamespaceName,
                            cancellationToken
                        );

                    if (nameAvailable) {
                        return serviceBusNamespaceName;
                    }
                }
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                // Will be thrown if there is no registered resource provider
                // found for specified location and/or api version to perform
                // name availability check.
                throw;
            }
            catch (Exception ex) {
                Log.Error(ex, "Failed to generate unique ServiceBus Namespace name");
                throw;
            }

            var errorMessage = $"Failed to generate unique ServiceBus Namespace name " +
                $"after {NUM_OF_MAX_NAME_AVAILABILITY_CHECKS} retries";

            Log.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        public async Task<NamespaceModelInner> CreateServiceBusNamespaceAsync(
            IResourceGroup resourceGroup,
            string serviceBusNamespaceName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure Service Bus Namespace: {serviceBusNamespaceName} ...");

                var namespaceModel = new NamespaceModelInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Sku = new Sku {
                        Name = "Standard",
                        Tier = "Standard"
                    }
                };

                namespaceModel.Validate();

                var serviceBusNamespace = await _serviceBusManagementClient
                    .Namespaces
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        serviceBusNamespaceName,
                        namespaceModel,
                        cancellationToken
                    );

                //serviceBusAuthorizationRule = await _serviceBusManagementClient
                //    .Namespaces
                //    .GetAuthorizationRuleAsync(
                //        resourceGroup.Name,
                //        serviceBusNamespaceName,
                //        SERVICE_BUS_AUTHORIZATION_RULE,
                //        cancellationToken
                //    );

                Log.Information($"Created Azure Service Bus Namespace: {serviceBusNamespaceName}");

                return serviceBusNamespace;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure Service Bus Namespace: {serviceBusNamespaceName}");
                throw;
            }
        }

        public async Task<string> GetServiceBusNamespaceConnectionStringAsync(
            IResourceGroup resourceGroup,
            NamespaceModelInner serviceBusNamespace,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Verbose($"Fetching connection string for Azure Service Bus Namespace: {serviceBusNamespace.Name} ...");

                var keysList = await _serviceBusManagementClient
                    .Namespaces
                    .ListKeysAsync(
                        resourceGroup.Name,
                        serviceBusNamespace.Name,
                        SERVICE_BUS_AUTHORIZATION_RULE,
                        cancellationToken
                    );

                Log.Verbose($"Fetched connection string for Azure Service Bus Namespace: {serviceBusNamespace.Name}");

                return keysList.PrimaryConnectionString;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to fetch connection string for Azure Service Bus Namespace: {serviceBusNamespace.Name}");
                throw;
            }
        }

        public void Dispose() {
            if (null != _serviceBusManagementClient) {
                _serviceBusManagementClient.Dispose();
            }
        }
    }
}
