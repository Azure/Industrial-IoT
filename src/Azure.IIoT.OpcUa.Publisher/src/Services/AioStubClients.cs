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
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Temporary AIO ADR stub until IoTHubby grows AIO/ADR coverage.
    /// </summary>
    public sealed class AioAdrStubClient : IAioAdrClient
    {
        /// <inheritdoc/>
        public event EventHandler<DeviceChangedEventArgs>? OnDeviceChanged;

        /// <inheritdoc/>
        public event EventHandler<AssetChangedEventArgs>? OnAssetChanged;

        /// <inheritdoc/>
        public ValueTask StartMonitoringAssetsAsync(string deviceName,
            string inboundEndpointName, CancellationToken ct = default)
        {
            _ = deviceName;
            _ = inboundEndpointName;
            _ = ct;
            // TODO(Phase 6): replace with IoTHubby/AIO ADR implementation.
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask StopMonitoringAssetsAsync(string deviceName,
            string inboundEndpointName, CancellationToken ct = default)
        {
            _ = deviceName;
            _ = inboundEndpointName;
            _ = ct;
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public EndpointCredentials GetEndpointCredentials(string deviceName,
            string inboundEndpointName, InboundEndpointSchemaMapValue settings)
        {
            _ = deviceName;
            _ = inboundEndpointName;
            _ = settings;
            return new EndpointCredentials();
        }

        /// <inheritdoc/>
        public ValueTask<AssetStatus> UpdateAssetStatusAsync(string deviceName,
            string inboundEndpointName, string assetName, AssetStatus status,
            TimeSpan? commandTimeout = null, CancellationToken ct = default)
        {
            _ = deviceName;
            _ = inboundEndpointName;
            _ = assetName;
            _ = commandTimeout;
            _ = ct;
            return ValueTask.FromResult(status);
        }

        /// <inheritdoc/>
        public ValueTask<DeviceStatus> UpdateDeviceStatusAsync(string deviceName,
            string inboundEndpointName, DeviceStatus status,
            TimeSpan? commandTimeout = null, CancellationToken ct = default)
        {
            _ = deviceName;
            _ = inboundEndpointName;
            _ = commandTimeout;
            _ = ct;
            return ValueTask.FromResult(status);
        }

        /// <inheritdoc/>
        public ValueTask<DiscoveredAssetResponseSchema> ReportDiscoveredAssetAsync(
            string deviceName, string inboundEndpointName, string assetName,
            DiscoveredAsset asset, TimeSpan? commandTimeout = null,
            CancellationToken cancellationToken = default)
        {
            _ = deviceName;
            _ = inboundEndpointName;
            _ = assetName;
            _ = asset;
            _ = commandTimeout;
            _ = cancellationToken;
            throw new NotSupportedException("AIO ADR discovery is not implemented by IoTHubby.");
        }

        /// <inheritdoc/>
        public ValueTask<DiscoveredDeviceResponseSchema> ReportDiscoveredDeviceAsync(
            string deviceName, DiscoveredDevice device, string inboundEndpointType,
            TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            _ = deviceName;
            _ = device;
            _ = inboundEndpointType;
            _ = commandTimeout;
            _ = cancellationToken;
            throw new NotSupportedException("AIO ADR discovery is not implemented by IoTHubby.");
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            OnDeviceChanged = null;
            OnAssetChanged = null;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Temporary AIO schema registry stub until IoTHubby grows AIO/ADR coverage.
    /// </summary>
    public sealed class AioSrStubClient : IAioSrClient
    {
        /// <inheritdoc/>
        public IDisposable Register(IAioSrCallbacks callbacks)
        {
            _ = callbacks;
            // TODO(Phase 6): replace with IoTHubby/AIO schema registry implementation.
            return new Registration();
        }

        /// <inheritdoc/>
        public ValueTask<string> RegisterAsync(IEventSchema schema,
            CancellationToken ct = default)
        {
            _ = ct;
            return ValueTask.FromResult(schema.Id ?? schema.Name);
        }

        private sealed class Registration : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
