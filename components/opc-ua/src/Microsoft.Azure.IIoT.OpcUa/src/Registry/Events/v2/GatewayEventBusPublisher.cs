﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Gateway registry event gateway
    /// </summary>
    public class GatewayEventBusPublisher : IGatewayRegistryListener {

        /// <summary>
        /// Create event gateway
        /// </summary>
        /// <param name="bus"></param>
        public GatewayEventBusPublisher(IEventBus bus) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <inheritdoc/>
        public Task OnGatewayDeletedAsync(RegistryOperationContextModel context,
            string gatewayId) {
            return _bus.PublishAsync(Wrap(GatewayEventType.Deleted, context,
                gatewayId, null, false));
        }

        /// <inheritdoc/>
        public Task OnGatewayNewAsync(RegistryOperationContextModel context,
            GatewayModel gateway) {
            return _bus.PublishAsync(Wrap(GatewayEventType.New, context,
                gateway.Id, gateway, false));
        }

        /// <inheritdoc/>
        public Task OnGatewayUpdatedAsync(RegistryOperationContextModel context,
            GatewayModel gateway, bool isPatch) {
            return _bus.PublishAsync(Wrap(GatewayEventType.Updated, context,
                gateway.Id, gateway, isPatch));
        }

        /// <summary>
        /// Create gateway event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="gatewayId"></param>
        /// <param name="gateway"></param>
        /// <param name="isPatch"></param>
        /// <returns></returns>
        private static GatewayEventModel Wrap(GatewayEventType type,
            RegistryOperationContextModel context, string gatewayId,
            GatewayModel gateway, bool isPatch) {
            return new GatewayEventModel {
                EventType = type,
                Context = context,
                Id = gatewayId,
                Gateway = gateway,
                IsPatch = isPatch == true ? true : (bool?)null
            };
        }

        private readonly IEventBus _bus;
    }
}
