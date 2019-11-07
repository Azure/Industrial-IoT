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

        public static string GenerateServiceBusNamespaceName(
            string prefix = DEFAULT_SERVICE_BUS_NAMESPACE_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        public async Task<NamespaceModelInner> CreateServiceBusNamespaceAsync(
            IResourceGroup resourceGroup,
            string serviceBusNamespaceName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                if (null == tags) {
                    tags = new Dictionary<string, string> { };
                }

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
