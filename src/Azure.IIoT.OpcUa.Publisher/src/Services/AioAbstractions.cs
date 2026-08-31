// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.Iot.Operations.Connector;
    using Azure.Iot.Operations.Connector.Files;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using Azure.Iot.Operations.Services.SchemaRegistry.SchemaRegistry;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Azure IoT Operations ADR client abstraction.
    /// </summary>
    public interface IAioAdrClient : IAsyncDisposable
    {
        /// <summary>
        /// Called when a device changed.
        /// </summary>
        event EventHandler<DeviceChangedEventArgs> OnDeviceChanged;

        /// <summary>
        /// Called when an asset changed.
        /// </summary>
        event EventHandler<AssetChangedEventArgs> OnAssetChanged;

        /// <summary>
        /// Start monitoring assets.
        /// </summary>
        ValueTask StartMonitoringAssetsAsync(string deviceName, string inboundEndpointName,
            CancellationToken ct = default);

        /// <summary>
        /// Stop monitoring assets.
        /// </summary>
        ValueTask StopMonitoringAssetsAsync(string deviceName, string inboundEndpointName,
            CancellationToken ct = default);

        /// <summary>
        /// Get endpoint credentials.
        /// </summary>
        EndpointCredentials GetEndpointCredentials(string deviceName, string inboundEndpointName,
            InboundEndpointSchemaMapValue settings);

        /// <summary>
        /// Update asset status.
        /// </summary>
        ValueTask<AssetStatus> UpdateAssetStatusAsync(string deviceName,
            string inboundEndpointName, string assetName, AssetStatus status,
            TimeSpan? commandTimeout = null, CancellationToken ct = default);

        /// <summary>
        /// Update device status.
        /// </summary>
        ValueTask<DeviceStatus> UpdateDeviceStatusAsync(string deviceName,
            string inboundEndpointName, DeviceStatus status,
            TimeSpan? commandTimeout = null, CancellationToken ct = default);

        /// <summary>
        /// Report discovered asset.
        /// </summary>
        ValueTask<DiscoveredAssetResponseSchema> ReportDiscoveredAssetAsync(
            string deviceName, string inboundEndpointName, string assetName,
            DiscoveredAsset asset, TimeSpan? commandTimeout = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Report discovered device.
        /// </summary>
        ValueTask<DiscoveredDeviceResponseSchema> ReportDiscoveredDeviceAsync(
            string deviceName, DiscoveredDevice device, string inboundEndpointType,
            TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Azure IoT Operations schema callbacks.
    /// </summary>
    public interface IAioSrCallbacks
    {
        /// <summary>
        /// Called when a schema is registered.
        /// </summary>
        ValueTask OnSchemaRegisteredAsync(IEventSchema schema, Schema registration,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Azure IoT Operations schema registry abstraction.
    /// </summary>
    public interface IAioSrClient : ISchemaRegistry
    {
        /// <summary>
        /// Register callbacks.
        /// </summary>
        IDisposable Register(IAioSrCallbacks callbacks);
    }
}
