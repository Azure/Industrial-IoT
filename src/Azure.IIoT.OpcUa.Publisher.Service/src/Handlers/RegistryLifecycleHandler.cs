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
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using System;
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
        /// <param name="iothub"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="gateways"></param>
        /// <param name="publishers"></param>
        /// <param name="applications"></param>
        /// <param name="endpoints"></param>
        /// <param name="supervisors"></param>
        /// <param name="discoverers"></param>
        public RegistryLifecycleHandler(IIoTHubTwinServices iothub,
            IJsonSerializer serializer, ILogger<RegistryLifecycleHandler> logger,
            IGatewayRegistryListener gateways = null,
            IPublisherRegistryListener publishers = null,
            IApplicationRegistryListener applications = null,
            IEndpointRegistryListener endpoints = null,
            ISupervisorRegistryListener supervisors = null,
            IDiscovererRegistryListener discoverers = null)
        {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _gateways = gateways;
            _publishers = publishers;
            _applications = applications;
            _endpoints = endpoints;
            _supervisors = supervisors;
            _discoverers = discoverers;
        }

        /// <inheritdoc/>
        public async ValueTask HandleAsync(string deviceId, string moduleId, ReadOnlyMemory<byte> payload,
            IReadOnlyDictionary<string, string> properties, CancellationToken ct)
        {
            if (!properties.TryGetValue("opType", out var opType) ||
                !properties.TryGetValue("operationTimestamp", out var ts))
            {
                return;
            }
            _ = DateTime.TryParse(ts, out var timestamp);
            if (timestamp + TimeSpan.FromSeconds(10) < DateTime.UtcNow)
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
            var type = twin.Tags?.GetValueOrDefault<string>(OpcUa.Constants.TwinPropertyTypeKey, null);
            if (string.IsNullOrEmpty(type))
            {
                try
                {
                    twin = await _iothub.GetAsync(deviceId, moduleId, ct).ConfigureAwait(false);
                    type = twin.Tags?.GetValueOrDefault<string>(OpcUa.Constants.TwinPropertyTypeKey, null);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Failed to materialize twin from registry.");
                    return;
                }
            }
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
            var type = twin.Tags?.GetValueOrDefault<string>(Constants.TwinPropertyTypeKey, null);
            var ctx = new OperationContextModel
            {
                Time = timestamp
            };
            switch (type)
            {
                case Constants.EntityTypeGateway:
                    await (_gateways?.OnGatewayDeletedAsync(ctx, twin.Id)).ConfigureAwait(false);
                    break;
                case Constants.EntityTypeEndpoint:
                    await (_endpoints?.OnEndpointDeletedAsync(ctx, twin.Id,
                        twin.ToEndpointRegistration(true).ToServiceModel())).ConfigureAwait(false);
                    break;
                case Constants.EntityTypeApplication:
                    await (_applications?.OnApplicationDeletedAsync(ctx, twin.Id,
                        twin.ToApplicationRegistration().ToServiceModel())).ConfigureAwait(false);
                    break;
                case Constants.EntityTypePublisher:
                    await (_supervisors?.OnSupervisorDeletedAsync(ctx,
                        HubResource.Format(null, twin.Id, twin.ModuleId))).ConfigureAwait(false);
                    await (_publishers?.OnPublisherDeletedAsync(ctx,
                        HubResource.Format(null, twin.Id, twin.ModuleId))).ConfigureAwait(false);
                    await (_discoverers?.OnDiscovererDeletedAsync(ctx,
                        HubResource.Format(null, twin.Id, twin.ModuleId))).ConfigureAwait(false);
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
            var type = twin.Tags?.GetValueOrDefault<string>(OpcUa.Constants.TwinPropertyTypeKey, null);
            var ctx = new OperationContextModel
            {
                Time = timestamp
            };
            switch (type)
            {
                case Constants.EntityTypeGateway:
                    await (_gateways?.OnGatewayNewAsync(ctx,
                        twin.ToGatewayRegistration().ToServiceModel())).ConfigureAwait(false);
                    break;
                case Constants.EntityTypeEndpoint:
                    await (_endpoints?.OnEndpointNewAsync(ctx,
                        twin.ToEndpointRegistration(true).ToServiceModel())).ConfigureAwait(false);
                    break;
                case Constants.EntityTypeApplication:
                    await (_applications?.OnApplicationNewAsync(ctx,
                        twin.ToApplicationRegistration().ToServiceModel())).ConfigureAwait(false);
                    break;
                case Constants.EntityTypePublisher:
                    await (_supervisors?.OnSupervisorNewAsync(ctx,
                        twin.ToPublisherRegistration(true).ToSupervisorModel())).ConfigureAwait(false);
                    await (_publishers?.OnPublisherNewAsync(ctx,
                        twin.ToPublisherRegistration(true).ToPublisherModel())).ConfigureAwait(false);
                    await (_discoverers?.OnDiscovererNewAsync(ctx,
                        twin.ToPublisherRegistration().ToDiscovererModel())).ConfigureAwait(false);
                    break;
            }
        }

        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IIoTHubTwinServices _iothub;
        private readonly IGatewayRegistryListener _gateways;
        private readonly IPublisherRegistryListener _publishers;
        private readonly IApplicationRegistryListener _applications;
        private readonly IEndpointRegistryListener _endpoints;
        private readonly ISupervisorRegistryListener _supervisors;
        private readonly IDiscovererRegistryListener _discoverers;
    }
}
