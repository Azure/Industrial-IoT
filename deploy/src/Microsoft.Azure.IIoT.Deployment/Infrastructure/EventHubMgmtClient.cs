// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.EventHub.Fluent;
    using Microsoft.Azure.Management.EventHub.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    class EventHubMgmtClient : IDisposable
    {
        public const string DEFAULT_EVENT_HUB_NAMESPACE_NAME_PREFIX = "eventhubnamespace-";
        public const string DEFAULT_EVENT_HUB_NAME_PREFIX = "eventhub-";

        private const string EVENT_HUB_NAMESPACE_AUTHORIZATION_RULE = "RootManageSharedAccessKey";

        private readonly EventHubManagementClient _eventHubManagementClient;

        public EventHubMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _eventHubManagementClient = new EventHubManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateEventHubNamespaceName(
            string prefix = DEFAULT_EVENT_HUB_NAMESPACE_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        public static string GenerateEventHubName(
            string prefix = DEFAULT_EVENT_HUB_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        public async Task<EHNamespaceInner> CreateEventHubNamespaceAsync(
            IResourceGroup resourceGroup,
            string eventHubNamespaceName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                if (null == tags) {
                    tags = new Dictionary<string, string> { };
                }

                Log.Information($"Creating Azure Event Hub Namespace: {eventHubNamespaceName} ...");

                var eventHubNamespaceParameters = new EHNamespaceInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Sku = new Sku {
                        Name = SkuName.Basic,
                        Tier = SkuTier.Basic,
                        Capacity = 1
                    },
                    IsAutoInflateEnabled = false,
                    MaximumThroughputUnits = 0
                };

                eventHubNamespaceParameters.Validate();

                var eventHubNamespace = await _eventHubManagementClient
                    .Namespaces
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        eventHubNamespaceName,
                        eventHubNamespaceParameters,
                        cancellationToken
                    );

                Log.Information($"Created Azure Event Hub Namespace: {eventHubNamespaceName}");

                return eventHubNamespace;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure Event Hub Namespace: {eventHubNamespaceName}");
                throw;
            }
        }

        public async Task<string> GetEventHubNamespaceConnectionStringAsync(
            IResourceGroup resourceGroup,
            EHNamespaceInner eventHubNamespace,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Verbose($"Fetching connection string for Event Hub Namespace: {eventHubNamespace.Name} ...");

                var eventHubNamespacesAccessKeys = await _eventHubManagementClient
                    .Namespaces
                    .ListKeysAsync(
                        resourceGroup.Name,
                        eventHubNamespace.Name,
                        EVENT_HUB_NAMESPACE_AUTHORIZATION_RULE,
                        cancellationToken
                    );

                Log.Verbose($"Fetched connection string for Event Hub Namespace: {eventHubNamespace.Name}");

                return eventHubNamespacesAccessKeys.PrimaryConnectionString;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to fetch connection string for Event Hub Namespace: {eventHubNamespace.Name}");
                throw;
            }
        }

        public async Task<EventhubInner> CreateEventHubAsync(
            IResourceGroup resourceGroup,
            EHNamespaceInner eventHubNamespace,
            string eventHubName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default

        ) {
            try {
                if (null == tags) {
                    tags = new Dictionary<string, string> { };
                }

                Log.Information($"Creating Azure Event Hub: {eventHubName} ...");

                var eventHubParameters = new EventhubInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    MessageRetentionInDays = 1,
                    PartitionCount = 2,
                    Status = EntityStatus.Active
                };

                eventHubParameters.Validate();

                var eventHub = await _eventHubManagementClient
                    .EventHubs
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        eventHubNamespace.Name,
                        eventHubName,
                        eventHubParameters,
                        cancellationToken
                    );

                // Create Azure Event Hub Authorization Rule
                var eventHubAuthorizationRuleName = SdkContext
                    .RandomResourceName("iothubroutes-" + eventHubName + "-", 5);

                var eventHubAuthorizationRuleRights = new List<AccessRights> {
                    AccessRights.Send
                };

                var eventHubAuthorizationRule = await _eventHubManagementClient
                    .EventHubs
                    .CreateOrUpdateAuthorizationRuleAsync(
                        resourceGroup.Name,
                        eventHubNamespace.Name,
                        eventHubName,
                        eventHubAuthorizationRuleName,
                        eventHubAuthorizationRuleRights,
                        cancellationToken
                    );

                Log.Information($"Created Azure Event Hub: {eventHubName}");

                return eventHub;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to created Azure Event Hub: {eventHubName}");
                throw;
            }
        }

        public void Dispose() {
            if (null != _eventHubManagementClient) {
                _eventHubManagementClient.Dispose();
            }
        }
    }
}
