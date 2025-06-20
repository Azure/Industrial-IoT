// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Converter between Asset/Device models and PublishedNodesEntryModel
    /// </summary>
    public class AssetDeviceConverter
    {
        /// <summary>
        /// Create asset converter
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public AssetDeviceConverter(IJsonSerializer serializer,
            ILogger<AssetDeviceConverter> logger)
        {
            _serializer = serializer;
            _logger = logger;
        }

        /// <summary>
        /// Convert an Asset to a collection of published nodes entries
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="assets"></param>
        /// <returns></returns>
        public List<PublishedNodesEntryModel> ToPublishedNodes(
            List<(Device device, string deviceName)> devices,
            List<(Asset asset, string assetName)> assets)
        {
            var entries = new List<PublishedNodesEntryModel>();
            var deviceLookup = devices.ToLookup(k => k.deviceName);
            foreach (var (asset, assetName) in assets)
            {
                // Skip if no device reference
                if (asset.DeviceRef == null)
                {
                    // TODO: Add error to status
                    return entries;
                }

                // Get device inbound endpoint
                var found = deviceLookup[asset.DeviceRef.DeviceName].SingleOrDefault();
                if (found.device?.Endpoints?.Inbound == null ||
                    !found.device.Endpoints.Inbound.TryGetValue(
                        asset.DeviceRef.EndpointName, out var endpoint))
                {
                    _logger.LogError("Device referenced by asset was not found");
                    // TODO: Add error to status
                    continue;
                }
                if (!TryDeserialize<PublishedNodesEntryModel>(
                    endpoint.AdditionalConfiguration?.RootElement.GetRawText(),
                    out var deviceEndpoint,
                    () => new PublishedNodesEntryModel { EndpointUrl = string.Empty }))
                {
                    continue;
                }
                deviceEndpoint.EndpointUrl = endpoint.Address;
                // Following device properties don't map directly:
                // - Attributes
                // - Enabled
                // - Endpoints (except OPC UA endpoints)
                // - ExternalDeviceId
                // - LastTransitionTime
                // - Manufacturer
                // - Model
                // - OperatingSystem
                // - OperatingSystemVersion
                // - Version
                // TODO: Add as extension fields
                AddAuthentication(deviceEndpoint, endpoint.Authentication);

                if (asset.Datasets != null && TryDeserialize<PublishedNodesEntryModel>(
                    asset.DefaultDatasetsConfiguration,
                    out var dataSettemplate,
                    () => new PublishedNodesEntryModel { EndpointUrl = string.Empty }))
                {
                    dataSettemplate = WithEndpoint(dataSettemplate, deviceEndpoint);
                    AddDataSets(entries, asset.Datasets, assetName, dataSettemplate,
                        asset.DefaultDatasetsDestinations);
                }

                if (asset.Events != null && TryDeserialize<PublishedNodesEntryModel>(
                    asset.DefaultEventsConfiguration,
                    out var eventsTemplate,
                    () => new PublishedNodesEntryModel { EndpointUrl = string.Empty }))
                {
                    eventsTemplate = WithEndpoint(eventsTemplate, deviceEndpoint);
                    AddEvents(entries, asset.Events, assetName, eventsTemplate,
                        asset.DefaultEventsDestinations);
                }
            }
            return entries;
        }

        private void AddDataSets(List<PublishedNodesEntryModel> entries,
            List<AssetDataset> datasets, string assetName, PublishedNodesEntryModel template,
            List<DatasetDestination>? defaultDatasetsDestinations)
        {
            // Process each dataset as a new DataSetWriter
            foreach (var dataset in datasets)
            {
                var nodes = new List<OpcNodeModel>();
                if (dataset.DataPoints != null)
                {
                    // Map datapoints to OPC nodes
                    foreach (var datapoint in dataset.DataPoints)
                    {
                        if (!TryDeserialize<OpcNodeModel>(
                            datapoint.DataPointConfiguration?
                                .RootElement.GetRawText(),
                            out var node,
                            () => new OpcNodeModel()))
                        {
                            continue;
                        }
                        nodes.Add(node with
                        {
                            Id = datapoint.DataSource,
                            DisplayName = datapoint.Name,
                            DataSetFieldId = datapoint.Name,
                        });
                    }
                }
                // Map dataset configuration on top of entry
                if (!TryDeserialize<PublishedNodesEntryModel>(
                    dataset.DatasetConfiguration, out var datasetTemplate,
                    () => template))
                {
                    continue;
                }
                var entry = CreateEntryForAssetEntity(template, assetName, datasetTemplate, nodes);
                AddDestination(entry, defaultDatasetsDestinations,
                    dataset.Destinations);
                entries.Add(entry with
                {
                    // Dataset maps to DataSetWriter
                    DataSetWriterId = dataset.Name,
                    DataSetName = dataset.Name,
                });
            }
        }

        private void AddEvents(List<PublishedNodesEntryModel> entries, List<AssetEvent> events,
            string assetName, PublishedNodesEntryModel template,
            List<EventStreamDestination>? defaultEventsDestinations)
        {
            // Process each event as a new DataSetWriter
            foreach (var @event in events)
            {
                // TODO create
                var nodes = new List<OpcNodeModel>();
                if (@event.DataPoints != null)
                {
                    // Map datapoints to OPC nodes
                    foreach (var datapoint in @event.DataPoints)
                    {
                        if (!TryDeserialize<OpcNodeModel>(
                            datapoint.DataPointConfiguration?
                                .RootElement.GetRawText(),
                            out var node,
                            () => new OpcNodeModel()))
                        {
                            continue;
                        }
                        nodes.Add(node with
                        {
                            Id = datapoint.DataSource,
                            DisplayName = datapoint.Name,
                            DataSetFieldId = datapoint.Name,
                        });
                    }
                }
                // Map dataset configuration on top of entry
                if (!TryDeserialize<PublishedNodesEntryModel>(
                    @event.EventConfiguration?.RootElement.GetRawText(),
                    out var eventTemplate, () => template))
                {
                    continue;
                }
                var entry = CreateEntryForAssetEntity(template, assetName,
                    eventTemplate, nodes);
                AddDestination(entry, defaultEventsDestinations, @event.Destinations);
                entries.Add(entry with
                {
                    // Dataset maps to DataSetWriter
                    DataSetWriterId = @event.Name,
                    DataSetName = @event.Name,
                });
            }
        }

        /// <summary>
        /// Create entry for an entity in the asset
        /// </summary>
        /// <param name="endpointTemplate"></param>
        /// <param name="assetName"></param>
        /// <param name="entityTemplate"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static PublishedNodesEntryModel CreateEntryForAssetEntity(
            PublishedNodesEntryModel endpointTemplate, string assetName,
            PublishedNodesEntryModel entityTemplate, List<OpcNodeModel> nodes)
        {
            return endpointTemplate with
            {
                // Asset maps to WriterGroup but we split it per transport
                // if there is more than mqtt destination defined. That
                // means we need to retain the asset name somewhere else
                // TODO
                DataSetWriterGroup = assetName,
                // TODO: WriterGroupName = asset.DisplayName,

                // Set defaults for iot operations, there will never be
                // any different configruation allowed
                BatchSize = 1,
                BatchTriggerInterval = 0,
                BatchTriggerIntervalTimespan = null,
                MessagingMode = MessagingMode.SingleDataSet,
                MessageEncoding = MessageEncoding.Json, // TODO, remove

                // Dataset configuration
                DataSetPublishingInterval =
                    entityTemplate.DataSetPublishingInterval,
                DataSetPublishingIntervalTimespan =
                    entityTemplate.DataSetPublishingIntervalTimespan,
                DataSetDescription =
                    entityTemplate.DataSetDescription,
                DataSetClassId =
                    entityTemplate.DataSetClassId,
                DataSetRouting =
                    entityTemplate.DataSetRouting,
                DataSetSamplingIntervalTimespan =
                    entityTemplate.DataSetSamplingIntervalTimespan,
                DataSetSamplingInterval =
                    entityTemplate.DataSetSamplingInterval,
                DataSetExtensionFields =
                    entityTemplate.DataSetExtensionFields,
                DataSetFetchDisplayNames =
                    entityTemplate.DataSetFetchDisplayNames,
                DataSetKeyFrameCount =
                    entityTemplate.DataSetKeyFrameCount,
                DataSetWriterWatchdogBehavior =
                    entityTemplate.DataSetWriterWatchdogBehavior,
                SendKeepAliveDataSetMessages =
                    entityTemplate.SendKeepAliveDataSetMessages,
                DefaultHeartbeatInterval =
                    entityTemplate.DefaultHeartbeatInterval,
                DefaultHeartbeatIntervalTimespan =
                    entityTemplate.DefaultHeartbeatIntervalTimespan,
                MaxKeepAliveCount =
                    entityTemplate.MaxKeepAliveCount,
                RepublishAfterTransfer =
                    entityTemplate.RepublishAfterTransfer,
                DefaultHeartbeatBehavior =
                    entityTemplate.DefaultHeartbeatBehavior,
                Priority =
                    entityTemplate.Priority,
                DisableSubscriptionTransfer =
                    entityTemplate.DisableSubscriptionTransfer,
                OpcNodes = nodes
            };
        }

        /// <summary>
        /// Copy the device endpoint info
        /// </summary>
        /// <param name="template"></param>
        /// <param name="deviceEndpoint"></param>
        private PublishedNodesEntryModel WithEndpoint(PublishedNodesEntryModel template,
            PublishedNodesEntryModel deviceEndpoint)
        {
            return template with
            {
                // Map the configured endpoint configuration into the dataset template
                EndpointUrl = deviceEndpoint.EndpointUrl,
                EndpointSecurityMode = deviceEndpoint.EndpointSecurityMode,
                EndpointSecurityPolicy = deviceEndpoint.EndpointSecurityPolicy,
                UseReverseConnect = deviceEndpoint.UseReverseConnect,
                OpcAuthenticationMode = deviceEndpoint.OpcAuthenticationMode,
                OpcAuthenticationPassword = deviceEndpoint.OpcAuthenticationPassword,
                OpcAuthenticationUsername = deviceEndpoint.OpcAuthenticationUsername,
            };
        }

        /// <summary>
        /// Add dataset destination configuration
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="defaultDestinations"></param>
        /// <param name="destinations"></param>
        private void AddDestination(PublishedNodesEntryModel entry,
            List<DatasetDestination>? defaultDestinations,
            List<DatasetDestination>? destinations)
        {
            entry = WithoutDestinationConfiguration(entry);
            if (destinations?.Count > 1)
            {
                // Report failure
            }
            var destination = destinations?.FirstOrDefault();
            destination ??= defaultDestinations?
                .FirstOrDefault(d => defaultDestinations.Count == 1
                    || d.Target == DatasetTarget.Mqtt);
            if (destination == null)
            {
                entry.WriterGroupTransport = WriterGroupTransport.Mqtt;
                return;
            }
            var configuration = destination.Configuration;
            switch (destination.Target)
            {
                case DatasetTarget.BrokerStateStore:
                    // TODO
                    break;
                case DatasetTarget.Mqtt:
                    entry.WriterGroupTransport = WriterGroupTransport.Mqtt;
                    entry.QualityOfService = configuration.Qos switch
                    {
                        QoS.Qos0 => Furly.Extensions.Messaging.QoS.AtMostOnce,
                        QoS.Qos1 => Furly.Extensions.Messaging.QoS.AtLeastOnce,
                        _ => null
                    };
                    entry.MessageRetention = configuration.Retain switch
                    {
                        Retain.Keep => true,
                        Retain.Never => false,
                        _ => null
                    };
                    entry.QueueName = configuration.Topic;
                    entry.MessageTtlTimespan = configuration.Ttl == null ? null
                        : TimeSpan.FromSeconds(configuration.Ttl.Value);
                    break;
                case DatasetTarget.Storage:
                    entry.WriterGroupTransport = WriterGroupTransport.FileSystem;
                    entry.QueueName = configuration.Path;
                    break;
            }
        }

        /// <summary>
        /// Add dataset destination configuration
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="defaultDestinations"></param>
        /// <param name="destinations"></param>
        private void AddDestination(PublishedNodesEntryModel entry,
            List<EventStreamDestination>? defaultDestinations,
            List<EventStreamDestination>? destinations)
        {
            entry = WithoutDestinationConfiguration(entry);
            if (destinations?.Count > 1)
            {
                // Report failure
            }
            var destination = destinations?.FirstOrDefault();
            destination ??= defaultDestinations?
                .FirstOrDefault(d => defaultDestinations.Count == 1
                    || d.Target == EventStreamTarget.Mqtt);
            if (destination == null)
            {
                entry.WriterGroupTransport = WriterGroupTransport.Mqtt;
                return;
            }
            var configuration = destination.Configuration;
            switch (destination.Target)
            {
                case EventStreamTarget.Mqtt:
                    entry.WriterGroupTransport = WriterGroupTransport.Mqtt;
                    entry.QualityOfService = configuration.Qos switch
                    {
                        QoS.Qos0 => Furly.Extensions.Messaging.QoS.AtMostOnce,
                        QoS.Qos1 => Furly.Extensions.Messaging.QoS.AtLeastOnce,
                        _ => null
                    };
                    entry.MessageRetention = configuration.Retain switch
                    {
                        Retain.Keep => true,
                        Retain.Never => false,
                        _ => null
                    };
                    entry.QueueName = configuration.Topic;
                    entry.MessageTtlTimespan = configuration.Ttl == null ? null
                        : TimeSpan.FromSeconds(configuration.Ttl.Value);
                    break;
                case EventStreamTarget.Storage:
                    entry.WriterGroupTransport = WriterGroupTransport.FileSystem;
                    entry.QueueName = configuration.Path;
                    break;
            }
        }

        /// <summary>
        /// Remove the destination configuration
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private static PublishedNodesEntryModel WithoutDestinationConfiguration(
            PublishedNodesEntryModel entry)
        {
            return entry with
            {
                WriterGroupMessageRetention = null,
                WriterGroupMessageTtlTimepan = null,
                WriterGroupQualityOfService = null,
                WriterGroupQueueName = null,
                DataSetRouting = null,
                QualityOfService = null,
                MessageRetention = null,
                MessageTtlTimespan = null,
                QueueName = null,
            };
        }

        /// <summary>
        /// Add authentication to the entry
        /// </summary>
        /// <param name="template"></param>
        /// <param name="authentication"></param>
        private void AddAuthentication(PublishedNodesEntryModel template,
            Authentication? authentication)
        {
            template.OpcAuthenticationMode = OpcAuthenticationMode.Anonymous;
            if (authentication == null)
            {
                return;
            }
            switch (authentication.Method)
            {
                case Method.Certificate:
                    if (authentication.X509Credentials == null)
                    {
                        break; // throw
                    }
                    template.OpcAuthenticationMode =
                        OpcAuthenticationMode.Certificate;
                    // TODO: load secret
                    template.OpcAuthenticationUsername =
                        authentication.X509Credentials.CertificateSecretName;
                    return;
                case Method.UsernamePassword:
                    if (authentication.UsernamePasswordCredentials == null)
                    {
                        break; // throw
                    }
                    template.OpcAuthenticationMode =
                        OpcAuthenticationMode.UsernamePassword;
                    // TODO: load secret
                    template.OpcAuthenticationPassword =
                        authentication.UsernamePasswordCredentials.PasswordSecretName;
                    template.OpcAuthenticationUsername =
                        authentication.UsernamePasswordCredentials.UsernameSecretName;
                    return;
            }
        }

        /// <summary>
        /// Deserialize configuration
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="value"></param>
        /// <param name="createDefault"></param>
        /// <returns></returns>
        private bool TryDeserialize<T>(string? configuration,
            [NotNullWhen(true)] out T? value, Func<T> createDefault)
        {
            try
            {
                T? result = default;
                if (configuration != null)
                {
                    result = _serializer.Deserialize<T>(configuration);
                }
                result ??= createDefault();
                value = result!;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to convert");
                // Report as status
                value = default;
                return false;
            }
        }

        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
