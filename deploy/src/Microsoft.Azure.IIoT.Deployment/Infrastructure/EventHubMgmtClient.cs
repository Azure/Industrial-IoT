// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
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
        public const int NUM_OF_MAX_NAME_AVAILABILITY_CHECKS = 5;

        public const string EVENT_HUB_CONSUMER_GROUP_TELEMETRY_UX = "telemetry_ux";
        public const int DEFAULT_MESSAGE_RETENTION_IN_DAYS = 2;
        public const int DEFUALT_PARTITION_COUNT = 4;

        private const string kEVENT_HUB_NAMESPACE_AUTHORIZATION_RULE = "RootManageSharedAccessKey";

        private readonly EventHubManagementClient _eventHubManagementClient;

        public EventHubMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            if (string.IsNullOrEmpty(subscriptionId)) {
                throw new ArgumentNullException(nameof(subscriptionId));
            }
            if (restClient is null) {
                throw new ArgumentNullException(nameof(restClient));
            }

            _eventHubManagementClient = new EventHubManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        /// <summary>
        /// Generate Event Hub Namespace name.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="suffixLen"></param>
        /// <returns></returns>
        public static string GenerateEventHubNamespaceName(
            string prefix = DEFAULT_EVENT_HUB_NAMESPACE_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        /// <summary>
        /// Generate Event Hub name.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="suffixLen"></param>
        /// <returns></returns>
        public static string GenerateEventHubName(
            string prefix = DEFAULT_EVENT_HUB_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        /// <summary>
        /// Checks whether given EventHub Namespace name is available.
        /// </summary>
        /// <param name="eventHubNamespaceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if name is available, False otherwise.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<bool> CheckNamespaceNameAvailabilityAsync(
            string eventHubNamespaceName,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrEmpty(eventHubNamespaceName)) {
                throw new ArgumentNullException(nameof(eventHubNamespaceName));
            }

            try {
                var nameAvailabilityInfo = await _eventHubManagementClient
                    .Namespaces
                    .CheckNameAvailabilityAsync(
                        eventHubNamespaceName,
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
                Log.Error(ex, $"Failed to check EventHub Namespace name availability for {eventHubNamespaceName}");
                throw;
            }

            // !nameAvailabilityInfo.NameAvailable.HasValue
            throw new Exception($"Failed to check EventHub Namespace name availability for {eventHubNamespaceName}");
        }

        /// <summary>
        /// Tries to generate EventHub Namespace name that is available.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>An available name for EventHub Namespace.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<string> GenerateAvailableNamespaceNameAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                for (var numOfChecks = 0; numOfChecks < NUM_OF_MAX_NAME_AVAILABILITY_CHECKS; ++numOfChecks) {
                    var eventHubNamespaceName = GenerateEventHubNamespaceName();
                    var nameAvailable = await CheckNamespaceNameAvailabilityAsync(
                            eventHubNamespaceName,
                            cancellationToken
                        );

                    if (nameAvailable) {
                        return eventHubNamespaceName;
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
                Log.Error(ex, "Failed to generate unique EventHub Namespace name");
                throw;
            }

            var errorMessage = $"Failed to generate unique EventHub Namespace name " +
                $"after {NUM_OF_MAX_NAME_AVAILABILITY_CHECKS} retries";

            Log.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        /// <summary>
        /// Create Standard tier Event Hub Namespace.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="eventHubNamespaceName"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<EHNamespaceInner> CreateEventHubNamespaceAsync(
            IResourceGroup resourceGroup,
            string eventHubNamespaceName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            if (resourceGroup is null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (string.IsNullOrEmpty(eventHubNamespaceName)) {
                throw new ArgumentNullException(nameof(eventHubNamespaceName));
            }

            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure Event Hub Namespace: {eventHubNamespaceName} ...");

                var eventHubNamespaceParameters = new EHNamespaceInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Sku = new Sku {
                        Name = SkuName.Standard,
                        Tier = SkuTier.Standard,
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

        /// <summary>
        /// Get Event Hub Namespace connection string for RootManageSharedAccessKey policy.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="eventHubNamespace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetEventHubNamespaceConnectionStringAsync(
            IResourceGroup resourceGroup,
            EHNamespaceInner eventHubNamespace,
            CancellationToken cancellationToken = default
        ) {
            if (resourceGroup is null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (eventHubNamespace is null) {
                throw new ArgumentNullException(nameof(eventHubNamespace));
            }

            try {
                Log.Verbose($"Fetching connection string for Event Hub Namespace: {eventHubNamespace.Name} ...");

                var eventHubNamespacesAccessKeys = await _eventHubManagementClient
                    .Namespaces
                    .ListKeysAsync(
                        resourceGroup.Name,
                        eventHubNamespace.Name,
                        kEVENT_HUB_NAMESPACE_AUTHORIZATION_RULE,
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

        /// <summary>
        /// Create an Event Hub in Event Hub Namespace.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="eventHubNamespace"></param>
        /// <param name="eventHubName"></param>
        /// <param name="messageRetentionInDays"></param>
        /// <param name="partitionCount"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<EventhubInner> CreateEventHubAsync(
            IResourceGroup resourceGroup,
            EHNamespaceInner eventHubNamespace,
            string eventHubName,
            int messageRetentionInDays,
            int partitionCount,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            if (resourceGroup is null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (eventHubNamespace is null) {
                throw new ArgumentNullException(nameof(eventHubNamespace));
            }
            if (string.IsNullOrEmpty(eventHubName)) {
                throw new ArgumentNullException(nameof(eventHubName));
            }

            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure Event Hub: {eventHubName} ...");

                var eventHubParameters = new EventhubInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    MessageRetentionInDays = messageRetentionInDays,
                    PartitionCount = partitionCount,
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

        /// <summary>
        /// Create a consumer group for an Event Hub.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="eventHubNamespace"></param>
        /// <param name="eventHub"></param>
        /// <param name="consumerGroupName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsumerGroupInner> CreateConsumerGroupAsync(
            IResourceGroup resourceGroup,
            EHNamespaceInner eventHubNamespace,
            EventhubInner eventHub,
            string consumerGroupName,
            CancellationToken cancellationToken = default
        ) {
            if (resourceGroup is null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (eventHubNamespace is null) {
                throw new ArgumentNullException(nameof(eventHubNamespace));
            }
            if (eventHub is null) {
                throw new ArgumentNullException(nameof(eventHub));
            }
            if (string.IsNullOrEmpty(consumerGroupName)) {
                throw new ArgumentNullException(nameof(consumerGroupName));
            }

            try {
                var consumerGroup = await _eventHubManagementClient
                    .ConsumerGroups
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        eventHubNamespace.Name,
                        eventHub.Name,
                        consumerGroupName,
                        cancellationToken: cancellationToken
                    );

                return consumerGroup;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to created a consumer group for " +
                    $"'{eventHub.Name}' Event Hub: {consumerGroupName}");
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
