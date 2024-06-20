// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Handlers
{
    using Azure.IIoT.OpcUa.Publisher.Service.Services.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Azure;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Gateway event handler.
    /// </summary>
    public sealed class RegistryLifecycleHandler : IMessageHandler
    {
        /// <inheritdoc/>
        public string MessageSchema => MessageSchemaTypes.DeviceLifecycleNotification;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="gateways"></param>
        /// <param name="publishers"></param>
        /// <param name="applications"></param>
        /// <param name="endpoints"></param>
        /// <param name="supervisors"></param>
        /// <param name="discoverers"></param>
        /// <param name="timeProvider"></param>
        public RegistryLifecycleHandler(IJsonSerializer serializer,
            ILogger<RegistryLifecycleHandler> logger,
            IGatewayRegistryListener? gateways = null,
            IPublisherRegistryListener? publishers = null,
            IApplicationRegistryListener? applications = null,
            IEndpointRegistryListener? endpoints = null,
            ISupervisorRegistryListener? supervisors = null,
            IDiscovererRegistryListener? discoverers = null,
            TimeProvider? timeProvider = null)
        {
            _serializer = serializer;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;

            _gateways = gateways;
            _publishers = publishers;
            _applications = applications;
            _endpoints = endpoints;
            _supervisors = supervisors;
            _discoverers = discoverers;
        }

        /// <inheritdoc/>
        public async ValueTask HandleAsync(string deviceId, string? moduleId, ReadOnlySequence<byte> payload,
            IReadOnlyDictionary<string, string?> properties, CancellationToken ct)
        {
            if (!properties.TryGetValue("opType", out var opType) ||
                !properties.TryGetValue("operationTimestamp", out var ts))
            {
                return;
            }
            _ = DateTime.TryParse(ts, out var timestamp);
            if (timestamp + TimeSpan.FromSeconds(10) < _timeProvider.GetUtcNow())
            {
                // Drop twin events that are too far in our past.
                _logger.LogDebug("Skipping {Event} from {DeviceId}({ModuleId}) from {Ts}.",
                    opType, deviceId, moduleId, timestamp);
                return;
            }
            var twin = Try.Op(() => _serializer.Deserialize<DeviceTwinModel>(payload));
            if (twin == null)
            {
                return;
            }
            twin.ModuleId ??= moduleId;
            twin.Id ??= deviceId;
            switch (opType)
            {
                case "createDeviceIdentity":
                case "createModuleIdentity":
                    await HandleCreateAsync(twin, timestamp).ConfigureAwait(false);
                    break;
                case "deleteDeviceIdentity":
                case "deleteModuleIdentity":
                    await HandleDeleteAsync(twin, timestamp).ConfigureAwait(false);
                    break;
            }
        }

        /// <summary>
        /// Handle deletion of module or device
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private async Task HandleDeleteAsync(DeviceTwinModel twin, DateTime timestamp)
        {
            var type = (twin.Tags?.GetValueOrDefault<string>(Constants.TwinPropertyTypeKey, null)) ??
                (twin.Tags?.GetValueOrDefault<string>(nameof(EntityRegistration.DeviceType), null));
            var ctx = new OperationContextModel
            {
                Time = timestamp
            };
            switch (type)
            {
                case Constants.EntityTypeGateway:
                    if (_gateways != null)
                    {
                        await _gateways.OnGatewayDeletedAsync(ctx, twin.Id).ConfigureAwait(false);
                    }
                    break;
                case Constants.EntityTypeEndpoint:
                    if (_endpoints != null)
                    {
                        var ev = twin.ToEndpointRegistration(true).ToServiceModel();
                        if (ev != null)
                        {
                            await _endpoints.OnEndpointDeletedAsync(ctx, twin.Id, ev).ConfigureAwait(false);
                        }
                    }
                    break;
                case Constants.EntityTypeApplication:
                    if (_applications != null)
                    {
                        var ev = twin.ToApplicationRegistration().ToServiceModel();
                        if (ev != null)
                        {
                            await _applications.OnApplicationDeletedAsync(ctx, twin.Id, ev).ConfigureAwait(false);
                        }
                    }
                    break;
                case Constants.EntityTypePublisher:
                    if (_supervisors != null)
                    {
                        await _supervisors.OnSupervisorDeletedAsync(ctx,
                        HubResource.Format(null, twin.Id, twin.ModuleId)).ConfigureAwait(false);
                    }
                    if (_publishers != null)
                    {
                        await _publishers.OnPublisherDeletedAsync(ctx,
                        HubResource.Format(null, twin.Id, twin.ModuleId)).ConfigureAwait(false);
                    }
                    if (_discoverers != null)
                    {
                        await _discoverers.OnDiscovererDeletedAsync(ctx,
                        HubResource.Format(null, twin.Id, twin.ModuleId)).ConfigureAwait(false);
                    }
                    break;
            }
        }

        /// <summary>
        /// Handle creation of module or device
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private async Task HandleCreateAsync(DeviceTwinModel twin, DateTime timestamp)
        {
            var type = (twin.Tags?.GetValueOrDefault<string>(Constants.TwinPropertyTypeKey, null))
                ?? (twin.Tags?.GetValueOrDefault<string>(nameof(EntityRegistration.DeviceType), null));
            var ctx = new OperationContextModel
            {
                Time = timestamp
            };
            switch (type)
            {
                case Constants.EntityTypeGateway:
                    if (_gateways != null)
                    {
                        var ev = twin.ToGatewayRegistration().ToServiceModel();
                        if (ev != null)
                        {
                            await _gateways.OnGatewayNewAsync(ctx, ev).ConfigureAwait(false);
                        }
                    }
                    break;
                case Constants.EntityTypeEndpoint:
                    if (_endpoints != null)
                    {
                        var ev = twin.ToEndpointRegistration(true).ToServiceModel();
                        if (ev != null)
                        {
                            await _endpoints.OnEndpointNewAsync(ctx, ev).ConfigureAwait(false);
                        }
                    }
                    break;
                case Constants.EntityTypeApplication:
                    if (_applications != null)
                    {
                        var ev = twin.ToApplicationRegistration().ToServiceModel();
                        if (ev != null)
                        {
                            await _applications.OnApplicationNewAsync(ctx, ev).ConfigureAwait(false);
                        }
                    }
                    break;
                case Constants.EntityTypePublisher:
                    if (_supervisors != null)
                    {
                        var ev = twin.ToPublisherRegistration().ToSupervisorModel();
                        if (ev != null)
                        {
                            await _supervisors.OnSupervisorNewAsync(ctx, ev).ConfigureAwait(false);
                        }
                    }
                    if (_publishers != null)
                    {
                        var ev = twin.ToPublisherRegistration().ToPublisherModel();
                        if (ev != null)
                        {
                            await _publishers.OnPublisherNewAsync(ctx, ev).ConfigureAwait(false);
                        }
                    }
                    if (_discoverers != null)
                    {
                        var ev = twin.ToPublisherRegistration().ToDiscovererModel();
                        if (ev != null)
                        {
                            await _discoverers.OnDiscovererNewAsync(ctx, ev).ConfigureAwait(false);
                        }
                    }
                    break;
            }
        }

        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private readonly IGatewayRegistryListener? _gateways;
        private readonly IPublisherRegistryListener? _publishers;
        private readonly IApplicationRegistryListener? _applications;
        private readonly IEndpointRegistryListener? _endpoints;
        private readonly ISupervisorRegistryListener? _supervisors;
        private readonly IDiscovererRegistryListener? _discoverers;
    }
}
