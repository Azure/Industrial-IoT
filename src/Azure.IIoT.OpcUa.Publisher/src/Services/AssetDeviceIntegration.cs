// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Discovery;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.Iot.Operations.Connector;
    using Azure.Iot.Operations.Connector.Files;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using Azure.Iot.Operations.Services.SchemaRegistry.SchemaRegistry;
    using Furly.Azure.IoT.Operations.Services;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Nito.AsyncEx;
    using Opc.Ua;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Asset and device configuration integration with Azure iot operations. Converts asset
    /// and device notifications into published nodes representation and signals configuration
    /// status errors back.
    /// </summary>
    public sealed partial class AssetDeviceIntegration : IAioSrCallbacks, IAsyncDisposable, IDisposable
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
        /// Create Akri and asset and device registry integration service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="schemaRegistry"></param>
        /// <param name="publishedNodes"></param>
        /// <param name="configurationServices"></param>
        /// <param name="connections"></param>
        /// <param name="discovery"></param>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public AssetDeviceIntegration(IAioAdrClient client, IAioSrClient schemaRegistry,
            IPublishedNodesServices publishedNodes, IConfigurationServices configurationServices,
            IConnectionServices<ConnectionModel> connections, IDiscoveryServices discovery,
            IJsonSerializer serializer, IOptions<PublisherOptions> options,
            ILogger<AssetDeviceIntegration> logger)
        {
            _client = client;
            _publishedNodes = publishedNodes;
            _configurationServices = configurationServices;
            _connections = connections;
            _discovery = discovery;
            _serializer = serializer;
            _options = options;
            _logger = logger;
            _cts = new CancellationTokenSource();
            _client.OnDeviceChanged += OnDeviceChanged;
            _client.OnAssetChanged += OnAssetChanged;
            _schemaRegistry = schemaRegistry;
            _srevents = schemaRegistry.Register(this);
            _changeFeed = Channel.CreateUnbounded<(string, Resource)>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _processor = Task.Factory.StartNew(() => RunAsync(_cts.Token), _cts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            _trigger = new AsyncManualResetEvent();
            _timer = new Timer(_ => _trigger.Set());
            _assetDiscovery = Task.Factory.StartNew(() => RunAssetDiscoveryAsync(_cts.Token), _cts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            _deviceDiscovery = Task.Factory.StartNew(() => RunDeviceDiscoveryAsync(_cts.Token), _cts.Token,
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
                _timer.Dispose();
                try
                {
                    await _assetDiscovery.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.FailedToCloseDiscoveryRunner(ex);
                }
                try
                {
                    await _processor.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.FailedToCloseConversionProcessor(ex);
                }
                try
                {
                    await _deviceDiscovery.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.FailedToCloseDiscoveryRunner(ex);
                }
            }
            finally
            {
                _srevents.Dispose();
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask OnSchemaRegisteredAsync(Furly.Extensions.Messaging.IEventSchema schema,
            Schema registration, CancellationToken ct)
        {
            if (registration.Name == null ||
                registration.Namespace == null ||
                registration.Version == null)
            {
                _logger.SchemaRegistrationInvalidValues();
                return;
            }

            if (schema.Id == null)
            {
                _logger.CannotRegisterSchemaWithoutIdentifier();
                return;
            }

            var pos = schema.Id.IndexOf('|', StringComparison.Ordinal);
            if (pos < 3)
            {
                _logger.MalformedSchemaId(schema.Id);
                return;
            }
            var assetName = schema.Id.Substring(0, pos);
            var resourceName = schema.Id.Substring(pos + 1);
            _logger.RegisteringSchema(registration.Name, registration.Version, resourceName,
                assetName, registration.Namespace);
            var reference = new MessageSchemaReference
            {
                SchemaName = registration.Name,
                SchemaRegistryNamespace = registration.Namespace,
                SchemaVersion = registration.Version
            };
            var assetResource = _assets.Values.FirstOrDefault(a => a.AssetName == assetName);
            if (assetResource == null)
            {
                _logger.NoAssetFoundForSchema(assetName, schema.Name);
                return;
            }
            var deviceName = assetResource.Asset.DeviceRef.DeviceName;
            var endpoint = assetResource.Asset.DeviceRef.EndpointName;
            Debug.Assert(deviceName != null);
            Debug.Assert(endpoint != null);

            List<AssetDatasetEventStreamStatus>? dataSetStatus = null;
            List<AssetDatasetEventStreamStatus>? eventStatus = null;
            var dataSet = assetResource.Asset.Datasets?.Find(d => d.Name == resourceName);
            if (dataSet != null)
            {
                dataSetStatus = [new AssetDatasetEventStreamStatus
                {
                    Name = dataSet.Name,
                    MessageSchemaReference = reference
                }];
            }
            else
            {
                var @event = assetResource.Asset.Events?.Find(e => e.Name == resourceName);
                if (@event != null)
                {
                    eventStatus = [new AssetDatasetEventStreamStatus
                    {
                        Name = @event.Name,
                        MessageSchemaReference = reference
                    }];
                }
            }
            if (eventStatus == null && dataSetStatus == null)
            {
                _logger.NoResourceFoundForSchema(schema.Name);
                return;
            }
            await _client.UpdateAssetStatusAsync(deviceName, endpoint, assetName, new AssetStatus
            {
                Datasets = dataSetStatus,
                Events = eventStatus
            }, ct: ct).ConfigureAwait(false);

            _logger.RegisteredSchema(registration.Name, registration.Version, resourceName,
                assetName, registration.Namespace);
        }

        /// <summary>
        /// Asset change handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal void OnAssetChanged(object? sender, AssetChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.DeviceName) ||
                string.IsNullOrEmpty(e.InboundEndpointName) ||
                string.IsNullOrEmpty(e.AssetName))
            {
                _logger.InvalidAssetChangeEventReceived(e.ChangeType, e.DeviceName,
                    e.InboundEndpointName, e.AssetName);
                return;
            }
            switch (e.ChangeType)
            {
                case ChangeType.Created:
                    OnAssetCreated(e.DeviceName, e.InboundEndpointName, e.AssetName, e.Asset!);
                    break;
                case ChangeType.Updated:
                    OnAssetUpdated(e.DeviceName, e.InboundEndpointName, e.AssetName, e.Asset!);
                    break;
                case ChangeType.Deleted:
                    OnAssetDeleted(e.DeviceName, e.InboundEndpointName, e.AssetName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e.ChangeType,
                        "Unknown asset change type");
            }
        }

        /// <summary>
        /// Device change handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal void OnDeviceChanged(object? sender, DeviceChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.DeviceName) ||
                string.IsNullOrEmpty(e.InboundEndpointName))
            {
                _logger.InvalidDeviceChangeEventReceived(e.ChangeType, e.DeviceName,
                    e.InboundEndpointName);
                return;
            }
            switch (e.ChangeType)
            {
                case ChangeType.Created:
                    OnDeviceCreated(e.DeviceName, e.InboundEndpointName, e.Device!);
                    break;
                case ChangeType.Updated:
                    OnDeviceUpdated(e.DeviceName, e.InboundEndpointName, e.Device!);
                    break;
                case ChangeType.Deleted:
                    OnDeviceDeleted(e.DeviceName, e.InboundEndpointName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e.ChangeType,
                        "Unknown device change type");
            }
        }

        /// <summary>
        /// Handle device created
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="inboundEndpointName"></param>
        /// <param name="device"></param>
        internal void OnDeviceCreated(string deviceName, string inboundEndpointName,
            Device device)
        {
            var name = CreateDeviceKey(deviceName, inboundEndpointName);
            var deviceResource = new DeviceResource(deviceName, device);
            var cur = _devices.AddOrUpdate(name, deviceResource,
                (_, cur) => device.Version == cur.Device.Version ? cur : deviceResource);
            if (!ReferenceEquals(cur, deviceResource))
            {
                // Update has same version as what we already have
                return;
            }
            _logger.DeviceAdded(deviceName, inboundEndpointName);
            Interlocked.Increment(ref _lastDeviceListVersion);
            var success = _changeFeed.Writer.TryWrite((name, deviceResource));
            ObjectDisposedException.ThrowIf(!success,
                $"Failed to process creation of device {name}");
        }

        /// <summary>
        /// Handle device updated
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="inboundEndpointName"></param>
        /// <param name="device"></param>
        internal void OnDeviceUpdated(string deviceName, string inboundEndpointName,
            Device device)
        {
            var name = CreateDeviceKey(deviceName, inboundEndpointName);
            var deviceResource = new DeviceResource(deviceName, device);
            var cur = _devices.AddOrUpdate(name, deviceResource,
                (_, cur) => device.Version == cur.Device.Version ? cur : deviceResource);
            if (!ReferenceEquals(cur, deviceResource))
            {
                // Update has same version as what we already have
                return;
            }
            _logger.DeviceUpdated(deviceName, inboundEndpointName);
            Interlocked.Increment(ref _lastDeviceListVersion);
            var success = _changeFeed.Writer.TryWrite((name, deviceResource));
            ObjectDisposedException.ThrowIf(!success,
                $"Failed to process update of device {name}");
        }

        /// <summary>
        /// Handle device deletion
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="inboundEndpointName"></param>
        internal void OnDeviceDeleted(String deviceName, String inboundEndpointName)
        {
            var name = CreateDeviceKey(deviceName, inboundEndpointName);
            if (!_devices.TryRemove(name, out var deviceResource))
            {
                _logger.ResourceDeletionNotFound(name);
                return;
            }
            _logger.DeviceRemoved(deviceName, inboundEndpointName);
            Interlocked.Increment(ref _lastDeviceListVersion);
            var success = _changeFeed.Writer.TryWrite((name, deviceResource));
            ObjectDisposedException.ThrowIf(!success,
                $"Failed to publish deletion of {name}.");
        }

        /// <summary>
        /// Handle asset created
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="inboundEndpointName"></param>
        /// <param name="assetName"></param>
        /// <param name="asset"></param>
        internal void OnAssetCreated(string deviceName, string inboundEndpointName,
            string assetName, Asset asset)
        {
            var name = CreateAssetKey(deviceName, inboundEndpointName, assetName);
            var assetResource = new AssetResource(assetName, asset);
            var cur = _assets.AddOrUpdate(name, assetResource,
                (_, cur) => asset.Version == cur.Asset.Version ? cur : assetResource);
            if (!ReferenceEquals(cur, assetResource))
            {
                // Update has same version as what we already have
                return;
            }
            _logger.AssetAdded(assetName, deviceName, inboundEndpointName);
            var success = _changeFeed.Writer.TryWrite((name, assetResource));
            ObjectDisposedException.ThrowIf(!success,
                $"Failed to publish creation of asset {name}.");
        }

        /// <summary>
        /// Handle asset updated
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="inboundEndpointName"></param>
        /// <param name="assetName"></param>
        /// <param name="asset"></param>
        internal void OnAssetUpdated(string deviceName, string inboundEndpointName,
            string assetName, Asset asset)
        {
            var name = CreateAssetKey(deviceName, inboundEndpointName, assetName);
            var assetResource = new AssetResource(assetName, asset);
            var cur = _assets.AddOrUpdate(name, assetResource,
                (_, cur) => asset.Version == cur.Asset.Version ? cur : assetResource);
            if (!ReferenceEquals(cur, assetResource))
            {
                // Update has same version as what we already have
                return;
            }
            _logger.AssetUpdated(assetName, deviceName, inboundEndpointName);
            var success = _changeFeed.Writer.TryWrite((name, assetResource));
            ObjectDisposedException.ThrowIf(!success,
                $"Failed to publish update of asset {name}.");
        }

        /// <summary>
        /// Handle asset deletion
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="inboundEndpointName"></param>
        /// <param name="assetName"></param>
        internal void OnAssetDeleted(string deviceName, string inboundEndpointName,
            string assetName)
        {
            var name = CreateAssetKey(deviceName, inboundEndpointName, assetName);
            if (!_assets.TryRemove(name, out var asseteResource))
            {
                _logger.ResourceDeletionNotFound(name);
                return;
            }
            _logger.AssetRemoved(assetName, deviceName, inboundEndpointName);
            var success = _changeFeed.Writer.TryWrite((name, asseteResource));
            ObjectDisposedException.ThrowIf(!success,
                $"Failed to publish deletion of asset {name}.");
        }

        /// <summary>
        /// Monitors the change events produced as result of the notifications and then
        /// processes the current state.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task RunAsync(CancellationToken ct)
        {
            try
            {
                // Read changes in batches
                while (!ct.IsCancellationRequested)
                {
                    var updatesReceived = false;
                    var deviceAddedOrUpdated = false;
                    while (_changeFeed.Reader.TryRead(out var change))
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
                                        }
                                        else
                                        {
                                            await _client.StartMonitoringAssetsAsync(device.DeviceName,
                                                endpoint, ct).ConfigureAwait(false);
                                            deviceAddedOrUpdated = true;
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
                                _logger.RemovingAssetWithoutDevice(name, key);
                                _assets.TryRemove(name, out var _);
                                await _client.StopMonitoringAssetsAsync(
                                    asset.Asset.DeviceRef.DeviceName,
                                    asset.Asset.DeviceRef.EndpointName, ct).ConfigureAwait(false);
                            }
                        }

                        // Convert assets to published nodes entries and update configuration
                        var assets = _assets.Values.ToList();
                        if (assets.Count > 0)
                        {
                            var errors = new ValidationErrors(this);
                            var devices = _devices.Values.ToList();

                            _logger.ConvertingAssetsOnDevices(assets.Count, devices.Count);
                            var entries = await ToPublishedNodesAsync(devices, assets, errors,
                                ct).ConfigureAwait(false);

                            // Report all errors
                            await errors.ReportAsync(ct).ConfigureAwait(false);

                            // Apply configuration
                            await _publishedNodes.SetConfiguredEndpointsAsync(entries,
                                ct).ConfigureAwait(false);
                            if (_logger.IsDebugLogConfigurationEnabled())
                            {
                                _logger.NewConfigurationApplied(
                                    _serializer.SerializeToString(entries, SerializeOption.Indented));
                            }
                            _logger.AssetsAndDevicesUpdated(assets.Count, devices.Count);
                        }
                    }
                    if (deviceAddedOrUpdated)
                    {
                        _logger.DevicesUpdatedStartingDiscovery();
                        // TODO Make period configurable
                        _timer.Change(TimeSpan.Zero, kDefaultDeviceDiscoveryRefresh);
                    }
                    await _changeFeed.Reader.WaitToReadAsync(ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                _logger.UnexpectedErrorProcessingChanges(ex);
                throw;
            }
        }

        /// <summary>
        /// Discover assets
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="endpointConfiguration"></param>
        /// <param name="errors"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async ValueTask RunDiscoveryUsingTypesAsync(DeviceEndpointResource resource,
            DeviceEndpointConfiguration endpointConfiguration, ValidationErrors errors,
            CancellationToken ct)
        {
            if (resource.Device.Endpoints == null ||
                !TryGetInboundEndpoint(resource.Device.Endpoints, resource.EndpointName, out var endpoint))
            {
                errors.OnError(resource, kDeviceNotFoundErrorCode, "Endpoint was not found");
                return; // throw
            }

            Debug.Assert(endpointConfiguration.AssetTypes != null);
            // endpoint
            var assetEndpoint = new PublishedNodesEntryModel
            {
                EndpointUrl = GetEndpointUrl(endpoint.Address),
                EndpointSecurityMode = endpointConfiguration.EndpointSecurityMode,
                EndpointSecurityPolicy = endpointConfiguration.EndpointSecurityPolicy,
                DumpConnectionDiagnostics = endpointConfiguration.DumpConnectionDiagnostics,
                DisableSubscriptionTransfer = endpointConfiguration.DisableSubscriptionTransfer,
                UseReverseConnect = endpointConfiguration.UseReverseConnect
            };
            var credentials = _client.GetEndpointCredentials(resource.DeviceName,
                resource.EndpointName, endpoint);
            assetEndpoint = AddEndpointCredentials(assetEndpoint, credentials, errors,
                resource);

            // Find all assets that comply with the provided types.
            var assetEntries = new List<PublishedNodesEntryModel>();
            foreach (var type in endpointConfiguration.AssetTypes.Distinct())
            {
                var template = assetEndpoint with
                {
                    // Expand the type - TODO, we could also pass all types here at once
                    OpcNodes = [new OpcNodeModel { Id = type }]
                };
                await foreach (var found in _configurationServices.ExpandAsync(template,
                    new PublishedNodeExpansionModel
                    {
                        CreateSingleWriter = false, // Writer per dataset
                        ExcludeRootIfInstanceNode = false,
                        DiscardErrors = true,
                        IncludeMethods = true,
                        FlattenTypeInstance = false,
                        NoSubTypesOfTypeNodes = false
                    }, ct).ConfigureAwait(false))
                {
                    if (found.ErrorInfo != null)
                    {
                        // Add to cumulative log
                        errors.OnError(resource, kDiscoveryError, AsString(found.ErrorInfo));
                        continue;
                    }
                    if (found.Result?.OpcNodes == null ||
                        found.Result.OpcNodes.Count == 0 ||
                        found.Result.DataSetWriterGroup == null ||
                        found.Result.WriterGroupRootNodeId == null ||
                        found.Result.WriterGroupType == null)
                    {
                        if (_logger.IsDebugLogConfigurationEnabled())
                        {
                            _logger.DroppingResultWithoutRequiredInformation(
                                _serializer.SerializeToString(found.Result));
                        }
                        else
                        {
                            _logger.DroppingResultWithoutRequiredInformation(
                                found.Result?.DataSetName);
                        }
                        continue;
                    }
                    assetEntries.Add(found.Result);
                }
            }

            // Convert assets to dAsset and report them as found. We group the assets per name
            // and asset type ref. We resolve the duplicate names later from the hashset
            var uniqueAssetNames = new HashSet<string>();
            foreach (var (assetName, assetId, assetTypeRef, opcNodes) in assetEntries
                .GroupBy(e => (
                    AssetName: e.DataSetWriterGroup!,
                    AssetId: e.WriterGroupRootNodeId!,
                    AssetTypeRef: e.WriterGroupType!
                ))
                .Select(group => (
                    group.Key.AssetName,
                    group.Key.AssetId,
                    group.Key.AssetTypeRef,
                    group.ToList())))
            {
                var uniqueId = "-" + (resource.DeviceName + assetId).ToSha1Hash();
                var assetResourceName = MakeValidArmResourceName(assetName, uniqueId).TrimEnd('-')
                    + uniqueId;
                var uniqueAssetName = assetResourceName;
                // Ensure unique asset names
                for (var i = 1; !uniqueAssetNames.Add(uniqueAssetName); i++)
                {
                    uniqueAssetName = assetResourceName + i;
                }
                // ensure no duplicate datasets and datapoints (names) are added into the asset
                var distinctEntries = opcNodes
                    .GroupBy(entry => entry.DataSetName)
                    .SelectMany(group => group.Select((d, i) => d with
                    {
                        DataSetName = i == 0 ? d.DataSetName : d.DataSetName + "." + i,
                        OpcNodes = d.OpcNodes!
                            .GroupBy(n => n.DisplayName)
                            .SelectMany(group => group.Select((n, i) => n with
                            {
                                DisplayName = i == 0 ? n.DisplayName : n.DisplayName + "." + i
                            }))
                            .ToList()
                    }))
                    .ToList();

                var distinctDatasets = distinctEntries
                    .Select(entry => entry with
                    {
                        OpcNodes = entry.OpcNodes!
                            .Where(n => n.AttributeId != NodeAttribute.EventNotifier &&
                                n.MethodMetadata == null)
                            .ToList(),
                    })
                    .Where(d => d.OpcNodes!.Count > 0)
                    .ToList();
                var distinctEvents = distinctEntries
                    .Select(entry => entry with
                    {
                        OpcNodes = entry.OpcNodes!
                            .Where(n => n.AttributeId == NodeAttribute.EventNotifier &&
                                n.MethodMetadata == null)
                            .ToList(),
                    })
                    .SelectMany(d => d.OpcNodes!.Select(n => d with { OpcNodes = [n] }))
                    .ToList();
                var distinctManagementGroups = distinctEntries
                    .Select(entry => entry with
                    {
                        OpcNodes = entry.OpcNodes!
                            .Where(n => n.MethodMetadata != null)
                            .ToList(),
                    })
                    .Where(d => d.OpcNodes!.Count > 0)
                    .ToList();
                var dAsset = new DiscoveredAsset
                {
                    DeviceRef = new AssetDeviceRef
                    {
                        DeviceName = resource.DeviceName,
                        EndpointName = resource.EndpointName
                    },
                    Attributes = new Dictionary<string, string>
                    {
                        [kAssetIdAttribute] = assetId,
                        [kAssetNameAttribute] = assetName
                    },
                    AssetName = uniqueAssetName,
                    // ExternalAssetId = assetId,
                    // DisplayName = assetName,
                    Model = assetName,
                    AssetTypeRefs = [assetTypeRef],
                    Datasets = distinctDatasets.ConvertAll(d => new DiscoveredAssetDataset
                    {
                        Name = GetAssetResourceName(d.DataSetName),
                        TypeRef = d.DataSetType,
                        DataSource = d.DataSetRootNodeId,
                        DataSetConfiguration = null,
                        DataPoints = d.OpcNodes!.Select(n => new DiscoveredAssetDatasetDataPoint
                        {
                            Name = n.DisplayName, // Name of property in message/schema
                            DataSource = n.Id!,
                            DataPointConfiguration = null,
                            TypeRef = n.TypeDefinitionId
                        }).ToList(),
                        Destinations =
                        [
                            new DatasetDestination
                            {
                                Target = DatasetTarget.Mqtt,
                                Configuration = new DestinationConfiguration
                                {
                                    Qos = _options.Value.DefaultQualityOfService
                                        == Furly.Extensions.Messaging.QoS.AtMostOnce ? QoS.Qos0 : QoS.Qos1,
                                    Topic = CreateTopic(resource.DeviceName, d.DataSetWriterGroup, d.DataSetName),
                                    Retain = _options.Value.DefaultMessageRetention
                                        == true ? Retain.Keep : Retain.Never,
                                    Ttl = (ulong?)_options.Value.DefaultMessageTimeToLive?.TotalSeconds
                                }
                            }
                        ]
                    }),
                    Events = distinctEvents.ConvertAll(d => new DetectedAssetEventSchemaElement
                    {
                        Name = GetAssetResourceName(d.DataSetName, d.OpcNodes![0].DisplayName),
                        TypeRef = d.OpcNodes![0].TypeDefinitionId,
                        EventNotifier = d.OpcNodes![0].Id!,
                        EventConfiguration = _serializer.SerializeToString(new EventConfiguration
                        {
                            DataSource = d.DataSetRootNodeId,
                            SourceName = d.DataSetName,
                            EventName = d.OpcNodes![0].DisplayName
                        }),
                        Destinations =
                        [
                            new EventStreamDestination
                            {
                                Target = EventStreamTarget.Mqtt,
                                Configuration = new DestinationConfiguration
                                {
                                    Qos = _options.Value.DefaultQualityOfService
                                        == Furly.Extensions.Messaging.QoS.AtMostOnce ? QoS.Qos0 : QoS.Qos1,
                                    Topic = CreateTopic(resource.DeviceName, d.DataSetWriterGroup,
                                        d.DataSetName, d.OpcNodes![0].DisplayName),
                                    Retain = _options.Value.DefaultMessageRetention
                                        == true ? Retain.Keep : Retain.Never,
                                    Ttl = (ulong?)_options.Value.DefaultMessageTimeToLive?.TotalSeconds
                                }
                            }
                        ]
                    }),
                    ManagementGroups = distinctManagementGroups.ConvertAll(d => new DiscoveredAssetManagementGroup
                    {
                        Name = GetAssetResourceName(d.DataSetName),
                        TypeRef = d.DataSetType,
                        DefaultTimeOutInSeconds = null,
                        DefaultTopic = null,
                        ManagementGroupConfiguration = _serializer.SerializeToString(new ManagementGroupConfiguration
                        {
                            DataSource = d.DataSetRootNodeId
                        }),
                        // TODO: Could have a object id here or in action as "dataSource"
                        Actions = d.OpcNodes!.Select(n => new DiscoveredAssetManagementGroupAction
                        {
                            Name = n.DisplayName!, // Name of command in schema
                            ActionType = AssetManagementGroupActionType.Call,
                            TargetUri = n.Id!, // Method id of the instance declaration
                            TypeRef = n.TypeDefinitionId, // Method type ref on the object type ref
                            TimeOutInSeconds = null,
                            Topic = CreateTopic(resource.DeviceName, d.DataSetWriterGroup, d.DataSetName, n.DisplayName),
                            ActionConfiguration = ConvertActionConfiguration(n.MethodMetadata!)
                        }).ToList()
                    }),
                    Streams = null
                };

                // TODO: Add attributes and other information from properties

                if (_logger.IsDebugLogConfigurationEnabled())
                {
                    _logger.ReportingNewDiscoveredAsset(uniqueAssetName, assetId, assetTypeRef,
                        JsonSerializer.Serialize(dAsset, kDebugSerializerOptions));
                }
                await _client.ReportDiscoveredAssetAsync(resource.DeviceName, resource.EndpointName,
                    uniqueAssetName, dAsset, cancellationToken: ct).ConfigureAwait(false);
            }
            static string GetAssetResourceName(string? resourceName, string? innerName = null)
            {
                innerName = string.IsNullOrEmpty(innerName) ? null : "." + innerName;
                resourceName = string.IsNullOrWhiteSpace(resourceName)
                    ? "Default" : MakeValidName(resourceName, innerName);
                return innerName != null ? resourceName + innerName : resourceName;
            }
        }

        /// <summary>
        /// Run endpoint discovery for the given endpoint uri.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="endpointUri"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask RunEndpointDiscoveryAsync(DeviceEndpointResource resource,
            Uri endpointUri, CancellationToken ct)
        {
            GetEndpointTypeAndVersion(out var endpointType, out var endpointTypeVersion);
            var dCurrentDevice = new DiscoveredDevice
            {
                ExternalDeviceId = resource.Device.ExternalDeviceId,
                Model = resource.Device.Model,
                Manufacturer = resource.Device.Manufacturer,
                OperatingSystem = resource.Device.OperatingSystem,
                OperatingSystemVersion = resource.Device.OperatingSystemVersion,
                Attributes = resource.Device.Attributes
            };

            // First run endpoint discovery to find endpoints on the current device
            var endpoints = await _discovery.FindEndpointsAsync(
                endpointUri, findServersOnNetwork: false, ct: ct).ConfigureAwait(false);

            // Now report all endpoints on the network that are not part of current device
            var alreadyFound = endpoints
                .Select(e => e.Description?.Server?.ApplicationUri)
                .Where(uri => uri != null)
                .Distinct()
                .ToHashSet();
            var servers = await _discovery.FindEndpointsAsync(endpointUri,
                findServersOnNetwork: true, ct: ct).ConfigureAwait(false);
            var currentDeviceIsDiscoveryServer = false;
            foreach (var server in servers
                .Where(ep => ep.Description?.Server?.ApplicationUri != null &&
                    !alreadyFound.Contains(ep.Description.Server.ApplicationUri))
                .GroupBy(ep => ep.Description.Server.ApplicationUri))
            {
                var serverEndpoints = server.ToList();
                Debug.Assert(serverEndpoints.Count > 0);
                currentDeviceIsDiscoveryServer = true;
                var applicationDescription = serverEndpoints[0].Description.Server;
                var serverDeviceId = "-" + server.Key.ToSha1Hash();
                var deviceName = MakeValidArmResourceName(
                    applicationDescription.ApplicationName.ToString(), serverDeviceId)
                    .TrimEnd('-') + serverDeviceId;
                var dServerDevice = new DiscoveredDevice
                {
                    ExternalDeviceId = serverDeviceId,
                    Model = applicationDescription.ProductUri
                };
                try
                {
                    await ReportDiscoveredDeviceAsync(deviceName, dServerDevice, serverEndpoints,
                        endpointType, endpointTypeVersion, ct: ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.FailedToReportDiscoveredServer(ex, deviceName);
                }
            }
            // Now update our device
            dCurrentDevice.Attributes ??= new Dictionary<string, string>();
            dCurrentDevice.Attributes.AddOrUpdate("LDS", currentDeviceIsDiscoveryServer.ToString());
            await ReportDiscoveredDeviceAsync(resource.DeviceName, dCurrentDevice, endpoints,
                endpointType, endpointTypeVersion, resource, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get endpoint type and version for new endpoints
        /// </summary>
        /// <param name="endpointType"></param>
        /// <param name="endpointTypeVersion"></param>
        private void GetEndpointTypeAndVersion(out string endpointType, out string? endpointTypeVersion)
        {
            var type = _options.Value.AioDiscoveredDeviceEndpointType;
            endpointTypeVersion = _options.Value.AioDiscoveredDeviceEndpointTypeVersion;
            if (type == null)
            {
                var releaseVersion = GetType().Assembly.GetReleaseVersion();
                endpointType = "Microsoft.OpcPublisher";
                endpointTypeVersion = $"{releaseVersion.Major}.{releaseVersion.Minor}";
            }
            else
            {
                endpointType = type;
            }
        }

        /// <summary>
        /// Report a new discovered device with the given endpoints
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="dDevice"></param>
        /// <param name="endpoints"></param>
        /// <param name="endpointType"></param>
        /// <param name="endpointTypeVersion"></param>
        /// <param name="resource"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ReportDiscoveredDeviceAsync(string deviceName, DiscoveredDevice dDevice,
            IEnumerable<DiscoveredEndpointModel> endpoints, string endpointType, string? endpointTypeVersion,
            DeviceEndpointResource? resource = null, CancellationToken ct = default)
        {
            var newEndpoints = new Dictionary<string, DiscoveredDeviceInboundEndpoint>(
                StringComparer.OrdinalIgnoreCase);
            dDevice.Attributes ??= new Dictionary<string, string>();
            var newEndpointCount = 0;
            foreach (var ep in endpoints)
            {
                var desc = ep.Description;

                dDevice.Attributes.AddOrUpdate(nameof(desc.Server.ApplicationType),
                    desc.Server.ApplicationType.ToString());
                if (desc.Server.ApplicationName?.Text != null)
                {
                    dDevice.Attributes.AddOrUpdate(nameof(desc.Server.ApplicationName),
                        desc.Server.ApplicationName.Text);
                }
                if (desc.Server.ApplicationUri != null)
                {
                    dDevice.Attributes.AddOrUpdate(nameof(desc.Server.ApplicationUri),
                        desc.Server.ApplicationUri);
                }
                if (desc.Server.ProductUri != null)
                {
                    dDevice.Attributes.AddOrUpdate(nameof(desc.Server.ProductUri),
                        desc.Server.ProductUri);
                }
                if (desc.Server.DiscoveryProfileUri != null)
                {
                    dDevice.Attributes.AddOrUpdate(nameof(desc.Server.DiscoveryProfileUri),
                        desc.Server.DiscoveryProfileUri);
                }
                if (desc.Server.DiscoveryUrls?.Count > 0)
                {
                    dDevice.Attributes.AddOrUpdate(nameof(desc.Server.DiscoveryUrls),
                        string.Join(',', desc.Server.DiscoveryUrls));
                }
                if (desc.Server.GatewayServerUri != null)
                {
                    dDevice.Attributes.AddOrUpdate(nameof(desc.Server.GatewayServerUri),
                        desc.Server.GatewayServerUri);
                }
                if (ep.Capabilities != null)
                {
                    foreach (var cap in ep.Capabilities)
                    {
                        dDevice.Attributes.AddOrUpdate(cap.ToUpperInvariant(), "True");
                    }
                }
                var name = desc.SecurityMode.ToString();
                if (desc.SecurityMode != MessageSecurityMode.None)
                {
                    name = $"l{desc.SecurityLevel}.{name}";
                    if (!Uri.TryCreate(desc.SecurityPolicyUri, UriKind.Absolute,
                        out var securityProfileUri) ||
                        securityProfileUri.Fragment.Length == 0 ||
                        securityProfileUri.Fragment[0] != '#')
                    {
                        continue;
                    }
                    name += $".{securityProfileUri.Fragment.AsSpan(1)}";
                }
                if (desc.TransportProfileUri != Profiles.UaTcpTransport)
                {
                    if (!Uri.TryCreate(desc.TransportProfileUri, UriKind.Absolute,
                        out var transportProfileUri))
                    {
                        continue;
                    }
                    var pathParts = transportProfileUri.PathAndQuery.Split('/');
                    if (pathParts.Length == 0)
                    {
                        continue;
                    }
                    name += $".{pathParts[pathParts.Length - 1]}";
                }
                // Add desc.ServerCertificate somewhere
                var epModel = new DeviceEndpointConfiguration
                {
                    Source = "Discovery",
                    EndpointSecurityMode = desc.SecurityMode.ToServiceType(),
                    EndpointSecurityPolicy = desc.SecurityPolicyUri
                };
                var supportedAuthenticationMethods = GetSupportedAuthenticationMethods(desc);
                var uniqueName = MakeValidArmResourceName(name);
                if (newEndpoints.ContainsKey(uniqueName))
                {
                    continue;
                }
                var existing = resource?.Device.Endpoints?.Inbound?.FirstOrDefault(e =>
                    e.Key.Equals(uniqueName, StringComparison.OrdinalIgnoreCase));
                if (existing.HasValue && existing.Value.Key != null)
                {
                    // Merge discovered into existing endpoint information
                    if (!string.IsNullOrEmpty(existing.Value.Value.AdditionalConfiguration))
                    {
                        // Deserialize existing configuration
                        var errors = new ValidationErrors(this);
                        var epModelExisting = Deserialize(existing.Value.Value.AdditionalConfiguration,
                            () => new DeviceEndpointConfiguration(), errors, resource!);
                        if (epModelExisting != null)
                        {
                            epModelExisting.EndpointSecurityMode ??= epModel.EndpointSecurityMode;
                            epModelExisting.EndpointSecurityPolicy ??= epModel.EndpointSecurityPolicy;
                            epModelExisting.Source = "Discovery";
                            epModel = epModelExisting;
                        }
                    }
                    uniqueName = existing.Value.Key; // Keep casing
#if SKIP_ADDRESS_MISMATCH
                    if (existing.Value.Value.Address != ep.AccessibleEndpointUrl)
                    {
                        continue; // Ignore if address does not match
                    }
#endif
#if SKIP_EXISTING_ENDPOINTS
                    continue;
#endif
                }
                var additionalConfiguration = _serializer.SerializeToString(epModel);
                if (additionalConfiguration.Length > 512)
                {
                    _logger.EndpointConfigurationTooLong(uniqueName, deviceName,
                        additionalConfiguration.Length);
                }
                newEndpoints.Add(uniqueName, new DiscoveredDeviceInboundEndpoint
                {
                    Address = SetEndpointUrl(ep.AccessibleEndpointUrl),
                    EndpointType = endpointType,
                    Version = endpointTypeVersion,
                    SupportedAuthenticationMethods = supportedAuthenticationMethods,
                    AdditionalConfiguration = additionalConfiguration
                });
                newEndpointCount++;
            }

            if (newEndpointCount == 0)
            {
                _logger.NoEndpointsFound(deviceName);
                return;
            }

            dDevice.Endpoints = new DiscoveredDeviceEndpoints { Inbound = newEndpoints };
            if (_logger.IsDebugLogConfigurationEnabled())
            {
                _logger.ReportingNewDiscoveredDevice(deviceName, endpointType,
                    JsonSerializer.Serialize(dDevice, kDebugSerializerOptions));
            }
            await _client.ReportDiscoveredDeviceAsync(deviceName, dDevice,
                endpointType, cancellationToken: ct).ConfigureAwait(false);

            static List<string>? GetSupportedAuthenticationMethods(EndpointDescription desc)
            {
                if (desc.UserIdentityTokens == null || desc.UserIdentityTokens.Count == 0)
                {
                    return null;
                }
                var methods = new HashSet<string>();
                foreach (var token in desc.UserIdentityTokens)
                {
                    switch (token.TokenType)
                    {
                        case UserTokenType.Anonymous:
                            methods.Add(nameof(Method.Anonymous));
                            break;
                        case UserTokenType.UserName:
                            methods.Add(nameof(Method.UsernamePassword));
                            break;
                        case UserTokenType.Certificate:
                            methods.Add(nameof(Method.Certificate));
                            break;
                        case UserTokenType.IssuedToken:
                            // Not supported
                            break;
                    }
                }
                return methods.ToList();
            }
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
                // Find the device and endpoint referenced by this asset
                var deviceRef = asset.Asset.DeviceRef;
                DeviceResource? deviceResource = null;
                InboundEndpointSchemaMapValue? endpoint = null;
                foreach (var device in deviceLookup[deviceRef.DeviceName])
                {
                    deviceResource = device;
                    if (string.Equals(device.DeviceName, deviceRef.DeviceName, StringComparison.OrdinalIgnoreCase)
                        && TryGetInboundEndpoint(device?.Device?.Endpoints, deviceRef.EndpointName, out endpoint))
                    {
                        break;
                    }
                }
                if (deviceResource == null)
                {
                    errors.OnError(asset, kDeviceNotFoundErrorCode, "Device referenced by asset was not found");
                    continue;
                }
                if (endpoint == null)
                {
                    errors.OnError(asset, kDeviceNotFoundErrorCode, "Endpoint referenced by asset was not found");
                    continue;
                }
                var deviceEndpointResource = new DeviceEndpointResource(deviceResource.DeviceName,
                    deviceResource.Device, deviceRef.EndpointName);
                var endpointConfiguration = Deserialize(endpoint.AdditionalConfiguration,
                    () => new DeviceEndpointConfiguration(), errors, deviceEndpointResource);
                if (endpointConfiguration == null)
                {
                    continue;
                }

                // Asset identification
                var assetEndpoint = new PublishedNodesEntryModel
                {
                    EndpointUrl = GetEndpointUrl(endpoint.Address),
                    EndpointSecurityMode = endpointConfiguration.EndpointSecurityMode,
                    EndpointSecurityPolicy = endpointConfiguration.EndpointSecurityPolicy,
                    DumpConnectionDiagnostics = endpointConfiguration.DumpConnectionDiagnostics,
                    DisableSubscriptionTransfer = endpointConfiguration.DisableSubscriptionTransfer,
                    UseReverseConnect = endpointConfiguration.UseReverseConnect,

                    // We map asset name to writer group as it contains writers for each of its
                    // entities. This will be split per destination, but this is ok as the name
                    // is retained and the Id property receives the unique group name.
                    DataSetWriterGroup = asset.AssetName,
                    DataSetSubject = asset.Asset.Uuid,
                    WriterGroupType = asset.Asset.AssetTypeRefs?.Count == 1 ?
                        asset.Asset.AssetTypeRefs[0] : null,
                    WriterGroupRootNodeId = asset.Asset.Attributes == null ?
                        null : asset.Asset.Attributes.TryGetValue(kAssetIdAttribute, out var rootNodeId) ?
                        rootNodeId?.ToString() : null,
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
                    var dataSetAdditionalConfiguration = Deserialize(asset.Asset.DefaultDatasetsConfiguration,
                        () => new PublishedNodesEntryModel { EndpointUrl = string.Empty },
                        errors, asset);
                    if (dataSetAdditionalConfiguration != null)
                    {
                        dataSetAdditionalConfiguration = WithEndpoint(dataSetAdditionalConfiguration, assetEndpoint);
                        foreach (var dataset in asset.Asset.Datasets)
                        {
                            var datasetResource = new DataSetResource(asset.AssetName, asset.Asset, dataset);
                            await AddEntryForDataSetAsync(dataSetAdditionalConfiguration, datasetResource,
                                entries, errors, ct).ConfigureAwait(false);
                        }
                    }
                }
                if (asset.Asset.Events != null)
                {
                    var eventAdditionalConfiguration = Deserialize(asset.Asset.DefaultEventsConfiguration,
                        () => new PublishedNodesEntryModel { EndpointUrl = string.Empty },
                        errors, asset);
                    if (eventAdditionalConfiguration != null)
                    {
                        eventAdditionalConfiguration = WithEndpoint(eventAdditionalConfiguration, assetEndpoint);
                        foreach (var @event in asset.Asset.Events)
                        {
                            var eventResource = new EventResource(asset.AssetName, asset.Asset, @event);
                            await AddEntryForEventAsync(eventAdditionalConfiguration, eventResource,
                                entries, errors, ct).ConfigureAwait(false);
                        }
                    }
                }

                if (asset.Asset.ManagementGroups != null)
                {
                    var managementGroupConfiguration = Deserialize(asset.Asset.DefaultManagementGroupsConfiguration,
                        () => new PublishedNodesEntryModel { EndpointUrl = string.Empty },
                        errors, asset);
                    if (managementGroupConfiguration != null)
                    {
                        managementGroupConfiguration = WithEndpoint(managementGroupConfiguration, assetEndpoint);
                        foreach (var managementGroup in asset.Asset.ManagementGroups)
                        {
                            var managementGroupResource = new ManagementGroupResource(asset.AssetName, asset.Asset,
                                managementGroup);
                            await AddEntryForManagementGroupAsync(managementGroupConfiguration, managementGroupResource,
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
            }
            return entries;
        }

        /// <summary>
        /// Create extension fields for attributes and properties that do not exist in our models
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        internal static Dictionary<string, VariantValue>? CollectAssetAndDeviceProperties(
            AssetResource asset, DeviceResource device)
        {
            var fields = new Dictionary<string, VariantValue>();
            static void Add(Dictionary<string, VariantValue> fields, string key, VariantValue? value)
            {
                if (!VariantValue.IsNullOrNullValue(value))
                {
                    fields.AddOrUpdate(key, value);
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
        private static PublishedNodesEntryModel AddEndpointCredentials(
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
            var additionalConfiguration = Deserialize(resource.DataSet.DatasetConfiguration,
                () => new DataSetConfiguration(), errors, resource);
            if (additionalConfiguration == null)
            {
                return ValueTask.CompletedTask;
            }

            // TODO: Resolve typeref to ensure it exists

            var nodes = new List<OpcNodeModel>();
            if (resource.DataSet.DataPoints != null)
            {
                // Map datapoints to OPC nodes
                foreach (var datapoint in resource.DataSet.DataPoints)
                {
                    var nodeFromAdditionalConfiguration = Deserialize(
                        datapoint.DataPointConfiguration, () => new DataSetDataPointConfiguration(),
                        errors, resource);
                    if (nodeFromAdditionalConfiguration == null)
                    {
                        continue;
                    }
                    nodes.Add(nodeFromAdditionalConfiguration with
                    {
                        Id = datapoint.DataSource,
                        DisplayName = datapoint.Name,
                        DataSetFieldId = datapoint.Name,
                        FetchDisplayName = false,
                        TypeDefinitionId = datapoint.TypeRef
                    });
                }
            }
            var entry = CreateEntryForAssetResource(template, additionalConfiguration, nodes);
            entry = AddDestination(entry, resource.Asset.DefaultDatasetsDestinations,
                resource.DataSet.Destinations, errors, resource);
            entries.Add(entry with
            {
                // Dataset writer id maps to name
                DataSetWriterId = resource.DataSet.Name,
                // Dataset name as well (-> source of the dataset)
                DataSetName = resource.DataSet.Name,
                // Root node id is the data source of the dataset
                DataSetRootNodeId = resource.DataSet.DataSource,
                // Type is the dataset typeref
                DataSetType = resource.DataSet.TypeRef,
                // Source is the data set source uri
                DataSetSourceUri = CreateSourceUri(resource.Asset, resource.DataSet.DataSource),
                // Subject is the asset uuid and dataset name
                DataSetSubject = CreateSubject(resource.Asset, resource.DataSet.Name),

                // Dataset configuration overrides
                DataSetKeyFrameCount = entry.DataSetKeyFrameCount ?? 10,
                SendKeepAliveDataSetMessages = entry.SendKeepAliveDataSetMessages ?? true,
                SendKeepAliveAsKeyFrameMessages = entry.SendKeepAliveAsKeyFrameMessages ?? true,
                MaxKeepAliveCount = entry.MaxKeepAliveCount ?? 30
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

            // Map event configuration on top of entry
            var additionalConfiguration = Deserialize(resource.Event.EventConfiguration,
                () => new EventConfiguration(), errors, resource);
            if (additionalConfiguration == null)
            {
                return ValueTask.CompletedTask;
            }

            var eventFilter = additionalConfiguration.EventFilter ?? new EventFilterModel();
            if (!string.IsNullOrEmpty(resource.Event.TypeRef))
            {
                eventFilter.TypeDefinitionId = resource.Event.TypeRef;
            }

            // Create the single event node here
            var node = new OpcNodeModel
            {
                Id = resource.Event.EventNotifier,
                DisplayName = resource.Event.Name,
                DataSetFieldId = resource.Event.Name,
                TypeDefinitionId = resource.Event.TypeRef,
                FetchDisplayName = false,
                EventFilter = eventFilter,
                ConditionHandling = additionalConfiguration.ConditionHandling,
                SkipFirst = additionalConfiguration.SkipFirst,
                QueueSize = additionalConfiguration.QueueSize,
                DiscardNew = additionalConfiguration.DiscardNew
            };

            // Map datapoints a filter statement
            if (resource.Event.DataPoints != null)
            {
                var selectClause = new List<EventDataPointConfiguration>();
                foreach (var datapoint in resource.Event.DataPoints)
                {
                    var operand = Deserialize(datapoint.DataPointConfiguration,
                        () => new EventDataPointConfiguration(), errors, resource);
                    if (operand == null)
                    {
                        continue;
                    }
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
                var newEventFilter = node.EventFilter with { SelectClauses = selectClause };
            }
            var entry = CreateEntryForAssetResource(template, additionalConfiguration, [node]);
            entry = AddDestination(entry, resource.Asset.DefaultEventsDestinations,
                resource.Event.Destinations, errors, resource);
            entries.Add(entry with
            {
                // Dataset writer id maps to name
                DataSetWriterId = resource.Event.Name,
                // Dataset name is the source of the event (!= event resource name)
                DataSetName = additionalConfiguration.SourceName ?? resource.Event.Name,
                // Root node id is the data source of the event
                DataSetRootNodeId = additionalConfiguration.DataSource,
                // Type ref is the event type
                DataSetType = resource.Event.TypeRef,
                // Source is the device uuid and event notifier (emitting the event)
                DataSetSourceUri = CreateSourceUri(resource.Asset, resource.Event.EventNotifier),
                // Subject is the asset uuid and dataset name
                DataSetSubject = CreateSubject(resource.Asset, resource.Event.Name,
                    additionalConfiguration.DataSource),

                // Event configuration overrides
                MaxKeepAliveCount = entry.MaxKeepAliveCount ?? 30
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
        private ValueTask AddEntryForManagementGroupAsync(PublishedNodesEntryModel template,
            ManagementGroupResource resource, List<PublishedNodesEntryModel> entries,
            ValidationErrors errors, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Map event configuration on top of entry
            var additionalConfiguration = Deserialize(
                resource.ManagementGroup.ManagementGroupConfiguration,
                () => new ManagementGroupConfiguration(), errors, resource);
            if (additionalConfiguration == null)
            {
                return ValueTask.CompletedTask;
            }

            if (resource.ManagementGroup.Actions == null ||
                resource.ManagementGroup.Actions.Count == 0)
            {
                return ValueTask.CompletedTask;
            }

            // map actions to nodes
            foreach (var action in resource.ManagementGroup.Actions
                .GroupBy(a => a.Topic ?? resource.ManagementGroup.DefaultTopic))
            {
                var nodes = new List<OpcNodeModel>();
                foreach (var a in action)
                {
                    var actionResource = new ManagementActionResource(resource.AssetName,
                        resource.Asset, resource.ManagementGroup, a);
                    nodes.Add(new OpcNodeModel
                    {
                        Id = a.TargetUri,
                        DisplayName = a.Name,
                        DataSetFieldId = a.Name,
                        FetchDisplayName = false,
                        MethodMetadata = ConvertActionConfiguration(a.ActionConfiguration, errors,
                            actionResource)
                    });
                }
                var entry = CreateEntryForAssetResource(template, additionalConfiguration, nodes);
                entries.Add(entry with
                {
                    // Writer id maps to the name of the management group
                    DataSetWriterId = resource.ManagementGroup.Name,
                    // The name as well (target of the actions)
                    DataSetName = resource.ManagementGroup.Name,
                    // Root node id is the data source of the management group
                    DataSetRootNodeId = additionalConfiguration.DataSource,
                    // Type ref is the type of the object that has the methods
                    DataSetType = resource.ManagementGroup.TypeRef,
                    // Source is the data set source uri
                    DataSetSourceUri = CreateSourceUri(resource.Asset, additionalConfiguration.DataSource),
                    // Subject is the asset uuid and management group name
                    DataSetSubject = CreateSubject(resource.Asset, resource.ManagementGroup.Name),

                    // Add topic
                    QueueName = action.Key
                });
            }
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Create entry for a management group in the asset
        /// </summary>
        /// <param name="endpointTemplate"></param>
        /// <param name="entityTemplate"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static PublishedNodesEntryModel CreateEntryForAssetResource(
            PublishedNodesEntryModel endpointTemplate, ManagementGroupConfiguration entityTemplate,
            List<OpcNodeModel> nodes)
        {
            return endpointTemplate with
            {
                //
                // Set defaults for iot operations, there will never be any different configuration
                // allowed in the resource of AIO
                //
                BatchSize = 0,
                BatchTriggerInterval = 0,
                BatchTriggerIntervalTimespan = null,
                MessagingMode = MessagingMode.SingleDataSet,
                DataSetFetchDisplayNames = false,
                MessageEncoding = entityTemplate.MessageEncoding == MessageEncoding.Avro
                    ? MessageEncoding.Avro : MessageEncoding.Json,

                OpcNodes = nodes,

                DataSetClassId =
                    entityTemplate.DataSetClassId,
                Priority =
                    entityTemplate.Priority,
            };
        }

        /// <summary>
        /// Create entry for a dataset in the asset
        /// </summary>
        /// <param name="endpointTemplate"></param>
        /// <param name="additionalConfiguration"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static PublishedNodesEntryModel CreateEntryForAssetResource(
            PublishedNodesEntryModel endpointTemplate, DataSetConfiguration additionalConfiguration,
            List<OpcNodeModel> nodes)
        {
            return endpointTemplate with
            {
                //
                // Set defaults for iot operations, there will never be any different configuration
                // allowed in the resource of AIO
                //
                BatchSize = 0,
                BatchTriggerInterval = 0,
                BatchTriggerIntervalTimespan = null,
                MessagingMode = MessagingMode.SingleDataSet,
                DataSetFetchDisplayNames = false,
                MessageEncoding = additionalConfiguration.MessageEncoding == MessageEncoding.Avro
                    ? MessageEncoding.Avro : MessageEncoding.Json,

                OpcNodes = nodes,

                // Dataset configuration
                DataSetPublishingInterval =
                    additionalConfiguration.PublishingInterval,
                DataSetSamplingInterval =
                    additionalConfiguration.SamplingInterval,
                DataSetClassId =
                    additionalConfiguration.DataSetClassId,
                DataSetKeyFrameCount =
                    additionalConfiguration.KeyFrameCount,
                DataSetWriterWatchdogBehavior =
                    additionalConfiguration.DataSetWriterWatchdogBehavior,
                SendKeepAliveDataSetMessages =
                    additionalConfiguration.SendKeepAliveDataSetMessages,
                SendKeepAliveAsKeyFrameMessages =
                    additionalConfiguration.SendKeepAliveAsKeyFrameMessages,
                MaxKeepAliveCount =
                    additionalConfiguration.MaxKeepAliveCount,
                RepublishAfterTransfer =
                    additionalConfiguration.RepublishAfterTransfer,
                Priority =
                    additionalConfiguration.Priority
            };
        }

        /// <summary>
        /// Create entry for an event in the asset
        /// </summary>
        /// <param name="endpointTemplate"></param>
        /// <param name="additionalConfiguration"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static PublishedNodesEntryModel CreateEntryForAssetResource(
            PublishedNodesEntryModel endpointTemplate, EventConfiguration additionalConfiguration,
            List<OpcNodeModel> nodes)
        {
            return endpointTemplate with
            {
                //
                // Set defaults for iot operations, there will never be any different configuration
                // allowed in the resource of AIO
                //
                BatchSize = 0,
                BatchTriggerInterval = 0,
                BatchTriggerIntervalTimespan = null,
                MessagingMode = MessagingMode.SingleDataSet,
                DataSetFetchDisplayNames = false,
                MessageEncoding =
                    additionalConfiguration.MessageEncoding == MessageEncoding.Avro
                        ? MessageEncoding.Avro : MessageEncoding.Json,

                OpcNodes = nodes,

                // Dataset configuration
                DataSetPublishingInterval =
                    additionalConfiguration.PublishingInterval,
                DataSetSamplingInterval =
                    additionalConfiguration.SamplingInterval,
                DataSetKeyFrameCount =
                    additionalConfiguration.KeyFrameCount,
                DataSetClassId =
                    additionalConfiguration.DataSetClassId,
                DataSetWriterWatchdogBehavior =
                    additionalConfiguration.DataSetWriterWatchdogBehavior,
                SendKeepAliveDataSetMessages =
                    additionalConfiguration.SendKeepAliveDataSetMessages,
                SendKeepAliveAsKeyFrameMessages =
                    additionalConfiguration.SendKeepAliveAsKeyFrameMessages,
                MaxKeepAliveCount =
                    additionalConfiguration.MaxKeepAliveCount,
                RepublishAfterTransfer =
                    additionalConfiguration.RepublishAfterTransfer,
                Priority =
                    additionalConfiguration.Priority
            };
        }

        /// <summary>
        /// Copy the device endpoint resource to the template and return a new entry
        /// </summary>
        /// <param name="template"></param>
        /// <param name="deviceEndpoint"></param>
        /// <private>
        /// Adds a new entry for a dataset. Will not add one if configuration parsing fails.
        /// This does not apply to opc nodes, if any fail to validate, there will still be
        /// an entry, but the status will reflect this error.
        /// </private>
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
                EncryptedAuthUsername = null,

                // Add the fixed asset information
                DataSetWriterGroup = deviceEndpoint.DataSetWriterGroup,
                WriterGroupType = deviceEndpoint.WriterGroupType,
                WriterGroupRootNodeId = deviceEndpoint.WriterGroupRootNodeId,
                WriterGroupProperties = deviceEndpoint.WriterGroupProperties
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
                // TODO: We could generate 2 or more entries each with different destination
                errors.OnError(resource, kTooManyDestinationsError,
                    "More than 1 destination is not allowed for datasets. Using Mqtt");
            }
            var destination = destinations?.FirstOrDefault();
            destination ??= defaultDestinations?
                .FirstOrDefault(d => defaultDestinations.Count == 1
                    || d.Target == DatasetTarget.Mqtt);
            if (destination == null)
            {
                return entry with { WriterGroupTransport = WriterGroupTransport.AioMqtt };
            }
            var configuration = destination.Configuration;
            switch (destination.Target)
            {
                case DatasetTarget.BrokerStateStore:
                    return entry with
                    {
                        WriterGroupTransport = WriterGroupTransport.AioDss,
                        QueueName = configuration.Key,
                        MessageTtlTimespan = configuration.Ttl == null ? null
                            : TimeSpan.FromSeconds(configuration.Ttl.Value),
                    };
                case DatasetTarget.Mqtt:
                    return entry with
                    {
                        WriterGroupTransport = WriterGroupTransport.AioMqtt,
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
                            : TimeSpan.FromSeconds(configuration.Ttl.Value),
                        MetaDataQueueName = configuration.Topic,
                        MetaDataRetention = configuration.Retain switch
                        {
                            Retain.Keep => true,
                            Retain.Never => false,
                            _ => null
                        },
                        MetaDataTtlTimespan = configuration.Ttl == null ? null
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
                return entry with { WriterGroupTransport = WriterGroupTransport.AioMqtt };
            }
            var configuration = destination.Configuration;
            switch (destination.Target)
            {
                case EventStreamTarget.Mqtt:
                    return entry with
                    {
                        WriterGroupTransport = WriterGroupTransport.AioMqtt,
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
                            : TimeSpan.FromSeconds(configuration.Ttl.Value),
                        MetaDataQueueName = configuration.Topic,
                        MetaDataRetention = configuration.Retain switch
                        {
                            Retain.Keep => true,
                            Retain.Never => false,
                            _ => null
                        },
                        MetaDataTtlTimespan = configuration.Ttl == null ? null
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
        /// Run network discovery if configured
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task RunDeviceDiscoveryAsync(CancellationToken ct)
        {
            if (_options.Value.AioNetworkDiscoveryMode == null ||
                _options.Value.AioNetworkDiscoveryMode == DiscoveryMode.Off)
            {
                _logger.NetworkDiscoveryDisabled();
                return;
            }
            var progress = new ProgressLogger(_logger);
            var request = new DiscoveryRequestModel
            {
                Discovery = _options.Value.AioNetworkDiscoveryMode.Value,
                Configuration = _options.Value.AioNetworkDiscovery,
            };
            GetEndpointTypeAndVersion(out var endpointType, out var endpointTypeVersion);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _logger.RunningNetworkDiscovery();
                    var servers = await _discovery.FindServersAsync(request, progress, ct)
                        .ConfigureAwait(false);

                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var server in servers
                        .Where(ep => ep.Description?.Server?.ApplicationUri != null)
                        .GroupBy(ep => ep.Description.Server.ApplicationUri))
                    {
                        if (!set.Add(server.Key) ||
                            _devices.Values.Any(d => d.Device.Attributes?.TryGetValue(
                            nameof(ApplicationDescription.ApplicationUri), out var uri) == true &&
                            string.Equals(uri, server.Key, StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.DeviceAlreadyPresentSkipping(server.Key);
                            continue;
                        }
                        var endpoints = server.ToList();
                        Debug.Assert(endpoints.Count > 0);
                        var applicationDescription = endpoints[0].Description.Server;
                        var serverDeviceId = "-" + server.Key.ToSha1Hash();
                        var deviceName = MakeValidArmResourceName(
                            applicationDescription.ApplicationName.ToString(), serverDeviceId)
                            .TrimEnd('-') + serverDeviceId;
                        var dServerDevice = new DiscoveredDevice
                        {
                            ExternalDeviceId = serverDeviceId,
                            Model = applicationDescription.ProductUri
                        };
                        try
                        {
                            await ReportDiscoveredDeviceAsync(deviceName, dServerDevice, endpoints,
                                endpointType, endpointTypeVersion, ct: ct).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.FailedToReportDiscoveredServer(ex, deviceName);
                        }
                    }
                    _logger.NetworkDiscoveryComplete();

                    if (!(_options.Value.AioNetworkDiscoveryInterval > TimeSpan.Zero))
                    {
                        _logger.NetworkDiscoveryConfiguredToOnlyRunOnce();
                        break;
                    }
                    await Task.Delay(_options.Value.AioNetworkDiscoveryInterval.Value, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.UnexpectedErrorDuringNetworkDiscovery(ex);
                }
            }
        }

        /// <summary>
        /// Run discovery for all known devices
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task RunAssetDiscoveryAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var lastListVersion = _lastDeviceListVersion;
                try
                {
                    _logger.RunningDiscoveryForAllDevices();
                    var errors = new ValidationErrors(this);
                    foreach (var device in _devices.Values.ToList())
                    {
                        if ((device.Device.Endpoints?.Inbound) == null)
                        {
                            continue;
                        }

                        var deviceDiscoveryComplete = false;
                        foreach (var endpoint in device.Device.Endpoints.Inbound)
                        {
                            var deviceEndpointResource = new DeviceEndpointResource(device.DeviceName,
                                device.Device, endpoint.Key);

                            var url = GetEndpointUrl(endpoint.Value.Address);
                            if (!Uri.TryCreate(url, UriKind.Absolute, out var endpointUri))
                            {
                                errors.OnError(deviceEndpointResource, kInvalidEndpointUrl,
                                    "Invalid endpoint URL: " + url);
                                continue;
                            }
                            var endpointConfiguration = Deserialize(endpoint.Value.AdditionalConfiguration,
                                () => new DeviceEndpointConfiguration(), errors, deviceEndpointResource);
                            if (endpointConfiguration == null)
                            {
                                continue;
                            }

                            // Test connection
                            var canConnectToEndpoint = await TestConnectionAsync(deviceEndpointResource,
                                endpoint.Value, endpointConfiguration, errors, ct).ConfigureAwait(false);
                            if (!canConnectToEndpoint)
                            {
                                continue;
                            }

                            if (endpointConfiguration.RunAssetDiscovery == true &&
                                endpointConfiguration.AssetTypes?.Count > 0)
                            {
                                // Run discovery
                                try
                                {
                                    await RunDiscoveryUsingTypesAsync(deviceEndpointResource,
                                        endpointConfiguration, errors, ct).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    errors.OnError(deviceEndpointResource, kDiscoveryError, ex.Message);
                                    _logger.FailedToRunDiscoveryForDevice(ex, device.DeviceName);
                                }
                            }

                            if (endpointConfiguration.Source == null &&
                                endpointConfiguration.EndpointSecurityMode == SecurityMode.None &&
                                !deviceDiscoveryComplete)
                            {
                                try
                                {
                                    await RunEndpointDiscoveryAsync(deviceEndpointResource,
                                        endpointUri, ct).ConfigureAwait(false);
                                    deviceDiscoveryComplete = true;
                                }
                                catch (Exception ex)
                                {
                                    errors.OnError(deviceEndpointResource, kDiscoveryError, ex.Message);
                                    _logger.FailedToRunEndpointDiscoveryForDevice(ex, device.DeviceName);
                                }
                            }
                        }

                        // Compare last device list version, if version changed, stop and continue on
                        // next timer fired. This will settle the changes and limit resource usage.
                        if (_lastDeviceListVersion != lastListVersion)
                        {
                            _logger.RunningDiscoveryInterrupted();
                            break;
                        }
                    }
                    await errors.ReportAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.UnexpectedErrorDuringDiscovery(ex);
                }
                if (_lastDeviceListVersion == lastListVersion)
                {
                    _logger.RunningDiscoveryCompleted();
                    _trigger.Reset();
                }
                try
                {
                    await _trigger.WaitAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.UnexpectedErrorDuringDiscovery(ex);
                }
            }
        }

        /// <summary>
        /// Test connectivity
        /// </summary>
        /// <param name="deviceEndpointResource"></param>
        /// <param name="endpoint"></param>
        /// <param name="endpointConfiguration"></param>
        /// <param name="errors"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<bool> TestConnectionAsync(DeviceEndpointResource deviceEndpointResource,
            InboundEndpointSchemaMapValue endpoint, DeviceEndpointConfiguration endpointConfiguration,
            ValidationErrors errors, CancellationToken ct)
        {
            var assetEndpoint = new PublishedNodesEntryModel
            {
                EndpointUrl = GetEndpointUrl(endpoint.Address),
                EndpointSecurityMode = endpointConfiguration.EndpointSecurityMode,
                EndpointSecurityPolicy = endpointConfiguration.EndpointSecurityPolicy,
                DumpConnectionDiagnostics = endpointConfiguration.DumpConnectionDiagnostics,
                DisableSubscriptionTransfer = endpointConfiguration.DisableSubscriptionTransfer,
                UseReverseConnect = endpointConfiguration.UseReverseConnect
            };
            var credentials = _client.GetEndpointCredentials(deviceEndpointResource.DeviceName,
                deviceEndpointResource.EndpointName, endpoint);
            assetEndpoint = AddEndpointCredentials(assetEndpoint, credentials, errors,
                deviceEndpointResource);
            var testConnection = await _connections.TestConnectionAsync(assetEndpoint.ToConnectionModel(),
                new TestConnectionRequestModel(), ct).ConfigureAwait(false);
            if (testConnection.ErrorInfo != null)
            {
                errors.OnError(deviceEndpointResource, kDiscoveryError,
                    "Failed to connect to endpoint: " + AsString(testConnection.ErrorInfo));
                _logger.FailedToConnectToDeviceEndpoint(deviceEndpointResource.DeviceName,
                    deviceEndpointResource.EndpointName, testConnection.ErrorInfo);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create a topic for the asset and dataset
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="assetName"></param>
        /// <param name="dataSetName"></param>
        /// <param name="extra"></param>
        /// <returns></returns>
        private static string CreateTopic(string? deviceName, string? assetName, string? dataSetName,
            string? extra = null)
        {
            var builder = new StringBuilder("{RootTopic}"); // /opcua/{Encoding}/{MessageType}");
            if (!string.IsNullOrEmpty(deviceName))
            {
                builder = builder.Append('/').Append(Furly.Extensions.Messaging.TopicFilter.Escape(deviceName));
            }
            if (!string.IsNullOrEmpty(assetName))
            {
                builder = builder.Append('/').Append(Furly.Extensions.Messaging.TopicFilter.Escape(assetName));
            }
            if (!string.IsNullOrEmpty(dataSetName))
            {
                foreach (var part in dataSetName.Split('.', StringSplitOptions.RemoveEmptyEntries))
                {
                    builder = builder.Append('/').Append(Furly.Extensions.Messaging.TopicFilter.Escape(part));
                }
            }
            if (!string.IsNullOrEmpty(extra))
            {
                builder = builder.Append('/').Append(Furly.Extensions.Messaging.TopicFilter.Escape(extra));
            }
            return builder.ToString();
        }

        /// <summary>
        /// Create cloud event source uri as per specification for azure iot operations
        /// Identifies the context in which the event happened.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="dataSource"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string CreateSourceUri(Asset asset, string? dataSource = null)
        {
            var key = CreateDeviceKey(asset.DeviceRef.DeviceName, asset.DeviceRef.EndpointName);
            if (!_devices.TryGetValue(key, out var device))
            {
                throw new ArgumentException("Device not found for asset", nameof(asset));
            }
            var builder = new StringBuilder("ms-aio:")
                .Append(device.Device.ExternalDeviceId)
                .Append('_')
                .Append(asset.DeviceRef.EndpointName);
            if (!string.IsNullOrEmpty(dataSource))
            {
                builder = builder.Append('/').Append(dataSource.UrlEncode());
            }
            return builder.ToString();
        }

        /// <summary>
        /// Create cloud event subject as per specification for azure iot operations
        /// Identifies what the event is about.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="dataSetName"></param>
        /// <param name="dataSubject"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string CreateSubject(Asset asset, string dataSetName, string? dataSubject = null)
        {
            var builder = new StringBuilder(asset.ExternalAssetId)
                .Append('/')
                .Append(dataSetName);
            if (!string.IsNullOrEmpty(dataSubject))
            {
                builder = builder.Append('/').Append(dataSubject.UrlEncode());
            }
            return builder.ToString();
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

        private static bool TryGetInboundEndpoint(DeviceEndpoints? endpoints,
            string endpointName, [NotNullWhen(true)] out InboundEndpointSchemaMapValue? endpoint)
        {
            endpoint = endpoints?.Inbound?.FirstOrDefault(
                ep => string.Equals(ep.Key, endpointName, StringComparison.OrdinalIgnoreCase)).Value;
            return endpoint != null;
        }

        /// <summary>
        /// Compress method metadata
        /// </summary>
        /// <param name="methodMetadata"></param>
        /// <param name="compressionLevel"></param>
        /// <returns></returns>
        internal string? ConvertActionConfiguration(MethodMetadataModel methodMetadata,
            int compressionLevel = 0)
        {
            byte[] compressed;
            using (var input = new MemoryStream(
                _serializer.SerializeToMemory(methodMetadata).ToArray()))
            using (var result = new MemoryStream())
            {
                using (var gs = new GZipStream(result, CompressionMode.Compress))
                {
                    input.CopyTo(gs);
                }
                compressed = result.ToArray();
            }
            var json = JsonSerializer.Serialize(new ActionConfiguration { CompiledMetadata = compressed });
            if (Encoding.UTF8.GetByteCount(json) > 512) // 512 is max size but we are leaving some room here
            {
                if (compressionLevel > 2)
                {
                    return null;
                }
                // We send the object id but compress or fully remove the argument information
                return ConvertActionConfiguration(methodMetadata with
                {
                    InputArguments = compressionLevel > 1 ? null : methodMetadata.InputArguments?
                        .Select(a => new MethodMetadataArgumentModel
                        {
                            Name = a.Name,
                            Type = new NodeModel { NodeId = a.Type.NodeId }
                        })
                        .ToList(),
                    OutputArguments = compressionLevel > 1 ? null : methodMetadata.OutputArguments?
                        .Select(a => new MethodMetadataArgumentModel
                        {
                            Name = a.Name,
                            Type = new NodeModel { NodeId = a.Type.NodeId }
                        })
                        .ToList()
                }, ++compressionLevel);
            }
            return json;
        }

        /// <summary>
        /// Decompress method metadata
        /// </summary>
        /// <param name="actionConfigurationJson"></param>
        /// <param name="errors"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        internal MethodMetadataModel ConvertActionConfiguration(string? actionConfigurationJson,
            ValidationErrors errors, Resource resource)
        {
            if (actionConfigurationJson == null)
            {
                return new MethodMetadataModel();
            }
            try
            {
                var actionConfiguration = Deserialize(actionConfigurationJson,
                    () => new ActionConfiguration { CompiledMetadata = [] }, errors, resource);
                if (actionConfiguration?.CompiledMetadata == null ||
                    actionConfiguration.CompiledMetadata.Length == 0)
                {
                    return new MethodMetadataModel();
                }
                byte[] decompressed;
                using (var input = new MemoryStream(actionConfiguration.CompiledMetadata))
                using (var output = new MemoryStream())
                {
                    using (var gs = new GZipStream(input, CompressionMode.Decompress))
                    {
                        gs.CopyTo(output);
                    }
                    decompressed = output.ToArray();
                }
                return _serializer.Deserialize<MethodMetadataModel>(decompressed)
                    ?? new MethodMetadataModel();
            }
            catch (Exception ex)
            {
                errors.OnError(resource, kJsonSerializationErrorCode + "."
                    + ex.HResult.ToString(CultureInfo.InvariantCulture), ex.Message);
                return new MethodMetadataModel();
            }
        }

        private static bool TryGetInboundEndpoint(DeviceStatusEndpoint? endpoints,
            string endpointName, [NotNullWhen(true)] out DeviceStatusInboundEndpointSchemaMapValue? endpoint)
        {
            endpoint = endpoints?.Inbound?.FirstOrDefault(
                ep => string.Equals(ep.Key, endpointName, StringComparison.OrdinalIgnoreCase)).Value;
            return endpoint != null;
        }

        private static string CreateDeviceKey(string deviceName, string inboundEndpointName)
            => $"{deviceName}_{inboundEndpointName}";

        private static string CreateAssetKey(string deviceName, string inboundEndpointName,
            string assetName) => $"{deviceName}_{inboundEndpointName}_{assetName}";

        private static string MakeValidArmResourceName(string input, string? postFix = null)
        {
            var leaveRoom = postFix == null ? 0 : Encoding.UTF8.GetByteCount(postFix);
            Debug.Assert(leaveRoom < 63);
            // Convert to lowercase
            string sanitized = input.ToLowerInvariant();
            // Ascii escape invalid characters with '-'
            sanitized = EscapeInvalid().Replace(sanitized, "-");
            // Ensure it starts and ends with an alphanumeric character
            sanitized = EnsureAlphaStart().Replace(sanitized, "");
            // Truncate to characters if necessary (63 max)
            var maxChars = 63 - leaveRoom;
            if (sanitized.Length > maxChars)
            {
                sanitized = sanitized.Substring(0, maxChars);
            }
            return sanitized;
        }

        private static string MakeValidName(string input, string? postFix = null)
        {
            var leaveRoom = postFix == null ? 0 : Encoding.UTF8.GetByteCount(postFix);
            var length = Encoding.UTF8.GetByteCount(input);
            // Truncate to characters if necessary (128 max)
            var maxChars = leaveRoom > 128 ? 0 : 128 - leaveRoom;
            if (length > maxChars)
            {
                return input.ToSha1Hash();
            }
            return input;
        }

        /// <summary>
        /// Create string from error
        /// </summary>
        /// <param name="errorInfo"></param>
        /// <returns></returns>
        private static string AsString(ServiceResultModel errorInfo)
        {
            var str = new StringBuilder();
            Append(str, errorInfo);
            return str.ToString();

            static StringBuilder Append(StringBuilder builder, ServiceResultModel error)
            {
                builder = builder
                    .Append(error.SymbolicId ??
                        error.StatusCode.ToString(CultureInfo.InvariantCulture))
                    .Append(':')
                    .Append(error.ErrorMessage);
                if (error.Inner != null)
                {
                    builder = builder.AppendLine("=>");
                    builder = Append(builder, error.Inner);
                }
                return builder;
            }
        }

        [GeneratedRegex("[^a-z0-9-]")]
        private static partial Regex EscapeInvalid();

        [GeneratedRegex("^-+|-+$")]
        private static partial Regex EnsureAlphaStart();

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
                _outer._logger.EncounteredError(code, error, resource);
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
                if (_devices.Count > 0 || _assets.Count > 0)
                {
                    _outer._logger.ReportingStatus(_assets.Count, _devices.Count);
                }

                var client = _outer._client;
                foreach (var (_, status) in _devices)
                {
                    try
                    {
                        await client.UpdateDeviceStatusAsync(status.DeviceName, status.EndpointName,
                            status.Status, ct: ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _outer._logger.FailedToUpdateDeviceStatus(ex,
                            status.DeviceName, status.EndpointName);
                    }
                }
                foreach (var (_, status) in _assets)
                {
                    try
                    {
                        await client.UpdateAssetStatusAsync(status.Asset.DeviceRef.DeviceName,
                            status.Asset.DeviceRef.EndpointName,
                            status.AssetName, status.Status, ct: ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _outer._logger.FailedToUpdateAssetStatus(ex, status.AssetName,
                            status.Asset.DeviceRef.DeviceName, status.Asset.DeviceRef.EndpointName);
                    }
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
                var ep = resource.Device.Endpoints?.Inbound?.FirstOrDefault().Key;
                if (string.IsNullOrEmpty(ep))
                {
                    // Cannot find an endpoint but we need it to attach status
                    return;
                }
                var status = GetDeviceEndpointStatusResource(new DeviceEndpointResource(
                    resource.DeviceName, resource.Device, ep));
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
                var status = GetDeviceEndpointStatusResource(resource);
                status.Status.Endpoints ??= new DeviceStatusEndpoint
                {
                    Inbound = new Dictionary<string, DeviceStatusInboundEndpointSchemaMapValue>()
                };
                Debug.Assert(status.Status.Endpoints.Inbound != null);
                if (!TryGetInboundEndpoint(status.Status.Endpoints, resource.EndpointName, out var entry))
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

            private DeviceEndpointStatusResource GetDeviceEndpointStatusResource(DeviceEndpointResource device)
            {
                var key = CreateDeviceKey(device.DeviceName, device.EndpointName);
                if (!_devices.TryGetValue(key, out var deviceStatus))
                {
                    deviceStatus = new DeviceEndpointStatusResource(device);
                    _devices.Add(key, deviceStatus);
                }
                return deviceStatus;
            }

            private AssetStatusResource GetAssetStatusResource(AssetResource asset)
            {
                var key = CreateAssetKey(asset.Asset.DeviceRef.DeviceName,
                    asset.Asset.DeviceRef.EndpointName, asset.AssetName);
                if (!_assets.TryGetValue(key, out var assetStatus))
                {
                    assetStatus = new AssetStatusResource(asset);
                    _assets.Add(key, assetStatus);
                }
                return assetStatus;
            }
            internal record class AssetStatusResource(AssetResource resource)
                : AssetResource(resource.AssetName, resource.Asset)
            {
                public AssetStatus Status { get; } = new();
            }
            internal record class DeviceEndpointStatusResource(DeviceEndpointResource resource)
                : DeviceEndpointResource(resource.DeviceName, resource.Device, resource.EndpointName)
            {
                public DeviceStatus Status { get; } = new();
            }

            private readonly Dictionary<string, AssetStatusResource> _assets = new();
            private readonly Dictionary<string, DeviceEndpointStatusResource> _devices = new();
            private readonly AssetDeviceIntegration _outer;
        }

        private const string kNotSupportedErrorCode = "500.0";
        private const string kJsonSerializationErrorCode = "500.1";
        private const string kDeviceNotFoundErrorCode = "500.2";
        private const string kTooManyDestinationsError = "500.4";
        private const string kAuthenticationValueMissing = "500.5";
        private const string kDiscoveryError = "500.6";
        private const string kInvalidEndpointUrl = "500.7";

        private static readonly TimeSpan kDefaultDeviceDiscoveryRefresh = TimeSpan.FromHours(6);
        private static readonly JsonSerializerOptions kDebugSerializerOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const string kAssetIdAttribute = "AssetId";
        private const string kAssetNameAttribute = "AssetName";

        private readonly ConcurrentDictionary<string, AssetResource> _assets = new();
        private readonly ConcurrentDictionary<string, DeviceResource> _devices = new();
        private readonly IAioSrClient _schemaRegistry;
        private readonly IDisposable _srevents;
        private readonly Channel<(string, Resource)> _changeFeed;
        private readonly IConfigurationServices _configurationServices;
        private readonly IConnectionServices<ConnectionModel> _connections;
        private readonly IDiscoveryServices _discovery;
        private readonly IAioAdrClient _client;
        private readonly IPublishedNodesServices _publishedNodes;
        private readonly IJsonSerializer _serializer;
        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processor;
        private readonly Task _assetDiscovery;
        private readonly Task _deviceDiscovery;
        private readonly AsyncManualResetEvent _trigger;
        private readonly Timer _timer;
        private int _lastDeviceListVersion;
        private bool _isDisposed;

        // TODO: START: Remove once adr is fixed
        private static string SetEndpointUrl(string endpointUrl)
            => $"{endpointUrl}{kAddressSplit}{Interlocked.Increment(ref _epindex)}";
        private static int _epindex;
        private static string GetEndpointUrl(string address)
            => address.Contains(kAddressSplit, StringComparison.Ordinal) ?
                address.Split(kAddressSplit)[0] : address;

        private const string kAddressSplit = "?___";
        // TODO: END Remove when adr is fixed
    }

    /// <summary>
    /// Source-generated logging extensions for AssetDeviceIntegration
    /// </summary>
    internal static partial class AssetDeviceIntegrationLogging
    {
        private const int EventClass = 2000;

        internal static bool IsDebugLogConfigurationEnabled(this ILogger logger)
#if !DEBUG
            => logger.IsEnabled(LogLevel.Debug);
#else
            => true;
#endif

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Debug,
            Message = "Failed to close discovery runner.")]
        internal static partial void FailedToCloseDiscoveryRunner(this ILogger logger,
            Exception ex);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Debug,
            Message = "Failed to close conversion processor")]
        internal static partial void FailedToCloseConversionProcessor(this ILogger logger,
            Exception ex);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Information,
            Message = "Device {DeviceName} with endpoint {EndpointName} added.")]
        internal static partial void DeviceAdded(this ILogger logger, string deviceName,
            string endpointName);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Debug,
            Message = "Device {DeviceName} with endpoint {EndpointName} updated.")]
        internal static partial void DeviceUpdated(this ILogger logger, string deviceName,
            string endpointName);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Debug,
            Message = "Reported deletion for resource {Resource} which was not found.")]
        internal static partial void ResourceDeletionNotFound(this ILogger logger, string resource);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Debug,
            Message = "Device {DeviceName} with endpoint {EndpointName} removed.")]
        internal static partial void DeviceRemoved(this ILogger logger,
            string deviceName, string endpointName);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Information,
            Message = "Asset {AssetName} on device {DeviceName} with endpoint {EndpointName} added.")]
        internal static partial void AssetAdded(this ILogger logger, string assetName,
            string deviceName, string endpointName);

        [LoggerMessage(EventId = EventClass + 8, Level = LogLevel.Debug,
            Message = "Asset {AssetName} on device {DeviceName} with endpoint {EndpointName} updated.")]
        internal static partial void AssetUpdated(this ILogger logger, string assetName,
            string deviceName, string endpointName);

        [LoggerMessage(EventId = EventClass + 9, Level = LogLevel.Debug,
            Message = "Asset {AssetName} on device {DeviceName} with endpoint {EndpointName} removed.")]
        internal static partial void AssetRemoved(this ILogger logger, string assetName,
            string deviceName, string endpointName);

        [LoggerMessage(EventId = EventClass + 10, Level = LogLevel.Debug,
            Message = "Removing asset {Asset} without device {Device}")]
        internal static partial void RemovingAssetWithoutDevice(this ILogger logger,
            string asset, string device);

        [LoggerMessage(EventId = EventClass + 11, Level = LogLevel.Information,
            Message = "Converting {Assets} Assets on {Devices} devices...")]
        internal static partial void ConvertingAssetsOnDevices(this ILogger logger,
            int assets, int devices);

        [LoggerMessage(EventId = EventClass + 12, Level = LogLevel.Debug, SkipEnabledCheck = true,
            Message = "New configuration applied:\n{Configuration}")]
        internal static partial void NewConfigurationApplied(this ILogger logger,
            string configuration);

        [LoggerMessage(EventId = EventClass + 13, Level = LogLevel.Information,
            Message = "{Assets} Assets on {Devices} devices updated.")]
        internal static partial void AssetsAndDevicesUpdated(this ILogger logger,
            int assets, int devices);

        [LoggerMessage(EventId = EventClass + 14, Level = LogLevel.Information,
            Message = "Devices were updated, starting immediate discovery.")]
        internal static partial void DevicesUpdatedStartingDiscovery(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 15, Level = LogLevel.Warning,
            Message = "Dropping result {Result} without required information.")]
        internal static partial void DroppingResultWithoutRequiredInformation(
            this ILogger logger, string? result);

        [LoggerMessage(EventId = EventClass + 16, Level = LogLevel.Debug, SkipEnabledCheck = true,
            Message = "Reporting new discovered asset {AssetName} with id " +
            "{AssetId} and type {AssetTypeRef}:\n{Asset}")]
        internal static partial void ReportingNewDiscoveredAsset(this ILogger logger,
            string assetName, string assetId, string assetTypeRef, string asset);

        [LoggerMessage(EventId = EventClass + 17, Level = LogLevel.Error,
            Message = "Additional configuration for endpoint {EndpointName} on " +
            "device {DeviceName} is too long ({Length} > 512). Skipping.")]
        internal static partial void EndpointConfigurationTooLong(this ILogger logger,
            string endpointName, string deviceName, int length);

        [LoggerMessage(EventId = EventClass + 18, Level = LogLevel.Debug,
            Message = "No endpoints found on device {DeviceName}.")]
        internal static partial void NoEndpointsFound(this ILogger logger, string deviceName);

        [LoggerMessage(EventId = EventClass + 19, Level = LogLevel.Debug, SkipEnabledCheck = true,
            Message = "Reporting new discovered device {DeviceName} with type {EndpointType}:\n{Device}")]
        internal static partial void ReportingNewDiscoveredDevice(this ILogger logger,
            string deviceName, string endpointType, string device);

        [LoggerMessage(EventId = EventClass + 20, Level = LogLevel.Information,
            Message = "Running discovery for all devices...")]
        internal static partial void RunningDiscoveryForAllDevices(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 21, Level = LogLevel.Error,
            Message = "Failed to run discovery for device {Device}")]
        internal static partial void FailedToRunDiscoveryForDevice(this ILogger logger,
            Exception ex, string device);

        [LoggerMessage(EventId = EventClass + 22, Level = LogLevel.Error,
            Message = "Failed to run endpoint discovery for device {Device}")]
        internal static partial void FailedToRunEndpointDiscoveryForDevice(this ILogger logger,
            Exception ex, string device);

        [LoggerMessage(EventId = EventClass + 23, Level = LogLevel.Information,
            Message = "Running discovery for all devices interrupted.")]
        internal static partial void RunningDiscoveryInterrupted(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 24, Level = LogLevel.Information,
            Message = "Running discovery for all devices completed.")]
        internal static partial void RunningDiscoveryCompleted(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 25, Level = LogLevel.Information,
            Message = "Reporting status for {Assets} assets and {Devices} devices.")]
        internal static partial void ReportingStatus(this ILogger logger, int assets, int devices);

        [LoggerMessage(EventId = EventClass + 26, Level = LogLevel.Warning,
            Message = "Encountered Error {Code} {Error} for resource {Resource}.")]
        internal static partial void EncounteredError(this ILogger logger, string code, string error,
            AssetDeviceIntegration.Resource resource);

        [LoggerMessage(EventId = EventClass + 27, Level = LogLevel.Error,
            Message = "Failed to report new discovered server {Device}")]
        internal static partial void FailedToReportDiscoveredServer(this ILogger logger,
            Exception ex, string device);

        [LoggerMessage(EventId = EventClass + 28, Level = LogLevel.Error,
            Message = "Invalid device change event {ChangeType} received: {Device} {Endpoint}")]
        internal static partial void InvalidDeviceChangeEventReceived(this ILogger logger,
            ChangeType changeType, string device, string endpoint);

        [LoggerMessage(EventId = EventClass + 29, Level = LogLevel.Error,
            Message = "Invalid asset change event {ChangeType} received: {Device} {Endpoint} {Asset}")]
        internal static partial void InvalidAssetChangeEventReceived(this ILogger logger,
            ChangeType changeType, string device, string endpoint, string asset);

        [LoggerMessage(EventId = EventClass + 30, Level = LogLevel.Information,
            Message = "Testing connection with device {Device} on endpoint {Endpoint} resulted in {ErrorInfo}")]
        internal static partial void FailedToConnectToDeviceEndpoint(this ILogger logger,
            string device, string endpoint, ServiceResultModel errorInfo);

        [LoggerMessage(EventId = EventClass + 31, Level = LogLevel.Error,
            Message = "Unexpected error processing asset and device changes")]
        internal static partial void UnexpectedErrorProcessingChanges(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 32, Level = LogLevel.Error,
            Message = "Registration of schema has invalid values")]
        internal static partial void SchemaRegistrationInvalidValues(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 33, Level = LogLevel.Debug,
            Message = "Cannot register schema without identifier")]
        internal static partial void CannotRegisterSchemaWithoutIdentifier(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 34, Level = LogLevel.Debug,
            Message = "Malformed schema id {Id} cannot be used for lookup")]
        internal static partial void MalformedSchemaId(this ILogger logger, string id);

        [LoggerMessage(EventId = EventClass + 35, Level = LogLevel.Information,
            Message = "Registering schema '{Name}' with version '{Version}' for resource '{Resource}' " +
            "in asset '{Asset}' inside schema namespace '{Namespace}'")]
        internal static partial void RegisteringSchema(this ILogger logger, string name, string version,
            string resource, string asset, string @namespace);

        [LoggerMessage(EventId = EventClass + 36, Level = LogLevel.Information,
            Message = "No asset found with name {AssetName} to attach schema {Schema} to.")]
        internal static partial void NoAssetFoundForSchema(this ILogger logger, string assetName, string? schema);

        [LoggerMessage(EventId = EventClass + 37, Level = LogLevel.Information,
            Message = "No resource found to attach the schema {Schema} to.")]
        internal static partial void NoResourceFoundForSchema(this ILogger logger, string? schema);

        [LoggerMessage(EventId = EventClass + 38, Level = LogLevel.Information,
            Message = "Registered schema '{Name}' with version '{Version}' for resource '{Resource}' in asset " +
            "'{Asset}' inside schema namespace '{Namespace}'")]
        internal static partial void RegisteredSchema(this ILogger logger, string name, string version,
            string resource, string asset, string @namespace);

        [LoggerMessage(EventId = EventClass + 39, Level = LogLevel.Error,
            Message = "Unexpected error during discovery.")]
        internal static partial void UnexpectedErrorDuringDiscovery(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 40, Level = LogLevel.Error,
            Message = "Failed to update asset status for asset {Asset} with device {Device} with endpoint {Endpoint}")]
        internal static partial void FailedToUpdateAssetStatus(this ILogger logger, Exception ex,
            string asset, string device, string endpoint);

        [LoggerMessage(EventId = EventClass + 41, Level = LogLevel.Error,
            Message = "Failed to update asset status for device {Device} with endpoint {Endpoint}")]
        internal static partial void FailedToUpdateDeviceStatus(this ILogger logger, Exception ex,
            string device, string endpoint);

        [LoggerMessage(EventId = EventClass + 42, Level = LogLevel.Information,
            Message = "Running network discovery ...")]
        internal static partial void RunningNetworkDiscovery(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 43, Level = LogLevel.Information,
            Message = "Network discovery completed.")]
        internal static partial void NetworkDiscoveryComplete(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 44, Level = LogLevel.Error,
            Message = "Unexpected error during network discovery.")]
        internal static partial void UnexpectedErrorDuringNetworkDiscovery(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 45, Level = LogLevel.Information,
            Message = "Device {Device} already present - skipping.")]
        internal static partial void DeviceAlreadyPresentSkipping(this ILogger logger, string device);

        [LoggerMessage(EventId = EventClass + 46, Level = LogLevel.Information,
            Message = "Network discovery disabled.")]
        internal static partial void NetworkDiscoveryDisabled(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 47, Level = LogLevel.Information,
            Message = "Network discovery was configured to only run once - exiting.")]
        internal static partial void NetworkDiscoveryConfiguredToOnlyRunOnce(this ILogger logger);
    }
}
