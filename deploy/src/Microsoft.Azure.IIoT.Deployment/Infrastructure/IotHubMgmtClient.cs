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

    using Microsoft.Azure.Management.IotHub;
    using Microsoft.Azure.Management.IotHub.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    class IotHubMgmtClient : IDisposable {

        public const string DEFAULT_IOT_HUB_NAME_PREFIX = "iothub-";
        public const int NUM_OF_MAX_NAME_AVAILABILITY_CHECKS = 5;

        public const int IOT_HUB_EVENT_HUB_PARTITIONS_COUNT = 4;
        public const int IOT_HUB_EVENT_HUB_RETENTION_TIME_IN_DAYS = 2;

        public const string IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME = "events";

        public const string IOT_HUB_EVENT_HUB_CONSUMER_GROUP_EVENTS_NAME = "events";
        public const string IOT_HUB_EVENT_HUB_CONSUMER_GROUP_TELEMETRY_NAME = "telemetry";
        public const string IOT_HUB_EVENT_HUB_CONSUMER_GROUP_ONBOARDING_NAME = "onboarding";

        public const string IOT_HUB_OWNER_KEY_NAME = "iothubowner";

        private const string kIOT_HUB_CONNECTION_STRING_FORMAT = "HostName={0};SharedAccessKeyName={1};SharedAccessKey={2}";

        private readonly IotHubClient _iotHubClient;

        public IotHubMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            // We need to initialize new RestClient so that we
            // extract RootHttpHandler and DelegatingHandlers out of it.
            var iotHubRestClient = RestClient
                .Configure()
                .WithEnvironment(restClient.Environment)
                .WithCredentials(restClient.Credentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            _iotHubClient = new IotHubClient(
                restClient.Credentials,
                iotHubRestClient.RootHttpHandler,
                iotHubRestClient.Handlers.ToArray()
            ) {
                SubscriptionId = subscriptionId
            };
        }

        /// <summary>
        /// Generate randomized IoT Hub name.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="suffixLen"></param>
        /// <returns></returns>
        public static string GenerateIotHubName(
            string prefix = DEFAULT_IOT_HUB_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        /// <summary>
        /// Checks whether given IoT Hub name is available.
        /// </summary>
        /// <param name="iotHubName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if name is available, False otherwise.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<bool> CheckNameAvailabilityAsync(
            string iotHubName,
            CancellationToken cancellationToken = default
        ) {
            try {
                var nameAvailabilityInfo = await _iotHubClient
                    .IotHubResource
                    .CheckNameAvailabilityAsync(
                        iotHubName,
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
                Log.Error(ex, $"Failed to check IoT Hub Service name availability for {iotHubName}");
                throw;
            }

            // !nameAvailabilityInfo.NameAvailable.HasValue
            throw new Exception($"Failed to check IoT Hub Service name availability for {iotHubName}");
        }

        /// <summary>
        /// Tries to generate IoT Hub name that is available.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>An available name for IoT Hub.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<string> GenerateAvailableNameAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                for (var numOfChecks = 0; numOfChecks < NUM_OF_MAX_NAME_AVAILABILITY_CHECKS; ++numOfChecks) {
                    var iotHubName = GenerateIotHubName();
                    var nameAvailable = await CheckNameAvailabilityAsync(
                            iotHubName,
                            cancellationToken
                        );

                    if (nameAvailable) {
                        return iotHubName;
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
                Log.Error(ex, "Failed to generate unique IoT Hub service name");
                throw;
            }

            var errorMessage = $"Failed to generate unique IoT Hub service name " +
                $"after {NUM_OF_MAX_NAME_AVAILABILITY_CHECKS} retries";

            Log.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        /// <summary>
        /// Create a Stanrard tier (S1) IoT Hub.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="iotHubName"></param>
        /// <param name="iotHubEventHubRetentionTimeInDays"></param>
        /// <param name="iotHubEventHubPartitionsCount"></param>
        /// <param name="storageAccountConectionString"></param>
        /// <param name="storageAccountIotHubContainerName"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IotHubDescription> CreateIotHubAsync(
            IResourceGroup resourceGroup,
            string iotHubName,
            int iotHubEventHubRetentionTimeInDays,
            int iotHubEventHubPartitionsCount,
            string storageAccountConectionString,
            string storageAccountIotHubContainerName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure IoT Hub: {iotHubName} ...");

                var iotHubSkuInfo = new IotHubSkuInfo(
                    name: "S1",
                    tier: IotHubSkuTier.Standard,
                    capacity: 1
                );

                iotHubSkuInfo.Validate();

                var iotHubProperties = new IotHubProperties {
                    IpFilterRules = new List<IpFilterRule>(),
                    EnableFileUploadNotifications = true,
                    Features = "None",
                    EventHubEndpoints = new Dictionary<string, EventHubProperties> {
                        // The only possible keys to this dictionary is 'events'.
                        { IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME, new EventHubProperties {
                                RetentionTimeInDays = iotHubEventHubRetentionTimeInDays,
                                PartitionCount = iotHubEventHubPartitionsCount
                            }
                        }
                    },
                    Routing = new RoutingProperties {
                        Endpoints = new RoutingEndpoints {
                            ServiceBusQueues = null,
                            ServiceBusTopics = null,
                            EventHubs = null,
                            StorageContainers = null
                        },
                        Routes = null,
                        FallbackRoute = new FallbackRouteProperties {
                            Name = "$fallback",
                            //Source = "DeviceMessages",  // Seem to be set by FallbackRouteProperties constructor.
                            Condition = "true",
                            IsEnabled = true,
                            EndpointNames = new List<string> { IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME }
                        }
                    },
                    StorageEndpoints = new Dictionary<string, StorageEndpointProperties> {
                        { "$default", new StorageEndpointProperties {
                                SasTtlAsIso8601 = TimeSpan.FromHours(1),
                                ConnectionString = storageAccountConectionString,
                                ContainerName = storageAccountIotHubContainerName
                            }
                        }
                    },
                    MessagingEndpoints = new Dictionary<string, MessagingEndpointProperties> {
                        { "fileNotifications", new MessagingEndpointProperties {
                                LockDurationAsIso8601 = TimeSpan.FromMinutes(1),
                                TtlAsIso8601 = TimeSpan.FromHours(1),
                                MaxDeliveryCount = 10
                            }
                        }
                    },
                    CloudToDevice = new CloudToDeviceProperties {
                        MaxDeliveryCount = 10,
                        DefaultTtlAsIso8601 = TimeSpan.FromHours(1),
                        Feedback = new FeedbackProperties {
                            LockDurationAsIso8601 = TimeSpan.FromMinutes(1),
                            TtlAsIso8601 = TimeSpan.FromHours(1),
                            MaxDeliveryCount = 10
                        }
                    }
                };

                iotHubProperties.Validate();

                var iotHubDescription = new IotHubDescription {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Sku = iotHubSkuInfo,
                    Properties = iotHubProperties
                };

                iotHubDescription.Validate();

                var iotHub = await _iotHubClient
                    .IotHubResource
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        iotHubName,
                        iotHubDescription,
                        null,
                        cancellationToken
                    );

                Log.Information($"Created Azure IoT Hub: {iotHubName}");

                return iotHub;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure IoT Hub: {iotHubName}");
                throw;
            }
        }

        /// <summary>
        /// Create a consumer group for given IoT Hub built-in Event Hub endpoint.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="iotHub"></param>
        /// <param name="eventHubEndpointName"></param>
        /// <param name="consumerGroupName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<EventHubConsumerGroupInfo> CreateEventHubConsumerGroupAsync(
            IResourceGroup resourceGroup,
            IotHubDescription iotHub,
            string eventHubEndpointName,
            string consumerGroupName,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Verbose($"Creating IoT Hub Event Hub Consumer Group: {consumerGroupName} ...");

                var eventHubConsumerGroupName = new EventHubConsumerGroupName {
                    Name = consumerGroupName,
                };

                var eventHubConsumerGroupInfo = await _iotHubClient
                    .IotHubResource
                    .CreateEventHubConsumerGroupAsync(
                        resourceGroup.Name,
                        iotHub.Name,
                        eventHubEndpointName,
                        consumerGroupName,
                        eventHubConsumerGroupName,
                        cancellationToken
                    );

                Log.Verbose($"Created IoT Hub Event Hub Consumer Group: {consumerGroupName}");

                return eventHubConsumerGroupInfo;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to created IoT Hub Event Hub Consumer Group: {consumerGroupName}");
                throw;
            }
        }

        /// <summary>
        /// Get IoT Hub connection string for given key/policy.
        /// Default key/policy name is 'iothubowner'.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="iotHub"></param>
        /// <param name="keyName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetIotHubConnectionStringAsync(
            IResourceGroup resourceGroup,
            IotHubDescription iotHub,
            string keyName = IOT_HUB_OWNER_KEY_NAME,
            CancellationToken cancellationToken = default
        ) {
            var iotHubKey = await _iotHubClient
                .IotHubResource
                .GetKeysForKeyNameAsync(
                    resourceGroup.Name,
                    iotHub.Name,
                    keyName
                );

            var iotHubConnectionString = string.Format(
                kIOT_HUB_CONNECTION_STRING_FORMAT,
                iotHub.Properties.HostName,
                iotHubKey.KeyName,
                iotHubKey.PrimaryKey,
                cancellationToken
            );

            return iotHubConnectionString;
        }

        public void Dispose() {
            if (null != _iotHubClient) {
                _iotHubClient.Dispose();
            }
        }
    }
}
