// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Application twin event handler
    /// </summary>
    public sealed class ApplicationTwinEventHandler : IIoTHubDeviceTwinEventHandler {

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="iothub"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        public ApplicationTwinEventHandler(IIoTHubTwinServices iothub,
            IRegistryEventBroker<IApplicationRegistryListener> broker, ILogger logger) {
            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        }

        /// <inheritdoc/>
        public async Task HandleDeviceTwinEventAsync(DeviceTwinEvent ev) {
            if (ev.Handled) {
                return;
            }
            if (string.IsNullOrEmpty(ev.Twin.Id)) {
                return;
            }
            var type = ev.Twin.Tags.GetValueOrDefault<string>(
                nameof(EntityRegistration.DeviceType), null);
            if ((ev.Event != DeviceTwinEventType.Delete && ev.IsPatch) || string.IsNullOrEmpty(type)) {
                try {
                    ev.Twin = await _iothub.GetAsync(ev.Twin.Id);
                    ev.IsPatch = false;
                    type = ev.Twin.Tags.GetValueOrDefault<string>(
                        nameof(EntityRegistration.DeviceType), null);
                }
                catch (Exception ex) {
                    _logger.Verbose(ex, "Failed to materialize twin");
                }
            }
            if (IdentityType.Application.EqualsIgnoreCase(type)) {
                var ctx = new RegistryOperationContextModel {
                    AuthorityId = ev.AuthorityId,
                    Time = ev.Timestamp
                };
                switch (ev.Event) {
                    case DeviceTwinEventType.New:
                        break;
                    case DeviceTwinEventType.Create:
                        await _broker.NotifyAllAsync(l => l.OnApplicationNewAsync(ctx,
                            ev.Twin.ToApplicationRegistration().ToServiceModel()));
                        break;
                    case DeviceTwinEventType.Update:
                        await _broker.NotifyAllAsync(l => l.OnApplicationUpdatedAsync(ctx,
                            ev.Twin.ToApplicationRegistration().ToServiceModel()));
                        break;
                    case DeviceTwinEventType.Delete:
                        await _broker.NotifyAllAsync(l => l.OnApplicationDeletedAsync(ctx,
                            ev.Twin.Id,
                            ev.Twin.ToApplicationRegistration().ToServiceModel()));
                        break;
                }
                ev.Handled = true;
            }
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly ILogger _logger;
        private readonly IRegistryEventBroker<IApplicationRegistryListener> _broker;
    }
}
