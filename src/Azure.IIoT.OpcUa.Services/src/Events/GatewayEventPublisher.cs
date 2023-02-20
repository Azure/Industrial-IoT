// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Events {
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Gateway registry event publisher
    /// </summary>
    public class GatewayEventPublisher<THub> : IGatewayRegistryListener {

        /// <inheritdoc/>
        public GatewayEventPublisher(ICallbackInvokerT<THub> callback) {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task OnGatewayDeletedAsync(OperationContextModel context,
            string gatewayId) {
            return PublishAsync(GatewayEventType.Deleted, context,
                gatewayId, null);
        }

        /// <inheritdoc/>
        public Task OnGatewayNewAsync(OperationContextModel context,
            GatewayModel gateway) {
            return PublishAsync(GatewayEventType.New, context,
                gateway.Id, gateway);
        }

        /// <inheritdoc/>
        public Task OnGatewayUpdatedAsync(OperationContextModel context,
            GatewayModel gateway) {
            return PublishAsync(GatewayEventType.Updated, context,
                gateway.Id, gateway);
        }

        /// <summary>
        /// Publish gateway event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="gatewayId"></param>
        /// <param name="gateway"></param>
        /// <returns></returns>
        public Task PublishAsync(GatewayEventType type,
            OperationContextModel context, string gatewayId,
            GatewayModel gateway) {
            var arguments = new object[] {
                new GatewayEventModel {
                    EventType = type,
                    Context = context,
                    Id = gatewayId,
                    Gateway = gateway
                }
            };
            return _callback.BroadcastAsync(
                EventTargets.GatewayEventTarget, arguments);
        }
        private readonly ICallbackInvoker _callback;
    }
}
