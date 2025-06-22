// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.Iot.Operations.Connector.Files;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using Furly.Azure.IoT.Operations.Services;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Asset and device configuration integration with Azure iot operations. Converts asset
    /// and device notifications into published nodes representation and signals configuration
    /// status errors back.
    /// </summary>
    public sealed class AssetDeviceIntegration : IAdrNotification, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Currently known assets
        /// </summary>
        internal IEnumerable<AssetResource> Assets => _assets.Values;

        /// <summary>
        /// Currently known devices
        /// </summary>
        internal IEnumerable<DeviceResource> Devices => _devices.Values;

        /// <summary>
        /// Create asset converter
        /// </summary>
        /// <param name="client"></param>
        /// <param name="publishedNodes"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public AssetDeviceIntegration(IAioAdrClient client, IPublishedNodesServices publishedNodes,
            IJsonSerializer serializer, ILogger<AssetDeviceIntegration> logger)
        {
            _client = client;
            _publishedNodes = publishedNodes;
            _serializer = serializer;
            _logger = logger;
            _cts = new CancellationTokenSource();
            _changeFeed
                = Channel.CreateUnbounded<(string, Resource)>(
                    new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = false
                    });
            _processor = Task.Factory.StartNew(() => RunAsync(_cts.Token), _cts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            try
            {
                await _cts.CancelAsync().ConfigureAwait(false);
                _changeFeed.Writer.TryComplete();
                try
                {
                    await _processor.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to close processor");
                }
            }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public void OnDeviceCreated(string deviceName, string inboundEndpointName,
            Device device)
        {
            var name = CreateDeviceKey(deviceName, inboundEndpointName);
            var deviceResource = new DeviceResource(deviceName, device);
            _devices.AddOrUpdate(name, deviceResource);
            if (!_changeFeed.Writer.TryWrite((name, deviceResource)))
            {
                _logger.LogError("Failed to process creation of device {Device}", name);
            }
        }

        /// <inheritdoc/>
        public void OnDeviceUpdated(string deviceName, string inboundEndpointName,
            Device device)
        {
            var name = CreateDeviceKey(deviceName, inboundEndpointName);
            var deviceResource = new DeviceResource(deviceName, device);
            _devices.AddOrUpdate(name, deviceResource);
            if (!_changeFeed.Writer.TryWrite((name, deviceResource)))
            {
                _logger.LogError("Failed to process update of device {Device}", name);
            }
        }

        /// <inheritdoc/>
        public void OnDeviceDeleted(string deviceName, string inboundEndpointName,
            Device? device)
        {
            var name = CreateDeviceKey(deviceName, inboundEndpointName);
            if (_devices.TryRemove(name, out var deviceResource) &&
               !_changeFeed.Writer.TryWrite((name, deviceResource)))
            {
                _logger.LogError("Failed to process deletion of device {Device}", name);
            }
        }

        /// <inheritdoc/>
        public void OnAssetCreated(string deviceName, string inboundEndpointName,
            string assetName, Asset asset)
        {
            var name = CreateAssetKey(deviceName, inboundEndpointName, assetName);
            var assetResource = new AssetResource(deviceName, asset);
            _assets.AddOrUpdate(name, assetResource);
            if (!_changeFeed.Writer.TryWrite((name, assetResource)))
            {
                _logger.LogError("Failed to process creation of asset {Asset}", name);
            }
        }

        /// <inheritdoc/>
        public void OnAssetUpdated(string deviceName, string inboundEndpointName,
            string assetName, Asset asset)
        {
            var name = CreateAssetKey(deviceName, inboundEndpointName, assetName);
            var assetResource = new AssetResource(deviceName, asset);
            _assets.AddOrUpdate(name, assetResource);
            if (!_changeFeed.Writer.TryWrite((name, assetResource)))
            {
                _logger.LogError("Failed to process update of asset {Asset}", name);
            }
        }

        /// <inheritdoc/>
        public void OnAssetDeleted(string deviceName, string inboundEndpointName,
            string assetName, Asset? asset)
        {
            var name = CreateAssetKey(deviceName, inboundEndpointName, assetName);
            if (_assets.TryRemove(name, out var asseteResource) &&
               !_changeFeed.Writer.TryWrite((name, asseteResource)))
            {
                _logger.LogError("Failed to process deletion of asset {Asset}", name);
            }
        }

        /// <summary>
        /// Monitors the change events produced as result of the notifications and then
        /// processes the current state.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                // Read changes in batches
                var updatesReceived = false;
                while (!ct.IsCancellationRequested && _changeFeed.Reader.TryRead(out var change))
                {
                    (var name, var resource) = change;
                    switch (resource)
                    {
                        case AssetResource asset:
                            if (!_assets.ContainsKey(name))
                            {
                                await _client.StopMonitoringAssetsAsync(
                                    asset.Asset.DeviceRef.DeviceName,
                                    asset.Asset.DeviceRef.EndpointName, ct).ConfigureAwait(false);
                            }
                            else
                            {
                                await _client.StartMonitoringAssetsAsync(
                                    asset.Asset.DeviceRef.DeviceName,
                                    asset.Asset.DeviceRef.EndpointName, ct).ConfigureAwait(false);
                            }
                            break;
                        case DeviceResource device:
                            if (device.Device.Endpoints?.Inbound != null)
                            {
                                foreach (var endpoint in device.Device.Endpoints.Inbound.Keys)
                                {
                                    if (!_devices.ContainsKey(name))
                                    {
                                        // Removed, stop monitoring assets and clear asset list
                                        await _client.StopMonitoringAssetsAsync(device.DeviceName,
                                            endpoint, ct).ConfigureAwait(false);
                                        break;
                                    }
                                    else
                                    {
                                        await _client.StartMonitoringAssetsAsync(device.DeviceName,
                                            endpoint, ct).ConfigureAwait(false);
                                    }
                                }
                            }
                            break;
                        default:
                            Debug.Fail("Should not happen");
                            break;
                    }
                    updatesReceived = true;
                }
                // Process changes
                if (updatesReceived)
                {
                    // Reconcile devices with assets - remove assets without devices or endpoints
                    foreach (var (name, asset) in _assets)
                    {
                        var key = CreateDeviceKey(
                            asset.Asset.DeviceRef.DeviceName,
                            asset.Asset.DeviceRef.EndpointName);
                        if (!_devices.ContainsKey(key))
                        {
                            _logger.LogDebug("Removing asset {Asset} without device {Device}",
                                name, key);
                            _assets.TryRemove(name, out var _);
                            await _client.StopMonitoringAssetsAsync(
                                asset.Asset.DeviceRef.DeviceName,
                                asset.Asset.DeviceRef.EndpointName, ct).ConfigureAwait(false);
                        }
                    }

                    // Convert
                    var errors = new ValidationErrors(this);
                    var devices = _devices.Values.ToList();
                    var assets = _assets.Values.ToList();

                    _logger.LogInformation("Converting {Assets} Assets on {Devices} devices...",
                        assets.Count, devices.Count);
                    var entries = await ToPublishedNodesAsync(devices, assets, errors,
                        ct).ConfigureAwait(false);

                    // Report all errors
                    await errors.ReportAsync(ct).ConfigureAwait(false);

                    // Apply to configuration
                    await _publishedNodes.SetConfiguredEndpointsAsync(entries,
                        ct).ConfigureAwait(false);
                    _logger.LogInformation("{Assets} Assets on {Devices} devices updated.",
                        assets.Count, devices.Count);
                }
                await _changeFeed.Reader.WaitToReadAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Convert an Asset to a collection of published nodes entries
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="assets"></param>
        /// <param name="errors"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async ValueTask<List<PublishedNodesEntryModel>> ToPublishedNodesAsync(
            ICollection<DeviceResource> devices, ICollection<AssetResource> assets,
            ValidationErrors errors, CancellationToken ct)
        {
            var entries = new List<PublishedNodesEntryModel>();
            var deviceLookup = devices.ToLookup(k => k.DeviceName);
            foreach (var asset in assets)
            {
                // Get device inbound endpoint
                var deviceResource = deviceLookup[asset.Asset.DeviceRef.DeviceName].SingleOrDefault();
                if (deviceResource?.Device?.Endpoints?.Inbound == null ||
                    !deviceResource.Device.Endpoints.Inbound.TryGetValue(
                        asset.Asset.DeviceRef.EndpointName, out var endpoint))
                {
                    _logger.LogError("Device referenced by asset was not found");
                    errors.OnError(asset, kDeviceNotFoundErrorCode, "Device was not found");
                    continue;
                }
                var deviceEndpointResource = new DeviceEndpointResource(
                    deviceResource.DeviceName, deviceResource.Device,
                    asset.Asset.DeviceRef.EndpointName);
                var endpointConfiguration = Deserialize(
                    endpoint.AdditionalConfiguration?.RootElement.GetRawText(),
                    () => new DeviceEndpointModel(),
                    errors, deviceEndpointResource);
                if (endpointConfiguration == null)
                {
                    continue;
                }

                // Asset identification
                var assetEndpoint = new PublishedNodesEntryModel
                {
                    EndpointUrl = endpoint.Address,
                    EndpointSecurityMode = endpointConfiguration.EndpointSecurityMode,
                    EndpointSecurityPolicy = endpointConfiguration.EndpointSecurityPolicy,
                    DumpConnectionDiagnostics = endpointConfiguration.DumpConnectionDiagnostics,
                    UseReverseConnect = endpointConfiguration.UseReverseConnect,

                    // We map asset name to writer group as it contains writers for each of its
                    // entities. This will be split per destination, but this is ok as the name
                    // is retained and the Id property receives the unique group name.
                    DataSetWriterGroup = asset.AssetName,
                    WriterGroupExternalId = asset.Asset.Uuid,
                    // And stick all unknown properties and attributes into the new extension section
                    WriterGroupProperties = CollectAssetAndDeviceProperties(asset, deviceResource)

                    // TODO In WoT side DataSetName is the Asset name. This also needs
                    // to be fixed over there!
                };
                var credentials = _client.GetEndpointCredentials(asset.Asset.DeviceRef.DeviceName,
                    asset.Asset.DeviceRef.EndpointName, endpoint);
                assetEndpoint = AddEndpointCredentials(assetEndpoint, credentials, errors,
                    deviceEndpointResource);
                if (asset.Asset.Datasets != null)
                {
                    var dataSetTemplate = Deserialize(asset.Asset.DefaultDatasetsConfiguration,
                        () => new PublishedNodesEntryModel { EndpointUrl = string.Empty },
                        errors, asset);
                    if (dataSetTemplate != null)
                    {
                        dataSetTemplate = WithEndpoint(dataSetTemplate, assetEndpoint);
                        foreach (var dataset in asset.Asset.Datasets)
                        {
                            var datasetResource = new DataSetResource(asset.AssetName, asset.Asset, dataset);
                            await AddEntryForDataSetAsync(dataSetTemplate, datasetResource,
                                entries, errors, ct).ConfigureAwait(false);
                        }
                    }
                }
                if (asset.Asset.Events != null)
                {
                    var eventsTemplate = Deserialize(asset.Asset.DefaultEventsConfiguration,
                        () => new PublishedNodesEntryModel { EndpointUrl = string.Empty },
                        errors, asset);
                    if (eventsTemplate != null)
                    {
                        eventsTemplate = WithEndpoint(eventsTemplate, assetEndpoint);
                        foreach (var @event in asset.Asset.Events)
                        {
                            var eventResource = new EventResource(asset.AssetName, asset.Asset, @event);
                            await AddEntryForEventAsync(eventsTemplate, eventResource,
                                entries, errors, ct).ConfigureAwait(false);
                        }
                    }
                }

                if (asset.Asset.Streams != null)
                {
                    foreach (var stream in asset.Asset.Streams)
                    {
                        var streamResource = new StreamResource(asset.AssetName,
                            asset.Asset, stream);
                        errors.OnError(streamResource, kNotSupportedErrorCode,
                            "Streams not supported yet");
                    }
                }

                if (asset.Asset.ManagementGroups != null)
                {
                    foreach (var managementGroup in asset.Asset.ManagementGroups)
                    {
                        var managementGroupResource = new ManagementGroupResource(asset.AssetName,
                            asset.Asset, managementGroup);
                        errors.OnError(managementGroupResource, kNotSupportedErrorCode,
                            "Management groups not supported yet");
                    }
                }
            }
            return entries;
        }

        /// <summary>
        /// Create extension fields for attributes and properties that do not exist in our models
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        private static Dictionary<string, VariantValue>? CollectAssetAndDeviceProperties(
            AssetResource asset, DeviceResource device)
        {
            var fields = new Dictionary<string, VariantValue>();
            static void Add(Dictionary<string, VariantValue> fields, string key, VariantValue? value)
            {
                if (!VariantValue.IsNullOrNullValue(value))
                {
                    fields.Add(key, value);
                }
            }

            if (device.Device.Attributes != null)
            {
                foreach (var attr in device.Device.Attributes)
                {
                    Add(fields, attr.Key, attr.Value);
                }
            }
            Add(fields, nameof(Device.ExternalDeviceId), device.Device.ExternalDeviceId);
            Add(fields, nameof(Device.Model), device.Device.Model);
            Add(fields, nameof(Device.Manufacturer), device.Device.Manufacturer);
            Add(fields, nameof(Device.OperatingSystem), device.Device.OperatingSystem);
            Add(fields, nameof(Device.OperatingSystemVersion), device.Device.OperatingSystemVersion);
            Add(fields, nameof(Device.Version), device.Device.Version);

            if (asset.Asset.Attributes != null)
            {
                foreach (var attr in asset.Asset.Attributes)
                {
                    Add(fields, attr.Key, attr.Value);
                }
            }
            Add(fields, nameof(Asset.ExternalAssetId), asset.Asset.ExternalAssetId);
            Add(fields, nameof(Asset.HardwareRevision), asset.Asset.HardwareRevision);
            Add(fields, nameof(Asset.Manufacturer), asset.Asset.Manufacturer);
            Add(fields, nameof(Asset.ManufacturerUri), asset.Asset.ManufacturerUri);
            Add(fields, nameof(Asset.Model), asset.Asset.Model);
            Add(fields, nameof(Asset.DisplayName), asset.Asset.DisplayName);
            Add(fields, nameof(Asset.ProductCode), asset.Asset.ProductCode);
            Add(fields, nameof(Asset.SerialNumber), asset.Asset.SerialNumber);
            Add(fields, nameof(Asset.SoftwareRevision), asset.Asset.SoftwareRevision);
            Add(fields, nameof(Asset.Version), asset.Asset.Version);
            Add(fields, nameof(Asset.DocumentationUri), asset.Asset.DocumentationUri);
            Add(fields, nameof(Asset.Description), asset.Asset.Description);

            if (fields.Count == 0)
            {
                return null;
            }
            return fields;
        }

        /// <summary>
        /// Add authentication for the endpoint to the entry
        /// </summary>
        /// <param name="template"></param>
        /// <param name="authentication"></param>
        /// <param name="errors"></param>
        /// <param name="resource"></param>
        private PublishedNodesEntryModel AddEndpointCredentials(
            PublishedNodesEntryModel template, EndpointCredentials authentication,
            ValidationErrors errors, DeviceEndpointResource resource)
        {
            template.OpcAuthenticationMode = OpcAuthenticationMode.Anonymous;
            if (authentication == null)
            {
                return template;
            }
            switch (authentication.AuthenticationMethod)
            {
                case Method.Certificate:
                    if (string.IsNullOrWhiteSpace(authentication.ClientCertificate))
                    {
                        errors.OnError(resource, kAuthenticationValueMissing,
                            "Client certificate missing");
                        break;
                    }
                    return template with
                    {
                        OpcAuthenticationMode = OpcAuthenticationMode.Certificate,
                        OpcAuthenticationUsername = authentication.ClientCertificate
                    };
                case Method.UsernamePassword:
                    if (string.IsNullOrWhiteSpace(authentication.Username) ||
                        string.IsNullOrWhiteSpace(authentication.Password))
                    {
                        errors.OnError(resource, kAuthenticationValueMissing,
                            "User name or password missing");
                        break;
                    }
                    return template with
                    {
                        OpcAuthenticationMode = OpcAuthenticationMode.UsernamePassword,
                        OpcAuthenticationUsername = authentication.Username,
                        OpcAuthenticationPassword = authentication.Password
                    };
            }
            return template;
        }

        /// <summary>
        /// Adds a new entry for a dataset. Will not add one if configuration parsing fails.
        /// This does not apply to opc nodes, if any fail to validate, there will still be
        /// an entry, but the status will reflect this error.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="resource"></param>
        /// <param name="entries"></param>
        /// <param name="errors"></param>
        /// <param name="ct"></param>
        private ValueTask AddEntryForDataSetAsync(PublishedNodesEntryModel template,
            DataSetResource resource, List<PublishedNodesEntryModel> entries,
            ValidationErrors errors, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Map dataset configuration on top of entry
            var datasetTemplate = Deserialize(resource.DataSet.DatasetConfiguration, () => template,
                errors, resource);
            if (datasetTemplate == null)
            {
                return ValueTask.CompletedTask;
            }

            // TODO: Resolve typeref

            var nodes = new List<OpcNodeModel>();
            if (resource.DataSet.DataPoints != null)
            {
                // Map datapoints to OPC nodes
                foreach (var datapoint in resource.DataSet.DataPoints)
                {
                    var node = Deserialize(
                        datapoint.DataPointConfiguration?.RootElement.GetRawText(),
                        () => new OpcNodeModel(), errors, resource);
                    if (node == null)
                    {
                        continue;
                    }
                    nodes.Add(node with
                    {
                        Id = datapoint.DataSource,
                        DisplayName = datapoint.Name,
                        FetchDisplayName = false,
                        DataSetFieldId = datapoint.Name,
                        VariableTypeDefinitionId = datapoint.TypeRef
                    });
                }
            }
            var entry = CreateEntryForEntityOfAsset(template, datasetTemplate, nodes);
            AddDestination(entry, resource.Asset.DefaultDatasetsDestinations, resource.DataSet.Destinations,
                errors, resource);
            entries.Add(entry with
            {
                // Dataset maps to DataSetWriter
                DataSetWriterId = resource.DataSet.Name,
                DataSetName = resource.DataSet.Name,
                DataSetType = resource.DataSet.TypeRef
            });
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Add an entry for the event. Will not add one if validation of content fails.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="resource"></param>
        /// <param name="entries"></param>
        /// <param name="errors"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private ValueTask AddEntryForEventAsync(PublishedNodesEntryModel template,
            EventResource resource, List<PublishedNodesEntryModel> entries,
            ValidationErrors errors, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Map dataset configuration on top of entry
            var eventTemplate = Deserialize(resource.Event.EventConfiguration?.RootElement.GetRawText(),
                () => template, errors, resource);
            if (eventTemplate == null)
            {
                return ValueTask.CompletedTask;
            }

            // Create event node
            var node = new OpcNodeModel
            {
                Id = resource.Event.EventNotifier,
                DisplayName = resource.Event.Name,
                DataSetFieldId = resource.Event.Name,
                FetchDisplayName = false,
                EventFilter = new EventFilterModel
                {
                    TypeDefinitionId = resource.Event.TypeRef
                }
            };

            // Map datapoints a filter statement
            if (resource.Event.DataPoints != null)
            {
                var selectClause = new List<SimpleAttributeOperandModel>();
                foreach (var datapoint in resource.Event.DataPoints)
                {
                    var operand = Deserialize<SimpleAttributeOperandModel>(
                        datapoint.DataPointConfiguration?.RootElement.GetRawText(),
                        () => new SimpleAttributeOperandModel(), errors, resource);
                    if (operand != null)
                    {
                        try
                        {
                            selectClause.Add(operand with
                            {
                                BrowsePath = datapoint.DataSource.ToRelativePath(out _).AsString(),
                                DisplayName = datapoint.Name,
                            });
                        }
                        catch (Exception ex)
                        {
                            errors.OnError(resource, kJsonSerializationErrorCode + "."
                                + ex.HResult.ToString(CultureInfo.InvariantCulture), ex.Message);
                        }
                    }
                }
                var newEventFilter = node.EventFilter with { SelectClauses = selectClause };
            }
            var entry = CreateEntryForEntityOfAsset(template, eventTemplate, [node]);
            AddDestination(entry, resource.Asset.DefaultEventsDestinations,
                resource.Event.Destinations, errors, resource);
            entries.Add(entry with
            {
                // Dataset maps to DataSetWriter
                DataSetWriterId = resource.Event.Name,
                DataSetName = resource.Event.Name,
                DataSetType = resource.Event.TypeRef
            });
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Create entry for an entity in the asset
        /// </summary>
        /// <param name="endpointTemplate"></param>
        /// <param name="entityTemplate"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static PublishedNodesEntryModel CreateEntryForEntityOfAsset(
            PublishedNodesEntryModel endpointTemplate, PublishedNodesEntryModel entityTemplate,
            List<OpcNodeModel> nodes)
        {
            return endpointTemplate with
            {
                //
                // Set defaults for iot operations, there will never be any different configuration
                // allowed in the resource of AIO
                //
                OpcNodes = nodes,
                BatchSize = 0,
                BatchTriggerInterval = 0,
                BatchTriggerIntervalTimespan = null,
                MessagingMode = MessagingMode.SingleDataSet, // TODO: Validate this is the correct format
                DataSetFetchDisplayNames = false,
                MessageEncoding =
                    entityTemplate.MessageEncoding == MessageEncoding.Avro
                        ? MessageEncoding.Avro : MessageEncoding.Json,

                // Dataset configuration
                DataSetPublishingInterval =
                    entityTemplate.DataSetPublishingInterval,
                DataSetPublishingIntervalTimespan =
                    entityTemplate.DataSetPublishingIntervalTimespan,
                DataSetRouting =
                    entityTemplate.DataSetRouting,
                DataSetSamplingIntervalTimespan =
                    entityTemplate.DataSetSamplingIntervalTimespan,
                DataSetSamplingInterval =
                    entityTemplate.DataSetSamplingInterval,
                DataSetClassId =
                    entityTemplate.DataSetClassId,
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
            };
        }

        /// <summary>
        /// Copy the device endpoint resource to the template and return a new entry
        /// </summary>
        /// <param name="template"></param>
        /// <param name="deviceEndpoint"></param>
        private static PublishedNodesEntryModel WithEndpoint(PublishedNodesEntryModel template,
            PublishedNodesEntryModel deviceEndpoint)
        {
            return template with
            {
                // Map the configured endpoint configuration into the event/dataset template
                EndpointUrl = deviceEndpoint.EndpointUrl,
                EndpointSecurityMode = deviceEndpoint.EndpointSecurityMode,
                EndpointSecurityPolicy = deviceEndpoint.EndpointSecurityPolicy,
                DumpConnectionDiagnostics = deviceEndpoint.DumpConnectionDiagnostics,
                UseReverseConnect = deviceEndpoint.UseReverseConnect,
                OpcAuthenticationMode = deviceEndpoint.OpcAuthenticationMode,
                OpcAuthenticationPassword = deviceEndpoint.OpcAuthenticationPassword,
                OpcAuthenticationUsername = deviceEndpoint.OpcAuthenticationUsername,
                UseSecurity = null,
                EncryptedAuthPassword = null,
                EncryptedAuthUsername = null
            };
        }

        /// <summary>
        /// Add dataset destination configuration
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="defaultDestinations"></param>
        /// <param name="destinations"></param>
        /// <param name="errors"></param>
        /// <param name="resource"></param>
        private static PublishedNodesEntryModel AddDestination(PublishedNodesEntryModel entry,
            List<DatasetDestination>? defaultDestinations, List<DatasetDestination>? destinations,
            ValidationErrors errors, Resource resource)
        {
            entry = WithoutDestinationConfiguration(entry);
            if (destinations?.Count > 1)
            {
                errors.OnError(resource, kTooManyDestinationsError,
                    "More than 1 destination is not allowed for datasets. Using Mqtt");
            }
            var destination = destinations?.FirstOrDefault();
            destination ??= defaultDestinations?
                .FirstOrDefault(d => defaultDestinations.Count == 1
                    || d.Target == DatasetTarget.Mqtt);
            if (destination == null)
            {
                return entry with { WriterGroupTransport = WriterGroupTransport.Mqtt };
            }
            var configuration = destination.Configuration;
            switch (destination.Target)
            {
                case DatasetTarget.BrokerStateStore:
                    // TODO
                    return entry;
                case DatasetTarget.Mqtt:
                    return entry with
                    {
                        WriterGroupTransport = WriterGroupTransport.Mqtt,
                        QualityOfService = configuration.Qos switch
                        {
                            QoS.Qos0 => Furly.Extensions.Messaging.QoS.AtMostOnce,
                            QoS.Qos1 => Furly.Extensions.Messaging.QoS.AtLeastOnce,
                            _ => null
                        },
                        MessageRetention = configuration.Retain switch
                        {
                            Retain.Keep => true,
                            Retain.Never => false,
                            _ => null
                        },
                        QueueName = configuration.Topic,
                        MessageTtlTimespan = configuration.Ttl == null ? null
                            : TimeSpan.FromSeconds(configuration.Ttl.Value)
                    };
                case DatasetTarget.Storage:
                    return entry with
                    {
                        WriterGroupTransport = WriterGroupTransport.FileSystem,
                        QueueName = configuration.Path
                    };
            }
            return entry;
        }

        /// <summary>
        /// Add event and stream destination configuration
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="defaultDestinations"></param>
        /// <param name="destinations"></param>
        /// <param name="errors"></param>
        /// <param name="resource"></param>
        private static PublishedNodesEntryModel AddDestination(PublishedNodesEntryModel entry,
            List<EventStreamDestination>? defaultDestinations, List<EventStreamDestination>? destinations,
            ValidationErrors errors, Resource resource)
        {
            entry = WithoutDestinationConfiguration(entry);
            if (destinations?.Count > 1)
            {
                errors.OnError(resource, kTooManyDestinationsError,
                    "More than 1 destination is not allowed for events. Using Mqtt");
            }
            var destination = destinations?.FirstOrDefault();
            destination ??= defaultDestinations?
                .FirstOrDefault(d => defaultDestinations.Count == 1
                    || d.Target == EventStreamTarget.Mqtt);
            if (destination == null)
            {
                return entry with { WriterGroupTransport = WriterGroupTransport.Mqtt };
            }
            var configuration = destination.Configuration;
            switch (destination.Target)
            {
                case EventStreamTarget.Mqtt:
                    return entry with
                    {
                        WriterGroupTransport = WriterGroupTransport.Mqtt,
                        QualityOfService = configuration.Qos switch
                        {
                            QoS.Qos0 => Furly.Extensions.Messaging.QoS.AtMostOnce,
                            QoS.Qos1 => Furly.Extensions.Messaging.QoS.AtLeastOnce,
                            _ => null
                        },
                        MessageRetention = configuration.Retain switch
                        {
                            Retain.Keep => true,
                            Retain.Never => false,
                            _ => null
                        },
                        QueueName = configuration.Topic,
                        MessageTtlTimespan = configuration.Ttl == null ? null
                            : TimeSpan.FromSeconds(configuration.Ttl.Value)
                    };
                case EventStreamTarget.Storage:
                    return entry with
                    {
                        WriterGroupTransport = WriterGroupTransport.FileSystem,
                        QueueName = configuration.Path
                    };
            }
            return entry;
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
        /// Deserialize configuration
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="createDefault"></param>
        /// <param name="errors"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        private T? Deserialize<T>(string? configuration, Func<T> createDefault,
            ValidationErrors errors, Resource resource)
        {
            try
            {
                T? result = default;
                if (configuration != null)
                {
                    result = _serializer.Deserialize<T>(configuration);
                }
                return result ??= createDefault();
            }
            catch (Exception ex)
            {
                errors.OnError(resource, kJsonSerializationErrorCode + "."
                    + ex.HResult.ToString(CultureInfo.InvariantCulture), ex.Message);
                return default;
            }
        }

        private static string CreateDeviceKey(string deviceName, string inboundEndpointName)
            => $"{deviceName}_{inboundEndpointName}";

        private static string CreateAssetKey(string deviceName, string inboundEndpointName,
            string assetName) => $"{deviceName}_{inboundEndpointName}_{assetName}";

        /// <summary>
        /// Base resoure and corresponding resources
        /// </summary>
        internal abstract record class Resource();

        internal record class DeviceResource(string DeviceName, Device Device) : Resource;
        internal record class AssetResource(string AssetName, Asset Asset) : Resource;

        internal record class DeviceEndpointResource(string DeviceName, Device Device,
            string EndpointName)
            : DeviceResource(DeviceName, Device);
        internal record class DataSetResource(string AssetName, Asset Asset,
            AssetDataset DataSet)
            : AssetResource(AssetName, Asset);
        internal record class EventResource(string AssetName, Asset Asset,
            AssetEvent Event)
            : AssetResource(AssetName, Asset);
        internal record class StreamResource(string AssetName, Asset Asset,
            AssetStream Stream)
            : AssetResource(AssetName, Asset);
        internal record class ManagementGroupResource(string AssetName, Asset Asset,
            AssetManagementGroup ManagementGroup)
            : AssetResource(AssetName, Asset);
        internal record class ManagementActionResource(string AssetName, Asset Asset,
            AssetManagementGroup ManagementGroup, AssetManagementGroupAction Action)
            : ManagementGroupResource(AssetName, Asset, ManagementGroup);

        /// <summary>
        /// Collects validation errors
        /// </summary>
        internal sealed class ValidationErrors
        {
            /// <summary>
            /// Create error collector
            /// </summary>
            /// <param name="outer"></param>
            public ValidationErrors(AssetDeviceIntegration outer)
            {
                _outer = outer;
            }

            /// <summary>
            /// Collect error for a particular resource such as asset, dataset
            /// or device.
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            public void OnError(Resource resource, string code, string error)
            {
                switch (resource)
                {
                    case DeviceEndpointResource dr:
                        Add(dr, code, error);
                        return;
                    case DeviceResource d:
                        Add(d, code, error);
                        return;
                    case DataSetResource d:
                        Add(d, code, error);
                        return;
                    case EventResource e:
                        Add(e, code, error);
                        return;
                    case StreamResource e:
                        Add(e, code, error);
                        return;
                    case ManagementActionResource e:
                        Add(e, code, error);
                        return;
                    case ManagementGroupResource e:
                        Add(e, code, error);
                        return;
                    case AssetResource a:
                        Add(a, code, error);
                        return;
                    default:
                        return;
                }
            }

            /// <summary>
            /// Report what is to be reported
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async ValueTask ReportAsync(CancellationToken ct)
            {
                var client = _outer._client;
                foreach (var (deviceName, status) in _devices)
                {
                    await client.UpdateDeviceStatusAsync(deviceName, null!,
                        status.Status, ct: ct).ConfigureAwait(false);
                }
                foreach (var (assetName, status) in _assets)
                {
                    await client.UpdateAssetStatusAsync(
                        status.Asset.DeviceRef.DeviceName,
                        status.Asset.DeviceRef.EndpointName,
                        assetName, status.Status, ct: ct).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Add the error to the device status for the device resource
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            private void Add(DeviceResource resource, string code, string error)
            {
                var status = GetDeviceStatusResource(resource);
                if (status.Status.Config == null)
                {
                    status.Status.Config = new ConfigStatus
                    {
                        Error = new ConfigError
                        {
                            Code = code,
                            Message = error
                        }
                    };
                }
            }

            /// <summary>
            /// Add the error to the device endpoint resource
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            private void Add(DeviceEndpointResource resource, string code, string error)
            {
                var status = GetDeviceStatusResource(resource);
                status.Status.Endpoints ??= new DeviceStatusEndpoint
                {
                    Inbound = new Dictionary<string, DeviceStatusInboundEndpointSchemaMapValue>()
                };
                Debug.Assert(status.Status.Endpoints.Inbound != null);
                if (!status.Status.Endpoints.Inbound.TryGetValue(resource.EndpointName, out var entry))
                {
                    entry = new DeviceStatusInboundEndpointSchemaMapValue();
                    status.Status.Endpoints.Inbound.Add(resource.EndpointName, entry);
                }
                entry.Error = new ConfigError
                {
                    Code = code,
                    Message = error
                };
            }

            /// <summary>
            /// Add error to asset resource
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            private void Add(AssetResource resource, string code, string error)
            {
                var status = GetAssetStatusResource(resource);
                if (status.Status.Config == null)
                {
                    status.Status.Config = new ConfigStatus
                    {
                        Error = new ConfigError
                        {
                            Code = code,
                            Message = error
                        }
                    };
                }
            }

            /// <summary>
            /// Add error to the dataset resource
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            private void Add(DataSetResource resource, string code, string error)
            {
                var status = GetAssetStatusResource(resource);
                status.Status.Datasets ??= new List<AssetDatasetEventStreamStatus>();
                Debug.Assert(status.Status.Datasets != null);
                status.Status.Datasets.Add(new AssetDatasetEventStreamStatus
                {
                    Name = resource.DataSet.Name,
                    MessageSchemaReference = null,
                    Error = new ConfigError
                    {
                        Code = code,
                        Message = error
                    }
                });
            }

            /// <summary>
            /// Add error to the event status based on event resource
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            private void Add(EventResource resource, string code, string error)
            {
                var status = GetAssetStatusResource(resource);
                status.Status.Events ??= new List<AssetDatasetEventStreamStatus>();
                Debug.Assert(status.Status.Events != null);
                status.Status.Events.Add(new AssetDatasetEventStreamStatus
                {
                    Name = resource.Event.Name,
                    MessageSchemaReference = null,
                    Error = new ConfigError
                    {
                        Code = code,
                        Message = error
                    }
                });
            }

            /// <summary>
            /// Add error to the stream status based on stream resource
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            private void Add(StreamResource resource, string code, string error)
            {
                var status = GetAssetStatusResource(resource);
                status.Status.Streams ??= new List<AssetDatasetEventStreamStatus>();
                Debug.Assert(status.Status.Streams != null);
                status.Status.Streams.Add(new AssetDatasetEventStreamStatus
                {
                    Name = resource.Stream.Name,
                    MessageSchemaReference = null,
                    Error = new ConfigError
                    {
                        Code = code,
                        Message = error
                    }
                });
            }

            /// <summary>
            /// Add error to the management group status based on management group resource
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            private void Add(ManagementGroupResource resource, string code, string error)
            {
                if (resource.ManagementGroup.Actions == null ||
                    resource.ManagementGroup.Actions.Count == 0)
                {
                    return;
                }
                var status = GetAssetStatusResource(resource);
                status.Status.ManagementGroups ??= new List<AssetManagementGroupStatusSchemaElement>();
                Debug.Assert(status.Status.ManagementGroups != null);
                status.Status.ManagementGroups.Add(new AssetManagementGroupStatusSchemaElement
                {
                    Name = resource.ManagementGroup.Name,
                    Actions = resource.ManagementGroup.Actions.ConvertAll(a =>
                        new AssetManagementGroupActionStatusSchemaElement
                        {
                            Name = a.Name,
                            Error = new ConfigError
                            {
                                Code = code,
                                Message = error
                            }
                        }
                    )
                });
            }

            /// <summary>
            /// Add error to the management action status based on management action resource
            /// </summary>
            /// <param name="resource"></param>
            /// <param name="code"></param>
            /// <param name="error"></param>
            private void Add(ManagementActionResource resource, string code, string error)
            {
                var status = GetAssetStatusResource(resource);
                status.Status.ManagementGroups ??= new List<AssetManagementGroupStatusSchemaElement>();
                Debug.Assert(status.Status.ManagementGroups != null);
                status.Status.ManagementGroups.Add(new AssetManagementGroupStatusSchemaElement
                {
                    Name = resource.ManagementGroup.Name,
                    Actions = new List<AssetManagementGroupActionStatusSchemaElement>
                    {
                        new AssetManagementGroupActionStatusSchemaElement
                        {
                            Name = resource.Action.Name,
                            Error = new ConfigError
                            {
                                Code = code,
                                Message = error
                            }
                        }
                    }
                });
            }

            private DeviceStatusResource GetDeviceStatusResource(DeviceResource device)
            {
                if (!_devices.TryGetValue(device.DeviceName, out var deviceStatus))
                {
                    deviceStatus = new DeviceStatusResource(device);
                    _devices.Add(device.DeviceName, deviceStatus);
                }
                return deviceStatus;
            }

            private AssetStatusResource GetAssetStatusResource(AssetResource device)
            {
                if (!_assets.TryGetValue(device.AssetName, out var assetStatus))
                {
                    assetStatus = new AssetStatusResource(device);
                    _assets.Add(device.AssetName, assetStatus);
                }
                return assetStatus;
            }
            internal record class AssetStatusResource(AssetResource resource)
                : AssetResource(resource.AssetName, resource.Asset)
            {
                public AssetStatus Status { get; } = new();
            }
            internal record class DeviceStatusResource(DeviceResource resource)
                : DeviceResource(resource.DeviceName, resource.Device)
            {
                public DeviceStatus Status { get; } = new();
            }

            private readonly Dictionary<string, AssetStatusResource> _assets = new();
            private readonly Dictionary<string, DeviceStatusResource> _devices = new();
            private readonly AssetDeviceIntegration _outer;
        }

        private const string kNotSupportedErrorCode = "500.0";
        private const string kJsonSerializationErrorCode = "500.1";
        private const string kDeviceNotFoundErrorCode = "500.2";
        private const string kBrowsePathInvalidCode = "500.3";
        private const string kTooManyDestinationsError = "500.4";
        private const string kAuthenticationValueMissing = "500.5";

        private readonly ConcurrentDictionary<string, AssetResource> _assets = new();
        private readonly ConcurrentDictionary<string, DeviceResource> _devices = new();
        private readonly Channel<(string, Resource)> _changeFeed;
        private readonly IAioAdrClient _client;
        private readonly IPublishedNodesServices _publishedNodes;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processor;
        private bool _isDisposed;
    }
}
