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

        public const int IOT_HUB_EVENT_HUB_PARTITIONS_COUNT = 4;

        public const string IOT_HUB_EVENT_HUB_ONBOARDING_ENDPOINT_NAME = "events";
        public const string IOT_HUB_EVENT_HUB_ONBOARDING_CONSUMER_GROUP_NAME = "onboarding";

        public const string IOT_HUB_OWNER_KEY_NAME = "iothubowner";

        private const string IOT_HUB_CONNECTION_STRING_FORMAT = "HostName={0};SharedAccessKeyName={1};SharedAccessKey={2}";

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

        public static string GenerateIotHubName(
            string prefix = DEFAULT_IOT_HUB_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        public async Task<IotHubDescription> CreateIotHubAsync(
            IResourceGroup resourceGroup,
            string iotHubName,
            int iotHubEventHubEndpointsPartitionsCount,
            string storageAccountConectionString,
            string storageAccountIotHubContainerName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                if (null == tags) {
                    tags = new Dictionary<string, string> { };
                }

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
                        { "events", new EventHubProperties {
                                RetentionTimeInDays = 1,
                                PartitionCount = iotHubEventHubEndpointsPartitionsCount
                            }
                        },
                        { "operationsMonitoringEvents", new EventHubProperties {
                                RetentionTimeInDays = 1,
                                PartitionCount = iotHubEventHubEndpointsPartitionsCount
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
                            EndpointNames = new List<string> { "events" }
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

        public async Task<EventHubConsumerGroupInfo> CreateEventHubConsumerGroupAsync(
            IResourceGroup resourceGroup,
            IotHubDescription iotHub,
            string eventHubEndpointName = IOT_HUB_EVENT_HUB_ONBOARDING_ENDPOINT_NAME,
            string consumerGroupName = IOT_HUB_EVENT_HUB_ONBOARDING_CONSUMER_GROUP_NAME,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Verbose($"Creating IoT Hub Event Hub Consumer Group: {consumerGroupName} ...");

                var eventHubConsumerGroupInfo = await _iotHubClient
                    .IotHubResource
                    .CreateEventHubConsumerGroupAsync(
                        resourceGroup.Name,
                        iotHub.Name,
                        eventHubEndpointName,
                        consumerGroupName,
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
                IOT_HUB_CONNECTION_STRING_FORMAT,
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
